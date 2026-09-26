using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartek.Common.Errors;
using Smartek.Common.Security;
using Stagiaire.Contracts.Events;
using SubjectMatching.Service.Data;
using SubjectMatching.Service.DTOs;
using SubjectMatching.Service.Models;

namespace SubjectMatching.Service.Controllers;

[ApiController]
[Route("api/v1/subject-change-requests")]
[Produces("application/json")]
[Authorize]
public class SubjectChangeRequestsController : ControllerBase
{
    private readonly SubjectMatchingDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SubjectChangeRequestsController> _logger;

    public SubjectChangeRequestsController(
        SubjectMatchingDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        ILogger<SubjectChangeRequestsController> logger)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Learner submits a change request for a different subject with a mandatory justification.
    /// Emits SubjectChangeRequested event.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubjectChangeRequestReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectChangeRequestReadDto>> RequestChange(
        [FromBody] CreateChangeRequestDto dto,
        CancellationToken cancellationToken)
    {
        var callerId = User.GetUserId();

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(ApiErrorResponse.Create("Le motif de la demande de changement est obligatoire.", "REASON_REQUIRED"));
        }

        var assignment = await _dbContext.SubjectAssignments
            .Include(a => a.Subject)
            .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId, cancellationToken);

        if (assignment is null || assignment.Subject is null)
        {
            throw NotFoundException.For("L'affectation de sujet", dto.AssignmentId);
        }

        if (User.IsLearner() && assignment.UtilisateurId != callerId)
        {
            throw NotFoundException.For("L'affectation de sujet", dto.AssignmentId);
        }

        var requestedSubject = await _dbContext.Subjects
            .FirstOrDefaultAsync(s => s.Id == dto.RequestedSubjectId, cancellationToken);

        if (requestedSubject is null)
        {
            throw NotFoundException.For("Le sujet demandé", dto.RequestedSubjectId);
        }

        if (requestedSubject.Status != SubjectStatus.Open)
        {
            return BadRequest(ApiErrorResponse.Create("Le sujet demandé n'est plus ouvert aux candidatures.", "SUBJECT_NOT_OPEN"));
        }

        var changeRequest = new SubjectChangeRequest
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignment.Id,
            StagiaireId = assignment.StagiaireId,
            UtilisateurId = assignment.UtilisateurId,
            CurrentSubjectId = assignment.SubjectId,
            RequestedSubjectId = dto.RequestedSubjectId,
            Reason = dto.Reason.Trim(),
            Status = ChangeRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            ReviewedAt = null
        };

        assignment.Status = AssignmentStatus.ChangeRequested;

        _dbContext.SubjectChangeRequests.Add(changeRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var traceId = HttpContext.TraceIdentifier;
        await _publishEndpoint.Publish(new SubjectChangeRequested(
            changeRequest.Id,
            changeRequest.StagiaireId,
            changeRequest.UtilisateurId,
            changeRequest.CurrentSubjectId,
            changeRequest.RequestedSubjectId,
            changeRequest.Reason,
            traceId
        ), cancellationToken);

        _logger.LogInformation("Change request {RequestId} submitted by user {UserId} for subject {SubjectId}",
            changeRequest.Id, changeRequest.UtilisateurId, changeRequest.RequestedSubjectId);

        return CreatedAtAction(nameof(GetById), new { id = changeRequest.Id }, ToDto(changeRequest, assignment.Subject, requestedSubject));
    }

    /// <summary>
    /// Admin lists all subject change requests.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(List<SubjectChangeRequestReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SubjectChangeRequestReadDto>>> GetAll(
        [FromQuery] ChangeRequestStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SubjectChangeRequests
            .Include(r => r.CurrentSubject)
            .Include(r => r.RequestedSubject)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var list = await query
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => ToDto(r, r.CurrentSubject!, r.RequestedSubject!))
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubjectChangeRequestReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectChangeRequestReadDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var callerId = User.GetUserId();

        var request = await _dbContext.SubjectChangeRequests
            .Include(r => r.CurrentSubject)
            .Include(r => r.RequestedSubject)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null || request.CurrentSubject is null || request.RequestedSubject is null)
        {
            throw NotFoundException.For("La demande de changement de sujet", id);
        }

        if (User.IsLearner() && request.UtilisateurId != callerId)
        {
            throw NotFoundException.For("La demande de changement de sujet", id);
        }

        return Ok(ToDto(request, request.CurrentSubject, request.RequestedSubject));
    }

    /// <summary>
    /// Admin approves a subject change request.
    /// Reallocates the assignment to the requested subject and emits SubjectChangeReviewed event.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(SubjectChangeRequestReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectChangeRequestReadDto>> Approve(
        Guid id,
        [FromBody] ReviewChangeRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var request = await _dbContext.SubjectChangeRequests
            .Include(r => r.CurrentSubject)
            .Include(r => r.RequestedSubject)
            .Include(r => r.Assignment)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null || request.CurrentSubject is null || request.RequestedSubject is null || request.Assignment is null)
        {
            throw NotFoundException.For("La demande de changement de sujet", id);
        }

        if (request.Status != ChangeRequestStatus.Pending)
        {
            return BadRequest(ApiErrorResponse.Create("Cette demande a déjà été traitée.", "REQUEST_ALREADY_REVIEWED"));
        }

        // Apply changes
        request.Status = ChangeRequestStatus.Approved;
        request.AdminComment = dto?.Comment?.Trim();
        request.ReviewedAt = DateTime.UtcNow;

        // If previously accepted, decrement filled positions on old subject
        if (request.Assignment.Status == AssignmentStatus.Accepted && request.CurrentSubject.FilledPositions > 0)
        {
            request.CurrentSubject.FilledPositions--;
            if (request.CurrentSubject.Status == SubjectStatus.Full)
            {
                request.CurrentSubject.Status = SubjectStatus.Open;
            }
        }

        // Reassign to requested subject
        request.Assignment.SubjectId = request.RequestedSubjectId;
        request.Assignment.Status = AssignmentStatus.Accepted;
        request.Assignment.RespondedAt = DateTime.UtcNow;

        request.RequestedSubject.FilledPositions++;
        if (request.RequestedSubject.FilledPositions >= request.RequestedSubject.AvailablePositions)
        {
            request.RequestedSubject.Status = SubjectStatus.Full;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var traceId = HttpContext.TraceIdentifier;
        await _publishEndpoint.Publish(new SubjectChangeReviewed(
            request.Id,
            request.StagiaireId,
            request.UtilisateurId,
            request.RequestedSubjectId,
            true,
            request.AdminComment,
            traceId
        ), cancellationToken);

        _logger.LogInformation("Change request {RequestId} approved for user {UserId}. Assigned to {NewSubjectId}",
            request.Id, request.UtilisateurId, request.RequestedSubjectId);

        return Ok(ToDto(request, request.CurrentSubject, request.RequestedSubject));
    }

    /// <summary>
    /// Admin rejects a subject change request with an explanation.
    /// Emits SubjectChangeReviewed event.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(SubjectChangeRequestReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectChangeRequestReadDto>> Reject(
        Guid id,
        [FromBody] ReviewChangeRequestDto dto,
        CancellationToken cancellationToken)
    {
        var request = await _dbContext.SubjectChangeRequests
            .Include(r => r.CurrentSubject)
            .Include(r => r.RequestedSubject)
            .Include(r => r.Assignment)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null || request.CurrentSubject is null || request.RequestedSubject is null || request.Assignment is null)
        {
            throw NotFoundException.For("La demande de changement de sujet", id);
        }

        if (request.Status != ChangeRequestStatus.Pending)
        {
            return BadRequest(ApiErrorResponse.Create("Cette demande a déjà été traitée.", "REQUEST_ALREADY_REVIEWED"));
        }

        request.Status = ChangeRequestStatus.Rejected;
        request.AdminComment = dto.Comment?.Trim();
        request.ReviewedAt = DateTime.UtcNow;

        // Reset assignment status back to Proposed
        request.Assignment.Status = AssignmentStatus.Proposed;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var traceId = HttpContext.TraceIdentifier;
        await _publishEndpoint.Publish(new SubjectChangeReviewed(
            request.Id,
            request.StagiaireId,
            request.UtilisateurId,
            request.CurrentSubjectId,
            false,
            request.AdminComment,
            traceId
        ), cancellationToken);

        _logger.LogInformation("Change request {RequestId} rejected for user {UserId}", request.Id, request.UtilisateurId);

        return Ok(ToDto(request, request.CurrentSubject, request.RequestedSubject));
    }

    private static SubjectChangeRequestReadDto ToDto(SubjectChangeRequest r, InternshipSubject current, InternshipSubject requested) => new()
    {
        Id = r.Id,
        AssignmentId = r.AssignmentId,
        StagiaireId = r.StagiaireId,
        UtilisateurId = r.UtilisateurId,
        CurrentSubjectId = r.CurrentSubjectId,
        CurrentSubjectTitle = current.Title,
        RequestedSubjectId = r.RequestedSubjectId,
        RequestedSubjectTitle = requested.Title,
        Reason = r.Reason,
        Status = r.Status,
        AdminComment = r.AdminComment,
        RequestedAt = r.RequestedAt,
        ReviewedAt = r.ReviewedAt
    };
}

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
[Route("api/v1/subject-assignments")]
[Produces("application/json")]
[Authorize]
public class SubjectAssignmentsController : ControllerBase
{
    private readonly SubjectMatchingDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SubjectAssignmentsController> _logger;

    public SubjectAssignmentsController(
        SubjectMatchingDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        ILogger<SubjectAssignmentsController> logger)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Admin proposes an internship subject to a candidate.
    /// Emits SubjectProposed event via RabbitMQ.
    /// </summary>
    [HttpPost("propose")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(SubjectAssignmentReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectAssignmentReadDto>> ProposeSubject(
        [FromBody] ProposeSubjectDto dto,
        CancellationToken cancellationToken)
    {
        var subject = await _dbContext.Subjects
            .FirstOrDefaultAsync(s => s.Id == dto.SubjectId, cancellationToken);

        if (subject is null)
        {
            throw NotFoundException.For("Le sujet de stage", dto.SubjectId);
        }

        if (subject.Status == SubjectStatus.Full || subject.Status == SubjectStatus.Closed)
        {
            return BadRequest(ApiErrorResponse.Create("Le sujet sélectionné n'est plus disponible.", "SUBJECT_NOT_AVAILABLE"));
        }

        // Check if there is already a match record to retrieve the score
        var match = await _dbContext.SubjectMatches
            .FirstOrDefaultAsync(m => m.StagiaireId == dto.StagiaireId && m.SubjectId == dto.SubjectId, cancellationToken);

        var score = match?.CompatibilityScore ?? 75.0m;

        // Check if candidate profile has utilisateurId
        var profile = await _dbContext.CandidateProfiles
            .FirstOrDefaultAsync(p => p.StagiaireId == dto.StagiaireId, cancellationToken);

        var utilisateurId = dto.UtilisateurId > 0 ? dto.UtilisateurId : (profile?.UtilisateurId ?? 0);

        // Deactivate or supersede any previous proposed assignment for this stagiaire
        var existing = await _dbContext.SubjectAssignments
            .Where(a => a.StagiaireId == dto.StagiaireId && a.Status == AssignmentStatus.Proposed)
            .ToListAsync(cancellationToken);

        foreach (var old in existing)
        {
            old.Status = AssignmentStatus.Reassigned;
        }

        var assignment = new SubjectAssignment
        {
            Id = Guid.NewGuid(),
            StagiaireId = dto.StagiaireId,
            UtilisateurId = utilisateurId,
            SubjectId = dto.SubjectId,
            Status = AssignmentStatus.Proposed,
            ScoreAtProposal = score,
            ProposedAt = DateTime.UtcNow,
            RespondedAt = null
        };

        _dbContext.SubjectAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish SubjectProposed event for notification and tracking
        var traceId = HttpContext.TraceIdentifier;
        await _publishEndpoint.Publish(new SubjectProposed(
            dto.StagiaireId,
            utilisateurId,
            dto.SubjectId,
            subject.Title,
            score,
            traceId,
            dto.CandidateEmail,
            dto.CandidateName,
            dto.EncadrantNom
        ), cancellationToken);

        _logger.LogInformation("Subject {SubjectId} proposed to Stagiaire {StagiaireId} (UtilisateurId: {UtilisateurId})",
            dto.SubjectId, dto.StagiaireId, utilisateurId);

        return CreatedAtAction(nameof(GetById), new { id = assignment.Id }, ToDto(assignment, subject));
    }

    /// <summary>
    /// Gets the current proposed or active subject assignment for the authenticated learner.
    /// Strictly scoped by caller's JWT userId.
    /// </summary>
    [HttpGet("my-assignment")]
    [ProducesResponseType(typeof(SubjectAssignmentReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectAssignmentReadDto>> GetMyAssignment(CancellationToken cancellationToken)
    {
        var callerId = User.GetUserId();
        if (callerId is null)
        {
            throw new NotFoundException("Identité de l'utilisateur introuvable.");
        }

        var assignment = await _dbContext.SubjectAssignments
            .Include(a => a.Subject)
            .Where(a => a.UtilisateurId == callerId &&
                        (a.Status == AssignmentStatus.Proposed || a.Status == AssignmentStatus.Accepted || a.Status == AssignmentStatus.ChangeRequested))
            .OrderByDescending(a => a.ProposedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null || assignment.Subject is null)
        {
            throw new NotFoundException("Aucun sujet de stage ne vous a encore été proposé.");
        }

        return Ok(ToDto(assignment, assignment.Subject));
    }

    [HttpGet("stagiaire/{stagiaireId:guid}")]
    [ProducesResponseType(typeof(SubjectAssignmentReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectAssignmentReadDto>> GetByStagiaireId(
        Guid stagiaireId,
        CancellationToken cancellationToken)
    {
        var callerId = User.GetUserId();

        var query = _dbContext.SubjectAssignments
            .Include(a => a.Subject)
            .Where(a => a.StagiaireId == stagiaireId);

        // Learners can only inspect their own assignment
        if (User.IsLearner())
        {
            if (callerId is null) throw new NotFoundException("Affectation introuvable.");
            query = query.Where(a => a.UtilisateurId == callerId);
        }

        var assignment = await query
            .OrderByDescending(a => a.ProposedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null || assignment.Subject is null)
        {
            throw NotFoundException.For("L'affectation de sujet pour le stagiaire", stagiaireId);
        }

        return Ok(ToDto(assignment, assignment.Subject));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubjectAssignmentReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectAssignmentReadDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var callerId = User.GetUserId();

        var query = _dbContext.SubjectAssignments
            .Include(a => a.Subject)
            .Where(a => a.Id == id);

        if (User.IsLearner())
        {
            if (callerId is null) throw new NotFoundException("Affectation introuvable.");
            query = query.Where(a => a.UtilisateurId == callerId);
        }

        var assignment = await query.FirstOrDefaultAsync(cancellationToken);
        if (assignment is null || assignment.Subject is null)
        {
            throw NotFoundException.For("L'affectation de sujet", id);
        }

        return Ok(ToDto(assignment, assignment.Subject));
    }

    /// <summary>
    /// Candidate accepts the proposed subject.
    /// Increments FilledPositions, locks assignment, and emits SubjectAccepted event.
    /// </summary>
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(typeof(SubjectAssignmentReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectAssignmentReadDto>> AcceptSubject(
        Guid id,
        CancellationToken cancellationToken)
    {
        var callerId = User.GetUserId();

        var assignment = await _dbContext.SubjectAssignments
            .Include(a => a.Subject)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (assignment is null || assignment.Subject is null)
        {
            throw NotFoundException.For("L'affectation de sujet", id);
        }

        // Only the assigned learner or an admin can accept
        if (User.IsLearner() && assignment.UtilisateurId != callerId)
        {
            // Return 404 to avoid leaking existence
            throw NotFoundException.For("L'affectation de sujet", id);
        }

        if (assignment.Status == AssignmentStatus.Accepted)
        {
            return Ok(ToDto(assignment, assignment.Subject));
        }

        var subject = assignment.Subject;

        // Update positions
        subject.FilledPositions++;
        if (subject.FilledPositions >= subject.AvailablePositions)
        {
            subject.Status = SubjectStatus.Full;
        }

        assignment.Status = AssignmentStatus.Accepted;
        assignment.RespondedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish SubjectAccepted
        var traceId = HttpContext.TraceIdentifier;
        await _publishEndpoint.Publish(new SubjectAccepted(
            assignment.StagiaireId,
            assignment.UtilisateurId,
            assignment.SubjectId,
            subject.Title,
            traceId
        ), cancellationToken);

        _logger.LogInformation("Subject {SubjectId} accepted by user {UserId}", subject.Id, assignment.UtilisateurId);

        return Ok(ToDto(assignment, subject));
    }

    private static SubjectAssignmentReadDto ToDto(SubjectAssignment a, InternshipSubject s) => new()
    {
        Id = a.Id,
        StagiaireId = a.StagiaireId,
        UtilisateurId = a.UtilisateurId,
        SubjectId = a.SubjectId,
        SubjectTitle = s.Title,
        SubjectDescription = s.Description,
        ProblemStatement = s.ProblemStatement,
        Department = s.Department,
        RequiredSkills = s.RequiredSkills,
        Status = a.Status,
        ScoreAtProposal = a.ScoreAtProposal,
        ProposedAt = a.ProposedAt,
        RespondedAt = a.RespondedAt
    };
}

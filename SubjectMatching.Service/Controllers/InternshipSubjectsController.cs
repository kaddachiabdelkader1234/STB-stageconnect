using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartek.Common.Errors;
using Smartek.Common.Security;
using SubjectMatching.Service.Data;
using SubjectMatching.Service.DTOs;
using SubjectMatching.Service.Models;

namespace SubjectMatching.Service.Controllers;

[ApiController]
[Route("api/v1/subjects")]
[Produces("application/json")]
[Authorize]
public class InternshipSubjectsController : ControllerBase
{
    private readonly SubjectMatchingDbContext _dbContext;
    private readonly ILogger<InternshipSubjectsController> _logger;

    public InternshipSubjectsController(
        SubjectMatchingDbContext dbContext,
        ILogger<InternshipSubjectsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SubjectReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SubjectReadDto>>> GetAll(
        [FromQuery] SubjectStatus? status = null,
        [FromQuery] string? department = null,
        [FromQuery] string? typeStage = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Subjects.AsNoTracking().AsQueryable();

        // Learners only see Open subjects or subjects they can request
        if (User.IsLearner())
        {
            query = query.Where(s => s.Status == SubjectStatus.Open);
        }
        else if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(s => s.Department.ToLower() == department.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(typeStage))
        {
            query = query.Where(s => s.TypeStage.ToLower() == typeStage.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(s) || x.Description.ToLower().Contains(s));
        }

        var list = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => ToDto(s))
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubjectReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectReadDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Subjects.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null)
        {
            throw NotFoundException.For("Le sujet de stage", id);
        }

        return Ok(ToDto(entity));
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(SubjectReadDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SubjectReadDto>> Create(
        [FromBody] SubjectCreateDto dto,
        CancellationToken cancellationToken)
    {
        var callerId = User.GetUserId() ?? 0;

        var entity = new InternshipSubject
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            ProblemStatement = dto.ProblemStatement.Trim(),
            Department = dto.Department.Trim(),
            TypeStage = dto.TypeStage.Trim(),
            RequiredSkills = dto.RequiredSkills ?? new List<string>(),
            PreferredSkills = dto.PreferredSkills ?? new List<string>(),
            EducationRequirements = dto.EducationRequirements?.Trim(),
            ExperienceRequirements = dto.ExperienceRequirements?.Trim(),
            Difficulty = dto.Difficulty,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            AvailablePositions = dto.AvailablePositions,
            FilledPositions = 0,
            Status = SubjectStatus.Open,
            CreatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Subjects.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Internship subject created: {Id} - {Title}", entity.Id, entity.Title);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(SubjectReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectReadDto>> Update(
        Guid id,
        [FromBody] SubjectUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Subjects.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            throw NotFoundException.For("Le sujet de stage", id);
        }

        entity.Title = dto.Title.Trim();
        entity.Description = dto.Description.Trim();
        entity.ProblemStatement = dto.ProblemStatement.Trim();
        entity.Department = dto.Department.Trim();
        entity.TypeStage = dto.TypeStage.Trim();
        entity.RequiredSkills = dto.RequiredSkills ?? new List<string>();
        entity.PreferredSkills = dto.PreferredSkills ?? new List<string>();
        entity.EducationRequirements = dto.EducationRequirements?.Trim();
        entity.ExperienceRequirements = dto.ExperienceRequirements?.Trim();
        entity.Difficulty = dto.Difficulty;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.AvailablePositions = dto.AvailablePositions;
        entity.Status = dto.Status;
        entity.UpdatedAt = DateTime.UtcNow;

        if (entity.FilledPositions >= entity.AvailablePositions && entity.Status == SubjectStatus.Open)
        {
            entity.Status = SubjectStatus.Full;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Internship subject updated: {Id}", entity.Id);

        return Ok(ToDto(entity));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Subjects.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            throw NotFoundException.For("Le sujet de stage", id);
        }

        _dbContext.Subjects.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Internship subject deleted: {Id}", id);

        return NoContent();
    }

    private static SubjectReadDto ToDto(InternshipSubject s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        Description = s.Description,
        ProblemStatement = s.ProblemStatement,
        Department = s.Department,
        TypeStage = s.TypeStage,
        RequiredSkills = s.RequiredSkills,
        PreferredSkills = s.PreferredSkills,
        EducationRequirements = s.EducationRequirements,
        ExperienceRequirements = s.ExperienceRequirements,
        Difficulty = s.Difficulty,
        StartDate = s.StartDate,
        EndDate = s.EndDate,
        AvailablePositions = s.AvailablePositions,
        FilledPositions = s.FilledPositions,
        Status = s.Status,
        CreatedBy = s.CreatedBy,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}

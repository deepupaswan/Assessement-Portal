using AssessmentService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssessmentService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssessmentsController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;

    public AssessmentsController(IAssessmentService assessmentService)
    {
        _assessmentService = assessmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAssessments()
    {
        var assessments = await _assessmentService.GetAllAssessmentsAsync();
        return Ok(assessments.Select(MapAssessment).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAssessment(Guid id)
    {
        var assessment = await _assessmentService.GetAssessmentByIdAsync(id);
        if (assessment == null)
            return NotFound();

        return Ok(MapAssessment(assessment));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required");

        var assessment = await _assessmentService.CreateAssessmentAsync(
            request.Title,
            request.Description,
            request.DurationMinutes,
            request.RandomizeQuestions);

        return CreatedAtAction(nameof(GetAssessment), new { id = assessment.Id }, MapAssessment(assessment));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAssessment(Guid id, [FromBody] UpdateAssessmentRequest request)
    {
        var success = await _assessmentService.UpdateAssessmentAsync(
            id,
            request.Title,
            request.Description,
            request.DurationMinutes,
            request.RandomizeQuestions);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAssessment(Guid id)
    {
        var success = await _assessmentService.DeleteAssessmentAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }

    private static AssessmentService.Application.DTOs.AssessmentDto MapAssessment(AssessmentService.Domain.Entities.Assessment assessment)
    {
        return new AssessmentService.Application.DTOs.AssessmentDto
        {
            Id = assessment.Id,
            Title = assessment.Title,
            Description = assessment.Description,
            DurationMinutes = assessment.DurationMinutes,
            IsPublished = assessment.IsPublished,
            QuestionCount = assessment.Questions.Count,
            RandomizeQuestions = assessment.RandomizeQuestions,
            CreatedAt = assessment.CreatedAt
        };
    }
}

public class CreateAssessmentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 60;
    public bool RandomizeQuestions { get; set; }
}

public class UpdateAssessmentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 60;
    public bool RandomizeQuestions { get; set; }
}

using AssessmentService.Application.Services;
using AssessmentService.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssessmentService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssessmentsController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;

    public AssessmentsController(IAssessmentService assessmentService)
    {
        _assessmentService = assessmentService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAssessments()
    {
        var assessments = await _assessmentService.GetAllAssessmentsAsync();
        return Ok(assessments.Select(MapAssessment).ToList());
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Candidate")]
    public async Task<IActionResult> GetAssessment(Guid id)
    {
        var assessment = await _assessmentService.GetAssessmentByIdAsync(id);
        if (assessment == null)
            return NotFound();

        return Ok(MapAssessment(assessment));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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

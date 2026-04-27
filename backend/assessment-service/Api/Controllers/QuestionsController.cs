using AssessmentService.Application.DTOs;
using AssessmentService.Application.Services;
using AssessmentService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssessmentService.Api.Controllers;

[ApiController]
[Route("api/assessments/{assessmentId}/questions")]
[Authorize]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;

    public QuestionsController(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Candidate")]
    public async Task<IActionResult> GetQuestions(Guid assessmentId)
    {
        var questions = await _questionService.GetQuestionsByAssessmentIdAsync(assessmentId);
        var dtos = questions.Select(q => new QuestionDto
        {
            Id = q.Id,
            AssessmentId = q.AssessmentId,
            Text = q.Text,
            Type = q.Type,
            MaxScore = q.MaxScore,
            CorrectAnswer = q.CorrectAnswer,
            IsRequired = q.IsRequired,
            Order = q.Order,
            CreatedAt = q.CreatedAt,
            Options = q.Options.Select(o => new QuestionOptionDto
            {
                Id = o.Id,
                Text = o.Text,
                IsCorrect = o.IsCorrect,
                Order = o.Order
            }).ToList()
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{questionId}")]
    [Authorize(Roles = "Admin,Candidate")]
    public async Task<IActionResult> GetQuestion(Guid assessmentId, Guid questionId)
    {
        var question = await _questionService.GetQuestionByIdAsync(questionId);
        if (question == null || question.AssessmentId != assessmentId)
            return NotFound();

        var dto = new QuestionDto
        {
            Id = question.Id,
            AssessmentId = question.AssessmentId,
            Text = question.Text,
            Type = question.Type,
            MaxScore = question.MaxScore,
            CorrectAnswer = question.CorrectAnswer,
            IsRequired = question.IsRequired,
            Order = question.Order,
            CreatedAt = question.CreatedAt,
            Options = question.Options.Select(o => new QuestionOptionDto
            {
                Id = o.Id,
                Text = o.Text,
                IsCorrect = o.IsCorrect,
                Order = o.Order
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateQuestion(Guid assessmentId, [FromBody] CreateQuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Question text is required");

        var normalizedType = string.IsNullOrWhiteSpace(request.Type) ? "MCQ" : request.Type.Trim().ToUpperInvariant();

        if (normalizedType == "MCQ" && (request.Options == null || !request.Options.Any()))
            return BadRequest("At least one option is required");

        if (normalizedType == "MCQ" && !request.Options.Any(o => o.IsCorrect))
            return BadRequest("At least one option must be marked correct");

        var question = await _questionService.CreateQuestionAsync(
            assessmentId,
            request.Text,
            normalizedType,
            request.MaxScore,
            request.CorrectAnswer,
            request.IsRequired,
            request.Order);

        // Add options
        foreach (var option in request.Options.OrderBy(o => o.Order))
        {
            if (string.IsNullOrWhiteSpace(option.Text))
                return BadRequest("Option text is required");

            await _questionService.AddOptionAsync(question.Id, option.Text, option.IsCorrect, option.Order);
        }

        var createdQuestion = await _questionService.GetQuestionByIdAsync(question.Id);

        var dto = new QuestionDto
        {
            Id = createdQuestion!.Id,
            AssessmentId = createdQuestion.AssessmentId,
            Text = createdQuestion.Text,
            Type = createdQuestion.Type,
            MaxScore = createdQuestion.MaxScore,
            CorrectAnswer = createdQuestion.CorrectAnswer,
            IsRequired = createdQuestion.IsRequired,
            Order = createdQuestion.Order,
            CreatedAt = createdQuestion.CreatedAt,
            Options = createdQuestion.Options.Select(o => new QuestionOptionDto
            {
                Id = o.Id,
                Text = o.Text,
                IsCorrect = o.IsCorrect,
                Order = o.Order
            }).ToList()
        };

        return CreatedAtAction(nameof(GetQuestion), new { assessmentId, questionId = dto.Id }, dto);
    }

    [HttpPut("{questionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateQuestion(Guid assessmentId, Guid questionId, [FromBody] UpdateQuestionRequest request)
    {
        var question = await _questionService.GetQuestionByIdAsync(questionId);
        if (question == null || question.AssessmentId != assessmentId)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Question text is required");

        await _questionService.UpdateQuestionAsync(questionId, request.Text, request.Type, request.MaxScore, request.CorrectAnswer, request.IsRequired, request.Order);
        return NoContent();
    }

    [HttpDelete("{questionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteQuestion(Guid assessmentId, Guid questionId)
    {
        var question = await _questionService.GetQuestionByIdAsync(questionId);
        if (question == null || question.AssessmentId != assessmentId)
            return NotFound();

        await _questionService.DeleteQuestionAsync(questionId);
        return NoContent();
    }

    // Options endpoints
    [HttpPost("{questionId}/options")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddOption(Guid assessmentId, Guid questionId, [FromBody] CreateQuestionOptionRequest request)
    {
        var question = await _questionService.GetQuestionByIdAsync(questionId);
        if (question == null || question.AssessmentId != assessmentId)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Option text is required");

        var option = await _questionService.AddOptionAsync(questionId, request.Text, request.IsCorrect, request.Order);

        var dto = new QuestionOptionDto
        {
            Id = option.Id,
            Text = option.Text,
            IsCorrect = option.IsCorrect,
            Order = option.Order
        };

        return CreatedAtAction(nameof(GetQuestion), new { assessmentId, questionId }, dto);
    }

    [HttpPut("{questionId}/options/{optionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOption(Guid assessmentId, Guid questionId, Guid optionId, [FromBody] UpdateQuestionOptionRequest request)
    {
        var question = await _questionService.GetQuestionByIdAsync(questionId);
        if (question == null || question.AssessmentId != assessmentId)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Option text is required");

        await _questionService.UpdateOptionAsync(optionId, request.Text, request.IsCorrect, request.Order);
        return NoContent();
    }

    [HttpDelete("{questionId}/options/{optionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteOption(Guid assessmentId, Guid questionId, Guid optionId)
    {
        var question = await _questionService.GetQuestionByIdAsync(questionId);
        if (question == null || question.AssessmentId != assessmentId)
            return NotFound();

        await _questionService.DeleteOptionAsync(optionId);
        return NoContent();
    }
}

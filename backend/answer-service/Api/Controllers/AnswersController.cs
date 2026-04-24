using AnswerService.Application.DTOs;
using AnswerService.Application.Services;
using AnswerService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AnswerService.Api.Controllers
{
    [ApiController]
    [Route("api/assessments/{assessmentId}/answers")]
    public class AnswersController : ControllerBase
    {
        private readonly IAnswerService _answerService;
        private readonly ILogger<AnswersController> _logger;

        public AnswersController(IAnswerService answerService, ILogger<AnswersController> logger)
        {
            _answerService = answerService;
            _logger = logger;
        }

        /// <summary>
        /// Get all answers for an assessment
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAnswersByAssessment(Guid assessmentId)
        {
            try
            {
                var answers = await _answerService.GetByAssessmentIdAsync(assessmentId);
                var mapper = MapToDto(assessmentId);
                return Ok(answers.Select(mapper).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting answers for assessment {AssessmentId}", assessmentId);
                return StatusCode(500, new { message = "Failed to get answers" });
            }
        }

        /// <summary>
        /// Get answers for a specific candidate in an assessment
        /// </summary>
        [HttpGet("candidates/{candidateId}")]
        public async Task<IActionResult> GetCandidateAnswers(Guid assessmentId, Guid candidateId)
        {
            try
            {
                var answers = await _answerService.GetByCandidateAndAssessmentAsync(candidateId, assessmentId);
                var mapper = MapToDto(assessmentId);
                var response = new CandidateAnswersResponse
                {
                    CandidateId = candidateId,
                    AssessmentId = assessmentId,
                    Answers = answers.Select(mapper).ToList(),
                    TotalScore = answers.Sum(a => a.PointsObtained ?? 0),
                    SubmittedAt = answers.FirstOrDefault()?.SubmittedAt
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting answers for candidate {CandidateId} in assessment {AssessmentId}", candidateId, assessmentId);
                return StatusCode(500, new { message = "Failed to get candidate answers" });
            }
        }

        /// <summary>
        /// Get a specific answer
        /// </summary>
        [HttpGet("{answerId}")]
        public async Task<IActionResult> GetAnswer(Guid assessmentId, Guid answerId)
        {
            try
            {
                var answer = await _answerService.GetByIdAsync(answerId);
                if (answer == null)
                    return NotFound(new { message = "Answer not found" });

                if (answer.AssessmentId != assessmentId)
                    return BadRequest(new { message = "Answer does not belong to this assessment" });

                var mapper = MapToDto(assessmentId);
                return Ok(mapper(answer));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting answer {AnswerId}", answerId);
                return StatusCode(500, new { message = "Failed to get answer" });
            }
        }

        /// <summary>
        /// Submit a single answer
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitAnswer(Guid assessmentId, [FromBody] SubmitAnswerRequest request)
        {
            try
            {
                if (assessmentId != request.AssessmentId)
                    return BadRequest(new { message = "Assessment ID mismatch" });

                if (string.IsNullOrWhiteSpace(request.AnswerText) && request.SelectedOptionId == null)
                    return BadRequest(new { message = "Either answer text or selected option must be provided" });

                var answer = await _answerService.SubmitAnswerAsync(
                    request.AssessmentId,
                    request.CandidateId,
                    request.QuestionId,
                    request.SelectedOptionId,
                    request.AnswerText
                );

                var mapper = MapToDto(assessmentId);
                return CreatedAtAction(nameof(GetAnswer), new { assessmentId, answerId = answer.Id }, mapper(answer));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answer");
                return StatusCode(500, new { message = "Failed to submit answer" });
            }
        }

        /// <summary>
        /// Submit multiple answers in batch
        /// </summary>
        [HttpPost("submit-batch")]
        public async Task<IActionResult> SubmitAnswersBatch(Guid assessmentId, [FromBody] BatchSubmitAnswersRequest request)
        {
            try
            {
                if (assessmentId != request.AssessmentId)
                    return BadRequest(new { message = "Assessment ID mismatch" });

                if (!request.Answers.Any())
                    return BadRequest(new { message = "No answers to submit" });

                var submittedAnswers = new List<AnswerDto>();
                var mapper = MapToDto(assessmentId);
                foreach (var answerRequest in request.Answers)
                {
                    var requestAssessmentId = answerRequest.AssessmentId == Guid.Empty
                        ? request.AssessmentId
                        : answerRequest.AssessmentId;
                    var requestCandidateId = answerRequest.CandidateId == Guid.Empty
                        ? request.CandidateId
                        : answerRequest.CandidateId;

                    if (requestAssessmentId != request.AssessmentId)
                        return BadRequest(new { message = "All answers must belong to the batch assessment" });

                    if (requestCandidateId != request.CandidateId)
                        return BadRequest(new { message = "All answers must belong to the batch candidate" });

                    var answer = await _answerService.SubmitAnswerAsync(
                        requestAssessmentId,
                        requestCandidateId,
                        answerRequest.QuestionId,
                        answerRequest.SelectedOptionId,
                        answerRequest.AnswerText
                    );
                    submittedAnswers.Add(mapper(answer));
                }

                return Ok(new { submitted = submittedAnswers.Count, answers = submittedAnswers });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting batch answers");
                return StatusCode(500, new { message = "Failed to submit answers" });
            }
        }

        /// <summary>
        /// Save candidate answers from the Angular assessment page.
        /// </summary>
        [HttpPost("/api/answers/bulk-save")]
        public async Task<IActionResult> BulkSaveAnswers([FromBody] BulkSaveAnswersRequest request)
        {
            try
            {
                if (request.AssessmentId == Guid.Empty)
                    return BadRequest(new { message = "Assessment ID is required" });

                if (request.CandidateId == Guid.Empty)
                    return BadRequest(new { message = "Candidate ID is required" });

                if (request.Answers == null || !request.Answers.Any())
                    return BadRequest(new { message = "No answers to submit" });

                var submittedAnswers = new List<AnswerDto>();
                var mapper = MapToDto(request.AssessmentId);

                foreach (var answerRequest in request.Answers)
                {
                    var answerText = answerRequest.DescriptiveAnswer
                        ?? answerRequest.CodingAnswer
                        ?? string.Empty;

                    var answer = await _answerService.SubmitAnswerAsync(
                        request.AssessmentId,
                        request.CandidateId,
                        answerRequest.QuestionId,
                        answerRequest.SelectedOptionId,
                        answerText);

                    submittedAnswers.Add(mapper(answer));
                }

                return Ok(new { submitted = submittedAnswers.Count, answers = submittedAnswers });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk-saving answers");
                return StatusCode(500, new { message = "Failed to save answers" });
            }
        }

        /// <summary>
        /// Grade an answer
        /// </summary>
        [HttpPost("{answerId}/grade")]
        public async Task<IActionResult> GradeAnswer(Guid assessmentId, Guid answerId, [FromBody] GradeAnswerRequest request)
        {
            try
            {
                if (answerId != request.AnswerId)
                    return BadRequest(new { message = "Answer ID mismatch" });

                var answer = await _answerService.GetByIdAsync(answerId);
                if (answer == null)
                    return NotFound(new { message = "Answer not found" });

                if (answer.AssessmentId != assessmentId)
                    return BadRequest(new { message = "Answer does not belong to this assessment" });

                var gradedAnswer = await _answerService.GradeAnswerAsync(answerId, request.IsCorrect, request.PointsObtained, request.Notes);
                var mapper = MapToDto(assessmentId);
                return Ok(mapper(gradedAnswer));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error grading answer {AnswerId}", answerId);
                return StatusCode(500, new { message = "Failed to grade answer" });
            }
        }

        // Helper method to map Answer entity to DTO
        private Func<Answer, AnswerDto> MapToDto(Guid assessmentId)
        {
            return answer => new AnswerDto
            {
                Id = answer.Id,
                AssessmentId = answer.AssessmentId,
                CandidateId = answer.CandidateId,
                QuestionId = answer.QuestionId,
                SelectedOptionId = answer.SelectedOptionId,
                QuestionText = answer.QuestionText,
                AnswerText = answer.AnswerText,
                IsCorrect = answer.IsCorrect,
                PointsObtained = answer.PointsObtained,
                TotalPoints = answer.TotalPoints,
                SubmittedAt = answer.SubmittedAt,
                GradedAt = answer.GradedAt,
                GradingNotes = answer.GradingNotes
            };
        }
    }
}


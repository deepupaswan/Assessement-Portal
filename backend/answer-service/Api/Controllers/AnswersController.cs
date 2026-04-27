using AnswerService.Application.DTOs;
using AnswerService.Application.Services;
using AnswerService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnswerService.Api.Controllers
{
    [ApiController]
    [Route("api/assessments/{assessmentId}/answers")]
    [Authorize]
    public class AnswersController : ControllerBase
    {
        private readonly IAnswerService _answerService;

        public AnswersController(IAnswerService answerService)
        {
            _answerService = answerService;
        }

        /// <summary>
        /// Get all answers for an assessment
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAnswersByAssessment(Guid assessmentId)
        {
            var answers = await _answerService.GetByAssessmentIdAsync(assessmentId);
            var mapper = MapToDto(assessmentId);
            return Ok(answers.Select(mapper).ToList());
        }

        /// <summary>
        /// Get answers for a specific candidate in an assessment
        /// </summary>
        [HttpGet("candidates/{candidateId}")]
        [Authorize(Roles = "Admin,Candidate")]
        public async Task<IActionResult> GetCandidateAnswers(Guid assessmentId, Guid candidateId)
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

        /// <summary>
        /// Get a specific answer
        /// </summary>
        [HttpGet("{answerId}")]
        [Authorize(Roles = "Admin,Candidate")]
        public async Task<IActionResult> GetAnswer(Guid assessmentId, Guid answerId)
        {
            var answer = await _answerService.GetByIdAsync(answerId);
            if (answer == null)
                return NotFound(new { message = "Answer not found" });

            if (answer.AssessmentId != assessmentId)
                return BadRequest(new { message = "Answer does not belong to this assessment" });

            var mapper = MapToDto(assessmentId);
            return Ok(mapper(answer));
        }

        /// <summary>
        /// Submit a single answer
        /// </summary>
        [HttpPost("submit")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> SubmitAnswer(Guid assessmentId, [FromBody] SubmitAnswerRequest request)
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

        /// <summary>
        /// Submit multiple answers in batch
        /// </summary>
        [HttpPost("submit-batch")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> SubmitAnswersBatch(Guid assessmentId, [FromBody] BatchSubmitAnswersRequest request)
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

        /// <summary>
        /// Save candidate answers from the Angular assessment page.
        /// </summary>
        [HttpPost("/api/answers/bulk-save")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> BulkSaveAnswers([FromBody] BulkSaveAnswersRequest request)
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

        /// <summary>
        /// Grade an answer
        /// </summary>
        [HttpPost("{answerId}/grade")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GradeAnswer(Guid assessmentId, Guid answerId, [FromBody] GradeAnswerRequest request)
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


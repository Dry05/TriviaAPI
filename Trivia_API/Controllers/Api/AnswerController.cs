// Controllers/Api/AnswersController.cs  
using Microsoft.AspNetCore.Mvc;
using Trivia_API.Models;
using TriviaAPI.Models;

[Route("api/[controller]")]
[ApiController]
public class AnswerController : ControllerBase
{
    private readonly ITriviaService _triviaService;

    public AnswerController(ITriviaService triviaService)
    {
        _triviaService = triviaService;
    }

    [HttpPost("checkanswer")]
    public async Task<IActionResult> Check([FromBody] AnswerRequest request)
    {
        var isCorrect = await _triviaService.CheckAnswerAsync(request.QuestionId, request.UserAnswer);
        return Ok(new { isCorrect });
    }

    [HttpPost("checkanswers")]
    public async Task<IActionResult> CheckAnswersBatch([FromBody] BatchAnswerRequest request)
    {
        if (request?.Answers == null || !request.Answers.Any())
        {
            return BadRequest(new { error = "No answers provided" });
        }

        var results = new List<object>();
        var score = 0;

        foreach (var answer in request.Answers)
        {
            var isCorrect = await _triviaService.CheckAnswerAsync(answer.QuestionId, answer.UserAnswer);
            results.Add(new
            {
                questionId = answer.QuestionId,
                isCorrect = isCorrect
            });

            if (isCorrect) score++;
        }

        return Ok(new
        {
            results = results,
            totalScore = score,
            totalQuestions = request.Answers.Count
        });
    }
}
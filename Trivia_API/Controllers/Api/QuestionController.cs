// Controllers/Api/QuestionsController.cs
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class QuestionController : ControllerBase
{
    private readonly ITriviaService _triviaService;

    public QuestionController(ITriviaService triviaService)
    {
        _triviaService = triviaService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int amount = 10)
    {
        var questions = await _triviaService.GetQuestionsAsync(amount);
        return Ok(questions);
    }
}
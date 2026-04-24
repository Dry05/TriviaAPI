using Trivia_API.Models;

namespace TriviaAPI.Models;

public class BatchAnswerRequest
{
    public List<AnswerRequest> Answers { get; set; } = new();
}



using Trivia_API.Models;

public interface ITriviaService
{
    Task<List<Question>> GetQuestionsAsync(int amount = 10);
    Task<bool> CheckAnswerAsync(int questionId, string userAnswer);
}

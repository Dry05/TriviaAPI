// Services/ITriviaService.cs
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Trivia_API.Models;


public class TriviaService : ITriviaService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TriviaService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }


    private Dictionary<int, string> GetSessionAnswers()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null) return new Dictionary<int, string>();

        var json = session.GetString("CorrectAnswers");
        return string.IsNullOrEmpty(json)
            ? new Dictionary<int, string>()
            : JsonConvert.DeserializeObject<Dictionary<int, string>>(json);
    }

    private void SetSessionAnswers(Dictionary<int, string> answers)
    {
        var json = JsonConvert.SerializeObject(answers);
        _httpContextAccessor.HttpContext?.Session.SetString("CorrectAnswers", json);
    }

    public async Task<List<Question>> GetQuestionsAsync(int amount = 10)
    {

        var apiUrl = $"https://opentdb.com/api.php?amount={amount}";
        var response = await _httpClient.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var results = JObject.Parse(json)["results"];

        if (results == null || !results.HasValues)
        {
            return new List<Question>();
        }

        var questions = new List<Question>();
        int id = 1;
        var correctAnswers = new Dictionary<int, string>();

        foreach (var item in results)
        {
            var questionText = item["question"]?.ToString();
            var correctAnswer = item["correct_answer"]?.ToString();
            var incorrectAnswers = item["incorrect_answers"]?.Select(x => x.ToString()).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList() ?? new List<string>();

            if (string.IsNullOrEmpty(questionText) || string.IsNullOrEmpty(correctAnswer))
            {
                continue;
            }

            var allAnswers = incorrectAnswers.Append(correctAnswer).ToList();
            var shuffledAnswers = allAnswers.OrderBy(x => Guid.NewGuid()).ToList();

            correctAnswers[id] = correctAnswer;

            questions.Add(new Question
            {
                Id = id,
                QuestionText = questionText,
                AllAnswers = shuffledAnswers
            });

            id++;
        }

        SetSessionAnswers(correctAnswers);

        return questions;
    }

    public Task<bool> CheckAnswerAsync(int questionId, string userAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer))
        {
            return Task.FromResult(false);
        }

        var correctAnswers = GetSessionAnswers();
        if (correctAnswers.TryGetValue(questionId, out var correctAnswer))
        {
            return Task.FromResult(userAnswer?.ToLower() == correctAnswer.ToLower());
        }
        return Task.FromResult(false);
    }
}
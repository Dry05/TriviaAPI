using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using Trivia_API.Models;
using Xunit;

namespace Trivia_API.Tests;

// ==================== UNIT TESTS ====================

public class TriviaServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ISession> _sessionMock;
    private readonly ITriviaService _triviaService;

    public TriviaServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        // Setup HttpContextAccessor mock
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _sessionMock = new Mock<ISession>();

        var httpContext = new DefaultHttpContext();
        httpContext.Session = _sessionMock.Object;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _triviaService = new TriviaService(_httpClient, _httpContextAccessorMock.Object);

        // Reset static dictionary (indien je nog static gebruikt)
        typeof(TriviaService).GetField("_correctAnswers",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Static)
            ?.SetValue(null, new Dictionary<int, string>());
    }

    [Fact]
    public async Task GetQuestions_ShouldReturnQuestions_WhenApiReturnsValidData()
    {
        // Arrange
        var jsonResponse = @"
        {
            ""response_code"": 0,
            ""results"": [
                {
                    ""question"": ""Wat is de hoofdstad van Frankrijk?"",
                    ""correct_answer"": ""Parijs"",
                    ""incorrect_answers"": [""Londen"", ""Berlijn"", ""Madrid""]
                }
            ]
        }";

        SetupHttpResponse(jsonResponse);

        // Setup session mock
        var sessionData = new Dictionary<string, byte[]>();
        _sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, value) => sessionData[key] = value);
        _sessionMock.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
            .Returns((string key, out byte[] value) => sessionData.TryGetValue(key, out value));

        // Act
        var questions = await _triviaService.GetQuestionsAsync(1);

        // Assert
        Assert.Single(questions);
        Assert.Equal("Wat is de hoofdstad van Frankrijk?", questions[0].QuestionText);
        Assert.Contains("Parijs", questions[0].AllAnswers);
    }

    [Fact]
    public async Task CheckAnswer_ShouldReturnTrue_WhenCorrect()
    {
        // Arrange
        var jsonResponse = @"
        {
            ""response_code"": 0,
            ""results"": [
                {
                    ""question"": ""Test"",
                    ""correct_answer"": ""Correct"",
                    ""incorrect_answers"": [""Fout1""]
                }
            ]
        }";

        SetupHttpResponse(jsonResponse);

        // Setup session voor opslaan
        var storedAnswers = "";
        _sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, value) =>
            {
                if (key == "CorrectAnswers")
                    storedAnswers = Encoding.UTF8.GetString(value);
            });

        _sessionMock.Setup(s => s.TryGetValue("CorrectAnswers", out It.Ref<byte[]>.IsAny))
            .Returns((string key, out byte[] value) =>
            {
                if (key == "CorrectAnswers" && !string.IsNullOrEmpty(storedAnswers))
                {
                    value = Encoding.UTF8.GetBytes(storedAnswers);
                    return true;
                }
                value = null;
                return false;
            });

        await _triviaService.GetQuestionsAsync(1);

        // Act
        var isCorrect = await _triviaService.CheckAnswerAsync(1, "Correct");

        // Assert
        Assert.True(isCorrect);
    }

    [Fact]
    public async Task CheckAnswer_ShouldReturnFalse_WhenWrong()
    {
        // Arrange
        var jsonResponse = @"
        {
            ""response_code"": 0,
            ""results"": [
                {
                    ""question"": ""Test"",
                    ""correct_answer"": ""Correct"",
                    ""incorrect_answers"": [""Fout1""]
                }
            ]
        }";

        SetupHttpResponse(jsonResponse);

        var storedAnswers = "";
        _sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, value) =>
            {
                if (key == "CorrectAnswers")
                    storedAnswers = Encoding.UTF8.GetString(value);
            });

        _sessionMock.Setup(s => s.TryGetValue("CorrectAnswers", out It.Ref<byte[]>.IsAny))
            .Returns((string key, out byte[] value) =>
            {
                if (key == "CorrectAnswers" && !string.IsNullOrEmpty(storedAnswers))
                {
                    value = Encoding.UTF8.GetBytes(storedAnswers);
                    return true;
                }
                value = null;
                return false;
            });

        await _triviaService.GetQuestionsAsync(1);

        // Act
        var isCorrect = await _triviaService.CheckAnswerAsync(1, "Fout1");

        // Assert
        Assert.False(isCorrect);
    }

    [Fact]
    public async Task CheckAnswer_ShouldBeCaseInsensitive()
    {
        // Arrange
        var jsonResponse = @"
        {
            ""response_code"": 0,
            ""results"": [
                {
                    ""question"": ""Test"",
                    ""correct_answer"": ""Parijs"",
                    ""incorrect_answers"": [""Londen""]
                }
            ]
        }";

        SetupHttpResponse(jsonResponse);

        var storedAnswers = "";
        _sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, value) =>
            {
                if (key == "CorrectAnswers")
                    storedAnswers = Encoding.UTF8.GetString(value);
            });

        _sessionMock.Setup(s => s.TryGetValue("CorrectAnswers", out It.Ref<byte[]>.IsAny))
            .Returns((string key, out byte[] value) =>
            {
                if (key == "CorrectAnswers" && !string.IsNullOrEmpty(storedAnswers))
                {
                    value = Encoding.UTF8.GetBytes(storedAnswers);
                    return true;
                }
                value = null;
                return false;
            });

        await _triviaService.GetQuestionsAsync(1);

        // Act
        var isCorrectLower = await _triviaService.CheckAnswerAsync(1, "parijs");
        var isCorrectUpper = await _triviaService.CheckAnswerAsync(1, "PARIJS");

        // Assert
        Assert.True(isCorrectLower);
        Assert.True(isCorrectUpper);
    }

    [Fact]
    public async Task GetQuestions_ShouldSkipInvalidQuestions()
    {
        // Arrange
        var jsonResponse = @"
        {
            ""response_code"": 0,
            ""results"": [
                {
                    ""question"": """",
                    ""correct_answer"": ""Parijs"",
                    ""incorrect_answers"": [""Londen""]
                },
                {
                    ""question"": ""Wat is 2+2?"",
                    ""correct_answer"": ""4"",
                    ""incorrect_answers"": [""3"", ""5""]
                }
            ]
        }";

        SetupHttpResponse(jsonResponse);

        // Act
        var questions = await _triviaService.GetQuestionsAsync(2);

        // Assert
        Assert.Single(questions);
        Assert.Equal("Wat is 2+2?", questions[0].QuestionText);
    }

    [Fact]
    public async Task GetQuestions_ShouldReturnEmptyList_WhenNoResults()
    {
        // Arrange
        SetupHttpResponse(@"{ ""response_code"": 0, ""results"": [] }");

        // Act
        var questions = await _triviaService.GetQuestionsAsync(5);

        // Assert
        Assert.Empty(questions);
    }

    private void SetupHttpResponse(string jsonResponse)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });
    }
}

// ==================== INTEGRATION TESTS ====================

public class TriviaServiceIntegrationTests
{
    [Fact]
    public async Task RealApiCall_ShouldReturnQuestions()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Setup echte HttpContextAccessor met session
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddSession();
        services.AddHttpContextAccessor();
        var serviceProvider = services.BuildServiceProvider();

        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.Session = new TestSession();
        httpContextAccessor.HttpContext = context;

        ITriviaService service = new TriviaService(httpClient, httpContextAccessor);

        // Act
        var questions = await service.GetQuestionsAsync(3);

        // Assert
        Assert.NotNull(questions);
        Assert.True(questions.Count > 0);
        foreach (var q in questions)
        {
            Assert.NotNull(q.QuestionText);
            Assert.True(q.AllAnswers.Count >= 2);
        }
    }
}

// Helper voor test session
public class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new Dictionary<string, byte[]>();

    public bool IsAvailable => true;
    public string Id => "test";
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value);
}
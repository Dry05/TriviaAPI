namespace Trivia_API.Models
{
    public class AnswerRequest
    {
        public int QuestionId { get; set; }  
        public string UserAnswer { get; set; }
    }
}

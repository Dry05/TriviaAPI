namespace Trivia_API.Models
{
    public class Question
    {
        public int Id { get; set; }          
        public string QuestionText { get; set; }
        public List<string> AllAnswers { get; set; } 
    }
}

namespace LandIt.Models
{
    public class GeneratedQuestion
    {
        public int Id { get; set; }

        public int QuestionRequestId { get; set; }
        public QuestionRequest QuestionRequest { get; set; }

        public string QuestionText { get; set; }

        public string Category { get; set; }

        public string? Tip { get; set; }
    }
}

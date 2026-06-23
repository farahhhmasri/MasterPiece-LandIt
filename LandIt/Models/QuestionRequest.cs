namespace LandIt.Models
{
    public class QuestionRequest
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public AppUser User { get; set; }

        public string? level { get; set; }

        public string JobTitle { get; set; }

        public string JobDescription { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<GeneratedQuestion> GeneratedQuestions { get; set; }
    }
}

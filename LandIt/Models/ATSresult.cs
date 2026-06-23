namespace LandIt.Models
{
    public class ATSresult
    {
        public int Id { get; set; }

        public int ResumeId { get; set; }
        public Resume Resume { get; set; }

        public string JobTitle { get; set; }

        public double Score { get; set; }

        public string Suggestions { get; set; }
        public string? KeywordMatches { get; set; }  
        public string? MissingKeywords { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

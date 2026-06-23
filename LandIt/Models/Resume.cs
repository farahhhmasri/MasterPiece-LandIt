namespace LandIt.Models
{
    public class Resume
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public AppUser User { get; set; }

        public string FilePath { get; set; }

        public string ParsedText { get; set; }

        public ICollection<ATSresult> ATSResults { get; set; }
    }
}

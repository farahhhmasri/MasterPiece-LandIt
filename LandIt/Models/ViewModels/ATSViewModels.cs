using System.ComponentModel.DataAnnotations;

namespace LandIt.Models.ViewModels;

// Form for uploading resume + job info
public class ATSUploadViewModel
{
    [Required]
    public IFormFile ResumeFile { get; set; } = null!;

    [Required, MaxLength(150)]
    public string JobTitle { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? CompanyName { get; set; }

    [Required]
    public string JobDescription { get; set; } = string.Empty;

    public string ExperienceLevel { get; set; } = "mid";
}

// Passed to the result view
public class ATSResultViewModel
{
    public int ResumeId { get; set; }
    public int ATSResultId { get; set; }
    public int Score { get; set; }
    public List<string> MatchedKeywords { get; set; } = new();
    public List<string> MissingKeywords { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public string JobTitle { get; set; } = string.Empty;

    // Set after resume generation
    public string? GeneratedFileUrl { get; set; }
    public bool ResumeGenerated => !string.IsNullOrEmpty(GeneratedFileUrl);
}
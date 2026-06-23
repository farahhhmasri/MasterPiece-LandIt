using System.ComponentModel.DataAnnotations;

namespace LandIt.Models.ViewModels;

public class QuestionRequestViewModel
{
    [Required, MaxLength(150)]
    public string JobTitle { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? CompanyName { get; set; }

    [Required]
    public string Level { get; set; } = "mid";

    [Required]
    public string JobDescription { get; set; } = string.Empty;
}
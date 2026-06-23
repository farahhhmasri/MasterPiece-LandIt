using System.ComponentModel.DataAnnotations;
using LandIt.Models;

namespace LandIt.Models.ViewModels;

public class TestimonialViewModel
{
    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public TestimonialSource Source { get; set; }

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; } = 5;
}

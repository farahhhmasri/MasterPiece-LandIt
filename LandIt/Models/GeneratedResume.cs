using System.ComponentModel.DataAnnotations;

namespace LandIt.Models;


public class GeneratedResume
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;   

    public virtual AppUser? User { get; set; }

    public int? ATSResultId { get; set; }

    public virtual ATSresult? ATSresult { get; set; }


    [MaxLength(150)]
    public string? JobTitle { get; set; }


    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? FileUrl { get; set; }

   
    [MaxLength(255)]
    public string? FileName { get; set; }
    
    [MaxLength(100)]
    public string? AIModelUsed { get; set; }

    public int? ATSScore { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
using LandIt.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LandIt.Services;

public class ResumePdfBuilder
{
    private readonly IWebHostEnvironment _env;

    public ResumePdfBuilder(IWebHostEnvironment env) => _env = env;

    public async Task<(string fileUrl, string fileName)> GenerateAsync(ParsedResume resume)
    {
        var outputDir = Path.Combine(_env.WebRootPath, "generated");
        Directory.CreateDirectory(outputDir);

        var fileName = $"resume_{Guid.NewGuid():N}.pdf";
        var filePath = Path.Combine(outputDir, fileName);

        await Task.Run(() =>
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Content().Column(col =>
                    {
                        col.Spacing(6);

                        // Name
                        col.Item().Text(resume.FullName).FontSize(22).Bold();
                        col.Item().Height(2);

                        // Contact line
                        var contacts = new[] { resume.Email, resume.Phone, resume.Location, resume.LinkedIn }
                            .Where(x => !string.IsNullOrWhiteSpace(x));
                        col.Item().Text(string.Join("  ·  ", contacts)).FontSize(9).FontColor("#555555");
                        col.Item().Height(4);

                        // Summary
                        if (!string.IsNullOrWhiteSpace(resume.Summary))
                        {
                            AddSectionTitle(col, "PROFESSIONAL SUMMARY");
                            col.Item().Text(resume.Summary).LineHeight(1.5f);
                        }

                        // Experience
                        if (resume.Experience.Any())
                        {
                            AddSectionTitle(col, "EXPERIENCE");
                            foreach (var exp in resume.Experience)
                            {
                                col.Item().PaddingTop(4).Column(inner =>
                                {
                                    inner.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(t =>
                                        {
                                            t.Span(exp.Title).Bold();
                                            if (!string.IsNullOrWhiteSpace(exp.Company))
                                                t.Span($"  —  {exp.Company}").FontColor("#555555");
                                        });
                                        row.AutoItem().AlignRight().Text(exp.Dates).FontSize(9).FontColor("#555555");
                                    });
                                    foreach (var b in exp.Bullets.Where(x => !string.IsNullOrWhiteSpace(x)))
                                        inner.Item().Row(r =>
                                        {
                                            r.ConstantItem(14).Text("•").FontColor("#888888");
                                            r.RelativeItem().Text(b).LineHeight(1.4f);
                                        });
                                });
                            }
                        }

                        // Education
                        if (resume.Education.Any())
                        {
                            AddSectionTitle(col, "EDUCATION");
                            foreach (var edu in resume.Education)
                                col.Item().PaddingTop(4).Row(row =>
                                {
                                    row.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text(edu.Degree).Bold();
                                        c.Item().Text(edu.Institution).FontSize(9).FontColor("#555555");
                                    });
                                    row.AutoItem().AlignRight().Text(edu.Dates).FontSize(9).FontColor("#555555");
                                });
                        }

                        // Skills
                        if (resume.Skills.Any())
                        {
                            AddSectionTitle(col, "SKILLS");
                            col.Item().Text(string.Join("  ·  ", resume.Skills)).LineHeight(1.6f);
                        }
                    });
                });
            }).GeneratePdf(filePath)
        );

        return ($"/generated/{fileName}", fileName);
    }

    private static void AddSectionTitle(ColumnDescriptor col, string title)
    {
        col.Item().PaddingTop(8).BorderBottom(1).BorderColor("#cccccc").Column(c =>
            c.Item().PaddingBottom(3).Text(title).FontSize(9).Bold().LetterSpacing(0.05f)
        );
    }
}
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace LandIt.Services;

public class ResumeExtractorService
{
    private readonly ILogger<ResumeExtractorService> _logger;

    public ResumeExtractorService(ILogger<ResumeExtractorService> logger)
        => _logger = logger;

    public async Task<string> ExtractTextAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{ext}");

        try
        {
            await using (var fs = File.Create(tempPath))
                await file.CopyToAsync(fs);

            return ext switch
            {
                ".pdf" => ExtractFromPdf(tempPath),
                ".docx" => ExtractFromDocx(tempPath),
                _ => throw new NotSupportedException("Only PDF and DOCX files are supported.")
            };
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private string ExtractFromPdf(string path)
    {
        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(path);
        foreach (Page page in doc.GetPages())
        {
            foreach (var word in page.GetWords())
                sb.Append(word.Text).Append(' ');
            sb.AppendLine();
        }
        var text = sb.ToString().Trim();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Could not extract text. The PDF may be image-based.");
        return text;
    }

    private string ExtractFromDocx(string path)
    {
        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart?.Document?.Body
            ?? throw new InvalidOperationException("Could not read the DOCX file.");
        foreach (var para in body.Descendants<Paragraph>())
        {
            var line = para.InnerText.Trim();
            if (!string.IsNullOrEmpty(line)) sb.AppendLine(line);
        }
        return sb.ToString().Trim();
    }
}
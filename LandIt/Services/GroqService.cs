using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LandIt.Services;

public class GroqService
{
    private readonly HttpClient _http;
    private readonly ILogger<GroqService> _logger;
    private readonly string _apiKey;

    private const string Model = "llama-3.3-70b-versatile";
    private const string BaseUrl = "https://api.groq.com/openai/v1/chat/completions";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GroqService(HttpClient http, IConfiguration config, ILogger<GroqService> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["Groq:ApiKey"]
            ?? throw new InvalidOperationException("Groq:ApiKey missing from configuration.");
    }

    // ATS Analysis

    public async Task<ATSAnalysisResult> AnalyzeAsync(
        string resumeText,
        string jobTitle,
        string jobDescription)
    {
        var prompt = $@"You are an expert ATS analyst. Analyze the resume against the job description.

Return ONLY valid JSON, no markdown, no explanation:
{{
  ""score"": 0,
  ""matched_keywords"": [""""],
  ""missing_keywords"": [""""],
  ""suggestions"": [""""]
}}

SCORING RULES:
- score: 0-100 based on keyword match and relevance
- matched_keywords: keywords from job description found in resume
- missing_keywords: important keywords from job description NOT in resume
- suggestions: max 6 specific actionable improvements

JOB TITLE: {jobTitle}
JOB DESCRIPTION: {jobDescription}
RESUME: {resumeText}";

        var text = await CallGroqAsync(prompt);

        try
        {
            return JsonSerializer.Deserialize<ATSAnalysisResult>(text, JsonOpts)
                ?? throw new InvalidOperationException("Invalid ATS response.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse ATS response: {Text}", text);
            throw new InvalidOperationException("Could not parse ATS analysis. Please try again.", ex);
        }
    }

    // Resume Rewrite

    public async Task<ParsedResume> RewriteResumeAsync(
        string resumeText,
        string jobTitle,
        string companyName,
        string jobDescription,
        string experienceLevel)
    {
        var prompt = $@"You are an expert ATS resume writer. Rewrite the resume for the target job.

RULES:
- Do NOT invent experience or companies
- Use strong action verbs
- Quantify achievements where possible
- Single-column structure only
- Experience level context: {experienceLevel}

Return ONLY valid JSON, no markdown, no explanation:
{{
  ""full_name"": """",
  ""email"": """",
  ""phone"": """",
  ""linkedin"": """",
  ""location"": """",
  ""summary"": """",
  ""experience"": [{{""company"":"""",""title"":"""",""dates"":"""",""location"":"""",""bullets"":[""""]}}],
  ""education"": [{{""institution"":"""",""degree"":"""",""dates"":"""",""gpa"":""""}}],
  ""skills"": [""""],
  ""certifications"": [""""]
}}

JOB TITLE: {jobTitle}
COMPANY: {companyName}
JOB DESCRIPTION: {jobDescription}
RESUME: {resumeText}";

        var text = await CallGroqAsync(prompt);

        try
        {
            return JsonSerializer.Deserialize<ParsedResume>(text, JsonOpts)
                ?? throw new InvalidOperationException("Invalid resume response.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse resume rewrite: {Text}", text);
            throw new InvalidOperationException("Could not parse resume rewrite. Please try again.", ex);
        }
    }

    // Shared HTTP call 
    private async Task<string> CallGroqAsync(string prompt)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        var body = new
        {
            model = Model,
            messages = new[]
            {
                new
                {
                    role    = "user",
                    content = prompt
                }
            },
            temperature = 0.2,
            max_tokens = 4096,
            response_format = new { type = "json_object" }  // force JSON output
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Groq error {Status}: {Body}", response.StatusCode, error);
            throw new HttpRequestException($"Groq API failed: {response.StatusCode} — {error}");
        }

        var json = await response.Content.ReadAsStringAsync();

        // Groq return format: choices[0].message.content
        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?? throw new InvalidOperationException("Empty Groq response.");

        return text.Replace("```json", "").Replace("```", "").Trim();
    }



    public async Task<List<GeneratedQuestionResult>> GenerateQuestionsAsync(
    string jobTitle,
    string jobDescription,
    string level,
    string? companyName)
    {
        var prompt = $@"You are an expert interview coach.

Generate 15 interview questions for the following position.

Return ONLY valid JSON, no markdown, no explanation:
{{
  ""questions"": [
    {{
      ""question_text"": """",
      ""category"": """",
      ""tip"": """"
    }}
  ]
}}

CATEGORY must be one of: Behavioral, Technical, Situational, Culture Fit, Role Specific

JOB TITLE: {jobTitle}
COMPANY: {companyName ?? "Not specified"}
LEVEL: {level}
JOB DESCRIPTION: {jobDescription}";

        var text = await CallGroqAsync(prompt);

        try
        {
            using var doc = JsonDocument.Parse(text);
            var questions = doc.RootElement
                .GetProperty("questions")
                .Deserialize<List<GeneratedQuestionResult>>(JsonOpts)
                ?? throw new InvalidOperationException("Empty questions response.");
            return questions;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse questions: {Text}", text);
            throw new InvalidOperationException("Could not parse questions. Please try again.", ex);
        }
    }
}



public class ATSAnalysisResult
{
    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("matched_keywords")]
    public List<string> MatchedKeywords { get; set; } = new();

    [JsonPropertyName("missing_keywords")]
    public List<string> MissingKeywords { get; set; } = new();

    [JsonPropertyName("suggestions")]
    public List<string> Suggestions { get; set; } = new();
}

public class ParsedResume
{
    [JsonPropertyName("full_name")] public string FullName { get; set; } = "";
    [JsonPropertyName("email")] public string Email { get; set; } = "";
    [JsonPropertyName("phone")] public string Phone { get; set; } = "";
    [JsonPropertyName("linkedin")] public string LinkedIn { get; set; } = "";
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("summary")] public string Summary { get; set; } = "";
    [JsonPropertyName("experience")] public List<WorkExperience> Experience { get; set; } = new();
    [JsonPropertyName("education")] public List<ResumeEducation> Education { get; set; } = new();
    [JsonPropertyName("skills")] public List<string> Skills { get; set; } = new();
    [JsonPropertyName("certifications")] public List<string> Certifications { get; set; } = new();
}

public class WorkExperience
{
    [JsonPropertyName("company")] public string Company { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("dates")] public string Dates { get; set; } = "";
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("bullets")] public List<string> Bullets { get; set; } = new();
}

public class ResumeEducation
{
    [JsonPropertyName("institution")] public string Institution { get; set; } = "";
    [JsonPropertyName("degree")] public string Degree { get; set; } = "";
    [JsonPropertyName("dates")] public string Dates { get; set; } = "";
    [JsonPropertyName("gpa")] public string GPA { get; set; } = "";
}


public class GeneratedQuestionResult
{
    [JsonPropertyName("question_text")] public string QuestionText { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("tip")] public string Tip { get; set; } = "";
}
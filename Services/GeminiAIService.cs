using System.Text;
using System.Text.Json;

namespace CloudStorage.Services;

public class GeminiAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiAIService> _logger;

    public GeminiAIService(IConfiguration configuration, ILogger<GeminiAIService> logger)
    {
        _httpClient = new HttpClient();
        _apiKey = configuration["GeminiAI:ApiKey"] ?? throw new InvalidOperationException("Gemini API Key not configured");
        _model = configuration["GeminiAI:Model"] ?? "gemini-2.0-flash-exp";
        _logger = logger;
    }

    public async Task<string> GenerateContentAsync(string prompt)
    {
        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Gemini API error: {responseContent}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new Exception("API quota exceeded. Please wait a moment and try again.");
                }
                
                throw new Exception($"Gemini API error: {response.StatusCode}");
            }

            var result = JsonDocument.Parse(responseContent);
            var text = result.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            throw;
        }
    }

    public async Task<FolderCreationInstruction> ParseFolderCreationPromptAsync(string userPrompt)
    {
        try
        {
            var systemPrompt = @"You are an AI assistant that helps create folders with files based on user requests.

Analyze the user's prompt carefully and return ONLY a valid JSON object (no markdown, no extra text) with this EXACT format:
{
  ""folderName"": ""descriptive_folder_name"",
  ""files"": [
    {
      ""fileName"": ""file1.ext"",
      ""content"": ""file content here""
    },
    {
      ""fileName"": ""file2.ext"",
      ""content"": ""file content here""
    }
  ]
}

CRITICAL RULES:
1. Return ONLY the JSON object - NO markdown code blocks, NO explanations, NO other text
2. folderName: Use descriptive name matching the request (e.g., 'Project_Report', 'Meeting_Notes', 'Website_Assets')
3. files: Create ALL requested files in the array
4. fileName: Include proper extensions (.txt, .md, .html, .css, .js, .json, .csv, .xml, etc)
5. content: Generate meaningful placeholder content relevant to the file type and request
6. If user asks for multiple numbered files (e.g., 5 files), create all 5 in the array
7. Make content useful - not just 'placeholder' or 'empty'

Examples:
- ""Create project documentation"" → folder: 'Project_Documentation', files: README.md, TODO.md, CHANGELOG.md
- ""5 test files"" → folder: 'Test_Files', files: test1.txt through test5.txt
- ""website template"" → folder: 'Website_Template', files: index.html, style.css, script.js

User prompt: "" + userPrompt + @""

Remember: Return ONLY the JSON, nothing else!";

            var response = await GenerateContentAsync(systemPrompt);
            _logger.LogInformation("AI Response for folder creation: {Response}", response);
            
            // Clean response - remove markdown code blocks if present
            var cleanResponse = response.Trim();
            if (cleanResponse.StartsWith("```json"))
            {
                cleanResponse = cleanResponse.Substring(7);
            }
            if (cleanResponse.StartsWith("```"))
            {
                cleanResponse = cleanResponse.Substring(3);
            }
            if (cleanResponse.EndsWith("```"))
            {
                cleanResponse = cleanResponse.Substring(0, cleanResponse.Length - 3);
            }
            cleanResponse = cleanResponse.Trim();
            
            // Extract JSON from response
            var jsonStart = cleanResponse.IndexOf('{');
            var jsonEnd = cleanResponse.LastIndexOf('}') + 1;
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonString = cleanResponse.Substring(jsonStart, jsonEnd - jsonStart);
                _logger.LogInformation("Extracted JSON: {Json}", jsonString);
                
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                };
                var result = JsonSerializer.Deserialize<FolderCreationInstruction>(jsonString, options);
                
                if (result != null && !string.IsNullOrEmpty(result.FolderName))
                {
                    return result;
                }
            }

            _logger.LogWarning("Failed to parse AI response: {Response}", cleanResponse);
            throw new Exception($"AI failed to understand the request. Please try rephrasing your prompt. AI Response: {cleanResponse.Substring(0, Math.Min(100, cleanResponse.Length))}");
        }
        catch (Exception ex) when (ex.Message.Contains("API quota") || ex.Message.Contains("API error"))
        {
            _logger.LogError(ex, "Gemini API error");
            throw new Exception($"AI Service Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing folder creation prompt");
            throw new Exception("Failed to process AI request. Please check your prompt and try again.", ex);
        }
    }

    public Task<string> ClassifyFileByNameAsync(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        
        // Quick classification by extension
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".webp", ".ico" };
        var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };
        var audioExtensions = new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a" };
        var documentExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt" };
        var archiveExtensions = new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2" };
        var codeExtensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h", ".css", ".html", ".xml", ".json" };

        if (imageExtensions.Contains(extension)) return Task.FromResult("Images");
        if (videoExtensions.Contains(extension)) return Task.FromResult("Videos");
        if (audioExtensions.Contains(extension)) return Task.FromResult("Audio");
        if (documentExtensions.Contains(extension)) return Task.FromResult("Documents");
        if (archiveExtensions.Contains(extension)) return Task.FromResult("Archives");
        if (codeExtensions.Contains(extension)) return Task.FromResult("Code");

        return Task.FromResult("Others");
    }
}

public class FolderCreationInstruction
{
    public string FolderName { get; set; } = "NewFolder";
    public List<FileInstruction> Files { get; set; } = new();
}

public class FileInstruction
{
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

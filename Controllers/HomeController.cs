using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ExamGenerator.Models;

namespace ExamGenerator.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _openaiApiKey;
    
    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _openaiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("API Key is missing from environment variables.");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openaiApiKey}");
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(ExamGenerateModel model)
    {
        string prompt = "You are an exam generator. Generate an exam with these parameters without including any code blocks or markdown formatting. Just generate Return pure HTML only.";
        if (model.ShowAnswers == "No") {
            prompt += " Do not include the answers in the exam.";
        }
        else if (model.ShowAnswers == "YesSeparate") {
            prompt += " You should have the questions and their answers. Include answers to the questions in a new answers section under the questions section. Do not show the correct answer as you are presenting the question, it should be a separate section.";
        }
        else {
            prompt += " You should show the correct answer right under the question.";
        }
        if (model.QuestionType == "MultipleChoice") {
            prompt += " You should show the multiple choices under each question.";
        }

        try
        {
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = prompt
                    },
                    new
                    {
                        role = "user",
                        content = $"CourseCode: {model.CourseCode}\n" +
                                $"CourseName: {model.CourseName}\n" +
                                $"QuestionType: {model.QuestionType}\n" +
                                $"NumberOfQuestions: {model.NumberOfQuestions}\n" +
                                $"NumberOfChoices: {model.NumberOfChoices}\n" +
                                $"CourseContent: {model.CourseContent}"
                    }
                },
                temperature = 1,
                max_tokens = 8192,
                top_p = 1,
                frequency_penalty = 0,
                presence_penalty = 0
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("chat/completions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return Content($"API Error: {responseContent}", "text/html");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, options);

            if (result?.Choices == null || result.Choices.Length == 0)
            {
                _logger.LogError("No choices returned in the response");
                return Content("Error: No response generated from the API.", "text/html");
            }

            var generatedContent = result.Choices[0].Message?.Content;

            if (string.IsNullOrEmpty(generatedContent))
            {
                return Content("No content was generated.", "text/html");
            }

            // Removing potential markdown-type formatting from GPT output
            generatedContent = generatedContent
                .Replace("```html", "")
                .Replace("```", "")
                .Trim();

            // If output is likely to be fully HTML, then just return it 
            // Otherwise, define a basic HTML structure around it 
            if (!generatedContent.StartsWith("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase))
            {
                generatedContent = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Generated Exam</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
        }}
        h2, h3 {{
            color: #333;
        }}
        ul {{
            list-style-type: none;
            padding-left: 20px;
        }}
        ol {{
            margin-bottom: 20px;
        }}
        li {{
            margin-bottom: 10px;
        }}
    </style>
</head>
<body>
    {generatedContent}
    <button id='printOrSaveExam' onclick='printExam()'>Print / Save Exam</button>
    <script>
        function printExam() {{
            document.getElementById('printOrSaveExam').style.display = 'none';
            window.print();
            setTimeout(() => {{
                document.getElementById('printOrSaveExam').style.display = 'block';
                }}, 3000)
        }}
    </script>
</body>
</html>";
            }

            return Content(generatedContent, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating exam");
            return Content($"Error generating exam: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "text/html");
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

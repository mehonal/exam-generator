using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ExamGenerator.Models;
using ExamGenerator.Data;
using Microsoft.EntityFrameworkCore;

namespace ExamGenerator.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _openaiApiKey;
    private readonly ApplicationDbContext _context;
    private const int DAILY_EXAM_LIMIT = 15;
    
    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _openaiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("API Key is missing from environment variables.");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openaiApiKey}");
    }

    private async Task<bool> HasReachedDailyLimit()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        
        var todayExamCount = await _context.ExamData
            .Where(e => e.CreatedAt >= today && e.CreatedAt < tomorrow)
            .CountAsync();
            
        return todayExamCount >= DAILY_EXAM_LIMIT;
    }

    [Route("/Results", Name = "exam-results")]
    public IActionResult Results()
    {
        // Get the exam ID from TempData
        if (TempData["ExamId"] is not int examId)
        {
            return RedirectToAction("Index");
        }

        // Fetch the exam data from the database
        var examData = _context.ExamData.FirstOrDefault(e => e.Id == examId);
        if (examData == null)
        {
            return RedirectToAction("Index");
        }

        var resultModel = new ExamResultModel
        {
            GeneratedHtml = examData.GeneratedHtml
        };

        return View(resultModel);
    }
    
    public IActionResult Index()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var todayExamCount = _context.ExamData
            .Count(e => e.CreatedAt >= today && e.CreatedAt < tomorrow);
            
        ViewData["RemainingExams"] = DAILY_EXAM_LIMIT - todayExamCount;
        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
        
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(ExamGenerateModel model)
    {

        if (await HasReachedDailyLimit())
        {
            TempData["ErrorMessage"] = "Daily exam generation limit (15) has been reached. Please try again tomorrow.";
            return RedirectToAction("Index");
        }

        string prompt = "You are an exam generator. Only use English latin characters in your exam. Generate an exam with these parameters without including any code blocks or markdown formatting. Just generate Return pure HTML only. Important: You must generate the exact number of questions requested. Even if requested 25 questions, you should generate all 25 questions.";
        
        if (model.QuestionType == "MultipleChoice") {
            prompt += " You should show the multiple choices under each question.";
        }
        if (model.ShowAnswers == "No") {
            prompt += " Do not include the answers in the exam.";
        }
        else if (model.ShowAnswers == "YesSeparate") {
            prompt += " You should have the questions and their answers. Include answers to the questions in a new answers section under the questions section. Do not show the correct answer as you are presenting the question, it should be a separate section.";
        }
        else {
            prompt += " You should show the correct answer under each question.";
        }

        string questionDifficulty = model.QuestionDifficulty;
        if (model.QuestionDifficulty == "EasyHard") {
            questionDifficulty = "Some Easy, Some Hard";
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
                        content = $"Course Code: {model.CourseCode}\n" +
                                $"Course Name: {model.CourseName}\n" +
                                $"Type of Questions: {model.QuestionType}\n" +
                                $"Question Difficulty: {questionDifficulty}" +
                                $"Number of questions in the exam (ensure you have exactly this amount): {model.NumberOfQuestions}\n" +
                                $"Number of choices per quesion: {model.NumberOfChoices}\n" +
                                $"Content to generate the questions based on: {model.CourseContent}"
                    }
                },
                temperature = 1,
                max_tokens = 16383,
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

            var examData = new ExamData
            {
                // form data 
                CourseCode = model.CourseCode,
                CourseName = model.CourseName,
                QuestionType = model.QuestionType,
                NumberOfQuestions = model.NumberOfQuestions,
                NumberOfChoices = model.NumberOfChoices,
                CourseContent = model.CourseContent,
                ShowAnswers = model.ShowAnswers,
                
                GeneratedHtml = generatedContent, // Store the response from GPT
                CreatedAt = DateTime.UtcNow // auto-gen
            };

            _context.ExamData.Add(examData);
            await _context.SaveChangesAsync();

            // Store the ID in TempData
            TempData["ExamId"] = examData.Id;
            
            // Redirect to results page
            return RedirectToRoute("exam-results");
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

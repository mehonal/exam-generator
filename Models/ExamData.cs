using System.ComponentModel.DataAnnotations;

namespace ExamGenerator.Models;

public class ExamData
{
    [Key]
    public int Id { get; set; }
    
    // Fields from ExamGenerateModel
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public int NumberOfQuestions { get; set; }
    public int NumberOfChoices { get; set; }
    public string CourseContent { get; set; } = string.Empty;
    public string ShowAnswers { get; set; } = string.Empty;
    
    // Resulting Fields
    public string GeneratedHtml { get; set; } = string.Empty;

    // Auto-generated fields
    public DateTime CreatedAt { get; set; }
}
using System;

namespace ExamGenerator.Models
{
    public class ExamResultModel
    {
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public string QuestionDifficulty { get; set; } = string.Empty;
        public int NumberOfQuestions { get; set; }
        public int NumberOfChoices { get; set; }
        public string CourseContent { get; set; } = string.Empty;
        public string ShowAnswers { get; set; } = string.Empty;
        public string GeneratedHtml { get; set; } = string.Empty;
    }
}

namespace ExamGenerator.Models
{
    public class OpenAIResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public long Created { get; set; }
        public string Model { get; set; } = string.Empty;
        public Choice[] Choices { get; set; } = Array.Empty<Choice>();
        public Usage Usage { get; set; } = new();
        public string? SystemFingerprint { get; set; }
    }

    public class Choice
    {
        public int Index { get; set; }
        public Message Message { get; set; } = new();
        public object? Logprobs { get; set; }
        public string FinishReason { get; set; } = string.Empty;
    }

    public class Message
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public object? Refusal { get; set; }
    }

    public class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public TokenDetails PromptTokensDetails { get; set; } = new();
        public TokenDetails CompletionTokensDetails { get; set; } = new();
    }

    public class TokenDetails
    {
        public int CachedTokens { get; set; }
        public int AudioTokens { get; set; }
        public int AcceptedPredictionTokens { get; set; }
        public int RejectedPredictionTokens { get; set; }
    }
}

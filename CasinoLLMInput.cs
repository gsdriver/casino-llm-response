using casino_llm_response;

namespace casino_llm_response
{
    public class CasinoLLMInput
    {
        public long? Timestamp { get; set; }

        public string? Game { get; set; }

        public string? UserId { get; set; }

        public int? GamesPlayed { get; set; }

        public int? Wins { get; set; }

        public int? Losses { get; set; }

        public string? Status { get; set; }

        public string? Speech { get; set; }

        public string? SpeechText { get; set; }
    }
}

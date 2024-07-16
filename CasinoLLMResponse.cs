namespace casino_llm_response
{
    public class CasinoLLMResponse
    {
        public int Status { get; set; }

        public string? Error { get; set; }

        public long TimeElapsed { get; set; }

        public string? Response { get; set; }
    }
}

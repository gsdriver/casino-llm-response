using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.RegularExpressions;

namespace casino_llm_response.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CasinoLLMResponseController : ControllerBase
    {
        private readonly ILogger<CasinoLLMResponseController> _logger;

        private CasinoLLMInput _input;

        public CasinoLLMResponseController(ILogger<CasinoLLMResponseController> logger)
        {
            _logger = logger;
        }

        // Verifies and processes input
        // Returns null if successful; or a response with an error if not
        private CasinoLLMResponse? ProcessInput(string key, string? game, long? timestamp, string? userId, int games, int wins, int losses, string status, string speech)
        {
            if (key != Environment.GetEnvironmentVariable("ACCESS_KEY"))
            {
                return new CasinoLLMResponse
                {
                    Status = 400,
                    Error = "Unauthorized",
                };
            }

            if ((status != "win") && (status != "lose"))
            {
                return new CasinoLLMResponse
                {
                    Status = 401,
                    Error = "Invalid input",
                };
            }

            // Update speech so it is text only, and remove the last question if present
            string speechText = "";
            string rawSpeech = Regex.Replace(speech, "<([^>]+)>", "");
            string[] speechSentences = Regex.Split(rawSpeech, @"(?<=[.!?])\s+");
            if (speechSentences[speechSentences.Length - 1].EndsWith("?"))
            {
                speechText = string.Join(" ", speechSentences.Take(speechSentences.Length - 1));
            }

            _input = new CasinoLLMInput
            {
                Game = game ?? "slots",
                Timestamp = timestamp ?? DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                UserId = userId ?? "default",
                GamesPlayed = games,
                Wins = wins,
                Losses = losses,
                Status = status,
                Speech = speech,
                SpeechText = speechText,
            };

            return null;
        }

        [HttpGet(Name = "GetResponse")]
        public CasinoLLMResponse Get(string key, string? game, long? timestamp, string? userId, int games, int wins, int losses, string status, string speech)
        {
            long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            CasinoLLMResponse? response;

            response = ProcessInput(key, game, timestamp, userId, games, wins, losses, status, speech);
            if (response != null)
            {
                response.TimeElapsed = DateTimeOffset.Now.ToUnixTimeMilliseconds() - start;
                return response;
            }

            // Make the OpenAI call
            OpenAIResult openAIResult = new OpenAIResult(_input);
            if (openAIResult.GetOpenAIResult()) 
            {
                ResponseStorage storage = new ResponseStorage(Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME"), Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_KEY"));
                storage.CommitResponse(_input, openAIResult);
            }

            // Return the result
            return new CasinoLLMResponse
            {
                Status = openAIResult?.Response is null ? 500 : 200,
                TimeElapsed = DateTimeOffset.Now.ToUnixTimeMilliseconds() - start,
                Response = openAIResult?.Response ?? "Internal error",
            };
        }
    }
}

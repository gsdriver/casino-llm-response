using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace casino_llm_response
{
    public class OpenAIResult
    {
        private string? _prompt;
        private string? _response;
        private CasinoLLMInput _input;

        public OpenAIResult(CasinoLLMInput input) {
            _input = input;
        }

        public string? Prompt { get { return _prompt; } }

        public string? Response { get { return _response; } }

        private string GeneratePrompt()
        {
            string[] systemPrompts;

            if (_input.Game == "slots")
            {
                systemPrompts = [
                    "You are a slot machine. Ask a question that encourages the user to keep playing. Be sarcastic if they lost. Keep the response under 20 words.",
                    "You are a slot machine. Make a joke about gambling but encourage the user to keep playing. End the response with a yes/no question and keep the response under 20 words.",
                    "Provide a response to let the user whether they won or lost. Look at how many games they\'ve played. Provide a funny response but encourage them to keep trying if they lost. The response should end in a yes/no question and be under 20 words.",
                    "Let the user know whether they won or lost. Observe their winning or losing streak. Ask if they would like to keep playing. Keep the response under 20 words.",
                    "Let the user know if they won or lost. Give a sarcastic response if they lost. Encourage them to keep playing with a yes/no question. Keep the response under 20 words.",
                ];
            } else
            {
                systemPrompts = [
                    "You are the casino playing a gambling game. Make a joke about gambling but encourage the user to keep playing. End the response with a yes/no question and keep the response under 20 words.",
                    "You are the dealer of a casino game. Let the user know if they won or lost with a sarcastic remark. Ask them a question about whether they want to keep playing. Keep the response under 20 words.",
                    "You are a professional gambler watching a sucker play. Observe their lack of skill and ask if they would like to keep playing. Keep the response under 20 words."
                ];
            }

            Random rnd = new Random();
            return systemPrompts[rnd.Next(0, systemPrompts.Length)];
        }

        public bool GetOpenAIResult()
        {
            // Ever the optimist
            bool result = true;

            try
            {
                _prompt = GeneratePrompt();

                AzureOpenAIClient azureClient = new(
                    new Uri(Environment.GetEnvironmentVariable("OPENAI_URL")),
                    new ApiKeyCredential(Environment.GetEnvironmentVariable("OPENAI_KEY")));

                ChatClient chatClient = azureClient.GetChatClient(Environment.GetEnvironmentVariable("OPENAI_DEPLOYMENT_ID"));
                ChatMessage[] chatMessages = [
                    new SystemChatMessage(_prompt),
                    new UserChatMessage($"You played {_input.GamesPlayed.ToString()} games this session"),
                    new UserChatMessage(_input.SpeechText),
                ];

                if (_input.Status == "win")
                {
                    chatMessages.Append(new AssistantChatMessage("You won this spin"));
                    if (_input.Losses > 0)
                    {
                        chatMessages.Append(new AssistantChatMessage($"You broke a losing streak of {_input.Losses.ToString()} games"));
                    }
                    else
                    {
                        chatMessages.Append(new AssistantChatMessage($"You are on a winning streak of {_input.Wins.ToString()} games"));
                    }
                }
                else
                {
                    chatMessages.Append(new AssistantChatMessage("You lost this spin"));
                    if (_input.Wins > 0)
                    {
                        chatMessages.Append(new AssistantChatMessage($"You broke a winning streak of {_input.Wins.ToString()} games"));
                    }
                    else
                    {
                        chatMessages.Append(new AssistantChatMessage($"You are on a losing streak of {_input.Losses.ToString()} games"));
                    }
                }

                ChatCompletion completion = chatClient.CompleteChat(chatMessages);

                Console.WriteLine($"{completion.Role}: {completion.Content[0].Text}");
                _response = completion.Content[0].Text;
            }
            catch (Exception e)
            {
                // Don't leak the result back
                Console.WriteLine($"{e.Message}");
                result = false;
            }

            return result;
        }
    }
}

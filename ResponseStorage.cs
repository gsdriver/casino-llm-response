using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using casino_llm_response;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace casino_llm_response
{
    public class ResponseObject
    {
        public CasinoLLMInput Input { get; set; }

        public string Prompt { get; set; }

        public string Response { get; set; }
    }

    public class ResponseStorage
    {
        private string _accountName;
        private string _accountKey;
        private BlobServiceClient _blobServiceClient;
        private StorageSharedKeyCredential _sharedKeyCredential;

        public ResponseStorage(string accountName, string accountKey) {
            _accountName = accountName;
            _accountKey = accountKey;
        }

        public bool CommitResponse(CasinoLLMInput input, OpenAIResult openAIResult)
        {
            // Always the optimist
            bool success = true;

            if (input is null || openAIResult is null)
            {
                return false;
            }

            try
            {
                if (_blobServiceClient == null)
                {
                    _sharedKeyCredential = new StorageSharedKeyCredential(_accountName, _accountKey);
                    _blobServiceClient = new(
                        new Uri($"https://{_accountName}.blob.core.windows.net"),
                        _sharedKeyCredential
                    );
                }

                // Remove non-alphanumeric values from userID
                string userId = Regex.Replace(input.UserId ?? "default", "[^a-zA-Z0-9]", "");
                string blobPath = $"{input.Game}/{input.Status}/{userId}/{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.json";

                // Upload the response to the blob storage
                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient("prompts");
                BlockBlobClient blockBlobClient = containerClient.GetBlockBlobClient(blobPath);
                ResponseObject responseObject = new ResponseObject
                {
                    Input = input,
                    Prompt = openAIResult.Prompt,
                    Response = openAIResult.Response,
                };

                // Create a memory stream from the byte array for the combined JSON
                string fileContents = JsonSerializer.Serialize(responseObject);
                byte[] byteArray = Encoding.UTF8.GetBytes(fileContents);
                using (MemoryStream stream = new MemoryStream(byteArray))
                {
                    // Upload the stream to the blob
                    blockBlobClient.Upload(stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                success = false;
            }

            return success;
        }
    }
}

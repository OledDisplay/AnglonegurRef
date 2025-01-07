using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class SynopsisGenerator
{
    private static readonly string ApiKey = "";
    private static readonly string ApiUrl = "https://api.openai.com/v1/chat/completions";

    public async Task<string> GenerateSynopsis(string plan, string info, string writingStyle)
    {
        try
        {
            string response = await GetSynopsisFromApi(plan, info, writingStyle);
            Console.WriteLine("Synopsis generated successfully.");
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return string.Empty;
        }
    }

    private static async Task<string> GetSynopsisFromApi(string plan, string info, string writingStyle)
    {
        using HttpClient client = new HttpClient();

        // Estimate token limits for output
        int inputTokenCount = Encoding.UTF8.GetByteCount(plan + info + writingStyle) / 4;
        int maxOutputTokens = Math.Max(4096 - inputTokenCount, 500);

        // Set up the request body
        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = $"You are a professional writer. Your task is to write a synopsis based on the provided plan and textbook information. Follow the plan strictly and ensure the synopsis adheres to the specified writing style: {writingStyle}. Do not include the textbook information or the plan in the output." },
                new { role = "user", content = $"Textbook Info:\n{info}\n\nPlan:\n{plan}" }
            },
            max_tokens = maxOutputTokens,
            temperature = 0
        };

        string jsonBody = JsonSerializer.Serialize(requestBody);

        // Debugging: Log the request body
        Console.WriteLine("Request Payload:");
        Console.WriteLine(jsonBody);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Headers = { { "Authorization", $"Bearer {ApiKey}" } },
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            string errorDetails = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API Error: {errorDetails}");
            throw new Exception($"Error: {response.StatusCode}");
        }

        string responseContent = await response.Content.ReadAsStringAsync();

        // Debugging: Log the response body
        Console.WriteLine("Response Payload:");
        Console.WriteLine(responseContent);

        using JsonDocument doc = JsonDocument.Parse(responseContent);

        // Extract the assistant's response
        string assistantResponse = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        // Debugging: Check response length
        if (string.IsNullOrEmpty(assistantResponse) || assistantResponse.Length < 1000)
        {
            Console.WriteLine("Warning: The generated synopsis may not meet the length requirement.");
        }

        return assistantResponse;
    }
}

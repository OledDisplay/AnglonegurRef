using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class AnalyzeScript
{
    private static readonly string ApiKey = ""; // Replace with your actual API key
    private static readonly string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private const int BytesPerToken = 4;
    private const int MaxTokensPerChunk = 2000; // Maximum tokens per chunk

    // Method to request writing style analysis from the API
    public static async Task<string> RequestStyle(string bulk,string prompt)
    {
        using HttpClient client = new HttpClient();
        List<string> splitText = Apiscript.SplitIntoChunks(bulk, MaxTokensPerChunk);

        var finalMessages = new List<object>
        {
            new
            {
                role = "system",
                content = "You are an expert writing style analyzer."
            },
            new
            {
                role = "user",
                content = prompt
         }
        };

        foreach (string chunk in splitText)
        {
            finalMessages.Add(new
            {
                role = "user",
                content = $"Here is part of the text to be analyzed:\n{chunk}"
            });
        }

        var finalRequestBody = new
        {
            model = "gpt-4o",
            messages = finalMessages,
            max_tokens = 4000, // Reserve space for response tokens
            temperature = 0.7,
            presence_penalty = 0.6,
            frequency_penalty = 0.3
        };

        string jsonFinalBody = JsonSerializer.Serialize(finalRequestBody);

        HttpRequestMessage finalRequest = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Headers = { { "Authorization", $"Bearer {ApiKey}" } },
            Content = new StringContent(jsonFinalBody, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage finalResponse = await client.SendAsync(finalRequest);

        if (!finalResponse.IsSuccessStatusCode)
        {
            string errorDetails = await finalResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"API Error on final request: {errorDetails}");
            throw new Exception($"Error: {finalResponse.StatusCode}");
        }

        string finalResponseContent = await finalResponse.Content.ReadAsStringAsync();
        using JsonDocument finalDoc = JsonDocument.Parse(finalResponseContent);

        // Log token usage
        if (finalDoc.RootElement.TryGetProperty("usage", out JsonElement usage))
        {
            Console.WriteLine($"Token Usage - Prompt: {usage.GetProperty("prompt_tokens")}, Completion: {usage.GetProperty("completion_tokens")}, Total: {usage.GetProperty("total_tokens")}");
        }

        return finalDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
    }

    // Method to save the writing style analysis to a file
    public static async Task SaveWrStyle(string text, string outputPath,string Inputprompt)
    {
        try
        {
            Console.WriteLine("Analyzing text...");
            string output = await RequestStyle(text,Inputprompt);
            File.WriteAllText(outputPath, output);
            Console.WriteLine($"Analysis saved to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while saving the writing style: {ex.Message}");
        }
    }
}


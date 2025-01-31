using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class Apiscript
{
    private static readonly string ApiKey = "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"; // Replace with your actual key
    private static readonly string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private const int BytesPerToken = 4;

    public static List<string> SplitIntoChunks(string input, int maxTokens)
    {
        int maxBytes = maxTokens * BytesPerToken;
        List<string> chunks = new List<string>();

        StringBuilder currentChunk = new StringBuilder();
        int currentBytes = 0;

        foreach (string word in input.Split(' '))
        {
            int wordBytes = Encoding.UTF8.GetByteCount(word + " ");

            if (currentBytes + wordBytes > maxBytes)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
                currentBytes = 0;
            }

            currentChunk.Append(word).Append(" ");
            currentBytes += wordBytes;
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        Console.WriteLine("Splitting into chunks...");
        return chunks;
    }

    public static async Task<string> GenerateSynopsis(List<string> infoChunks, string plan, string writingStyle, string size, string extraNotes)
{
    try
    {
        using HttpClient client = new HttpClient();

        var finalMessages = new List<object>
        {
            new
            {
                role = "system",
                content = "You are tasked with creating a detailed conspectus based on the provided plan, writing style, and example texts. Follow the plan strictly, expanding each point with 2–3 detailed dashes under each bullet point. The conspectus must be {size} words (±10 words) and follow the provided example texts' structure and style. Use Bulgarian language and follow the language level of provided by the writing style."
            },
            new
            {
                role = "user",
                content = $"Plan:\n{plan}\n\nWriting style:\n{writingStyle}\n\nExtra writing notes to keep in mind:\n{extraNotes}\n\nGenerate a detailed conspectus strictly following the plan. Expand all points fully and make it exaclty {size} words in size(a bigger size would mean more in depth information)."
            }
        };

        foreach (string chunk in infoChunks)
        {
            finalMessages.Add(new
            {
                role = "user",
                content = $"Here is part of the information:\n{chunk}"
            });
        }

        var finalRequestBody = new
        {
            model = "gpt-4o",
            messages = finalMessages,
            max_tokens = 15000,
            temperature = 0.6,
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
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
        return string.Empty;
    }
} 

}
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
    private static readonly string ApiKey = ""; // Replace with your actual key
    private static readonly string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private const int BytesPerToken = 4;

    // Dynamically split the input into manageable chunks based on token size
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

    // Preprocess the chunks to remove filler words and summarize relevant information
    public static async Task<List<string>> PreprocessChunks(List<string> infoChunks)
    {
        List<string> processedChunks = new List<string>();
        using HttpClient client = new HttpClient();

        foreach (string chunk in infoChunks)
        {
            var messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = "You are tasked with cleaning up the provided text. It will contain some gibberish and filler words. YOU HAVE should keep the original info, but you can rewrite parts that are barley understandable and should summarize long parts, keeping the original depth and crucial examples."
                },
                new
                {
                    role = "user",
                    content = $"Here is the text to preprocess:\n{chunk}\n\nClean up this text without removing any information"
                }
            };

            var requestBody = new
            {
                model = "gpt-4o",
                messages = messages,
                max_tokens = 2048, // Adjust for each chunk
                temperature = 0.5
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
            {
                Headers = { { "Authorization", $"Bearer {ApiKey}" } },
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            HttpResponseMessage response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string errorDetails = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error during preprocessing: {errorDetails}");
                throw new Exception($"Error: {response.StatusCode}");
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseContent);
            string processedText = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            processedChunks.Add(processedText);
        }

        return processedChunks;
    }

    // Generate the final synopsis
    public static async Task<string> GenerateSynopsis(List<string> infoChunks, string plan, string writingStyle, string size, string extraNotes,string SysPrompt,string UserPrompt)
    {
        try
        {
            using HttpClient client = new HttpClient();

            // Build the final messages dynamically
            var finalMessages = new List<object>
            {
                new
                {
                    role = "system",
                    content = SysPrompt
                },
                new
                {
                    role = "user",
                    content = $"Plan:\n{plan}\n\nWriting style analysi of simular text to use when writing:\n{writingStyle}\n\nExtra writing notes to be heavily guided by:\n{extraNotes}\n\nSize in charaters:\n{size}\n\n" + UserPrompt
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

            // Calculate tokens for input and adjust `max_tokens` dynamically
            int inputTokens = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(finalMessages)) / BytesPerToken;
            int maxAllowedTokens = Math.Min(16000, 32000 - inputTokens); // Ensure we don't exceed the model's limits

            Console.WriteLine($"Input tokens: {inputTokens}, Allocating for completion: {maxAllowedTokens}");

            var finalRequestBody = new
            {
                model = "gpt-4o",
                messages = finalMessages,
                max_tokens = maxAllowedTokens,
                temperature = 0.6,
                presence_penalty = 0.3, // Adjusted for better verbosity
                frequency_penalty = 0.1 // Reduced to allow more flowing content
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

            // Log token usage for debugging
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

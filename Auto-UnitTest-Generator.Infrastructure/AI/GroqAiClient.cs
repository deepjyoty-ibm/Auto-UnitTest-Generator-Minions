using Auto_UnitTest_Generator.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Auto_UnitTest_Generator.Infrastructure.AI
{
    public class GroqAiClient : IAiClient
    {
        private const string BaseUrl =
            "https://api.groq.com/openai/v1/chat/completions";

        private readonly HttpClient
            _httpClient;

        private readonly string
            _model;

        public GroqAiClient(AiConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                throw new ArgumentException("Groq API key is required.", nameof(config.ApiKey));
            }

            _model = string.IsNullOrWhiteSpace(config.Model)
                ? "llama-3.3-70b-versatile"
                : config.Model;

            _httpClient = new HttpClient();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

            _httpClient
                .DefaultRequestHeaders
                .Accept
                .Add(
                    new MediaTypeWithQualityHeaderValue(
                        "application/json"));
        }

        public async Task<string> GenerateCompletionAsync(string prompt, double temperature = 0.2)
        {
            try
            {
                var requestBody =
                    new
                    {
                        model = _model,

                        messages = new[]
                        {
                        new
                        {
                            role = "system",
                            content =
                                """
                                You are an expert senior C# software engineer specializing in unit testing and test-driven development.
                                You specialize in:
                                - xUnit
                                - NUnit
                                - MSTest
                                - Moq
                                - FluentAssertions
                                - Unit testing
                                - Clean Architecture
                                - TDD
                                Return ONLY raw csharp code.
                                """
                        },

                        new
                        {
                            role = "user",
                            content = prompt
                        }
                        },

                        temperature =
                            temperature,

                        max_tokens = 4000
                    };

                var json =
                    JsonSerializer.Serialize(
                        requestBody);

                using var content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                var response =
                    await _httpClient
                        .PostAsync(
                            BaseUrl,
                            content);

                var responseContent =
                    await response
                        .Content
                        .ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Groq API Error: {response.StatusCode}\n{responseContent}");
                }

                var jsonResponse =
                    JsonSerializer
                        .Deserialize<JsonElement>(
                            responseContent);

                var completion =
                    jsonResponse
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                return completion
                    ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to generate completion from Groq: {ex.Message}",
                    ex);
            }
        }
    }
}

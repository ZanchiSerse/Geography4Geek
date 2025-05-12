using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Geography4Geek_1.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Geography4Geek_1.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApiKey"] ?? "";
            _logger = logger;

            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        }

        // Aggiorna il metodo GenerateCountryQuizAsync in GeminiService.cs
        public async Task<QuizModel> GenerateCountryQuizAsync(string countryName, int difficultyLevel = 2, int questionCount = 5)
        {
            try
            {
                _logger.LogInformation($"Generazione quiz per paese: {countryName}, difficoltà: {difficultyLevel}");

                // Crea un nuovo quiz
                var quiz = new QuizModel  // Usa QuizModel invece di Quiz
                {
                    Title = $"Quiz su {countryName}",
                    Description = $"Testa le tue conoscenze su {countryName}",
                    CountryName = countryName,
                    CountryCode = countryName.Substring(0, 2).ToUpper(),
                    Difficulty = difficultyLevel switch
                    {
                        1 => "Facile",
                        2 => "Media",
                        3 => "Difficile",
                        _ => "Media"
                    },
                    CreatedAt = DateTime.UtcNow
                };

                // Genera le domande con Gemini
                var questions = await GenerateQuestionsAsync(countryName, difficultyLevel, questionCount);

                // Usa il metodo helper per assegnare le domande
                quiz.SetQuestions(questions);

                return quiz;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore nella generazione del quiz per {countryName}");
                throw;
            }
        }

        public async Task<string> GetCountryInfoAsync(string countryName)
        {
            try
            {
                _logger.LogInformation($"Richiesta informazioni per paese: {countryName}");

                string prompt = $"Fornisci informazioni geografiche su {countryName} in formato markdown. " +
                                "Includi dati su popolazione, capitale, lingua, superficie, valuta e principali attrazioni turistiche.";

                // Prepara la richiesta per l'API Gemini
                var request = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        maxOutputTokens = 1024
                    }
                };

                var requestJson = JsonSerializer.Serialize(request);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // Effettua la chiamata all'API
                var response = await _httpClient.PostAsync($"v1beta/models/gemini-pro:generateContent?key={_apiKey}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Errore API Gemini: {errorContent}");
                    throw new Exception($"Errore nella chiamata all'API Gemini: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                // Estrai il testo dalla risposta
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
                var text = geminiResponse?.candidates?[0]?.content?.parts?[0]?.text ??
                          $"Non sono disponibili informazioni per {countryName}.";

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore nel recupero delle informazioni per {countryName}");
                return $"Si è verificato un errore: {ex.Message}";
            }
        }

        private async Task<List<QuizQuestion>> GenerateQuestionsAsync(string countryName, int difficultyLevel, int questionCount)
        {
            string prompt = CreatePrompt(countryName, difficultyLevel, questionCount);

            // Prepara la richiesta per l'API Gemini
            var request = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    temperature = 0.8,
                    maxOutputTokens = 2048
                }
            };

            var requestJson = JsonSerializer.Serialize(request);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            // Effettua la chiamata all'API
            var response = await _httpClient.PostAsync($"v1beta/models/gemini-pro:generateContent?key={_apiKey}", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Errore API Gemini: {errorContent}");
                throw new Exception($"Errore nella chiamata all'API Gemini: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            // Analizza la risposta e trasforma in domande
            return ParseQuestionsFromResponse(responseContent);
        }

        private string CreatePrompt(string countryName, int difficultyLevel, int questionCount)
        {
            string difficulty = difficultyLevel switch
            {
                1 => "facile",
                2 => "medio",
                3 => "difficile",
                _ => "medio"
            };

            return $@"Genera {questionCount} domande a scelta multipla sul paese {countryName} di livello {difficulty}. 
                     Ogni domanda deve avere 4 opzioni di risposta, con una sola risposta corretta.
                     Formatta l'output in JSON con questa struttura:
                     [
                       {{
                         ""question"": ""Qual è la capitale di {countryName}?"",
                         ""options"": [""Opzione1"", ""Opzione2"", ""Opzione3"", ""Opzione4""],
                         ""correctOptionIndex"": 0
                       }},
                       ...
                     ]
                     Assicurati che l'output sia un JSON valido che posso deserializzare direttamente.";
        }

        private List<QuizQuestion> ParseQuestionsFromResponse(string responseContent)
        {
            try
            {
                var response = JsonSerializer.Deserialize<GeminiResponse>(responseContent);

                if (response?.candidates == null || response.candidates.Length == 0)
                {
                    _logger.LogWarning("Risposta Gemini non contiene 'candidates'");
                    return new List<QuizQuestion>();
                }

                var text = response.candidates[0]?.content?.parts?[0]?.text;

                if (string.IsNullOrEmpty(text))
                {
                    _logger.LogWarning("Risposta Gemini non contiene testo");
                    return new List<QuizQuestion>();
                }

                // Estrai il JSON dalla risposta
                var jsonStart = text.IndexOf('[');
                var jsonEnd = text.LastIndexOf(']') + 1;

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = text.Substring(jsonStart, jsonEnd - jsonStart);
                    _logger.LogInformation($"JSON estratto: {json}");

                    return JsonSerializer.Deserialize<List<QuizQuestion>>(json) ?? new List<QuizQuestion>();
                }

                _logger.LogWarning("Impossibile estrarre JSON dalla risposta");
                return new List<QuizQuestion>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'analisi della risposta Gemini");
                return new List<QuizQuestion>();
            }
        }

        // Classi per la deserializzazione della risposta Gemini
        private class GeminiResponse
        {
            public Candidate[] candidates { get; set; } = Array.Empty<Candidate>();
        }

        private class Candidate
        {
            public Content content { get; set; } = new Content();
        }

        private class Content
        {
            public Part[] parts { get; set; } = Array.Empty<Part>();
        }

        private class Part
        {
            public string text { get; set; } = string.Empty;
        }
    }
}
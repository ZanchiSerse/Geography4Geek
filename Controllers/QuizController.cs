//using Microsoft.AspNetCore.Mvc;
//using System.Text;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace Geography4Geek_1.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class QuizController : ControllerBase
//    {
//        private readonly ILogger<QuizController> _logger;
//        private readonly IConfiguration _configuration;
//        private readonly HttpClient _httpClient;

//        public QuizController(ILogger<QuizController> logger, IConfiguration configuration)
//        {
//            _logger = logger;
//            _configuration = configuration;
//            _httpClient = new HttpClient();
//        }

//        public class QuizGenerationRequest
//        {
//            [JsonPropertyName("countryName")]
//            public string CountryName { get; set; } = string.Empty;

//            [JsonPropertyName("questionCount")]
//            public int QuestionCount { get; set; } = 10;

//            [JsonPropertyName("difficulty")]
//            public string Difficulty { get; set; } = "medium";
//        }

//        public class QuizQuestion
//        {
//            public string Question { get; set; } = string.Empty;
//            public List<string> Options { get; set; } = new List<string>();
//            public int CorrectAnswerIndex { get; set; }
//            public string Explanation { get; set; } = string.Empty;
//            public string Category { get; set; } = string.Empty;
//            public string TextReference { get; set; } = string.Empty;
//        }

//        public class QuizData
//        {
//            public string CountryName { get; set; } = string.Empty;
//            public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
//        }

//        [HttpPost("generate")]
//        public async Task<IActionResult> GenerateQuiz([FromBody] QuizGenerationRequest request)
//        {
//            // Controllo completo dei parametri ricevuti
//            try
//            {
//                // Codice per loggare la richiesta...

//                // Verifica parametro CountryName
//                if (string.IsNullOrEmpty(request.CountryName))
//                {
//                    _logger.LogWarning("Richiesta ricevuta senza nome del paese");
//                    return BadRequest(new { success = false, message = "Il nome del paese è richiesto." });
//                }

//                try
//                {
//                    // Log dettagliato di tutti i parametri ricevuti
//                    _logger.LogInformation("Parametri quiz ricevuti:");
//                    _logger.LogInformation("- CountryName: {CountryName}", request.CountryName);
//                    _logger.LogInformation("- QuestionCount: {QuestionCount}", request.QuestionCount);
//                    _logger.LogInformation("- Difficulty: {Difficulty}", request.Difficulty);

//                    // Valida e correggi il numero di domande
//                    if (request.QuestionCount <= 0)
//                    {
//                        _logger.LogWarning("Ricevuto numero di domande non valido: {Count}. Utilizzo default: 10", request.QuestionCount);
//                        request.QuestionCount = 10;
//                    }
//                    else if (request.QuestionCount > 20)
//                    {
//                        _logger.LogWarning("Ricevuto numero di domande troppo grande: {Count}. Limitato a 20", request.QuestionCount);
//                        request.QuestionCount = 20;
//                    }

//                    // Valida e correggi la difficoltà
//                    if (string.IsNullOrEmpty(request.Difficulty) ||
//                        !new[] { "easy", "medium", "hard" }.Contains(request.Difficulty.ToLower()))
//                    {
//                        _logger.LogWarning("Ricevuta difficoltà non valida: {Difficulty}. Utilizzo default: medium", request.Difficulty);
//                        request.Difficulty = "medium";
//                    }

//                    _logger.LogInformation("Generazione quiz con parametri finali: {Count} domande, difficoltà {Difficulty} per {Country}",
//                        request.QuestionCount, request.Difficulty, request.CountryName);

//                    // CORREZIONE: Ottieni le informazioni sul paese
//                    var countryInfo = await GetCountryReadMoreInfo(request.CountryName);

//                    // Genera domande basate sulle informazioni
//                    var quizData = await GenerateQuizQuestionsFromGemini(
//                        request.CountryName,
//                        countryInfo,
//                        request.QuestionCount,
//                        request.Difficulty
//                    );

//                    // Randomizza le posizioni delle risposte corrette
//                    quizData = RandomizeAnswerPositions(quizData);

//                    _logger.LogInformation("Quiz generato con successo e posizioni delle risposte randomizzate: {Count} domande per {Country}",
//                        quizData.Questions.Count, request.CountryName);

//                    // Restituisci i dati del quiz
//                    return Ok(new
//                    {
//                        success = true,
//                        message = "Quiz generato con successo",
//                        quiz = quizData
//                    });
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Errore nella generazione del quiz per {CountryName}", request.CountryName);
//                    return StatusCode(500, new { success = false, message = $"Errore: {ex.Message}" });
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Errore nella lettura del corpo della richiesta");
//                return StatusCode(500, new { success = false, message = $"Errore nella lettura della richiesta: {ex.Message}" });
//            }
//        }
//        private async Task<string> GetCountryInfoFromGemini(string countryName)
//        {
//            try
//            {
//                _logger.LogInformation("Richiesta informazioni paese per {CountryName}", countryName);

//                // Costruisci URL base dalla richiesta corrente
//                var baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
//                var response = await _httpClient.GetAsync($"{baseUrl}/api/GeminiProxy/countryinfo?countryName={Uri.EscapeDataString(countryName)}");

//                if (!response.IsSuccessStatusCode)
//                {
//                    _logger.LogWarning("Risposta non valida da GeminiProxy: {StatusCode}", response.StatusCode);
//                    throw new Exception($"Errore nel recupero delle informazioni sul paese: {response.StatusCode}");
//                }

//                var countryInfo = await response.Content.ReadAsStringAsync();

//                // Log delle informazioni ricevute (primi 500 caratteri)
//                _logger.LogInformation("Ricevute informazioni su {CountryName} (primi 500 caratteri): {Info}",
//                    countryName, countryInfo.Substring(0, Math.Min(500, countryInfo.Length)));

//                // Verifica se sono state ricevute informazioni valide
//                if (string.IsNullOrWhiteSpace(countryInfo) || countryInfo.Length < 100)
//                {
//                    _logger.LogWarning("Informazioni ricevute su {CountryName} potrebbero essere insufficienti (lunghezza: {Length})",
//                        countryName, countryInfo.Length);
//                }

//                return countryInfo;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Errore nel recupero delle informazioni su {CountryName}", countryName);

//                // Usa dati fallback in caso di errore per alcuni paesi comuni
//                if (IsCommonCountry(countryName))
//                {
//                    _logger.LogInformation("Utilizzando dati fallback per {CountryName}", countryName);
//                    return GetFallbackCountryInfo(countryName);
//                }

//                throw;
//            }
//        }
//        // Metodo per randomizzare le posizioni delle risposte
//        private QuizData RandomizeAnswerPositions(QuizData quizData)
//        {
//            _logger.LogInformation("Randomizzazione delle posizioni delle risposte per {Count} domande", quizData.Questions.Count);

//            // Crea un nuovo oggetto Random per generare numeri casuali
//            var random = new Random();

//            foreach (var question in quizData.Questions)
//            {
//                // Salva la risposta corretta
//                var correctAnswer = question.Options[question.CorrectAnswerIndex];

//                // Crea una copia delle opzioni
//                var shuffledOptions = question.Options.ToList();

//                // Mescola le opzioni usando l'algoritmo Fisher-Yates
//                int n = shuffledOptions.Count;
//                while (n > 1)
//                {
//                    n--;
//                    int k = random.Next(n + 1);
//                    var value = shuffledOptions[k];
//                    shuffledOptions[k] = shuffledOptions[n];
//                    shuffledOptions[n] = value;
//                }

//                // Trova la nuova posizione della risposta corretta
//                question.Options = shuffledOptions;
//                question.CorrectAnswerIndex = shuffledOptions.IndexOf(correctAnswer);

//                _logger.LogDebug("Domanda randomizzata: risposta corretta ora in posizione {Position}", question.CorrectAnswerIndex);
//            }

//            return quizData;
//        }
//        private bool IsCommonCountry(string countryName)
//        {
//            var commonCountries = new[] { "Italy", "France", "Germany", "Spain", "United Kingdom", "United States",
//                "China", "Japan", "India", "Brazil", "Russia", "Australia", "Canada", "Mexico", "Hungary",
//                "Puerto Rico", "Nicaragua" };

//            return commonCountries.Any(c => countryName.Equals(c, StringComparison.OrdinalIgnoreCase));
//        }

//        // Aggiunta del metodo mancante
//        private string GetFallbackCountryInfo(string countryName)
//        {
//            // Dati minimi per alcuni paesi comuni in caso Gemini non sia disponibile
//            switch (countryName.ToLower())
//            {
//                case "nicaragua":
//                    return "<h4><i class='fas fa-users'></i> Popolazione</h4><p>Il Nicaragua ha una popolazione di circa 6,6 milioni di abitanti (2023).</p><h4><i class='fas fa-landmark'></i> Capitale</h4><p>Managua è la capitale del Nicaragua.</p><h4><i class='fas fa-language'></i> Lingue</h4><p>La lingua ufficiale è lo spagnolo.</p>";

//                case "hungary":
//                    return "<h4><i class='fas fa-users'></i> Popolazione</h4><p>L'Ungheria ha una popolazione di circa 9,7 milioni di abitanti (2023).</p><h4><i class='fas fa-landmark'></i> Capitale</h4><p>Budapest è la capitale dell'Ungheria.</p><h4><i class='fas fa-language'></i> Lingue</h4><p>La lingua ufficiale è l'ungherese (magyar).</p>";

//                case "italy":
//                    return "<h4><i class='fas fa-users'></i> Popolazione</h4><p>L'Italia ha una popolazione di circa 60 milioni di abitanti (2023).</p><h4><i class='fas fa-landmark'></i> Capitale</h4><p>Roma è la capitale dell'Italia.</p><h4><i class='fas fa-language'></i> Lingue</h4><p>La lingua ufficiale è l'italiano.</p>";

//                case "puerto rico":
//                    return "<h4><i class='fas fa-users'></i> Popolazione</h4><p>Puerto Rico ha una popolazione di circa 3,2 milioni di abitanti (2023).</p><h4><i class='fas fa-landmark'></i> Capitale</h4><p>San Juan è la capitale di Puerto Rico.</p><h4><i class='fas fa-language'></i> Lingue</h4><p>Le lingue ufficiali sono lo spagnolo e l'inglese.</p>";

//                default:
//                    return $"<h4><i class='fas fa-globe'></i> {countryName}</h4><p>Informazioni di base su {countryName} non disponibili al momento.</p>";
//            }
//        }

//        private async Task<QuizData> GenerateQuizQuestionsFromGemini(string countryName, string countryInfo, int questionCount = 10, string difficulty = "medium")
//        {
//            try
//            {
//                // Log dettagliato all'inizio del metodo
//                _logger.LogInformation("GenerateQuizQuestionsFromGemini chiamato con i seguenti parametri:");
//                _logger.LogInformation("- countryName: {CountryName}", countryName);
//                _logger.LogInformation("- questionCount: {QuestionCount}", questionCount);
//                _logger.LogInformation("- difficulty: {Difficulty}", difficulty);

//                // Ottieni API key da configurazione
//                var apiKey = _configuration["GeminiApiKey"];
//                if (string.IsNullOrEmpty(apiKey))
//                {
//                    throw new Exception("GeminiApiKey non trovata nella configurazione");
//                }

//                // URL API Gemini
//                var endpoint = "https://generativelanguage.googleapis.com/v1/models/gemini-1.5-pro:generateContent";
//                var url = $"{endpoint}?key={apiKey}";

//                _logger.LogInformation("Chiamata Gemini API per generazione quiz di {QuestionCount} domande su {Country} con difficoltà {Difficulty}",
//                    questionCount, countryName, difficulty);

//                // Adatta la difficoltà per il prompt
//                string difficultyText;
//                switch (difficulty.ToLower())
//                {
//                    case "easy":
//                        difficultyText = "facile, con domande semplici e dirette";
//                        break;
//                    case "hard":
//                        difficultyText = "difficile, con domande dettagliate e specifiche";
//                        break;
//                    default:
//                        difficultyText = "media, con domande moderatamente complesse";
//                        break;
//                }

//                // Crea prompt per generare domande di quiz
//                // Crea prompt per generare domande di quiz
//                // Nel metodo GenerateQuizQuestionsFromGemini
//                var promptText = $@"IMPORTANTE: Genererai un quiz basato ESCLUSIVAMENTE sul seguente testo che descrive {countryName}.
//Non usare ASSOLUTAMENTE conoscenze esterne o informazioni non presenti in questo testo.

//============ TESTO SU {countryName.ToUpper()} (INIZIA QUI) ============
//{countryInfo}
//============ TESTO SU {countryName.ToUpper()} (FINISCE QUI) ============

//Crea un quiz di {questionCount} domande a scelta multipla basate SOLO sul testo sopra.
//Le domande devono essere di difficoltà {difficultyText}, con 4 opzioni di risposta per ogni domanda.

//REQUISITI ESSENZIALI:
//1. OGNI domanda DEVE essere basata ESCLUSIVAMENTE su informazioni presenti nel testo sopra
//2. NON INVENTARE fatti, cifre, date o informazioni
//3. Se il testo non contiene abbastanza informazioni su un argomento, NON creare domande su quell'argomento
//4. Per ogni domanda, nella spiegazione, indica da quale parte del testo hai preso l'informazione
//5. Le risposte sbagliate devono essere plausibili ma chiaramente non corrette in base al testo

//Formatta la risposta come un JSON valido con la seguente struttura:
//{{
//  ""countryName"": ""{countryName}"",
//  ""questions"": [
//    {{
//      ""question"": ""Domanda basata sul testo"",
//      ""options"": [""Opzione A"", ""Opzione B"", ""Opzione C"", ""Opzione D""],
//      ""correctAnswerIndex"": 0,
//      ""explanation"": ""Spiegazione che cita la parte del testo da cui è tratta l'informazione"",
//      ""category"": ""Categoria"",
//      ""textReference"": ""La parte esatta del testo da cui hai preso l'informazione""
//    }},
//    ... altre domande ...
//  ]
//}}
//NOTA IMPORTANTE: Varia la posizione della risposta corretta tra le opzioni. Non inserirla sempre nella stessa posizione.
//Genera esattamente {questionCount} domande. Verifica che ogni domanda sia effettivamente basata sul testo e non su conoscenze esterne.
//Produci SOLO il JSON, senza altri commenti o spiegazioni.";
//                // Preparazione della richiesta per Gemini
//                // Preparazione della richiesta per Gemini con temperature più bassa per maggiore affidabilità
//                var requestObj = new
//                {
//                    contents = new[]
//                    {
//        new
//        {
//            parts = new[]
//            {
//                new { text = promptText }
//            },
//            role = "user"
//        }
//    },
//                    generationConfig = new
//                    {
//                        temperature = 0.1, // Temperatura molto bassa per risposte più deterministiche
//                        maxOutputTokens = 4000,
//                        topP = 0.95,
//                        topK = 40
//                    },
//                    safetySettings = new[]
//                    {
//        new
//        {
//            category = "HARM_CATEGORY_HARASSMENT",
//            threshold = "BLOCK_NONE"
//        },
//        new
//        {
//            category = "HARM_CATEGORY_HATE_SPEECH",
//            threshold = "BLOCK_NONE"
//        },
//        new
//        {
//            category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
//            threshold = "BLOCK_NONE"
//        },
//        new
//        {
//            category = "HARM_CATEGORY_DANGEROUS_CONTENT",
//            threshold = "BLOCK_NONE"
//        }
//    }
//                };
//                // Converti la richiesta in JSON
//                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
//                var jsonRequest = JsonSerializer.Serialize(requestObj, jsonOptions);

//                // Invia la richiesta a Gemini
//                var requestContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync(url, requestContent);

//                if (!response.IsSuccessStatusCode)
//                {
//                    _logger.LogWarning("Risposta non valida da Gemini API: {StatusCode}", response.StatusCode);
//                    throw new Exception($"Errore nella chiamata a Gemini API: {response.StatusCode}");
//                }

//                var responseContent = await response.Content.ReadAsStringAsync();
//                _logger.LogDebug("Risposta ricevuta da Gemini API");

//                // Estrai il JSON dalla risposta di Gemini
//                var jsonResponse = JsonDocument.Parse(responseContent);
//                var candidates = jsonResponse.RootElement.GetProperty("candidates");

//                if (candidates.GetArrayLength() == 0)
//                {
//                    throw new Exception("Nessun contenuto generato da Gemini");
//                }

//                var content = candidates[0].GetProperty("content");
//                var parts = content.GetProperty("parts");

//                if (parts.GetArrayLength() == 0)
//                {
//                    throw new Exception("Contenuto vuoto nella risposta");
//                }

//                var text = parts[0].GetProperty("text").GetString();

//                // Pulisci il testo per assicurarsi che sia JSON valido
//                text = CleanJsonText(text ?? "");

//                _logger.LogDebug("JSON ricevuto e pulito da Gemini API");

//                // Deserializza il JSON in QuizData o usa dati di fallback se fallisce
//                QuizData quizData;
//                try
//                {

//                    quizData = JsonSerializer.Deserialize<QuizData>(text, new JsonSerializerOptions
//                    {
//                        PropertyNameCaseInsensitive = true
//                    }) ?? new QuizData { CountryName = countryName };

//                    // Verifica i dati
//                    if (quizData.Questions.Count == 0)
//                    {
//                        throw new Exception("JSON valido ma nessuna domanda trovata");
//                    }

//                    // SOLUZIONE ALTERNATIVA: Forza il numero esatto di domande richieste
//                    _logger.LogInformation("Ricevute {ReceivedCount} domande da Gemini, richieste {RequestedCount}",
//                        quizData.Questions.Count, questionCount);

//                    // Se abbiamo troppe domande, riduci il numero
//                    if (quizData.Questions.Count > questionCount)
//                    {
//                        _logger.LogInformation("Rimuovendo {ExcessCount} domande in eccesso",
//                            quizData.Questions.Count - questionCount);
//                        quizData.Questions = quizData.Questions.Take(questionCount).ToList();
//                    }
//                    // Se abbiamo meno domande del richiesto, crea domande aggiuntive
//                    else if (quizData.Questions.Count < questionCount)
//                    {
//                        int missingCount = questionCount - quizData.Questions.Count;
//                        _logger.LogInformation("Aggiungendo {MissingCount} domande mancanti", missingCount);

//                        // Genera domande aggiuntive
//                        var additionalQuestions = CreateAdditionalQuestions(countryName, missingCount);
//                        quizData.Questions.AddRange(additionalQuestions);
//                    }

//                }
//                catch (JsonException ex)
//                {
//                    _logger.LogError(ex, "Errore parsing JSON per il quiz di {Country}. JSON ricevuto: {JsonText}",
//                        countryName, text.Substring(0, Math.Min(500, text.Length)));

//                    // Usa dati di fallback
//                    quizData = GetFallbackQuizData(countryName, questionCount);
//                }


//                return quizData;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Errore nella generazione delle domande per il quiz su {CountryName}", countryName);

//                // Usa dati di fallback in caso di errore
//                return GetFallbackQuizData(countryName, questionCount);
//            }
//        }

//        private string CleanJsonText(string text)
//        {
//            if (string.IsNullOrEmpty(text)) return "{}";

//            // Rimuovi eventuali backtick o delimitatori markdown
//            text = text.Replace("```json", "").Replace("```", "").Trim();

//            // Rimuovi spazi iniziali o finali
//            return text.Trim();
//        }
//        private async Task<string> GetCountryReadMoreInfo(string countryName)
//        {
//            try
//            {
//                _logger.LogInformation("Ottenendo informazioni per {Country} da GeminiProxy", countryName);

//                // Usa lo stesso endpoint che viene chiamato quando l'utente fa "Read More"
//                var baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
//                var endpoint = $"{baseUrl}/api/GeminiProxy/countryinfo?countryName={Uri.EscapeDataString(countryName)}";

//                // Crea HttpClient locale se non disponibile come campo
//                using var httpClient = new HttpClient();

//                var response = await httpClient.GetAsync(endpoint);
//                if (!response.IsSuccessStatusCode)
//                {
//                    _logger.LogWarning("Risposta non valida da GeminiProxy: {StatusCode}", response.StatusCode);
//                    throw new Exception($"Errore nel recupero delle informazioni sul paese: {response.StatusCode}");
//                }

//                var countryInfo = await response.Content.ReadAsStringAsync();

//                // Verifica la validità delle informazioni
//                if (string.IsNullOrWhiteSpace(countryInfo) || countryInfo.Length < 100)
//                {
//                    _logger.LogWarning("Informazioni insufficienti su {Country} ({Length} caratteri)",
//                        countryName, countryInfo?.Length ?? 0);

//                    // Usa dati fallback per paesi comuni
//                    if (IsCommonCountry(countryName))
//                    {
//                        _logger.LogInformation("Utilizzando dati fallback per {Country}", countryName);
//                        return GetFallbackCountryInfo(countryName);
//                    }
//                }

//                _logger.LogInformation("Ottenute {Length} caratteri di informazioni su {Country}",
//                    countryInfo?.Length ?? 0, countryName);

//                return countryInfo ?? string.Empty;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Errore nel recupero delle informazioni su {Country}", countryName);

//                // Usa dati fallback in caso di errore per alcuni paesi comuni
//                if (IsCommonCountry(countryName))
//                {
//                    _logger.LogInformation("Utilizzando dati fallback per {Country} dopo errore", countryName);
//                    return GetFallbackCountryInfo(countryName);
//                }

//                // Se non abbiamo un fallback, genera un messaggio generico
//                return $"<h4>Informazioni su {countryName}</h4><p>Il {countryName} è un paese con una ricca storia e cultura.</p>";
//            }
//        }
//        private List<QuizQuestion> CreateAdditionalQuestions(string countryName, int count)
//        {
//            _logger.LogInformation("Creazione di {Count} domande aggiuntive per {Country}", count, countryName);

//            var fallbackQuiz = GetFallbackQuizData(countryName, count);
//            return fallbackQuiz.Questions;
//        }

//        private QuizData GetFallbackQuizData(string countryName, int questionCount = 10)
//        {
//            _logger.LogWarning("Utilizzo quiz di fallback per {Country} con {QuestionCount} domande", countryName, questionCount);

//            // Lista che conterrà le domande di fallback
//            var fallbackQuestions = new List<QuizQuestion>();
//            _logger.LogWarning("Utilizzo quiz di fallback per {Country} con {QuestionCount} domande", countryName, questionCount);



//            // Crea l'oggetto QuizData
//            var quizData = new QuizData
//            {
//                CountryName = countryName,
//                Questions = fallbackQuestions
//            };

//            // Randomizza le posizioni delle risposte
//            return RandomizeAnswerPositions(quizData);

//            // Assicurati di limitare il numero di domande al valore richiesto
//            //if (fallbackQuestions.Count > questionCount)
//            //{
//            //    _logger.LogInformation("Riducendo il numero di domande di fallback da {Original} a {Requested}",
//            //        fallbackQuestions.Count, questionCount);
//            //    fallbackQuestions = fallbackQuestions.Take(questionCount).ToList();
//            //}
//            //else if (fallbackQuestions.Count < questionCount)
//            //{
//            //    _logger.LogInformation("Mancano {Missing} domande per raggiungere il numero richiesto",
//            //        questionCount - fallbackQuestions.Count);

//            //    // Aggiungi domande generiche fino a raggiungere il numero richiesto
//            //    int missingCount = questionCount - fallbackQuestions.Count;
//            //    for (int i = 0; i < missingCount; i++)
//            //    {
//            //        fallbackQuestions.Add(new QuizQuestion
//            //        {
//            //            Question = $"Domanda generica #{i + 1} su {countryName}",
//            //            Options = new List<string> { "Opzione A", "Opzione B", "Opzione C", "Opzione D" },
//            //            CorrectAnswerIndex = 0,
//            //            Explanation = $"Spiegazione generica per la domanda #{i + 1}",
//            //            Category = i % 2 == 0 ? "Geografia" : "Cultura"
//            //        });
//            //    }
//            //}

//            //_logger.LogInformation("Restituzione di {Count} domande di fallback per {Country}",
//            //    fallbackQuestions.Count, countryName);

//            //return new QuizData
//            //{
//            //    CountryName = countryName,
//            //    Questions = fallbackQuestions
//            //};
//        }
//    }
//}
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace Geography4Geek_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizController : ControllerBase
    {
        private readonly ILogger<QuizController> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _geminiApiKey;

        public QuizController(ILogger<QuizController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = new HttpClient();
            _geminiApiKey = _configuration["GeminiApiKey"] ?? "";
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateQuiz([FromBody] JsonElement requestData)
        {
            try
            {
                _logger.LogInformation("Richiesta di generazione quiz ricevuta: {Data}", JsonSerializer.Serialize(requestData));

                // Estrai i dati dalla richiesta
                string countryName = string.Empty;
                int questionCount = 5; // Default value
                string difficulty = "medium"; // Default value

                if (requestData.TryGetProperty("CountryName", out var countryElem))
                {
                    countryName = countryElem.GetString() ?? string.Empty;
                }

                if (requestData.TryGetProperty("QuestionCount", out var countElem))
                {
                    questionCount = countElem.GetInt32();
                }

                if (requestData.TryGetProperty("Difficulty", out var diffElem))
                {
                    difficulty = diffElem.GetString() ?? "medium";
                }

                _logger.LogInformation("Parametri quiz: Country={Country}, Questions={Count}, Difficulty={Difficulty}",
                    countryName, questionCount, difficulty);

                // Verifica il paese
                if (string.IsNullOrEmpty(countryName))
                {
                    return BadRequest(new { success = false, message = "Nome del paese non specificato" });
                }

                // Lista di paesi che richiedono una gestione speciale diretta con Gemini API
                var problematicCountries = new List<string> { "Peru", "Haiti" };

                // Per paesi problematici, usa direttamente Gemini API 1.5 Flash
                if (problematicCountries.Contains(countryName, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Generazione diretta da Gemini 1.5 Flash per {Country}", countryName);

                    var questions = await GenerateQuestionsWithGemini15(countryName, questionCount, difficulty);

                    if (questions == null || !questions.Any())
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Non è stato possibile generare domande per {countryName}"
                        });
                    }

                    var result = new
                    {
                        countryName = countryName,
                        questions = questions
                    };

                    return Ok(new
                    {
                        success = true,
                        message = "Quiz generato con successo",
                        quiz = result
                    });
                }

                // Per gli altri paesi, usa il flusso normale con GeminiProxy
                string baseUrl = $"{Request.Scheme}://{Request.Host}";
                string geminiUrl = $"{baseUrl}/api/GeminiProxy/country/{countryName}";

                var countryInfoResponse = await _httpClient.GetAsync(geminiUrl);

                if (!countryInfoResponse.IsSuccessStatusCode)
                {
                    var errorContent = await countryInfoResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Errore GeminiProxy: {Status}, Content: {Content}",
                        countryInfoResponse.StatusCode, errorContent);

                    // Se il GeminiProxy fallisce, prova con la chiamata diretta a Gemini
                    _logger.LogInformation("Tentativo di generazione diretta con Gemini dopo fallimento proxy");

                    var backupQuestions = await GenerateQuestionsWithGemini15(countryName, questionCount, difficulty);

                    if (backupQuestions != null && backupQuestions.Any())
                    {
                        var backupResult = new
                        {
                            countryName = countryName,
                            questions = backupQuestions
                        };

                        return Ok(new
                        {
                            success = true,
                            message = "Quiz generato con successo (metodo alternativo)",
                            quiz = backupResult
                        });
                    }

                    return BadRequest(new
                    {
                        success = false,
                        message = $"Non è stato possibile ottenere informazioni su {countryName}"
                    });
                }

                var countryInfo = await countryInfoResponse.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(countryInfo) || countryInfo.Length < 100)
                {
                    _logger.LogWarning("Contenuto insufficiente da GeminiProxy: {Length} caratteri",
                        countryInfo?.Length ?? 0);

                    return BadRequest(new
                    {
                        success = false,
                        message = $"Informazioni insufficienti per {countryName}"
                    });
                }

                // Genera domande dal contenuto
                var sections = ExtractSectionsFromHtml(countryInfo);
                var generatedQuestions = GenerateQuestionsFromSections(countryName, sections, questionCount, difficulty);

                var standardResult = new
                {
                    countryName = countryName,
                    questions = generatedQuestions
                };

                return Ok(new
                {
                    success = true,
                    message = "Quiz generato con successo",
                    quiz = standardResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella generazione del quiz");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Errore interno: {ex.Message}"
                });
            }
        }

        [HttpGet("{country}")]
        public async Task<IActionResult> GetQuiz(string country)
        {
            try
            {
                if (string.IsNullOrEmpty(country))
                {
                    return BadRequest("Nome del paese non specificato");
                }

                _logger.LogInformation("Richiesta GET quiz per {Country}", country);

                // Lista di paesi problematici
                var problematicCountries = new List<string> { "Peru", "Haiti" };

                // Per paesi problematici, usa generazione diretta
                if (problematicCountries.Contains(country, StringComparer.OrdinalIgnoreCase))
                {
                    var geminiQuestions = await GenerateQuestionsWithGemini15(country, 5, "medium");

                    if (geminiQuestions == null || !geminiQuestions.Any())
                    {
                        return BadRequest($"Non è stato possibile generare domande per {country}");
                    }

                    var result = new
                    {
                        country = country,
                        questions = geminiQuestions,
                        countryInfo = $"Informazioni su {country}" // Placeholder
                    };

                    return Ok(result);
                }

                // Per altri paesi, continua con il flusso normale
                string baseUrl = $"{Request.Scheme}://{Request.Host}";
                string geminiUrl = $"{baseUrl}/api/GeminiProxy/country/{country}";

                var countryInfoResponse = await _httpClient.GetAsync(geminiUrl);

                if (!countryInfoResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Errore nel recupero delle informazioni per {Country}: {Status}",
                        country, countryInfoResponse.StatusCode);

                    // Tenta la generazione diretta come fallback
                    var backupQuestions = await GenerateQuestionsWithGemini15(country, 5, "medium");

                    if (backupQuestions != null && backupQuestions.Any())
                    {
                        var result = new
                        {
                            country = country,
                            questions = backupQuestions,
                            countryInfo = $"Informazioni alternative su {country}"
                        };

                        return Ok(result);
                    }

                    return BadRequest($"Non è stato possibile ottenere informazioni su {country}");
                }

                var countryInfo = await countryInfoResponse.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(countryInfo))
                {
                    _logger.LogError("Informazioni vuote per {Country}", country);
                    return BadRequest($"Informazioni non disponibili per {country}");
                }

                var sections = ExtractSectionsFromHtml(countryInfo);
                var sectionQuestions = GenerateQuestionsFromSections(country, sections);

                var response = new
                {
                    country = country,
                    questions = sectionQuestions,
                    countryInfo = countryInfo
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella generazione del quiz per {Country}", country);
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

        // Metodo per generare domande direttamente con Gemini 1.5 Flash
        private async Task<List<object>> GenerateQuestionsWithGemini15(string country, int questionCount, string difficulty)
        {
            try
            {
                _logger.LogInformation("Generazione {Count} domande con Gemini 1.5 Flash per {Country}",
                    questionCount, country);

                // Ottieni la API key da configurazione
                var apiKey = _configuration["GeminiApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("API key Gemini non configurata");
                    return null;
                }

                // Crea prompt specifico per la generazione di quiz con enfasi sul numero di domande
                string prompt = $@"Genera ESATTAMENTE {questionCount} domande a scelta multipla sul paese {country} di livello {difficulty}. 
                           Il numero di domande deve essere ESATTAMENTE {questionCount}, non di più e non di meno.
                           Ogni domanda deve avere 4 opzioni di risposta, con UNA SOLA risposta corretta.
                           Includi domande sulla capitale, geografia, popolazione, cultura, storia ed economia.
                           
                           Formatta il risultato come un array JSON con questa struttura esatta:
                           [
                             {{
                               ""question"": ""Domanda sulla capitale di {country}?"",
                               ""category"": ""Capitale"",
                               ""options"": [""Risposta corretta"", ""Opzione errata 1"", ""Opzione errata 2"", ""Opzione errata 3""],
                               ""correctAnswerIndex"": 0,
                               ""explanation"": ""Spiegazione della risposta corretta.""
                             }},
                             ... ripeti fino ad avere {questionCount} domande in totale ...
                           ]
                           
                           Assicurati che l'output sia un JSON valido che possa essere deserializzato direttamente.
                           Usa solo fatti reali e verificabili su {country}.
                           Distribuisci casualmente l'indice della risposta corretta tra 0, 1, 2 e 3.";

                // Prepara la richiesta per l'API Gemini 1.5 Flash
                var request = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,  // Temperatura bassa per risultati più coerenti
                        maxOutputTokens = 4096,  // Aumentato per supportare quiz più lunghi
                        topP = 0.95,
                        topK = 40
                    }
                };

                var requestJson = JsonSerializer.Serialize(request);
                var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // Effettua la chiamata all'API Gemini 1.5 Flash
                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}",
                    requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Errore risposta Gemini API: {Status}, Content: {Content}",
                        response.StatusCode, errorContent);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Risposta Gemini ricevuta: {Length} caratteri", responseContent.Length);

                // Estrai il JSON dalla risposta
                var responseJson = JsonDocument.Parse(responseContent);

                if (!responseJson.RootElement.TryGetProperty("candidates", out var candidates) ||
                    candidates.GetArrayLength() == 0)
                {
                    _logger.LogWarning("Nessun candidato trovato nella risposta Gemini");
                    return null;
                }

                var firstCandidate = candidates[0];

                if (!firstCandidate.TryGetProperty("content", out var candidateContent) ||
                    !candidateContent.TryGetProperty("parts", out var parts) ||
                    parts.GetArrayLength() == 0)
                {
                    _logger.LogWarning("Struttura della risposta Gemini non valida");
                    return null;
                }

                var text = parts[0].GetProperty("text").GetString() ?? "";

                // Trova l'array JSON nella risposta usando regex
                var jsonMatch = Regex.Match(text, @"\[\s*\{[\s\S]*\}\s*\]");

                if (!jsonMatch.Success)
                {
                    _logger.LogWarning("Nessun array JSON trovato nella risposta");
                    return null;
                }

                var json = jsonMatch.Value;

                try
                {
                    // Deserializza il JSON in una lista di domande
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var questions = JsonSerializer.Deserialize<List<object>>(json, options);

                    // Verifica che ci siano domande
                    if (questions == null || !questions.Any())
                    {
                        _logger.LogWarning("Nessuna domanda trovata nel JSON");
                        return null;
                    }

                    // Gestisci il caso in cui il numero di domande non corrisponda a quello richiesto
                    int actualCount = questions.Count;

                    if (actualCount != questionCount)
                    {
                        _logger.LogWarning("Il numero di domande generate ({Actual}) non corrisponde a quello richiesto ({Requested})",
                            actualCount, questionCount);

                        if (actualCount < questionCount)
                        {
                            // Se abbiamo meno domande del necessario, aggiungi domande generiche
                            _logger.LogInformation("Aggiunta di domande generiche per raggiungere il numero richiesto");

                            var categoryOptions = new[] { "Geografia", "Storia", "Cultura", "Economia" };
                            var random = new Random();

                            // Rinominato da genericQuestions a additionalQuestions per evitare confusione
                            var additionalQuestions = new List<object>();
                            for (int i = actualCount; i < questionCount; i++)
                            {
                                var category = categoryOptions[random.Next(categoryOptions.Length)];

                                additionalQuestions.Add(new
                                {
                                    question = $"Quale delle seguenti affermazioni su {country} è corretta?",
                                    category = category,
                                    options = new[] {
                        $"{country} è un paese con una storia ricca e interessante.",
                        $"{country} è famoso per la sua cucina tradizionale.",
                        $"{country} ha un'economia basata principalmente sul turismo.",
                        $"{country} ha un clima molto variabile a seconda della regione."
                    },
                                    correctAnswerIndex = random.Next(4),
                                    explanation = $"Questa è un'informazione generica su {country}."
                                });
                            }

                            // Converte le domande aggiuntive in oggetti JsonElement
                            var jsonString = JsonSerializer.Serialize(additionalQuestions);
                            var additionalQuestionsElements = JsonSerializer.Deserialize<List<object>>(jsonString, options);

                            // Aggiunge le domande aggiuntive a quelle esistenti
                            questions.AddRange(additionalQuestionsElements);
                        }
                        else if (actualCount > questionCount)
                        {
                            // Se abbiamo più domande del necessario, prendi solo le prime questionCount
                            questions = questions.Take(questionCount).ToList();
                        }
                    }

                    _logger.LogInformation("Domande generate con successo: {Count}", questions.Count);
                    return questions;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Errore nella deserializzazione del JSON: {Json}", json);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore generale nella generazione con Gemini 1.5");
                return null;
            }
        }

        private Dictionary<string, string> ExtractSectionsFromHtml(string html)
        {
            var sections = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(html))
            {
                return sections;
            }

            // Array di categorie da cercare
            var categories = new[]
            {
                "Popolazione", "Capitale", "Lingue", "Valuta", "Geografia",
                "Economia", "Cultura", "Storia", "Sistema Politico", "Fatti Interessanti"
            };

            foreach (var category in categories)
            {
                try
                {
                    // Pattern per trovare il contenuto tra i titoli h4
                    var pattern = $@"<h4><i class=['""]fas fa-[^""']+['""]></i>\s*{category}</h4>(.*?)(?=<h4>|$)";
                    var match = Regex.Match(html, pattern, RegexOptions.Singleline);

                    if (match.Success)
                    {
                        sections[category] = match.Groups[1].Value.Trim();
                    }
                    else
                    {
                        sections[category] = "";
                    }
                }
                catch
                {
                    sections[category] = "";
                }
            }

            return sections;
        }

        private List<object> GenerateQuestionsFromSections(string country, Dictionary<string, string> sections, int questionCount = 5, string difficulty = "medium")
        {
            var questions = new List<object>();
            var random = new Random();

            try
            {
                // Log del numero richiesto per debugging
                _logger.LogInformation("Generazione di {Count} domande per {Country} con difficoltà {Difficulty}",
                    questionCount, country, difficulty);

                // Capitale
                if (sections.ContainsKey("Capitale") && !string.IsNullOrEmpty(sections["Capitale"]))
                {
                    var capitalText = StripHtml(sections["Capitale"]);
                    var capitalMatch = new Regex(@"^([^,.]+)").Match(capitalText);
                    var capital = capitalMatch.Success ? capitalMatch.Groups[1].Value.Trim() : "Sconosciuta";

                    // Distractor capitals
                    var capitals = new[] {
                        "Parigi", "Londra", "Roma", "Madrid", "Berlino", "Lisbona",
                        "Vienna", "Atene", "Praga", "Budapest", "Dublino", "Lima",
                        "Santiago", "Bogotà", "Buenos Aires", "Quito"
                    };

                    var options = new List<string>();
                    options.Add(capital);

                    while (options.Count < 4)
                    {
                        var randomCapital = capitals[random.Next(capitals.Length)];
                        if (!options.Contains(randomCapital))
                        {
                            options.Add(randomCapital);
                        }
                    }

                    // Shuffle options
                    options = options.OrderBy(x => random.Next()).ToList();
                    var correctIndex = options.IndexOf(capital);

                    questions.Add(new
                    {
                        question = $"Qual è la capitale di {country}?",
                        category = "Capitale",
                        options = options.ToArray(),
                        correctAnswerIndex = correctIndex,
                        explanation = $"La capitale di {country} è {capital}."
                    });
                }

                // Popolazione
                if (sections.ContainsKey("Popolazione") && !string.IsNullOrEmpty(sections["Popolazione"]))
                {
                    var popText = StripHtml(sections["Popolazione"]);
                    var popMatch = new Regex(@"(\d[\d\.,]+)\s*(?:abitanti|persone|milioni)").Match(popText);

                    if (popMatch.Success)
                    {
                        var popValue = popMatch.Groups[1].Value;

                        var options = new string[4];
                        var correctIndex = random.Next(4);

                        for (int i = 0; i < 4; i++)
                        {
                            if (i == correctIndex)
                            {
                                options[i] = $"{popValue} abitanti";
                            }
                            else
                            {
                                // Genera valore casuale
                                var factor = 0.5 + random.NextDouble() * 1.5;
                                options[i] = $"{random.Next(1, 100)} milioni di abitanti";
                            }
                        }

                        questions.Add(new
                        {
                            question = $"Qual è la popolazione di {country}?",
                            category = "Popolazione",
                            options = options,
                            correctAnswerIndex = correctIndex,
                            explanation = $"La popolazione di {country} è {options[correctIndex]}."
                        });
                    }
                }

                // Genera altre domande sulle sezioni disponibili
                // (Geografia, Storia, Economia, ecc.)

                // Genera domande generiche se non abbiamo abbastanza domande
                while (questions.Count < questionCount)
                {
                    var categoryOptions = new[] { "Geografia", "Storia", "Cultura", "Economia" };
                    var category = categoryOptions[random.Next(categoryOptions.Length)];

                    var genericQuestion = new
                    {
                        question = $"Quale delle seguenti affermazioni su {country} è corretta?",
                        category = category,
                        options = new[] {
                            $"{country} è un paese con una storia ricca e interessante.",
                            $"{country} è famoso per la sua cucina tradizionale.",
                            $"{country} ha un'economia basata principalmente sul turismo.",
                            $"{country} ha un clima molto variabile a seconda della regione."
                        },
                        correctAnswerIndex = random.Next(4),
                        explanation = $"Questa è un'informazione generica su {country}."
                    };

                    questions.Add(genericQuestion);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella generazione delle domande");

                // Fallback a domande generiche
                questions.Clear(); // Pulisci eventuali domande parziali

                // Assicurati di generare esattamente questionCount domande
                for (int i = 0; i < questionCount; i++)
                {
                    questions.Add(new
                    {
                        question = $"Domanda su {country} #{i + 1}",
                        category = "Generale",
                        options = new[] { "Opzione A", "Opzione B", "Opzione C", "Opzione D" },
                        correctAnswerIndex = 0,
                        explanation = $"Questa è una domanda generica su {country}."
                    });
                }
            }

            // Assicurati che il numero di domande sia esattamente quello richiesto
            if (questions.Count > questionCount)
            {
                // Se abbiamo troppe domande, prendiamo solo le prime questionCount
                questions = questions.Take(questionCount).ToList();
            }

            return questions;
        }

        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            return Regex.Replace(html, @"<[^>]+>", "").Trim();
        }
    }
}
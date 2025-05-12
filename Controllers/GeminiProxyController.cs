using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
//SAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAARS
namespace Geography4Geek_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeminiProxyController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiProxyController> _logger;
        private readonly IMemoryCache _memoryCache;

        // Tempo di cache in ore
        private const int CACHE_HOURS = 24;

        public GeminiProxyController(IConfiguration configuration, ILogger<GeminiProxyController> logger, IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        [HttpGet("countryinfo")]
        public async Task<IActionResult> GetCountryInfo(string countryName)
        {
            if (string.IsNullOrEmpty(countryName))
            {
                return BadRequest("Il nome del paese è richiesto.");
            }

            try
            {
                // IMPORTANTE: Genera sempre le informazioni direttamente dall'API, 
                // senza MAI controllare o usare la cache
                _logger.LogInformation("Richiesta informazioni direttamente dall'API per {CountryName} (bypass cache)", countryName);

                // Chiamata diretta all'API Gemini senza controlli sulla cache
                string countryInfo = await GenerateCountryInfo(countryName);

                // LOG per debug
                _logger.LogInformation($"Informazioni generate per {countryName} - Lunghezza contenuto: {countryInfo?.Length ?? 0} caratteri");

                // NON memorizzare mai in cache i risultati
                // La riga seguente era nella versione precedente ed è stata rimossa:
                // _memoryCache.Set(cacheKey, countryInfo, cacheOptions);

                return Content(countryInfo, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero delle informazioni sul paese {CountryName}", countryName);
                return StatusCode(500, "Errore durante il recupero delle informazioni sul paese.");
            }
        }



        [HttpGet("countryinfo/refresh")]
        public async Task<IActionResult> RefreshCountryInfo(string countryName)
        {
            if (string.IsNullOrEmpty(countryName))
            {
                return BadRequest("Il nome del paese è richiesto.");
            }

            try
            {
                string cacheKey = $"country_info_{countryName.ToLower().Replace(" ", "_")}";

                // Rimuovi eventuali informazioni in cache
                _memoryCache.Remove(cacheKey);
                _logger.LogInformation("Rimossa cache per {CountryName}", countryName);

                // Genera nuove informazioni
                string countryInfo = await GenerateCountryInfo(countryName);

                // NON salvare in cache - modalità refresh forzato

                return Content(countryInfo, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento delle informazioni per {CountryName}", countryName);
                return StatusCode(500, "Errore durante l'aggiornamento delle informazioni sul paese.");
            }
        }

        [HttpGet("debug/nocache")]
        public IActionResult DebugNoCache(string countryName)
        {
            if (string.IsNullOrEmpty(countryName))
            {
                return BadRequest("Il nome del paese è richiesto.");
            }

            string cacheKey = $"country_info_{countryName.ToLower().Replace(" ", "_")}";
            bool existsInCache = _memoryCache.TryGetValue(cacheKey, out _);

            return Ok(new
            {
                countryName = countryName,
                cacheKey = cacheKey,
                existsInCache = existsInCache,
                cacheDisabled = true,
                timestamp = DateTime.UtcNow,
                user = User.Identity?.Name ?? "Anonymous"
            });
        }

        [HttpGet("test-key")]
        public IActionResult TestApiKey()
        {
            var apiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest("API Key non trovata nella configurazione");
            }

            // Mostra solo i primi 4 caratteri per sicurezza
            return Ok($"API Key configurata: {apiKey.Substring(0, 4)}...");
        }

        [HttpGet("cache/list")]
        public IActionResult ListCache()
        {
            try
            {
                var cacheField = typeof(MemoryCache).GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var cache = cacheField?.GetValue(_memoryCache) as IDictionary<object, object>;

                if (cache == null)
                    return Ok(new { message = "Impossibile accedere alla cache" });

                var countryEntries = cache.Keys
                    .Where(k => k.ToString().StartsWith("country_info_"))
                    .Select(k => k.ToString().Replace("country_info_", "").Replace("_", " "))
                    .ToList();

                return Ok(new { countries = countryEntries, count = countryEntries.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("cache/clear")]
        public IActionResult ClearCache()
        {
            try
            {
                var cacheField = typeof(MemoryCache).GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var cache = cacheField?.GetValue(_memoryCache) as IDictionary<object, object>;

                if (cache != null)
                {
                    var countryEntries = cache.Keys
                        .Where(k => k.ToString().StartsWith("country_info_"))
                        .ToList();

                    foreach (var key in countryEntries)
                    {
                        _memoryCache.Remove(key);
                    }

                    return Ok(new { message = $"Cache svuotata: rimossi {countryEntries.Count} paesi" });
                }

                return Ok(new { message = "Cache già vuota o inaccessibile" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<string> GenerateCountryInfo(string countryName)
        {
            try
            {
                // Log per tracciabilità
                _logger.LogInformation($"GenerateCountryInfo - Inizio generazione per {countryName}");

                // Chiamata all'API di Gemini - sempre nuova richiesta
                var geminiResponse = await CallGeminiAPI(countryName);

                // Log per debug
                _logger.LogInformation($"GenerateCountryInfo - Ricevuta risposta dall'API per {countryName}");

                // Verifica se la risposta contiene un errore
                if (geminiResponse != null && geminiResponse.StartsWith("Errore:"))
                {
                    // Controllo specifico per errore di quota
                    if (geminiResponse.Contains("429") ||
                        geminiResponse.Contains("RESOURCE_EXHAUSTED") ||
                        geminiResponse.Contains("quota"))
                    {
                        return GetQuotaExceededMessage(countryName, geminiResponse);
                    }

                    return GetErrorMessage(countryName, geminiResponse);
                }

                // Verifica se la risposta è valida
                if (!string.IsNullOrEmpty(geminiResponse))
                {
                    // Pulisci la risposta rimuovendo tag HTML indesiderati
                    geminiResponse = CleanupHtmlResponse(geminiResponse);

                    // Log per debug
                    _logger.LogInformation($"GenerateCountryInfo - Risposta elaborata con successo per {countryName}");

                    // Restituisci la risposta formattata con stili CSS
                    return $@"<div class='country-facts-container'>{geminiResponse}</div>
            <style>
                .country-facts-container {{
     font-family: 'Segoe UI', 'Roboto', sans-serif;
     line-height: 1.7;
     color: #333;
 }}
 .country-facts-container h4 {{
     color: #0f5e97;
     font-weight: 600;
     margin-top: 20px;
     margin-bottom: 14px;
     padding-bottom: 8px;
     border-bottom: 2px solid #e8f0f8;
     font-size: 1.4rem;
     display: flex;
     align-items: center;
 }}
 .country-facts-container h4 i {{
     margin-right: 10px;
 }}
 .country-facts-container p {{
     margin-bottom: 16px;
     text-align: justify;
 }}
 .country-facts-container ul {{
     list-style-type: none;
     padding-left: 0;
 }}
 .country-facts-container li {{
     position: relative;
     background-color: #f1f9ff;
     padding: 12px 15px 12px 45px;
     border-radius: 6px;
     margin-bottom: 10px;
     border-left: 3px solid #0f5e97;
 }}
 .country-facts-container li:before {{
     content: '';
     position: absolute;
     left: 15px;
     top: 50%;
     transform: translateY(-50%);
     width: 20px;
     height: 20px;
     background-color: #0f5e97;
     border-radius: 50%;
 }}
 
 /* Stili per evidenziare importanti valori numerici */
 .highlight-number {{
     font-weight: 600;
     color: #2980b9;
 }}
            </style>";
                }

                // Se la risposta non è valida, restituisci un messaggio di errore
                return GetErrorMessage(countryName, "Risposta vuota o non valida");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata a Gemini API per {CountryName}", countryName);
                return GetErrorMessage(countryName, ex.Message);
            }
        }

        private async Task<string?> CallGeminiAPI(string countryName)
        {
            _logger.LogInformation("Chiamata API diretta per {CountryName} senza usare cache interna", countryName);
            string[] apiKeys = new[] {
        "AIzaSyDIFWr-C_ZuIADU_jzLvEDHtljEWwC61ZA", // Sostituisci con la tua nuova chiave
        _configuration["GeminiApiKey"] ?? "",
        "AIzaSyCJ954A8QX-X1vp8dbZ5OE5I9aeT8wpm1Y"
    };

            // Modelli da provare in ordine
            string[] models = new[] {
        "gemini-1.5-flash",
    };

            Exception lastException = null;

            // Prova ogni combinazione di chiave API e modello
            foreach (var apiKey in apiKeys.Where(k => !string.IsNullOrEmpty(k)))
            {
                foreach (var model in models)
                {
                    try
                    {
                        _logger.LogInformation("Tentativo con modello {Model} e chiave API che inizia con {KeyPrefix}",
                            model, apiKey.Substring(0, Math.Min(5, apiKey.Length)) + "...");

                        // Configura client HTTP
                        using var httpClient = new HttpClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(30);

                        var endpoint = $"https://generativelanguage.googleapis.com/v1/models/{model}:generateContent";
                        var url = $"{endpoint}?key={apiKey}";

                        // Il tuo prompt originale completo
                        string prompt = $@"Fornisci informazioni dettagliate e accurate su {countryName} nei seguenti ambiti:
1. Popolazione (con statistiche specifiche, numeri aggiornati e percentuali)
2. Capitale (con dettagli storici e culturali)
3. Lingue ufficiali e parlate (con percentuali e dettagli sulle minoranze linguistiche)
4. Valuta (con dettagli storici e caratteristiche)
5. Geografia (con dati precisi su dimensioni, caratteristiche geografiche, confini, ecc.)
6. Economia (con statistiche, percentuali del PIL, settori principali, export)
7. Cultura (con dettagli sul patrimonio, tradizioni, contributi culturali globali)
8. Storia (eventi chiave con date precise e fatti significativi)
9. Sistema politico (dettagli sulla struttura governativa)
10. Almeno 10 fatti interessanti, molto specifici e poco conosciuti (con date, numeri e dettagli precisi)

Rispondi in italiano. Includi sempre dati numerici specifici (percentuali, date, dimensioni) per ogni sezione. Le informazioni devono essere sufficientemente dettagliate per creare un quiz difficile di 10 domande.

IMPORTANTE sulla FORMATTAZIONE:
- Formatta la risposta direttamente in HTML puro senza delimitatori markdown o blocchi di codice.
- NON usare ```html o ``` all'inizio o alla fine della risposta.
- Scrivi tutti i numeri SENZA simboli di valuta nel mezzo. Gli anni e le date devono essere numeri puri (1881, 1993, 2025).
- Per i valori monetari, usa il simbolo della valuta PRIMA del valore numerico (es. €50) o il nome della valuta DOPO con uno spazio (es. 50 euro).
- ATTENZIONE: NON inserire MAI simboli ($, €, £, ecc.) in mezzo ai numeri.

Usa i seguenti tag:
- Usa tag <h4> per i titoli delle sezioni con icone FontAwesome appropriate
- Usa tag <p> per i paragrafi
- Usa tag <ul> e <li> per elencare i fatti interessanti
- NON aggiungere il nome del paese all'inizio come titolo

Esempi di tag con icone:
<h4><i class='fas fa-users'></i> Popolazione</h4>
<h4><i class='fas fa-landmark'></i> Capitale</h4>
<h4><i class='fas fa-language'></i> Lingue</h4>
<h4><i class='fas fa-money-bill-wave'></i> Valuta</h4>
<h4><i class='fas fa-mountain'></i> Geografia</h4>
<h4><i class='fas fa-chart-line'></i> Economia</h4>
<h4><i class='fas fa-theater-masks'></i> Cultura</h4>
<h4><i class='fas fa-book-open'></i> Storia</h4>
<h4><i class='fas fa-balance-scale'></i> Sistema Politico</h4>
<h4><i class='fas fa-lightbulb'></i> Fatti Interessanti</h4>";

                        // Configura la richiesta come nell'originale, ma usa modello e chiave API correnti
                        var requestObj = new
                        {
                            contents = new[]
                            {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            },
                            role = "user"
                        }
                    },
                            generationConfig = new
                            {
                                temperature = 0.2,
                                maxOutputTokens = 4000,
                                topP = 0.95,
                                topK = 40
                            },
                            safetySettings = new[]
                            {
                        new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
                    }
                        };

                        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                        var jsonRequest = JsonSerializer.Serialize(requestObj, jsonOptions);
                        var requestContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                        var response = await httpClient.PostAsync(url, requestContent);
                        var responseRaw = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var jsonResponse = JsonDocument.Parse(responseRaw);

                            if (jsonResponse.RootElement.TryGetProperty("candidates", out var candidates) &&
                                candidates.GetArrayLength() > 0 &&
                                candidates[0].TryGetProperty("content", out var content) &&
                                content.TryGetProperty("parts", out var parts) &&
                                parts.GetArrayLength() > 0 &&
                                parts[0].TryGetProperty("text", out var textElement))
                            {
                                var result = textElement.GetString();
                                if (!string.IsNullOrEmpty(result))
                                {
                                    result = RemoveMarkdownCodeDelimiters(result);
                                    result = FixMixedNumbersWithSymbols(result);

                                    _logger.LogInformation("Richiesta completata con successo usando modello {Model}", model);
                                    return result;
                                }
                            }

                            _logger.LogWarning("Formato risposta non standard con {Model}", model);
                            continue; // Prova il prossimo modello o chiave
                        }
                        else
                        {
                            _logger.LogWarning("Errore API {Status} con modello {Model}: {Error}",
                                response.StatusCode, model, responseRaw);
                            continue; // Prova il prossimo modello o chiave
                        }
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, "Eccezione con modello {Model}", model);
                        // Continua con la prossima combinazione
                    }
                }
            }

            // Se tutte le combinazioni falliscono
            if (lastException != null)
            {
                _logger.LogError(lastException, "Tutti i tentativi di API falliti per {CountryName}", countryName);
                return $"Errore: Tutti i tentativi di connessione all'API hanno fallito. Ultimo errore: {lastException.Message}";
            }
            else
            {
                return "Errore: Impossibile ottenere informazioni dal servizio Gemini dopo multipli tentativi.";
            }
        }
        // Cache per le risposte (riduce chiamate API)
        private static readonly Dictionary<string, Tuple<string, DateTime>> _countryInfoCache = new();
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

        // Metodo per provare con una chiave API alternativa
        private async Task<string?> TryAlternativeApiKey(string countryName)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // Usa una chiave alternativa (generane una nuova dalla console Google AI Studio)
                string alternativeApiKey = "INSERISCI_QUI_UNA_NUOVA_CHIAVE_API";

                // URL per gemini-1.0-pro (più stabile e con quote più alte)
                var endpoint = "https://generativelanguage.googleapis.com/v1/models/gemini-1.0-pro:generateContent";
                var url = $"{endpoint}?key={alternativeApiKey}";

                _logger.LogInformation("Tentativo con chiave API alternativa e modello fallback");

                // Usa lo stesso prompt e configurazione dell'originale...
                // [il resto del codice è simile al metodo principale]

                // Crea un prompt più breve per gemini-1.0-pro (per evitare errori di quota)
                string prompt = $@"Fornisci informazioni essenziali su {countryName} in questi ambiti: Popolazione, Capitale, Lingue, Valuta, Geografia, Economia, Cultura, Storia, Sistema Politico e 5 fatti interessanti. Rispondi in italiano con HTML formattato usando h4 per titoli e p per paragrafi.";

                // Richiesta con parametri più conservativi
                var requestObj = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        maxOutputTokens = 2000,
                        topP = 0.8,
                        topK = 40
                    }
                };

                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var jsonRequest = JsonSerializer.Serialize(requestObj, jsonOptions);
                var requestContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, requestContent);
                var responseRaw = await response.Content.ReadAsStringAsync();

                // Gestione della risposta...
                if (!response.IsSuccessStatusCode)
                {
                    return "Errore: Anche la chiave API alternativa ha fallito. Prova di nuovo più tardi.";
                }

                // Elabora risposta come nel metodo principale...
                var jsonResponse = JsonDocument.Parse(responseRaw);
                if (jsonResponse.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var textElement))
                {
                    var result = textElement.GetString();
                    if (!string.IsNullOrEmpty(result))
                    {
                        result = RemoveMarkdownCodeDelimiters(result);
                        result = FixMixedNumbersWithSymbols(result);
                        return result;
                    }
                }

                return "Errore: Formato risposta non previsto anche con chiave alternativa.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eccezione durante chiamata con chiave alternativa per {CountryName}", countryName);
                return $"Errore con chiave alternativa: {ex.Message}";
            }
        }

        // Metodo per provare con una chiave API alternativa

        // Metodo per controllare prima la cache
        private string? GetCountryInfoWithCache(string countryName)
        {
            // Controlla se abbiamo una risposta in cache
            if (_countryInfoCache.TryGetValue(countryName, out var cachedInfo))
            {
                if (DateTime.UtcNow - cachedInfo.Item2 < _cacheDuration)
                {
                    _logger.LogInformation("Informazioni per {CountryName} recuperate dalla cache", countryName);
                    return cachedInfo.Item1;
                }
            }

            // Se non in cache o scaduta, restituisci null
            return null;
        }

        // Metodo per salvare in cache
        private void CacheCountryInfo(string countryName, string info)
        {
            _countryInfoCache[countryName] = Tuple.Create(info, DateTime.UtcNow);
            _logger.LogInformation("Informazioni per {CountryName} salvate in cache", countryName);
        }



        private string RemoveMarkdownCodeDelimiters(string response)
        {
            // Usa regex per rimuovere delimitatori di codice in vari formati
            string pattern = @"^```(?:html|HTML)?[\r\n]+([\s\S]*?)```[\r\n]*$";

            var match = Regex.Match(response, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            return response;
        }

        // Metodo potenziato per correggere qualsiasi simbolo di valuta mischiato con numeri
        private string FixMixedNumbersWithSymbols(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            _logger.LogInformation("Inizio correzione numeri con simboli valuta");

            // Array di tutti i possibili simboli di valuta
            string[] currencySymbols = new[] { "$", "€", "£", "¥", "₹", "₽", "₩", "₱", "₺", "₲", "₴", "₸" };

            // Crea un pattern regex dinamico per qualsiasi simbolo di valuta
            string symbolsPattern = string.Join("|", currencySymbols.Select(s => Regex.Escape(s)));

            // Pattern per trovare qualsiasi numero che contiene un simbolo di valuta nel mezzo
            string generalPattern = $@"(\d+)({symbolsPattern})(\d+)";

            // Applica la correzione generale: rimuovi il simbolo dal mezzo dei numeri
            string corrected = Regex.Replace(html, generalPattern, m => {
                string symbol = m.Groups[2].Value;
                string number = m.Groups[1].Value + m.Groups[3].Value;

                // Check if it looks like a year (1800-2099)
                bool looksLikeYear = false;
                if (int.TryParse(number, out int year) && year >= 1700 && year <= 2099)
                {
                    looksLikeYear = true;
                }

                // Se sembra un anno, non aggiungere il simbolo della valuta
                if (looksLikeYear)
                {
                    _logger.LogInformation("Corretto anno: {Original} -> {Fixed}", m.Value, number);
                    return number;
                }

                // Altrimenti posiziona il simbolo della valuta davanti al numero
                _logger.LogInformation("Corretto valore monetario: {Original} -> {Fixed}", m.Value, symbol + number);
                return symbol + number;
            });

            // Post-elaborazione per colorare i numeri significativi (date/anni) in rosso
            corrected = HighlightDates(corrected);

            return corrected;
        }

        // Evidenzia le date (opzionale)
        private string HighlightDates(string html)
        {
            // Evidenzia anni 1800-2099
            string patternYears = @"\b(1[7-9]\d{2}|20\d{2})\b";

            // Sostituisci con un elemento span colorato, ma solo se non è già dentro un tag
            string resultHtml = Regex.Replace(
                html,
                patternYears,
                match => {
                    // Evita sostituzione all'interno di tag HTML
                    var prevText = html.Substring(0, match.Index);
                    int openTags = Regex.Matches(prevText, "<").Count;
                    int closeTags = Regex.Matches(prevText, ">").Count;

                    if (openTags > closeTags) // Siamo dentro un tag
                        return match.Value;

                    return $"<span style='color:#e74c3c;font-weight:bold'>{match.Value}</span>";
                }
            );

            return resultHtml;
        }

        private string CleanupHtmlResponse(string response)
        {
            return response
                .Replace("<html>", "")
                .Replace("</html>", "")
                .Replace("<body>", "")
                .Replace("</body>", "")
                .Replace("<head>", "")
                .Replace("</head>", "")
                .Replace("<meta", "")
                .Replace("<title>", "")
                .Replace("</title>", "");
        }

        private string GetQuotaExceededMessage(string countryName, string errorDetails)
        {
            // Non cercare più paesi nella cache, poiché non usiamo più la cache

            // Calcola quando la quota sarà disponibile di nuovo
            var nowUtc = DateTime.UtcNow;
            var resetTime = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
            var timeUntilReset = resetTime - nowUtc;

            return $@"
    <div class='country-facts-container'>
        <div class='quota-error-box'>
            <h4><i class='fas fa-exclamation-triangle'></i> Limite di richieste raggiunto</h4>
            <p>Abbiamo raggiunto il limite di richieste giornaliere all'API di Gemini.</p>
            <p>La quota si resetterà tra <strong>{timeUntilReset.Hours} ore e {timeUntilReset.Minutes} minuti</strong> (alla mezzanotte UTC).</p>
        </div>
        
        <div class='manual-content-box'>
            <h4><i class='fas fa-info-circle'></i> Informazioni di base su {countryName}</h4>
            <p>Possiamo comunque procedere con un quiz basilare su {countryName} utilizzando informazioni geografiche generali.</p>
            
            <div class='geography-section'>
                <p>Ecco alcuni argomenti che potrebbero essere inclusi nel quiz:</p>
                <ul class='topic-list'>
                    <li>Posizione geografica e confini</li>
                    <li>Capitale e città principali</li>
                    <li>Lingue ufficiali e principali gruppi etnici</li>
                    <li>Forma di governo e sistema politico</li>
                    <li>Valuta e economia</li>
                    <li>Caratteristiche geografiche principali (fiumi, montagne, ecc.)</li>
                    <li>Eventi storici significativi</li>
                    <li>Tradizioni e cultura</li>
                </ul>
            </div>
        </div>
    </div>
    <style>
         .country-facts-container {{
      font-family: 'Segoe UI', 'Roboto', sans-serif;
      line-height: 1.7;
      color: #333;
  }}
  .quota-error-box {{
      background-color: #fff3cd;
      border-left: 4px solid #ffc107;
      padding: 18px;
      margin-bottom: 20px;
      border-radius: 4px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  }}
  .cached-countries-box {{
      background-color: #e8f4ff;
      border-left: 4px solid #0d6efd;
      padding: 18px;
      margin-bottom: 20px;
      border-radius: 4px;
  }}
  .cached-countries-box h5 {{
      color: #0d6efd;
      margin-bottom: 12px;
      font-weight: 600;
  }}
  .cached-countries-box h5 i {{
      margin-right: 8px;
  }}
  .country-tags {{
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-top: 12px;
  }}
  .country-tag {{
      background-color: #0d6efd;
      color: white;
      padding: 4px 12px;
      border-radius: 20px;
      font-size: 14px;
      cursor: pointer;
      transition: all 0.2s ease;
  }}
  .country-tag:hover {{
      background-color: #0a58ca;
      transform: translateY(-2px);
      box-shadow: 0 3px 5px rgba(0,0,0,0.2);
  }}
  .manual-content-box {{
      background-color: #f8f9fa;
      border-radius: 8px;
      padding: 20px;
      margin-top: 20px;
  }}
  .manual-content-box h4 {{
      color: #0f5e97;
      font-weight: 600;
      margin-bottom: 14px;
      display: flex;
      align-items: center;
  }}
  .manual-content-box h4 i {{
      margin-right: 10px;
  }}
  .geography-section {{
      margin-top: 16px;
  }}
  .topic-list {{
      list-style-type: none;
      padding-left: 0;
  }}
  .topic-list li {{
      position: relative;
      background-color: #f1f9ff;
      padding: 10px 15px 10px 40px;
      border-radius: 6px;
      margin-bottom: 8px;
      border-left: 3px solid #0f5e97;
  }}
  .topic-list li:before {{
      content: '';
      position: absolute;
      left: 15px;
      top: 50%;
      transform: translateY(-50%);
      width: 16px;
      height: 16px;
      background-color: #0f5e97;
      border-radius: 50%;
  }}
    </style>";
        }

        private string GetErrorMessage(string countryName, string errorDetails = "")
        {
            string errorInfo = !string.IsNullOrEmpty(errorDetails) ?
                $"<p><strong>Dettaglio tecnico:</strong> {errorDetails}</p>" : "";

            return $@"
            <div class='country-facts-container'>
                <div class='api-error-box'>
                    <h4><i class='fas fa-exclamation-circle'></i> Problema di connessione</h4>
                    <p>Si è verificato un errore durante la connessione all'API Gemini.</p>
                    {errorInfo}
                </div>
                
                <h4><i class='fas fa-lightbulb'></i> Informazioni per Quiz</h4>
                <p>Per creare un quiz interessante su {countryName}, cerca informazioni su:</p>
                <ul>
                    <li>Geografia: confini, capitale, montagne, fiumi e caratteristiche naturali uniche</li>
                    <li>Storia: fondazione, date significative, eventi importanti, personalità storiche</li>
                    <li>Cultura: arte, letteratura, musica, tradizioni, gastronomia</li>
                    <li>Economia: industrie principali, esportazioni, risorse naturali</li>
                    <li>Politica: sistema di governo, leader famosi, politiche internazionali</li>
                </ul>
            </div>
            <style>
                .country-facts-container {{
                    font-family: 'Segoe UI', 'Roboto', sans-serif;
                    line-height: 1.7;
                }}
                .api-error-box {{
                    background-color: #fff3f3;
                    border-left: 4px solid #e74c3c;
                    padding: 15px;
                    margin-bottom: 20px;
                    border-radius: 4px;
                }}
            </style>";
        }
    }

    // Classi per la serializzazione/deserializzazione JSON
    public class GeminiRequest
    {
        public List<GeminiContentItem> Contents { get; set; } = new List<GeminiContentItem>();
        public GeminiGenerationConfig GenerationConfig { get; set; } = new GeminiGenerationConfig();
    }

    public class GeminiContentItem
    {
        public List<GeminiPart> Parts { get; set; } = new List<GeminiPart>();
    }

    public class GeminiPart
    {
        public string Text { get; set; } = string.Empty;
    }

    public class GeminiGenerationConfig
    {
        public float Temperature { get; set; }
        public int TopK { get; set; }
        public float TopP { get; set; }
        public int MaxOutputTokens { get; set; }
    }
}
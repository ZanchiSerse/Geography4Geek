using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Geography4Geek_1.Pages.Quiz
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public bool IsQuizAvailable { get; set; }
        public string CountryName { get; set; } = string.Empty;
        public string FlagUrl { get; set; } = string.Empty;
        public Models.QuizQuestion CurrentQuestion { get; set; } = new();
        public int CurrentQuestionIndex { get; set; }
        public int CurrentQuestionNumber => CurrentQuestionIndex + 1;
        public int TotalQuestions { get; set; }
        public bool AnswerSubmitted { get; set; }
        public int SelectedAnswerIndex { get; set; }
        public bool IsCorrectAnswer { get; set; }
        public bool IsLastQuestion => CurrentQuestionIndex >= TotalQuestions - 1;
        public bool QuizCompleted { get; set; }
        public int Score { get; set; }
        public double ProgressPercentage => ((double)CurrentQuestionNumber / TotalQuestions) * 100;
        public string ScoreClass { get; set; } = string.Empty;
        public string ResultClass { get; set; } = string.Empty;
        public string ResultMessage { get; set; } = string.Empty;
        public string ResultDescription { get; set; } = string.Empty;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        // Solo un metodo OnGet con parametri opzionali
        public void OnGet(string country = null, string action = null)
        {
            try
            {
                _logger.LogInformation("OnGet chiamato con country={Country}, action={Action}", country, action);

                // Controlla se è stato passato un paese
                if (!string.IsNullOrEmpty(country))
                {
                    CountryName = country;
                    FlagUrl = GetFlagUrl(country);

                    // Controlla se è richiesto un reset
                    if (action == "restart")
                    {
                        _logger.LogInformation("Richiesto restart del quiz per {Country}", country);
                        // Elimina il quiz corrente (usa localStorage nel JavaScript)
                        IsQuizAvailable = true;
                        return;
                    }

                    // Semplicemente indica che è disponibile un quiz
                    IsQuizAvailable = true;
                }
                else
                {
                    // Nessun paese specificato
                    _logger.LogInformation("Nessun paese specificato");
                    IsQuizAvailable = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel caricamento del quiz");
                IsQuizAvailable = false;
            }
        }

        private string GetFlagUrl(string countryName)
        {
            // Semplificazione del codice per ottenere il codice ISO del paese
            var countryCode = GetCountryCode(countryName);
            return countryCode != null ? $"https://flagcdn.com/w160/{countryCode}.png" : "";
        }

        private string? GetCountryCode(string countryName)
        {
            // Dizionario semplificato dei codici paese
            var countryCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Nicaragua", "ni" }, // Aggiunto Nicaragua
                { "Afghanistan", "af" }, { "Albania", "al" }, { "Algeria", "dz" },
                { "Andorra", "ad" }, { "Angola", "ao" }, { "Argentina", "ar" },
                { "Armenia", "am" }, { "Australia", "au" }, { "Austria", "at" },
                { "Azerbaijan", "az" }, { "Bahamas", "bs" }, { "Bahrain", "bh" },
                { "Bangladesh", "bd" }, { "Barbados", "bb" }, { "Belarus", "by" },
                { "Belgium", "be" }, { "Belize", "bz" }, { "Benin", "bj" },
                { "Bhutan", "bt" }, { "Bolivia", "bo" }, { "Bosnia", "ba" },
                { "Botswana", "bw" }, { "Brazil", "br" }, { "Brunei", "bn" },
                { "Bulgaria", "bg" }, { "Burkina Faso", "bf" }, { "Burundi", "bi" },
                { "Cambodia", "kh" }, { "Cameroon", "cm" }, { "Canada", "ca" },
                { "Cape Verde", "cv" }, { "Central African Republic", "cf" },
                { "Chad", "td" }, { "Chile", "cl" }, { "China", "cn" },
                { "Colombia", "co" }, { "Comoros", "km" }, { "Costa Rica", "cr" },
                { "Croatia", "hr" }, { "Cuba", "cu" }, { "Cyprus", "cy" },
                { "Czechia", "cz" }, { "Czech Republic", "cz" }, { "Denmark", "dk" },
                { "Djibouti", "dj" }, { "Dominica", "dm" }, { "Dominican Republic", "do" },
                { "DR Congo", "cd" }, { "Ecuador", "ec" }, { "Egypt", "eg" },
                { "El Salvador", "sv" }, { "Equatorial Guinea", "gq" }, { "Eritrea", "er" },
                { "Estonia", "ee" }, { "Eswatini", "sz" }, { "Ethiopia", "et" },
                { "Fiji", "fj" }, { "Finland", "fi" }, { "France", "fr" },
                { "Gabon", "ga" }, { "Gambia", "gm" }, { "Georgia", "ge" },
                { "Germany", "de" }, { "Ghana", "gh" }, { "Greece", "gr" },
                { "Grenada", "gd" }, { "Guatemala", "gt" }, { "Guinea", "gn" },
                { "Guinea-Bissau", "gw" }, { "Guyana", "gy" }, { "Haiti", "ht" },
                { "Honduras", "hn" }, { "Hungary", "hu" }, { "Iceland", "is" },
                { "India", "in" }, { "Indonesia", "id" }, { "Iran", "ir" },
                { "Iraq", "iq" }, { "Ireland", "ie" }, { "Israel", "il" },
                { "Italy", "it" }, { "Ivory Coast", "ci" }, { "Jamaica", "jm" },
                { "Japan", "jp" }, { "Jordan", "jo" }, { "Kazakhstan", "kz" },
                { "Kenya", "ke" }, { "Kiribati", "ki" }, { "Kosovo", "xk" },
                { "Kuwait", "kw" }, { "Kyrgyzstan", "kg" }, { "Laos", "la" },
                { "Latvia", "lv" }, { "Lebanon", "lb" }, { "Lesotho", "ls" },
                { "Liberia", "lr" }, { "Libya", "ly" }, { "Liechtenstein", "li" },
                { "Lithuania", "lt" }, { "Luxembourg", "lu" }, { "Madagascar", "mg" },
                { "Malawi", "mw" }, { "Malaysia", "my" }, { "Maldives", "mv" },
                { "Mali", "ml" }, { "Malta", "mt" }, { "Marshall Islands", "mh" },
                { "Mauritania", "mr" }, { "Mauritius", "mu" }, { "Mexico", "mx" },
                { "Micronesia", "fm" }, { "Moldova", "md" }, { "Monaco", "mc" },
                { "Mongolia", "mn" }, { "Montenegro", "me" }, { "Morocco", "ma" },
                { "Mozambique", "mz" }, { "Myanmar", "mm" }, { "Namibia", "na" },
                { "Nauru", "nr" }, { "Nepal", "np" }, { "Netherlands", "nl" },
                { "New Zealand", "nz" }, { "Niger", "ne" },
                { "Nigeria", "ng" }, { "North Korea", "kp" }, { "North Macedonia", "mk" },
                { "Norway", "no" }, { "Oman", "om" }, { "Pakistan", "pk" },
                { "Palau", "pw" }, { "Palestine", "ps" }, { "Panama", "pa" },
                { "Papua New Guinea", "pg" }, { "Paraguay", "py" }, { "Peru", "pe" },
                { "Philippines", "ph" }, { "Poland", "pl" }, { "Portugal", "pt" },
                { "Puerto Rico", "pr" }, { "Qatar", "qa" }, { "Romania", "ro" },
                { "Russia", "ru" }, { "Rwanda", "rw" }, { "Saint Kitts and Nevis", "kn" },
                { "Saint Lucia", "lc" }, { "Saint Vincent and the Grenadines", "vc" },
                { "Samoa", "ws" }, { "San Marino", "sm" }, { "São Tomé and Príncipe", "st" },
                { "Saudi Arabia", "sa" }, { "Senegal", "sn" }, { "Serbia", "rs" },
                { "Seychelles", "sc" }, { "Sierra Leone", "sl" }, { "Singapore", "sg" },
                { "Slovakia", "sk" }, { "Slovenia", "si" }, { "Solomon Islands", "sb" },
                { "Somalia", "so" }, { "South Africa", "za" }, { "South Korea", "kr" },
                { "South Sudan", "ss" }, { "Spain", "es" }, { "Sri Lanka", "lk" },
                { "Sudan", "sd" }, { "Suriname", "sr" }, { "Sweden", "se" },
                { "Switzerland", "ch" }, { "Syria", "sy" }, { "Taiwan", "tw" },
                { "Tajikistan", "tj" }, { "Tanzania", "tz" }, { "Thailand", "th" },
                { "Timor-Leste", "tl" }, { "Togo", "tg" }, { "Tonga", "to" },
                { "Trinidad and Tobago", "tt" }, { "Tunisia", "tn" }, { "Turkey", "tr" },
                { "Turkmenistan", "tm" }, { "Tuvalu", "tv" }, { "Uganda", "ug" },
                { "Ukraine", "ua" }, { "United Arab Emirates", "ae" }, { "United Kingdom", "gb" },
                { "United States", "us" }, { "Uruguay", "uy" }, { "Uzbekistan", "uz" },
                { "Vanuatu", "vu" }, { "Vatican City", "va" }, { "Venezuela", "ve" },
                { "Vietnam", "vn" }, { "Yemen", "ye" }, { "Zambia", "zm" },
                { "Zimbabwe", "zw" }
            };

            return countryCodes.TryGetValue(countryName, out var code) ? code : null;
        }
    }
}
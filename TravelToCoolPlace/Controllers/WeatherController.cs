using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using TravelToCoolPlace.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TravelToCoolPlace.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherController : ControllerBase
    {
        #region Constructor and Properties

        private const string WeatherApiUrl = "https://api.open-meteo.com/v1/forecast";
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _jwtSecretKey = "tHIs_iS_mY_secREt_kEY_foR_StraTIvE_aSsiGNmenT_@2023";

        public WeatherController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _httpClient = _httpClientFactory.CreateClient();
        }

        #endregion


        #region All API's

        [HttpPost("GetAuthenticated")]
        public ActionResult<Token> GenerateJwtToken(string username, string password)
        {
            // Perform authentication logic and validate the username and password
            if (IsValidUser(username, password))
            {
                // Create claims for the user
                var claims = new[]
                {
            new Claim(ClaimTypes.Name, username)
        };

                // Generate JWT token
                var token = GenerateToken(claims);

                var tokenModel = new Token
                {
                    AccessToken = token,
                    Expires = DateTime.UtcNow.AddHours(1)
                };

                return Ok(tokenModel);
            }

            return Unauthorized();
        }

        [HttpGet("TemperatureForecasts")]
        public async Task<IActionResult> GetTemperatureForecasts([FromHeader(Name = "Authorization")] string accessToken)
        {
            bool isValidToken = ValidateAccessToken(accessToken);
            if (!isValidToken)
                return Unauthorized("Invalid access token.");

            var districts = await GetDistricts();
            if (districts == null)
                return NotFound("District data not available.");

            var forecasts = await GetWeatherForecasts(districts);

            return Ok(forecasts);
        }

        [HttpGet("CoolestDistricts")]
        public async Task<IActionResult> GetCoolestDistricts([FromHeader(Name = "Authorization")] string accessToken)
        {
            bool isValidToken = ValidateAccessToken(accessToken);
            if (!isValidToken)
                return Unauthorized("Invalid access token.");

            var districts = await GetDistricts();
            if (districts == null)
                return NotFound("District data not available.");

            var forecasts = await GetCoolestWeatherForecasts(districts);
            if (forecasts == null)
                return NotFound("Weather forecasts not available.");

            var coolestDistricts = forecasts
                .GroupBy(f => f.Name)
                .Select(g => new
                {
                    DistrictName = g.Key,
                    AverageTemperature = g.Average(f => f.Temperature)
                })
                .OrderBy(d => d.AverageTemperature)
                .Take(10)
                .ToList();

            return Ok(coolestDistricts);
        }

        [HttpGet("TravelSuggestion")]
        public async Task<IActionResult> TravelSuggestion([FromHeader(Name = "Authorization")] string accessToken, string friendLocation, string destination, DateTime travelDate)
        {
            bool isValidToken = ValidateAccessToken(accessToken);
            if (!isValidToken)
                return Unauthorized("Invalid access token.");

            var districts = await GetDistricts();
            if (districts == null)
                return NotFound("District data not available.");

            var friendForecast = await GetTravelWeatherForecasts(districts, friendLocation, travelDate);
            var destinationForecast = await GetTravelWeatherForecasts(districts, destination, travelDate);

            if (friendForecast == null || destinationForecast == null)
                return NotFound("Weather forecast not available for one or both locations.");

            var result = (friendForecast[0].Temperature < destinationForecast[0].Temperature)
                ? $"Your Friend's Location is Coolest, So You Should Travel to {friendLocation}"
                : $"Your Location is Coolest, So You Should Travel to {destination}";

            return Ok(result);
        }

        #endregion


        #region All Methods
        private async Task<List<District>> GetDistricts()
        {
            List<District> districts = new List<District>();

            try
            {
                using (var client = _httpClientFactory.CreateClient())
                {
                    HttpResponseMessage response = await client.GetAsync("https://raw.githubusercontent.com/strativ-dev/technical-screening-test/main/bd-districts.json");

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonText = await response.Content.ReadAsStringAsync();

                        JObject jsonObject = JObject.Parse(jsonText);
                        JArray districtArray = (JArray)jsonObject["districts"];

                        foreach (JToken districtToken in districtArray)
                        {
                            District district = new District();

                            district.Name = districtToken["name"].ToString();
                            district.Latitude = Convert.ToDouble(districtToken["lat"].ToString());
                            district.Longitude = Convert.ToDouble(districtToken["long"].ToString());

                            districts.Add(district);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return districts;


        }

        private async Task<Dictionary<DateTime, List<WeatherForecast>>> GetWeatherForecasts(List<District> districts)
        {
            var forecasts = new Dictionary<DateTime, List<WeatherForecast>>();

            foreach (var district in districts)
            {
                var response = await _httpClient.GetAsync($"{WeatherApiUrl}?latitude={district.Latitude}&longitude={district.Longitude}&current_weather={true}&forecast_days=7&hourly=temperature_2m");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    dynamic weatherData = JObject.Parse(json);

                    var hourlyData = weatherData.hourly;

                    var timeList = (JArray)hourlyData.time;
                    var temperatureList = (JArray)hourlyData.temperature_2m;

                    var getTwoPM = 14; // 2 PM

                    for (int i = 1; i <= 7; i++)
                    {
                        DateTime date = DateTime.Parse(timeList[getTwoPM].ToString());
                        double temperature = (double)temperatureList[getTwoPM];

                        getTwoPM = getTwoPM + 24;

                        WeatherForecast forecast = new WeatherForecast
                        {
                            Name = district.Name,
                            Date = date,
                            Temperature = temperature
                        };

                        if (forecasts.ContainsKey(date))
                        {
                            forecasts[date].Add(forecast);
                        }
                        else
                        {
                            forecasts[date] = new List<WeatherForecast> { forecast };
                        }
                    }
                }
            }

            return forecasts;
        }

        private async Task<List<WeatherForecast>> GetCoolestWeatherForecasts(List<District> districts)
        {
            var forecasts = new List<WeatherForecast>();

            foreach (var district in districts)
            {
                var response = await _httpClient.GetAsync($"{WeatherApiUrl}?latitude={district.Latitude}&longitude={district.Longitude}&current_weather={true}&forecast_days=7&hourly=temperature_2m");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    dynamic weatherData = JObject.Parse(json);

                    var hourlyData = weatherData.hourly;
                    var timeList = (JArray)hourlyData.time;
                    var temperatureList = (JArray)hourlyData.temperature_2m;

                    var getTwoPM = 14; // 2 PM

                    for (int i = 1; i <= 7; i++)
                    {
                        DateTime date = DateTime.Parse(timeList[getTwoPM].ToString());
                        double temperature = (double)temperatureList[getTwoPM];

                        getTwoPM += 24;

                        WeatherForecast forecast = new WeatherForecast
                        {
                            Name = district.Name,
                            Date = date,
                            Temperature = temperature
                        };

                        forecasts.Add(forecast);

                    }


                }
            }

            return forecasts;


        }

        private async Task<List<WeatherForecast>> GetTravelWeatherForecasts(List<District> districts, string location, DateTime travelDate)
        {
            double latitude = 0.0, longitude = 0.0;

            District result = districts.FirstOrDefault(d => d.Name == location);
            if (result != null)
            {
                latitude = result.Latitude;
                longitude = result.Longitude;
            }

            var forecasts = new List<WeatherForecast>();
            var response = await _httpClient.GetAsync($"{WeatherApiUrl}?latitude={latitude}&longitude={longitude}&hour=14&hourly=temperature_2m&date={travelDate}");


            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                dynamic weatherData = JObject.Parse(json);

                var hourlyData = weatherData.hourly;

                var timeList = (JArray)hourlyData.time;
                var temperatureList = (JArray)hourlyData.temperature_2m;

                var getTwoPM = 14; // 2 PM

                for (int i = 1; i <= 7; i++)
                {
                    DateTime date = DateTime.Parse(timeList[getTwoPM].ToString());
                    double temperature = (double)temperatureList[getTwoPM];

                    getTwoPM = getTwoPM + 24;

                    WeatherForecast forecast = new WeatherForecast
                    {
                        Name = location,
                        Date = date,
                        Temperature = temperature
                    };

                    forecasts.Add(forecast);
                }
            }

            var desiredForecasts = forecasts.Where(forecast => forecast.Date.Date == travelDate.Date).ToList();

            return desiredForecasts;

        }

        private string GenerateToken(Claim[] claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "your_issuer",
                audience: "your_audience",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool IsValidUser(string username, string password)
        {
            return username == "raskin" && password == "123";
        }

        private bool ValidateAccessToken(string accessToken)
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = "your_issuer",
                ValidAudience = "your_audience",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey))
            };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(accessToken, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}

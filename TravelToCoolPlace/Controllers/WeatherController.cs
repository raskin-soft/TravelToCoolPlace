using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using TravelToCoolPlace.Models;

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

        public WeatherController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _httpClient = _httpClientFactory.CreateClient();
        }
        #endregion


        #region API's
        [HttpGet("TemperatureForecasts")]
        public async Task<IActionResult> GetTemperatureForecasts()
        {
            var districts = await GetDistricts();
            if (districts == null)
                return NotFound("District data not available.");

            var forecasts = await GetWeatherForecasts(districts);

            return Ok(forecasts);
        }

        [HttpGet("CoolestDistricts")]
        public async Task<IActionResult> GetCoolestDistricts()
        {
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
        #endregion


        #region Methods
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

        #endregion
    }
}

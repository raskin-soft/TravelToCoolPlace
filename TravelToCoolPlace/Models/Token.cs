using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace TravelToCoolPlace.Models
{
    [SwaggerSchema("Token")]
    public class Token
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }
        public DateTime Expires { get; set; }
    }
}

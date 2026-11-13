using System.Text.Json.Serialization;

namespace Server.Models;

class NetworkPackage
{
    [JsonPropertyName("type")]
    public string Type = string.Empty;
}

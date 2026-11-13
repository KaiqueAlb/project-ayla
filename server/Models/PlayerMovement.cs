using System.Text.Json.Serialization;

namespace Server.Models;

class PlayerMovement: NetworkPackage
{
    [JsonPropertyName("x")]
    public float X;
    [JsonPropertyName("y")]
    public float Y;
    [JsonPropertyName("delta")]
    public float delta;
}

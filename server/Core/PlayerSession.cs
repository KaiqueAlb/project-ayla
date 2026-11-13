using System.Net.Sockets;

namespace Server.Core;

class PlayerSession
{
    public string Id;
    public TcpClient Client;
    public NetworkStream Stream;

    public float X;
    public float Y;

    public PlayerSession(string id, TcpClient client)
    {
        Id = id;
        Client = client;
        Stream = client.GetStream();
        X = 0f;
        Y = 0f;
    }
}

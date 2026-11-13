using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Server.Core;
using Server.Models;

namespace Server.Services;

class TcpServerService : BackgroundService
{
    private readonly ILogger<TcpServerService> _logger;
    private const int Port = 8080;

    private readonly ConcurrentDictionary<string, PlayerSession> _session = new ();

    public TcpServerService(ILogger<TcpServerService> logger)
    {
        this._logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        _logger.LogInformation("[TPC SERVICE] Serviço iniciado no IP:{Ip} Porta:{port}", IPAddress.Any, Port);

        cancellationToken.Register(() => listener.Stop());

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
                _logger.LogInformation("[TCP SERVICE] Cliente conectado! {EndPoint}", client.Client.RemoteEndPoint);

                _ = HandleClient(client, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError("[TCP SERVICE] Erro ao aceitar conexão: {error}", ex.Message);
            }
        }
    }

    private async Task HandleClient(TcpClient client, CancellationToken cancellationToken)
    {
        string id = Guid.NewGuid().ToString("N")[..8];
        var session = new PlayerSession(id, client);
        _session.TryAdd(id, session);

        _logger.LogInformation("[TPC SERVICE] Cliente de Id {Id} conectado. Total de conectado {Count}.", id, _session.Count);

        byte[] buffer = new byte[1024];

        try
        {
            var welcome = new { type = "welcome", id = id };
            await SendToSessinAsync(session, welcome, cancellationToken);

            int bytesReads;
            while ((bytesReads = await session.Stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                string payload = Encoding.UTF8.GetString(buffer, 0, bytesReads);
                _logger.LogInformation("[TCP SERVICE] Mensagem recebida do cliente: {Payload}", payload);

                string[] mensages = payload.Split("\n", StringSplitOptions.RemoveEmptyEntries);

                foreach(var json in mensages)
                {
                    await ProcessMensageAsync(session, json, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("[ERROR TCP SERVICE] Erro na conexão com o cliente: {error}", ex.Message);
        }
        finally
        {
            client.Close();
            _logger.LogInformation("[TCP SERVICE] Conexão encerrada com o cliente, cliente desconectado");
        }

    }

    private async Task SendToSessinAsync(PlayerSession session, object payload, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(payload) + "\n";
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await session.Stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
    }

    private async Task ProcessMensageAsync(PlayerSession sender, string jsonString, CancellationToken cancellationToken)
    {
        try
        {
            var basePkg = JsonSerializer.Deserialize<NetworkPackage>(jsonString);
            if (basePkg == null) return;

            switch (basePkg.Type)
            {
                case "player_move":
                    var movePkg = JsonSerializer.Deserialize<PlayerMovement>(jsonString);
                    if (movePkg == null) return;

                    var broadcastPkg = new
                    {
                        type = "player_moved",
                        id = sender.Id,
                        x = sender.X,
                        y = sender.Y,
                    };

                    await BroadcastSendAsync(broadcastPkg, sender.Id, cancellationToken);
                    break;
                case null:
                    _logger.LogError("[TCP SERVER] Erro tipo invalido de pacote invalido ou indisponivel.");
                    break;

            }
        }
        catch (JsonException ex)
        {
            _logger.LogError("[TCP SERVER] Error ao ler o pacote enviado por: {sender}. Error: {error}", sender.Id, ex.Message);
        }
    }

    private async Task BroadcastSendAsync(object package, string? exculdeId, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(package);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        foreach(var player in _session.Values)
        {
            if (exculdeId == player.Id) continue;

            try
            {
                await player.Stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            }
            catch
            {

            }
        }
    }
}

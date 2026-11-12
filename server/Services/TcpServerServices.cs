using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Server.Services;

class TcpServerService : BackgroundService
{
    private readonly ILogger<TcpServerService> _logger;
    private const int Port = 8080;

    public TcpServerService(ILogger<TcpServerService> logger)
    {
        this._logger = logger;
        _logger.LogInformation("");
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        _logger.LogInformation("[TPC SERVICE] Serviço iniciado {port}:", Port);
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
                _logger.LogError("[TCP SERVICE] Erro ao aceitar conexão: " + ex.Message);
            }
        }
    }

    private async Task HandleClient(TcpClient client, CancellationToken cancellationToken)
    {
        using NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            int bytesReads;
            while ((bytesReads = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                string payload = Encoding.UTF8.GetString(buffer, 0, bytesReads);
                _logger.LogInformation("[TCP SERVICE] Mensagem recebida do cliente", payload);

                string ackMessage = $"{{\"status\":\"OK\",\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
                byte[] ackBytes = Encoding.UTF8.GetBytes(ackMessage);

                await stream.WriteAsync(ackBytes, 0, ackBytes.Length, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("[ERROR TCP SERVICE] Erro na conexão com o cliente:", ex.Message);
        }
        finally
        {
            client.Close();
            _logger.LogInformation("[TCP SERVICE] Conexão encerrada com o cliente, cliente desconectado");
        }

    }
}

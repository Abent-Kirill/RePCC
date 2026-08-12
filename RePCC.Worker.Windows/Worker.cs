using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RePCC.Worker.Windows;

public class Worker(ILogger<Worker> logger) : BackgroundService, IDisposable
{
    private const int _udpPort = 8888;  // Порт для обнаружения ПК
    private const int _httpPort = 8889; // Порт для команд выключения
    private HttpListener? _httpListener;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Запускаем два параллельных потока: обнаружение и управление
        var udpTask = StartUdpDiscoveryAsync(stoppingToken);
        var httpTask = StartHttpServerAsync(stoppingToken);

        await Task.WhenAll(udpTask, httpTask);
    }

    /// <summary>
    /// Вещание доступности (UDP-ответчик)
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    private async Task StartUdpDiscoveryAsync(CancellationToken stoppingToken)
    {
        using var udpClient = new UdpClient(_udpPort);
        logger.LogInformation("UDP-сервер обнаружения запущен на порту {Port}...", _udpPort);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await udpClient.ReceiveAsync(stoppingToken);
                var message = Encoding.UTF8.GetString(result.Buffer);

                if (message == "DISCOVER_PC_SERVICE")
                {
                    var responseData = Encoding.UTF8.GetBytes($"PC_AVAILABLE:{Environment.MachineName}");
                    await udpClient.SendAsync(responseData, responseData.Length, result.RemoteEndPoint);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Ошибка в UDP-сервере");
            }
        }
    }

    /// <summary>
    /// Прием команды на выключение (HTTP-сервер)
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    private async Task StartHttpServerAsync(CancellationToken stoppingToken)
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://*:{_httpPort}/");
        _httpListener.Start();
        logger.LogInformation("HTTP-сервер управления запущен на порту {Port}...", _httpPort);

        // Закрываем listener при отмене токена
        using var registration = stoppingToken.Register(() => _httpListener.Stop());

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                var context = await _httpListener.GetContextAsync();
                _ = Task.Run(() => HandleHttpRequestAsync(context), stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Ошибка в HTTP-сервере");
            }
    }

    private async Task HandleHttpRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        using var response = context.Response;

        if (request.Url?.AbsolutePath == "/shutdown" && request.HttpMethod == "POST")
        {
            logger.LogWarning("Получена команда на выключение ПК!");

            var buffer = Encoding.UTF8.GetBytes("Shutting down...");
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            ShutdownComputer();
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
        }
    }

    private static void ShutdownComputer()
    {
        var psi = new ProcessStartInfo("shutdown", "/s /t 5 /f")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };
        Process.Start(psi);
    }

    public override void Dispose()
    {
        _httpListener?.Close();
        base.Dispose();
    }
}


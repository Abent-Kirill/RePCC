using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace RePCC.Worker.Windows;
//Так как служба обычно устанавливается через PowerShell-скрипт или installer от имени администратора, добавь регистрацию URLACL прямо в скрипт развертывания:

//PowerShell
//# PowerShell (Запускать от Администратора)

//$serviceName = "RePCCWorker"
//$exePath = "C:\Services\RePCC\RePCC.Worker.Windows.exe"
//$port = 5000

//# 1. Регистрируем URLACL для порта
//netsh http add urlacl url="http://+:$port/" user="NT AUTHORITY\Local Service"

//# 2. Создаем службу
//New-Service -Name $serviceName -BinaryPathName $exePath -StartupType Automatic -Credential "NT AUTHORITY\Local Service"

//# 3. Запускаем службу
//Start-Service -Name $serviceName
public sealed class Worker(ILogger<Worker> logger) : BackgroundService, IDisposable
{
    /// <summary>
    /// Порт для обнаружения ПК
    /// </summary>
    private const int _udpPort = 8888;
    /// <summary>
    /// Порт для команд выключения
    /// </summary>
    private const int _httpPort = 8889;
    private HttpListener? _httpListener;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Запуск фоновых служб UDP и HTTP...");

            var udpTask = StartUdpDiscoveryAsync(stoppingToken);
            var httpTask = StartHttpServerAsync(stoppingToken);

            await Task.WhenAll(udpTask, httpTask);
        }
        catch (Exception ex)
        {
            // Если что-то упадет при старте сокетов/HTTP, мы увидим это в логах/EventViewer
            logger.LogCritical(ex, "Критическая ошибка при работе фоновых задач службы");
            throw; // Перевыбрасываем, чтобы SCM зафиксировал остановку
        }
    }

    /// <summary>
    /// Вещание доступности (UDP-ответчик)
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    private async Task StartUdpDiscoveryAsync(CancellationToken stoppingToken)
    {
        using var udpClient = new UdpClient(_udpPort);
        logger.LogInformation("UDP-сервер обнаружения запущен...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await udpClient.ReceiveAsync(stoppingToken);
                var message = Encoding.UTF8.GetString(result.Buffer);

                if (message == "DISCOVER_PC_SERVICE")
                {
                    var macAddress = GetMacAddress();
                    var responseData = Encoding.UTF8.GetBytes($"PC_AVAILABLE:{Environment.UserName};{macAddress}");
                    await udpClient.SendAsync(responseData, responseData.Length, result.RemoteEndPoint);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Ошибка в UDP-сервере");
            }
        }
    }

    private static string GetMacAddress()
    {
        var mac = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                          nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault();

        return mac ?? "000000000000";
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


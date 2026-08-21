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
public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    private const int _udpPort = 8888;
    private const int _httpPort = 8889;
    private HttpListener? _httpListener;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Запуск фоновых служб UDP и HTTP...");

        var udpTask = StartUdpDiscoveryAsync(cancellationToken);
        var httpTask = StartHttpServerAsync(cancellationToken);

        await Task.WhenAll(udpTask, httpTask);
    }

    /// <summary>
    /// Вещание доступности (UDP-ответчик)
    /// </summary>
    private async Task StartUdpDiscoveryAsync(CancellationToken cancellationToken)
    {
        using var udpClient = new UdpClient(_udpPort);
        logger.LogInformation("UDP-сервер обнаружения запущен на порту {Port}...", _udpPort);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await udpClient.ReceiveAsync(cancellationToken);
                var message = Encoding.UTF8.GetString(result.Buffer);

                if (message == "DISCOVER_PC_SERVICE")
                {
                    var macAddress = GetMacAddress();
                    if (string.IsNullOrEmpty(macAddress))
                    {
                        logger.LogWarning("Не удалось определить MAC-адрес ПК. Ответ на UDP-запрос пропущен.");
                        continue;
                    }

                    var responseData = Encoding.UTF8.GetBytes($"PC_AVAILABLE:{Environment.UserName};{macAddress}");
                    await udpClient.SendAsync(responseData, result.RemoteEndPoint, cancellationToken);

                    logger.LogInformation("Отправлен UDP-ответ устройству {EndPoint}", result.RemoteEndPoint);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обработке UDP-запроса");
            }
        }
    }

    private static string? GetMacAddress()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                          nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault(mac => !string.IsNullOrEmpty(mac));
    }

    /// <summary>
    /// Прием команды на выключение (HTTP-сервер)
    /// </summary>
    private async Task StartHttpServerAsync(CancellationToken cancellationToken)
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://*:{_httpPort}/");

        try
        {
            _httpListener.Start();
            logger.LogInformation("HTTP-сервер управления запущен на порту {Port}...", _httpPort);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Не удалось запустить HTTP-listener. Возможно, порт занят или нужны права Администратора.");
            return;
        }

        using var registration = cancellationToken.Register(() =>
        {
            try { _httpListener.Stop(); } catch { }
        });

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                _ = HandleHttpRequestAsync(context, cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                // Ошибка вызовется искусственно методом _httpListener.Stop() при закрытии службы. Это норма.
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в цикле приема HTTP-запросов");
            }
        }
    }

    private async Task HandleHttpRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        using var response = context.Response;

        if (request.Url?.AbsolutePath != "/shutdown" || request.HttpMethod != "POST")
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        logger.LogWarning("Получена валидная команда на выключение ПК от {RemoteEndPoint}!", request.RemoteEndPoint);

        try
        {
            var buffer = Encoding.UTF8.GetBytes("Shutting down...");
            response.ContentLength64 = buffer.Length;
            response.StatusCode = (int)HttpStatusCode.OK;

            await response.OutputStream.WriteAsync(buffer, cancellationToken);
            response.OutputStream.Close();

            ShutdownComputer();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка отправки HTTP-ответа клиенту");
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
        try { _httpListener?.Close(); } catch { }
        base.Dispose();
    }
}


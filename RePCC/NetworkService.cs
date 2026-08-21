using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using RePCC.Models;

namespace RePCC;

public static class NetworkService
{
    /// <summary>
    /// Порт для обнаружения ПК
    /// </summary>
    private const int _udpPort = 8888;

    public static async Task<IReadOnlyCollection<Computer>> ScanLocalNetworkAsync(CancellationToken cancellationToken = default)
    {
        var computers = new List<Computer>();
        using var udpClient = new UdpClient();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(3));
        var token = cts.Token;

        var requestData = Encoding.UTF8.GetBytes("DISCOVER_PC_SERVICE");
        var targetEndPoint = new IPEndPoint(IPAddress.Broadcast, _udpPort);

        token.ThrowIfCancellationRequested();
        await udpClient.SendAsync(requestData, requestData.Length, targetEndPoint);

        while (!token.IsCancellationRequested)
        {
            try
            {
                var result = await udpClient.ReceiveAsync(token);
                var responseMessage = Encoding.UTF8.GetString(result.Buffer);

                if (responseMessage.StartsWith("PC_AVAILABLE:"))
                {
                    // Отрезаем префикс "PC_AVAILABLE:" -> "ИмяПК;MAC-Адрес"
                    var dataPayload = responseMessage.Replace("PC_AVAILABLE:", "");

                    // Разделяем строку по точке с запятой
                    var parts = dataPayload.Split(';');
                    if (parts.Length >= 2)
                    {
                        var pcName = parts[0];
                        var macAddress = parts[1];
                        var pcIp = result.RemoteEndPoint.Address;
                        if (PhysicalAddress.TryParse(macAddress, out var MACAddress))
                        {
                            if (!computers.Any(c => c.IPAddress!.Equals(pcIp)))
                            {
                                var computer = new Computer(pcName, MACAddress, pcIp);
                                computers.Add(computer);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка приема UDP: {ex.Message}");
            }
        }
        return computers;
    }
}

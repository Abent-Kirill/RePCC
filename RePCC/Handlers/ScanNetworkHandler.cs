using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using MediatR;
using RePCC.Requests;

namespace RePCC.Handlers;

internal sealed record ScanNetworkHandler : IRequestHandler<ScanNetworkRequest, IEnumerable<Computer>>
{
    /// <summary>
    /// Порт для обнаружения ПК
    /// </summary>
    private const int _udpPort = 8888;

    public async Task<IEnumerable<Computer>> Handle(ScanNetworkRequest request, CancellationToken cancellationToken)
    {
        var computers = new List<Computer>();
        using var udpClient = new UdpClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        var requestData = Encoding.UTF8.GetBytes("DISCOVER_PC_SERVICE");
        var targetEndPoint = new IPEndPoint(IPAddress.Broadcast, _udpPort);
        await udpClient.SendAsync(requestData, requestData.Length, targetEndPoint);

        while (!cts.IsCancellationRequested)
        {
            try
            {
                var result = await udpClient.ReceiveAsync(cts.Token);
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
                        var macAddress = parts[1]; // Получили наш MAC!
                        var pcIp = result.RemoteEndPoint.Address;
                        if (PhysicalAddress.TryParse(macAddress, out var MACAddress))
                        {
                            if (!computers.Any(c => c.IPAddress.Equals(pcIp)))
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

using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace RePCC;

internal sealed class ScanWorker(UdpClient udpClient) : BackgroundService
{
    public async IEnumerable<Computer> Execute()
    {
        udpClient.EnableBroadcast = true;
        udpClient.Client.ReceiveTimeout = 3000;

        // 1. Отправляем UDP-запрос на поиск ПК
        var requestData = Encoding.UTF8.GetBytes("DISCOVER_PC_SERVICE");
        var targetEndPoint = new IPEndPoint(IPAddress.Broadcast, 8888);
        await udpClient.SendAsync(requestData, requestData.Length, targetEndPoint);

        // 2. Ждем ответ от службы Windows
        var result = await udpClient.ReceiveAsync();
        var responseMessage = Encoding.UTF8.GetString(result.Buffer);

        if (responseMessage.StartsWith("PC_AVAILABLE:"))
        {
            var pcName = responseMessage.Replace("PC_AVAILABLE:", "");
            var pcIp = result.RemoteEndPoint.Address.ToString();

            Console.WriteLine($"Найдена служба на ПК: {pcName} (IP: {pcIp})");
            Console.Write("Выключить этот компьютер? (y/n): ");

            if (Console.ReadLine()?.ToLower() == "y")
            {
                // 3. Отправляем HTTP POST запрос на выключение
                using var httpClient = new HttpClient();
                var httpResponse = await httpClient.PostAsync($"http://{pcIp}:8889/shutdown", null);

                if (httpResponse.IsSuccessStatusCode)
                    Console.WriteLine("Сигнал на выключение успешно доставлен!");
                else
                    Console.WriteLine($"Ошибка сервера: {httpResponse.StatusCode}");
            }
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => throw new NotImplementedException();
}

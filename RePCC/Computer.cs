using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RePCC;

public sealed record class Computer(string Name, PhysicalAddress MACAddress, IPAddress IPAddress)
{
    /// <summary>
    /// Порт для команд выключения
    /// </summary>
    private const int _httpPort = 8889;

    private readonly Uri _baseHttpUrl = new($"http://{IPAddress}:{_httpPort}", UriKind.Absolute);

    public bool IsOnline { get; private set; }

    /// <summary>
    /// Команда на включение
    /// </summary>
    /// <exception cref="SocketException"/>
    /// <exception cref="InvalidOperationException"/>
    public async Task TurnOnAsync()
    {
        var macBytes = MACAddress.GetAddressBytes();
        var packet = new byte[6 + (16 * macBytes.Length)]; // Магический пакет: 6 байт 0xFF + 16 раз повторенный MAC-адрес
        Array.Fill(packet, (byte)0xFF, 0, 6); // Быстрое заполнение первых 6 байт

        for (uint i = 0; i < 16; i++)
            Array.Copy(macBytes, 0, packet, 6 + (i * macBytes.Length), macBytes.Length);

        using var client = new UdpClient(new IPEndPoint(IPAddress.Broadcast, 9));
        client.EnableBroadcast = true;
        await client.SendAsync(packet, packet.Length);
        IsOnline = true;
    }

    /// <summary>
    /// Команда на выключение
    /// </summary>
    /// <exception cref="NotImplementedException"/>
    public async Task TurnOffAsync(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient()
        {
            BaseAddress = _baseHttpUrl,
            Timeout = TimeSpan.FromSeconds(5)
        };

        var httpResponse = await httpClient.PostAsync(new Uri("shutdown", UriKind.Relative), null, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();
        IsOnline = false;
    }
}

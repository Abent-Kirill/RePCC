using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RePCC;

internal sealed record class Computer(string Name, bool Status,
    PhysicalAddress MACAddress, IPAddress IPAddress)
{
    /// <summary>
    /// Команда на включение
    /// </summary>
    /// <exception cref="SocketException"/>
    /// <exception cref="InvalidOperationException"/>
    public void TurnOn()
    {
        var macBytes = MACAddress.GetAddressBytes();
        var packet = new byte[6 + (16 * macBytes.Length)]; // Магический пакет: 6 байт 0xFF + 16 раз повторенный MAC-адрес
        Array.Fill(packet, (byte)0xFF, 0, 6); // Быстрое заполнение первых 6 байт

        for (uint i = 0; i < 16; i++)
            Array.Copy(macBytes, 0, packet, 6 + (i * macBytes.Length), macBytes.Length);

        using var client = new UdpClient();
        client.EnableBroadcast = true;
        var endPoint = new IPEndPoint(IPAddress.Broadcast, 9);
        client.Send(packet, packet.Length, endPoint);
    }

    /// <summary>
    /// Команда на выключение
    /// </summary>
    /// <exception cref="NotImplementedException"/>
    public async void TurnOff()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.PostAsync("http://192.168.1", null);
        response.EnsureSuccessStatusCode();
    }
}

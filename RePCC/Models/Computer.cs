using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RePCC.Models;

public sealed record class Computer(string Name, PhysicalAddress MACAddress, IPAddress? IPAddress)
{
    private const int _httpPort = 8889;

    public bool IsOnline { get; set; } = true;

    /// <summary>
    /// Команда на включение (WOL)
    /// </summary>
    public async Task TurnOnAsync(CancellationToken cancellationToken = default)
    {
#if ANDROID
        var context = Android.App.Application.Context;
        var wifiManager = (Android.Net.Wifi.WifiManager?)context.GetSystemService(Android.Content.Context.WifiService);

        using var multicastLock = wifiManager?.CreateMulticastLock("WolMulticastLock");
        multicastLock?.Acquire();
#endif
        try
        {
            var macBytes = MACAddress.GetAddressBytes();
            var packet = new byte[6 + (16 * macBytes.Length)];
            Array.Fill(packet, (byte)0xFF, 0, 6);

            for (uint i = 0; i < 16; i++)
            {
                Array.Copy(macBytes, 0, packet, 6 + (i * macBytes.Length), macBytes.Length);
            }

            using var client = new UdpClient();
            client.EnableBroadcast = true;

            var targetEndpoint = new IPEndPoint(IPAddress.Broadcast, 9);

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendAsync(packet, packet.Length, targetEndpoint);

            IsOnline = true;
        }
        finally
        {
#if ANDROID
            if (multicastLock is { IsHeld: true })
            {
                multicastLock.Release();
            }
#endif
        }
    }

    /// <summary>
    /// Команда на выключение
    /// </summary>
    public async Task TurnOffAsync(CancellationToken cancellationToken = default)
    {
        if (IPAddress is null || IPAddress.Equals(IPAddress.Any) || IPAddress.Equals(IPAddress.Loopback))
        {
            throw new InvalidOperationException($"Невалидный IP-адрес для выключения: {IPAddress}");
        }

        var requestUri = new Uri($"http://{IPAddress}:{_httpPort}/shutdown", UriKind.Absolute);

        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        var response = await httpClient.PostAsync(requestUri, null, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            IsOnline = false;
            return;
        }
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"Ошибка выключения ПК ({response.StatusCode}): {errorContent}");
    }
}

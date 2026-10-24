using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using QRCoder;

namespace FamilyFeudLive.Services;

public class QrCodeService
{
    public List<string> GetLocalIPv4Addresses()
    {
        var ips = new List<string>();
        try
        {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.OperationalStatus == OperationalStatus.Up &&
                    iface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    foreach (var addr in iface.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(addr.Address))
                        {
                            ips.Add(addr.Address.ToString());
                        }
                    }
                }
            }
        }
        catch
        {
            // fallback if network interfaces query fails
        }

        if (ips.Count == 0)
        {
            ips.Add("localhost");
        }
        return ips;
    }

    public string ResolveHostIp(string currentUri)
    {
        try
        {
            var uri = new Uri(currentUri);
            if (!uri.IsLoopback && !string.IsNullOrEmpty(uri.Host))
            {
                return uri.Host;
            }

            var ips = GetLocalIPv4Addresses();
            return ips.FirstOrDefault(ip => ip != "localhost" && ip != "127.0.0.1") ?? "localhost";
        }
        catch
        {
            return "localhost";
        }
    }

    public int ResolvePort(string currentUri)
    {
        try
        {
            var uri = new Uri(currentUri);
            return (uri.Port == 7267) ? 5046 : (uri.Port > 0 ? uri.Port : 5046);
        }
        catch
        {
            return 5046;
        }
    }

    public string BuildBuzzerUrl(string currentUri, string? team = null)
    {
        var host = ResolveHostIp(currentUri);
        var port = ResolvePort(currentUri);
        var baseUrl = $"http://{host}:{port}/buzzer";
        if (!string.IsNullOrEmpty(team))
        {
            return $"{baseUrl}?team={Uri.EscapeDataString(team)}";
        }
        return baseUrl;
    }

    public string GenerateSvgQrCode(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new SvgQRCode(qrCodeData);
        // SvgQRCode generates pure SVG xml
        return qrCode.GetGraphic(10);
    }
}

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

    public string GenerateSvgQrCode(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new SvgQRCode(qrCodeData);
        // SvgQRCode generates pure SVG xml
        return qrCode.GetGraphic(10);
    }
}

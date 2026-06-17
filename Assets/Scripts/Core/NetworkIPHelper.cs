using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// Helper klasa za dohvaćanje IP adrese
public static class NetworkIPHelper
{
    /// Dohvati lokalnu IP adresu ovog računala
    public static string GetLocalIPAddress()
    {
        try
        {
            // Koristi DNS za dohvaćanje host name-a i IP adresa
            string hostName = Dns.GetHostName();
            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);

            // Pronađi IPv4 adresu
            foreach (IPAddress ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    string ipAddress = ip.ToString();
                    // Preskoči loopback adresu
                    if (!ipAddress.StartsWith("127."))
                    {
                        return ipAddress;
                    }
                }
            }

            // Ako nije pronađena LAN adresa, vrati localhost
            return "127.0.0.1";
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to get local IP address: {e.Message}");
            return "127.0.0.1";
        }
    }

    /// Provjeri je li IP adresa validna
    public static bool IsValidIPAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return false;

        return IPAddress.TryParse(ipAddress, out _);
    }
}
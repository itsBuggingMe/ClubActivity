using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClubActivity;
internal class Server
{
    public static Server Instance { get; } = new();
    private int _port;
    private TcpListener _listener;
    private List<TcpClient> _clients = new List<TcpClient>();
    private ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
    private IPAddress _ip;
    public ConcurrentQueue<string> Messages => messages;
    private Server()
    {
        _port = 8996;
        _listener = new TcpListener(_ip = (GetLocalIPAddress() ?? throw new Exception()), _port);
    }

    public void Run()
    {
        _listener.Start();
        Console.WriteLine(_ip.ToString());
        Task.Run(() =>
        {
            for (;;)
            {
                TcpClient client;
                _clients.Add(client = _listener.AcceptTcpClient());
                Task.Run(() =>
                {
                    using (StreamReader r = new(client.GetStream()))
                    {
                        messages.Enqueue(r.ReadToEnd());
                    }
                });
            }
        });
    }

    static IPAddress? GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip;
            }
        }
        return null;
    }
}

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace P2PChat
{
    public class ChatApp
    {
        private readonly User _localUser;
        private readonly PeerManager _peers = new();
        private readonly int _listenPort;
        private readonly int _udpPort = 5001;
        private UdpDiscoveryService? _discovery;

        public ChatApp(string name, int port)
        {
            _localUser = new User { Name = name };
            _listenPort = port;
        }

        public async Task RunAsync()
        {
            var listenerService = new TcpListenerService(_listenPort, _peers, _localUser);
            _ = listenerService.StartAsync();

            _discovery = new UdpDiscoveryService(_udpPort, _listenPort, _localUser, _peers);
            _discovery.Start();

            Console.WriteLine("Commands:");
            Console.WriteLine("/connect ip:port  - Connect to a peer manually");
            Console.WriteLine("/peers            - List connected peers");
            Console.WriteLine("/quit             - Exit");
            Console.WriteLine("Type your message and press Enter to broadcast.\n");

            while (true)
            {
                var line = Console.ReadLine();
                if (line == null) continue;

                if (line.StartsWith("/"))
                {
                    await HandleCommand(line);
                }
                else
                {
                    var msg = new ChatMessage(_localUser, line);
                    await _peers.BroadcastAsync(msg);
                }
            }
        }

        private async Task HandleCommand(string line)
        {
            var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();

            switch (cmd)
            {
                case "/quit":
                    Environment.Exit(0);
                    break;
                case "/peers":
                    _peers.ListPeers();
                    break;
                case "/connect":
                    if (parts.Length < 2) { Console.WriteLine("Usage: /connect ip:port"); break; }
                    await ConnectToPeer(parts[1]);
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }

        private async Task ConnectToPeer(string ipPort)
        {
            try
            {
                var tokens = ipPort.Split(':');
                string ip = tokens[0];
                int port = tokens.Length > 1 ? int.Parse(tokens[1]) : _listenPort;

                var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Parse(ip), port);

                var peerUser = new User { EndPoint = $"{ip}:{port}" };
                var connection = new PeerConnection(peerUser, client);
                await connection.InitAsync(_localUser.Name); // TCP handshake
                _peers.AddPeer(connection);

                // Start receiving messages
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        var msg = await connection.ReceiveAsync();
                        if (msg == null) break;
                        Console.WriteLine(msg);
                    }
                    _peers.RemovePeer(peerUser.Id);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
            }
        }
    }
}

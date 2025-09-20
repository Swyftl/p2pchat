using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace P2PChat
{
    public class TcpListenerService
    {
        private readonly int _port;
        private readonly PeerManager _peerManager;
        private readonly User _localUser;

        public TcpListenerService(int port, PeerManager manager, User localUser)
        {
            _port = port;
            _peerManager = manager;
            _localUser = localUser;
        }

        public async Task StartAsync()
        {
            var listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            Console.WriteLine($"Listening for peers on port {_port}...");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
                var peerUser = new User { Name = _localUser.Name, EndPoint = endpoint };
                var connection = new PeerConnection(peerUser, client);

                // handshake
                await connection.InitAsync();

                _peerManager.AddPeer(connection);

                // receive loop
                while (true)
                {
                    var msg = await connection.ReceiveAsync();
                    if (msg == null) break;
                    Console.WriteLine(msg);
                }

                _peerManager.RemovePeer(peerUser.Id);
            }
            catch { }
        }
    }
}
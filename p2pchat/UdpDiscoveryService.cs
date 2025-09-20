using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2PChat
{
    public class UdpDiscoveryService
    {
        private readonly int _udpPort;
        private readonly int _tcpPort;
        private readonly User _localUser;
        private readonly PeerManager _peerManager;
        private CancellationTokenSource _cts = new();

        public UdpDiscoveryService(int udpPort, int tcpPort, User localUser, PeerManager peerManager)
        {
            _udpPort = udpPort;
            _tcpPort = tcpPort;
            _localUser = localUser;
            _peerManager = peerManager;
        }

        public void Start()
        {
            _ = Task.Run(() => BroadcastLoop(_cts.Token));
            _ = Task.Run(() => ListenLoop(_cts.Token));
        }

        public void Stop() => _cts.Cancel();

        private async Task BroadcastLoop(CancellationToken ct)
        {
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var msg = $"P2PCHAT|{_tcpPort}|{_localUser.Id}|{_localUser.Name}";
                    var data = Encoding.UTF8.GetBytes(msg);
                    await udp.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, _udpPort));
                }
                catch { }
                await Task.Delay(5000, ct);
            }
        }

        private async Task ListenLoop(CancellationToken ct)
        {
            var udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, _udpPort));

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = await udp.ReceiveAsync(ct);
                    var text = Encoding.UTF8.GetString(result.Buffer);
                    if (!text.StartsWith("P2PCHAT|")) continue;

                    var parts = text.Split('|');
                    if (parts.Length < 4) continue;

                    int peerTcpPort = int.Parse(parts[1]);
                    string peerId = parts[2];
                    string peerName = parts[3];

                    // ignore self
                    if (peerId == _localUser.Id) continue;

                    string peerIp = result.RemoteEndPoint.Address.ToString();

                    // ignore if already connected
                    if (_peerManager.HasPeer(peerId)) continue;

                    // Only connect if local ID is smaller
                    if (string.Compare(_localUser.Id, peerId, StringComparison.Ordinal) > 0)
                        continue;

                    // connect via TCP
                    _ = Task.Run(async () =>
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            try
                            {
                                var client = new TcpClient();
                                await client.ConnectAsync(peerIp, peerTcpPort);

                                var peerUser = new User { Id = peerId, Name = peerName, EndPoint = $"{peerIp}:{peerTcpPort}" };
                                var connection = new PeerConnection(peerUser, client);
                                await connection.InitAsync(); // handshake

                                _peerManager.AddPeer(connection);

                                // receive loop
                                while (true)
                                {
                                    var msg = await connection.ReceiveAsync();
                                    if (msg == null) break;
                                    Console.WriteLine(msg);
                                }

                                _peerManager.RemovePeer(peerId);
                                break; // connected
                            }
                            catch
                            {
                                await Task.Delay(1000);
                            }
                        }
                    });
                }
                catch { }
            }
        }
    }
}

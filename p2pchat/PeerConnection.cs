using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2PChat
{
    public class PeerConnection
    {
        public User Peer { get; }
        private readonly TcpClient _client;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        public PeerConnection(User peer, TcpClient client)
        {
            Peer = peer;
            _client = client;

            var ns = _client.GetStream();
            _reader = new StreamReader(ns, Encoding.UTF8);
            _writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };
        }

        // Simple handshake: exchange names
        public async Task InitAsync()
        {
            // Send local name
            await _writer.WriteLineAsync(Peer.Name);

            // Read remote name
            var remoteName = await _reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(remoteName))
            {
                Peer.Name = remoteName.Trim();
            }
        }

        public async Task SendAsync(string message)
        {
            await _writer.WriteLineAsync(message);
        }

        public async Task<string?> ReceiveAsync()
        {
            return await _reader.ReadLineAsync();
        }

        public void Close() => _client.Close();
    }
}
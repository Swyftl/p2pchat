using System;
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

        // Event triggered when the peer disconnects
        public event Action<PeerConnection>? OnDisconnected;

        public PeerConnection(User peer, TcpClient client)
        {
            Peer = peer;
            _client = client;

            var ns = _client.GetStream();
            _reader = new StreamReader(ns, Encoding.UTF8);
            _writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };
        }

        // TCP handshake: send local name, receive remote name
        public async Task InitAsync(string localName)
        {
            // send local name
            await _writer.WriteLineAsync(localName);

            // read remote name
            var remoteName = await _reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(remoteName))
                Peer.Name = remoteName.Trim();
        }

        // Send a message to this peer
        public async Task SendAsync(string message)
        {
            try
            {
                await _writer.WriteLineAsync(message);
            }
            catch
            {
                TriggerDisconnect();
            }
        }

        // Receive a message; triggers OnDisconnected if stream closes
        public async Task<string?> ReceiveAsync()
        {
            try
            {
                var line = await _reader.ReadLineAsync();
                if (line == null)
                {
                    TriggerDisconnect();
                    return null;
                }
                return line;
            }
            catch
            {
                TriggerDisconnect();
                return null;
            }
        }

        // Close the TCP connection
        public void Close()
        {
            try
            {
                _client.Close();
            }
            catch { }
            TriggerDisconnect();
        }

        private void TriggerDisconnect()
        {
            OnDisconnected?.Invoke(this);
            OnDisconnected = null; // ensure it only fires once
        }
    }
}

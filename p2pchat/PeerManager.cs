using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace P2PChat
{
    public class PeerManager
    {
        private readonly ConcurrentDictionary<string, PeerConnection> _peers = new();

        public void AddPeer(PeerConnection conn)
        {
            _peers[conn.Peer.Id] = conn;
            Console.WriteLine($"{conn.Peer.Name} connected.");
        }

        public void RemovePeer(string peerId)
        {
            if (_peers.TryRemove(peerId, out var conn))
            {
                conn.Close();
                Console.WriteLine($"{conn.Peer.Name} disconnected.");
            }
        }

        public bool HasPeer(string peerId) => _peers.ContainsKey(peerId);

        public async Task BroadcastAsync(ChatMessage msg)
        {
            foreach (var conn in _peers.Values)
            {
                try
                {
                    await conn.SendAsync(msg.ToString());
                }
                catch { }
            }
            Console.WriteLine(msg.ToString()); // also show locally
        }

        public void ListPeers()
        {
            if (_peers.IsEmpty)
                Console.WriteLine("No peers connected.");
            else
                foreach (var p in _peers.Values)
                    Console.WriteLine(p.Peer);
        }
    }
}
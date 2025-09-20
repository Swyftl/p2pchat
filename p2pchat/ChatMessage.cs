using System;

namespace P2PChat
{
    public class ChatMessage
    {
        public User Sender { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.Now;
        public string Text { get; init; }

        public ChatMessage(User sender, string text)
        {
            Sender = sender;
            Text = text;
        }

        public override string ToString() =>
            $"[{Timestamp:HH:mm}] {Sender.Name}: {Text}";
    }
}
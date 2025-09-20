using System;
using System.Threading.Tasks;

namespace P2PChat
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Write("Enter your name: ");
            var name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name)) name = "Anon";

            Console.Write("Enter TCP port to listen on (default 5000): ");
            var portInput = Console.ReadLine();
            int port = 5000;
            if (!string.IsNullOrWhiteSpace(portInput) && int.TryParse(portInput, out var p)) port = p;

            var app = new ChatApp(name, port);
            await app.RunAsync();
        }
    }
}
namespace P2PChat
{
    public class User
    {
        public string Id { get; init; } = System.Guid.NewGuid().ToString();
        public string Name { get; set; } = "Anon";
        public string EndPoint { get; set; } = "";

        public override string ToString() => $"{Name} ({EndPoint})";
    }
}
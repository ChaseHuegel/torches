using Networking;
using Packets.Chat;

namespace Chat.Server;

public class Application
{
    private readonly SessionService _sessionService;

    public Application(SessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public void Run()
    {
        Session session = _sessionService.RequestNew().Value;
        for (byte i = 0; i <= 6; i++)
        {
            var broadcast = new BroadcastMessage(i, session.ID, "Hello world!");
            var packet = new Packet(
                PacketType.BroadcastMessage,
                broadcast.Serialize()
            );

            var data = packet.Serialize();

            var deserializedPacket = Packet.Deserialize(data, 0, data.Length);
            var deserializedBroadcast = BroadcastMessage.Deserialize(deserializedPacket.Data, 0, deserializedPacket.Data.Length);
            Console.WriteLine($"{Smart.Format("{:L:Chat.Format}", deserializedBroadcast)}");
        }
    }
}
using Packets.Chat;

namespace Chat.Server;

public class Application
{
    public void Run()
    {
        for (byte i = 0; i <= 6; i++)
        {
            var broadcast = new BroadcastMessage(i, i, "Hello world!");
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
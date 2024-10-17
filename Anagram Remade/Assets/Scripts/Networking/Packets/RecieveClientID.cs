using tehelee.networking;
using Unity.Networking.Transport;
namespace testingui.networking.packets
{
    public class RecieveClientID : Packet
    {
        public override ushort id { get { return (ushort)PacketRegistry.RecieveClientID; } }

        public override int bytes { get { return sizeof(ushort) + 4 + sizeof(int); } }

        ////////////////////////////////


        public ushort networkId;

        public RecieveClientID() : base() { }

        public RecieveClientID(ref DataStreamReader reader) : base(ref reader)
        {
            networkId = reader.ReadUShort();
        }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteUShort(networkId);
        }
    }
}

using tehelee.networking;
using Unity.Networking.Transport;
namespace testingui.networking.packets
{
    public class GetClientID : Packet
    {
        public override ushort id { get { return (ushort)PacketRegistry.GetClientID; } }

        public override int bytes { get { return sizeof(ushort) + 4 + sizeof(int); } }

        ////////////////////////////////


        public ushort networkId;

        public GetClientID() : base() { }

        public GetClientID(ref DataStreamReader reader) : base(ref reader)
        {
            networkId = reader.ReadUShort();
        }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteUShort(networkId);
        }
}
}

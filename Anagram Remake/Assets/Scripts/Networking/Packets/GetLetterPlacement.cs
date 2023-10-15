using tehelee.networking;
using Unity.Networking.Transport;

namespace testingui.networking.packets
{
    public class GetLetterPlacement : Packet
    {
        ////////////////////////////////

        public override ushort id { get { return (ushort)PacketRegistry.GetLetterPlacement; } }

        public override int bytes { get { return sizeof(int) + 4 + sizeof(int); } }

        ////////////////////////////////


        public int networkId;

        public int textLength;

        ////////////////////////////////


        ////////////////////////////////

        public GetLetterPlacement() : base() { }

        public GetLetterPlacement(ref DataStreamReader reader) : base(ref reader)
        {
            networkId = reader.ReadInt();

            textLength = reader.ReadInt();
        }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteInt(networkId);

            writer.WriteInt(textLength);
        }
    }
}
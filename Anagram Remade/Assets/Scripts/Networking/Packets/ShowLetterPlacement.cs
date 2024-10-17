using tehelee.networking;
using Unity.Networking.Transport;

namespace testingui.networking.packets
{
    public class ShowLetterPlacement : Packet
    {
        ////////////////////////////////

        public override ushort id { get { return (ushort)PacketRegistry.ShowLetterPlacement; } }

        public override int bytes { get { return sizeof(ushort) + 4 + sizeof(int); } }

        ////////////////////////////////


        public ushort networkId;

        public int textLength;

        ////////////////////////////////


        ////////////////////////////////

        public ShowLetterPlacement() : base() { }

        public ShowLetterPlacement(ref DataStreamReader reader) : base(ref reader)
        {
            networkId = reader.ReadUShort();

            textLength = reader.ReadInt();
        }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteUShort(networkId);

            writer.WriteInt(textLength);
        }
    }
}
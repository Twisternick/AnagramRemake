using tehelee.networking;
using Unity.Networking.Transport;
using System;
namespace testingui.networking.packets
{
    public class ValidWord : Packet
    {
        ////////////////////////////////

        public override ushort id { get { return (ushort)PacketRegistry.ValidWord; } }

        public override int bytes { get { return sizeof(ushort) + 4 + 4 + sizeof(int); } }

        ////////////////////////////////


        public ushort networkId;

        public bool isValid;

        public int score;

        ////////////////////////////////


        ////////////////////////////////

        public ValidWord() : base() { }

        public ValidWord(ref DataStreamReader reader) : base(ref reader)
        {
            networkId = reader.ReadUShort();
            isValid = Convert.ToBoolean(reader.ReadInt());
            score = reader.ReadInt();

        }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteUShort(networkId);
            writer.WriteInt(isValid ? 1 : 0);
            writer.WriteInt(score);
        }

        ////////////////////////////////
    }
}

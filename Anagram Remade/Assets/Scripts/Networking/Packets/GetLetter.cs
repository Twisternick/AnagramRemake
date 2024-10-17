using tehelee.networking;
using Unity.Networking.Transport;

namespace testingui.networking.packets
{
    public class GetLetter : Packet
    {
        ////////////////////////////////

        public override ushort id { get { return (ushort)PacketRegistry.GetLetter; } }

        public override int bytes { get { return sizeof(ushort) + 4 + sizeof(int); } }

        ////////////////////////////////


        public ushort networkId;

        public enum LetterChoice
        {
            Vowel,
            Consonant,
            ServerChoice
        }

        public LetterChoice letterChoice;

        ////////////////////////////////


        ////////////////////////////////

        public GetLetter() : base() { }

        public GetLetter(ref DataStreamReader reader) : base(ref reader)
        {
            networkId = reader.ReadUShort();

            letterChoice = (LetterChoice)reader.ReadInt();
        }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteUShort(networkId);

            writer.WriteInt((int)letterChoice);
        }
    }
}
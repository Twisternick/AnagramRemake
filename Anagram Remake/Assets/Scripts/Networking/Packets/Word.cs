using tehelee.networking;
using Unity.Networking.Transport;

namespace testingui.networking.packets
{
    public class Word : Packet
    {
        ////////////////////////////////

        public override ushort id { get { return (ushort)PacketRegistry.Word; } }

        public override int bytes { get { return sizeof(ushort) + 4 + 4 + (string.IsNullOrEmpty(text) ? 0 : text.Length * 2); } }

        ////////////////////////////////


        public ushort networkId;

        public string text;

        public int score;

        ////////////////////////////////


        ////////////////////////////////

        public Word() : base() { }

        public Word(ref DataStreamReader reader) : base(ref reader)
        {
            networkId = reader.ReadUShort();
            score = reader.ReadInt();

            int _textLength = reader.ReadInt();
            if (_textLength > 0)
            {
                char[] textChars = new char[_textLength];
                for (int i = 0; i < _textLength; i++)
                    textChars[i] = (char)reader.ReadUShort();
                text = new string(textChars);
            }
            else
            {
                text = string.Empty;
            }
        }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteUShort(networkId);
            writer.WriteInt(score);

            if (string.IsNullOrEmpty(text))
            {
                writer.WriteInt(0);
            }
            else
            {
                int _textLength = text.Length;
                writer.WriteInt(text.Length);

                for (int i = 0; i < _textLength; i++)
                    writer.WriteUShort((ushort)text[i]);
            }
        }

        ////////////////////////////////
    }
}

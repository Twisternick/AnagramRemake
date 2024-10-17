using tehelee.networking;
using Unity.Networking.Transport;
using System;
namespace testingui.networking.packets
{
    public class Score : Packet
    {
        ////////////////////////////////

        public override ushort id { get { return (ushort)PacketRegistry.Score; } }

        public override int bytes
        {
            get
            {
                return sizeof(int) + 4 + sizeof(int) + sizeof(int) + sizeof(int) + (string.IsNullOrEmpty(word) ? 0 : word.Length * 2);
            }
        }

        ////////////////////////////////


        public int networkId;

        public int score;

        public int round;

        public bool winner = false;

        public string word = "";
        ////////////////////////////////


        ////////////////////////////////

        public Score() : base() { }

        public Score(ref DataStreamReader reader) : base(ref reader)
        {
            networkId = reader.ReadInt();
            score = reader.ReadInt();
            round = reader.ReadInt();
            winner = Convert.ToBoolean(reader.ReadInt());
            int _typeLength = reader.ReadInt();
            if (_typeLength > 0)
            {
                char[] typeChars = new char[_typeLength];
                for (int i = 0; i < _typeLength; i++)
                    typeChars[i] = (char)reader.ReadUShort();
                word = new string(typeChars);
            }
            else
            {
                word = string.Empty;
            }
        }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteInt(networkId);
            writer.WriteInt(score);
            writer.WriteInt(round);
            writer.WriteInt(Convert.ToInt32(winner));

            if (string.IsNullOrEmpty(word))
            {
                writer.WriteInt(0);
            }
            else
            {
                int _typeLength = word.Length;
                writer.WriteInt(word.Length);

                for (int i = 0; i < _typeLength; i++)
                    writer.WriteUShort(word[i]);
            }
        }

        ////////////////////////////////
    }
}

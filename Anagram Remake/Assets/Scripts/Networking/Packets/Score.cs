using tehelee.networking;
using Unity.Networking.Transport;
using System;
namespace testingui.networking.packets
{
    public class Score : Packet
    {
        ////////////////////////////////

        public override ushort id { get { return (ushort)PacketRegistry.Score; } }

        public override int bytes { get { return sizeof(int)  + 4 + sizeof(int) + sizeof(int); } }

        ////////////////////////////////


        public int networkId;

        public int score;

        public bool winner = false;
        ////////////////////////////////


        ////////////////////////////////

        public Score() : base() { }

        public Score(ref DataStreamReader reader) : base(ref reader)
        {
            networkId = reader.ReadInt();
            score = reader.ReadInt();
            winner = Convert.ToBoolean(reader.ReadInt());
        }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteInt(networkId);
            writer.WriteInt(score);
            writer.WriteInt(Convert.ToInt32(winner));
        }

        ////////////////////////////////
    }
}

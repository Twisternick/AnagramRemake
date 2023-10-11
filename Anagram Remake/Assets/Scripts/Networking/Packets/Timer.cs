using tehelee.networking;
using Unity.Networking.Transport;
using System;
namespace testingui.networking.packets
{
    public class Timer : Packet
    {
        ////////////////////////////////

        public override ushort id { get { return (ushort)PacketRegistry.Timer; } }

        public override int bytes { get { return sizeof(ushort) + 4 + sizeof(float); } }

        ////////////////////////////////

        public float time;

        public Timer() : base() { }

        public Timer(ref DataStreamReader reader) : base(ref reader)
        {
            time = reader.ReadFloat();
        }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteFloat(time);
        }
    }

}

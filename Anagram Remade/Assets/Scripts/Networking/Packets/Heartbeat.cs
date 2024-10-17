using Unity.Networking.Transport;

namespace tehelee.networking.packets
{
    public class Heartbeat : Packet
    {
        ////////////////////////////////

        public override ushort id { get { return (ushort)PacketRegistry.Heartbeat; } }

        public override int bytes { get { return 4; } }

        ////////////////////////////////

        public float time;

        ////////////////////////////////

        public Heartbeat() : base() { }

        public Heartbeat(ref DataStreamReader reader) : base(ref reader)
        {
            time = reader.ReadFloat();
        }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteFloat(time);
        }

        ////////////////////////////////
    }
}
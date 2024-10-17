using System.Collections.Generic;
using Unity.Networking.Transport;

namespace tehelee.networking.packets
{
    public class PacketBundle : Packet
    {
        ////////////////////////////////

        public override ushort id { get { return (ushort)PacketRegistry.PacketBundle; } }

        public override int bytes
        {
            get
            {
                int bytes = 4;

                foreach (Packet packet in packets)
                    bytes += (packet.bytes + 2);

                return bytes;
            }
        }

        ////////////////////////////////

        public List<Packet> packets = new List<Packet>();

        ////////////////////////////////

        public PacketBundle() : base() { }

        public PacketBundle(ref DataStreamReader reader) : base(ref reader) { }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteInt(packets.Count);

            foreach (Packet packet in packets)
            {
                writer.WriteInt(packet.id);

                packet.Write(ref writer);
            }
        }

        ////////////////////////////////
    }
}
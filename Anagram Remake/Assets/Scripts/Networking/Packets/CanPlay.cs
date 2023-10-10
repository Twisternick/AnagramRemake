using tehelee.networking;
using Unity.Networking.Transport;
using System;

namespace testingui.networking.packets
{
    public class CanPlay : Packet
    {
		////////////////////////////////

		public override ushort id { get { return (ushort)PacketRegistry.CanPlay; } }

		public override int bytes { get { return sizeof(ushort) + 4 + sizeof(int); } }

		////////////////////////////////


		public ushort networkId;

		public bool canPlay;

		////////////////////////////////


		////////////////////////////////

		public CanPlay() : base() { }

		public CanPlay(ref DataStreamReader reader) : base(ref reader)
		{
			networkId = reader.ReadUShort();

			canPlay = Convert.ToBoolean(reader.ReadInt());
		}

		public override void Write(ref DataStreamWriter writer)
		{
			writer.WriteUShort(networkId);

			writer.WriteInt(canPlay ? 1 : 0);
		}

		////////////////////////////////
	}
}


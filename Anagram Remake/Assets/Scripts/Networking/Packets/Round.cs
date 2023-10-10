using tehelee.networking;
using Unity.Networking.Transport;

namespace testingui.networking.packets
{
	public class Round : Packet
	{
		////////////////////////////////

		 public override ushort id { get { return (ushort)PacketRegistry.Round; } }

		public override int bytes { get { return sizeof(ushort) + sizeof(int) + sizeof(int) + sizeof(RoundState); } }

		////////////////////////////////

		public int counter;

		public RoundState roundState;

		public int clientToChoose;

		public enum RoundState
        {
			roundStart,
			roundEnd,
			roundWaiting
        }

		////////////////////////////////


		////////////////////////////////

		public Round() : base() { }

		public Round(ref DataStreamReader reader) : base(ref reader)
		{
			roundState = (RoundState)reader.ReadInt();
			counter = reader.ReadInt();
			clientToChoose = reader.ReadInt();
		}

		public override void Write(ref DataStreamWriter writer)
		{
			writer.WriteInt((int)roundState);
			writer.WriteInt(counter);
			writer.WriteInt(clientToChoose);
		}

		////////////////////////////////
	}
}


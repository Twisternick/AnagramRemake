using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

using tehelee.networking;

namespace tehelee.networking.packets
{
	public class Heartbeat : Packet
	{
		////////////////////////////////
		
		public override ushort id { get { return ( ushort ) PacketRegistry.Heartbeat; } }
		
		public override int bytes { get { return 4; } }

		////////////////////////////////
		
		public float time;

		////////////////////////////////

		public Heartbeat() : base() { }

		public Heartbeat( ref DataStreamReader reader, ref DataStreamReader.Context context ) : base( ref reader, ref context )
		{
			time = reader.ReadFloat( ref context );
		}

		public override void Write( ref DataStreamWriter writer )
		{
			writer.Write( time );
		}

		////////////////////////////////
	}
}
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Networking.Transport;

using tehelee.networking;

namespace studentviewer.networking.packets
{
	public class SendServerClose : Packet
	{
		////////////////////////////////
		
		public override ushort id { get { return ( ushort ) PacketRegistry.SendServerClose; } }
		
		public override int bytes { get { return 0; } }

		////////////////////////////////


		////////////////////////////////

		public SendServerClose() : base() { }

		public SendServerClose( ref DataStreamReader reader ) : base( ref reader )
		{
		}

		public override void Write( ref DataStreamWriter writer )
		{
		}

		////////////////////////////////
	}
}
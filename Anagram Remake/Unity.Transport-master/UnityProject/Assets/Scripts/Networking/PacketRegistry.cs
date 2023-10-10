using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tehelee.networking
{
	public enum PacketRegistry : ushort
	{
		Invalid,
		PacketBundle,
		Heartbeat,

	}
}
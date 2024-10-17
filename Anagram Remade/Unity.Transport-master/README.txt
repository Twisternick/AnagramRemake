This is a bare-bones higher-level api for the Unity Network Transport Layer.

This uses a modified version of my script preprocessor, available here under an identical license: https://github.com/Tehelee/Unity.ScriptPreprocessor

////////////////////////////////

Packets should be created under Networking / Packets; these will have the packet template applied to them and be added to the packet registry enum.
The enum name will be prefixed by containing folders underneath the Networking/Packets folder; Networking/Packets/Player/Setup/RequestSpawn will become Player_Setup_RequestSpawn.

////////////////////////////////

"Reliable" packets can only have 32 in progress to a client at a time, the PacketBundle exists for this reason and is used automatically by the server when sending to clients.
Clients automatically unpack these bundles and read them as individual packets.

////////////////////////////////

Scripts can subscribe listeners to packet read events (based on the packet registry enum value) and return a result code for consuming, processing, skipping, or failing per read.

////////////////////////////////

A client should send a routine heartbeat packet to the server to maintain a keep alive signal and prevent from being kicked for unresponsivness.

////////////////////////////////

Once a Server or Client component is running in your scene, you can call:
Server.instance.Send( new Packet() ); // Also provides an overload to target specific network connections.
Client.instance.Send( new Packet() );

////////////////////////////////

The Unity Transport api can be updated independently, however DataStream.cs : DataStreamReader.Context needs to have a constructor added:

public Context( Context clone )
{
	m_ReadByteIndex = clone.m_ReadByteIndex;
	m_BitIndex = clone.m_BitIndex;
	m_BitBuffer = clone.m_BitBuffer;
}

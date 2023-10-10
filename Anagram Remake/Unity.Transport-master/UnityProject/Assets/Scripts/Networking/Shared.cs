using System.Collections;
using System.Collections.Generic;
using System.Net;

using UnityEngine;

using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

namespace tehelee.networking
{
	[System.Serializable]
	public abstract class Packet
	{
		public virtual ushort id { get { return ( ushort ) PacketRegistry.Invalid; } }

		public virtual int bytes { get { return -1; } }

		public bool valid { get { return ( ( id > 0 ) && ( bytes > 0 ) ); } }

		public List<NetworkConnection> targets = new List<NetworkConnection>();

		public abstract void Write( ref DataStreamWriter writer );

		public Packet() { }

		public Packet( ref DataStreamReader reader, ref DataStreamReader.Context context ) { }
	}

	public enum ReadResult : byte
	{
		Skipped,	// Not utilized by this listener
		Processed,	// Used by this listener, but non exclusively.
		Consumed,	// Used by this listener, and stops all further listener checks.
		Error		// A problem was encountered, store information into readHandlerErrorMessage
	}

	public class Shared : MonoBehaviour
	{
		////////////////////////////////
		public static readonly HashSet<PacketRegistry> silentPackets = new HashSet<PacketRegistry>( new PacketRegistry[]
		{
			PacketRegistry.Heartbeat,
		} );

		////////////////////////////////

		[System.Serializable]
		protected struct PacketQueue
		{
			public Queue<Packet> reliable;
			public Queue<Packet> unreliable;
		}

		protected PacketQueue packetQueue = new PacketQueue() { reliable = new Queue<Packet>(), unreliable = new Queue<Packet>() };

		public virtual void Send( Packet packet, bool reliable = false )
		{
			if( !open )
				return;

			if( null == packet )
				return;

			if( reliable )
				packetQueue.reliable.Enqueue( packet );
			else
				packetQueue.unreliable.Enqueue( packet );

			if( debug && !silentPackets.Contains( ( PacketRegistry ) packet.id ) )
				Debug.LogWarningFormat( "{0}.Write( {1} ) using {2} channel.{3}", this is Server ? "Server" : "Client", ( PacketRegistry ) packet.id, reliable ? "verified" : "fast", packet.targets.Count > 0 ? string.Format( " Sent only to {0} connections.", packet.targets.Count ) : string.Empty );
		}
		
		protected void Read( NetworkConnection connection, ref DataStreamReader reader )
		{
			DataStreamReader.Context context = default( DataStreamReader.Context );

			Read( connection, ref reader, ref context );
		}

		protected void Read( NetworkConnection connection, ref DataStreamReader reader, ref DataStreamReader.Context context )
		{
			ushort packetId = reader.ReadUShort( ref context );

			if( packetId == ( ushort ) PacketRegistry.PacketBundle )
			{
				int count = reader.ReadInt( ref context );
				
				for( int i = 0; i < count; i++ )
				{
					Read( connection, ref reader, ref context );
				}

				return;
			}

			if( packetListeners.ContainsKey( packetId ) && packetListeners[ packetId ] != null )
			{
				bool silentPacket = silentPackets.Contains( (PacketRegistry) packetId );

				if( debug && !silentPacket )
					Debug.LogFormat( "{0}.Read( {1} ) with {2} listeners.", this is Server ? "Server" : "Client", ( PacketRegistry ) packetId, packetListeners[ packetId ].Count );

				Dictionary<int, ReadHandler> listeners = packetListeners[ packetId ];
				List<int> keys = new List<int>( listeners.Keys );
				keys.Sort();

				bool processed = false;
				
				DataStreamReader.Context iterationContext, processedContext = context;
				for( int i = 0; i < keys.Count; i++ )
				{
					int key = keys[ i ];
					ReadHandler readHandler = listeners[ key ];

					iterationContext = new DataStreamReader.Context( context );

					if( readHandler != null )
					{
						ReadResult result = readHandler( connection, ref reader, ref iterationContext );
						if( result != ReadResult.Skipped )
						{
							switch( result )
							{
								case ReadResult.Processed:
									if( debug && !silentPacket )
										Debug.LogFormat( "{0}.Read( {1} ) processed by listener {2}.", this is Server ? "Server" : "Client", ( PacketRegistry ) packetId, i );
									processedContext = iterationContext;
									processed = true;
									break;
								case ReadResult.Consumed:
									if( debug && !silentPacket )
										Debug.LogFormat( "{0}.Read( {1} ) consumed by listener {2}.", this is Server ? "Server" : "Client", ( PacketRegistry ) packetId, i );
									context = iterationContext;
									return;
								case ReadResult.Error:
									Debug.LogErrorFormat( "{0}.Read( {1} ) encountered an error on listener {2}.{3}", this is Server ? "Server" : "Client", ( PacketRegistry ) packetId, i, readHandlerErrorMessage != null ? string.Format( "\nError Message: {0}", readHandlerErrorMessage ) : string.Empty );
									return;
							}
						}
					}
				}

				if( processed )
					context = processedContext;

				if( !processed && debug )
					Debug.LogWarningFormat( "{0}.Read( {1} ) failed to be consumed by one of the {2} listeners.", this is Server ? "Server" : "Client", ( PacketRegistry ) packetId, packetListeners[ packetId ].Count );

				return;
			}

			if( debug )
				Debug.LogWarningFormat( "{0}.Read( {1} ) skipped; no associated listeners.", this is Server ? "Server" : "Client", ( PacketRegistry ) packetId );
		}

		public string readHandlerErrorMessage = null;

		public delegate ReadResult ReadHandler( NetworkConnection connection, ref DataStreamReader reader, ref DataStreamReader.Context context );

		Dictionary<ushort, Dictionary<int,ReadHandler>> packetListeners = new Dictionary<ushort, Dictionary<int, ReadHandler>>();

		public void RegisterListener( PacketRegistry packetId, ReadHandler handler )
		{
			RegisterListener( ( ushort ) packetId, handler );
		}

		public void RegisterListener( ushort packetId, ReadHandler handler, int priority = 0 )
		{
			if( !packetListeners.ContainsKey( packetId ) )
			{
				packetListeners.Add( packetId, new Dictionary<int, ReadHandler>() );
			}

			Dictionary<int, ReadHandler> listeners = packetListeners[ packetId ];

			while( listeners.ContainsKey( priority ) )
				priority++;

			packetListeners[ packetId ].Add( priority, handler );
		}

		public void DropListener( PacketRegistry packetId, ReadHandler handler )
		{
			DropListener( ( ushort ) packetId, handler );
		}

		public void DropListener( ushort packetId, ReadHandler handler )
		{
			if( packetListeners.ContainsKey( packetId ) && packetListeners[ packetId ].ContainsValue( handler ) )
			{
				Dictionary<int, ReadHandler> listeners = packetListeners[ packetId ];
				List<int> keys = new List<int>( listeners.Keys );
				for( int i = 0; i < keys.Count; i++ )
				{
					int k = keys[ i ];
					if( listeners[ k ] == handler )
					{
						packetListeners[ packetId ].Remove( k );
						break;
					}
				}

				if( packetListeners[ packetId ].Count == 0 )
					packetListeners.Remove( packetId );
			}
		}

		////////////////////////////////

		public UdpNetworkDriver driver;

		[System.Serializable]
		public struct Pipeline
		{
			public NetworkPipeline reliable;
			public NetworkPipeline unreliable;

			public Pipeline( UdpNetworkDriver driver )
			{
				reliable = driver.CreatePipeline( typeof( ReliableSequencedPipelineStage ) );
				unreliable = driver.CreatePipeline( typeof( UnreliableSequencedPipelineStage ) );
			}

			public NetworkPipeline this[ bool reliable ] { get { return reliable ? this.reliable : this.unreliable; } }
		}

		public Pipeline pipeline { get; private set; }

		public const string LoopbackAddress = "127.0.0.1";
		public string serverAddress = LoopbackAddress;

		public ushort port = 16448;

		public bool open { get; private set; }

		public bool debug = false;

		////////////////////////////////

		public virtual void Open()
		{
			if( open )
				return;
			
			Dictionary<string, string> args = new Dictionary<string, string>();
			args.Add( "serverAddress", null );
			args.Add( "serverPort", null );

			Utils.GetArgsFromDictionary( ref args );

			if( !string.IsNullOrEmpty( args[ "serverAddress" ] ) )
				this.serverAddress = args[ "serverAddress" ];

			this.serverAddress = this.serverAddress ?? LoopbackAddress;

			if( args[ "serverPort" ] != null )
			{
				ushort port;
				if( ushort.TryParse( args[ "serverPort" ], out port ) )
					this.port = port;
			}

			driver = new UdpNetworkDriver( new ReliableUtility.Parameters { WindowSize = 32 }, new NetworkConfigParameter { connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS, disconnectTimeoutMS = 15000, maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts } );

			pipeline = new Pipeline( driver );
			
			open = true;

			if( debug )
				Debug.LogFormat( "{0}.Open()", this is Server ? "Server" : "Client" );
		}

		public virtual void Close()
		{
			if( !open )
				return;

			open = false;

			pipeline = default( Pipeline );

			driver.Dispose();

			if( debug )
				Debug.LogFormat( "{0}.Close()", this is Server ? "Server" : "Client" );
		}

		////////////////////////////////
	}
}

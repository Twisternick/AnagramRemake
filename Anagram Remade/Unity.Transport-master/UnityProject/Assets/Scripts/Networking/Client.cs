using System.Collections;
using System.Collections.Generic;
using System.Net;

using UnityEngine;

using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tehelee.networking
{
	public class Client : Shared
	{
		////////////////////////////////

		public static Client instance { get; private set; }

		private static Callback startupCallback;
		public static void AddStartupListener( Callback callback )
		{
			startupCallback += callback;

			if( instance )
				callback();
		}

		////////////////////////////////

		private void Awake()
		{
			instance = this;

			if( startupCallback != null )
			startupCallback();
		}

		private void OnEnable()
		{
			if( openOnEnable )
				Open();
		}

		private void OnDisable()
		{
			Close();
		}

		////////////////////////////////

		protected NetworkConnection connection;

		private uint reconnectAttempts = 0;

		////////////////////////////////

		public UnityEngine.Events.UnityEvent onConnected = new UnityEngine.Events.UnityEvent();
		public UnityEngine.Events.UnityEvent onDisconnected = new UnityEngine.Events.UnityEvent();

		public bool reattemptFailedConnections = false;

		public bool openOnEnable = false;

		////////////////////////////////

		public override void Open()
		{
			base.Open();

			connection = default( NetworkConnection );

			connection = driver.Connect( NetworkEndPoint.Parse( this.serverAddress ?? string.Empty, this.port ) );
		}

		public override void Close()
		{
			if( connection.IsCreated )
			{
				connection.Disconnect( driver );
				connection = default( NetworkConnection );
			}

			onDisconnected.Invoke();

			base.Close();
		}

		////////////////////////////////

		private void QueryForEvents()
		{
			DataStreamReader stream;
			NetworkEvent.Type netEventType;
			while( driver.IsCreated && connection.IsCreated && ( netEventType = connection.PopEvent( driver, out stream ) ) != NetworkEvent.Type.Empty )
			{
				if( netEventType == NetworkEvent.Type.Connect )
				{
					if( debug )
						Debug.Log( "Client: Connected to the server." );

					reconnectAttempts = 0;

					onConnected.Invoke();
				}
				else if( netEventType == NetworkEvent.Type.Data )
				{
					Read( connection, ref stream );
				}
				else if( netEventType == NetworkEvent.Type.Disconnect )
				{
					if( debug )
						Debug.Log( "Client: Removed from server." );

					Close();
				}
			}
		}

		private void SendQueue()
		{
			Packet packet;

			while( packetQueue.reliable.Count > 0 )
			{
				packet = packetQueue.reliable.Dequeue();

				DataStreamWriter writer = new DataStreamWriter( packet.bytes + 2, Allocator.Temp );

				writer.Write( packet.id );

				packet.Write( ref writer );

				connection.Send( driver, pipeline.reliable, writer );

				writer.Dispose();
			}

			while( packetQueue.unreliable.Count > 0 )
			{
				packet = packetQueue.unreliable.Dequeue();

				DataStreamWriter writer = new DataStreamWriter( packet.bytes + 2, Allocator.Temp );

				writer.Write( packet.id );

				packet.Write( ref writer );

				connection.Send( driver, pipeline.unreliable, writer );

				writer.Dispose();
			}
		}

		////////////////////////////////

		private void Update()
		{
			if( !driver.IsCreated )
				return;

			driver.ScheduleUpdate().Complete();

			if( !connection.IsCreated )
				return;

			QueryForEvents();

			// Events *could* result in destruction of these, so now we re-check.
			if( !driver.IsCreated || !connection.IsCreated )
				return;

			NetworkConnection.State connectionState = driver.GetConnectionState( connection );

			if( reattemptFailedConnections && ( connectionState == NetworkConnection.State.Disconnected ) )
			{
				Debug.LogFormat( "Client: Connection to '{0}:{1}' failed; auto-reconnect attempt {2}.", serverAddress, port, ++reconnectAttempts );
				
				connection = driver.Connect( NetworkEndPoint.Parse( this.serverAddress ?? string.Empty, this.port ) );

				return;
			}

			if( connectionState != NetworkConnection.State.Connected )
				return;

			SendQueue();
		}

		private void OnDestroy()
		{
			if( driver.IsCreated )
				driver.Dispose();
		}

		////////////////////////////////
	}


#if UNITY_EDITOR
	[CustomEditor( typeof( Client ) )]
	public class EditorClient : Editor
	{
		Client client;
		public void OnEnable()
		{
			client = ( Client ) target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();

			GUILayout.Space( 10f );

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();

			EditorGUI.BeginDisabledGroup( !Application.isPlaying );

			if( client.open )
			{
				if( GUILayout.Button( "Close Connection" ) )
				{
					client.Close();
				}
			}
			else
			{
				if( GUILayout.Button( "Open Connection" ) )
				{
					client.Open();
				}
			}
			
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space( 10f );

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}
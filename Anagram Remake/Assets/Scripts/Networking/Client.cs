
using UnityEngine;
using Unity.Networking.Transport;
//using Newtonsoft.Json.Linq;
using tehelee.networking.packets;
using System.Collections;
using Unity.Profiling;


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
        public static void AddStartupListener(Callback callback)
        {
            startupCallback += callback;

            if (instance)
                callback();
        }


        public int clientID = -1;

        ////////////////////////////////

        private void Awake()
        {
            instance = this;

            if (startupCallback != null)
                startupCallback();
        }

        private void OnEnable()
        {
            if (openOnEnable)
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

            connection = default(NetworkConnection);

            connection = driver.Connect(NetworkEndPoint.Parse(this.serverAddress ?? string.Empty, this.port, NetworkFamily.Ipv4));
            reconnectAttempts = 0;

            if (connection == default(NetworkConnection))
            {
                Debug.LogError("Client: Failed to connect to server.");
                return;
            }
            Debug.Log(connection.GetState(driver));
            /*   Debug.Log(connection.InternalId); */

        }

        public override void Close()
        {
            if (connection.IsCreated)
            {
                connection.Disconnect(driver);
                connection = default(NetworkConnection);
            }

            onDisconnected.Invoke();

            base.Close();
        }

        ////////////////////////////////

        private void QueryForEvents()
        {
            DataStreamReader stream;
            NetworkEvent.Type netEventType;
            while (driver.IsCreated && connection.IsCreated && (netEventType = connection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
            {
                if (netEventType == NetworkEvent.Type.Connect)
                {
                    if (debug)
                        Debug.Log("Client: Connected to the server.");

                    reconnectAttempts = 0;

                    // Send the server to get our client ID

                    onConnected.Invoke();
                }
                else if (netEventType == NetworkEvent.Type.Data)
                {
                    if (debug)
                    {
                        Debug.Log("Client: Received data");
                    }
                    Read(connection, ref stream);
                }
                else if (netEventType == NetworkEvent.Type.Disconnect)
                {
                    if (debug)
                        Debug.Log("Client: Removed from server.");

                    Close();
                }
            }
        }

        private void SendQueue()
        {
            Packet packet;

            while (packetQueue.reliable.Count > 0)
            {
                packet = packetQueue.reliable.Dequeue();
                driver.BeginSend(connection, out DataStreamWriter writer);

                //DataStreamWriter writer = new DataStreamWriter( packet.bytes + 2, Allocator.Temp );

                writer.WriteUShort(packet.id);

                packet.Write(ref writer);

                driver.EndSend(writer);

                //writer.Dispose();
            }

            while (packetQueue.unreliable.Count > 0)
            {
                packet = packetQueue.unreliable.Dequeue();

                driver.BeginSend(connection, out DataStreamWriter writer);

                writer.WriteUShort(packet.id);

                packet.Write(ref writer);

                driver.EndSend(writer);

                //writer.Dispose();
            }
        }
        ////////////////////////////////

        private void Update()
        {
            //Debug.Log(driver.GetConnectionState(connection));
            //Debug.Log(connection.GetState(driver));
            if (!driver.IsCreated)
                return;

            driver.ScheduleUpdate().Complete();

            NetworkConnection.State connectionState = driver.GetConnectionState(connection);
            print(connectionState);

            if (connectionState == NetworkConnection.State.Disconnected && reattemptFailedConnections && reconnectAttempts < 10)
            {
                Debug.LogError("Client Disconnected");
                connection = driver.Connect(NetworkEndPoint.Parse(this.serverAddress ?? string.Empty, this.port));
                reconnectAttempts++;
                return;
            }

            if (!connection.IsCreated)
                return;

            QueryForEvents();

            // Events *could* result in destruction of these, so now we re-check.
            if (!driver.IsCreated || !connection.IsCreated)
                return;


            if (reconnectAttempts < 10)
            {
                if (reattemptFailedConnections && (connectionState == NetworkConnection.State.Disconnected))
                {
                    Debug.LogFormat("Client: Connection to '{0}:{1}' failed; auto-reconnect attempt {2}.", serverAddress, port, ++reconnectAttempts);

                    connection = driver.Connect(NetworkEndPoint.Parse(this.serverAddress ?? string.Empty, this.port));

                    return;
                }
            }

            if (connectionState != NetworkConnection.State.Connected)
                return;

            SendQueue();
        }

        public void SetIPAddress(string ip)
        {
            serverAddress = ip;
        }

        private void OnDestroy()
        {
            if (driver.IsCreated)
                driver.Dispose();
        }

        ////////////////////////////////
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(Client))]
    public class EditorClient : Editor
    {
        Client client;
        public void OnEnable()
        {
            client = (Client)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            GUILayout.Space(10f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            if (client.open)
            {
                if (GUILayout.Button("Close Connection"))
                {
                    client.Close();
                }
            }
            else
            {
                if (GUILayout.Button("Open Connection"))
                {
                    client.Open();
                }
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10f);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
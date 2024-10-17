using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;
using PacketBundle = tehelee.networking.packets.PacketBundle;

namespace tehelee.networking
{
    public class Server : Shared
    {
        ////////////////////////////////

        public static Server instance { get; private set; }

        private static Callback startupCallback;
        public static void AddStartupListener(Callback callback)
        {
            startupCallback += callback;

            if (instance)
                callback();
        }

        ////////////////////////////////

        private void Awake()
        {
            instance = this;

            if (startupCallback != null)
                startupCallback();
        }

        private void OnEnable()
        {
            Open();
        }

        private void OnDisable()
        {
            Close();
        }

        ////////////////////////////////

        protected NativeList<NetworkConnection> connections;

        protected List<NetworkConnection> _connections = new List<NetworkConnection>();

        public byte playerLimit = 32;

        public byte playerCount { get { return (byte)connections.Length; } }

        protected Dictionary<NetworkConnection, LinkedList<Packet>> managedQueue = new Dictionary<NetworkConnection, LinkedList<Packet>>();

        ////////////////////////////////

        [System.Serializable]
        public class OnNetworkConnection : UnityEngine.Events.UnityEvent<NetworkConnection> { };

        public OnNetworkConnection onClientAdded = new OnNetworkConnection();
        public OnNetworkConnection onClientDropped = new OnNetworkConnection();

        ////////////////////////////////

        public override void Open()
        {
            base.Open();

            var endPoint = NetworkEndPoint.AnyIpv4;
            endPoint = NetworkEndPoint.Parse(this.serverAddress ?? string.Empty, this.port, NetworkFamily.Ipv4);


            if (driver.Bind(endPoint) != 0)
            {
                Debug.LogErrorFormat("Server: Failed to bind to '{0}' on port {1}.", this.serverAddress, port);
            }
            else
            {
                driver.Listen();

                Debug.LogFormat("Server: Bound to '{0}' on port {1}.", this.serverAddress, port);
            }

            connections = new NativeList<NetworkConnection>((int)playerLimit, Allocator.Persistent);
        }

        public override void Close()
        {
            connections.Dispose();

            base.Close();
        }

        public override void Send(Packet packet, bool reliable = false)
        {
            if (playerCount == 0)
                return;

            base.Send(packet, reliable);
        }

        ////////////////////////////////

        private void CleanupOldConnections()
        {
            for (int i = 0; i < connections.Length; i++)
            {
                if (!connections[i].IsCreated || driver.GetConnectionState(connections[i]) == NetworkConnection.State.Disconnected)
                {
                    if (debug)
                        Debug.LogWarningFormat("Server: Removed client {0}", connections[i].InternalId);

                    onClientDropped.Invoke(connections[i]);

                    _connections.Remove(connections[i]);

                    connections.RemoveAtSwapBack(i);
                    --i;
                }
            }
        }

        private void AcceptNewConnections()
        {
            NetworkConnection networkConnection;
            while ((networkConnection = driver.Accept()) != default(NetworkConnection))
            {
                if (debug)
                {
                    Debug.Log("Accepted new Client ID: " + networkConnection.InternalId);
                    
                }

                connections.Add(networkConnection);

                _connections.Add(networkConnection);

                onClientAdded.Invoke(networkConnection);

                if (debug)
                    Debug.LogWarningFormat("Server: Added client {0}", networkConnection.InternalId);
            }
        }

        private void QueryForEvents()
        {
            DataStreamReader stream;
            for (int i = 0; i < connections.Length; i++)
            {
                if (!connections[i].IsCreated)
                    continue;

                NetworkEvent.Type netEventType;
                while ((netEventType = driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
                {
                    if (netEventType == NetworkEvent.Type.Data)
                    {
                        Read(connections[i], ref stream);
                    }
                }
            }
        }

        ////////////////////////////////

        private unsafe int GetReliabilityError(NetworkConnection connection)
        {
            NativeArray<byte> readProcessingBuffer = default;
            NativeArray<byte> writeProcessingBuffer = default;
            NativeArray<byte> sharedBuffer = default;

            var reliableId = NetworkPipelineStageCollection.GetStageId(typeof(ReliableSequencedPipelineStage));

            driver.GetPipelineBuffers(pipeline.reliable, reliableId, connection, out readProcessingBuffer, out writeProcessingBuffer, out sharedBuffer);

            ReliableUtility.SharedContext* unsafePointer = (ReliableUtility.SharedContext*)sharedBuffer.GetUnsafePtr();

            if (unsafePointer->errorCode != 0)
            {
                int errorId = (int)unsafePointer->errorCode;
                return errorId;
            }
            else
            {
                return 0;
            }
        }

        private void SendQueue()
        {
            Packet packet;

            List<NetworkConnection> targets = new List<NetworkConnection>();

            while (packetQueue.reliable.Count > 0)
            {
                packet = packetQueue.reliable.Dequeue();

                targets.AddRange(packet.targets);
                packet.targets.Clear();

                if (targets.Count == 0)
                    targets.AddRange(_connections);

                foreach (NetworkConnection target in targets)
                {
                    if (!managedQueue.ContainsKey(target))
                        managedQueue.Add(target, new LinkedList<Packet>());

                    managedQueue[target].AddLast(packet);
                }

                targets.Clear();
            }

            targets.AddRange(managedQueue.Keys);
            foreach (NetworkConnection target in targets)
            {
                LinkedList<Packet> packets = managedQueue[target];

                if (packets.Count > 1)
                {
                    PacketBundle packetBundle = new PacketBundle();

                    packetBundle.packets.AddRange(packets);

                    packets.Clear();

                    packets.AddFirst(packetBundle);
                }

                while (packets.Count > 0)
                {
                    packet = packets.First.Value;

                    driver.BeginSend(target, out DataStreamWriter writer);

                    // Add Packet identifier
                    writer.WriteUShort(packet.id);
                    Debug.Log(packet.id);

                    // Apply / write packet data to data stream writer
                    packet.Write(ref writer);

                    driver.EndSend(writer);
                    int errorId = GetReliabilityError(target);
                    if (errorId != 0)
                    {
                        ReliableUtility.ErrorCodes error = (ReliableUtility.ErrorCodes)errorId;
                        if (error == ReliableUtility.ErrorCodes.OutgoingQueueIsFull)
                        {
                            /*
							if( debug )
								Debug.LogWarningFormat( "Server.SendReliable( {0} ): Outgoing reliable queue is full, delaying...", target );
							*/
                            break;
                        }
                        else
                        {
                            Debug.LogErrorFormat("Reliability Error: {0}", error);
                        }
                    }

                    //writer.Dispose();

                    packets.RemoveFirst();
                }

                if (packets.Count == 0)
                    managedQueue.Remove(target);
            }

            List<DataStreamWriter> sendAllUnreliable = new List<DataStreamWriter>();

            while (packetQueue.unreliable.Count > 0)
            {
                packet = packetQueue.unreliable.Dequeue();
                if (packet.targets.Count > 0)
                {
                    foreach (NetworkConnection connection in packet.targets)
                    {
                        driver.BeginSend(connection, out DataStreamWriter writer);

                        writer.WriteUShort(packet.id);


                        packet.Write(ref writer);
                        driver.EndSend(writer);
                    }
                }
                else
                {
                    foreach (NetworkConnection connection in connections)
                    {
                        driver.BeginSend(connection, out DataStreamWriter writer);

                        //Debug.Log(packet.id);
                        writer.WriteUShort(packet.id);
                        packet.Write(ref writer);
                        driver.EndSend(writer);
                    }
                }
            }
        }

        ////////////////////////////////

        public NetworkConnection GetConnection(int internalId)
        {
            if (internalId >= 0 && internalId < connections.Length)
            {
                return connections[internalId];
            }

            return default(NetworkConnection);
        }

        ////////////////////////////////

        private void Update()
        {
            if (!driver.IsCreated)
                return;

            driver.ScheduleUpdate().Complete();

            CleanupOldConnections();

            AcceptNewConnections();

            QueryForEvents();

            SendQueue();
        }

        private void OnDestroy()
        {
            if (driver.IsCreated)
                driver.Dispose();

            if (connections.IsCreated)
                connections.Dispose();
        }

        ////////////////////////////////
    }
}

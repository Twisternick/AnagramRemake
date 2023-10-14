using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace tehelee.networking
{
    [System.Serializable]
    public abstract class Packet
    {
        public virtual ushort id { get { return (ushort)PacketRegistry.Invalid; } }

        public virtual int bytes { get { return -1; } }

        public bool valid { get { return ((id > 0) && (bytes > 0)); } }

        public List<NetworkConnection> targets = new List<NetworkConnection>();

        public abstract void Write(ref DataStreamWriter writer);

        public Packet() { }

        public Packet(ref DataStreamReader reader) { }
    }

    public enum ReadResult : byte
    {
        Skipped,    // Not utilized by this listener
        Processed,  // Used by this listener, but non exclusively.
        Consumed,   // Used by this listener, and stops all further listener checks.
        Error       // A problem was encountered, store information into readHandlerErrorMessage
    }

    public class Shared : MonoBehaviour
    {
        ////////////////////////////////
        public static readonly HashSet<PacketRegistry> silentPackets = new HashSet<PacketRegistry>(new PacketRegistry[]
        {
            PacketRegistry.Heartbeat,
        });

        public static bool readyToClose;
        public static int attemptsToClose;
        ////////////////////////////////

        [System.Serializable]
        protected struct PacketQueue
        {
            public Queue<Packet> reliable;
            public Queue<Packet> unreliable;
        }

        protected PacketQueue packetQueue = new PacketQueue() { reliable = new Queue<Packet>(), unreliable = new Queue<Packet>() };

        public virtual void Send(Packet packet, bool reliable = false)
        {
            if (!open)
                return;

            if (null == packet)
            {
                if (debug)
                {
                    Debug.Log("Packet was null");
                }
                return;
            }

            if (reliable)
                packetQueue.reliable.Enqueue(packet);
            else
                packetQueue.unreliable.Enqueue(packet);

            if (debug && !silentPackets.Contains((PacketRegistry)packet.id))
                Debug.LogWarningFormat("{0}.Write( {1} ) using {2} channel.{3}", this is Server ? "Server" : "Client", (PacketRegistry)packet.id, reliable ? "verified" : "fast", packet.targets.Count > 0 ? string.Format(" Sent only to {0} connections.", packet.targets.Count) : string.Empty);
        }



        protected void Read(NetworkConnection connection, ref DataStreamReader reader)
        {
            ushort packetId = reader.ReadUShort();

            if (packetId == (ushort)PacketRegistry.PacketBundle)
            {
                int count = reader.ReadInt();

                for (int i = 0; i < count; i++)
                {
                    Read(connection, ref reader);
                }

                return;
            }

            if (packetListeners.ContainsKey(packetId) && packetListeners[packetId] != null)
            {
                bool silentPacket = silentPackets.Contains((PacketRegistry)packetId);

                if (debug && !silentPacket)
                    UnityEngine.Debug.LogFormat("{0}.Read( {1} ) with {2} listeners.", this is Server ? "Server" : "Client", (PacketRegistry)packetId, packetListeners[packetId].Count);

                Dictionary<int, ReadHandler> listeners = packetListeners[packetId];
                List<int> keys = new List<int>(listeners.Keys);
                keys.Sort();

                bool processed = false;


                for (int i = 0; i < keys.Count; i++)
                {
                    int key = keys[i];
                    ReadHandler readHandler = listeners[key];


                    if (readHandler != null)
                    {
                        ReadResult result = readHandler(connection, ref reader);
                        if (result != ReadResult.Skipped)
                        {
                            switch (result)
                            {
                                case ReadResult.Processed:
                                    if (debug && !silentPacket)
                                        UnityEngine.Debug.LogFormat("{0}.Read( {1} ) processed by listener {2}.", this is Server ? "Server" : "Client", (PacketRegistry)packetId, i);
                                    processed = true;
                                    break;
                                case ReadResult.Consumed:
                                    if (debug && !silentPacket)
                                        UnityEngine.Debug.LogFormat("{0}.Read( {1} ) consumed by listener {2}.", this is Server ? "Server" : "Client", (PacketRegistry)packetId, i);
                                    return;
                                case ReadResult.Error:
                                    UnityEngine.Debug.LogErrorFormat("{0}.Read( {1} ) encountered an error on listener {2}.{3}", this is Server ? "Server" : "Client", (PacketRegistry)packetId, i, readHandlerErrorMessage != null ? string.Format("\nError Message: {0}", readHandlerErrorMessage) : string.Empty);
                                    return;
                            }
                        }
                    }
                }


                if (!processed && debug)
                    Debug.LogWarningFormat("{0}.Read( {1} ) failed to be consumed by one of the {2} listeners.", this is Server ? "Server" : "Client", (PacketRegistry)packetId, packetListeners[packetId].Count);

                return;
            }

            if (debug)
                Debug.LogWarningFormat("{0}.Read( {1} ) skipped; no associated listeners.", this is Server ? "Server" : "Client", (PacketRegistry)packetId);
        }

        public string readHandlerErrorMessage = null;

        public delegate ReadResult ReadHandler(NetworkConnection connection, ref DataStreamReader reader);

        Dictionary<ushort, Dictionary<int, ReadHandler>> packetListeners = new Dictionary<ushort, Dictionary<int, ReadHandler>>();

        public void RegisterListener(PacketRegistry packetId, ReadHandler handler)
        {
            RegisterListener((ushort)packetId, handler);
        }

        public void RegisterListener(ushort packetId, ReadHandler handler, int priority = 0)
        {
            if (!packetListeners.ContainsKey(packetId))
            {
                packetListeners.Add(packetId, new Dictionary<int, ReadHandler>());
            }

            Dictionary<int, ReadHandler> listeners = packetListeners[packetId];

            while (listeners.ContainsKey(priority))
                priority++;

            packetListeners[packetId].Add(priority, handler);
        }

        public void DropListener(PacketRegistry packetId, ReadHandler handler)
        {
            DropListener((ushort)packetId, handler);
        }

        public void DropListener(ushort packetId, ReadHandler handler)
        {
            if (packetListeners.ContainsKey(packetId) && packetListeners[packetId].ContainsValue(handler))
            {
                Dictionary<int, ReadHandler> listeners = packetListeners[packetId];
                List<int> keys = new List<int>(listeners.Keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    int k = keys[i];
                    if (listeners[k] == handler)
                    {
                        packetListeners[packetId].Remove(k);
                        break;
                    }
                }

                if (packetListeners[packetId].Count == 0)
                    packetListeners.Remove(packetId);
            }
        }

        ////////////////////////////////

        public NetworkDriver driver;

        [System.Serializable]
        public struct Pipeline
        {
            public NetworkPipeline reliable;
            public NetworkPipeline unreliable;

            public Pipeline(NetworkDriver driver)
            {
                reliable = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
                unreliable = driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            }

            public NetworkPipeline this[bool reliable] { get { return reliable ? this.reliable : this.unreliable; } }
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
            if (open)
                return;

            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("serverAddress", null);
            args.Add("serverPort", null);

            Utils.GetArgsFromDictionary(ref args);

            if (!string.IsNullOrEmpty(args["serverAddress"]))
                this.serverAddress = args["serverAddress"];

            this.serverAddress = this.serverAddress ?? LoopbackAddress;

            if (args["serverPort"] != null)
            {
                ushort port;
                if (ushort.TryParse(args["serverPort"], out port))
                    this.port = port;
            }


            //new Pipeline(driver);


            driver = NetworkDriver.Create(new NetworkConfigParameter() { connectTimeoutMS = 1000, disconnectTimeoutMS = 1000, maxConnectAttempts = 20 });
            
            //driver = new NetworkDriver( new ReliableUtility.Parameters { WindowSize = 32 }, new NetworkConfigParameter { connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS, disconnectTimeoutMS = 15000, maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts } );



            open = true;

            if (debug)
            {
                Debug.LogFormat("{0}.Open()", this is Server ? "Server" : "Client");
            }
        }

        public virtual void Close()
        {
            if (!open)
                return;

            open = false;

            pipeline = default(Pipeline);

            driver.Dispose();

            if (debug)
                Debug.LogFormat("{0}.Close()", this is Server ? "Server" : "Client");
        }

        ////////////////////////////////
    }
}

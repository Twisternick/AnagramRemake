using System;
using tehelee.networking;
using Unity.Networking.Transport;

namespace testingui.networking.packets
{
    public class Letter : Packet
    {
        ////////////////////////////////

        public override ushort id { get { return (ushort)PacketRegistry.Letter; } }

        public override int bytes
        {
            get
            {
                int _bytes = 0;
                _bytes += 4 + 4 + (string.IsNullOrEmpty(text) ? 0 : text.Length * 2); // type
                return _bytes;
            }
        }

        ////////////////////////////////
        
        public int networkId;

        public string text;


        public Letter() : base() { }

        public Letter(ref DataStreamReader reader) : base(ref reader)
        {
            networkId = reader.ReadInt();
            int _typeLength = reader.ReadInt();
            if (_typeLength > 0)
            {
                char[] typeChars = new char[_typeLength];
                for (int i = 0; i < _typeLength; i++)
                    typeChars[i] = (char)reader.ReadUShort();
                text = new string(typeChars);
            }
            else
            {
                text = string.Empty;
            }
        }

        public override void Write(ref DataStreamWriter writer)
        {
            writer.WriteInt(networkId);
            if (string.IsNullOrEmpty(text))
            {
                writer.WriteInt(0);
            }
            else
            {
                int _typeLength = text.Length;
                writer.WriteInt(text.Length);

                for (int i = 0; i < _typeLength; i++)
                    writer.WriteUShort(text[i]);
            }
        }

        ////////////////////////////////
    }
}


using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace tehelee
{
    public delegate void Callback();

    public delegate void Callback<T>(T context);

    public delegate void Callback<T, T2>(T context, T2 context2);

    // This is a static utility class for method defines used in multiple locations

    public class Utils : MonoBehaviour
    {
        ////////////////
        // Hashing
        ////////////////

        #region Hasing

        private static readonly ushort CrcPolynomial = 0xA001;

        private static ushort[] _CrcTable = null;
        private static ushort[] CrcTable
        {
            get
            {
                if (_CrcTable == null)
                {
                    ushort[] table = new ushort[256];

                    ushort value;
                    ushort temp;

                    for (ushort i = 0; i < 256; ++i)
                    {
                        value = 0;
                        temp = i;
                        for (byte j = 0; j < 8; j++)
                        {
                            if (((value ^ temp) & 0x0001) != 0)
                            {
                                value = (ushort)((value >> 1) ^ CrcPolynomial);
                            }
                            else
                            {
                                value >>= 1;
                            }
                            temp >>= 1;
                        }
                        table[i] = value;
                    }

                    _CrcTable = table;
                }

                return _CrcTable;
            }
        }

        public static ushort HashCRC(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);

            ushort[] table = CrcTable;

            ushort crc = 0xFFF;
            for (int i = 0; i < bytes.Length; ++i)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }

            return crc;
        }

        public static ulong HashSDBM(string str)
        {
            ulong hash = 0;

            for (ulong i = 0, c = (ulong)str.Length; i < c; i++)
            {
                hash = i + (hash << 6) + (hash << 16) - hash;
            }

            return hash;
        }

        #endregion

        ////////////////////////
        // Program Arguments
        ////////////////////////

        #region Args

        public static void GetArgsFromDictionary(ref Dictionary<string, string> args)
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            string[] _args = System.Environment.GetCommandLineArgs();
            List<string> keys = new List<string>(args.Keys);
            for (int i = 0; i < _args.Length; i++)
            {
                foreach (string key in keys)
                {
                    if (string.Format("-{0}", key).Equals(_args[i]))
                    {
                        args[key] = _args[++i];
                    }
                }
            }
#endif
        }

        #endregion
    }
}
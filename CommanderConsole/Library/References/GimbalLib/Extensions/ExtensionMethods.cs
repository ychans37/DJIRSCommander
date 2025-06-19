using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommanderConsole.Extensions
{
    public static class ExtensionMethods
    {
        public static string ByteArrayToHexString(this byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] BitArrayToBytes(this BitArray bitarray)
        {
            if (bitarray.Length == 0)
            {
                throw new System.ArgumentException("must have at least length 1", "bitarray");
            }

            int num_bytes = bitarray.Length / 8;

            if (bitarray.Length % 8 != 0)
            {
                num_bytes += 1;
            }

            var bytes = new byte[num_bytes];
            bitarray.CopyTo(bytes, 0);
            return bytes;
        }

        public static string ToHex(this byte[] bytes)
        {
            char[] c = new char[bytes.Length * 2];

            byte b;

            for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx)
            {
                b = ((byte)(bytes[bx] >> 4));
                c[cx] = (char)(b > 9 ? b - 10 + 'A' : b + '0');

                b = ((byte)(bytes[bx] & 0x0F));
                c[++cx] = (char)(b > 9 ? b - 10 + 'A' : b + '0');
            }

            return new string(c);
        }

        public static string ConvertByteToHexString(this byte[] bytes)
        {
            string convertArrString = string.Empty;
            convertArrString = string.Concat(Array.ConvertAll(bytes, byt => byt.ToString("X2")));
            return convertArrString;
        }

        public static byte[] ConvertHexStringToByte(this string hexString)
        {
            byte[] convertArr = new byte[hexString.Length / 2];

            for (int i = 0; i < convertArr.Length; i++)
            {
                convertArr[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return convertArr;
        }
        /*
        public static byte[] HexStringToByteArray(this string hex_str)
        {
            byte[] convertArr = new byte[hex_str.Length / 2];

            for (int i = 0; i < convertArr.Length; i++)
            {
                convertArr[i] = Convert.ToByte(hex_str.Substring(i * 2, 2), 16);
            }
            return convertArr;
        }
        */

        public static byte[] HexStringToByteArray(this string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}

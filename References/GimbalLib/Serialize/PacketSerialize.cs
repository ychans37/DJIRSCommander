using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace CMDSender.Serialize
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public class HeaderPacket
    {
        [FieldOffset(0)]
        public byte SOF = 0xAA;
        [FieldOffset(1)]
        public ushort LenVer;
        [FieldOffset(3)]
        public byte CmdType;
        [FieldOffset(4)]
        public byte ENC;
        [FieldOffset(5)]
        public byte RES;
        [FieldOffset(8)]
        public ushort SEQ;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public class CommandPacket
    {
        [FieldOffset(0)]
        public byte SOF = 0xAA;
        [FieldOffset(1)]
        public ushort LenVer;
        [FieldOffset(3)]
        public byte CmdType;
        [FieldOffset(4)]
        public byte ENC;
        [FieldOffset(5)]
        public byte RES;
        [FieldOffset(8)]
        public ushort SEQ;
        [FieldOffset(10)]
        public ushort CRC16;
        [FieldOffset(12)]
        public byte CmdSet;
        [FieldOffset(13)]
        public byte CmdID;
        [FieldOffset(14)]
        public byte[]? CmdData;
    }

    // TEST
    public class PacketSerialize
    {
        BinaryFormatter bf = new BinaryFormatter(); // Field
        public PacketSerialize()
        {
            var packet = new HeaderPacket();
            int size = Marshal.SizeOf(typeof(HeaderPacket));
            byte[] buff = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(packet, ptr, true);
            Marshal.Copy(ptr, buff, 0, size);
            Marshal.FreeHGlobal(ptr);

            //Console.WriteLine(buff.Length);
        }
        
        private void Serialize_TryWriteBytes(byte[] buffer, int offset, ushort data)
        {
            BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, sizeof(ushort)), data);
        }
    }
}

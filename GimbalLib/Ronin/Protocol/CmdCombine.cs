
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using System.Runtime.Intrinsics.Arm;
using GimbalLib.Ronin;

namespace DJIRSCommander.Gymbal.Ronin.Protocol
{
    /* DJI R SDK Protocol Description
     *
     * 2.1 Data Format
     * +----------------------------------------------+------+------+------+
     * |                     PREFIX                   | CRC  | DATA | CRC  |
     * |------+----------+-------+------+------+------+------+------+------|
     * |SOF   |Ver/Length|CmdType|ENC   |RES   |SEQ   |CRC-16|DATA  |CRC-32|
     * |------|----------|-------|------|------|------|------|------|------|
     * |1-byte|2-byte    |1-byte |1-byte|3-byte|2-byte|2-byte|n-byte|4-byte|
     * +------+----------+-------+------+------+------+------+------+------+
     *
     * 2.2 Data Segment (field DATA in 2.1 Data Format)
     * +---------------------+
     * |           DATA      |
     * |------+------+-------|
     * |CmdSet|CmdID |CmdData|
     * |------|------|-------|
     * |1-byte|1-byte|n-byte |
     * +------+------+-------+
     */


    public class CmdCombine
    {
        private readonly byte[] verBytes;

        private const byte SOF = 0xAA;
        private const byte RES = 0x00;
        private const int HEADERSIZE = 10;

        ushort seqInit = 2;

        public int GetCurSeqence()
        {
            return seqInit;
        }

        public CmdCombine(ushort ver = 0)
        {
            verBytes = BitConverter.GetBytes(ver);
        }
        
        public byte[] Combine(in byte cmd_type, in byte cmd_set, in byte cmd_id, in byte[] cmd_data)
        {
            /// 데이터 패킷 생성
            if(seqInit >= 65533) 
            {
                seqInit = 2;
            }
            seqInit++;
            var sequeceByte = BitConverter.GetBytes(seqInit);
            int cmdDataLength = 2 + cmd_data.Length;
            ushort totalDataLength = (ushort)(cmdDataLength + 16); // 총 길이            
            byte[] combineDataBuffer = new byte[totalDataLength];

            // LSB First
            BitArray lenBitArray = new BitArray(BitConverter.GetBytes(totalDataLength));
            BitArray verBitArray = new BitArray(verBytes);
            var lenVerByteArray = lenBitArray.Or(verBitArray.LeftShift(10)).BitArrayToBytes();

            /// 헤더 데이터 삽입
            combineDataBuffer[0] = SOF;
            combineDataBuffer[1] = lenVerByteArray[0];
            combineDataBuffer[2] = lenVerByteArray[1];
            combineDataBuffer[3] = cmd_type;
            combineDataBuffer[4] = 0x00; // enc
            combineDataBuffer[5] = 0x00; // res 1
            combineDataBuffer[6] = 0x00; // res 2
            combineDataBuffer[7] = 0x00; // res 3
            combineDataBuffer[8] = sequeceByte[0];
            combineDataBuffer[9] = sequeceByte[1];

            // CRC16 삽입
            byte[] crc16 = BitConverter.GetBytes(SDKCRC.CalCRC16(combineDataBuffer, HEADERSIZE));
            Buffer.BlockCopy(crc16, 0, combineDataBuffer, HEADERSIZE, 2); // crc16

            // 커멘드 데이터 삽입
            combineDataBuffer[12] = cmd_set;
            combineDataBuffer[13] = cmd_id;
            Buffer.BlockCopy(cmd_data, 0, combineDataBuffer, HEADERSIZE + 2 + 2, cmd_data.Length);

            // CRC32 삽입
            byte[] crc32 = BitConverter.GetBytes(SDKCRC.CalCRC32(combineDataBuffer, HEADERSIZE + 2 + cmdDataLength));
            Buffer.BlockCopy(crc32, 0, combineDataBuffer, HEADERSIZE + 2 + cmdDataLength, 4);

            return combineDataBuffer;
        }

    }
}

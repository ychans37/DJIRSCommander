using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommanderConsole.Extensions;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace CommanderConsole.Service
{
    public class CanComm
    {
        const byte STD_DATA = 0x74;     // t
        const byte STD_REMOTE = 0x54;   // T
        const byte EXT_DATA = 0x65;     // e
        const byte EXT_REMOTE = 0x45;   // E

        const int PACKET_CR_LENGTH = 1;
        const int PACKET_STD_HEADER_LENGTH = 5;
        const int PACKET_EXT_HEADER_LENGTH = 10;

        const int PACKET_FORMAT = 0;
        const int PACKET_ID = 1;
        const int PACKET_STD_DLC = 4;
        const int PACKET_EXT_DLC = 9;

        const int FRAME_LEN = 8;

        const int DLC = 8;

        private string sendID = "223";
        private string recvID = "222";
        private byte[] recvIDToByte = new byte[] { };

        private SerialDevice serialDevice;
        byte[]? receiveData;

        ConcurrentQueue<byte[]> recvRS3DataQueue = new ConcurrentQueue<byte[]>();
        public byte[]? GetRecevedData()
        {
            recvRS3DataQueue.TryDequeue(out byte[]? data);
            return data;
        }

        public bool IsExistRS3Data { get { return (recvRS3DataQueue.Count > 0); } }

        public bool IsCanReady { 
            get 
            {
                bool result = false;
                if(serialDevice != null)
                {
                    result = serialDevice.IsOpen();
                }
                return result;
            } 
        }

        public CanComm(string sendID, string recvID, string port, int baudRate = 115200, int dataBit = 8, Parity parity = Parity.None, StopBits stopBit = StopBits.One)
        {
            // MakeSendPacket(new Byte[] {0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 });
            SetId(sendID, recvID);
            // handshake default none
            serialDevice = new SerialDevice(port, baudRate, parity, dataBit, stopBit);
        }

        public bool Init()
        {
            serialDevice.OnReceive += SerialDevice_OnReceive;
            var result = serialDevice.Open();
            return result;
        }

        public bool SendCanFrameMsg(byte[] data)
        {
            bool result = false;
            if (serialDevice.IsOpen())
            {
                int data_len = (int)data.Length;
                int full_frame_num = data_len / FRAME_LEN;
                int left_len = data_len % FRAME_LEN;

                for(int i = 0; i< full_frame_num; i++)
                {
                    byte[] sendBuffer = new byte[FRAME_LEN];
                    Buffer.BlockCopy(data, i * FRAME_LEN, sendBuffer, 0, FRAME_LEN);
                    var packet = MakeSendPacket(sendBuffer);
                    if (packet != null)
                    {
                        serialDevice.Write(packet);
                    }
                }

                if(left_len > 0)
                {
                    byte[] sendBuffer = new byte[left_len];
                    Buffer.BlockCopy(data, (full_frame_num * FRAME_LEN), sendBuffer, 0, left_len);
                    var packet = MakeSendPacket(sendBuffer, left_len);
                    if (packet != null)
                    {
                        serialDevice.Write(packet);
                    }
                }
                result = true;
            }
            return result;
        }

        private void SetId(string sendID, string recvID)
        {
            this.sendID = sendID;
            this.recvID = recvID;
            recvIDToByte = ("0"+recvID).HexStringToByteArray();
        }

        private void SerialDevice_OnReceive(object? sender, SerialDevice.StreamEventArguments e)
        {            
            if(serialDevice.IsOpen()) // 시리얼 연결 확인
            {                
                ProcessData(e.Stream, e.Stream.Length);
            }
        }

        private void ProcessData(byte[] bytes, int length)
        {
            try
            {
                if (serialDevice.IsOpen())
                {
                    if (receiveData == null)
                    {
                        receiveData = bytes;
                    }
                    else
                    {
                        byte[] tempReceiveData = receiveData;                                           // 현재 데이터 저장
                        receiveData = new byte[tempReceiveData.Length + length];                        // 현재 데이터 길이 + 받은 데이터 길이
                        Array.Copy(tempReceiveData, 0, receiveData, 0, tempReceiveData.Length);         // 이전 데이터 복사
                        Array.Copy(bytes, 0, receiveData, tempReceiveData.Length, length);              // 새로 들어온 데이터 복사
                    }
                    MakeReceivePacket();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DataReceived Failed ::: " + ex);
            }
        }

        private void MakeReceivePacket()
        {
            int dataLength;
            
            if(receiveData != null) // 받아 오늘 데이터 형태를 확인해야함
            {
                if (!(receiveData[PACKET_FORMAT] == STD_DATA || receiveData[PACKET_FORMAT] == STD_REMOTE ||
                    receiveData[PACKET_FORMAT] == EXT_DATA || receiveData[PACKET_FORMAT] == EXT_REMOTE))                    // 첫번재 패킷 검사 추후에 STD_DATA 인지만 확인하면 될듯
                {
                    var hexString = Encoding.ASCII.GetString(receiveData);
                    receiveData = null;
                    Console.WriteLine("Packet Reset !!!!!!!!!!!!!!!!!!!!!!!!!!!!!! {0}", hexString);
                }
                else if (receiveData[PACKET_FORMAT] == STD_DATA && receiveData.Length >= PACKET_STD_HEADER_LENGTH)          // STD DATA 프레임
                {
                    // && receiveData[PACKET_ID] == 0
                    // 패킷 아이디 체크 0x222
                    // 0x30 ASCII 0
                    // 0x31 ASCII 1
                    // 0x32 ASCII 2
                    // dataLength 측정 방법 검증 필요함.
                    var availableDataLength = (receiveData[PACKET_STD_DLC] - 0x30) * 2;
                    byte[] availableData = new byte[availableDataLength];
                    dataLength = PACKET_STD_HEADER_LENGTH + (availableDataLength) + PACKET_CR_LENGTH;  // 브릿지가 시리얼이기 때문에 CR

                    if (receiveData.Length == dataLength)
                    {
                        Buffer.BlockCopy(receiveData, PACKET_STD_HEADER_LENGTH, availableData, 0, availableDataLength);     // 실사용 데이터만 잘라냄
                        var hexString = Encoding.ASCII.GetString(availableData);
                        availableData = hexString.ConvertHexStringToByte();
                        recvRS3DataQueue.Enqueue(availableData);
                        receiveData = null;                                                                                 // 버퍼 초기화
                    }
                    else if (receiveData.Length > dataLength)                                                               // 데이터가 더 있을경우
                    {
                        byte[] tempReceiveData = new byte[dataLength];
                        
                        Buffer.BlockCopy(receiveData, 0, tempReceiveData, 0, dataLength);                                   // 완성 프레임 임시 버퍼에 복사
                        Buffer.BlockCopy(receiveData, PACKET_STD_HEADER_LENGTH, availableData, 0, availableDataLength);     // 실사용 데이터만 잘라냄
                        var hexString = Encoding.ASCII.GetString(availableData);
                        availableData = hexString.ConvertHexStringToByte();
                        recvRS3DataQueue.Enqueue(availableData);                                                            // 사용 가능한 데이터 출력
                        // 완성 데이터 필요 부분만 가져오기
                        // 이부분 단위 테스트 필요함
                        
                        tempReceiveData = receiveData;                                                                      // 받은 데이터 전체 임시 버퍼에 저장
                        receiveData = new byte[tempReceiveData.Length - dataLength];                                        // 완성 데이터 외의 Length 측정
                        Array.Copy(tempReceiveData, dataLength, receiveData, 0, receiveData.Length);                        // receiveData에 처리 되지 않은 패킷만 복사
                        MakeReceivePacket();                                                                                // 재귀 호출 이 부분 별도의 Task 또는 Thread로 추후 변경 // 민수
                    }
                }
            }
        }
        
        /// <summary>
        /// Full Frame 용
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected byte[]? MakeSendPacket(byte[] bytes)
        {
            if (!IsCanReady || bytes.Length != 8)
            {
                return null;
            }

            // Full Frame 기준            
            string sendData = ((char)STD_DATA).ToString();
            sendData += sendID;
            sendData += DLC;
            sendData += bytes.ByteArrayToHexString();
            sendData += "\r"; //0x0d

            Console.WriteLine("Make Send Data 1 :: {0}", sendData);

            return Encoding.ASCII.GetBytes(sendData);
        }

        protected byte[]? MakeSendPacket(byte[] bytes, int length)
        {
            if (!IsCanReady)
            {
                return null;
            }
            
            // DLC 길이 만큼의 크기
            string sendData = ((char)STD_DATA).ToString();
            sendData += sendID; // 223 
            sendData += length;
            sendData += bytes.ByteArrayToHexString();
            sendData += "\r";

            Console.WriteLine("Make Send Data 2 :: {0}", sendData);

            return Encoding.ASCII.GetBytes(sendData);
        }

        protected byte[]? MakeSendPacket(string hexStr)
        {
            if(!IsCanReady || hexStr.Length != 16)
            {
                return null;
            }
            // 8Byte 크기에 맞추어서 넣어야 함.            
            string sendData = "";
            // STD_DATA only
            sendData = ((char)STD_DATA).ToString();
            sendData += sendID;
            sendData += DLC;
            sendData += hexStr;
            sendData += "\r";            
            return Encoding.ASCII.GetBytes(sendData);
        }

        public virtual void Close()
        {
            if(serialDevice != null)
            {
                serialDevice.Close();
                serialDevice.Dispose();
            }
        }
    }
}

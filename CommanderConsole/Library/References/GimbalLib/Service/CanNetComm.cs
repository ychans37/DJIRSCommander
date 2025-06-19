using CommanderConsole.Service;
using CommanderConsole.Extensions;
using CommanderConsole.Service.Tcp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static CommanderConsole.Service.CanNetComm;

namespace CommanderConsole.Service
{
    public class CanEventArgs
    {
        public CanEventEnum Event { get; set; }
        public byte[]? Data { get; set; } = null;
        public string Message { get; set; } = "";
    }

    public class CanNetComm
    {
        public enum CanEventEnum : uint
        {
            Unknown = 0,
            Trace,
            ConnectionOk,
            Disconnected,
            Received
        };

        const byte STD_DATA = 0x04;     // t
        const byte STD_REMOTE = 0x05;   // T
        const byte EXT_DATA = 0x06;     // e
        const byte EXT_REMOTE = 0x07;   // E

        const byte ERR_FRAME = 0xFF;   // E

        const int PACKET_CR_LENGTH = 1;
        const int PACKET_STD_HEADER_LENGTH = 5;
        const int PACKET_EXT_HEADER_LENGTH = 10;

        const int PACKET_FORMAT = 0;
        const int PACKET_ID = 1;
        const int PACKET_STD_DLC = 4;
        const int PACKET_EXT_DLC = 9;

        const int TCP_PACKET_COUNT = 14;
        const int TCP_PACKET_HEADER_COUNT = 6;
        const int FRAME_LEN = 8;

        const int DLC = 8;

        private byte[] sendIDBytes = new byte[4];
        private string sendID = "223";
        private string recvID = "222";
        private byte[] recvIDToByte = new byte[] { };

        public string TcpServerAddress = "127.0.0.1";
        public ushort TcpServerPortNumber = 9000;
        public bool Connected = false;
        public string PacketDelimiter = "\r\n";

        private StandardClient mStandardClient;
        private Thread mClientThread;
        private NetworkStream mConnectedNetworkStream;
        private ByteBuffer BB = new ByteBuffer();

        ConcurrentQueue<CanEventArgs> mEventQueue = new ConcurrentQueue<CanEventArgs>();

        public bool IsCanReady
        {
            get
            {
                return Connected;
            }
        }

        public int GetEventCount()
        {
            return mEventQueue.Count;
        }

        public CanEventArgs? GetEvent()
        {
            CanEventArgs? evt = null;
            if (mEventQueue.Count > 0)
                mEventQueue.TryDequeue(out evt);
            
            return evt;
        }

        public CanNetComm(string sendID, string recvID, string addr, ushort port)
        {            
            // MakeSendPacket(new Byte[] {0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 });
            SetId(sendID, recvID);
            // handshake default none
            TcpServerAddress = addr;
            TcpServerPortNumber = port;
        }

        public bool Start()
        {
            bool result = true;
            mStandardClient = new StandardClient(this.OnMessage, this.ConnectionHandler, TcpServerAddress, TcpServerPortNumber); //Uses default host and port and timeouts
            mClientThread = new Thread(this.mStandardClient.Run);
            mClientThread.Start();
            return result;
        }

        protected virtual void ConnectionHandler(NetworkStream connectedAutoDisposedNetStream)
        {
            if (!connectedAutoDisposedNetStream.CanRead && !connectedAutoDisposedNetStream.CanWrite)
            {
                return; //We need to be able to read and write
            }

            mConnectedNetworkStream = connectedAutoDisposedNetStream;

            Connected = true;
            mEventQueue.Enqueue(new CanEventArgs()
            {
                Event = CanEventEnum.ConnectionOk
            });

            var writer = new StreamWriter(connectedAutoDisposedNetStream) { AutoFlush = true };
            var reader = new StreamReader(connectedAutoDisposedNetStream);

            byte[] recvBuffer = new byte[4096];

            while (!this.mStandardClient.ExitSignal) //Tight network message-loop (optional)
            {
                try
                {

                    int length = connectedAutoDisposedNetStream.Read(recvBuffer, 0, recvBuffer.Length);

                    if (length <= 0)
                    {
                        Debug.WriteLine($"RecvBuffer :: ***************************************************** Check {length}");
                        break;
                    }
                    
                    BB.PutByteBlock(recvBuffer, length);

                    while (BB.GetLength() >= TCP_PACKET_COUNT)
                    {
                        byte[] completedPacket = new byte[TCP_PACKET_COUNT];
                        byte[] rs3Packet = new byte[FRAME_LEN];
                        BB.PopByteBlock(ref completedPacket, TCP_PACKET_COUNT);
                        Buffer.BlockCopy(completedPacket, TCP_PACKET_HEADER_COUNT, rs3Packet, 0, FRAME_LEN);

                        mEventQueue.Enqueue(new CanEventArgs()
                        {
                            Event = CanEventEnum.Received,
                            Data = rs3Packet
                        });
                    }
                }
                catch (IOException ex)
                {
                    Connected = false;
                    _ = ex;
                    //this.OnHostMessages.Invoke(HostEvent.Trace, ex.ToString());
                    Debug.WriteLine($"RecvBuffer !! :: ***************************************************** Check {ex.Message}");
                    break;
                }
            }

            Connected = false;

            mConnectedNetworkStream = null;

            mEventQueue.Enqueue(new CanEventArgs()
            {
                Event = CanEventEnum.Disconnected
            });
        }

        public bool WriteMessage(string message)
        {
            byte[] msg = Encoding.Default.GetBytes(message);
            return WriteBytes(msg);
        }

        public bool WriteBytes(byte[] data)
        {
            if (mConnectedNetworkStream != null && Connected)
            {
                try
                {
                    mConnectedNetworkStream.Write(data, 0, data.Length);
                    mConnectedNetworkStream.Flush();
                }
                catch (Exception ex)
                {
                    _ = ex;
                    Debug.WriteLine($"Socket - 03 Exception {ex.Message}");
                    return false;
                }

                return true;
            }

            return false;
        }

        protected virtual void OnMessage(string message)
        {
            Debug.WriteLine($"OnMessage :: {message}");
            mEventQueue.Enqueue(new CanEventArgs()
            {
                Event = CanEventEnum.Trace,
                Message = message
            });
        }

        public bool SendCanFrameMsg(byte[] data)
        {
            bool result = false;
            if (Connected)
            {
                int data_len = (int)data.Length;
                int full_frame_num = data_len / FRAME_LEN;
                int left_len = data_len % FRAME_LEN;

                for (int i = 0; i < full_frame_num; i++)
                {
                    byte[] sendBuffer = new byte[FRAME_LEN];
                    Buffer.BlockCopy(data, i * FRAME_LEN, sendBuffer, 0, FRAME_LEN);
                    var packet = MakeSendPacket(sendBuffer);
                    if (packet != null)
                    {
                        WriteBytes(packet);
                    }
                }

                if (left_len > 0)
                {
                    byte[] sendBuffer = new byte[left_len];
                    Buffer.BlockCopy(data, (full_frame_num * FRAME_LEN), sendBuffer, 0, left_len);
                    var packet = MakeSendPacket(sendBuffer, left_len);
                    if (packet != null)
                    {
                        WriteBytes(packet);
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
            sendIDBytes = ("0" + sendID).HexStringToByteArray();
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

            byte[] packet = new byte[14];

            packet[0] = STD_DATA;
            packet[3] = sendIDBytes[0];
            packet[4] = sendIDBytes[1];
            packet[5] = DLC;
            Buffer.BlockCopy(bytes, 0, packet, 6, 8);
            /*
            // Full Frame 기준            
            string sendData = ((char)STD_DATA).ToString();
            sendData += sendID;
            sendData += DLC;
            sendData += bytes.ByteArrayToHexString();
            */
            
            return packet;
        }

        protected byte[]? MakeSendPacket(byte[] bytes, int length)
        {
            if (!IsCanReady)
            {
                return null;
            }

            byte[] packet = new byte[14];

            packet[0] = STD_DATA;
            packet[3] = sendIDBytes[0];
            packet[4] = sendIDBytes[1];
            packet[5] = DLC;

            Buffer.BlockCopy(bytes, 0, packet, 6, length);
            // DLC 길이 만큼의 크기
            /*
            string sendData = ((char)STD_DATA).ToString();
            sendData += sendID;
            sendData += length;
            sendData += bytes.ByteArrayToHexString();            
            Console.WriteLine("Make Send Data 2 :: {0}", sendData);

            return Encoding.ASCII.GetBytes(sendData);
            */
            return packet;
        }

        protected byte[]? MakeSendPacket(string hexStr)
        {
            if (!IsCanReady || hexStr.Length != 16)
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
            return Encoding.ASCII.GetBytes(sendData);
        }

        public void Dispose()
        {
            //mStandardClient.
            if (mConnectedNetworkStream != null)
            {
                mConnectedNetworkStream.Close();
            }

            if (mStandardClient != null)
            {
                mStandardClient.ExitSignal = true;
            }

            if (mClientThread != null)
            {
                mClientThread.Join();
            }

            BB.Clear();
        }

        public virtual void Close()
        {
            Dispose();
        }
    }
}

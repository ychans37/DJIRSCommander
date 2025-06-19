using CommanderConsole.Extensions;
using CommanderConsole.Service.Tcp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CommanderConsole.Service.Tcp
{
    public class TcpClientHost
    {

        public enum HostEventEnum : uint
        {
            Unknown = 0,
            Trace,
            ConnectionOk,
            Disconnected,
            Receive
        };

        public class HostEventData
        {
            public HostEventEnum eventEnum { get; set; }
            public string eventMessage { get; set; }
        }


        [System.Serializable]
        public delegate bool HostEventDelegate(int code, string message);

        public string TcpServerAddress = "127.0.0.1";
        public ushort TcpServerPortNumber = 9000;
        public bool Connected = false;
        public string PacketDelimiter = "\r\n";
        //public HostEventDelegate OnHostEventHandler;

        private StandardClient mStandardClient;
        private Thread mClientThread;
        private NetworkStream mConnectedNetworkStream;
        private byte[] mPacketDelimiter;
        ConcurrentQueue<HostEventData> mEventQueue = new System.Collections.Concurrent.ConcurrentQueue<HostEventData>();


        public void Awake()
        {
            TcpServerAddress = "192.168.10.223";
            TcpServerPortNumber = (ushort)4001;

            string tmpPacketDelimiter = PacketDelimiter;
            tmpPacketDelimiter = tmpPacketDelimiter.Replace("\\r", "\r").Replace("\\n", "\n");

            mPacketDelimiter = Encoding.Default.GetBytes(tmpPacketDelimiter);
        }
        // Start is called before the first frame update
        public void Start()
        {
            mStandardClient = new StandardClient(this.OnMessage, this.ConnectionHandler, TcpServerAddress, TcpServerPortNumber); //Uses default host and port and timeouts
            mClientThread = new Thread(this.mStandardClient.Run);

            mClientThread.Start();
        }

        // Update is called once per frame

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
        }

        protected virtual void ConnectionHandler(NetworkStream connectedAutoDisposedNetStream)
        {
            if (!connectedAutoDisposedNetStream.CanRead && !connectedAutoDisposedNetStream.CanWrite)
            {
                return; //We need to be able to read and write
            }

            mConnectedNetworkStream = connectedAutoDisposedNetStream;

            Connected = true;
            mEventQueue.Enqueue(new HostEventData()
            {
                eventEnum = HostEventEnum.ConnectionOk,
                eventMessage = "Connection Ok"
            });

            var writer = new StreamWriter(connectedAutoDisposedNetStream) { AutoFlush = true };
            var reader = new StreamReader(connectedAutoDisposedNetStream);

            ByteBuffer BB = new ByteBuffer();
            byte[] recvBuffer = new byte[4096];

            while (!this.mStandardClient.ExitSignal) //Tight network message-loop (optional)
            {
                try
                {
                    int length = connectedAutoDisposedNetStream.Read(recvBuffer, 0, recvBuffer.Length);

                    if (length <= 0)
                    {
                        break;
                    }

                    Console.WriteLine(length);
                }
                catch (IOException ex)
                {
                    _ = ex;
                    //this.OnHostMessages.Invoke(HostEvent.Trace, ex.ToString());
                    break;
                }
            }

            Connected = false;

            mConnectedNetworkStream = null;

            mEventQueue.Enqueue(new HostEventData()
            {
                eventEnum = HostEventEnum.Disconnected,
                eventMessage = "Disconnected"
            });
        }

        public bool WriteMessage(string message)
        {
            byte[] msg = Encoding.Default.GetBytes(message + Encoding.Default.GetString(mPacketDelimiter));
            if (mConnectedNetworkStream != null)
            {
                try
                {
                    mConnectedNetworkStream.Write(msg, 0, msg.Length);
                    mConnectedNetworkStream.Flush();
                }
                catch (Exception ex)
                {
                    _ = ex;

                    return false;
                }

                return true;
            }

            return false;
        }

        protected virtual void OnMessage(string message)
        {
            mEventQueue.Enqueue(new HostEventData()
            {
                eventEnum = HostEventEnum.Trace,
                eventMessage = message
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommanderConsole.Service
{
    public class SerialDevice : IDisposable
    {
        public struct SerialConnectionInfo
        {
            public string PortName { get; set; }
            public int Baudrate { get; set; }

            public SerialConnectionInfo(string port = "COM1", int baudRate = 19200)
            {
                PortName = port;
                Baudrate = baudRate;
            }
        }

        public class StreamEventArguments
        {
            public int Status { get; set; }

            public byte[] Stream { get; }

            public StreamEventArguments(byte[] buffer)
            {
                this.Status = 0;
                this.Stream = buffer;
            }
        }

        // private static readonly Logger ClassLogger = LogManager.GetCurrentClassLogger();
        private static int[] _serialBaudrates = new int[4]
        {
            9600,
            19200,
            38400,
            115200
        };

        private static string[] _serialParities = new string[3]
        {
            "None",
            "Even",
            "Odd"
        };
        public static Parity[] ParityValues = new Parity[3]
        {
            Parity.None,
            Parity.Even,
            Parity.Odd
        };
        private static int[] _serialDataBits = new int[2]
        {
            7,
            8
        };
        private static string[] _serialStopBits = new string[3]
        {
            "1",
            "2",
            "1.5"
        };
        public static StopBits[] StopBitValues = new StopBits[3]
        {
            StopBits.One,
            StopBits.Two,
            StopBits.OnePointFive
        };
        public static string[] StopBitConfigValues = new string[3]
        {
            "One",
            "Two",
            "OnePointFive"
        };

        private volatile bool _isStop = true;
        private int _dataBit = 8;
        private StopBits _stopBit = StopBits.One;
        private const int CriticalLimit = 20;
        private string _port;
        private int _baudRate;
        private Parity _parity;
        private SerialPort _serialPort;
        private Thread _thread;
        private double _packetRate;
        private DateTime _lastReceiveTime;

        public static int[] SerialBaudrates
        {
            get
            {
                return SerialDevice._serialBaudrates;
            }
        }

        public static string[] SerialParities
        {
            get
            {
                return SerialDevice._serialParities;
            }
        }

        public static int[] SerialDataBits
        {
            get
            {
                return SerialDevice._serialDataBits;
            }
        }

        public static string[] SerialStopBits
        {
            get
            {
                return SerialDevice._serialStopBits;
            }
        }

        public SerialDevice(string port = "COM1", int baudRate = 19200, Parity parity = Parity.None, int dataBit = 8, StopBits stopBit = StopBits.One)
        {
            this._port = port;
            this._baudRate = baudRate;
            this._dataBit = dataBit;
            this._stopBit = stopBit;
            this._parity = parity;
            this._lastReceiveTime = DateTime.MinValue;
        }

        public event EventHandler<StreamEventArguments> OnReceive;

        public string Port
        {
            get
            {
                return this._port;
            }
        }

        public int BaudRate
        {
            get
            {
                return this._baudRate;
            }
        }

        public StopBits StopBit
        {
            get
            {
                return this._stopBit;
            }
        }

        public Parity ParityBit
        {
            get
            {
                return this._parity;
            }
        }

        public int DataBit
        {
            get
            {
                return this._dataBit;
            }
        }

        private void CreateReceiveThread()
        {
            this._thread = new Thread(new ThreadStart(this.DoReceiveThread));
            this._thread.Priority = ThreadPriority.Normal;
            this._thread.Name = string.Format("[SerialThread-{0}]", (object)this._thread.ManagedThreadId);
            this._thread.Start();
        }

        public static string[] GetPortList()
        {
            try
            {
                return SerialPort.GetPortNames();
            }
            catch (Win32Exception ex)
            {
                // SerialDevice.ClassLogger.Error(ex.Message);
                return (string[])null;
            }
        }

        public override string ToString()
        {
            return string.Format("[Port: {0}, Baudrate: {1}]", (object)this._port, (object)this._baudRate);
        }

        public static int GetBaudrateIndex(int baudrate)
        {
            for (int index = 0; index < SerialDevice._serialBaudrates.Length; ++index)
            {
                if (baudrate == SerialDevice._serialBaudrates[index])
                    return index;
            }
            return 0;
        }

        public static int GetDataBitIndex(int dataBit)
        {
            for (int index = 0; index < SerialDevice._serialDataBits.Length; ++index)
            {
                if (dataBit == SerialDevice._serialDataBits[index])
                    return index;
            }
            return 0;
        }

        public static int GetParityIndex(Parity parity)
        {
            for (int index = 0; index < SerialDevice.ParityValues.Length; ++index)
            {
                if (parity == SerialDevice.ParityValues[index])
                    return index;
            }
            return 0;
        }

        public static int GetStopBitIndex(StopBits stopBits)
        {
            for (int index = 0; index < SerialDevice.StopBitValues.Length; ++index)
            {
                if (stopBits == SerialDevice.StopBitValues[index])
                    return index;
            }
            return 0;
        }

        public bool Open()
        {
            try
            {
                _isStop = true;

                if (this._serialPort == null)
                {
                    this._serialPort = new SerialPort(this._port, this._baudRate, this._parity, this._dataBit, this._stopBit);
                }

                if (!this._serialPort.IsOpen)
                {
                    this._serialPort.ReadTimeout = -1;
                    this._serialPort.WriteTimeout = -1;
                    this._serialPort.Open();
                    if (!this._serialPort.IsOpen)
                    {
                        Debug.WriteLine("Serial Already Open");
                        return false;
                    }

                    this.CreateReceiveThread();
                }
            }
            catch (Exception ex)
            {
                //SerialDevice.ClassLogger.Error(ex.Message);
                Debug.WriteLine(ex.Message);
                return false;
            }
            Debug.WriteLine("Serial Open Success {0}, {1}", _port, _baudRate);
            return true;
        }

        public bool DiscardInBuffer()
        {
            bool result = false;
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.DiscardInBuffer();
                result = true;
            }
            return result;
        }

        public bool Open(string port, int baudRate, Parity parity = Parity.None, int dataBit = 8, StopBits stopBit = StopBits.One)
        {
            this._port = port;
            this._baudRate = baudRate;
            this._dataBit = dataBit;
            this._parity = parity;
            this._stopBit = stopBit;

            return this.Open();
        }

        public void Close()
        {
            _isStop = false;

            if (this._thread != null)
            {
                this._thread.Join();
            }
            if (this._serialPort != null && this._serialPort.IsOpen)
            {
                this._serialPort.Close();
            }
        }

        public bool IsOpen()
        {
            return this._serialPort != null && this._serialPort.IsOpen;
        }

        public bool Reset()
        {
            this.Close();
            return this.Open();
        }

        public bool Reset(string port, int baudRate)
        {
            this.Close();
            return this.Open(port, baudRate, Parity.None, 8, StopBits.One);
        }

        public void Write(byte[] packet)
        {
            Console.WriteLine("#0 Write To Serial : " + packet.Length);
            this._serialPort.Write(packet, 0, packet.Length);
        }

        public void Write(byte[] packet, int length)
        {
            Console.WriteLine("#1 Write To Serial : " + packet.Length);
            this._serialPort.Write(packet, 0, length);
        }

        public int Read(byte[] bytes, int offset, int count)
        {
            int num = 0;
            if (count > 0)
            {
                num = this._serialPort.Read(bytes, offset, count);
            }
            return num;
        }

        public void Dispose()
        {
            this.Close();
            if (this._serialPort == null)
            {
                return;
            }

            this._serialPort.Dispose();
            this._serialPort = null;
        }

        private void DoReceiveThread()
        {
            while (this._isStop)
            {
                int bytesToRead = this._serialPort.BytesToRead;
                TimeSpan timeSpan = DateTime.Now - this._lastReceiveTime;
                byte[] numArray = new byte[bytesToRead];
                int num = this.Read(numArray, 0, bytesToRead);
                if (num > 0)
                {
                    //Console.WriteLine(Encoding.ASCII.GetString(numArray));
                    this.OnReceiving(numArray);
                }

                this._packetRate = (this._packetRate + (double)num) / 2.0;
                this._lastReceiveTime = DateTime.Now;
                if ((double)(num + this._serialPort.BytesToRead) / 2.0 <= this._packetRate && timeSpan.Milliseconds > 0)
                {
                    Thread.Sleep(timeSpan.Milliseconds > 20 ? 20 : timeSpan.Milliseconds);
                }
            }
        }

        private void OnReceiving(byte[] res)
        {
            EventHandler<StreamEventArguments> onReceive = this.OnReceive;
            if (onReceive == null)
            {
                return;
            }

            onReceive((object)this, new StreamEventArguments(res));
        }

        public void StopWork()
        {
            this._isStop = false;
        }

        public void Log()
        {
            string.Format("Port: {0}, Baudrate: {1}, Parity: {2}, Data: {3}, Stop: {4}", (object)this.Port, (object)this.BaudRate, (object)this.ParityBit.ToString(), (object)this.DataBit, (object)this.StopBit.ToString());
        }
    }
}

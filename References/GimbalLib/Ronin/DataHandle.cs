using CMDSender.Model;
using CMDSender.Service;
using Extensions;
using GimbalLib.Ronin;
using GimbalLib.Service;
using Microsoft.VisualBasic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using static CMDSender.Gymbal.Ronin.RoninComm;

namespace CMDSender.Gymbal.Ronin
{
    public class DataHandle
    {
        Thread? __recvProcessThread;
        bool isRunning = false;
        const int FRAME_LEN = 8;
        public bool IsRunning { get { return isRunning; } }
        object commnadLockObject = new object();
        private List<byte[]> cmdList = new List<byte[]>();
        private CanNetComm canComm; // dev

        public event EventHandler<CommonEventArgs> OnReceived;

        public DataHandle(CanNetComm canComm)
        {
            this.canComm = canComm;
        }

        public void Init()
        {
            isRunning = true;
            __recvProcessThread = new Thread(new ThreadStart(DoRecvProcess));
            this.__recvProcessThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
            if (__recvProcessThread != null)
            {
                __recvProcessThread.Join();
            }
            canComm = null;
        }

        public void AddCommand(byte[] cmd)
        {
            lock(commnadLockObject)
            {
                cmdList.Add(cmd);
                // 커멘드를 10개 이상은 가지고 있지 않는다 나머지 버림
                // 왜 버리는지는 모르겠음.
                if (cmdList.Count > 10) 
                {
                    byte[]? result = cmdList[0];
                    cmdList.RemoveAt(0);
                    if (result == null)
                    {
                        Console.WriteLine("Dequeue failed");
                    }
                    if(result != null)
                    {
                        Console.WriteLine($"10 Over Dequeue Data {result.ByteArrayToHexString()}");
                    }
                }
            }
        }

        private void DoRecvProcess()
        {
            Queue<byte> v1_pack_list = new Queue<byte>();
            int pack_len = 0;
            int step = 0;
            
            while (isRunning)
            {
                if(canComm.GetEventCount() > 0)
                {
                    var recvEvent = canComm.GetEvent();
                    byte[]? recvData = null;
                    if (recvEvent != null)
                    {
                        recvData = recvEvent.Data;
                    }
                    
                    if (recvData != null)
                    {
                        for (int i = 0; i<recvData.Length; i++ )
                        {
                            if (step == 0)
                            {
                                if (recvData[i] == 0xAA)
                                {
                                    v1_pack_list.Enqueue(recvData[i]);
                                    step = 1;
                                }
                            }
                            else if (step == 1) // packet length 1
                            {
                                pack_len = recvData[i];
                                v1_pack_list.Enqueue(recvData[i]);
                                step = 2;
                            }
                            else if (step == 2) // packet length 2
                            {
                                pack_len |= (recvData[i] & 0x3) << 8; // 총 10비트의 패킷 길이 버전 pass
                                v1_pack_list.Enqueue(recvData[i]);
                                step = 3; 
                            }                            
                            else if (step == 3)
                            {
                                v1_pack_list.Enqueue(recvData[i]);
                                if (v1_pack_list.Count == 12) // 헤더가 완료되면
                                {
                                    //Console.WriteLine(v1_pack_list.ToArray());
                                    if (SDKCRC.CheckHeadCRC(v1_pack_list.ToArray()))
                                    {
                                        //Console.WriteLine("Header Complete");
                                        step = 4;
                                    }
                                    else
                                    {
                                        step = 0;
                                        v1_pack_list.Clear();
                                    }
                                }
                            }
                            else if (step == 4)
                            {
                                v1_pack_list.Enqueue(recvData[i]);
                                if (v1_pack_list.Count == pack_len)
                                {
                                    step = 0;
                                    if (SDKCRC.CheckPackCRC(v1_pack_list.ToArray()))
                                    {
                                        ProcessCMD(v1_pack_list.ToArray()); // 패킷 Validation 마무리
                                    }
                                    v1_pack_list.Clear();
                                }
                            }
                            else
                            {
                                step = 0;
                                v1_pack_list.Clear();
                            }
                        }
                    }
                }
                Thread.Sleep(100); // 100 ms 주기
            }
        }

        /// <summary>
        /// 파싱 패킷
        /// </summary>
        /// <param name="data"></param>
        void ProcessCMD(byte[] data)
        {
            byte cmd_type = (byte)data[3];
            ConsoleColor color = ConsoleColor.Yellow;
            if (cmd_type == 0x20)
            {
                color = data[14] == 0x00 ? ConsoleColor.Green : ConsoleColor.Red;
            }
            Console.ForegroundColor = color;
            Console.WriteLine($"RX: {data.ByteArrayToHexString()}");
            Console.ResetColor();

            bool is_ok = false;
            byte[] cmd_key = new byte[2] {0,0};

            // If it is a response frame, need to check the corresponding send command
            if (cmd_type == 0x20)
            {
                lock (commnadLockObject)
                {
                    for (int i = 0; i < cmdList.Count; i++)
                    {
                        byte[] cmd = cmdList[i];
                        if (cmd.Length >= 10)
                        {
                            UInt16 last_cmd_seq = BitConverter.ToUInt16(new byte[] { cmd[8], cmd[9] });
                            UInt16 data_seq = BitConverter.ToUInt16(new byte[] { data[8], data[9] });
                            if (last_cmd_seq == data_seq)
                            {
                                cmd_key[0] = cmd[12];
                                cmd_key[1] = cmd[13];
                                //  사용된 데이터 지우기
                                cmdList.RemoveAt(i);
                                is_ok = true;
                                break;
                            }
                        }
                    }
                }
            }
            else // 응답 타입이 아닐경우
            {
                cmd_key[0] = data[12];
                cmd_key[1] = data[13];
                is_ok = true;
            }

            UInt16 cmd_key_value = BitConverter.ToUInt16(cmd_key);
            
            if (is_ok)
            {
                switch (cmd_key_value)
                {
                    case 0x000e: // 2.3.4.1 Handheld Gimbal Position Control
                        {
                            Console.WriteLine($"Set Position control : {ReturnCodeResult(data[14])}");
                            break;
                        }
                    case 0x010e: // 2.3.4.1 Handheld Gimbal Position Control
                        {
                            Console.WriteLine($"Set Speed control : {ReturnCodeResult(data[14])}");
                            break;
                        }
                    case 0x020e: // 2.3.4.3 Obtain Handheld Gimbal Information (GetAngle 함수)
                        {
                            var yaw = BitConverter.ToInt16(new byte[] { data[16], data[17] });
                            var roll = BitConverter.ToInt16(new byte[] { data[18], data[19] });
                            var pitch = BitConverter.ToInt16(new byte[] { data[20], data[21] });
                            CmdResultEventArgs info = new CmdResultEventArgs();
                            if (data[15] == 0x01)
                            {
                                // Attitude
                                info.Command = (COMMAND)data[13];
                                info.Data = new AttitudeAngle(yaw, roll, pitch);
                                info.Param = data[15];
                            }
                            else if (data[15] == 0x02)
                            {
                                // Joint
                                info.Command = (COMMAND)data[13];
                                info.Data = new JointAngle(yaw, roll, pitch);
                                info.Param = data[15];
                            }
                            else
                            {
                                // not ready
                            }
                            OnReceived(this, (CommonEventArgs)info);
                            break;
                        }
                    case 0x070e: // Set information push of gimbal parameters. (Enable / Disable)
                        {
                            var cmd_info_str = $"[CMD :{cmd_key_value}]  Set information push of handheld gimbal parameter {ReturnCodeResult(data[14])}";
                            break;
                        }
                    case 0x080e: // Gimbal parameter push.
                        {
                            byte ctrl_byte = data[14];
                            Int16 yaw = BitConverter.ToInt16(new byte[] { data[15], data[16] });
                            Int16 roll = BitConverter.ToInt16(new byte[] { data[17], data[18] });
                            Int16 pitch = BitConverter.ToInt16(new byte[] { data[19], data[20] });

                            Int16 yaw_joint_angle = BitConverter.ToInt16(new byte[] { data[21], data[22] });
                            Int16 roll_joint_angle = BitConverter.ToInt16(new byte[] { data[23], data[24] });
                            Int16 pitch_joint_angle = BitConverter.ToInt16(new byte[] { data[25], data[26] });

                            byte pitch_max = data[27];
                            byte pitch_min = data[28];
                            byte yew_max = data[29];
                            byte yew_min = data[30];
                            byte roll_max = data[31];
                            byte roll_min = data[32];

                            byte pitch_stiffness = data[33];
                            byte yew_stiffness = data[34];
                            byte roll_stiffness = data[35];

                            // Debug.WriteLine($"!!!!!!!!!! Push Parameter Update : {DateTime.Now.ToString("ss.fff")}");

                            PushParameterEventArgs info = new PushParameterEventArgs()
                            {
                                Command = (COMMAND)data[13],
                                ParameterDataItem = new PushGimbalParameterDataItem(yaw, roll, pitch, yaw_joint_angle, roll_joint_angle, pitch_joint_angle, yew_max, yew_min, roll_max, roll_min, pitch_max, pitch_min, pitch_stiffness, yew_stiffness, roll_stiffness)
                            };

                            OnReceived(this, (CommonEventArgs)info);
                            break;
                        }
                    case 0x120e: // Focus Motor Position Control
                        { 
                            byte command_sub_id = data[15];
                            int motor_type = data[16];

                            int endpoints_calibration_status = data[17]; // 0x01: No calibration, 0x02: Calibrating, 0x03: Calibration complete

                            Int32 focus_motor_current_position = BitConverter.ToInt32(new byte[] { data[18], data[19], data[20], data[21] });

                            CommonEventArgs info = new CommonEventArgs();
                            info.Command = (COMMAND)data[13];
                            info.Data = focus_motor_current_position;
                            info.Param = command_sub_id;

                            OnReceived(this, (CommonEventArgs)info);

                            break;
                        }
                        
                    default:
                        {
                            Console.WriteLine("get unknown request\n");
                            
                            break;
                        }
                }
            }
        }

        private string ReturnCodeResult(byte return_code)
        {
            string result_string = "Unknown";

            switch(return_code)
            {
                case 0x00:
                    {
                        result_string = "Command execution succeed";
                        break;
                    }
                case 0x01:
                    {
                        result_string = "Command parse error";
                        break;
                    }
                case 0x02:
                    {
                        result_string = "Command execution fail";
                        break;
                    }
                case 0xFF:
                    {
                        result_string = "Undefined error";
                        break;
                    }
            }
            return result_string;
        }
        
        public class CommonEventArgs
        {
            public COMMAND Command { get; set; }
            public object? Data { get; set; }
            public object? Param { get; set; }
        }

        public class CmdResultEventArgs : CommonEventArgs
        {
            public byte ResultCode { get; set; }
            public string ResultString { get; set; } = "";
        }

        public class PushParameterEventArgs : CommonEventArgs
        {
            public PushGimbalParameterDataItem? ParameterDataItem { get; set; }
        }
    }
}

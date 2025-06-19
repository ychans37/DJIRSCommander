using System;
using System.Collections;
using Extensions;
using CMDSender.Service;
using CMDSender.Gymbal.Ronin.Protocol;
using static CMDSender.Service.SerialDevice;
using CMDSender.Model;
using System.ComponentModel.Design.Serialization;
using System.Formats.Asn1;
using System.Net.Http.Headers;
using GimbalLib.Service;
using System.Diagnostics;

namespace CMDSender.Gymbal.Ronin
{
    public enum ReturnCode
    {
        EXECUTION_SUCCESSFUL = 0,
        PARSE_ERROR,
        EXECUTION_FAILS,
        // UndefinedError 0xFF
    };

    public enum AxisType
    {
        YAW = 0,
        ROLL,
        PITCH
    };

    public enum MoveMode
    {
        INCREMENTAL_CONTROL = 0,
        ABSOLUTE_CONTROL
    };

    public enum SpeedControl
    {
        DISABLED = 0,
        ENABLED = 1
    };

    public enum FocalControl
    {
        ENABLED = 0,
        DISABLED = 1
    };

    enum FLAG : byte
    {
        BIT1 = 0x01,
        BIT2 = 0x02,
        BIT3 = 0x04,
        BIT4 = 0x08,
        BIT5 = 0x10,
        BIT6 = 0x20,
        BIT7 = 0x40
    };

    public enum COMMAND
    {
        SET_POSITION = 0x00,
        SET_SPEED = 0x01,
        GET_ANGLE_INFOMATION = 0x02,
        SET_LIMIT_ANGLE = 0x03,
        GET_LIMIT_ANGLE = 0x04,
        SET_MOTOR_STIFFNESS = 0x05,
        GET_MOTOR_STIFFNESS = 0x06,
        SET_INFO_PARAMETER = 0x07,
        PUSH_INFO_PARAMETER = 0x08, // PUSH TYPE
        GET_VERSION = 0x09,
        PUSH_JOYSTICK_COMMAND = 0x0A, // PUSH TYPE
        GET_USER_PARAMETER = 0x0B,
        SET_USER_PARAMETER = 0x0C,
        SET_OPERATING_MODE = 0x0D,
        SET_RSF_MODE = 0x0E, // Recenter, Selfie, Follow
        FOCUSMOTOR_CONTROL = 0x12, // Has Sub ID
    }

    public enum COMMAND_SUB_ID
    {
        SET_FOCUSMOTOR_POSITION = 0x01,
        FOCUSMOTOR_CALIBRATION = 0x02,
        GET_FOCUSMOTOR_POSITION = 0x15,
    }

    public class RoninComm : IDisposable
    {
        private bool disposedValue;
        public CmdCombine cmdCombine;

        public ushort yawSpeedValue { get; set; } = 30;                      // Unit: 0.1°/s (range: 0°/s to 360°/s
        public ushort rollSpeedValue { get; set; } = 30;                     // Unit: 0.1°/s (range: 0°/s to 360°/s
        public ushort pitchSpeedValue { get; set; } = 30;                    // Unit: 0.1°/s (range: 0°/s to 360°/s

        public short yewValue { get; set; } = 0;                              // yaw angle, unit: 0.1° (range: -1800 to +1800)
        public short rollValue { get; set; } = 0;                             // roll angle, unit: 0.1° (range: -1800 to +1800)
        public short pitchValue { get; set; } = 0;                            // pitch angle, unit: 0.1° (range: -1800 to +1800)

        protected CanNetComm canComm;
        protected DataHandle dataHandle;

        private byte positionCtrlByte;
        private byte speedCtrlByte;

        private Entirelnformation info;
        public Entirelnformation Info
        {
            get { return info; }
        }


        public RoninComm(ushort ver = 0)
        {
            positionCtrlByte = 0;
            speedCtrlByte = 0;

            positionCtrlByte |= (byte)FLAG.BIT1; //MoveMode - ABSOLUTE_CONTROL
            
            speedCtrlByte |= (byte)FLAG.BIT3;    //SpeedControl - DISABLED, FocalControl - DISABLED

            cmdCombine = new CmdCombine(ver);
            info = new Entirelnformation();
        }

        public bool Connect(string ipAddr, ushort port)
        {
            canComm = new CanNetComm("223", "222", ipAddr, port);
            dataHandle = new DataHandle(canComm);
            dataHandle.OnReceived += DataHandle_OnReceived;
            
            if (canComm.Start())
            {
                Console.WriteLine("CanComm Init Success");
                dataHandle.Init();
                return true;
            }
            else
            {
                Console.WriteLine("CanComm Init Fail");
                return false;
            }
        }

        protected virtual void DataHandle_OnReceived(object? sender, DataHandle.CommonEventArgs e)
        {
            switch (e.Command)
            {
                case COMMAND.PUSH_INFO_PARAMETER:
                    var evt = e as DataHandle.PushParameterEventArgs;
                    if(evt != null && evt.ParameterDataItem != null)
                    {
                        info.CopyValueFromObject(evt.ParameterDataItem);
                    }
                    break;

                case COMMAND.GET_ANGLE_INFOMATION:
                    if (e.Param != null)
                    {
                        if ((byte)e.Param == 0x01) // attitude
                        {
                            info.AttitudeAngle.CopyValueFromObject(e.Data as AttitudeAngle);
                        }
                        else if ((byte)e.Param == 0x02) // joint
                        {
                            info.JointAngle.CopyValueFromObject(e.Data as JointAngle);
                        }
                    }
                    break;

                case COMMAND.FOCUSMOTOR_CONTROL:
                    if (e.Param != null)
                    {
                        if ((byte)e.Param == 0x15) // Get Focus Motor Position
                        {
                            info.FocusMotorPosition = (int)e.Data;
                        }
                    }
                    
                    break;
            }
        }

        // 2.3.4.1 Handheld Gimbal Position Control
        public bool MoveTo(short yaw, short roll, short pitch, short time_ms)
        {
            byte cmd_type = 0x00; //  Reply is required after data is sent 추후 변경
            byte cmd_set = 0x0E;
            byte cmd_id = 0x00;
            byte time = (byte)(time_ms / 100); // Command execution speed, unit: 0.1

            byte[] yaw_bytes = BitConverter.GetBytes(yaw);
            byte[] roll_bytes = BitConverter.GetBytes(roll);
            byte[] pitch_bytes = BitConverter.GetBytes(pitch);

            byte[] data_payload = new byte[]
            {
                yaw_bytes[0], yaw_bytes[1],
                roll_bytes[0], roll_bytes[1],
                pitch_bytes[0], pitch_bytes[1],
                positionCtrlByte, time
            };
            /*
            var cmd = cmdCombine.Combine(cmd_type, cmd_set, cmd_id, data_payload);
            dataHandle.AddCommand(cmd);
            bool ret = canComm.SendCanFrameMsg(cmd);
            return ret;
            */
            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.2 Handheld Gimbal Speed Control
        public bool SetSpeed(short yaw_speed, short roll_speed, short pitch_speed)
        {
            byte cmd_type = 0x00; //  Reply is required after data is sent 추후 변경
            byte cmd_set = 0x0E;
            byte cmd_id = 0x01;
            
            byte[] yaw_speed_bytes = BitConverter.GetBytes(yaw_speed);
            byte[] roll_speed_bytes = BitConverter.GetBytes(roll_speed);
            byte[] pitch_speed_bytes = BitConverter.GetBytes(pitch_speed);

            byte[] data_payload = new byte[]
            {
                yaw_speed_bytes[0], yaw_speed_bytes[1],
                roll_speed_bytes[0], roll_speed_bytes[1],
                pitch_speed_bytes[0], pitch_speed_bytes[1],
                0x88//speedCtrlByte    // preset 0x11
            };
            
            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.3 Handheld Gimbal Information Obtaining
        public bool GetAngle(bool is_attitude = true)
        {
            byte cmd_type = 0x03; //  Reply is required after data is sent 추후 변경
            byte cmd_set = 0x0E;
            byte cmd_id = 0x02;

            byte controlByte = (byte)((is_attitude) ? 0x01 : 0x02); // 0x00에 대하여 확인 필요

            byte[] data_payload = new byte[]
            {
                controlByte
            };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.4 Handheld Gimbal Limit Angle Settings
        public bool SetLimitAngle(byte yaw_min, byte yaw_max, byte roll_min, byte roll_max, byte pitch_min, byte pitch_max)
        {
            byte cmd_type = 0x00;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x03;

            byte[] data_payload = new byte[]
            {
                0x01,
                pitch_max, pitch_min,
                yaw_max, yaw_min,
                roll_max, roll_min
            };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.5 Obtain Handheld Gimbal Limit Angle
        public bool GetLimitAngle()
        {
            byte cmd_type = 0x03;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x04;

            byte[] data_payload = new byte[] { 0x01 };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.6 Handheld Gimbal Motor Stiffness Settings
            // Stiffness Value: 0 ~ 100
        public bool SetStiffness(ushort yaw_stf, ushort roll_stf, ushort pitch_stf)
        {
            byte cmd_type = 0x00;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x05;

            byte[] yaw_stf_bytes = BitConverter.GetBytes(yaw_stf);
            byte[] roll_stf_bytes = BitConverter.GetBytes(roll_stf);
            byte[] pitch_stf_bytes = BitConverter.GetBytes(pitch_stf);

            byte[] data_payload = new byte[]
            {
                0x01,
                pitch_stf_bytes[0], roll_stf_bytes[0], yaw_stf_bytes[0]
            };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.7 Obtain Handheld Gimbal Motor Stiffness
        public bool GetStiffness()
        {
            byte cmd_type = 0x03;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x06;

            byte[] data_payload = new byte[] { 0x01 };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.8 Handheld Gimbal Parameter Push Settings
        public bool ParameterPushSetting(bool enable_value)
        {
            byte cmd_type = 0x00;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x07;

            byte[] data_payload = new byte[]
            {
                (byte)((enable_value)? 0x01:0x02)
            };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.10 Obtain Module Version Number
            // isSdk == true: DJI R SDK
            // isSdk == false: Remote Controller
        public bool GetVersion(bool isSdk)
        {
            byte cmd_type = 0x03;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x09;

            byte device_id = (byte)(isSdk ? 0x00000001 : 0x00000002);

            byte[] data_payload = new byte[] { device_id };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.11 External Device Control Command Push
        /* Controller Type:
         * Unknown controller: 0x00
         * Joystick controller: 0x01
         * Dial controller: 0x02
         */
        public bool JoystickControll(Int16 yaw_speed, Int16 roll_speed, Int16 pitch_speed)
        {
            // Default: yaw_speed = X axis, pitch_speed = Y axis, roll_speed = 0.
            // Speed value: -15,000 ~ 15,000.
            byte cmd_type = 0x00; //This command has no reply frame
            byte cmd_set = 0x0E;
            byte cmd_id = 0x0A;

            byte[] yaw_speed_bytes = BitConverter.GetBytes(yaw_speed);
            byte[] roll_speed_bytes = BitConverter.GetBytes(roll_speed);
            byte[] pitch_speed_bytes = BitConverter.GetBytes(pitch_speed);

            byte[] data_payload = new byte[]
            {
                0x01,
                pitch_speed_bytes[0], pitch_speed_bytes[1],
                roll_speed_bytes[0], roll_speed_bytes[1],
                yaw_speed_bytes[0], yaw_speed_bytes[1]
            };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.12 Obtain Handheld Gimbal User Parameters (TLV)

        // 2.3.4.13 Gimbal Parameter Information Push Setting (TLV)

        // 2.3.4.14 Handheld Gimbal Operating Mode Settings
        public bool SwitchOperating()
        {
            byte cmd_type = 0x03;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x0D;

            byte operating_mode = 0xFE; // Mode remains unchanged (확인 필요)
            byte switch_mode = 0x05; // Various modes available. (0x05 is auto switch between Landscape and Portrait mode.)

            byte[] data_payload = new byte[] { operating_mode, switch_mode };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.15 Handheld Gimbal Recenter, Selfie, and Follow Modes Settigs
            // 2.3.4.15 _ 1: Recenter
        public bool Recenter()
        {
            byte cmd_type = 0x00;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x0E;

            byte[] data_payload = new byte[] { 0xFE, 0x01 };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }
            // 2.3.4.15 _ 2: Selfie
        public bool Selfie()
        {
            byte cmd_type = 0x03;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x0E;
            
            byte[] data_payload = new byte[] { 0xFE, 0x02 };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }
            // 2.3.4.15 _ 3: Follow mode (gimbal Lock, Yaw Follow Mode, Sport Mode)
        public bool FollowMode(string operating_mode)
        {
            byte cmd_type = 0x03;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x0E;
            byte mode_data = 0x01;
            switch (operating_mode)
            {
                case ("lock"):
                    mode_data = 0x00;
                    break;
                case ("yawfollow"): // pan,tilt follow mode
                    mode_data = 0x02;
                    break;
                case ("sports"): 
                    mode_data = 0x03;
                    break;

            }
            if (mode_data == 0x01) return false;

            byte[] data_payload = new byte[] { mode_data, 0x00 };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.16 Gimbal Auto Calibration Settings (TLV)
        public bool AutoCalibration()
        {
            byte cmd_type = 0x00;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x0F;
            
            byte[] data_payload = new byte[]
            {
                0x00, 0x01, 0x81
            };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.17 Gimbal Auto Calibration Status Push (TLV)
        /*public bool CalibrationStatus()
        {
            byte cmd_type = 0x00;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x0F;

            byte[] tlv = new byte[]
            {
                0x00
            };

            byte[] data_payload = new byte[]
            {
                0x00, 0x06, 0x81
            };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }*/

        // 2.3.4.18 gimbal Activate Track Settings
        // Toggle enable / disable.
        public bool ActiveTrack()
        {
            byte cmd_type = 0x00;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x11;

            byte[] data_payload = new byte[] { 0x03 };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.4.19 Focus Motor Control Command
            // 2.3.4.19 _ 1: Focus motor Position control
            // focus_value Range: 0 ~ 4,095
        public bool SetFocusMotor(UInt16 focus_value)
        {
            byte cmd_type = 0x00; // This command has no reply frame.
            byte cmd_set = 0x0E;
            byte cmd_id = 0x12;

            byte[] focus_value_bytes = BitConverter.GetBytes(focus_value);

            byte[] data_payload = new byte[]
            {
                0x01, 0x00, 0x01,
                focus_value_bytes[0], focus_value_bytes[1]
            };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }
            // 2.3.4.19 _ 2: Focus motor Calibration
        public bool FocusMotorCalibration(bool isStart = true)
        {
            byte cmd_type = 0x03;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x12;

            byte enable_calibration = (byte)(isStart ? 0x01 : 0x06); // 0x01 means 'Auto Calibration', 0x06 means 'Stop Calibration'

            byte[] data_payload = new byte[]
            {
                0x02, 0x00, enable_calibration
            };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }
            // 2.3.4.19 _ 3: Obtain Focus Position information
        public bool GetFocusMotorPosition()
        {
            byte cmd_type = 0x03;
            byte cmd_set = 0x0E;
            byte cmd_id = 0x12;

            byte[] data_payload = new byte[] { 0x15, 0x00 };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.5.1 Third-Party Camera Motion command
        public bool CameraCommand(string command)
        {
            byte cmd_type = 0x03;
            byte cmd_set = 0x0D;
            byte cmd_id = 0x00;
            byte command_data = 0x0000;

            switch (command)
            {
                case ("shutter"):
                    command_data = 0x0001;
                    break;
                case ("stopshutter"):
                    command_data = 0x0002;
                    break;
                case ("recording"):
                    command_data = 0x0003;
                    break;
                case ("stoprecording"):
                    command_data = 0x0004;
                    break;
                case ("centerfocus"):
                    command_data = 0x0005;
                    break;
                case ("stopcenterfocus"):
                    command_data = 0x000B;
                    break;
            }
            if (command_data == 0x0000) return false;

            byte[] data_payload = new byte[] { command_data };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        // 2.3.5.2 Third-Party Camera Status Obtain Command
        public bool GetCameraStatus()
        {
            byte cmd_type = 0x03;
            byte cmd_set = 0x0D;
            byte cmd_id = 0x00;

            byte[] data_payload = new byte[] { 0x01 };

            return SendCanFrameMsg(cmd_type, cmd_set, cmd_id, data_payload);
        }

        private bool SendCanFrameMsg(byte cmd_type, byte cmd_set, byte cmd_id, byte[] cmd_data)
        {
            var cmd = cmdCombine.Combine(cmd_type, cmd_set, cmd_id, cmd_data);
            Console.WriteLine($"SEND: {cmd.ByteArrayToHexString()}");
            if (cmd_type == 0x03)
            {
                dataHandle.AddCommand(cmd);
            }

            bool ret = canComm.SendCanFrameMsg(cmd);
            return ret;
        }

        /*
         0x00: No operation
         0x01: Enable handheld gimbal
         parameter push
         0x02: Disable handheld gimbal
         parameter push 
        */
        public bool SetInvertedAxis(AxisType axis, bool invert)
        {
            if (axis == AxisType.YAW)
            {
                if (invert)
                    positionCtrlByte |= (byte)FLAG.BIT2;
                else
                    positionCtrlByte &= (byte)~FLAG.BIT2;
            }

            if (axis == AxisType.ROLL)
            {
                if (invert)
                    positionCtrlByte |= (byte)FLAG.BIT3;
                else
                    positionCtrlByte &= (byte)~FLAG.BIT3;
            }

            if (axis == AxisType.PITCH)
            {
                if (invert)
                    positionCtrlByte |= (byte)FLAG.BIT4;
                else
                    positionCtrlByte &= (byte)~FLAG.BIT4;
            }

            return true;
        }

        public bool SetMoveMode(MoveMode type)
        {
            if (type == MoveMode.INCREMENTAL_CONTROL)
            {
                positionCtrlByte &= (byte)~FLAG.BIT1;
            }
            else
                positionCtrlByte |= (byte)FLAG.BIT1;

            return true;
        }

        public MoveMode GetMoveMode()
        {
            var result = MoveMode.ABSOLUTE_CONTROL;
            if (positionCtrlByte == 0x00)
            {
                result = MoveMode.INCREMENTAL_CONTROL;
            }
            else if(positionCtrlByte == 0x01)
            {
                result = MoveMode.ABSOLUTE_CONTROL;
            }
            return result;
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
                    if (canComm != null)
                    {
                        canComm.Close(); // CanComm Close
                    }
                    if (dataHandle != null)
                    {
                        dataHandle.Stop();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
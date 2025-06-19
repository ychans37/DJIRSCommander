using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMDSender.Model
{
    public class PushGimbalParameterDataItem
    {
        public AttitudeAngle AttitudeAngle { get; set; } = new AttitudeAngle();
        public JointAngle JointAngle { get; set; } = new JointAngle();
        public LimitAngle LimitAngle { get; set; } = new LimitAngle();
        public MotorStiffness MotorStiffness { get; set; } = new MotorStiffness();

        public PushGimbalParameterDataItem()
        {
            
        }
        
        public PushGimbalParameterDataItem(int yaw, int roll, int pitch, int yawJointAngle, int rollJointAngle, int pitchJointAngle, int yawMax, int yawMin, int rollMax, int rollMin, int pitchMax, int pitchMin, int pitchStiffness, int yawStiffness, int rollStiffness)
        {
            AttitudeAngle.Yaw = yaw;
            AttitudeAngle.Roll = roll;
            AttitudeAngle.Pitch = pitch;
            
            JointAngle.Yaw = yawJointAngle;
            JointAngle.Roll = rollJointAngle;
            JointAngle.Pitch = pitchJointAngle;

            LimitAngle.YawMax = yawMax;
            LimitAngle.YawMin = yawMin;
            LimitAngle.RollMax = rollMax;
            LimitAngle.RollMin = rollMin;
            LimitAngle.PitchMax = pitchMax;
            LimitAngle.PitchMin = pitchMin;

            MotorStiffness.PitchStiffness = pitchStiffness;
            MotorStiffness.YawStiffness = yawStiffness;
            MotorStiffness.RollStiffness = rollStiffness;
        }

        public override string ToString()
        {
            var print_str = AttitudeAngle.ToString() + "\r\n" + JointAngle.ToString() + "\r\n" + LimitAngle.ToString() + "\r\n" + MotorStiffness.ToString() + "\r\n" + "\r\n";
            return print_str;
        }

        public void CopyValueFromObject(PushGimbalParameterDataItem item)
        {
            AttitudeAngle.Yaw = item.AttitudeAngle.Yaw;
            AttitudeAngle.Roll = item.AttitudeAngle.Roll;
            AttitudeAngle.Pitch = item.AttitudeAngle.Pitch;
            JointAngle.Yaw = item.JointAngle.Yaw;
            JointAngle.Roll = item.JointAngle.Roll;
            JointAngle.Pitch = item.JointAngle.Pitch;
            LimitAngle.YawMax = item.LimitAngle.YawMax;
            LimitAngle.YawMin = item.LimitAngle.YawMin;
            LimitAngle.RollMax = item.LimitAngle.RollMax;
            LimitAngle.RollMin = item.LimitAngle.RollMin;
            LimitAngle.PitchMax = item.LimitAngle.PitchMax;
            LimitAngle.PitchMin = item.LimitAngle.PitchMin;
            MotorStiffness.PitchStiffness = item.MotorStiffness.PitchStiffness;
            MotorStiffness.YawStiffness = item.MotorStiffness.YawStiffness;
            MotorStiffness.RollStiffness = item.MotorStiffness.RollStiffness;
        }
    }

    public class AttitudeAngle
    {
        public int Yaw { get; set; }
        public int Roll { get; set; }
        public int Pitch { get; set; }
        
        public AttitudeAngle()
        {
            // default
        }
        
        public AttitudeAngle(int yaw, int roll, int pitch)
        {
            Yaw = yaw;
            Roll = roll;
            Pitch = pitch;
        }

        public override string ToString()
        {
            var print_str = $"[Position] Yaw = {Yaw}, Roll = {Roll}, Pitch = {Pitch}\r\n";
            return print_str;
        }

        public void CopyValueFromObject(AttitudeAngle? item)
        {
            if(item != null)
            {
                Yaw = item.Yaw;
                Roll = item.Roll;
                Pitch = item.Pitch;
            }
        }
    }

    public class JointAngle
    {
        
        public int Yaw { get; set; }
        public int Roll { get; set; }
        public int Pitch { get; set; }
        
        public JointAngle()
        {
            // default
        }

        public JointAngle(int yawJointAngle, int rollJointAngle, int pitchJointAngle)
        {
            Yaw = yawJointAngle;
            Roll = rollJointAngle;
            Pitch = pitchJointAngle;
        }

        public override string ToString()
        {
            var print_str = $"[Joint Angle] Yaw = {Yaw}, Roll = {Roll}, Pitch = {Pitch}\r\n";
            return print_str;
        }

        public void CopyValueFromObject(JointAngle? item)
        {
            if(item != null)
            {
                Yaw = item.Yaw;
                Roll = item.Roll;
                Pitch = item.Pitch;
            }
        }
    }

    public class LimitAngle
    {
        public int YawMax { get; set; }
        public int YawMin { get; set; }

        public int RollMax { get; set; }
        public int RollMin { get; set; }

        public int PitchMax { get; set; }
        public int PitchMin { get; set; }
        
        public LimitAngle()
        {
            // default
        }

        public LimitAngle(int yawMax, int yawMin, int rollMax, int rollMin, int pitchMax, int pitchMin)
        {
            YawMax = yawMax;
            YawMin = yawMin;
            RollMax = rollMax;
            RollMin = rollMin;
            PitchMax = pitchMax;
            PitchMin = pitchMin;
        }

        public override string ToString()
        {
            var print_str = $"[Limit] -{YawMin} ~ {YawMax}, Roll = -{RollMin} ~ {RollMax}, Pitch = -{PitchMin} ~ {PitchMax}\r\n";
            return print_str;
        }

        public void CopyValueFromObject(LimitAngle item)
        {
            YawMax = item.YawMax;
            YawMin = item.YawMin;
            RollMax = item.RollMax;
            RollMin = item.RollMin;
            PitchMax = item.PitchMax;
            PitchMin = item.PitchMin;
        }
    }

    public class MotorStiffness
    {
        public int PitchStiffness { get; set; }
        public int YawStiffness { get; set; }
        public int RollStiffness { get; set; }

        public MotorStiffness()
        {
            // default
        }

        public MotorStiffness(int pitchStiffness, int yawStiffness, int rollStiffness)
        {
            PitchStiffness = pitchStiffness;
            YawStiffness = yawStiffness;
            RollStiffness = rollStiffness;
        }

        public override string ToString()
        {
            var print_str = $"[Stiffness] Yaw = {YawStiffness}, Roll = {RollStiffness}, Pitch = {PitchStiffness}";
            return print_str;
        }

        public void CopyValueFromObject(MotorStiffness item)
        {
            PitchStiffness = item.PitchStiffness;
            YawStiffness = item.YawStiffness;
            RollStiffness = item.RollStiffness;
        }
    }
}

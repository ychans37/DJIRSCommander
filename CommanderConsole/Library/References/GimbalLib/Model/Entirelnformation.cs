using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommanderConsole.Model
{
    public sealed class Entirelnformation : PushGimbalParameterDataItem
    {
        public string DeviceID { get; set; } = "unknown";
        public uint VersionNumber { get; set; } = 0x0000000000;
        public int YawSpeed { get; set; }
        public int RollSpeed { get; set; }
        public int PitchSpeed { get; set; }
        public int FocusMotorPosition { get; set; } = 0;

        public Entirelnformation()
        {
            
        }
        
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace libPWMdeviceControl
{
    /// <summary>Represents a Motor Device, controlled from three PWN channels</summary>
    public class Motor
    {
        public int FWD_Channel { get; set; }
        public int REV_Channel { get; set; }
        public int PWM_Channel { get; set; }
        public float MinSpeed { get; set; }
        public float MaxSpeed { get; set; }
        public float Speed_Cmd { get; set; }
        public int CountsLastScan { get; set; }
        public bool Start_Cmd { get; set; }
        public bool Stop_Cmd { get; set; }
        public bool Reverse_Cmd { get; set; }
        public bool Running_Sts { get; set; }
        public bool FWD_Sts { get; set; }
        public bool REV_Sts { get; set; }

        public bool DirectionLastScan { get; set; }

        public Motor()
        {

        }
        public Motor(int PWM_Chan, int FWD_Chan, int REV_Chan, int MinSpd = 0, int MaxSpd = 100)
        {
            FWD_Channel = FWD_Chan;
            REV_Channel = REV_Chan;
            PWM_Channel = PWM_Chan;
            MinSpeed = MinSpd;
            MaxSpeed = MaxSpd;
        }
    }

    /// <summary>Represents a Servo Device, controlled from one PWN channel</summary>
    public class Servo
    {
        public int PWM_Channel { get; set; }
        public float MinRange { get; set; }
        public float MaxRange { get; set; }
        public float MinPulseWidth { get; set; }
        public float MaxPulseWidth { get; set; }
        public float Position_Cmd { get; set; }
        public float PositionLastScan { get; set; }
        public Servo()
        {

        }
        public Servo(int PWM_Chan, float MinRng = -90.0f, float MaxRng = 90.0f, float MinPlsWdth = 0.0005f, float MaxPlsWdth = 0.0025f)
        {
            PWM_Channel = PWM_Chan;
            MinRange = MinRng;
            MaxRange = MaxRng;
            MinPulseWidth = MinPlsWdth;
            MaxPulseWidth = MaxPlsWdth;
        }
    }
}

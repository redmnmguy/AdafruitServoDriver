using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;
using libPCA9685;
using Windows.UI.Xaml;
using System.Diagnostics;

namespace libPWMdeviceControl
{
    /// <summary>
    /// The DeviceControl class provides PWM control for Servos and H-Bridge DC Motors using a PCA9685 device such as Adafruit's 16-Channel 12-bit PWM/Servo Driver board (Product ID 815).
    /// This class creates a thread that periodically processes any of the Motor (M#) and Servo (S#) properties which assigned to a class instance (not null).
    /// To use this library, connect the PCA9685 to your device's I2C bus.  Instantiate as many or as few Servo and/or Motor classes, setting their '*_Channel' properties as physically connected to the PCA9685 board.
    /// Invoke the  initDevice() method.  When the  initDevice() function return successfully, begin manipulate the properties of your Motor/Servo class(s) to yield the desired servo position / motor speed. 
    /// </summary>
    public class DeviceControl : IDisposable
    {
        #region Fields
        private PCA9685 _device = new PCA9685();
        private ThreadPoolTimer _periodicTimer;
        private ulong _periodicTimerMilliseconds;
        private bool _task_lock = true;
        #endregion

        #region Properties
        /// <summary>
        /// The initDevice() method has been called and has returned successfully, the users Motor and Servo object's assigned to the M# and S# properties are being processed. 
        /// </summary>
        public bool Running { get; private set; } = false;

        /// <summary>
        /// The update frequency of the PWM hardware device.  This property is set with the call to the initDevice() or SetFrequency() methods. 
        /// </summary>
        public int Frequency 
        {
            get { return _device.Frequency; }                
        }

        /// <summary>
        /// The rate, in miliSeconds, at which the M# and S# objects are periodically processed.  This property is set with the call to the initDevice() method. 
        /// </summary>
        public ulong UpdateRate
        {
            get { return _periodicTimerMilliseconds; }
            private set
            {
                if (value < 1000)
                    _periodicTimerMilliseconds = 1000;
                else if (value > 1)
                    _periodicTimerMilliseconds = 1;
                else
                    _periodicTimerMilliseconds = value;
            }
        }

        /// <summary>Users libPWMdeviceControl.Motor object to be processed </summary>
        public Motor M1 { get; set; }
        /// <summary>Users libPWMdeviceControl.Motor object to be processed </summary
        public Motor M2 { get; set; }
        /// <summary>Users libPWMdeviceControl.Motor object to be processed </summary>
        public Motor M3 { get; set; }
        /// <summary>Users libPWMdeviceControl.Motor object to be processed </summary>
        public Motor M4 { get; set; }
        /// <summary>Users libPWMdeviceControl.Servo object to be processed </summary>
        public Servo S1 { get; set; }
        /// <summary>Users libPWMdeviceControl.Servo object to be processed </summary>
        public Servo S2 { get; set; }
        /// <summary>Users libPWMdeviceControl.Servo object to be processed </summary>
        public Servo S3 { get; set; }
        /// <summary>Users libPWMdeviceControl.Servo object to be processed </summary>
        public Servo S4 { get; set; }
        /// <summary>Users libPWMdeviceControl.Servo object to be processed </summary>
        // TODO: Add 12 additional servos
        #endregion

        public DeviceControl()
        {
        }

        /// <summary>
        /// Releases all resources used by the libPWMdeviceControl.DeviceControl object.
        /// </summary>
        public void Dispose()
        {
            CancelPeriodicTimer();
            _device?.Dispose();
            _device = null;
        }

        /// <summary>
        /// Initialize the PCA9685 device and start processing the Motor and Servo object's assigned to the M# and S# properties.
        /// The method returns true if the PCA9685 was initialized and the worker thread for processing the M# and S# objects was successfully created.
        /// </summary>
        /// <param name="I2Caddress">I2C Address of the PCA9685 device</param>
        /// <param name="HzFrequency">Update Rate to request of the PCA9685 </param>
        /// <param name="mSecUpdateRate">The rate, in miliSeconds, at which the M# and S# objects will be periodically processed.</param>
        /// <returns></returns>
        public async Task<bool> initDevice(int I2Caddress, int HzFrequency = 50, ulong mSecUpdateRate = 10)
        {
            if (Running)
                CancelPeriodicTimer();

            Running = false;

            UpdateRate = mSecUpdateRate;
            
            bool initOK = await _device.asyncInitDevice(I2Caddress);
            if (initOK)
            {
                SetFrequency(HzFrequency);
                _periodicTimer = ThreadPoolTimer.CreatePeriodicTimer(timer => { PeriodicTask(timer); }, TimeSpan.FromMilliseconds(UpdateRate));
                _task_lock = false;
            }
            if (_periodicTimer == null)
                Running = false;

            return Running;
        }

        /// <summary>
        /// Cancel periodic processing of the M# and S# objects.
        /// </summary>
        public void CancelPeriodicTimer()
        {
            _periodicTimer?.Cancel();
            _periodicTimer = null;
            _device?.SetAllOff();
            Running = false;
        }

        /// <summary>
        /// Set the update rate of the PCA9685 device.
        /// </summary>
        /// <param name="HzFrequency"></param>
        public void SetFrequency(int HzFrequency)
        {
            _device.SetFrequency(HzFrequency);
        }

        // periodic task for processing M# and S# objects.
        private void PeriodicTask(ThreadPoolTimer timer)
        {
            if (_task_lock)
            {
                Debug.Write("PWM Device Control: Periodic Task Overrun!\n");
                return;
            }

            _task_lock = true;

            if (!_device.Ready)
            {
                _task_lock = false;
                return;
            }

            if(M1 != null)
                ControlMotor(M1);

            if (M2 != null)
                ControlMotor(M2);

            if (M3 != null)
                ControlMotor(M3);

            if (M4 != null)
                ControlMotor(M4);

            if (S1 != null)
                ControlServo(S1);

            if (S2 != null)
                ControlServo(S2);

            if (S3 != null)
                ControlServo(S3);

            if (S4 != null)
                ControlServo(S4);
            // TODO:  Add 12 additional servos

            _task_lock = false;
        }

        // Process M# object
        private  void ControlMotor(Motor MC)
        {
            int counts;
            float speed;

            // Apply Speed Limits
            speed = MC.Speed_Cmd;
            if (speed > MC.MaxSpeed)
                speed = MC.MaxSpeed;
            else if (speed < MC.MinSpeed)
                speed = MC.MinSpeed;
            //  scale the speed reference to PCA9685 device "counts"
            if (MC.MinSpeed == MC.MaxSpeed)
                counts = _device.MinCounts;
            else
                counts = (int)((speed - MC.MinSpeed) * ((float)(_device.MaxCounts - _device.MinCounts) / (MC.MaxSpeed - MC.MinSpeed)) + (float)_device.MinCounts);
            // Start
            if (MC.Start_Cmd && !MC.Running_Sts)
            {
                if (MC.Reverse_Cmd)
                {
                    _device.SetChannelOff(MC.FWD_Channel);
                    MC.FWD_Sts = false;
                    _device.SetChannelOn(MC.REV_Channel);
                    MC.REV_Sts = true;
                }
                else
                {
                    _device.SetChannelOff(MC.REV_Channel);
                    MC.REV_Sts = false;
                    _device.SetChannelOn(MC.FWD_Channel);
                    MC.FWD_Sts = true;
                }
                MC.DirectionLastScan = MC.Reverse_Cmd;
                _device.SetPwm(MC.PWM_Channel, _device.MinCounts, counts);
                MC.CountsLastScan = counts;
                MC.Running_Sts = true;
            }
            // Stop
            if (MC.Stop_Cmd && MC.Running_Sts)
            {
                _device.SetChannelOff(MC.REV_Channel);
                _device.SetChannelOff(MC.FWD_Channel);
                _device.SetChannelOff(MC.PWM_Channel);
                MC.CountsLastScan = counts;
                MC.Running_Sts = false;
                MC.Stop_Cmd = false;
            }
            // Speed
            if ((counts != MC.CountsLastScan) && MC.Running_Sts)
            {
               _device.SetPwm(MC.PWM_Channel, _device.MinCounts, counts);
                MC.CountsLastScan = counts;
            }
            // Direction
            if (MC.Running_Sts && (MC.Reverse_Cmd != MC.DirectionLastScan))
            {
                if (MC.Reverse_Cmd)
                {
                    _device.SetChannelOff(MC.FWD_Channel);
                    MC.FWD_Sts = false;
                    _device.SetChannelOn(MC.REV_Channel);
                    MC.REV_Sts = true;
                }
                else
                {
                    _device.SetChannelOff(MC.REV_Channel);
                    MC.REV_Sts = false;
                    _device.SetChannelOn(MC.FWD_Channel);
                    MC.FWD_Sts = true;
                }
                MC.DirectionLastScan = MC.Reverse_Cmd;
            }

            MC.Start_Cmd = false;
            MC.Stop_Cmd = false;
        }

        // Process S# object
        private void ControlServo(Servo SC)
        {
            if (SC.Position_Cmd == SC.PositionLastScan)
                return;

             SC.PositionLastScan = SC.Position_Cmd;

            int counts;
            float position;
            float ServoMinCounts;
            float ServoMaxCounts;

            // Apply Position Limits
            position = SC.Position_Cmd;
            if (position > SC.MaxRange)
                position = SC.MaxRange;
            else if (position < SC.MinRange)
                position = SC.MinRange;

            ServoMinCounts = SC.MinPulseWidth * (float)((_device.MaxCounts - _device.MinCounts) * _device.Frequency);
            if (ServoMinCounts < _device.MinCounts)
                ServoMinCounts = _device.MinCounts;

            ServoMaxCounts = SC.MaxPulseWidth * (float)((_device.MaxCounts - _device.MinCounts) * _device.Frequency);
            if (ServoMaxCounts > _device.MaxCounts)
                ServoMaxCounts = _device.MaxCounts;

            //  scale the position reference to PCA9685 device "counts"
            if (SC.MaxRange == SC.MinRange)
                counts = _device.MinCounts;
            else
                counts = (int)(((position-SC.MinRange) * (float)(ServoMaxCounts - ServoMinCounts) / (SC.MaxRange - SC.MinRange)) + ServoMinCounts);

            // Position
            _device.SetPwm(SC.PWM_Channel, _device.MinCounts, counts);

        }
    }
}

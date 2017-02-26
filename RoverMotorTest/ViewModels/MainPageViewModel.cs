using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoverMotorTest.Keypad;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using libPWMdeviceControl;
using System.IO;

namespace RoverMotorTest.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {

        #region Fields
        private DeviceControl pwmDevice;
        // TODO: The _keypadOpen variable prevents more than one Numeric Keypad Object from being
        // open at one time.  If a keypad is open and an attempt to open another is made, an
        // exception will be thrown.  The VS IDE output window displays "Only a single ContentDialog
        // can be open at any time".  Refactor the Numeric Keypad class to handle this more elegantly.
        // Ref: http://stackoverflow.com/questions/33018346/only-a-single-contentdialog-can-be-open-at-any-time-error-while-opening-anoth
        private bool _keypadOpen = false;
        #endregion

        /// <summary>
        /// default constructor
        /// </summary>
        /// <remarks>
        /// Create the PWM Device Control object,
        /// create/assign Motor objects to PWM Device Control object, initialize their pin #'s,
        /// call the initDevice(I2C Channel #) method to start the PWM Device Control.
        /// </remarks>   
        public MainPageViewModel()
        {
            CreatePWMDevice();
        }

        private async void CreatePWMDevice()
        {
            pwmDevice = new DeviceControl();
            pwmDevice.M1 = new Motor(15, 14, 13);
            pwmDevice.M2 = new Motor(9, 8, 7);
            pwmDevice.M3 = new Motor(10, 11, 12);
            pwmDevice.M4 = new Motor(4, 5, 6);
            pwmDevice.S1 = new Servo(0);
            // Our Adafruit Servo Driver is at the default I2C Address 0x40.
            await pwmDevice.initDevice(0x40);
        }

        #region BindingProperties
        public string Frequency
        {
            get { return pwmDevice?.Frequency.ToString(); }
            set { Set(ref _frequency, value); }
        }
        private string _frequency;
        public string M1Speed
        {
            //get { return _m1Speed; }
            get { return pwmDevice?.M1?.Speed_Cmd.ToString(); }
            set { Set(ref _m1Speed, value); }
        }
        private string _m1Speed;
        public string M2Speed
        {
            //get { return _m2Speed; }
            get { return pwmDevice?.M2?.Speed_Cmd.ToString(); }
            set { Set(ref _m2Speed, value); }
        }
        private string _m2Speed;
        public string M3Speed
        {
            //get { return _m3Speed; }
            get { return pwmDevice?.M3?.Speed_Cmd.ToString(); }
            set { Set(ref _m3Speed, value); }
        }
        private string _m3Speed;
        public string M4Speed
        {
           // get { return _m4Speed; }
            get { return pwmDevice?.M4?.Speed_Cmd.ToString(); }
            set { Set(ref _m4Speed, value); }
        }
        private string _m4Speed;
        public string S1Position
        {
            get { return pwmDevice?.S1?.Position_Cmd.ToString(); }
            set { Set(ref _s1Position, value); }
        }
        private string _s1Position;
        #endregion

        #region BindingCommands
        public async void buttonFrequency_Click(object sender, RoutedEventArgs e)
        {
            if (_keypadOpen)
                return;
            _keypadOpen = true;
            NumericKeypad displayKeypad = new NumericKeypad("Frequency", Frequency, 50.0f, 1000.0f);
            displayKeypad.VerticalAlignment = VerticalAlignment.Center;
            displayKeypad.HorizontalAlignment = HorizontalAlignment.Center;
            ContentDialogResult result = await displayKeypad.ShowAsync();
            if (displayKeypad.Result == KeypadResult.EntryOK)
            {
                pwmDevice.SetFrequency((int)displayKeypad.ReturnValue);
                Frequency = pwmDevice.Frequency.ToString();
            }
            _keypadOpen = false;
        }

        public async void buttonPosition_Click(object sender, RoutedEventArgs e)
        {
            if (_keypadOpen)
                return;
            _keypadOpen = true;
            NumericKeypad displayKeypad = new NumericKeypad("Position", S1Position, pwmDevice.S1.MinRange, pwmDevice.S1.MaxRange);
            displayKeypad.VerticalAlignment = VerticalAlignment.Center;
            displayKeypad.HorizontalAlignment = HorizontalAlignment.Center;
            ContentDialogResult result = await displayKeypad.ShowAsync();
            if (displayKeypad.Result == KeypadResult.EntryOK)
            {
                pwmDevice.S1.Position_Cmd = displayKeypad.ReturnValue;
                S1Position = pwmDevice.S1.Position_Cmd.ToString();
            }
            _keypadOpen = false;
        }

        // Motor 1 GUI Object event handlers
        public async void buttonM1Speed_Click(object sender, RoutedEventArgs e)
        {
            if (_keypadOpen)
                return;
            _keypadOpen = true;
            NumericKeypad displayKeypad = new NumericKeypad("Speed", M1Speed, pwmDevice.M1.MinSpeed, pwmDevice.M1.MaxSpeed);
            ContentDialogResult result = await displayKeypad.ShowAsync();
            if (displayKeypad.Result == KeypadResult.EntryOK)
            {
                pwmDevice.M1.Speed_Cmd = displayKeypad.ReturnValue;
                M1Speed = displayKeypad.ReturnValue.ToString();
            }
            _keypadOpen = false;
        }
        public void buttonM1Forward_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M1.Reverse_Cmd = false;
        }

        public void buttonM1Reverse_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M1.Reverse_Cmd = true;
        }

        public void buttonM1Start_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M1.Start_Cmd = true;
        }

        public void buttonM1Stop_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M1.Stop_Cmd = true;
        }

        // Motor 2 GUI Object event handlers
        public async void buttonM2Speed_Click(object sender, RoutedEventArgs e)
        {
            if (_keypadOpen)
                return;
            _keypadOpen = true;
            NumericKeypad displayKeypad = new NumericKeypad("Speed", M2Speed, pwmDevice.M2.MinSpeed, pwmDevice.M2.MaxSpeed);
            ContentDialogResult result = await displayKeypad.ShowAsync();
            if (displayKeypad.Result == KeypadResult.EntryOK)
            {
                pwmDevice.M2.Speed_Cmd = displayKeypad.ReturnValue;
                M2Speed = displayKeypad.ReturnValue.ToString();
            }
            _keypadOpen = false;
        }
        public void buttonM2Forward_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M2.Reverse_Cmd = false;
        }

        public void buttonM2Reverse_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M2.Reverse_Cmd = true;
        }

        public void buttonM2Start_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M2.Start_Cmd = true;
        }

        public void buttonM2Stop_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M2.Stop_Cmd = true;
        }

        // Motor 3 GUI Object event handlers
        public async void buttonM3Speed_Click(object sender, RoutedEventArgs e)
        {
            if (_keypadOpen)
                return;
            _keypadOpen = true;
            NumericKeypad displayKeypad = new NumericKeypad("Speed", M3Speed, pwmDevice.M3.MinSpeed, pwmDevice.M3.MaxSpeed);
            ContentDialogResult result = await displayKeypad.ShowAsync();
            if (displayKeypad.Result == KeypadResult.EntryOK)
            {
                pwmDevice.M3.Speed_Cmd = displayKeypad.ReturnValue;
                M3Speed = displayKeypad.ReturnValue.ToString();
            }
            _keypadOpen = false;
        }
        public void buttonM3Forward_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M3.Reverse_Cmd = false;
        }

        public void buttonM3Reverse_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M3.Reverse_Cmd = true;
        }

        public void buttonM3Start_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M3.Start_Cmd = true;
        }

        public void buttonM3Stop_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M3.Stop_Cmd = true;
        }

        // Motor 4 GUI Object event handlers
        public async void buttonM4Speed_Click(object sender, RoutedEventArgs e)
        {
            if (_keypadOpen)
                return;
            _keypadOpen = true;
            NumericKeypad displayKeypad = new NumericKeypad("Speed", M4Speed, pwmDevice.M4.MinSpeed, pwmDevice.M4.MaxSpeed);
            ContentDialogResult result = await displayKeypad.ShowAsync();
            if (displayKeypad.Result == KeypadResult.EntryOK)
            {
                pwmDevice.M4.Speed_Cmd = displayKeypad.ReturnValue;
                M4Speed = displayKeypad.ReturnValue.ToString();
            }
            _keypadOpen = false;
        }
        public void buttonM4Forward_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M4.Reverse_Cmd = false;
        }

        public void buttonM4Reverse_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M4.Reverse_Cmd = true;
        }

        public void buttonM4Start_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M4.Start_Cmd = true;
        }

        public void buttonM4Stop_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice.M4.Stop_Cmd = true;
        }
        #endregion

        public void Close_Click(object sender, RoutedEventArgs e)
        {
            pwmDevice?.Dispose();
            // Shutdown the Pi
            // Windows.System.ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0.5));
            // Shutdown the current application
            Application.Current.Exit();
            StreamWriter sw=  File.CreateText("");
            sw.Dispose();
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;
            storage = value;
            RaisePropertyChanged(propertyName);
            return true;
        }
    }
}

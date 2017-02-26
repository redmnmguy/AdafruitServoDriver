using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

/// <summary>
///  Library for communicating with (via I2C), initializing and sending commands to control
///  the pulse width of the individual channels of an Intel PCA9685 based PWM Controller board.
/// </summary>
namespace libPCA9685
{
    /// <summary>
    ///  Enumeration for addressing registers of the PCA9685 
    ///  for configuration, control and operational modes.
    /// </summary>
    enum Register
    {
        MODE1 = 0x00,
        MODE2 = 0x01,
        PRESCALE = 0xFE,
        CHAN0_ON_L = 0x06,
        CHAN0_ON_H = 0x07,
        CHAN0_OFF_L = 0x08,
        CHAN0_OFF_H = 0x09,
        ALLCHAN_ON_L = 0xFA,
        ALLCHAN_ON_H = 0xFB,
        ALLCHAN_OFF_L = 0xFC,
        ALLCHAN_OFF_H = 0xFD,
    }
    /// <summary>
    ///  Enumeration for command codes to the PCA9685 
    /// </summary>
    enum Command
    {
        RESTART = 0x80,
        SLEEP   = 0x10,
        ALLCALL = 0x01,
        INVRT   = 0x10,
        OUTDRV  = 0x04,
    }

    /// <summary>
    /// Provides configuration, control and communications to 
    /// a PCA9685 device, such as the Adafruit 815 Servo Shield
    /// via a inter-integrated circuit (I2C) bus.
    /// </summary>
    public class PCA9685 : IDisposable
    {
        // Represents a communications channel to a device on an inter-integrated circuit (I2C) bus.
        // This class is provided via the Windows.Devices namespace of the Windows API
        // Reference the PCA9685 Datasheet here: http://www.nxp.com/documents/data_sheet/PCA9685.pdf
        private I2cDevice _device;

        #region Properties
        // Immutable Types

        /// <summary>
        /// The minimum value that should be passed to the onCount or offCount parameters of the SetPwm() method.
        /// <para />This property is read-only.
        /// </summary>
        public int MinCounts { get; } = 0;

        /// <summary>
        /// The minimum value that should be passed to the onCount or the offCount parameters of the SetPwm() method.
        /// <para />This property is read-only.
        /// </summary>
        public int MaxCounts { get; } = 4095;

        /// <summary>
        /// Minimum value that should be passed to the SetFrequency() method.  This property is read-only.
        /// </summary>
        public int MinFrequency { get; } = 0;

        /// <summary>
        /// Minimum value that should be passed to the SetFrequency() method.  This property is read-only.
        /// </summary>
        public int MaxFrequency { get; } = 1000;

        /// <summary>
        /// Indication that the asyncInitDevice() method has been called and I2C communications
        /// have been successfully established to the PCA9685 device.
        /// </summary>
        public bool Ready { get; private set; }

        /// <summary>
        /// I2C address of the connected PCA9685 device.  This property is read-only.
        /// This property value is set when a call to SetFrequency() method returns successfully. 
        /// </summary>
        public int Address { get; private set; }

        private int _frequency;
        /// <summary>
        /// Update frequency of the channels of the PCA9685 device.  This property is read-only,
        /// This property value is set when a call to asyncInitDevice() method returns successfully. 
        /// </summary>
        public int Frequency{
            get { return _frequency; }
            private set
            {
                if (value<MinFrequency)
                    _frequency = MinFrequency;
                else if (value > MaxFrequency)
                    _frequency = MaxFrequency;
                else
                    _frequency = value;
            }
        }
        #endregion

        public PCA9685()
        {
        }

        /// <summary>
        /// Releases all resources used by the libPCA9685.PCA9685 object.
        /// </summary>
        public void Dispose()
        {
            if (_device != null)
                SetAllOff();
            _device?.Dispose();
        }
        /// <summary>
        /// Sets the PWM update rate.
        /// </summary>
        /// <param name="frequency">The update frequency, in hz. Valid range is MinFrequency to MaxFrequency</param>
        /// <remarks>This function was converted from python code of the Adafruit_Python_PCA9685 library
        /// this library example can be found here: https://github.com/adafruit/Adafruit_Python_PCA9685
        /// Reference Datasheet: 7.3.5 PWM frequency PRE_SCALE</remarks>
        public void SetFrequency(int frequency)
        {
            Frequency = frequency;
            decimal preScale = 25000000.0m;     // 25MHz
            preScale /= 4096m;                  // 12-bit
            preScale /= (int)Frequency;

            preScale -= 1.0m;

            decimal prescale = Math.Floor(preScale + 0.5m);

            byte oldmode = _readRegister(Register.MODE1);
            byte newmode = (byte)((oldmode & 0x7F) | (byte)Command.SLEEP);     // sleep

            _writeRegister(Register.MODE1, newmode);             // go to sleep

            _writeRegister(Register.PRESCALE, (byte)Math.Floor(prescale));
            _writeRegister(Register.MODE1, oldmode);
            Task.Delay(5);
            _writeRegister(Register.MODE1, oldmode | (byte)Command.RESTART);        // 0xa1  ??
        }

        /// <summary>
        /// Initialize I2C connection to PCA9685 device.
        /// </summary>
        /// <param name="address">The I2C address of the PCA9685 device</param>
        /// <param name="FastBusSpeed"> true = 400KHz bus speed, false = 100KHz bus speed </param>
        /// <returns>awaitable task of type bool.  true if initialization was successful, false otherwise.</returns>
        public async Task<bool> asyncInitDevice(int address, bool FastBusSpeed = true)
        {
            Ready = false;
            Address = address;
            try
            {
                I2cConnectionSettings settings = new I2cConnectionSettings(Address);
                if(FastBusSpeed)
                    settings.BusSpeed = I2cBusSpeed.FastMode;                   // 400KHz bus speed 
                else
                    settings.BusSpeed = I2cBusSpeed.StandardMode;               // 100KHz bus speed

                string aqs = I2cDevice.GetDeviceSelector();                                 // Get a selector string that will return all I2C controllers on the system 
                DeviceInformationCollection dic = await DeviceInformation.FindAllAsync(aqs);// Find the I2C bus controller devices with our selector string             
                _device = await I2cDevice.FromIdAsync(dic[0].Id, settings);                 // Create an I2cDevice with our selected bus controller and I2C settings   
                if (_device == null)
                {
                    return Ready;
                }
                // Write the register settings 
                _writeRegister(Register.MODE1, 0x00);
                // if the _writeRegister method throws an exception, do not set the Address and do not set Ready to True
                // proceed directly to the catch() clause, do not pass go and do not collect $200 ;-p
                Ready = true;
            }
            // The _writeREgister method failed.  Indicate to the caller with a return value of false;
            catch (Exception)
            {
                Ready = false;
                Address = -1;
            }

            // Return the status of the method to the caller
            return Ready;
        }

        /// <summary>
        /// Set the PWM duty cycle of one of the PCA9685 device channels
        /// </summary>
        /// <param name="channel">channel number to set PWM</param>
        /// <param name="onCount">Count # to switch channel ON. Valid range is MinCounts to MaxCounts</param>
        /// <param name="offCount">Count # to switch channel OFF.  Valid range is MinCounts to MaxCounts</param>
        public void SetPwm(int channel, int onCount, int offCount)
        {
            _writeRegister(Register.CHAN0_ON_L + 4 * (int)channel, onCount & 0xFF);
            _writeRegister(Register.CHAN0_ON_H + 4 * (int)channel, onCount >> 8);
            _writeRegister(Register.CHAN0_OFF_L + 4 * (int)channel, offCount & 0xFF);
            _writeRegister(Register.CHAN0_OFF_H + 4 * (int)channel, offCount >> 8);
        }

        /// <summary>
        /// Disable PWM on channel.  Channel output set to OFF.
        /// </summary>
        /// <param name="channel">Channel number to set output to OFF</param>
        public void SetChannelOff(int channel)
        {
            SetPwm(channel, 0x0000, 0x1000);
        }

        /// <summary>
        /// Set all channels of  PCA9685 device to OFF.  Disable all active PWM on all channels.
        /// </summary>
        public void SetAllOff()
        {
            _writeRegister(Register.ALLCHAN_OFF_L, 0x00);
            _writeRegister(Register.ALLCHAN_OFF_H, 0x10);
        }

        /// <summary>
        /// Set channel to be continuously ON.
        /// </summary>
        /// <param name="channel">Channel number to set output to ON</param>
        public void SetChannelOn(int channel)
        {
            SetPwm(channel, 0x1000, 0x0000);
        }

        // Write the byte value specified to the PCA9685 register address specified
        private void _writeRegister(Register register, byte data)
        {
            try
            {
                _device?.Write(new[] { (byte)register, data });
            }
            catch(System.IO.FileNotFoundException)
            {
                Debug.WriteLine("libPCA9685: File Not Found Exception Caught in call to _writeRegister():");
                Debug.WriteLine(string.Format("libPCA9685: Could not communicate with device at I2C Address 0x{0}.", Address.ToString("X2")));
                throw;
            }
            catch(Exception ex)
            {
                Debug.WriteLine("libPCA9685: General Exception Caught in call to _writeRegister(): ");
                Debug.WriteLine(string.Format("libPCA9685: {0}. ", ex.Message));
                Debug.WriteLine(string.Format("libPCA9685: Failed to write value {0} to register {1} at I2C Address 0x{2}.", data.ToString(), register.ToString(),Address.ToString("X2")));
                throw;
            }
        }

        // Write the lower BYTE of the INT specified to the PCA9685 register address specified
        private void _writeRegister(Register register, int data)
        {
            _writeRegister(register, (byte)data);
        }

        // Read a byte value from the PCA9685 register address specified
        private byte _readRegister(Register register)
        {
            byte[] value = new byte[1];
            byte[] reg = new byte[1];

            try
            {
                reg[0] = (byte)register;
                _device?.Write(reg);
                _device?.Read(value);
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine("libPCA9685: File Not Found Exception Caught in call to _readRegister(): ");
                Debug.WriteLine(string.Format("libPCA9685: Could not communicate with device at I2C Address 0x{0}. ", Address.ToString("X2")));
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("libPCA9685: General Exception Caught in call to _readRegister(): ");
                Debug.WriteLine(string.Format("libPCA9685: {0}. ", ex.Message));
                Debug.WriteLine(string.Format("libPCA9685: Failed to read register {0} from I2C device address 0x{1}",register.ToString(), Address.ToString("X2")));
                throw;
            }

            return value[0];
        }
    }
}

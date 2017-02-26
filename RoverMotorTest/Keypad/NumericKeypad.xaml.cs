using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RoverMotorTest.Keypad
{
    public enum KeypadResult
    {
        EntryOK,
        EntryCancel,
        EntryError
    }
    public partial class NumericKeypad : ContentDialog , INotifyPropertyChanged
    {
        #region private fields         
        private int _maxCVLen = 12;
        private bool _autoClearOnKeypress = true;
        #endregion

        #region properties     
        public KeypadResult Result { get; private set; }
        #endregion

        #region BindingProperties
        public float ReturnValue
        {
            get { return _returnValue; }
            set { Set(ref _returnValue, value); }
        }
        private float _returnValue;

        public float MinValue
        {
            get { return _minValue; }
            set { Set(ref _minValue, value); }
        }
        private float _minValue;

        public float MaxValue
        {
            get { return _maxValue; }
            set { Set(ref _maxValue, value); }
        }
        private float _maxValue;

        public string SetpointLabel
        {
            get { return _setpointLabel; }
            set { Set(ref _setpointLabel, value); }
        }
        private string _setpointLabel = "Setpoint Label:";

        public string CurrentValue
        {
           get { return _currentValue; }
           set { Set(ref _currentValue, value); }
        }
        private string _currentValue = "0";

        public SolidColorBrush BackgroundBrush
        {
            get { return _backgroundBrush; }
            set { Set(ref _backgroundBrush, value); }
        }
        private SolidColorBrush _backgroundBrush = new SolidColorBrush();

        public Color NormalBackgroundColor
        {
            get { return _normalBackgroundColor; }
            set { Set(ref _normalBackgroundColor, value); }
        }
        private Color _normalBackgroundColor = Color.FromArgb(255, 255, 255, 255);    // White

        public Color LimitBackgroundColor
        {
            get { return _limitBackgroundColor; }
            set { Set(ref _limitBackgroundColor, value); }
        }
        private Color _limitBackgroundColor = Color.FromArgb(255, 255, 0, 0);        // Red
        #endregion

        // Default Constructor
        public NumericKeypad()
        {
            this.InitializeComponent();
            SetpointLabel = "";
            _returnValue = 0.0f;
            _currentValue = "0";
            MinValue = 0.0f;
            MaxValue = 100.0f;
        }
        public NumericKeypad(string Label, string InitValue, float Minimum, float Maximum)
        {
            this.InitializeComponent();
            _setpointLabel = Label;
            _currentValue = InitValue;
            _minValue = Minimum;
            _maxValue = Maximum;
        }

        private void ButtonClickCommand(object sender, RoutedEventArgs e)
        {
            char charVal;
            Windows.UI.Xaml.Controls.Button buttonObject;
            buttonObject = (Windows.UI.Xaml.Controls.Button)sender;
            try
            {
                charVal = System.Convert.ToChar(buttonObject.CommandParameter);
                // The character is an numeric value, add it to the current value string
                if (char.IsNumber(charVal))
                {
                    if (_autoClearOnKeypress)
                    {
                        ClearCV();
                        _autoClearOnKeypress = false;
                    }
                    AddCharToCV(charVal);
                }
                    
                else if (charVal.Equals('-'))
                    AddNegative();
                else if (charVal.Equals('.'))
                    AddDecimal();
                else if (charVal.Equals('B'))
                    Backspace();
                else if (charVal.Equals('C'))
                    ClearCV();
                if (charVal.Equals('E'))
                {
                    try
                    {
                        if (CheckCurrentValue())
                        {
                            _returnValue = Convert.ToSingle(_currentValue);
                            Result = KeypadResult.EntryOK;
                            this.Hide();
                        }
                    }
                    catch(Exception)
                    {
                        Result = KeypadResult.EntryError;
                        this.Hide();
                    }
                    /*
                    catch (FormatException e)
                    {
                        Result = KeypadResult.EntryError;
                    }
                    catch (OverflowException e)
                    {
                        Result = KeypadResult.EntryError;
                    }
                    */
                }
                if (charVal.Equals('X'))
                {
                    Result = KeypadResult.EntryCancel;
                    this.Hide();
                }
            }
            catch (System.FormatException)
            {
                // System.Console.WriteLine("The string is longer than one character.");
            }
            catch (System.ArgumentNullException)
            {
                // System.Console.WriteLine("The string is null.");
            }
            catch (System.InvalidCastException)
            {
                // System.Console.WriteLine("Invalid Cast.");
            }
            catch (System.OverflowException)
            {
                // System.Console.WriteLine("Overflow.");
            }
            return;
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        private int AddCharToCV(char CharToAdd)
        {
            int CurrentCVLen;
            int NegSgnLoc;

            _backgroundBrush.Color = NormalBackgroundColor;
            // _strCurrentValue = textBlock_Value.Text;

            // If the length is 1 and that character is a 0, remove it
            if (_currentValue.Length == 1)
            {
                char[] MyChar = { '0', ' ' };
                _currentValue = _currentValue.TrimStart(MyChar);
            }
            // Prevent multiple zeros without a decimal point.
            if ((_currentValue.Length == 2) && !(CharToAdd == '.'))
            {
                NegSgnLoc = _currentValue.IndexOf("-", 0);
                if (NegSgnLoc >= 0)
                {
                    char[] MyChar = { '0', ' ' };
                    _currentValue = _currentValue.TrimEnd(MyChar);
                }
            }

            CurrentCVLen = _currentValue.Length;
            if (CurrentCVLen > _maxCVLen)
                return CurrentCVLen;

            if ((_currentValue.Length <= 0) && (CharToAdd == '.'))
            {
                _currentValue = "0";
                _currentValue += CharToAdd;
            }
            else
            {
                _currentValue += CharToAdd;
            }

          OnPropertyChanged("CurrentValue");
            return CurrentCVLen + 1;
        }
        private void Backspace()
        {
            int CurrentCVLen;

            _backgroundBrush.Color = NormalBackgroundColor;

            CurrentCVLen = _currentValue.Length;
            if (CurrentCVLen <= 1)
            {
                ClearCV();
                return;
            }
            _currentValue = _currentValue.Remove(CurrentCVLen - 1, 1);
            OnPropertyChanged("CurrentValue");
        }
        private void ClearCV()
        {
            _backgroundBrush.Color = NormalBackgroundColor;
            _currentValue = "";
            OnPropertyChanged("CurrentValue");
        }
        private void AddDecimal()
        {
            if (!_currentValue.Contains("."))
            {
                _backgroundBrush.Color = NormalBackgroundColor;
                AddCharToCV('.');
                OnPropertyChanged("CurrentValue");
            }
        }
        private void AddNegative()
        {
            int NegSgnLoc;

            _backgroundBrush.Color = NormalBackgroundColor;

            if (_currentValue.Length > 0)
            {
                NegSgnLoc = _currentValue.IndexOf("-", 0);


                if (NegSgnLoc >= 0)
                {
                    char[] MyChar = { '-', ' ' };
                    _currentValue = _currentValue.TrimStart(MyChar);
                }
                else
                {
                    _currentValue = "-" + _currentValue;
                }

                OnPropertyChanged("CurrentValue");
            }
        }
        private bool CheckCurrentValue()
        {
            // Check to see if the Current Value edit box is empty.
            if (_currentValue.Length == 0)
            {
                _backgroundBrush.Color = LimitBackgroundColor;
                return false;
            }
            float ValueEntered = Convert.ToSingle(_currentValue);
            if ((ValueEntered >= _minValue) && (ValueEntered <= _maxValue))
            {
                // Set the Edit Control's Background color
                _backgroundBrush.Color = NormalBackgroundColor;
                return true;
            }
            else
            {
                _backgroundBrush.Color = LimitBackgroundColor;
                return false;
            }
        }
    }
}

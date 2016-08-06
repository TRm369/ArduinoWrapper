using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoWrapper {
    public class Arduino : ArduinoSerialPort {
        //Public variables/properties
        public bool[] PinSignal { get; protected set; }
        public short[] AnalogPinSignal { get; protected set; }

        //Protected variables
        protected Action<int, bool> OnInputChangeCallback;
        protected Action<int, short> OnAnalogInputChangeCallback;
        protected ArduinoBoardSpecification BoardSpecs;

        //Public functions
        public Arduino(ArduinoBoardSpecification BoardSpecs) {
            this.BoardSpecs = BoardSpecs;
            Init();
        }

        public Arduino (ArduinoBoardSpecification BoardSpecs, string Port) : base(Port) {
            this.BoardSpecs = BoardSpecs;
            Init();
        }

        public void SetPinMode (int Pin, PinMode Mode) {
            Write(new byte[] { Constant.pinModeCommand, (byte)Pin, (byte)Mode });
        }

        public void DigitalWrite (int Pin, bool Signal) {
            Write(new byte[] { Constant.digitalWriteCommand, (byte)Pin, Util.BoolToByte(Signal) } );
        }

        public void AnaloglWrite(int Pin, byte Signal) {
            Write(new byte[] { Constant.analogWriteCommand, (byte)Pin, Signal });
        }

        public void EnableAnalogRead(int pin, bool Enabled) {
            Write(new byte[] { Constant.EnableAnalogReadCommand, (byte)pin, Util.BoolToByte(Enabled) });
        }

        public void RegisterOnInputChageCallback(Action<int, bool> Callback) {
            OnInputChangeCallback += Callback;
        }
        public void UnregisterOnInputChageCallback(Action<int, bool> Callback) {
            OnInputChangeCallback -= Callback;
        }

        public void RegisterOnAnalogInputChageCallback(Action<int, short> Callback) {
            OnAnalogInputChangeCallback += Callback;
        }
        public void UnregisterOnAnalogInputChageCallback(Action<int, short> Callback) {
            OnAnalogInputChangeCallback -= Callback;
        }

        //Protected functions

        protected void Init() {
            RegisterOnReadCallback(OnRead);
            PinSignal = new bool[BoardSpecs.PinCount];
            AnalogPinSignal = new short[BoardSpecs.AnalogPinCount];
        }

        protected void OnRead() {
            Message m = Read();
            for (int i = 0; m.DataLength > i + 2; i = i + 3) { //Loop through all commands in the message
                //digitalRead command
                if (m.Data[i] == Constant.digitalReadCommand) {
                    PinSignal[m.Data[i + 1]] = Util.ByteToBool(m.Data[i + 2]);
                    if (OnInputChangeCallback != null)
                        OnInputChangeCallback(m.Data[i + 1], PinSignal[m.Data[i + 1]]);
                }
                //analogRead command
                if (m.Data[i] == Constant.analogReadCommand) {
                    AnalogPinSignal[m.Data[i + 1]] = Util.ByteArrayToShort(m.Data[i + 2], m.Data[i + 3]);
                    if (OnAnalogInputChangeCallback != null)
                        OnAnalogInputChangeCallback(m.Data[i + 1], AnalogPinSignal[m.Data[i + 1]]);
                    i++;
                    continue;
                }
            }
        }

        public enum PinMode {
            INPUT = 0,
            OUTPUT = 1,
            INPUT_PULLUP = 2
        }
    }
}

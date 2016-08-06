using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;

namespace ArduinoWrapper {
    public class ArduinoSerialPort {
        //Variables wrapped using properties
        private string _Port;
        private int _BaudRate = 9600;
        private int _ACKtimeout = 1000;
        private int _MaxResendCount = 3;
        private int _KeepAliveTimeout = 60000;
        private int _AnalogReadInterval = 100;
        private int _AnalogFilter = 5;

        //Public variables/properties
        public bool IsConnected { get; private set; }
        public int BaudRate {
            get {
                return _BaudRate;
            }
            set {
                if (IsConnected == false)
                    _BaudRate = value;
                else
                    throw new Exception("Cannot change baud rate while connected.");
            }
        }
        public string Port {
            get {
                return _Port;
            }
            set {
                if (IsConnected == false)
                    _Port = value;
                else
                    throw new Exception("Cannot change port while connected.");
            }
        }
        public int ACKtimeout {
            get {
                return _ACKtimeout;
            }
            set {
                if (IsConnected == false)
                    _ACKtimeout = value;
                else
                    throw new Exception("Cannot change ACKtimeout while connected.");
            }
        }
        public int MaxMessageLength = 32;
        public int MaxResendCount {
            get {
                return _MaxResendCount;
            }
            set {
                if (IsConnected == false)
                    _MaxResendCount = value;
                else
                    throw new Exception("Cannot change MaxResendCount while connected.");
            }
        }
        public int KeepAliveTimeout {
            get {
                return _KeepAliveTimeout;
            }
            set {
                if (IsConnected == false)
                    _KeepAliveTimeout = value;
                else
                    throw new Exception("Cannot change KeepAliveTimeout while connected.");
            }
        }
        public int KeepAliveSendInterval = 55000;
        public int AnalogReadInterval {
            get {
                return _AnalogReadInterval;
            }
            set {
                if (IsConnected == false)
                    _AnalogReadInterval = value;
                else
                    throw new Exception("Cannot change AnalogReadInterval while connected.");
            }
        }
        public int AnalogFilter {
            get {
                return _AnalogFilter;
            }
            set {
                if (IsConnected == false)
                    _AnalogFilter = value;
                else
                    throw new Exception("Cannot change AnalogFilter while connected.");
            }
        }
        public int MessagesToRead { get { return ReadBuffer.Count; } }
        public int MessagesToSend { get { return WriteBuffer.Count; } }

        //Private variables
        private SerialPort ArduinoPort;
        private Thread TrafficControllerThread;
        private Queue<Message> WriteBuffer = new Queue<Message>();
        private Queue<Message> ReadBuffer = new Queue<Message>();
        private Action OnReadCallback;
        private Action<Exception> OnExceptionOccuredCallback;
        private System.Diagnostics.Stopwatch Stopwatch = new System.Diagnostics.Stopwatch();

        //Public functions
        public ArduinoSerialPort (string Port) {
            this.Port = Port;
        }

        public ArduinoSerialPort() { }

        public void Connect() {
            ArduinoPort = new SerialPort(Port, BaudRate);
            ArduinoPort.Open(); //Open the port

            if (ArduinoPort.IsOpen == false) {
                IsConnected = false;
                return;
            }

            TrafficControllerThread = new Thread(TrafficController);
            TrafficControllerThread.Start();

            //Prepeare parameters for Arduino
            byte offset = 0;
            int[] parameters = new int[5];
            parameters[0] = ACKtimeout;
            parameters[1] = MaxResendCount;
            parameters[2] = KeepAliveTimeout;
            parameters[3] = AnalogReadInterval;
            parameters[4] = AnalogFilter;

            //Send the parameters
            Message m = new Message(MaxMessageLength);
            m.AppendData(new byte[] { Constant.ConnectingIntro, offset});
            for (byte i = 0; i < parameters.Length; i++) {
                if (m.AppendData(Util.IntToByteArray(parameters[i])) == false) {
                    m.Trim();
                    Write(m);
                    m = new Message(MaxMessageLength);
                    offset += i;
                    m.AppendData(new byte[] { Constant.ConnectingIntro, offset });
                    m.AppendData(Util.IntToByteArray(parameters[i]));
                }
            }
            m.Trim();
            Write(m);

            while (true) {
                if (ReadBuffer.Count > 0) {
                    m = ReadBuffer.Dequeue();
                    if (m.Data[0] == Constant.ConnectionSuccessful)
                        break;
                }
                Thread.Sleep(10);
            }
            Thread.Sleep(100); //To prevent multi-threading issues
            IsConnected = true;
        } 

        public void Disconnect() {
            if (IsConnected == false)
                return;

            TrafficControllerThread.Abort();
            WriteToPort(Constant.Disconnect);
            ReadACK();
            ArduinoPort.Close();
            IsConnected = false;
        }

        public void RegisterOnReadCallback (Action Callback) {
            OnReadCallback += Callback;
        }

        public void UnregisterOnReadCallback (Action Callback) {
            OnReadCallback -= Callback;
        }

        public void RegisterOnExceptionOccuredCallback(Action<Exception> Callback) {
            OnExceptionOccuredCallback += Callback;
        }

        public void UnregisterOnExceptionOccuredCallback(Action<Exception> Callback) {
            OnExceptionOccuredCallback -= Callback;
        }

        //Protected functions

        protected void Write(Message m) {
            WriteBuffer.Enqueue(m);
        }

        protected void Write(byte[] Data) {
            WriteBuffer.Enqueue(new Message(Data));
        }

        protected Message Read() {
            if (ReadBuffer.Count > 0)
                return ReadBuffer.Dequeue();
            else
                return null;
        }

        //Private functions
        private void TrafficController() {
            Message m;
            Stopwatch.Start();

            while (true) {
                Thread.Sleep(10);
                //Check if port is open
                if (ArduinoPort.IsOpen == false) {
                    if (OnExceptionOccuredCallback != null) OnExceptionOccuredCallback(new Exception("Arduino port is closed."));
                    IsConnected = false;
                    TrafficControllerThread.Abort();
                }
                //KeepAlive check
                if (Stopwatch.ElapsedMilliseconds >= KeepAliveSendInterval) {
                    Write(Constant.KeepAlive);
                    Stopwatch.Restart();
                }

                //READ
                m = ReadFromPort();                   //Read message if any
                if (m != null) {                      //Message received
                    ReceiveMessage(m);
                }
                //WRITE
                while (WriteBuffer.Count > 0) {                                //Check if any messages are ready to be sent
                    m = WriteBuffer.Dequeue();                                 //If so grab it
                    WriteToPort(m);                                            //and send it,
                    if (ReadACK() == false)                                    //if ACK didn't arrive
                        if (m.ResendCount < MaxResendCount) {
                            m.ResendCount++;
                            WriteBuffer.Enqueue(m);                            //reenqueue the message
                        } else
                            if (OnExceptionOccuredCallback != null) OnExceptionOccuredCallback(new Exception("Failed to send message."));
                }
            }
        }

        private bool ReadACK() {
            Message m;
            int timeLeft = ACKtimeout;
            while (timeLeft > 0) {
                m = ReadFromPort();
                if (m != null) {
                    if (m.IsACK()) {
                        return true;
                    } else {
                        ReceiveMessage(m);
                    }
                }
                timeLeft -= 10;
                Thread.Sleep(10);
            }
            return false;
        }

        private void ReceiveMessage(Message m) {
            if (m.DataCorrupted() == false) { //If message is fine
                ReadBuffer.Enqueue(m);        //add it to buffer,
                WriteToPort(Constant.ACK);    //send a ACK and
                if (IsConnected)
                    OnReadCallback();         //call the appropriate callback
            }                                 //if it's corrupted do nothing and Arduino will send it again
        }

        private Message ReadFromPort() {
            //Wait for beginning of message
            if (ArduinoPort.BytesToRead > 1) { //Two or more bytes ready, check if they are a beggining of a message
                if (ArduinoPort.ReadByte() == Constant.Beginning[0]) {
                    if (ArduinoPort.ReadByte() == Constant.Beginning[1]) {
                        //Beggining received, read data length and checksum
                        while (ArduinoPort.BytesToRead < 2) { Thread.Sleep(10); }
                        byte[] data = new byte[2];
                        ArduinoPort.Read(data, 0, 2);
                        int length = Util.ByteArrayToShort(data);
                        byte checksum = (byte)ArduinoPort.ReadByte();

                        //Read data
                        while (ArduinoPort.BytesToRead < length) { Thread.Sleep(10); }
                        data = new byte[length];
                        ArduinoPort.Read(data, 0, length);

                        Message m = new Message(data, checksum);
                        return m;
                    }
                }
            }
            return null;
        }

        private bool WriteToPort(Message m) {
            if (m.Length > MaxMessageLength)
                throw new Exception("Data too long.");

            ArduinoPort.Write(m.Header(), 0, 5);
            ArduinoPort.Write(m.Data, 0, m.DataLength);

            return false;
        }
    }
}
using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;

namespace ArduinoWrapper {
    class ArduinoSerialPort {
        //Variables wrapped using properties
        protected string _Port;
        protected int _BaudRate = 9600;

        //Public variables/properties
        public bool IsConnected { get; protected set; }
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
        public int ACKtimeout = 1000;

        //Protected variables
        protected SerialPort ArduinoPort;
        protected Thread TrafficControllerThread;
        protected Queue<Message> WriteBuffer = new Queue<Message>();
        protected Queue<Message> ReadBuffer = new Queue<Message>();
        protected Action onReadCallback;

        //Public functions
        public ArduinoSerialPort (string Port) {
            this.Port = Port;
        }

        public ArduinoSerialPort() { }

        public void Connect() {
            ArduinoPort = new SerialPort(Port, BaudRate);
            ArduinoPort.Open();

            //TODO: Three-way handshake

            if (ArduinoPort.IsOpen == false) {
                IsConnected = false;
                return;
            }

            TrafficControllerThread = new Thread(TrafficController);
            TrafficControllerThread.Start();
            IsConnected = true;
        } 

        public void Write(Message m) {
            WriteBuffer.Enqueue(m);
        }

        public void Write(byte[] Data) {
            WriteBuffer.Enqueue(new Message(Data));
        }

        public void DEBUG(string s) {
            byte[] message = { 0x20, 44, 1, 0x20, 37, 0, 0x21, 44, 1};
            onReadCallback = a;
            Write(new Message(message));
        }
        public void a() {
            Message m = ReadBuffer.Dequeue();
            foreach (byte b in m.Data)
                Console.WriteLine(b);
        }

        //Protected functions
        protected void TrafficController() {
            Message m;

            while (true) {
                //READ
                m = ReadFromPort();                   //Read message if any
                if (m != null) {                      //Message received
                    ReceiveMessage(m);
                }
                //WRITE
                while (WriteBuffer.Count > 0) {     //Check if any messages are ready to be sent
                    m = WriteBuffer.Dequeue();      //If so grab it
                    WriteToPort(m);                 //and send it,
                    if (ReadACK() == false)         //if ACK didn't arrive
                        WriteBuffer.Enqueue(m);     //reenqueue the message
                }
            }
        }

        protected bool ReadACK() {
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

        protected void ReceiveMessage(Message m) {
            if (m.DataCorrupted() == false) { //If message is fine
                ReadBuffer.Enqueue(m);        //add it to buffer,
                WriteToPort(Constant.ACK);    //send a ACK and
                onReadCallback();             //call the appropriate callback
            }                                 //if it's corrupted do nothing and Arduino will send it again
        }
        
        protected Message ReadFromPort() {
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

        protected bool WriteToPort(Message m) {
            if (m.Length > short.MaxValue)
                throw new Exception("Data too long.");

            ArduinoPort.Write(m.Header(), 0, 5);
            ArduinoPort.Write(m.Data, 0, m.Length);

            return false;
        }
    }
}
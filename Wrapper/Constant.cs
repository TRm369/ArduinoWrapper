using System;

namespace ArduinoWrapper {
    public class Constant {
        //Constants declaration
        public static readonly byte[] Beginning = { 0x48, 0x69 };
        public static readonly byte ConnectingIntro = 0x11;
        public static readonly byte ConnectionSuccessful = 0x12;

        public static readonly Message ACK = new Message(new byte[] { 0x06 });
        public static readonly Message Disconnect = new Message(new byte[] { 0x13, 0x00, 0x00 });
        public static readonly Message KeepAlive = new Message(new byte[] { 0x16, 0x00, 0x00 });

        public static readonly byte pinModeCommand = 0x20;
        public static readonly byte digitalWriteCommand = 0x21;
        public static readonly byte analogWriteCommand = 0x22;
        public static readonly byte EnableAnalogReadCommand = 0x23;

        public static readonly byte digitalReadCommand = 0x30;
        public static readonly byte analogReadCommand = 0x31;

        public static readonly ArduinoBoardSpecification ArduinoDue = new ArduinoBoardSpecification(54, 12);
    }
}

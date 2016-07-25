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
    }
}

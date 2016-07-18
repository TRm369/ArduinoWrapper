using System;

namespace ArduinoWrapper {
    public class Constant {
        //Constants declaration
        public static readonly byte[] Beginning = { 0x48, 0x69 };
        public static readonly Message ACK = new Message(new byte[] { 0x06 });
    }
}

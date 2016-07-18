using System;

namespace ArduinoWrapper {
    public class Message {
        public byte[] Data { get; protected set; }
        public short Length {
            get {
                return (short)Data.Length;
            }
        }
        public byte Checksum { get; protected set; }

        public Message(byte[] Data, byte Checksum) {
            this.Data = Data;
            this.Checksum = Checksum;
        }

        public Message(byte[] Data) {
            this.Data = Data;
            this.Checksum = CalculateChecksum();
        }

        public bool DataCorrupted() {
            return Checksum != CalculateChecksum();
        }

        public byte CalculateChecksum() {
            int sum = 0;
            foreach (byte b in Data)
                sum += b;
            return (byte)(sum % 256);
        }

        public byte[] Header() {
            byte[] header = new byte[5];
            header[0] = Constant.Beginning[0];
            header[1] = Constant.Beginning[1];
            Util.ShortToByteArray(Length).CopyTo(header, 2);
            header[4] = CalculateChecksum();
            return header;
        }

        public bool IsACK() {
            return Data[0] == 0x06;
        }
    }
}
using System;

namespace ArduinoWrapper {
    public class Message {
        protected int DataCount;

        public byte[] Data { get; protected set; }
        public short Length {
            get {
                return (short)(Data.Length + 5);
            }
        }
        public short DataLength {
            get {
                return (short)Data.Length;
            }
        }
        public byte Checksum { get; protected set; }
        public int ResendCount = 0;

        public Message(byte[] Data, byte Checksum) {
            this.Data = Data;
            this.DataCount = Data.Length;
            this.Checksum = Checksum;
        }

        public Message(byte[] Data) {
            this.Data = Data;
            this.DataCount = Data.Length;
            this.Checksum = CalculateChecksum();
        }

        public Message(int Length) {
            Data = new byte[Length];
            DataCount = 0;
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
            Util.ShortToByteArray(DataLength).CopyTo(header, 2);
            header[4] = CalculateChecksum();
            return header;
        }

        public bool IsACK() {
            return Data[0] == 0x06;
        }

        public bool AppendData (byte[] Data) {
            if (DataCount + Data.Length > this.Data.Length)
                return false;
            Data.CopyTo(this.Data, DataCount);
            DataCount += Data.Length;
            return true;
        }

        public void Trim() {
            if (DataCount == DataLength)
                return;

            byte[] newData = new byte[DataCount];
            for (int i = 0; i < DataCount; i++) {
                newData[i] = Data[i];
            }
            Data = newData;
        }
    }
}
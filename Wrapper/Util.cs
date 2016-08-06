using System;

namespace ArduinoWrapper {
    public class Util {
        protected static readonly int Pow2_8 = (int)Math.Pow(2,8);
        protected static readonly int Pow2_16 = (int)Math.Pow(2, 16);
        protected static readonly int Pow2_24 = (int)Math.Pow(2, 24);

        public static byte[] IntToByteArray(int value) {
            byte[] array = new byte[4];
            int temp = value;
            array[0] = (byte)Math.Floor((decimal)temp / Pow2_24);
            temp = temp % Pow2_24;
            array[1] = (byte)Math.Floor((decimal)temp / Pow2_16);
            temp = temp % Pow2_16;
            array[2] = (byte)Math.Floor((decimal)temp / Pow2_8);
            array[3] = (byte)(temp % Pow2_8);

            return array;
        }

        public static byte[] ShortToByteArray(short value) {
            byte[] array = new byte[2];
            short temp = value;
            array[0] = (byte)Math.Floor((decimal)temp / Pow2_8);
            array[1] = (byte)(temp % Pow2_8);

            return array;
        }

        public static short ByteArrayToShort(byte[] value) {
            return (short)(value[0] * Pow2_8 + value[1]);
        }

        public static short ByteArrayToShort(byte byte0, byte byte1) {
            return (short)(byte0 * Pow2_8 + byte1);
        }

        public static byte BoolToByte (bool value) {
            if (value)
                return 1;
            else
                return 0;
        }
        public static bool ByteToBool (byte value) {
            if (value == 1)
                return true;
            else
                return false;
        }
    }
}

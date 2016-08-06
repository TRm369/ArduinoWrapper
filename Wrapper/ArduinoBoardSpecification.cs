using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoWrapper {
    public class ArduinoBoardSpecification {
        public int PinCount { get; protected set; }
        public int AnalogPinCount { get; protected set; }

        public ArduinoBoardSpecification (int PinCount, int AnalogPinCount) {
            this.PinCount = PinCount;
            this.AnalogPinCount = AnalogPinCount;
        }
    }
}

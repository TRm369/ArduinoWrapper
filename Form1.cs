using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArduinoWrapper {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
            ss = stupid;
        }

        Arduino ard = new Arduino(Constant.ArduinoDue);

        private void button1_Click(object sender, EventArgs e) {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
        }

        private void button2_Click(object sender, EventArgs e) {
            ard.Port = (string)comboBox1.SelectedItem;
            //ard.AnalogReadInterval = 2000;
            ard.Connect();
            checkBox1.Checked = ard.IsConnected;
        }

        private void button3_Click(object sender, EventArgs e) {
            ard.EnableAnalogRead(0, true);
            ard.SetPinMode(3, Arduino.PinMode.OUTPUT);
            ard.SetPinMode(44, Arduino.PinMode.OUTPUT);
            ard.SetPinMode(42, Arduino.PinMode.OUTPUT);
            ard.SetPinMode(37, Arduino.PinMode.INPUT);
            ard.DigitalWrite(44, true);
            ard.RegisterOnInputChageCallback(a);
            ard.RegisterOnAnalogInputChageCallback(b);
        }

        public void a (int pin, bool sig) {
            Console.WriteLine("Pin " + pin + " changed status to " + sig);
            ard.DigitalWrite(42, ard.PinSignal[37]);
        }

        public void b (int pin, short sig) {
            Console.WriteLine("Analog pin " + pin + " changed status to " + sig);
            if (pin == 0) trackBar2.Invoke(ss,sig);
        }

        public delegate void s(int val);
        public s ss;
        public void stupid (int val) {
            trackBar2.Value = val;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            ard.Disconnect();
        }

        private void button4_Click(object sender, EventArgs e) {
            ard.Disconnect();
            checkBox1.Checked = ard.IsConnected;
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e) {
            ard.AnaloglWrite(3, (byte)trackBar1.Value);
        }
    }
}

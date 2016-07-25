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
        }

        ArduinoSerialPort ard = new ArduinoSerialPort();

        private void button1_Click(object sender, EventArgs e) {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
        }

        private void button2_Click(object sender, EventArgs e) {
            ard.Port = (string)comboBox1.SelectedItem;
            ard.Connect();
            checkBox1.Checked = ard.IsConnected;
        }

        private void button3_Click(object sender, EventArgs e) {
            ard.DEBUG(textBox1.Text);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            ard.Disconnect();
        }

        private void button4_Click(object sender, EventArgs e) {
            ard.Disconnect();
            checkBox1.Checked = ard.IsConnected;
        }
    }
}

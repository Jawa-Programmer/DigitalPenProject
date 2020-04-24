using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows.Forms;

namespace PenProject_test
{
    public partial class Form1 : Form
    {
        List<string> data_s = new List<string>();
        public Form1()
        {
            InitializeComponent();
            string[] ports = SerialPort.GetPortNames();
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(ports);
        }


        void data(object sender, SerialDataReceivedEventArgs e)
        {

            while (port1.BytesToRead < 12) ;

            byte[] arr = new byte[12];
            port1.Read(arr, 0, 12);
            short[] data = new short[6];
            for (int i = 0; i < 6; i++)
            {
                data[i] = (short)((arr[i * 2] << 8) & 0xff00 | (arr[i * 2 + 1]));
            }
            this.BeginInvoke((Action)(() =>
            {
                richTextBox1.Text = "ax: " + data[0] + ", ay: " + data[1] + ", az: " + data[2];
                richTextBox1.ScrollToCaret();
            }));
        }
        private void button1_Click(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(ports);
        }
        SerialPort port1;
        private void button2_Click(object sender, EventArgs e)
        {
            port1 = new SerialPort(comboBox1.SelectedItem.ToString(), 1000000);
            port1.Open();
            port1.DataReceived += data;
            richTextBox1.Text += "connected\n";
        }
    }
}

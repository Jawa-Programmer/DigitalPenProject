using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace Canvas_pen
{

    struct angles
    {
        public float alpha, beta, gamma;
    };

    class Point
    {
        public float x, y; public int count;
        public Point(float nx, float ny) { x = nx; y = ny; count = 1; }
    }

    public partial class Form1 : Form
    {
        static Queue<angles> angls = new Queue<angles>();
        static List<Point> points = new List<Point>();
        void data(object sender, SerialDataReceivedEventArgs e)
        {
            while (srp.ReadByte() != 0xFF) if (srp.BytesToRead <= 0) return;
            while (srp.ReadByte() != 0x00) if (srp.BytesToRead <= 0) return;
            string[] data = srp.ReadLine().Replace('.', ',').Split('\t');

            angles a = new angles();

            a.alpha = float.Parse(data[0]) / 10000f;
            a.beta = float.Parse(data[1]) / 10000f;
            a.gamma = float.Parse(data[2]) / 10000f;
            lock (angls)
                angls.Enqueue(a);
        }

        const float PI_05 = (float)Math.PI / 2;
        static float hypot(float x, float y) { return (float)Math.Sqrt(x * x + y * y); }
        //const float K = 0.9f;
        Task thread = new Task(() =>
        {

            while (true)
            {
                lock (angls)
                    while (angls.Count > 0)
                    {
                        angles a = angls.Dequeue();
                        float gam = PI_05 - a.gamma + a.beta;
                        //float gam = a.gamma;

                        // gam *= -1;
                        while (gam > Math.PI) gam -= (float)Math.PI * 2;
                        while (gam < -Math.PI) gam += (float)Math.PI * 2;

                        // Console.WriteLine("{0:f4}\t{1:f4}\t{2:f4}\t{3:f2}", alpha * toDeg, beta * toDeg, gamma * toDeg, gam * toDeg);

                        //  Console.WriteLine("{0:f3}                  \n{1:f3}         ", ang1 * 180 / Math.PI, azim2 * 180 / Math.PI);

                        float l1 = arm_len * (float)Math.Tan(a.alpha);

                        float l2 = l1 - (float)Math.Sin(a.alpha);



                        int px = (int)(200 + l1 * 250 * Math.Cos(gam));
                        int pz = (int)(200 + l1 * 250 * Math.Sin(gam));


                        if (px > 398 || pz > 398 || pz < 1 || px < 1) { px = 200; pz = 200; }
                        lock (points)
                        {
                            if (points.Count == 0) points.Add(new Point(px, pz));
                            else
                            {
                                Point p = points[points.Count - 1];
                                if (hypot(p.x - px, p.y - pz) <= 10)
                                {
                                    float K = 1f / (p.count + 1);
                                    p.x = K * p.count * p.x + K * px;
                                    p.y = K * p.count * p.y + K * pz;
                                    p.count++;
                                }
                                else if (p.count < 6)
                                {
                                    points.RemoveAt(points.Count - 1);
                                    points.Add(new Point(px, pz));
                                }
                                else points.Add(new Point(px, pz));
                            }
                        }
                    }

                Thread.Sleep(10);
            }

        });
        static Pen blackPen = new Pen(Color.Black, 2);

        Task drawTh = new Task(() =>
        {
            int TIMER = 0;
            while (true)
            {
                using (var graphics = Graphics.FromImage(img))
                {
                    float px = 0, py = 0;
                    graphics.Clear(Color.White);
                    graphics.DrawRectangle(Pens.Red, 195, 195, 10, 10);
                    graphics.DrawString("count: " + points.Count, SystemFonts.DefaultFont, Brushes.Black, 10, 10);
                    lock (points)
                        for (int i = 0; i < points.Count; i++)
                        {
                            /*  if (TIMER >= 5 && points[i].count < 10)
                              {
                                  points.RemoveAt(i);
                                  i--;
                                  continue;
                              }*/
                            if (i > 0)
                            {
                                graphics.DrawLine(blackPen, px, py, points[i].x, points[i].y);
                            }
                            px = points[i].x;
                            py = points[i].y;
                        }
                }
                imbx.Image = img;
                TIMER++;
                if (TIMER > 5) TIMER = 0;
                Thread.Sleep(60);
            }
        });

        const float arm_len = 6.5f / 17f;

        SerialPort srp;
        public Form1()
        {
            InitializeComponent();
            string[] coms = SerialPort.GetPortNames();
            foreach (string a in coms)
                listView1.Items.Add(a);
            pictureBox1.Image = img;
            imbx = pictureBox1;
            thread.Start();
            drawTh.Start();
        }

        static Bitmap img = new Bitmap(400, 400);
        static PictureBox imbx;

        private void button1_Click(object sender, EventArgs e)
        {
            if (srp == null || !srp.IsOpen)
            {
                try
                {
                    button1.Enabled = false;
                    srp = new SerialPort(listView1.SelectedItems[0].Text, 57600);
                    srp.Open();
                    srp.DataReceived += data;
                    richTextBox1.Text = "Успешно подключен";
                }
                catch (Exception ex)
                {
                    richTextBox1.Text = ex.Message;
                    button1.Enabled = true;
                    if (srp != null && srp.IsOpen) srp.Close();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            string[] coms = SerialPort.GetPortNames();
            foreach (string a in coms)
                listView1.Items.Add(a);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            lock (points) points.Clear();
        }
    }
}

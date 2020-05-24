using System;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUI_DataSetCollector
{

    public partial class Form1 : Form
    {
        static double abs(double x) { return x >= 0 ? x : -x; }
        static double hypot(vector3d V) { return Math.Sqrt(V.x * V.x + V.y * V.y + V.z * V.z); }

        static double median(double a, double b, double c)
        {
            return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }
        static void medianFilter3x(vector3d[] vec)
        {
            for (int i = 1; i < vec.Length - 1; i++)
            {
                vec[i].x = median(vec[i - 1].x, vec[i].x, vec[i + 1].x);
                vec[i].y = median(vec[i - 1].y, vec[i].y, vec[i + 1].y);
                vec[i].z = median(vec[i - 1].z, vec[i].z, vec[i + 1].z);
            }
        }
        static SerialPort srp;
        static ringBuffer stk = new ringBuffer();
        static int Co = 0;
        static bool SIGNAL = false, READY_READ = false, SO_POWERFUL = false, IGNORE = false;
        static bool isRuning = true;
        static vector3d[] toDraw;
        static Panel panell;
        Task refr = new Task(() =>
        {
            vector3d[] buff;
            while (isRuning)
            {
                if (READY_READ)
                {
                    lock (stk)
                        buff = stk.getCopy();
                    READY_READ = false;
                    // IGNORE = true;
                    if (toDraw == null)
                    {
                        toDraw = new vector3d[33];
                        for (int i = 0; i < toDraw.Length; i++)
                        {
                            toDraw[i] = new vector3d();
                        }
                    }
                    medianFilter3x(buff);
                    for (int i = 1, j = 0; i < buff.Length; i += 3, j++)
                    {
                        toDraw[j].x = (buff[i - 1].x + buff[i].x + buff[i + 1].x) / 3;
                        toDraw[j].y = (buff[i - 1].y + buff[i].y + buff[i + 1].y) / 3;
                        toDraw[j].z = (buff[i - 1].z + buff[i].z + buff[i + 1].z) / 3;
                    }
                    panell.BeginInvoke((Action)(() =>
                    {
                        panell.Refresh();
                    }));
                }
                Thread.Sleep(10);
            }

        });

        private void button1_Click(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            listView1.Items.Clear();
            foreach (string s in ports) { listView1.Items.Add(s); }
        }
        static Pen pen = new Pen(Color.LightGray);
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);
            float wd = panel1.Width / 32;
            pen.Color = Color.LightGray;
            pen.Width = 1;
            for (int x = 1; x < 33; x++)
            {
                g.DrawLine(pen, x * wd, 0, x * wd, panel1.Height);
            }
            float hd = panel1.Height / 47;
            for (int y = 1; y < 48; y++)
            {
                g.DrawLine(pen, 0, y * hd, panel1.Width, y * hd);
            }
            pen.Color = Color.Gray;
            pen.Width = 2;
            g.DrawLine(pen, 0, hd * 24, panel1.Width, 24 * hd);

            if (toDraw != null)
            {
                pen.Color = Color.Blue;
                pen.Width = 1;
                double py = toDraw[0].x;
                for (int x = 1; x < 33; x++)
                {
                    g.DrawLine(pen, (x - 1)*wd, panel1.Height / 2 + (float)py * hd, x*wd, panel1.Height / 2 + (float)toDraw[x].x * hd);
                    py = toDraw[x].x;
                }
                pen.Color = Color.Red;
                py = toDraw[0].y;
                for (int x = 1; x < 33; x++)
                {
                    g.DrawLine(pen, (x - 1) * wd, panel1.Height / 2 + (float)py * hd, x * wd, panel1.Height / 2 + (float)toDraw[x].y * hd);
                    py = toDraw[x].y;
                }
                pen.Color = Color.Green;
                py = toDraw[0].z;
                for (int x = 1; x < 33; x++)
                {
                    g.DrawLine(pen, (x - 1) * wd, panel1.Height / 2 + (float)py * hd, x * wd, panel1.Height / 2 + (float)toDraw[x].z * hd);
                    py = toDraw[x].z;
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            isRuning = false;
            if (srp != null) srp.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (srp == null)
            {
                srp = new SerialPort(listView1.SelectedItems[0].Text, 57600);
                srp.DataReceived += dataRecived;
                try
                {
                    srp.Open();
                    button2.Text = "отключить";
                }
                catch (Exception ex)
                {
                    button2.Text = "подключить";
                    srp.Close();
                    srp.Dispose();
                    srp = null;
                }
            }
            else
                lock (srp)
                {
                    button2.Text = "подключить";
                    srp.Close();
                    srp.Dispose();
                    srp = null;
                }

        }

        static void dataRecived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (srp)
            {
                while (IGNORE && srp.BytesToRead > 0) srp.ReadByte();
                while (srp.ReadByte() != 0xff) ;
                while (srp.BytesToRead < 12) ;
                // while (srp.BytesToRead > 11)
                lock (stk)
                {
                    vector3d V = new vector3d();
                    byte[] data = new byte[12];
                    srp.Read(data, 0, 12);
                    V.x = BitConverter.ToSingle(data, 0);
                    V.y = BitConverter.ToSingle(data, 4);
                    V.z = BitConverter.ToSingle(data, 8);
                    V.x /= 400;
                    V.y /= 400;
                    V.z /= 400;

                    stk.add(V);

                    if (!SIGNAL && !READY_READ && (abs(V.x) > 10.0 || abs(V.y) > 10.0 || abs(V.z) > 10.0))
                    {
                        SO_POWERFUL = false;
                        SIGNAL = true;
                        Co = 0;
                    }
                    if (SIGNAL)
                    {
                        if (abs(V.x) > 60 || abs(V.y) > 60 || abs(V.z) > 60) SO_POWERFUL = true;
                        Co += 1;
                        if (Co >= 71)
                        {
                            READY_READ = true && !SO_POWERFUL;
                            SIGNAL = false;
                        }
                    }
                }
            }
        }
        public Form1()
        {
            InitializeComponent();
            string[] ports = SerialPort.GetPortNames();
            listView1.Items.Clear();
            foreach (string s in ports) { listView1.Items.Add(s); }
            panell = panel1;
            refr.Start();
        }
    }

    class vector3d
    {
        public vector3d() { x = 0; y = 0; z = 0; }
        public double x, y, z;
    };
    class NeuroLink
    {
        public static double SigmoidGrad(double x)
        {
            return ((1 - x) * x);
        }
        public static double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-x));
        }
        private class Neuron
        {
            public Neuron(int prk)
            {
                kf = new double[prk];
                dkf = new double[prk];
            }
            //коэфециенты входящих синапсисов
            public double[] kf, dkf;
            public double output, delta, bias, dbias = 0;
            public void update(Neuron[] neus)
            {
                output = bias;
                for (int i = 0; i < neus.Length; i++) output += neus[i].output * kf[i];
                output = Sigmoid(output);
            }
            public void findDelta(int pos, Neuron[] neus)
            {
                double sum = 0;
                for (int i = 0; i < neus.Length; i++) sum += neus[i].kf[pos] * neus[i].delta;
                delta = sum * SigmoidGrad(output);
            }
            public void findDelta(double cor)
            {
                delta = (cor - output) * SigmoidGrad(output);
            }
            public void correctWeights(Neuron[] neus)
            {
                for (int i = 0; i < kf.Length; i++)
                {
                    double gradient = neus[i].output * delta;
                    double dw = E * gradient + A * dkf[i];
                    dkf[i] = dw;
                    kf[i] += dw;
                }
                {
                    double gradient = delta;
                    double dw = E * gradient + A * dbias;
                    dbias = dw;
                    bias += dw;
                }
            }
        }
        const int INP = 99, H1 = 60, H2 = 30, O = 10;
        //E - обучаемость, A - инертность
        const double E = 0.000005, A = 0.05;
        //массив массивов нейронов. Нейроны распределны по массивам в соответсвии со слоем
        Neuron[][] layouts = new Neuron[4][];
        string filepath;
        public NeuroLink(string fl = "neurolink.txt")
        {
            filepath = fl;
            layouts[0] = new Neuron[INP];
            layouts[1] = new Neuron[H1];
            layouts[2] = new Neuron[H2];
            layouts[3] = new Neuron[O];
            if (File.Exists(filepath))
            {
                StreamReader sr = new StreamReader(filepath);
                string[] kf = sr.ReadToEnd().Split(' ');
                sr.Close();

                int kk = 0;
                for (int i = 0; i < layouts[0].Length; i++)
                {
                    layouts[0][i] = new Neuron(0);
                }
                for (int i = 0; i < layouts[1].Length; i++)
                {
                    layouts[1][i] = new Neuron(INP);
                    for (int j = 0; j < layouts[1][i].kf.Length; j++)
                    {
                        layouts[1][i].kf[j] = double.Parse(kf[kk]);
                        kk++;
                    }
                    layouts[1][i].bias = double.Parse(kf[kk]);
                    kk++;
                }
                for (int i = 0; i < layouts[2].Length; i++)
                {
                    layouts[2][i] = new Neuron(H1);
                    for (int j = 0; j < layouts[2][i].kf.Length; j++)
                    {
                        layouts[2][i].kf[j] = double.Parse(kf[kk]);
                        kk++;
                    }
                    layouts[2][i].bias = double.Parse(kf[kk]);
                    kk++;
                }
                for (int i = 0; i < layouts[3].Length; i++)
                {
                    layouts[3][i] = new Neuron(H2);
                    for (int j = 0; j < layouts[3][i].kf.Length; j++)
                    {
                        layouts[3][i].kf[j] = double.Parse(kf[kk]);
                        kk++;
                    }
                    layouts[3][i].bias = double.Parse(kf[kk]);
                    kk++;
                }
            }
            else
            {
                Random r = new Random();
                StreamWriter sw = new StreamWriter(filepath, false, System.Text.Encoding.Default);
                for (int i = 0; i < layouts[0].Length; i++)
                {
                    layouts[0][i] = new Neuron(0);
                }
                for (int i = 0; i < layouts[1].Length; i++)
                {
                    layouts[1][i] = new Neuron(INP);
                    for (int j = 0; j < layouts[1][i].kf.Length; j++)
                    {
                        double ko = (r.NextDouble() * 5) - 2.5;
                        layouts[1][i].kf[j] = ko;
                        sw.Write("{0} ", ko);
                    }
                    layouts[1][i].bias = (r.NextDouble() * 5) - 2.5;
                    sw.Write("{0} ", layouts[1][i].bias);
                }
                for (int i = 0; i < layouts[2].Length; i++)
                {
                    layouts[2][i] = new Neuron(H1);
                    for (int j = 0; j < layouts[2][i].kf.Length; j++)
                    {
                        double ko = (r.NextDouble() * 5) - 2.5;
                        layouts[2][i].kf[j] = ko;
                        sw.Write("{0} ", ko);
                    }
                    layouts[2][i].bias = (r.NextDouble() * 10) - 5;
                    sw.Write("{0} ", layouts[2][i].bias);
                }
                for (int i = 0; i < layouts[3].Length; i++)
                {
                    layouts[3][i] = new Neuron(H2);
                    for (int j = 0; j < layouts[3][i].kf.Length; j++)
                    {
                        double ko = (r.NextDouble() * 5) - 2.5;
                        layouts[3][i].kf[j] = ko;
                        sw.Write("{0} ", ko);
                    }
                    layouts[3][i].bias = (r.NextDouble() * 5) - 2.5;
                    sw.Write("{0} ", layouts[3][i].bias);
                }
                sw.Close();
            }

        }

        public void think(double[] data)
        {
            for (int i = 0; i < layouts[0].Length; i++)
                layouts[0][i].output = Sigmoid(data[i]);
            for (int j = 1; j < layouts.Length; j++)
                for (int i = 0; i < layouts[j].Length; i++)
                    layouts[j][i].update(layouts[j - 1]);
        }
        public double[] result()
        {
            double[] uns = new double[O];

            for (int i = 0; i < O; i++) uns[i] = layouts[layouts.Length - 1][i].output;
            return uns;
        }
        public void learn(double[] correct)
        {

            for (int i = 0; i < O; i++)
                layouts[layouts.Length - 1][i].findDelta(correct[i]);
            for (int i = layouts.Length - 2; i > 0; i--)
            {
                for (int j = 0; j < layouts[i].Length; j++)
                {
                    layouts[i][j].findDelta(j, layouts[i + 1]);
                }
            }
            for (int i = layouts.Length - 1; i > 0; i--)
            {
                for (int j = 0; j < layouts[i].Length; j++)
                {
                    layouts[i][j].correctWeights(layouts[i - 1]);
                }
            }
        }
        public double curError(double[] correct)
        {
            double un = 0;
            for (int i = 0; i < layouts[layouts.Length - 1].Length; i++)
                un += (correct[i] - layouts[layouts.Length - 1][i].output) * (correct[i] - layouts[layouts.Length - 1][i].output);
            return un / layouts.Length;
        }
        public void saveToFile()
        {
            StreamWriter sw = new StreamWriter(filepath, false, System.Text.Encoding.Default);

            for (int i = 0; i < layouts[1].Length; i++)
            {
                for (int j = 0; j < layouts[1][i].kf.Length; j++)
                {
                    sw.Write("{0} ", layouts[1][i].kf[j]);
                }
                sw.Write("{0} ", layouts[1][i].bias);
            }
            for (int i = 0; i < layouts[2].Length; i++)
            {
                for (int j = 0; j < layouts[2][i].kf.Length; j++)
                {
                    sw.Write("{0} ", layouts[2][i].kf[j]);
                }
                sw.Write("{0} ", layouts[2][i].bias);
            }
            for (int i = 0; i < layouts[3].Length; i++)
            {
                for (int j = 0; j < layouts[3][i].kf.Length; j++)
                {
                    sw.Write("{0} ", layouts[3][i].kf[j]);
                }
                sw.Write("{0} ", layouts[3][i].bias);
            }
            sw.Close();
        }
    }


    class ringBuffer
    {
        private vector3d[] data;

        const int SIZE = 99;

        private int head = 0;

        public ringBuffer()
        {
            data = new vector3d[SIZE];
            for (int i = 0; i < SIZE; i++) data[i] = new vector3d();
        }
        public void add(vector3d vec)
        {
            data[head++] = vec;
            head %= SIZE;
        }
        public vector3d peek()
        {
            return (head > 0) ? data[head - 1] : data[SIZE - 1];
        }
        public vector3d[] getCopy()
        {
            vector3d[] copy = new vector3d[SIZE];
            int h = head;
            for (int i = 0; i < SIZE; i++)
            {
                copy[i] = data[h++];
                h %= SIZE;
            }
            return copy;
        }

    }

    class dataset
    {
        public double[] data = new double[99], result = new double[10];
        public dataset(string path)
        {
            StreamReader sr = new StreamReader(path, Encoding.Default);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = double.Parse(sr.ReadLine()) / 3;
            }
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = double.Parse(sr.ReadLine());
            }
            sr.Close();
        }
    }


}

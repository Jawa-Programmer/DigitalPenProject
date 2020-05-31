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
        const int LERNINGS = 200, TESTINGS = 20;

        static NeuroLink link = new NeuroLink();
        static AppMode appMode = AppMode.COLLECTING;
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
        static bool isRuning = true, isLearning = false;
        static vector3d[] toDraw;
        static vector4d prevError = new vector4d(), curError = new vector4d();
        static Panel panell;
        static RadioButton lern, testing, collecting;
        static Bitmap buferImage;
        static StringBuilder STR = new StringBuilder();
        static int maxPos(double[] vect)
        {
            double max = -2; int pos = 0;
            for (int i = 0; i < vect.Length; i++)
            {
                if (max < vect[i]) { max = vect[i]; pos = i; }
            }
            return pos;
        }
        void Demiss()
        {
            if (appMode != AppMode.COLLECTING) return;
            toDraw = null;
            panel1.Refresh();
            IGNORE = false;
        }
        void Accept()
        {
            if (appMode != AppMode.COLLECTING || !IGNORE || toDraw == null) return;
            string path = (checkBox1.Checked ? "t" : "l") + numericUpDown1.Value + "/";
            Directory.CreateDirectory(path);
            string[] fls = Directory.GetFiles(path);
            int nxtfl = 0;
            foreach (string f in fls)
            {
                int tmp = int.Parse(f.Substring(3, f.Length - 7));
                if (nxtfl < tmp + 1) nxtfl = tmp + 1;
            }
            StreamWriter sw = new StreamWriter(path + nxtfl + ".txt", false, Encoding.Default);
            for (int i = 0; i < toDraw.Length; i++)
            {
                sw.WriteLine(toDraw[i].x);
                sw.WriteLine(toDraw[i].y);
                sw.WriteLine(toDraw[i].z);
            }
            for (int i = 0; i < 10; i++)
            {
                sw.WriteLine(i == numericUpDown1.Value ? 1.0 : 0.0);
            }
            sw.Close();
            sw.Dispose();
            IGNORE = false;
            label2.Text = "уже собрано: " + (fls.Length + 1) + " из " + (checkBox1.Checked ? TESTINGS : LERNINGS);
        }

        Action dataRefr = new Action(() =>
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

                    if (appMode == AppMode.TESTING)
                    {
                        double[] buffr = new double[99];
                        for (int i = 0; i < toDraw.Length; i++)
                        {
                            buffr[3 * i] = toDraw[i].x;
                            buffr[3 * i + 1] = toDraw[i].y;
                            buffr[3 * i + 2] = toDraw[i].z;
                        }
                        link.think(buffr);
                        buffr = link.result();
                        char ch = '_';
                        int mx = maxPos(buffr);
                        if (buffr[mx] >= 0)
                            switch (mx)
                            {
                                case 0:
                                    ch = '0';
                                    break;
                                case 1:
                                    ch = '1';
                                    break;
                                case 2:
                                    ch = '2';
                                    break;
                                case 3:
                                    ch = '3';
                                    break;
                                case 4:
                                    ch = '4';
                                    break;
                                case 5:
                                    ch = '5';
                                    break;
                                case 6:
                                    ch = '6';
                                    break;
                                case 7:
                                    ch = '7';
                                    break;
                                case 8:
                                    ch = '8';
                                    break;
                                case 9:
                                    ch = '9';
                                    break;
                            }
                        STR.Append(ch);
                    }

                    panell.BeginInvoke((Action)(() =>
                    {
                        panell.Refresh();
                    }));
                }
                Thread.Sleep(10);
            }

        });
        Action learnStatus = new Action(() =>
        {
            inited = false;
            curError = new vector4d();

            dataset[][] datasets = new dataset[10][];
            dataset[][] testsets = new dataset[10][];
            for (int i = 0; i < 10; i++)
            {
                string[] path = Directory.GetFiles("l" + i + "/");
                datasets[i] = new dataset[LERNINGS];
                for (int j = 0; j < LERNINGS; j++)
                {
                    datasets[i][j] = new dataset(path[j]);
                }
                path = Directory.GetFiles("t" + i + "/");
                testsets[i] = new dataset[TESTINGS];
                for (int j = 0; j < TESTINGS; j++)
                {
                    testsets[i][j] = new dataset(path[j]);
                }
            }
            link.EpochInit();
            for (; link.generation < 1000000; link.generation++)
            {
                if (!isLearning) break;
                double learnError = 0;
                for (int i = 0; i < LERNINGS; i++)
                {
                    for (int n = 0; n < 10; n++)
                    {
                        link.think(datasets[n][i].data);
                        learnError += link.curError(datasets[n][i].result);
                        link.learn(datasets[n][i].result);
                    }
                }
                link.fixRProp();
                if (link.generation % 500 == 0)
                {
                    learnError /= (LERNINGS * 10);
                    double mindError = 0, corct = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        for (int n = 0; n < 10; n++)
                        {
                            link.think(testsets[n][i].data);
                            mindError += link.curError(testsets[n][i].result);
                            int mx = maxPos(link.result());
                            if (mx == n && link.result()[n] > 0) corct++;
                        }
                    }
                    mindError /= (TESTINGS * 10);
                    corct /= (TESTINGS * 10);
                    prevError = curError;
                    curError = new vector4d();
                    curError.x = link.generation;
                    curError.y = mindError;
                    curError.z = learnError;
                    curError.t = corct;
                    StreamWriter sr = new StreamWriter("errors.csv", true, Encoding.Default);
                    sr.WriteLine("{0};{1};{2};{3}", link.generation, mindError, learnError, corct);
                    sr.Close();
                    panell.BeginInvoke((Action)(() =>
                    {
                        panell.Refresh();
                    }));
                }

                if (link.generation % 1000 == 0) { link.saveToFile(); link.saveToFileCopy(); }
            }
            Thread.Sleep(0);

        });
        Task refr, lernThr;

        private void button1_Click(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            listView1.Items.Clear();
            foreach (string s in ports) { listView1.Items.Add(s); }
        }
        static Pen pen = new Pen(Color.LightGray);

        private void button3_Click(object sender, EventArgs e)
        {
            Demiss();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Accept();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            label2.Text = (Directory.Exists((checkBox1.Checked ? "t" : "l") + numericUpDown1.Value + "/") ? ("уже собрано: " + (Directory.GetFiles((checkBox1.Checked ? "t" : "l") + numericUpDown1.Value + "/").Length)) : ("уже собрано: 0")) + " из " + (checkBox1.Checked ? TESTINGS : LERNINGS);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {

            switch (e.KeyCode)
            {
                case Keys.A:
                    Accept();
                    e.Handled = true;
                    break;
                case Keys.D:
                    Demiss();
                    e.Handled = true;
                    break;
                case Keys.T:
                    checkBox1.Checked = !checkBox1.Checked;
                    e.Handled = true;
                    break;
            }

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
                appMode = AppMode.COLLECTING;
            if (radioButton2.Checked)
                appMode = AppMode.LEARNING;
            if (radioButton3.Checked)
                appMode = AppMode.WATCHING_SIGNAL;
            if (radioButton4.Checked)
                appMode = AppMode.TESTING;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            switch (appMode)
            {
                case AppMode.COLLECTING:
                    button4.Enabled = true;
                    button3.Enabled = true;
                    if (refr == null)
                    {
                        isRuning = true;
                        refr = new Task(dataRefr);
                        refr.Start();
                    }
                    break;
                case AppMode.LEARNING:
                    if (refr != null)
                    {
                        isRuning = false;
                        refr.Wait();
                        refr.Dispose();
                        refr = null;
                    }
                    button5.Enabled = true;
                    break;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            isLearning = !isLearning;
            if (isLearning)
            {
                radioButton1.Enabled = false;
                radioButton2.Enabled = false;
                radioButton3.Enabled = false;
                radioButton4.Enabled = false;
                button5.Text = "остановить обучение";
                buferImage = new Bitmap(5000, 5000 * panel1.Height / panel1.Width);
                lernThr = new Task(learnStatus, TaskCreationOptions.LongRunning);
                lernThr.Start();
            }
            else
            {
                lernThr.Wait();
                lernThr.Dispose();
                lernThr = null;
                radioButton1.Enabled = true;
                radioButton2.Enabled = true;
                radioButton3.Enabled = true;
                radioButton4.Enabled = true;
                buferImage = new Bitmap(1000, 1000 * panel1.Height / panel1.Width);
                button5.Text = "начать обучение";
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            PMX = e.X;
            PMY = e.Y;
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            DX = PDX + e.X - PMX;
            DY = PDY + e.Y - PMY;
            panel1.Refresh();
            panel1.Update();
            PDX = DX;
            PDY = DY;
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                DX = PDX + e.X - PMX;
                DY = PDY + e.Y - PMY;
                panel1.Refresh();
            }
        }
        private void panel1_MouseWheel(object sender, MouseEventArgs e)
        {
            SCALE += e.Delta * 0.0005f;
            panel1.Refresh();
        }

        static bool inited = false;

        static int DX, DY, PDX, PDY, PMX, PMY, prevGen = -1;
        static float SCALE = 1;
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            switch (appMode)
            {
                case AppMode.TESTING:
                case AppMode.COLLECTING:
                case AppMode.WATCHING_SIGNAL:
                    {
                        if (appMode == AppMode.TESTING)
                            richTextBox2.Text = STR.ToString();
                        Graphics g = Graphics.FromImage(buferImage);
                        g.Clear(Color.White);
                        float wd = buferImage.Width / 32;
                        pen.Color = Color.LightGray;
                        pen.Width = 1;
                        for (int x = 1; x < 33; x++)
                        {
                            g.DrawLine(pen, x * wd, 0, x * wd, buferImage.Height);
                        }
                        float hd = buferImage.Height / 23;
                        for (int y = 1; y < 24; y++)
                        {
                            g.DrawLine(pen, 0, y * hd, buferImage.Width, y * hd);
                        }
                        pen.Color = Color.Gray;
                        pen.Width = 2;
                        g.DrawLine(pen, 0, hd * 12, buferImage.Width, 12 * hd);

                        if (toDraw != null)
                        {
                            pen.Color = Color.Blue;
                            pen.Width = 1;
                            double py = toDraw[0].x;
                            for (int x = 1; x < 33; x++)
                            {
                                g.DrawLine(pen, (x - 1) * wd, buferImage.Height / 2 - (float)py * hd, x * wd, buferImage.Height / 2 - (float)toDraw[x].x * hd);
                                py = toDraw[x].x;
                                if (x % 5 == 0) g.DrawString("" + Math.Round(py, 2), System.Drawing.SystemFonts.DefaultFont, Brushes.Blue, x * wd, buferImage.Height / 2 - (float)py * hd);
                            }
                            pen.Color = Color.Red;
                            py = toDraw[0].y;
                            for (int x = 1; x < 33; x++)
                            {
                                g.DrawLine(pen, (x - 1) * wd, buferImage.Height / 2 - (float)py * hd, x * wd, buferImage.Height / 2 - (float)toDraw[x].y * hd);
                                py = toDraw[x].y;
                                if (x % 5 == 2) g.DrawString("" + Math.Round(py, 2), System.Drawing.SystemFonts.DefaultFont, Brushes.Red, x * wd, buferImage.Height / 2 - (float)py * hd);
                            }
                            pen.Color = Color.Green;
                            py = toDraw[0].z;
                            for (int x = 1; x < 33; x++)
                            {
                                g.DrawLine(pen, (x - 1) * wd, buferImage.Height / 2 - (float)py * hd, x * wd, buferImage.Height / 2 - (float)toDraw[x].z * hd);
                                py = toDraw[x].z;
                                if (x % 5 == 4) g.DrawString("" + Math.Round(py, 2), System.Drawing.SystemFonts.DefaultFont, Brushes.Green, x * wd, buferImage.Height / 2 - (float)py * hd);
                            }
                        }
                        e.Graphics.DrawImage(buferImage, DX, DY, buferImage.Width * SCALE, buferImage.Height * SCALE);
                    }
                    break;
                case AppMode.LEARNING:
                    {
                        Graphics g = Graphics.FromImage(buferImage);
                        if (!inited)
                        {
                            inited = true;
                            g.Clear(Color.White);
                            float wd = buferImage.Width / 100;
                            float hd = buferImage.Height / 4;
                            pen.Color = Color.LightGray;
                            pen.Width = 1;
                            for (int x = 1; x < 110; x++)
                            {
                                g.DrawLine(pen, x * wd, 0, x * wd, buferImage.Height);
                            }
                            for (int y = 1; y < 5; y++)
                            {
                                g.DrawLine(pen, 0, y * hd, buferImage.Width, y * hd);
                            }
                            pen.Color = Color.Gray;
                            pen.Width = 2;
                            g.DrawLine(pen, 0, hd * 5, buferImage.Width, 5 * hd);
                        }
                        float kx = buferImage.Width / 1000000f, ky = buferImage.Height;
                        pen.Color = Color.Red;
                        pen.Width = 1;
                        g.DrawLine(pen, (float)prevError.x * kx, (1 - (float)prevError.y) * ky, (float)curError.x * kx, (1 - (float)curError.y) * ky);
                        pen.Color = Color.Green;
                        g.DrawLine(pen, (float)prevError.x * kx, (1 - (float)prevError.z) * ky, (float)curError.x * kx, (1 - (float)curError.z) * ky);
                        pen.Color = Color.Blue;
                        g.DrawLine(pen, (float)prevError.x * kx, (1 - (float)prevError.t) * ky, (float)curError.x * kx, (1 - (float)curError.t) * ky);
                        if (curError.x > prevGen)
                        {
                            richTextBox2.AppendText("gen#" + curError.x + "; ошибка обобщения: " + curError.y + "; ошибка обучения: " + curError.z + "; Степень распознания: " + curError.t + "\n");
                            richTextBox2.ScrollToCaret();
                            prevGen = (int)curError.x;
                        }
                        e.Graphics.DrawImage(buferImage, DX, DY, buferImage.Width * SCALE, buferImage.Height * SCALE);
                    }
                    break;
            }
            System.GC.Collect();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            isRuning = false;
            if (srp != null) lock (srp)
                {
                    srp.Close();
                    srp.Dispose();
                    srp = null;
                }
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
                    richTextBox1.AppendText("Ручка успешно подключена\n");
                    richTextBox1.ScrollToCaret();
                }
                catch (Exception ex)
                {
                    button2.Text = "подключить";
                    srp.Close();
                    srp.Dispose();
                    srp = null;
                    richTextBox1.AppendText(ex.Message + "\n");
                    richTextBox1.ScrollToCaret();
                }
            }
            else
                lock (srp)
                {
                    button2.Text = "подключить";
                    srp.Close();
                    srp.Dispose();
                    srp = null;
                    richTextBox1.AppendText("Ручка отключена\n");
                    richTextBox1.ScrollToCaret();
                }

        }

        static void dataRecived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (srp)
            {
                if (IGNORE) { while (srp.BytesToRead > 0) srp.ReadByte(); return; }
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
                    V.x /= 1600;
                    V.y /= 1600;
                    V.z /= 1600;

                    stk.add(V);

                    if (!SIGNAL && !READY_READ && (abs(V.x) > 2.3 || abs(V.y) > 2.3 || abs(V.z) > 2.3))
                    {
                        SO_POWERFUL = false;
                        SIGNAL = true;
                        Co = 0;
                    }
                    if (SIGNAL)
                    {
                        if (abs(V.x) > 9 || abs(V.y) > 9 || abs(V.z) > 9) SO_POWERFUL = true;
                        Co += 1;
                        if (Co >= 71)
                        {
                            READY_READ = true && !SO_POWERFUL;
                            if (collecting.Checked) IGNORE = READY_READ;
                            SIGNAL = false;
                        }
                    }
                }
            }
        }
        public Form1()
        {
            InitializeComponent();
            buferImage = new Bitmap(1000, 1000 * panel1.Height / panel1.Width);
            SCALE = (float)panel1.Height / buferImage.Height;
            label2.Text = (Directory.Exists((checkBox1.Checked ? "t" : "l") + numericUpDown1.Value + "/") ? ("уже собрано: " + (Directory.GetFiles((checkBox1.Checked ? "t" : "l") + numericUpDown1.Value + "/").Length)) : ("уже собрано: 0")) + " из " + (checkBox1.Checked ? TESTINGS : LERNINGS);
            KeyPreview = true;
            string[] ports = SerialPort.GetPortNames();
            listView1.Items.Clear();
            foreach (string s in ports) { listView1.Items.Add(s); }
            panell = panel1;
            collecting = radioButton1;
            lern = radioButton2;
            testing = radioButton3;
            refr = new Task(dataRefr);
            refr.Start();
        }
    }

    class vector3d
    {
        public double x, y, z;
    };
    class vector4d
    {
        public double x, y, z, t;
    };
    class NeuroLink
    {
        /*
        public static double SigmoidGrad(double x)
        {
            return ((1 - x) * x);
        }
        public static double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-x));
        }*/
        public static double TanhGrad(double fx)
        {
            return 1 - fx * fx;
        }
        private class Neuron
        {
            public Neuron(int prk)
            {
                kf = new double[prk];
                dkf = new double[prk];

                // далее только для упругого спуска

                grad = new double[prk];
                prevGrSig = new bool[prk];
                dbias = 0.1;
                for (int i = 0; i < prk; i++) dkf[i] = 0.1;
            }
            //коэфециенты входящих синапсисов
            public double[] kf, dkf, grad;
            bool[] prevGrSig;
            bool prevBGrSig;
            public double output, delta, bias, dbias, biasGrad;
            public void initGrads()
            {
                for (int i = 0; i < grad.Length; i++)
                    grad[i] = 0;
                biasGrad = 0;
            }
            public void update(Neuron[] neus)
            {
                output = bias;
                for (int i = 0; i < neus.Length; i++) output += neus[i].output * kf[i];
                output = Math.Tanh(output);
            }
            public void findDelta(int pos, Neuron[] neus)
            {
                double sum = 0;
                for (int i = 0; i < neus.Length; i++) sum += neus[i].kf[pos] * neus[i].delta;
                delta = sum * TanhGrad(output);
            }
            public void findDelta(double cor)
            {
                delta = (cor - output) * TanhGrad(output);
            }
            public void fixRProps()
            {
                for (int i = 0; i < grad.Length; i++)
                {
                    if (grad[i] == 0) continue;
                    bool sign = grad[i] > 0;
                    dkf[i] *= (sign == prevGrSig[i]) ? N : n;
                    if (dkf[i] < 0.000000001) dkf[i] = 0.000000001;
                    else if (dkf[i] > 50) dkf[i] = 50;
                    kf[i] += grad[i] > 0 ? dkf[i] : -dkf[i];
                    grad[i] = 0;
                    prevGrSig[i] = sign;
                }
                {
                    if (biasGrad == 0) return;
                    bool sign = biasGrad > 0;
                    dbias *= (sign == prevBGrSig) ? N : n;
                    if (dbias < 0.000000001) dbias = 0.000000001;
                    else if (dbias > 50) dbias = 50;
                    bias += biasGrad > 0 ? dbias : -dbias;
                    biasGrad = 0;
                    prevBGrSig = sign;
                }
            }
            public void correctWeights(Neuron[] neus)
            {//Метод простого градентного спуска
                /*
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
                }*/
                //метод упругого спуска
                for (int i = 0; i < kf.Length; i++)
                {
                    grad[i] += neus[i].output * delta;
                }
                {
                    biasGrad += delta;
                }
            }
        }
        public int generation;
        //const int INP = 99, H1 = 60, H2 = 30, O = 10;
        //const int INP = 99, H1 = 150, H2 = 60, O = 10;
        const int INP = 99, H1 = 100, H2 = 40, O = 10;
        //E - обучаемость, A - инертность
        const double E = 0.000005, A = 0.06;
        //Изменение прироста коэффециентов в случаях с градиентом одного или разных знаков
        const double n = 0.5, N = 1.2;
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
                generation = int.Parse(kf[0]);
                int kk = 1;
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
                sw.Write("0 ");
                for (int i = 0; i < layouts[0].Length; i++)
                {
                    layouts[0][i] = new Neuron(0);
                }
                for (int i = 0; i < layouts[1].Length; i++)
                {
                    layouts[1][i] = new Neuron(INP);
                    for (int j = 0; j < layouts[1][i].kf.Length; j++)
                    {
                        double ko = (r.NextDouble() * 10) - 5;
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
                        double ko = (r.NextDouble() * 10) - 5;
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
                        double ko = (r.NextDouble() * 10) - 5;
                        layouts[3][i].kf[j] = ko;
                        sw.Write("{0} ", ko);
                    }
                    layouts[3][i].bias = (r.NextDouble() * 5) - 2.5;
                    sw.Write("{0} ", layouts[3][i].bias);
                }
                sw.Close();
            }

        }

        
        public void EpochInit()
        {
            for (int i = 0; i < layouts.Length; i++) for (int j = 0; j < layouts[i].Length; j++) layouts[i][j].initGrads();
        }
        public void think(double[] data)
        {
            for (int i = 0; i < layouts[0].Length; i++)
                layouts[0][i].output = Math.Tanh(data[i]);
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
                int max = Math.Max(layouts[i].Length, layouts[i + 1].Length);
                for (int j = 0; j < max; j++)
                {
                    if (layouts[i].Length > j) layouts[i][j].findDelta(j, layouts[i + 1]);
                    if (layouts[i + 1].Length > j) layouts[i + 1][j].correctWeights(layouts[i]);
                }
            }
            for (int j = 0; j < layouts[1].Length; j++)
                layouts[1][j].correctWeights(layouts[0]);
        }

        public void fixRProp()
        {
            for (int i = 1; i < layouts.Length; i++) for (int j = 0; j < layouts[i].Length; j++) layouts[i][j].fixRProps();
        }
        public double curError(double[] correct)
        {
            double un = 0;
            for (int i = 0; i < layouts[layouts.Length - 1].Length; i++)
                un += (correct[i] - layouts[layouts.Length - 1][i].output) * (correct[i] - layouts[layouts.Length - 1][i].output);
            return un / layouts[layouts.Length - 1].Length;
        }
        public void saveToFileCopy()
        {
            Directory.CreateDirectory("neuro_history");
            File.Copy(filepath, "neuro_history/gen_" + generation + ".txt");
        }
        public void saveToFile()
        {
            StreamWriter sw = new StreamWriter(filepath, false, System.Text.Encoding.Default);
            sw.Write("{0} ", generation);
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
                data[i] = double.Parse(sr.ReadLine());
            }
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = double.Parse(sr.ReadLine());
            }
            sr.Close();
        }
    }

    enum AppMode { COLLECTING, LEARNING, WATCHING_SIGNAL, TESTING };
}

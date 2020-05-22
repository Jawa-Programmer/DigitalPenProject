using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace DataSetCollector
{
    class vector3d
    {
        public vector3d() { x = 0; y = 0; z = 0; }
        public double x, y, z;
    };
    class NeuroLink
    {
        /*public static double hyperTanGrad(double x)
          {
              return 1 - x * x;
          }*/
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
        const int INP = 48, H1 = 40, H2 = 20, O = 10;
        //E - обучаемость, A - инертность
        const double E = 0.000005, A = 0.05;
        //массив массивов нейронов. Нейроны распределны по массивам в соответсвии со слоем
        Neuron[][] layouts = new Neuron[4][];

        public NeuroLink()
        {
            layouts[0] = new Neuron[INP];
            layouts[1] = new Neuron[H1];
            layouts[2] = new Neuron[H2];
            layouts[3] = new Neuron[O];
            if (File.Exists("neurolink.txt"))
            {
                StreamReader sr = new StreamReader("neurolink.txt");
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
                StreamWriter sw = new StreamWriter("neurolink.txt", false, System.Text.Encoding.Default);
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
            StreamWriter sw = new StreamWriter("neurolink.txt", false, System.Text.Encoding.Default);

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

        const int SIZE = 80;

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
        public double[] data = new double[48], result = new double[10];
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
    class Program
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
        static System.Diagnostics.Stopwatch sw = new Stopwatch();
        static bool SIGNAL = false, READY_READ = false, SO_POWERFUL = false;
        static void dataRecived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (srp)
            {
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
                        sw.Restart();
                        //  sw.Restart();
                    }

                    if (SIGNAL)
                    {
                        if (abs(V.x) > 60.0 || abs(V.y) > 60.0 || abs(V.z) > 60.0) SO_POWERFUL = true;
                        Co += 1;
                        if (Co >= 60)
                        {
                            READY_READ = true && !SO_POWERFUL;
                            SIGNAL = false;
                            // sw.Stop();
                        }
                    }
                }
            }
        }

        //  static dataset[] DS1, DS2, DS3, TS1, TS2, TS3;

        static void Main(string[] args)
        {
            //Random rand = new Random();
            //NeuroLink link = new NeuroLink();
            /* Console.WriteLine("loading datasets and testing samples");

             {
                 string[] files = Directory.GetFiles(@"l1\");
                 DS1 = new dataset[files.Length];
                 for (int i = 0; i < files.Length; i++)
                     DS1[i] = new dataset(files[i]);
             }
             {
                 string[] files = Directory.GetFiles(@"l2\");
                 DS2 = new dataset[files.Length];
                 for (int i = 0; i < files.Length; i++)
                     DS2[i] = new dataset(files[i]);
             }
             {
                 string[] files = Directory.GetFiles(@"l3\");
                 DS3 = new dataset[files.Length];
                 for (int i = 0; i < files.Length; i++)
                     DS3[i] = new dataset(files[i]);
             }
             Console.WriteLine("Loaded {0} datasets for '1', {1} datasets for '2' and {2} datasets for '3'", DS1.Length, DS2.Length, DS3.Length);
             {
                 string[] files = Directory.GetFiles(@"t1\");
                 TS1 = new dataset[files.Length];
                 for (int i = 0; i < files.Length; i++)
                     TS1[i] = new dataset(files[i]);
             }
             {
                 string[] files = Directory.GetFiles(@"t2\");
                 TS2 = new dataset[files.Length];
                 for (int i = 0; i < files.Length; i++)
                     TS2[i] = new dataset(files[i]);
             }
             {
                 string[] files = Directory.GetFiles(@"t3\");
                 TS3 = new dataset[files.Length];
                 for (int i = 0; i < files.Length; i++)
                     TS3[i] = new dataset(files[i]);
             }
             Console.WriteLine("Loaded {0} testing sets for '1', {1} testing sets for '2' and {2} testing sets for '3'", TS1.Length, TS2.Length, TS3.Length);

             Console.WriteLine("learning started");
             Console.Beep();
             for (int gen = 0; gen < 5000000; gen++)
             {
                 for (int i = 0; i < 50; i++)
                 {
                     link.think(DS1[i].data);
                     link.learn(DS1[i].result);

                     link.think(DS2[i].data);
                     link.learn(DS2[i].result);

                     link.think(DS3[i].data);
                     link.learn(DS3[i].result);
                 }
                 if (gen % 1000 == 0)
                 {
                     double error = 0;
                     for (int i = 0; i < 10; i++)
                     {
                         link.think(TS1[i].data);
                         error += link.curError(TS1[i].result);
                         link.think(TS2[i].data);
                         error += link.curError(TS2[i].result);
                         link.think(TS3[i].data);
                         error += link.curError(TS3[i].result);
                     }
                     error /= 30;
                     Console.WriteLine("Gen#{0}. Error: {1:f7}", gen, error);
                 }
                 if (gen % 25000 == 0)
                 {
                     Console.WriteLine("Gen#{0}. Testing: ", gen);
                     for (int i = 0; i < 3; i++)
                     {
                         int a = rand.Next(0, 10);
                         link.think(TS1[a].data);
                         Console.WriteLine("true answer '1'. Neurolink thing: '1':{0:f3};\t'2':{1:f3};\t'3':{2:f3}", link.result()[0], link.result()[1], link.result()[2]);
                         a = rand.Next(0, 10);
                         link.think(TS2[a].data);
                         Console.WriteLine("true answer '2'. Neurolink thing: '1':{0:f3};\t'2':{1:f3};\t'3':{2:f3}", link.result()[0], link.result()[1], link.result()[2]);
                         a = rand.Next(0, 10);
                         link.think(TS3[a].data);
                         Console.WriteLine("true answer '3'. Neurolink thing: '1':{0:f3};\t'2':{1:f3};\t'3':{2:f3}", link.result()[0], link.result()[1], link.result()[2]);
                     }

                 }

                 if (gen % 10000 == 0) link.saveToFile();
             }
             link.saveToFile();

             Console.Beep();
             Console.ReadKey();*/


            string NUM = "4/";
            int cur = 0;

            {
                string[] fls = Directory.GetFiles(NUM);
                foreach (string s in fls)
                {
                    int tmp = int.Parse(s.Substring(2, s.Length - 5));
                    if (tmp > cur) cur = tmp;
                }
            }
            vector3d[] buff;

            srp = new SerialPort("com8", 57600);
            srp.DataReceived += dataRecived;

            Console.WriteLine("3");
            Thread.Sleep(500);
            Console.WriteLine("2");
            Thread.Sleep(500);
            Console.WriteLine("1");
            Thread.Sleep(300);

            Console.WriteLine("Openning port");
            try
            {
                srp.Open();
            }
            catch (Exception e) { Console.WriteLine(e.Message); Console.ReadKey(); return; }
            Console.WriteLine("Port opened");
            while (true)
            {
                if (READY_READ)
                {
                    StreamWriter fw2 = new StreamWriter(NUM + cur + ".txt", false, Encoding.Default);
                    lock (stk)
                        buff = stk.getCopy();
                    medianFilter3x(buff);
                    READY_READ = false;
                    for (int i = 2; i < buff.Length; i += 5)
                    {
                        fw2.WriteLine((buff[i - 1].x + buff[i].x + buff[i + 1].x) / 3);
                        fw2.WriteLine((buff[i - 1].y + buff[i].y + buff[i + 1].y) / 3);
                        fw2.WriteLine((buff[i - 1].z + buff[i].z + buff[i + 1].z) / 3);
                    }
                    fw2.WriteLine("0\n0\n0\n1\n0\n0\n0\n0\n0\n0");
                    fw2.Close();
                    sw.Stop();
                    Console.WriteLine("#:{0}\tesc:{1}", cur, sw.ElapsedMilliseconds);
                    cur++;
                    System.GC.Collect();
                }
                Thread.Sleep(5);
                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape) break;
            }
            lock (srp)
                srp.Close();
            /*
                        srp = new SerialPort("com8", 57600);
                        int C = 0;
                        srp.DataReceived += dataRecived;
                        try
                        {
                            srp.Open();
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); Console.ReadKey(); return; }
                        Console.WriteLine("Connected");
                        vector3d[] buff;
                        double[] data = new double[48];
                        StringBuilder str = new StringBuilder();
                        int PP = 3;
                        while (true)
                        {
                            if (READY_READ)
                            {
                                lock (stk)
                                    buff = stk.getCopy();
                                READY_READ = false;

                                medianFilter3x(buff);
                                for (int i = 2, j = 0; i < buff.Length; i += 5, j += 3)
                                {
                                    data[j] = (buff[i - 1].x + buff[i].x + buff[i + 1].x) / 9;
                                    data[j + 1] = (buff[i - 1].y + buff[i].y + buff[i + 1].y) / 9;
                                    data[j + 2] = (buff[i - 1].z + buff[i].z + buff[i + 1].z) / 9;
                                }
                                link.think(data);
                                double[] res = link.result();
                                char ch = '_';
                                if (res[0] >= 0.65 && res[0] > res[1] && res[0] > res[2]) ch = '1';
                                else if (res[1] >= 0.65 && res[1] > res[0] && res[1] > res[2]) ch = '2';
                                else if (res[2] >= 0.65 && res[2] > res[1] && res[2] > res[0]) ch = '3';

                                str.Append(ch);
                                Console.SetCursorPosition(0, 1);
                                Console.WriteLine(str);
                                Console.SetCursorPosition(0, PP);
                                Console.WriteLine("#{3}:\t'1':{0:f3};\t'2':{1:f3};\t'3':{2:f3}   ", res[0], res[1], res[2], PP-2);
                                PP++;

                            }
                            Thread.Sleep(20);
                            if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape) break;
                        }
                        lock (srp)
                            srp.Close();*/
        }
    }
}

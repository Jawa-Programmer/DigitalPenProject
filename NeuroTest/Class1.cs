using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroTest
{

    class NeuroLink
    {
        public static double hyperTanGrad(double x)
        {
            return 1 - x * x;
        }
        /*   public static double SigmoidGrad(double x)
           {
               return ((1 - x) * x);
           }
           public static double Sigmoid(double x)
           {
               return 1 / (1 + Math.Exp(-x));
           }*/
        private class Neuron
        {
            public Neuron(int prk)
            {
                kf = new double[prk];
                dkf = new double[prk];
            }
            //коэфециенты входящих синапсисов
            public double[] kf, dkf;
            public double output, delta;
            public void update(Neuron[] neus)
            {
                output = 0;
                for (int i = 0; i < neus.Length; i++) output += neus[i].output * kf[i];
                output = Math.Tanh(output);
            }
            public void findDelta(int pos, Neuron[] neus)
            {
                double sum = 0;
                for (int i = 0; i < neus.Length; i++) sum += neus[i].kf[pos] * neus[i].delta;
                delta = sum * hyperTanGrad(output);
            }
            public void findDelta(double cor)
            {
                delta = (cor - output) * hyperTanGrad(output);
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
            }
        }
        const int H1 = 40, H2 = 40, O = 3;
        //E - обучаемость, A - инертность
        const double E = 0.002, A = 0.00;
        //массив массивов нейронов. Нейроны распределны по массивам в соответсвии со слоем
        Neuron[][] layouts = new Neuron[4][];

        public NeuroLink()
        {
            layouts[0] = new Neuron[30];
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
                    layouts[1][i] = new Neuron(30);
                    for (int j = 0; j < layouts[1][i].kf.Length; j++)
                    {
                        layouts[1][i].kf[j] = double.Parse(kf[kk]);
                        kk++;
                    }
                }
                for (int i = 0; i < layouts[2].Length; i++)
                {
                    layouts[2][i] = new Neuron(H1);
                    for (int j = 0; j < layouts[2][i].kf.Length; j++)
                    {
                        layouts[2][i].kf[j] = double.Parse(kf[kk]);
                        kk++;
                    }
                }
                for (int i = 0; i < layouts[3].Length; i++)
                {
                    layouts[3][i] = new Neuron(H2);
                    for (int j = 0; j < layouts[3][i].kf.Length; j++)
                    {
                        layouts[3][i].kf[j] = double.Parse(kf[kk]);
                        kk++;
                    }
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
                    layouts[1][i] = new Neuron(30);
                    for (int j = 0; j < layouts[1][i].kf.Length; j++)
                    {
                        double ko = r.NextDouble();
                        layouts[1][i].kf[j] = ko;
                        sw.Write("{0} ", ko);
                    }
                }
                for (int i = 0; i < layouts[2].Length; i++)
                {
                    layouts[2][i] = new Neuron(H1);
                    for (int j = 0; j < layouts[2][i].kf.Length; j++)
                    {
                        double ko = r.NextDouble();
                        layouts[2][i].kf[j] = ko;
                        sw.Write("{0} ", ko);
                    }
                }
                for (int i = 0; i < layouts[3].Length; i++)
                {
                    layouts[3][i] = new Neuron(H2);
                    for (int j = 0; j < layouts[3][i].kf.Length; j++)
                    {
                        double ko = r.NextDouble();
                        layouts[3][i].kf[j] = ko;
                        sw.Write("{0} ", ko);
                    }
                }
                sw.Close();
            }

        }

        public char think(double[] data)
        {
            for (int i = 0; i < layouts[0].Length; i++)
                layouts[0][i].output = Math.Tanh(data[i]);
            for (int j = 1; j < layouts.Length; j++)
                for (int i = 0; i < layouts[j].Length; i++)
                    layouts[j][i].update(layouts[j - 1]);
            char uns = '?';
            Neuron[] neus = layouts[layouts.Length - 1];
            if (Math.Round(neus[0].output) == 0)
            {
                if (Math.Round(neus[1].output) == 1) uns = '1';
                else if (Math.Round(neus[2].output) == 1) uns = '2';
            }
            return uns;
        }
        public void learn(char cr)
        {
            double[] correct = new double[O];
            switch (cr)
            {
                case '?':
                    correct[0] = 1;
                    break;
                case '1':
                    correct[1] = 1;
                    break;
                case '2':
                    correct[2] = 1;
                    break;
            }

            for (int i = 0; i < layouts[layouts.Length - 1].Length; i++)
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
        public double curError(char cr)
        {
            double[] correct = new double[O];
            switch (cr)
            {
                case '?':
                    correct[0] = 1;
                    break;
                case '1':
                    correct[1] = 1;
                    break;
                case '2':
                    correct[2] = 1;
                    break;
            }
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
            }
            for (int i = 0; i < layouts[2].Length; i++)
            {
                for (int j = 0; j < layouts[2][i].kf.Length; j++)
                {
                    sw.Write("{0} ", layouts[2][i].kf[j]);
                }
            }
            for (int i = 0; i < layouts[3].Length; i++)
            {
                for (int j = 0; j < layouts[3][i].kf.Length; j++)
                {
                    sw.Write("{0} ", layouts[3][i].kf[j]);
                }
            }
            sw.Close();
        }
    }

    class MainClass
    {
        //   static double[,] dataset = { { 6, 1, 1 }, { 6, 10, 0 }, { 1, 1, 0 }, { 10, 10, 0 }, { 10, 4, 1 }, { 8, 6, 1 }, { 9, 8, 1 }, { 0, 5, 0 }, { 8, 2, 1 }, { 9, 3, 1 }, { 1, 4, 0 }, { 2, 6, 0 }, { 0, 2, 0 }, { 4, 4, 0 }, { 6, 5, 1 }, { 7, 10, 0 }, { 3, 3, 0 }, { 10, 8, 1 }, { 10, 2, 1 }, { 1, 8, 0 }, { 7, 4, 1 }, { 6, 2, 1 }, { 3, 2, 1 }, { 8, 0, 1 }, { 4, 9, 0 }, { 9, 1, 1 }, { 5, 2, 1 }, { 2, 2, 0 }, { 9, 0, 1 }, { 10, 10, 0 }, { 8, 6, 1 }, { 3, 4, 0 }, { 8, 8, 0 }, { 2, 5, 0 }, { 9, 2, 1 }, { 5, 6, 0 }, { 0, 0, 0 }, { 6, 7, 0 }, { 8, 10, 0 }, { 2, 9, 0 }, { 4, 4, 0 }, { 10, 7, 1 }, { 6, 6, 0 }, { 5, 0, 1 }, { 1, 8, 0 }, { 10, 10, 0 }, { 3, 2, 1 }, { 0, 1, 0 }, { 0, 1, 0 }, { 5, 6, 0 }, { 10, 3, 1 }, { 1, 2, 0 }, { 4, 3, 1 }, { 0, 4, 0 }, { 6, 5, 1 }, { 4, 9, 0 }, { 9, 1, 1 }, { 5, 4, 1 }, { 6, 7, 0 }, { 9, 2, 1 }, { 3, 1, 1 }, { 3, 1, 1 }, { 3, 6, 0 }, { 2, 9, 0 }, { 0, 5, 0 }, { 7, 7, 0 }, { 5, 7, 0 }, { 10, 1, 1 }, { 2, 8, 0 }, { 3, 3, 0 }, { 6, 1, 1 }, { 5, 7, 0 }, { 3, 0, 1 }, { 1, 2, 0 }, { 7, 7, 0 }, { 5, 4, 1 }, { 3, 3, 0 }, { 3, 8, 0 }, { 10, 0, 1 }, { 9, 8, 1 }, { 0, 1, 0 }, { 4, 1, 1 }, { 9, 8, 1 }, { 4, 1, 1 }, { 1, 3, 0 }, { 1, 9, 0 }, { 3, 5, 0 }, { 7, 1, 1 }, { 7, 7, 0 }, { 8, 3, 1 }, { 5, 0, 1 }, { 2, 3, 0 }, { 10, 2, 1 }, { 3, 1, 1 }, { 4, 1, 1 }, { 2, 2, 0 }, { 9, 5, 1 }, { 2, 4, 0 }, { 8, 3, 1 }, { 8, 6, 1 } };

        static int[,] dataset = { { } };
        static char[,] datasetA = {  };
        public static void Main(string[] args)
        {
            //  Random rand = new Random();
            NeuroLink link = new NeuroLink();
            /* for (int gen = 0; gen < 100000; gen++)
             {
                 bool log = gen % 1000 == 0;
                 if (log)
                 {
                     Console.SetCursorPosition(0, 0);
                     Console.WriteLine("Gen#{0}", gen);
                 }
                 double error = 0;
                 for (int i = 0; i < 100; i++)
                 {
                     double[] data = { rand.Next(0, 10), rand.Next(0, 10) };
                     double uns = link.think(data);
                     // if (log) Console.WriteLine("{0} > {1} : {2}", data[0], data[1], Math.Round(uns));
                     link.learn(new double[] { ((data[0] > data[1]) ? 1 : 0) });
                     error += (((data[0] > data[1]) ? 1 : 0) - uns) * (((data[0] > data[1]) ? 1 : 0) - uns);
                 }
                 error /= 100;
                 if (log) Console.WriteLine("error: {0:f7}", error);
             }
             link.saveToFile();
             {
                 double error = 0;
                 for (int i = 0; i < 100; i++)
                 {
                     double[] data = { i / 10, i % 10 };
                     double uns = link.think(data);
                     if ((((int)Math.Round(uns) == 1) && (data[0] > data[1])) || (((int)Math.Round(uns) == 0) && (data[0] <= data[1]))) Console.ForegroundColor = ConsoleColor.Green;
                     else Console.ForegroundColor = ConsoleColor.Red;
                     Console.WriteLine("{0} > {1} : {2}", data[0], data[1], Math.Round(uns));
                     Console.ForegroundColor = ConsoleColor.White;
                     error += (((data[0] > data[1]) ? 1 : 0) - uns) * (((data[0] > data[1]) ? 1 : 0) - uns);
                 }
                 error /= 1000;
                 Console.WriteLine("error: {0:f7}", error);
             }*/
            link.saveToFile();

            Console.ReadKey();
        }
    }
}

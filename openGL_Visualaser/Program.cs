using OpenTK;
using System;
using System.IO.Ports;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using System.Drawing.Imaging;

namespace openGL_Visualaser
{

    class KalmanFilterSimple1D
    {
        public double X0 { get; private set; } // predicted state
        public double P0 { get; private set; } // predicted covariance

        public double F { get; private set; } // factor of real value to previous real value
        public double Q { get; private set; } // measurement noise
        public double H { get; private set; } // factor of measured value to real value
        public double R { get; private set; } // environment noise

        public double State { get; private set; }
        public double Covariance { get; private set; }

        public KalmanFilterSimple1D(double q, double r, double f = 1, double h = 1)
        {
            Q = q;
            R = r;
            F = f;
            H = h;
        }

        public void SetState(double state, double covariance)
        {
            State = state;
            Covariance = covariance;
        }

        public void Correct(double data)
        {
            //time update - prediction
            X0 = F * State;
            P0 = F * Covariance * F + Q;

            //measurement update - correction
            var K = H * P0 / (H * P0 * H + R);
            State = X0 + K * (data - H * X0);
            Covariance = (1 - K * H) * P0;
        }
    }


    class Program
    {

        static KalmanFilterSimple1D kax = new KalmanFilterSimple1D(f: 1, h: 1, q: 2, r: 15), kay = new KalmanFilterSimple1D(f: 1, h: 1, q: 2, r: 15), kaz = new KalmanFilterSimple1D(f: 1, h: 1, q: 2, r: 15);


        static float[,] calA = {
            { 0.477735f,-0.005839f, -0.003274f },
            { -0.005839f, 0.501778f, 0.031176f },
            { -0.003274f, 0.031176f, 0.499463f } };
        static void data(object sender, SerialDataReceivedEventArgs e)
        {
            while (srp.ReadByte() != 0xFF) if (srp.BytesToRead <= 0) return;
            while (srp.ReadByte() != 0x00) if (srp.BytesToRead <= 0) return;
            while (srp.BytesToRead < 18) ;

            byte[] arr = new byte[18];
            srp.Read(arr, 0, 18);
            short[] data = new short[9];
            for (int i = 0; i < 9; i++)
            {
                data[i] = (short)((arr[i * 2] << 8) & 0xff00 | (arr[i * 2 + 1]));
            }


            /// 16200f

            /*  gax = data[0];
              gay = data[1];
              gaz = data[2];
              ggx = data[3] ;
              ggy = data[4] ;
              ggz = data[5] ;*/
            kax.Correct(data[0]);
            kay.Correct(data[1]);
            kaz.Correct(data[2]);
            gax = (float)kax.State;
            gay = (float)kay.State;
            gaz = (float)kaz.State;
            /*  gax = Filter(gax, data[0], 0.8f);
              gay = Filter(gay, -data[1], 0.8f);
              gaz = Filter(gaz, -data[2], 0.8f);*/
            /* ggx = Filter(ggx, data[3], 0.06f);
             ggy = Filter(ggy, data[4], 0.06f);
             ggz = Filter(ggz, data[5], 0.06f);*/

            gm[0] = Filter(gm[0], data[6] - 48.843195f, 0.8f);
            gm[1] = Filter(gm[1], data[7] + 183.689342f, 0.8f);
            gm[2] = Filter(gm[2], data[8] - 109.846870f, 0.8f);
            mult();
        }

        static float Filter(float prew, float raw, float k)
        {
            return (1 - k) * prew + k * raw;
        }

        static void mult()
        {
            float[] tmg = { gm[0] * calA[0, 0] + gm[1] * calA[0, 1] + gm[2] * calA[0, 2], gm[0] * calA[1, 0] + gm[1] * calA[1, 1] + gm[2] * calA[1, 2], gm[0] * calA[2, 0] + gm[1] * calA[2, 1] + gm[2] * calA[2, 2] };
            gm[0] = tmg[0];
            gm[1] = tmg[1];
            gm[2] = tmg[2];
        }

        static SerialPort srp;
        static float gax, gay, gaz;//ggx, ggy, ggz;
        static float[] gm = { 0, 0, 0 };


        const float arm_len = 0.28571428571f;
        const float toRadSec = 0.01745329252f / 131f;

        [STAThread]
        static void Main(string[] args)
        {
            /*  kax.SetState(0,0.1);
              kay.SetState(0,0.1);
              kaz.SetState(0,0.1);*/
            while (true)
            {
                Console.WriteLine("Выберете порт из списка: ");
                string[] ports = SerialPort.GetPortNames();
                if (ports.Length == 1) try
                    {
                        srp = new SerialPort(ports[0], 57600);
                        srp.Open();
                        srp.DataReceived += data;
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Ошибка подключения: " + e.Message);
                    }

                foreach (string s in ports) Console.WriteLine(s);
                Console.WriteLine("Напишите refresh для обновления списка");
                string port = Console.ReadLine();
                if (port != "refresh")
                {
                    try
                    {
                        srp = new SerialPort(port, 57600);
                        srp.Open();
                        srp.DataReceived += data;
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Ошибка подключения: " + e.Message);
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine("Успешно подключен");
            const float PI_05 = (float)Math.PI / 2;
            float t = PI_05;
            Vector3 me = new Vector3(0, 0.7f, 0);
            Vector3 up = Vector3.UnitY;
            Vector3 at = new Vector3(0, 0, 5f);

            const float dt = 1f / 60f;

            float a_gx = 0, a_ax = 0, a_gz = 0;

            int prevx = 100, prevy = 100;
            Bitmap img = new Bitmap(100, 100);
            for (int x = 0; x < 100; x++)
                for (int y = 0; y < 100; y++)
                    img.SetPixel(x, y, Color.White);


            using (var game = new GameWindow())
            {
                game.Load += (sender, e) =>
                {
                    // setup settings, load textures, sounds
                    game.VSync = VSyncMode.On;
                };

                game.Resize += (sender, e) =>
                {
                    GL.Viewport(0, 0, game.Width, game.Height);
                };

                game.RenderFrame += (sender, e) =>
                {
                    Console.SetCursorPosition(0, 0);


                    float ax = gax / 16200f, ay = gay / 16200f, az = gaz / 16200f;//, gx = ggx * toRadSec, gy = ggy * toRadSec, gz = ggz * toRadSec;

                    float N = (float)Math.Sqrt(ax * ax + ay * ay + az * az);


                    if (ax > 1f) ax = 1f;
                    else if (ax < -1f) ax = -1f;
                    if (ay > 1f) ay = 1f;
                    else if (ay < -1f) ay = -1f;
                    if (az > 1f) az = 1f;
                    else if (az < -1f) az = -1f;

                    Console.WriteLine("{0:f3}\t{1:f3}\t{2:f3}/           ", ax, ay, az);



                    float alpha = (float)(Math.Acos(ay));

                    float azim = (float)Math.Atan2(-gm[1], gm[2]);
                    Console.WriteLine("{0:f3}\t{1:f3}\t{2:f3}                  \n{3:f3}         ", gm[0], gm[1], gm[2], azim * 180 / Math.PI);

                    float azim2 = 0, ang1 = 0;

                    {
                        double alf = -Math.Acos(ax);
                        double bet = -Math.Acos(az);
                        double gam = -Math.Atan2(ax, az);

                        //rot gamma
                        double mx1 = gm[0];
                           double my1 = gm[1] * Math.Cos(gam) - gm[2] * Math.Sin(gam);
                           double mz1 = gm[1] * Math.Sin(gam) + gm[2] * Math.Cos(gam);
                        /*float mx1 = gm[0];
                        float my1 = gm[1];
                        float mz1 = gm[2];*/
                        //rot beta
                        double mx2 = mx1 * Math.Cos(bet) + mz1 * Math.Sin(bet);
                        double my2 = my1;
                        double mz2 = -mx1 * Math.Sin(bet) + mz1 * Math.Cos(bet);
                        //rot alpha
                        double my3 = mx2 * Math.Sin(alf) + my2 * Math.Cos(alf);
                        double mz3 = mz2;

                        azim2 = (float)Math.Atan2(my3, mz3);
                        ang1 = (float)gam;
                    }

                    Console.WriteLine("{0:f3}                  \n{1:f3}         ", ang1 * 180 / Math.PI, azim2 * 180 / Math.PI);

                    float l1 = arm_len / ay * (float)Math.Sin(alpha);

                    float l2 = l1 - (float)Math.Sin(alpha);



                    int px = (int)(50 + l1 * 50);
                    int pz = (int)(50 + l1 * 50);


                    if (px > 98) px = 98;
                    else if (px < 1) px = 1;
                    if (pz > 98) pz = 98;
                    else if (pz < 1) pz = 1;

                    img.SetPixel(px, pz, Color.Black);

                    //Console.WriteLine("{0:f3}\t{1:f3}\t{2:f3}                ", l1, l2, ay);




                    SetPerspectiveProjection(game.Width, game.Height, 70);

                    GL.ClearColor(0.3f, 0.3f, 0.3f, 1f);
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    me.X = 5 * (float)Math.Cos(t);
                    me.Z = 5 + 5 * (float)Math.Sin(t);
                    //t += 0.005f;

                    SetLookAtCamera(me, at, up);

                    GL.Begin(PrimitiveType.Quads);
                    GL.Color4(1f, 1f, 1f, 1f);
                    GL.Vertex3(-1f, 0f, 4f);
                    GL.Vertex3(1f, 0f, 4f);
                    GL.Vertex3(1f, 0f, 6f);
                    GL.Vertex3(-1f, 0f, 6f);
                    GL.End();
                    GL.Disable(EnableCap.Texture2D);

                    GL.LineWidth(5);
                    GL.Begin(PrimitiveType.Lines);

                    GL.Color3(0f, 1f, 0f);
                    GL.Vertex3(0, 0, 5f);
                    GL.Vertex3(0, arm_len * 2, 5f);


                    GL.Color3(1f, 0f, 0f);
                    GL.Vertex3(l1 * 2 * Math.Cos(azim2), 0, 5f + l1 * 2 * Math.Sin(azim2));
                    GL.Color3(0f, 0f, 1f);
                    GL.Vertex3(l2 * 2 * Math.Cos(azim2), ay * 2, 5f + l2 * 2 * Math.Sin(azim2));

                    GL.Color3(1f, 0f, 1f);
                    GL.Vertex3(l2 * 2, ay * 2, 5f);

                    GL.Vertex3(l2 * 2, ay * 2 + Math.Cos(ang1), 5f + Math.Sin(ang1));

                    GL.End();


                    prevx = px;
                    prevy = pz;
                    game.SwapBuffers();
                };
                // Run the game at 60 updates per second
                Console.Clear();
                game.Run(60.0);
            }

            img.Save("test.png");
        }

        private static void SetLookAtCamera(Vector3 position, Vector3 target, Vector3 up)
        {
            Matrix4 modelViewMatrix = Matrix4.LookAt(position, target, up);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelViewMatrix);
        }

        static int LoadTexture(Bitmap bitmap)
        {
            int tex;
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            GL.GenTextures(1, out tex);
            GL.BindTexture(TextureTarget.Texture2D, tex);

            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bitmap.UnlockBits(data);


            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            return tex;
        }
        private static void SetPerspectiveProjection(int width, int height, float FOV)
        {
            Matrix4 projectionMatrix = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI * (FOV / 180f), width / (float)height, 0.01f, 20f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projectionMatrix); // this replaces the old matrix, no need for GL.LoadIdentity()
            GL.ShadeModel(ShadingModel.Smooth);
            GL.Enable(EnableCap.DepthTest);            // Разрешить тест глубины
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

        }

    }
}

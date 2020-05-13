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




    class Program
    {



        static void data(object sender, SerialDataReceivedEventArgs e)
        {
            while (srp.ReadByte() != 0xFF) if (srp.BytesToRead <= 0) return;
            while (srp.ReadByte() != 0x00) if (srp.BytesToRead <= 0) return;
            string[] data = srp.ReadLine().Replace('.', ',').Split('\t');

            alpha = float.Parse(data[0])/1000f;
            beta = float.Parse(data[1])/1000f;
            gamma = float.Parse(data[2])/1000f;

        }

        static float alpha, beta, gamma;

        static SerialPort srp;



        const float arm_len = 6f / 15f;
        const float toDeg = 180f / (float)Math.PI;

        [STAThread]
        static void Main(string[] args)
        {
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
            float t = 0;
            Vector3 me = new Vector3(0, 5f, 0);
            Vector3 up = Vector3.UnitY;
            Vector3 at = new Vector3(0, 0, 10f);

            const float dt = 1f / 60f;


            int prevx = 200, prevy = 200;
            Bitmap img = new Bitmap(400, 400);
            for (int x = 0; x < 400; x++)
                for (int y = 0; y < 400; y++)
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


                    //lock (key)
                    {
                        float gam = gamma - beta;
                        while (gam > Math.PI) gam -= (float)Math.PI * 2;
                        while (gam < -Math.PI) gam += (float)Math.PI * 2;

                        Console.WriteLine("{0:f4}\t{1:f4}\t{2:f4}\t{3:f2}", alpha * toDeg, beta * toDeg, gamma * toDeg, gam * toDeg);

                        //  Console.WriteLine("{0:f3}                  \n{1:f3}         ", ang1 * 180 / Math.PI, azim2 * 180 / Math.PI);

                        float l1 = arm_len * (float)Math.Tan(alpha);

                        float l2 = l1 - (float)Math.Sin(alpha);



                        int px = (int)(200 + l1 * 200 * Math.Cos(gam));
                        int pz = (int)(200 + l1 * 200 * Math.Sin(gam));


                        if (px > 398) px = 200;
                        else if (px < 1) px = 200;
                        if (pz > 398) pz = 200;
                        else if (pz < 1) pz = 200;

                        img.SetPixel(px, pz, Color.Black);

                        //Console.WriteLine("{0:f3}\t{1:f3}\t{2:f3}                ", l1, l2, ay);




                        SetPerspectiveProjection(game.Width, game.Height, 70);

                        GL.ClearColor(0.3f, 0.3f, 0.3f, 1f);
                        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                        /*   me.X = 10 * (float)Math.Cos(t);
                           me.Z = 10 + 10 * (float)Math.Sin(t);*/
                        //t += 0.005f;

                        SetLookAtCamera(me, at, up);

                        GL.Begin(PrimitiveType.Quads);
                        GL.Color4(1f, 1f, 1f, 1f);
                        GL.Vertex3(-5f, 0f, 5f);
                        GL.Vertex3(5f, 0f, 5f);
                        GL.Vertex3(5f, 0f, 15f);
                        GL.Vertex3(-5f, 0f, 15f);
                        GL.End();
                        GL.Disable(EnableCap.Texture2D);

                        GL.LineWidth(5);
                        GL.Begin(PrimitiveType.Lines);

                        GL.Color3(0f, 1f, 0f);
                        GL.Vertex3(0, 0, 10f);
                        GL.Vertex3(0, arm_len * 2, 10f);


                        GL.Color3(1f, 0f, 0f);
                        GL.Vertex3(l1 * 2 * Math.Cos(gam), 0, 10f + l1 * 2 * Math.Sin(gam));
                        GL.Color3(0f, 0f, 1f);
                        GL.Vertex3(l2 * 2 * Math.Cos(gam), Math.Cos(alpha) * 2, 10f + l2 * 2 * Math.Sin(gam));
                        /*
                        GL.Color3(1f, 0f, 1f);
                        GL.Vertex3(l2 * 2, ay * 2, 5f);

                        GL.Vertex3(l2 * 2, ay * 2 + Math.Cos(ang1), 5f - Math.Sin(ang1));*/

                        GL.End();


                        prevx = px;
                        prevy = pz;
                    }
                    game.SwapBuffers();
                };
                // Run the game at 60 updates per second
                Console.Clear();
                game.Run(30.0);
            }

            img.Save(@"C:\Users\Спок\Desktop\pen\test.png");
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Asynchronous_Server_Socket
{
    public class StateObject
    {
        // Tamaño del buffer receptor
        public const int BufferSize = 1024;

        // Buffer receptor
        public byte[] buffer = new byte[BufferSize];

        // Data recibida
        public StringBuilder sb = new StringBuilder();

        // Socket Cliente
        public Socket workSocket = null;
    }


    class Program
    {
        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);


        public static void StartListening()
        {
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);


            try
            {
                listener.Bind(new IPEndPoint(IPAddress.Any, 5555));
                listener.Listen(100);

                while (true)
                {
                    allDone.Reset();

                    Console.WriteLine("Esperando una conexion...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Espera una conexion
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPresione enter para continuar...");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Le comunica al main que continue
            allDone.Set();


            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);


            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;


            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;


            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {

                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));


                content = state.sb.ToString();


                if (content.StartsWith("("))
                {
                    Console.WriteLine("Interpretando alarma: " + content);
                    //InterpretarMessageAlarm(message.Trim());
                } else if (content.StartsWith("$"))
                {
                    string hexadecimal = BitConverter.ToString(state.buffer, 0, bytesRead).Replace("-", string.Empty).Substring(0, 74);
                    Console.WriteLine("Interpretando estado: " + hexadecimal);
                    //InterpretMessageStatus(hexadecimal);
                } else
                {
                    Console.WriteLine("No se pudo interpretar el mensaje: " + content);
                }


                // Hay que buscar el fin del mensaje? en ese caso hay que llamar a la misma funcion
                //if (content.IndexOf("<EOF>") == -1)
                //{
                //    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                //    new AsyncCallback(ReadCallback), state);
                //}

            }
        }


        // Para el caso en que necesitemos mandarle algo al GPS dentro de ReadCallBack se llama a Send(handler, content);
        private static void Send(Socket handler, String data)
        {

            byte[] byteData = Encoding.ASCII.GetBytes(data);


            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {

                Socket handler = (Socket)ar.AsyncState;

                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Se envio {0} bytes al cliente", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void Main(string[] args)
        {
            try
            {
                StartListening();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocurrio un error: " + ex);
                throw;
            }
            

        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace ClientServer
{
    class Program
    {
        private static string _address = "tcp://127.0.0.1:";
        private const int RoundtripCount = 10;

        public static void Main(string[] args)
        {
            _address += new Random().Next(1000, 10000);

            var serverThread = new Thread(StartServer);
            serverThread.Start();

            var clientThread = new Thread(StartClient);
            clientThread.Start();


            clientThread.Join();
            serverThread.Join();

            Console.WriteLine("Done with example for ClientServer");
        }

        private static void StartServer()
        {

            //  Initialise 0MQ infrastructure
            using (var ctx = new ZContext())
            {
                using (var publisher = new ZSocket(ctx, ZSocketType.PUB))
                {
                    publisher.Linger = TimeSpan.Zero;
                    publisher.Immediate = true;
                    publisher.Bind(_address);

                    Console.WriteLine("Server has bound");

                    //  Bounce the messages.
                    for (var i = 0; i < RoundtripCount; i++)
                    {
                        using (var message = new ZMessage())
                        {
                            message.Add(new ZFrame("A"));
                            message.Add(new ZFrame("We don't want to see this"));
                            publisher.Send(message);
                        }
                        using (var message = new ZMessage())
                        {
                            message.Add(new ZFrame("B"));
                            message.Add(new ZFrame("We would like to see this"));
                            publisher.Send(message);
                        }
                        Thread.Sleep(1000);
                    }
                }
            }

            Console.WriteLine("Done with server");
        }

        private static void StartClient()
        {
            //  Initialise 0MQ infrastructure
            using (var ctx = new ZContext())
            {
                using (var subscriber = new ZSocket(ctx, ZSocketType.SUB))
                {
                    subscriber.Connect(_address);
                    subscriber.Subscribe("B");
                    int i = 1;
                    Console.WriteLine("Client has bound");
                    while (i < RoundtripCount)
                    {
                        using (ZMessage message = subscriber.ReceiveMessage())
                        {
                            // Read envelope with address
                            string address = message[0].ReadString();

                            // Read message contents
                            string contents = message[1].ReadString();

                            Console.WriteLine("[{0}] {1}", address, contents);
                            i++;
                        }
                        Thread.Sleep(1000);
                    }
                }
            }

            Console.WriteLine("Done with client");
        }

    }

}

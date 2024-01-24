using System;
using System.Net;
using System.Threading;
using WorldsAdriftServer.Server;

namespace WorldsAdriftServer
{
    internal class WorldsAdriftServer
    {
        private static RequestRouterServer restServer;
        private static Thread serverThread;
        public static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        static void Main(string[] args)
        {
            int restPort = 8080;

            restServer = new RequestRouterServer(IPAddress.Any, restPort);
            serverThread = new Thread(new ThreadStart(StartServer));

            serverThread.Start();

            Console.WriteLine("Congratulations on setting up Worlds Adrift Reborn.");
            Console.WriteLine("Type 'stop' and press Enter to stop the server.");

            string command;
            do
            {
                command = Console.ReadLine();
            } while (command != "stop");

            StopServer();

            Console.WriteLine("Server stopped. Use [dotnet WorldsAdriftServer.dll] to restart.");
        }

        private static void StartServer()
        {
            restServer.Start();
        }

        private static void StopServer()
        {
            restServer.Stop();
            serverThread.Join(); // Wait for the server thread to finish
        }
    }
}

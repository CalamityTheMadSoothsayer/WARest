using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WorldsAdriftServer.Handlers.DataHandler
{
    internal static class CommandHandler
    {
        internal static void processCommand(HttpSession session, HttpRequest request, HttpServer server, string publicKey, string privateKey)
        {
            string providedEncryptionKey = "";

            for (int i = 0; i < (int)request.Headers; i++)
            {
                if (request.Header(i).ToString().Contains("EncryptionKey"))
                {
                    string[] headerParts = request.Header(i).ToString().Split(':');
                    if (headerParts.Length > 1)
                    {
                        providedEncryptionKey = headerParts[1];
                    }
                    else
                    {
                        providedEncryptionKey = headerParts[0];
                    }
                    break; // Exit the loop after finding the EncryptionKey header
                }
            }


            // Verify the encryption key
            if (!VerifyEncryptionKey(providedEncryptionKey, publicKey))
            {
                Console.WriteLine("KEY FROM HEADER: " + providedEncryptionKey);
                Console.WriteLine("KEY FROM CONFIG: " + publicKey);
                Console.WriteLine("Invalid encryption key. Command not processed.");
                return;
            }

            // Decrypt the command
            string decryptedCommand = Decrypt(encryptedCommand, privateKey);

            Console.WriteLine($"Received command: {decryptedCommand}");

            bool commandSuccesful = false;

            switch (decryptedCommand.ToLower())
            {
                case "connect":
                    Console.Write("Handling Connection request...");
                    commandSuccesful = Connect();
                    if (commandSuccesful)
                    {
                        new HttpResponse(200, "Connection Successful", "HTTP/1.1");
                    }
                    else
                    {
                        new HttpResponse(500, "Connection Failed", "HTTP/1.1");
                    }
                    break;
                case "restart":
                    Console.WriteLine("Restarting server...");
                    commandSuccesful = ReStartServer(server);
                    if (commandSuccesful)
                    {
                        new HttpResponse(200, "Restart Successful", "HTTP/1.1");
                    }
                    else
                    {
                        new HttpResponse(500, "Restart Failed", "HTTP/1.1");
                    }
                    break;
                case "shutdown":
                    Console.WriteLine("Shutting down server...");
                    commandSuccesful = StopServer(server);
                    if (commandSuccesful)
                    {
                        new HttpResponse(200, "Shutdown Succesful", "HTTP/1.1");
                    }
                    else
                    {
                        new HttpResponse(500, "Shutdown Failed", "HTTP/1.1");
                    }
                    break;
                case "start":
                    Console.WriteLine("Starting server...");
                    commandSuccesful = StartServer(server);
                    if (commandSuccesful)
                    {
                        new HttpResponse(200, "Start Successful", "HTTP/1.1");
                    }
                    else
                    {
                        new HttpResponse(500, "Start Failed", "HTTP/1.1");
                    }
                    break;
            }
        }

        private static bool VerifyEncryptionKey(string providedKey, string expectedKey)
        {
            return string.Equals(providedKey, expectedKey, StringComparison.OrdinalIgnoreCase);
        }

        private static string Decrypt(string encryptedText, string privateKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, false);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }

        private static bool Connect()
        {
            return true;
        }

        private static bool ReStartServer(HttpServer server)
        {
            // Implement logic to restart the server
            Console.WriteLine("Restarting server...");
            try
            {
                server.Restart();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool StopServer(HttpServer server)
        {
            // Implement logic to stop the server
            Console.WriteLine("Stopping server...");
            try
            {
                server.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool StartServer(HttpServer server)
        {
            // Implement logic to start the server
            Console.WriteLine("Starting server...");
            try
            {
                server.Start();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

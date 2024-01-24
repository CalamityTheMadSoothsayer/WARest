using Azure;
using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WorldsAdriftServer.Handlers.DataHandler
{
    internal static class CommandHandler
    {
        internal static void processCommand(HttpSession session, HttpRequest request, HttpServer server, string publicKey, string privateKey)
        {
            string encryptedCommand = request.Body;
            string providedEncryptionKey = "";

            for (int i = 0; i < (int)request.Headers; i++)
            {
                if (request.Header(i).ToString().Contains("EncryptionKey"))
                {
                    string headerParts = request.Header(i).ToString();
                    Console.WriteLine("HEADER: " + headerParts);

                    // Extract the value between '<RSAKeyValue>' and '</RSAKeyValue>'
                    int startIndex = headerParts.IndexOf("<RSAKeyValue>");
                    int endIndex = headerParts.IndexOf("</RSAKeyValue>");
                    if (startIndex != -1 && endIndex != -1)
                    {
                        providedEncryptionKey = headerParts.Substring(startIndex, endIndex - startIndex + "</RSAKeyValue>".Length);
                    }
                    else
                    {
                        // Handle the case where the RSAKeyValue tags are not found
                        Console.WriteLine("Invalid EncryptionKey format in header. Command not processed.");
                        return;
                    }


                    break; // Exit the loop after finding the EncryptionKey header
                }
            }

            string formattedKeysForConfig = providedEncryptionKey.Replace("<RSAKeyValue>", "").Replace("</RSAKeyValue>", "").Replace("<Modulus>", "").Replace("</Modulus>", "").Replace("<Exponent>", "").Replace("</Exponent>", "");

            // Verify the encryption key
            if (!VerifyEncryptionKey(formattedKeysForConfig, publicKey))
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
                    Console.Write("Handling Connection request...\n\r");
                    commandSuccesful = Connect(); // returns true for now. may add more functionality later.
                    if (commandSuccesful)
                    {
                        Console.WriteLine("WAR tool connected. \n\r");
                        // Use ResponseBuilder to construct and send the response
                        Utilities.ResponseBuilder.BuildAndSendResponse(
                            session,
                            200
                        );
                    }
                    else
                    {
                        Utilities.ResponseBuilder.BuildAndSendResponse(
                            session,
                            500
                        );
                    }
                    break;
                case "restart":
                    Console.WriteLine("Restarting server...\n\r");
                    commandSuccesful = ReStartServer(server);
                    if (commandSuccesful)
                    {
                        Console.WriteLine("Server Restarted.\n\r");
                        Utilities.ResponseBuilder.BuildAndSendResponse(
                            session,
                            200
                        );
                    }
                    else
                    {
                        Utilities.ResponseBuilder.BuildAndSendResponse(session, 500);
                    }
                    break;
                case "shutdown":
                    Console.WriteLine("Shutting down server...\n\r");
                    commandSuccesful = ShutdownServer(server);
                    if (commandSuccesful)
                    {
                        Console.WriteLine("Server shutdown.\n\r");
                        Utilities.ResponseBuilder.BuildAndSendResponse(session, 200);
                    }
                    else
                    {
                        Utilities.ResponseBuilder.BuildAndSendResponse(session, 500);
                    }
                    break;
                case "stop":
                    Console.WriteLine("Stopping server...\n\r");
                    commandSuccesful = StopServer(server);
                    if (commandSuccesful)
                    {
                        Console.WriteLine("Server stopped.\n\r");
                        Utilities.ResponseBuilder.BuildAndSendResponse(session, 200);
                    }
                    else
                    {
                        Utilities.ResponseBuilder.BuildAndSendResponse(session, 500);
                    }
                    break;
                case "start":
                    Console.WriteLine("Starting server...\n\r");
                    commandSuccesful = StartServer(server);
                    if (commandSuccesful)
                    {
                        Console.WriteLine("Server started..\n\r");
                        Utilities.ResponseBuilder.BuildAndSendResponse(session, 200);
                    }
                    else
                    {
                        Utilities.ResponseBuilder.BuildAndSendResponse(session, 500);
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
            try
            {
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(privateKey);

                    // Trim any leading/trailing whitespaces in the encryptedText
                    encryptedText = encryptedText.Trim();

                    byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                    byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, false);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during decryption: {ex.Message} \n\r {privateKey}");
                return string.Empty;
            }
        }


        private static bool Connect()
        {
            return true;
        }

        private static bool ReStartServer(HttpServer server)
        {
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

        private static bool ShutdownServer(HttpServer server)
        {
            
            try
            {
                WorldsAdriftServer.cancellationTokenSource.Cancel();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool StopServer(HttpServer server)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
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

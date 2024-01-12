using System;
using System.Net;
using NetCoreServer;
using Npgsql;
using Newtonsoft.Json.Linq;
using WorldsAdriftServer.Handlers.DataHandler;

namespace WorldsAdriftServer.Handlers.CharacterScreen
{
    internal class CharacterNameHandler
    {
        // ...

        public static void SaveCharacterUid(HttpRequest request, string userKey, HttpSession session)
        {
            try
            {
                // Parse the request body to get the character UID and screen name
                JObject requestBody = JObject.Parse(request.Body);

                // Extract data from the request
                var characterUid = requestBody["characterUid"]?.ToString();
                var screenName = requestBody["screenName"]?.ToString(); // Assuming the screen name is available in the request

                using (NpgsqlConnection connection = new NpgsqlConnection(DataHandler.DataStorage.connectionString))
                {
                    connection.Open();

                    // Check if the character UID already exists for the player in the database
                    string checkCharacterSql = $"SELECT * FROM CharacterDetails WHERE userKey = '{userKey}' AND characteruid = '{characterUid}'";
                    using (NpgsqlCommand checkCharacterCommand = new NpgsqlCommand(checkCharacterSql, connection))
                    using (NpgsqlDataReader checkCharacterReader = checkCharacterCommand.ExecuteReader())
                    {
                        if (checkCharacterReader.HasRows)
                        {
                            // Character UID already exists, send appropriate response
                            Console.WriteLine("Character UID already exists.");

                            // Set variables here as needed
                            RequestRouterHandler.status = HttpStatusCode.OK;

                            return;
                        }
                    }

                    // Insert the character UID into the database along with the screen name and other necessary columns
                    string insertCharacterSql = $"INSERT INTO CharacterDetails (userKey, characteruid, name) VALUES ('{userKey}', '{characterUid}', '{screenName}')";
                    using (NpgsqlCommand insertCharacterCommand = new NpgsqlCommand(insertCharacterSql, connection))
                    {
                        insertCharacterCommand.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Success: Character UID saved successfully");

                // Construct a response to send back to the game
                JObject gameResponse = new JObject();
                gameResponse.Add("status", "success");
                gameResponse.Add("message", "Character UID saved successfully");

                HttpResponse gameHttpResponse = new HttpResponse();
                gameHttpResponse.SetBegin((int)HttpStatusCode.OK);
                gameHttpResponse.SetBody(gameResponse.ToString());

                // Send the response back to the game
                session.SendResponseAsync(gameHttpResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving character UID: {ex.Message}");

                // Construct an error response to send back to the game
                JObject errorResponse = new JObject();
                errorResponse.Add("status", "error");
                errorResponse.Add("message", $"Error saving character UID: {ex.Message}");

                HttpResponse errorHttpResponse = new HttpResponse();
                errorHttpResponse.SetBegin((int)HttpStatusCode.InternalServerError);
                errorHttpResponse.SetBody(errorResponse.ToString());

                // Send the error response back to the game
                session.SendResponseAsync(errorHttpResponse);
            }
        }

    }
}
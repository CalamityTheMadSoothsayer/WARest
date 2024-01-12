using NetCoreServer;
using Npgsql;
using Newtonsoft.Json.Linq;
using WorldsAdriftServer.Helper.CharacterSelection;

namespace WorldsAdriftServer.Handlers.CharacterScreen
{
    internal class CharacterDetailHandler
    {
        internal static void SaveDetails(HttpSession session, string userKey, HttpRequest request)
        {

            JObject requestBody = JObject.Parse(request.Body);
            try
            {
                // Extract data from the request
                var characterUid = requestBody["Character UID"]?.ToString();
                var name = requestBody["Name"]?.ToString();
                var server = requestBody["Server"]?.ToString();
                var serverIdentifier = requestBody["Server Identifier"]?.ToString();
                var gender = requestBody["Misc"]["Gender"]?.ToString();
                var seenIntro = requestBody["Misc"]["Seen Intro"]?.ToString();
                var skippedTutorial = requestBody["Misc"]["Skipped Tutorial"]?.ToString();

                // TODO: Extract other properties as needed

                using (NpgsqlConnection connection = new NpgsqlConnection(DataHandler.DataStorage.connectionString))
                {
                    connection.Open();

                    // Insert character details into the database
                    string insertCharacterSql = $@"
                        INSERT INTO CharacterDetails (userKey, characterUid, name, server, serverIdentifier, gender, seenIntro, skippedTutorial)
                        VALUES ('{userKey}', '{characterUid}', '{name}', '{server}', '{serverIdentifier}', '{gender}', '{seenIntro}', '{skippedTutorial}')
                    ";

                    using (NpgsqlCommand insertCharacterCommand = new NpgsqlCommand(insertCharacterSql, connection))
                    {
                        insertCharacterCommand.ExecuteNonQuery();
                    }
                }

                // Set response status to success (200)
                HttpResponse httpResponse = new HttpResponse();
                httpResponse.SetBegin(200);
                session.SendResponseAsync(httpResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving character details: {ex.Message}");

                // Set response status to internal server error (500)
                HttpResponse errorHttpResponse = new HttpResponse();
                errorHttpResponse.SetBegin(500);
                session.SendResponseAsync(errorHttpResponse);
            }
        }
    }
}

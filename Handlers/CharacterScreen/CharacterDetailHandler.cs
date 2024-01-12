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
            Console.WriteLine(requestBody.ToString());
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

                // Extract Head properties
                var headId = requestBody["Head"]["Id"]?.ToString();
                var headPrefab = requestBody["Head"]["Prefab"]?.ToString();

                // Extract Body properties
                var bodyId = requestBody["Body"]["Id"]?.ToString();
                var bodyPrefab = requestBody["Body"]["Prefab"]?.ToString();

                // Extract Feet properties
                var feetId = requestBody["Feet"]["Id"]?.ToString();
                var feetPrefab = requestBody["Feet"]["Prefab"]?.ToString();

                // Extract Face properties
                var faceId = requestBody["Face"]["Id"]?.ToString();
                var facePrefab = requestBody["Face"]["Prefab"]?.ToString();

                // Extract Facial Hair properties
                var facialHairId = requestBody["Facial Hair"]["Id"]?.ToString();
                var facialHairPrefab = requestBody["Facial Hair"]["Prefab"]?.ToString();


                using (NpgsqlConnection connection = new NpgsqlConnection(DataHandler.DataStorage.connectionString))
                {
                    connection.Open();

                    // Insert character details into the database
                    string insertCharacterSql = $@"
                        INSERT INTO CharacterDetails (userKey, characterUid, name, server, serverIdentifier, gender, seenIntro, skippedTutorial,
                        headId, headPrefab, bodyId, bodyPrefab, feetId, feetPrefab, faceId, facePrefab, facialHairId, facialHairPrefab)
                        VALUES ('{userKey}', '{characterUid}', '{name}', '{server}', '{serverIdentifier}', '{gender}', '{seenIntro}', '{skippedTutorial}',
                        '{headId}', '{headPrefab}', '{bodyId}', '{bodyPrefab}', '{feetId}', '{feetPrefab}', '{faceId}', '{facePrefab}', '{facialHairId}', '{facialHairPrefab}')
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

//Properties:
//Id: 0

//    Character UID:0094183f - 23c6 - 4775 - 9df1 - 60eddda37d1f
//    Name:warsome
//    Server:serverName?
//    Server Identifier:community_server

//Head:
//	Id: h
//    Prefab:hair_mohawk

//Body:
//	Id: t
//    Prefab:torso_ponchoVariantB

//Feet:
//	Id: l
//    Prefab:legs_boots

//Face:
//	Id: f
//    Prefab:face_B

//Facial Hair:
//	Id: f
//    Prefab:facialHair_hunterBeard

//Misc:
//	Gender: Male
//    Seen Intro: Yes
//    Skipped Tutorial: Yes

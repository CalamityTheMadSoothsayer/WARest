using System.Text;
using NetCoreServer;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WorldsAdriftServer.Helper.CharacterSelection;
using WorldsAdriftServer.Objects.CharacterSelection;
using Npgsql;

namespace WorldsAdriftServer.Handlers.CharacterScreen
{
    internal class CharacterListHandler
    {
        internal static void HandleCharacterListRequest(HttpSession session, HttpRequest request, string serverIdentifier, string userKey)
        {
            // Check if there are characters associated with the userKey in the CharacterDetails table
            (RequestRouterHandler.characterList, RequestRouterHandler.status) = GetCharacterList(userKey);
            Console.WriteLine($"Character Retrieval for {userKey} Done.");

            RequestRouterHandler.characterList.Add(Character.GenerateNewCharacter(serverIdentifier, RequestRouterHandler.userName));
            Console.WriteLine("Blank character created");

            CharacterListResponse response = new CharacterListResponse(RequestRouterHandler.characterList);

            if (RequestRouterHandler.characterList.Count == 0)
            {
                // If no characters are found, try to fetch the Steam username
                string steamUsername = GetSteamUsername(userKey);

                Console.WriteLine(steamUsername + " has connected.");

                RequestRouterHandler.userName = steamUsername;

                // Generate a new character with the obtained Steam username
                Character.GenerateRandomCharacter(RequestRouterHandler.Server.ToString(), "placeholder character");

                response.unlockedSlots = 1;
            }
            else
            {
                response.unlockedSlots = RequestRouterHandler.characterList.Count;
            }

            // Use ResponseBuilder to construct and send the response
            Utilities.ResponseBuilder.BuildAndSendResponse(
                session,
                (int)RequestRouterHandler.status,
                "characterList", response.characterList,
                "unlockedSlots", response.unlockedSlots,
                "hasMainCharacter", true, // directly set the value
                "havenFinished", response.havenFinished
            );
        }

        private static string GetSteamUsername( string userKey )
        {
            // Replace with a config entry for server owners
            // obtain api key from here https://steamcommunity.com/dev/apikey
            // tutorial if needed https://www.youtube.com/watch?v=Sb5p8cGyVQw
            string steamApiKey = "7CFC29CEEB87BDC1BDF9637AF4FE2DF1";

            // Build the Steam API URL
            string steamApiUrl = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={steamApiKey}&format=json&steamids={userKey}";

            // Use HttpClient to make the request
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                try
                {
                    // Make a GET request to the Steam API
                    var response = httpClient.GetAsync(steamApiUrl).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;

                        // Parse the response to get the Steam username
                        var responseObject = JsonConvert.DeserializeObject<JObject>(responseContent);

                        if (responseObject != null && responseObject["response"]["players"].HasValues)
                        {
                            var player = responseObject["response"]["players"].First;
                            var steamUsername = player["personaname"].ToString();

                            // Return the obtained Steam username
                            return steamUsername;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error fetching Steam username: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching Steam username: {ex.Message}");
                }
            }

            // Return a default username if there was an error
            return "STEAM USER NAME";
        }

        public static (List<CharacterCreationData>, HttpStatusCode) GetCharacterList(string userKey)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(DataHandler.DataStorage.connectionString))
            {
                connection.Open();

                // Check if the user exists
                string checkUserSql = $"SELECT * FROM UserData WHERE userKey = '{userKey}'";
                using (NpgsqlCommand checkUserCommand = new NpgsqlCommand(checkUserSql, connection))
                using (NpgsqlDataReader checkUserReader = checkUserCommand.ExecuteReader())
                {
                    if (!checkUserReader.HasRows)
                    {
                        Console.WriteLine("User not found.");
                        return (new List<CharacterCreationData>(), HttpStatusCode.NotFound);
                    }
                }

                // Fetch character list based on userKey
                string selectCharacterSql = $"SELECT * FROM CharacterDetails WHERE userKey = '{userKey}'";
                using (NpgsqlCommand selectCharacterCommand = new NpgsqlCommand(selectCharacterSql, connection))
                using (NpgsqlDataReader characterReader = selectCharacterCommand.ExecuteReader())
                {
                    if (characterReader.HasRows)
                    {
                        List<CharacterCreationData> characterList = new List<CharacterCreationData>();
                        while (characterReader.Read())
                        {
                            CharacterCreationData characterData = new CharacterCreationData
                            (
                                characterReader.GetInt32(characterReader.GetOrdinal("id")),
                                characterReader.GetString(characterReader.GetOrdinal("character_uid")),
                                characterReader.GetString(characterReader.GetOrdinal("name")),
                                characterReader.GetString(characterReader.GetOrdinal("server")),
                                characterReader.GetString(characterReader.GetOrdinal("server_identifier")),
                                new Dictionary<CharacterSlotType, ItemData>(), // Initialize if needed
                                new CharacterUniversalColors(), // Initialize if needed
                                characterReader.GetBoolean(characterReader.GetOrdinal("is_male")),
                                characterReader.GetBoolean(characterReader.GetOrdinal("seen_intro")),
                                characterReader.GetBoolean(characterReader.GetOrdinal("skipped_tutorial"))
                            );


                            characterList.Add(characterData);
                        }

                        return (characterList, HttpStatusCode.OK);
                    }
                    else
                    {
                        Console.WriteLine("No characters found for the user.");
                        return (new List<CharacterCreationData>(), HttpStatusCode.NotFound);
                    }
                }
            }
        }

    }
}

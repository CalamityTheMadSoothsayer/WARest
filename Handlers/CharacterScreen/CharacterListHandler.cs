using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NetCoreServer;
using Npgsql;
using WorldsAdriftServer.Helper.CharacterSelection;
using WorldsAdriftServer.Objects.CharacterSelection;
using WorldsAdriftServer.Objects.UnityObjects;

namespace WorldsAdriftServer.Handlers.CharacterScreen
{
    internal class CharacterListHandler
    {
        internal static void HandleCharacterListRequest(HttpSession session, HttpRequest request, string serverIdentifier, string userKey)
        {
            try
            {
                // Check if there are characters associated with the userKey in the CharacterDetails table
                (RequestRouterHandler.characterList, RequestRouterHandler.status) = GetCharacterList(userKey);
                Console.WriteLine($"Character Retrieval for {userKey} Done.");

                // If no characters are found, try to fetch the Steam username
                RequestRouterHandler.userName = GetSteamUsername(userKey);

                Console.WriteLine($"{RequestRouterHandler.userName} has connected.");

                // add a new character to the list
                RequestRouterHandler.characterList.Add(Character.GenerateNewCharacter(serverIdentifier, RequestRouterHandler.userName));
                Console.WriteLine("Blank character created");

                CharacterListResponse response = new CharacterListResponse(RequestRouterHandler.characterList);

                // Determine the number of unlocked slots
                response.unlockedSlots = Math.Max(1, RequestRouterHandler.characterList.Count);

                // Use ResponseBuilder to construct and send the response
                Utilities.ResponseBuilder.BuildAndSendResponse(
                    session,
                    (int)RequestRouterHandler.status,
                    "characterList", response.characterList,
                    "unlockedSlots", response.unlockedSlots,
                    "hasMainCharacter", true,
                    "havenFinished", true
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling character list request: {ex.Message}");
                // Handle the exception or log it appropriately
                // Set an appropriate status code for the response
                RequestRouterHandler.status = HttpStatusCode.InternalServerError;
            }
        }

        private static string GetSteamUsername(string userKey)
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
                string checkUserSql = $"SELECT * FROM userdata WHERE userKey = '{userKey}'";
                using (NpgsqlCommand checkUserCommand = new NpgsqlCommand(checkUserSql, connection))
                using (NpgsqlDataReader checkUserReader = checkUserCommand.ExecuteReader())
                {
                    if (!checkUserReader.HasRows)
                    {
                        Console.WriteLine("User not found.");
                        return (new List<CharacterCreationData>(), HttpStatusCode.OK);
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
                            // Map properties from the database to CharacterCreationData
                            CharacterCreationData characterData = new CharacterCreationData
                            (
                                characterReader.GetInt32(characterReader.GetOrdinal("id")),
                                characterReader.GetString(characterReader.GetOrdinal("characteruid")),
                                characterReader.GetString(characterReader.GetOrdinal("name")),
                                characterReader.GetString(characterReader.GetOrdinal("server")),
                                characterReader.GetString(characterReader.GetOrdinal("serveridentifier")),
                                new Dictionary<CharacterSlotType, ItemData>(), // Initialize if needed
                                new CharacterUniversalColors(), // Initialize if needed
                                characterReader.GetBoolean(characterReader.GetOrdinal("ismale")),
                                characterReader.GetBoolean(characterReader.GetOrdinal("seenintro")),
                                characterReader.GetBoolean(characterReader.GetOrdinal("skippedtutorial"))
                            );

                            // Retrieve cosmetics and universal colors properties
                            characterData.Cosmetics = ExtractCosmetics(characterReader);
                            characterData.UniversalColors = ExtractUniversalColors(characterReader);

                            characterList.Add(characterData);
                        }

                        return (characterList, HttpStatusCode.OK);
                    }
                    else
                    {
                        Console.WriteLine("No characters found for the user.");
                        // this is fine, user can create characters
                        return (new List<CharacterCreationData>(), HttpStatusCode.OK);
                    }
                }
            }
        }

        // Helper method to extract cosmetics properties from the reader
        private static Dictionary<CharacterSlotType, ItemData> ExtractCosmetics(NpgsqlDataReader reader)
        {
            Dictionary<CharacterSlotType, ItemData> cosmetics = new Dictionary<CharacterSlotType, ItemData>();

            // Add logic to extract cosmetics properties from the reader
            string cosmeticsJson = reader.GetString(reader.GetOrdinal("cosmetics"));

            if (!string.IsNullOrEmpty(cosmeticsJson))
            {
                JObject cosmeticsObject = JObject.Parse(cosmeticsJson);

                foreach (CharacterSlotType slotType in Enum.GetValues(typeof(CharacterSlotType)))
                {
                    if (slotType != CharacterSlotType.None)
                    {
                        string slotName = slotType.ToString();
                        var itemData = ExtractItemData(cosmeticsObject, slotName);
                        if (itemData != null)
                        {
                            cosmetics[slotType] = itemData;
                        }
                    }
                }
            }

            return cosmetics;
        }

        // Helper method to extract universal colors properties from the reader
        private static CharacterUniversalColors ExtractUniversalColors(NpgsqlDataReader reader)
        {
            CharacterUniversalColors universalColors = new CharacterUniversalColors();

            // Add logic to extract universal colors properties from the reader
            universalColors.HairColor = ExtractUnityColor(reader, "universalcolorshaircolor");
            universalColors.SkinColor = ExtractUnityColor(reader, "universalcolorsskincolor");
            universalColors.LipColor = ExtractUnityColor(reader, "universalcolorslipcolor");

            return universalColors;
        }

        // Helper method to extract a UnityColor from the reader based on field names
        private static UnityColor ExtractUnityColor(NpgsqlDataReader reader, string fieldNamePrefix)
        {
            UnityColor color = new UnityColor
            {
                r = (float)reader.GetDouble(reader.GetOrdinal($"{fieldNamePrefix}r")),
                g = (float)reader.GetDouble(reader.GetOrdinal($"{fieldNamePrefix}g")),
                b = (float)reader.GetDouble(reader.GetOrdinal($"{fieldNamePrefix}b")),
                a = (float)reader.GetDouble(reader.GetOrdinal($"{fieldNamePrefix}a")) 
            };

            return color;
        }


        // Helper method to extract item data from the cosmetics JSON
        private static ItemData ExtractItemData(JObject cosmeticsObject, string slotName)
        {
            var itemDataJson = cosmeticsObject[slotName];

            if (itemDataJson != null)
            {
                return new ItemData
                (
                    itemDataJson["Id"]?.ToString(),
                    itemDataJson["Prefab"]?.ToString(),
                    ExtractColorProperties(itemDataJson["ColorProps"]),
                    itemDataJson["Health"]?.ToObject<float>() ?? 0.0f
                );
            }

            return null;
        }

        // Helper method to extract color properties
        private static ColorProperties ExtractColorProperties(JToken colorPropsJson)
        {
            return new ColorProperties
            {
                PrimaryColor = ExtractColor(colorPropsJson?["PrimaryColor"]),
                SecondaryColor = ExtractColor(colorPropsJson?["SecondaryColor"]),
                TertiaryColor = ExtractColor(colorPropsJson?["TertiaryColor"]),
                SpecColor = ExtractColor(colorPropsJson?["SpecColor"])
            };
        }

        // Helper method to extract a UnityColor from a JSON object
        private static UnityColor ExtractColor(JToken colorJson)
        {
            return new UnityColor
            {
                r = colorJson?["r"]?.ToObject<float>() ?? 0.0f,
                g = colorJson?["g"]?.ToObject<float>() ?? 0.0f,
                b = colorJson?["b"]?.ToObject<float>() ?? 0.0f,
                a = colorJson?["a"]?.ToObject<float>() ?? 0.0f
            };
        }

    }
}

using System.Text;
using NetCoreServer;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WorldsAdriftServer.Helper.CharacterSelection;
using WorldsAdriftServer.Objects.CharacterSelection;

namespace WorldsAdriftServer.Handlers.CharacterScreen
{
    internal class CharacterListHandler
    {
        internal static void HandleCharacterListRequest( HttpSession session, HttpRequest request, string serverIdentifier, string userKey )
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

            response.hasMainCharacter = true;
            response.havenFinished = true;

            JObject responseObject = (JObject)JToken.FromObject(response);
            Console.WriteLine(responseObject.ToString());

            if (responseObject != null)
            {
                HttpResponse httpResponse = new HttpResponse();
                // use status from previous operation
                httpResponse.SetBegin((int)RequestRouterHandler.status);
                httpResponse.SetBody(responseObject.ToString());

                session.SendResponseAsync(httpResponse);
            }
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

        public static (List<CharacterCreationData>, HttpStatusCode) GetCharacterList( string userKey )
        {
            lock (DataHandler.DataStorage.apiLock)
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var requestData = new
                    {
                        UserKey = userKey
                    };

                    var jsonData = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

                    // Make a POST request to API endpoint to fetch character data
                    try
                    {
                        response = httpClient.PostAsync($"{DataHandler.DataStorage.ApiBaseUrl}/getCharacterList.php", content).Result;
                    }
                    catch (Exception ex)
                    {
                        // Handle failure
                        Console.WriteLine($"Error making POST request: {ex.Message}");
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;

                        try
                        {
                            var responseObject = JsonConvert.DeserializeObject<JObject>(responseContent);

                            if (responseObject != null && responseObject["status"].ToString() == "success")
                            {
                                // Extract and return character list from the response along with the HTTP status code
                                var characterList = responseObject["characterList"].ToObject<List<CharacterCreationData>>();
                                return (characterList, response.StatusCode);
                            }
                            else
                            {
                                Console.WriteLine($"Error fetching character data: {response.StatusCode}");
                                // Handle error accordingly
                            }
                        }
                        catch (JsonReaderException)
                        {
                            // Handle response as a simple string (assuming it's an error message)
                            Console.WriteLine($"Error fetching character data: {responseContent}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error fetching character data: {response.StatusCode}");
                        // Handle error accordingly
                    }

                    // Return an empty list and the HTTP status code if there was an error or no characters found
                    return (new List<CharacterCreationData>(), response.StatusCode);
                }
            }
        }

    }
}

using System;
using System.Net;
using NetCoreServer;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using WorldsAdriftServer.Handlers.DataHandler;

namespace WorldsAdriftServer.Handlers.CharacterScreen
{
    internal class CharacterNameHandler
    {
        public static void SaveCharacterUid( HttpRequest request, string userKey, HttpSession session )
        {
            try
            {
                // Parse the request body to get the character UID
                JObject requestBody = JObject.Parse(request.Body);

                // Extract data from the request
                var requestData = new
                {
                    screenName = userKey,
                    characterUid = requestBody["characterUid"]?.ToString()
                };

                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    // Convert the data to JSON
                    var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);

                    // Set the content type
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

                    // Make a POST request to your PHP API endpoint to save the character UID
                    var response = httpClient.PostAsync($"{DataStorage.ApiBaseUrl}/saveCharacterUid.php", new StringContent(jsonData, Encoding.UTF8, "application/json")).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        var responseObject = JObject.Parse(responseContent);

                        if (responseObject["status"].ToString() == "success")
                        {
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
                        else
                        {
                            Console.WriteLine($"Error saving character UID: {responseObject["message"]}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error saving character UID: {response.StatusCode}");
                    }
                }
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

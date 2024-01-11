using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WorldsAdriftServer.Handlers.DataHandler
{
    public class DataStorage
    {
        public static readonly ConcurrentDictionary<string, JObject> userDataDictionary = new ConcurrentDictionary<string, JObject>();
        public static readonly object apiLock = new object();
        public const string ApiBaseUrl = "https://projexstudio.net"; // API endpoint, store this in config so it can be changed

        public static void StoreUserData( string SessionId, string userKey )
        {
            int retryCount = 3;
            bool success = false;

            while (retryCount > 0)
            {
                lock (apiLock)
                {
                    using (var httpClient = new HttpClient())
                    {
                        var userData = new
                        {
                            UserKey = userKey,
                            sessionId = SessionId
                        };

                        var jsonData = JsonConvert.SerializeObject(userData);
                        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                        // Default to failed
                        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

                        // Make a POST request to API endpoint to store user data
                        try
                        {
                            response = httpClient.PostAsync($"{ApiBaseUrl}/storeUserData.php", content).Result;
                        }
                        catch (Exception ex)
                        {
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
                                    Console.WriteLine($"\n\r Success: {responseObject["message"]}");
                                    RequestRouterHandler.status = response.StatusCode;
                                    success = true;
                                    break; 
                                }
                                else
                                {
                                    Console.WriteLine($"Error storing user data: {response.StatusCode}");
                                    RequestRouterHandler.status = response.StatusCode;
                                    // Handle error accordingly
                                }
                            }
                            catch (JsonReaderException ex)
                            {
                                Console.WriteLine($"FAILED to store user data. [REASON] {ex.Message}");
                                RequestRouterHandler.status = response.StatusCode;
                                break; 
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Error storing user data: {response.StatusCode}");
                            RequestRouterHandler.status = response.StatusCode;
                        }
                    }
                }

                // Retry logic
                retryCount--;
                Console.WriteLine($"Retrying... {retryCount} attempts remaining");
                System.Threading.Thread.Sleep(1000); // Sleep for 1 second before retrying
            }

            if (!success)
            {
                Console.WriteLine("StoreUserData: Max retry count reached. Operation failed.");
            }
        }

        public static void LoadUserDataFromApi()
        {
            int retryCount = 3;
            bool success = false;

            while (retryCount > 0)
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        HttpResponseMessage response = httpClient.GetAsync($"{ApiBaseUrl}/getUserData.php").Result;

                        if (response.IsSuccessStatusCode)
                        {
                            var json = response.Content.ReadAsStringAsync().Result;
                            var responseObject = JsonConvert.DeserializeObject<JObject>(json);

                            if (responseObject != null)
                            {
                                // Print all data received in the response
                                Console.WriteLine($"Message: {responseObject["message"]}");
                                Console.WriteLine($"SessionUid: {responseObject["sessionUid"]}");

                                RequestRouterHandler.sessionId = responseObject["sessionUid"].ToString();

                                var userDataArray = responseObject["userData"] as JArray;
                                
                                lock (apiLock)
                                {
                                    DataStorage.userDataDictionary.Clear();
                                    foreach (var entry in userDataArray)
                                    {
                                        var key = entry["userKey"].ToString();
                                        DataStorage.userDataDictionary.TryAdd(key, (JObject)entry);
                                    }
                                }

                                // Extract userKey from the first entry in the data list
                                var userKey = userDataArray.FirstOrDefault()?["userKey"]?.ToString();
                                if (!string.IsNullOrEmpty(userKey))
                                {
                                    // userKey gets assigned by request, just check if it matches
                                    if (userDataArray != null)
                                    {
                                        foreach (var userDataEntry in userDataArray)
                                        {
                                            if(userDataEntry["userKey"].ToString() == userKey)
                                            {
                                                success = true;
                                            }
                                            else
                                            {
                                                success = false;
                                            }
                                        }
                                    }
                                }

                                Console.WriteLine($"\n\r Success: Loaded user data from API");
                                break;
                            }
                            else
                            {
                                Console.WriteLine($"Error loading user data: {response.StatusCode}");
                                RequestRouterHandler.status = response.StatusCode;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var response = new HttpResponseMessage();
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    Console.WriteLine($"Error loading user data: {ex.Message}");
                    RequestRouterHandler.status = response.StatusCode;
                }

                // Retry logic
                retryCount--;
                Console.WriteLine($"Retrying... {retryCount} attempts remaining");
                System.Threading.Thread.Sleep(1000); // Sleep for 1 second before retrying
            }

            if (!success)
            {
                var response = new HttpResponseMessage();
                response.StatusCode = HttpStatusCode.InternalServerError;
                Console.WriteLine("LoadUserDataFromApi: Max retry count reached. Operation failed.");
                RequestRouterHandler.status = response.StatusCode;
            }
        }

    }
}

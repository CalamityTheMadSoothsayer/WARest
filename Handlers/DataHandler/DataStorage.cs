using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;
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
                    // Your database connection string
                    string connectionString = $"Data Source={RequestRouterHandler.serverName};Initial Catalog={RequestRouterHandler.dbName};User Id={RequestRouterHandler.userName};Password={RequestRouterHandler.password};";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        // Your SQL query to retrieve user data
                        string sqlQuery = "SELECT * FROM UserData";

                        using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    JArray userDataArray = new JArray();

                                    while (reader.Read())
                                    {
                                        // Assuming your UserData table has 'username' and 'email' columns
                                        JObject userData = new JObject
                                        {
                                            { "username", reader["username"].ToString() },
                                            { "email", reader["email"].ToString() }
                                            // Add more fields as needed
                                        };

                                        userDataArray.Add(userData);
                                    }

                                    // Include session ID in the response
                                    JObject response = new JObject
                                    {
                                        { "status", "success" },
                                        { "message", "User data retrieved successfully" },
                                        { "sessionUid", RequestRouterHandler.sessionId },
                                        { "userData", userDataArray }
                                    };

                                    // Convert response to JSON string
                                    string jsonResponse = response.ToString();

                                    // Handle the jsonResponse as needed (e.g., send it back in the HTTP response)
                                    Console.WriteLine(jsonResponse);
                                }
                                else
                                {
                                    Console.WriteLine("No user data found.");
                                }
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

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;

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

            while (retryCount > 0)
            {
                try
                {
                    // Your database connection string for PostgreSQL
                    string connectionString = $"Host={RequestRouterHandler.serverName};Port=5432;Database={RequestRouterHandler.dbName};Username={RequestRouterHandler.username};Password={RequestRouterHandler.password};";

                    using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                    {
                        connection.Open();

                        // Your SQL query to retrieve user data
                        string sqlQuery = "SELECT * FROM UserData";

                        using (NpgsqlCommand command = new NpgsqlCommand(sqlQuery, connection))
                        {
                            using (NpgsqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    JArray userDataArray = new JArray();

                                    while (reader.Read())
                                    {
                                        // Assuming your UserData table has 'userKey', 'characterUID', and 'sessionToken' columns
                                        JObject userData = new JObject
                                        {
                                            { "userKey", reader["userKey"].ToString() },
                                            { "characterUID", reader["characterUID"].ToString() },
                                            { "sessionToken", reader["sessionToken"].ToString() }
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

                                    // Operation succeeded, break out of the retry loop
                                    return;
                                }
                                else
                                {
                                    Console.WriteLine("No user data found.");
                                }
                            }
                        }
                    }

                    // If no exception occurred, decrement the retry count
                    retryCount--;
                    Console.WriteLine($"Retrying... {retryCount} attempts remaining");
                    System.Threading.Thread.Sleep(1000); // Sleep for 1 second before retrying
                }
                catch (NpgsqlException ex)
                {
                    // Log the exception for analysis
                    Console.WriteLine($"Npgsql Exception: {ex.Message}");

                    // Retry only for specific Npgsql exceptions that indicate transient issues
                    if (IsTransientNpgsqlException(ex))
                    {
                        // Decrement the retry count
                        retryCount--;
                        Console.WriteLine($"Retrying... {retryCount} attempts remaining");
                        System.Threading.Thread.Sleep(1000); // Sleep for 1 second before retrying
                    }
                    else
                    {
                        // Break out of the retry loop for non-transient exceptions
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception for analysis
                    Console.WriteLine($"Exception: {ex.Message}");

                    // Retry only for specific exceptions that indicate transient issues
                    if (IsTransientException(ex))
                    {
                        // Decrement the retry count
                        retryCount--;
                        Console.WriteLine($"Retrying... {retryCount} attempts remaining");
                        System.Threading.Thread.Sleep(1000); // Sleep for 1 second before retrying
                    }
                    else
                    {
                        // Break out of the retry loop for non-transient exceptions
                        break;
                    }
                }
            }

            // If the operation still fails after all retries, handle it accordingly
            Console.WriteLine("LoadUserDataFromApi: Max retry count reached. Operation failed.");
            // You might want to set the status code or take other actions
        }

        private static bool IsTransientNpgsqlException(NpgsqlException ex)
        {
            // Implement your logic to determine if the NpgsqlException is transient
            // You might want to check specific error codes, messages, or other properties
            return true; // Replace with your actual logic
        }

        private static bool IsTransientException(Exception ex)
        {
            // Implement your logic to determine if the exception is transient
            // You might want to check specific error codes, messages, or other properties
            return true; // Replace with your actual logic
        }
    }
}

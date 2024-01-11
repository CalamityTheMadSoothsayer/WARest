using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NetCoreServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace WorldsAdriftServer.Handlers.DataHandler
{
    public class DataStorage
    {
        public static readonly ConcurrentDictionary<string, JObject> userDataDictionary = new ConcurrentDictionary<string, JObject>();
        public static string connectionString = $"Host={RequestRouterHandler.serverName};Port=5432;Database={RequestRouterHandler.dbName};Username={RequestRouterHandler.username};Password={RequestRouterHandler.password};";

        public static void StoreUserData(HttpSession session, string SessionId, string userKey)
        {
            int retryCount = 3;
            bool success = false;

            while (retryCount > 0)
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

                try
                {
                    using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                    {
                        connection.Open();

                        // Check if the character already exists for the player
                        string checkCharacterSql = $"SELECT userKey FROM CharacterDetails WHERE userKey = '{userKey}'";
                        using (NpgsqlCommand checkCharacterCommand = new NpgsqlCommand(checkCharacterSql, connection))
                        using (NpgsqlDataReader checkCharacterReader = checkCharacterCommand.ExecuteReader())
                        {
                            if (checkCharacterReader.HasRows)
                            {
                                // Character already exists, send appropriate response
                                Console.WriteLine("Character data already exists.");

                                // Build and send response
                                BuildAndSendErrorResponse(session, "Character data already exists.");

                                return;
                            }
                        }

                        // Check if the server session is set
                        if (String.IsNullOrEmpty(RequestRouterHandler.sessionId))
                        {
                            // Game Session is not set, update the session token in UserData using a prepared statement
                            string updateSessionSql = $"UPDATE UserData SET sessionToken = '{SessionId}' WHERE userKey = '{userKey}'";
                            using (NpgsqlCommand updateSessionCommand = new NpgsqlCommand(updateSessionSql, connection))
                            {
                                if (updateSessionCommand.ExecuteNonQuery() > 0)
                                {
                                    // Store userKey in the session
                                    RequestRouterHandler.sessionId = SessionId;
                                    Console.WriteLine("Session updated successfully.");
                                    retryCount = 0;
                                    success = true;

                                    // Build and send success response
                                    BuildAndSendSuccessResponse(session);

                                    break;
                                }
                                else
                                {
                                    // Include the complete error message in the response
                                    Console.WriteLine($"Error updating session: {updateSessionSql}");

                                    // Build and send error response
                                    BuildAndSendErrorResponse(session, $"Error updating session: {updateSessionSql}");
                                }
                            }
                        }
                        else
                        {
                            // Session is already set, send appropriate response
                            Console.WriteLine("Session already set.");

                            // Build and send error response
                            BuildAndSendErrorResponse(session, "Session already set.");

                            return;
                        }
                    }
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
                        // Build and send error response
                        BuildAndSendErrorResponse(session, $"Npgsql Exception: {ex.Message}");

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
                        // Build and send error response
                        BuildAndSendErrorResponse(session,$"Exception: {ex.Message}");

                        // Break out of the retry loop for non-transient exceptions
                        break;
                    }
                }
            }

            if (!success)
            {
                Console.WriteLine("StoreUserData: Max retry count reached. Operation failed.");

                // Build and send error response
                BuildAndSendErrorResponse(session,"Max retry count reached. Operation failed.");
            }
        }

        private static void BuildAndSendSuccessResponse(HttpSession session)
        {
            var successResponse = new
            {
                status = "success",
                message = "Session updated successfully"
                // Add additional fields as needed
            };

            var successJson = JsonConvert.SerializeObject(successResponse);
            var successContent = new StringContent(successJson, Encoding.UTF8, "application/json");

            // Send success response
            HttpResponse resp = new HttpResponse();

            resp.SetBegin(200);
            resp.SetBody("{}"); // the game does want to have a valid JObject. Its stored in CharacterSelectionHandler.LastReceivedCharacterList so maybe important to pass valid stuff here in the future

            session.SendResponseAsync(resp);
        }

        private static void BuildAndSendErrorResponse(HttpSession session, string errorMessage)
        {
            var errorResponse = new
            {
                status = "error",
                message = errorMessage
                // Add additional fields as needed
            };

            var errorJson = JsonConvert.SerializeObject(errorResponse);
            var errorContent = new StringContent(errorJson, Encoding.UTF8, "application/json");

            // Send error response
            HttpResponse resp = new HttpResponse();

            resp.SetBegin(500);
            resp.SetBody("{}"); // the game does want to have a valid JObject. Its stored in CharacterSelectionHandler.LastReceivedCharacterList so maybe important to pass valid stuff here in the future

            session.SendResponseAsync(resp);
        }


        public static void LoadUserDataFromApi()
        {
            int retryCount = 3;

            while (retryCount > 0)
            {
                try
                {
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
                                    retryCount = 0;
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

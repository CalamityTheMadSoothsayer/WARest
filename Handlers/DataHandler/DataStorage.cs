using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.IdentityModel.Tokens;
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

        public static void LoadUserDataFromApi()
        {
            int retryCount = 3;
            bool newPlayer = false;

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
                                        };

                                        userDataArray.Add(userData);

                                        // Add user data to the dictionary
                                        userDataDictionary.TryAdd(reader["userKey"].ToString(), userData);
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
                                    newPlayer = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
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

            if (!newPlayer)
            {
                // If the operation still fails after all retries, handle it accordingly
                Console.WriteLine("LoadUserDataFromApi: Max retry count reached. Operation failed.");
                // You might want to set the status code or take other actions
            }
        }

        public static void StoreUserData(HttpSession session, string SessionId, string userKey)
        {
            int retryCount = 3;
            bool success = false;
            bool previousUser = false;
            string dbSession = "-1";

            while (retryCount > 0)
            {
                try
                {
                    using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                    {
                        connection.Open();

                        // Check if the user already exists
                        string checkUserSql = $"SELECT * FROM userdata WHERE userKey = '{userKey}'";
                        using (NpgsqlCommand checkUserCommand = new NpgsqlCommand(checkUserSql, connection))
                        using (NpgsqlDataReader checkUserReader = checkUserCommand.ExecuteReader())
                        {
                            if (checkUserReader.HasRows)
                            {
                                // User already exists
                                Console.WriteLine("User data already exists.");

                                previousUser = true;
                                checkUserReader.Read();
                                dbSession = checkUserReader["sessionToken"].ToString();
                            }
                        }

                        // Check if the session matches
                        
                        if (dbSession != SessionId)
                        {
                            // Session doesn't match, update the session token
                            string updateSessionSql = $"UPDATE userdata SET sessionToken = '{SessionId}' WHERE userKey = '{userKey}'";
                            using (NpgsqlCommand updateSessionCommand = new NpgsqlCommand(updateSessionSql, connection))
                            {
                                if (updateSessionCommand.ExecuteNonQuery() > 0)
                                {
                                    // Update successful
                                    Console.WriteLine("Session updated successfully.");
                                    retryCount = 0;
                                    success = true;
                                    RequestRouterHandler.status = HttpStatusCode.OK;
                                }
                                else
                                {
                                    // Update failed
                                    Console.WriteLine($"Error updating session: {updateSessionSql}");
                                    RequestRouterHandler.status = HttpStatusCode.InternalServerError;
                                }
                            }
                        }
                        else
                        {
                            // Session matches, send OK response
                            Console.WriteLine("Session already matches.");
                            RequestRouterHandler.status = HttpStatusCode.OK;
                            return;
                        }
                          
                        // If the user doesn't exist, insert a new user
                        if (!previousUser)
                        {
                            string insertUserSql = $"INSERT INTO userdata (userKey, sessionToken) VALUES ('{userKey}', '{SessionId}')";
                            using (NpgsqlCommand insertUserCommand = new NpgsqlCommand(insertUserSql, connection))
                            {
                                if (insertUserCommand.ExecuteNonQuery() > 0)
                                {
                                    // Insert successful
                                    Console.WriteLine("User inserted successfully.");
                                    retryCount = 0;
                                    success = true;
                                    RequestRouterHandler.status = HttpStatusCode.OK;
                                    break;
                                }
                                else
                                {
                                    // Insert failed
                                    Console.WriteLine($"Error inserting user: {insertUserSql}");
                                    RequestRouterHandler.status = HttpStatusCode.InternalServerError;
                                    break;
                                }
                            }
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
                        // Set variables here as needed
                        RequestRouterHandler.status = HttpStatusCode.InternalServerError;

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
                        // Set variables here as needed
                        RequestRouterHandler.status = HttpStatusCode.InternalServerError;

                        // Break out of the retry loop for non-transient exceptions
                        break;
                    }
                }
            }

            if (!success)
            {
                // Insert or update failed
                Console.WriteLine("Insert or update failed.");
                RequestRouterHandler.status = HttpStatusCode.InternalServerError;
            }
        


            if (!success)
            {
                Console.WriteLine("StoreUserData: Max retry count reached. Operation failed.");

                // Set variables here as needed
                RequestRouterHandler.status = HttpStatusCode.InternalServerError;
            }
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

using Npgsql;
using Newtonsoft.Json.Linq;
using NetCoreServer;
using WorldsAdriftServer.Utilities;

namespace WorldsAdriftServer.Handlers.CharacterScreen
{
    internal static class ComponentHandler
    {
        private static string connectionString = DataHandler.DataStorage.connectionString;

        internal static void getComponentDate(HttpSession session, HttpRequest request)
        {
            JObject requestBody = JObject.Parse(request.Body);

            try
            {
                long entityId = Convert.ToInt64(requestBody["entityId"]);

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand("SELECT componentid, data FROM gameobjects WHERE entityid = @entityId", connection))
                    {
                        cmd.Parameters.AddWithValue("entityId", entityId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            JObject responseData = new JObject();

                            while (reader.Read())
                            {
                                long componentId = reader.GetInt64(0);
                                string jsonData = reader.GetString(1);

                                // Add component data to the JSON response
                                responseData.Add(new JProperty($"component_{componentId}", JObject.Parse(jsonData)));
                            }

                            if (responseData.Count > 0)
                            {
                                // Build response with entityId as the root and components as keys
                                Utilities.ResponseBuilder.BuildAndSendResponse(
                                    session,
                                    (int)RequestRouterHandler.status,
                                    "entityId", entityId,
                                    "components", responseData
                                );
                            }
                            else
                            {
                                ResponseBuilder.BuildAndSendResponse(session, 500, "status", "No data found for the specified entity.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ResponseBuilder.BuildAndSendResponse(session, 500, "status", "error: " + ex.Message);
            }
        }


        internal static void setComponentDate(HttpSession session, HttpRequest request)
        {
            JObject requestBody = JObject.Parse(request.Body);

            try
            {
                long entityId = Convert.ToInt64(requestBody["entityId"]);
                long componentId = Convert.ToInt64(requestBody["componentId"]);
                string jsonData = requestBody["data"].ToString();

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand("INSERT INTO gameobjects (entityid, componentid, data) VALUES (@entityId, @componentId, @data) ON CONFLICT (entityid, componentid) DO UPDATE SET data = @data", connection))
                    {
                        cmd.Parameters.AddWithValue("entityId", entityId);
                        cmd.Parameters.AddWithValue("componentId", componentId);
                        cmd.Parameters.AddWithValue("data", jsonData);

                        cmd.ExecuteNonQuery();

                        // Send a successful response with HTTP status code 200
                        ResponseBuilder.BuildAndSendResponse(session, 200, "status", "success");
                    }
                }
            }
            catch (Exception ex)
            {
                ResponseBuilder.BuildAndSendResponse(session, 500, "status", "error: " + ex.Message);
            }
        }

    }
}

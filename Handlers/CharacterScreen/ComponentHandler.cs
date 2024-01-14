using Npgsql;
using Newtonsoft.Json.Linq;
using System;
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
                long componentId = Convert.ToInt64(requestBody["componentId"]);

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand("SELECT data FROM gameobjects WHERE entityid = @entityId AND componentid = @componentId", connection))
                    {
                        cmd.Parameters.AddWithValue("entityId", entityId);
                        cmd.Parameters.AddWithValue("componentId", componentId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string jsonData = reader.GetString(0);

                                // Build response
                                Utilities.ResponseBuilder.BuildAndSendResponse(
                                    session,
                                    (int)RequestRouterHandler.status,
                                    "entityId", entityId,
                                    "componentId", componentId,
                                    "data", jsonData
                                );
                            }
                            else
                            {
                                ResponseBuilder.BuildAndSendResponse(session, 500, "status", "No data found for the specified entity and component.");
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

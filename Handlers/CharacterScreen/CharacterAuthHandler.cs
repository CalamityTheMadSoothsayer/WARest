using NetCoreServer;
using Newtonsoft.Json.Linq;
using System.Text;
using WorldsAdriftServer.Objects.CharacterSelection;
using static System.Net.WebRequestMethods;

namespace WorldsAdriftServer.Handlers.CharacterScreen
{
    internal static class CharacterAuthHandler
    {
        internal static void HandleCharacterAuth(HttpSession session, HttpRequest request)
        {
            // Assuming Headers is a long
            long headersCount = (long)request.Headers;

            // Extracting headers from the request
            string securityToken = GetHeaderValue(request, "Security", headersCount);
            string characterUid = GetHeaderValue(request, "characterUid", headersCount);

            // Validate securityToken and characterUid if needed (future improvement)

            // Creating a response object
            HttpResponse resp = new HttpResponse();
            CharacterAuthResponse authResp = new CharacterAuthResponse("token", "1", 123, "12.12.12", true);

            // Converting the response object to JSON
            JObject respO = (JObject)JToken.FromObject(authResp);

            if (respO != null)
            {
                resp.SetBegin(200);
                resp.SetBody(respO.ToString());

                // Sending the response asynchronously
                session.SendResponseAsync(resp);
            }
        }

        private static string GetHeaderValue(HttpRequest request, string headerName, long headersCount)
        {
            // Implement your logic to extract the header value based on your actual scenario
            // This is a placeholder and should be adapted to your specific situation
            for (int i = 0; i < headersCount; i++)
            {
                // Adjust the index and logic based on the actual structure of request.Headers
                string header = request.Header(i).ToString();

                if (header.StartsWith(headerName))
                {
                    // Assuming a format like "Security: WarToken"
                    return header.Split(':')[1]?.Trim();
                }
            }

            return "PlaceholderHeaderValue"; // Replace with actual logic
        }
    }
}

    //Request method: GET
    //Request URL: / authorizeCharacter
    //Request protocol: HTTP / 1.1
    //Request headers: 8
    //Security: WarToken
    //characterUid : 5338fdfb - a4c9 - 437f - a903 - a311e372cce8
    //Host: 89.116.212.172:8080
    //Accept - Encoding : gzip, identity
    //Connection : Keep - Alive, TE
    //TE : identity
    //User-Agent : BestHTTP
    //Content-Length : 0
    //Request body: 0

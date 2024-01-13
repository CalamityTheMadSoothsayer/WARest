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
            long headersValue = (long)request.Headers;

            // Convert the long value to a string (or the appropriate type) based on your actual implementation
            string headersString = headersValue.ToString();

            Console.WriteLine(headersString);

            // Validate securityToken and characterUid if needed (future improvement)

            // Creating a response object
            CharacterAuthResponse authResp = new CharacterAuthResponse("WarToken", RequestRouterHandler.userId, 123, "12.12.12", true);

            // Converting the response object to JSON
            JObject respO = (JObject)JToken.FromObject(authResp);

            // Creating the HTTP response
            HttpResponse resp = new HttpResponse();
            if (respO != null)
            {
                resp.SetBegin(200);
                resp.SetBody(respO.ToString());

                // Sending the response asynchronously
                session.SendResponseAsync(resp);
            }
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

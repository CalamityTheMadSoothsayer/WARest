using NetCoreServer;
using Newtonsoft.Json.Linq;
using WorldsAdriftServer.Objects.SteamObjects;

namespace WorldsAdriftServer.Handlers.Authentication
{
    internal static class SteamAuthenticationHandler
    {
        internal static void HandleAuthRequest(HttpSession session, HttpRequest request, string playerName)
        {
            JObject reqO = JObject.Parse(request.Body);
            if (reqO != null)
            {
                SteamAuthRequestToken reqToken = reqO.ToObject<SteamAuthRequestToken>();

                if (reqToken != null)
                {
                    // i think this token hats usernames with spaces


                    SteamAuthResponseToken respToken = new SteamAuthResponseToken("WarToken", RequestRouterHandler.userId, "999", true);
                    respToken.screenName = playerName;
                    respToken.playerId = RequestRouterHandler.userId;

                    JObject respO = (JObject)JToken.FromObject(respToken);
                    if (respO != null)
                    {
                        HttpResponse resp = new HttpResponse();
                        resp.SetBegin((int)RequestRouterHandler.status);
                        resp.SetBody(respO.ToString());

                        session.SendResponseAsync(resp);
                    }
                }
            }
        }
    }
}

using NetCoreServer;
using Newtonsoft.Json.Linq;
using WorldsAdriftServer.Objects.CharacterSelection;
using WorldsAdriftServer.Helper.CharacterSelection;

namespace WorldsAdriftServer.Handlers.CharacterScreen
{
    internal class CharacterSlotHandler
    {
        internal static void HandleCharacterSlotResponse( HttpSession session, HttpRequest request, string serverIdentifier, string userKey )
        {
            RequestRouterHandler.CharacterResponse = new CharacterListResponse(RequestRouterHandler.characterList)
            {
                unlockedSlots = RequestRouterHandler.characterList.Count,
                hasMainCharacter = true,
                havenFinished = true
            };

            JObject CresponseObject = (JObject)JToken.FromObject(RequestRouterHandler.CharacterResponse);

            // Send response based on the reservation result
            HttpResponse httpResponse = new HttpResponse();

            httpResponse.SetBegin((int)RequestRouterHandler.status);

            httpResponse.SetBody(CresponseObject.ToString());
            session.SendResponseAsync(httpResponse);
        }
    }
}

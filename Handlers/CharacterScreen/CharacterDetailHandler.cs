using NetCoreServer;

namespace WorldsAdriftServer.Handlers.CharacterScreen
{
    internal class CharacterDetailHandler
    {
        internal static void SaveDetails(HttpSession session)
        {

            // IDK, cant find method in dlls, poking at it
            HttpResponse httpResponse = new HttpResponse();

            httpResponse.SetBegin((int)RequestRouterHandler.status);

            httpResponse.SetBody();
            session.SendResponseAsync(httpResponse);
        }
    }
}

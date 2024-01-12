using NetCoreServer;
using Newtonsoft.Json.Linq;

namespace WorldsAdriftServer.Utilities
{
    public static class ResponseBuilder
    {
        public static void BuildAndSendResponse(HttpSession session, int statusCode, params object[] parameters)
        {
            JObject responseObj = new JObject();

            if (parameters != null && parameters.Length % 2 == 0)
            {
                for (int i = 0; i < parameters.Length; i += 2)
                {
                    if (parameters[i] is string key && parameters[i + 1] is object value)
                    {
                        responseObj[key] = JToken.FromObject(value);
                    }
                }
            }

            HttpResponse response = new HttpResponse();
            response.SetBegin(statusCode);
            response.SetBody(responseObj.ToString());

            session.SendResponseAsync(response);
        }
    }
}

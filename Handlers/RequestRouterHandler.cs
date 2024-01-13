using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Xml.Linq;
using NetCoreServer;
using Newtonsoft.Json.Linq;
using WorldsAdriftServer.Handlers.Authentication;
using WorldsAdriftServer.Handlers.CharacterScreen;
using WorldsAdriftServer.Handlers.DataHandler;
using WorldsAdriftServer.Handlers.ServerStatus;
using WorldsAdriftServer.Helper.CharacterSelection;
using WorldsAdriftServer.Objects.CharacterSelection;

namespace WorldsAdriftServer.Handlers
{
    internal class RequestRouterHandler : HttpSession
    {
        public static HttpSession activeSession;
        public static string sessionId;
        public static string characterUID;
        public static HttpServer Server;
        public static string userId;
        public static string userName;
        public static HttpStatusCode status;
        public static List<CharacterCreationData> characterList = new List<CharacterCreationData>();
        public static CharacterListResponse CharacterResponse = new CharacterListResponse(characterList);

        public static string serverName;
        public static string dbName;
        public static string username;
        public static string password;

        public static string desiredServerName;
        public static string serverIdentity;

        public RequestRouterHandler( HttpServer server ) : base(server) 
        {
            Server = server;
        }

        protected override void OnReceived( byte[] buffer, long offset, long size )
        {
            // OnReceived isn't guaranteed to get entire request. Report what was received.
            if (buffer != null && size != 0)  { DataParser.ParseIncomingData(buffer, offset, size); }
            base.OnReceived(buffer, offset, size);
        }

        // NOTE: OnReceivedRequest is only called once a complete request has been constructed inside HttpRequest's _cache.
        protected override void OnReceivedRequest( HttpRequest request )
        {
            bool loaded = loadConfig();

            if(request != null && loaded)
            {
                if (request.Method == "POST" && request.Url == "/authenticate")
                {
                    // this is always the first request received. Get user data from server and session id
                    DataStorage.LoadUserDataFromApi();

                    // Parse the JSON body
                    JObject requestBody = JObject.Parse(request.Body);

                    // Extract userKey
                    string? userKey = requestBody["steamCredential"]?["userKey"]?.ToString()?.Trim();

                    if(userKey != null)
                    {
                        sessionId = this.Id.ToString();
                        Console.WriteLine($"[SESSION ID] {sessionId}");
                    }

                    // create session and begin storing user
                    DataStorage.StoreUserData(this, sessionId, userKey);

                    // Call the authentication handler with the extracted data
                    SteamAuthenticationHandler.HandleAuthRequest(this, request, userKey);

                    userId = userKey;
                }

                else if (request.Method == "GET" && request.Url.Contains("/characterList/") && request.Url.Contains("/steam/1234"))
                {
                    // will need further update to handle server selection including name of server
                    CharacterListHandler.HandleCharacterListRequest(this, request, "community_server", userId);
                }
                else if (request.Method == "POST" && request.Url.Contains("/reserveCharacterSlot/") && request.Url.Contains("/steam/1234"))
                {
                    CharacterSlotHandler.HandleCharacterSlotResponse(this, request, "community_server", userId);
                }
                else if (request.Method == "POST" && request.Url.Contains("player/reserveName"))
                {
                    CharacterNameHandler.SaveCharacterUid(request, userId, this);
                }
                else if (request.Method == "POST" && request.Url.Contains("character/0.3.32.0.1880/steam/1234"))
                {
                    CharacterDetailHandler.SaveDetails(this, userId, request);
                }
                else if(request.Method == "GET" && request.Url == "/deploymentStatus")
                {
                    DeploymentStatusHandler.HandleDeploymentStatusRequest(this, request, desiredServerName, "community_server", 0);
                }
                else if(request.Method == "GET" && request.Url == "/authorizeCharacter")
                {
                    CharacterAuthHandler.HandleCharacterAuth(this, request);
                }
                //else if(request.Method == "POST" && request.Url.Contains("/character/") && request.Url.Contains("/steam/1234/"))
                //{
                //    CharacterSaveHandler.HandleCharacterSave(this, request);
                //}
                else
                {
                    Console.WriteLine($"URL unhandled for request: \n\r {request}");
                }
            }
            else
            {
                Console.WriteLine("Config failed to load");
            }
        }

        protected override void OnReceivedRequestError( HttpRequest request, string error )
        {
            Console.WriteLine("Request error: " + error);
        }

        protected override void OnError( SocketError error )
        {
            Console.WriteLine("Socket error: " + error.ToString());
        }

        protected bool loadConfig()
        {
            try
            {
                // Specify the full path to the XML file
                string xmlFilePath = "./db.xml";

                // Load the XML file
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFilePath);

                // Extract database connection details
                XmlNode serverNode = xmlDoc.SelectSingleNode("/configuration/database/servername");
                XmlNode dbNode = xmlDoc.SelectSingleNode("/configuration/database/dbname");
                XmlNode userNode = xmlDoc.SelectSingleNode("/configuration/database/username");
                XmlNode passwordNode = xmlDoc.SelectSingleNode("/configuration/database/password");
                XmlNode dServerName = xmlDoc.SelectSingleNode("/configuration/database/desiredservername");
                XmlNode dServerId = xmlDoc.SelectSingleNode("/configuration/database/serveridentity");

                // Check for null before accessing node values
                if (serverNode != null && dbNode != null && userNode != null && passwordNode != null)
                {
                    serverName = serverNode.InnerText;
                    dbName = dbNode.InnerText;
                    username = userNode.InnerText;
                    password = passwordNode.InnerText;

                    desiredServerName = dServerName.InnerText;
                    serverIdentity = dServerId.InnerText;

                    Console.WriteLine($"Loaded serverName: {RequestRouterHandler.serverName}");
                    Console.WriteLine($"Loaded dbName: {RequestRouterHandler.dbName}");
                    Console.WriteLine($"Loaded username: {RequestRouterHandler.username}");
                    Console.WriteLine($"Loaded password: {RequestRouterHandler.password}");

                    return true;
                }
                else
                {
                    Console.WriteLine("One or more nodes are missing in the XML file.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}

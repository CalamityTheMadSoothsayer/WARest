using NetCoreServer;
using Npgsql;
using Newtonsoft.Json.Linq;
using WorldsAdriftServer.Helper.CharacterSelection;
using System.Drawing;
using System.Reflection;
using WorldsAdriftServer.Objects.CharacterSelection;
using WorldsAdriftServer.Objects.UnityObjects;
using Newtonsoft.Json;

namespace WorldsAdriftServer.Handlers.CharacterScreen
{
    internal class CharacterDetailHandler
    {
        internal static void SaveDetails(HttpSession session, string userKey, HttpRequest request)
        {
            JObject requestBody = JObject.Parse(request.Body);
            try
            {
                CharacterCreationData character = new CharacterCreationData
                (
                    requestBody["Id"]?.ToObject<int>() ?? 0,
                    requestBody["characterUid"]?.ToString(),
                    requestBody["Name"]?.ToString(),
                    requestBody["Server"]?.ToString(),
                    requestBody["serverIdentifier"]?.ToString(),
                    ExtractCosmetics(requestBody),
                    ExtractUniversalColors(requestBody),
                    requestBody["isMale"]?.ToObject<bool>() ?? false,
                    requestBody["seenIntro"]?.ToObject<bool>() ?? false,
                    requestBody["skippedTutorial"]?.ToObject<bool>() ?? false
                );


                using (NpgsqlConnection connection = new NpgsqlConnection(DataHandler.DataStorage.connectionString))
                {
                    connection.Open();

                    string insertCharacterSql = @"
                        INSERT INTO characterdetails
                        (id, userKey, characterUid, name, server, serverIdentifier, cosmetics, isMale, seenIntro, skippedTutorial, 
                        universalColorsHairColorR, universalColorsHairColorG, universalColorsHairColorB, universalColorsHairColorA,
                        universalColorsSkinColorR, universalColorsSkinColorG, universalColorsSkinColorB, universalColorsSkinColorA,
                        universalColorsLipColorR, universalColorsLipColorG, universalColorsLipColorB, universalColorsLipColorA)
                        VALUES
                        (@Id, @UserKey, @CharacterUid, @Name, @Server, @ServerIdentifier, @Cosmetics, @IsMale, @SeenIntro, @SkippedTutorial,
                        @HairColorR, @HairColorG, @HairColorB, @HairColorA,
                        @SkinColorR, @SkinColorG, @SkinColorB, @SkinColorA,
                        @LipColorR, @LipColorG, @LipColorB, @LipColorA)
                    ";

                    using (NpgsqlCommand insertCharacterCommand = new NpgsqlCommand(insertCharacterSql, connection))
                    {
                        // Add parameters to the command
                        insertCharacterCommand.Parameters.AddWithValue("@Id", character.Id);
                        insertCharacterCommand.Parameters.AddWithValue("@UserKey", userKey);
                        insertCharacterCommand.Parameters.AddWithValue("@CharacterUid", character.characterUid);
                        insertCharacterCommand.Parameters.AddWithValue("@Name", character.Name);
                        insertCharacterCommand.Parameters.AddWithValue("@Server", RequestRouterHandler.serverName);
                        insertCharacterCommand.Parameters.AddWithValue("@ServerIdentifier", RequestRouterHandler.desiredServerName);
                        insertCharacterCommand.Parameters.AddWithValue("@Cosmetics", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(character.Cosmetics));
                        insertCharacterCommand.Parameters.AddWithValue("@IsMale", character.isMale);
                        insertCharacterCommand.Parameters.AddWithValue("@SeenIntro", character.seenIntro);
                        insertCharacterCommand.Parameters.AddWithValue("@SkippedTutorial", character.skippedTutorial);
                        
                        insertCharacterCommand.Parameters.AddWithValue("@HairColorR", character.UniversalColors.HairColor.r);
                        insertCharacterCommand.Parameters.AddWithValue("@HairColorG", character.UniversalColors.HairColor.g);
                        insertCharacterCommand.Parameters.AddWithValue("@HairColorB", character.UniversalColors.HairColor.b);
                        insertCharacterCommand.Parameters.AddWithValue("@HairColorA", character.UniversalColors.HairColor.a);

                        insertCharacterCommand.Parameters.AddWithValue("@SkinColorR", character.UniversalColors.SkinColor.r);
                        insertCharacterCommand.Parameters.AddWithValue("@SkinColorG", character.UniversalColors.SkinColor.g);
                        insertCharacterCommand.Parameters.AddWithValue("@SkinColorB", character.UniversalColors.SkinColor.b);
                        insertCharacterCommand.Parameters.AddWithValue("@SkinColorA", character.UniversalColors.SkinColor.a);

                        insertCharacterCommand.Parameters.AddWithValue("@LipColorR", character.UniversalColors.LipColor.r);
                        insertCharacterCommand.Parameters.AddWithValue("@LipColorG", character.UniversalColors.LipColor.g);
                        insertCharacterCommand.Parameters.AddWithValue("@LipColorB", character.UniversalColors.LipColor.b);
                        insertCharacterCommand.Parameters.AddWithValue("@LipColorA", character.UniversalColors.LipColor.a);
                        // Execute the query
                        insertCharacterCommand.ExecuteNonQuery();
                    }


                }

                RequestRouterHandler.characterList[RequestRouterHandler.characterList.Count-1] = character;
                RequestRouterHandler.characterList.Add(Character.GenerateNewCharacter(RequestRouterHandler.serverName, "Blank"));

                CharacterListResponse response = new CharacterListResponse(RequestRouterHandler.characterList);

                response.characterList = RequestRouterHandler.characterList;
                response.unlockedSlots = RequestRouterHandler.characterList.Count;

                // Use ResponseBuilder to construct and send the response
                Utilities.ResponseBuilder.BuildAndSendResponse(
                    session,
                    (int)RequestRouterHandler.status,
                    "characterList", response.characterList,
                    "unlockedSlots", response.unlockedSlots,
                    "hasMainCharacter", true,
                    "havenFinished", true
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving character details: {ex.Message}");

                // Set response status to internal server error (500)
                HttpResponse errorHttpResponse = new HttpResponse();
                errorHttpResponse.SetBegin(500);
                session.SendResponseAsync(errorHttpResponse);
            }
        }

        private static CharacterUniversalColors ExtractUniversalColors(JObject requestBody)
        {
            return new CharacterUniversalColors
            {
                HairColor = ExtractColor(requestBody["UniversalColors"]?["HairColor"]),
                SkinColor = ExtractColor(requestBody["UniversalColors"]?["SkinColor"]),
                LipColor = ExtractColor(requestBody["UniversalColors"]?["LipColor"])
            };
        }

        private static UnityColor ExtractColor(JToken colorJson)
        {
            return new UnityColor
            {
                r = colorJson?["r"]?.ToObject<float>() ?? 0.0f,
                g = colorJson?["g"]?.ToObject<float>() ?? 0.0f,
                b = colorJson?["b"]?.ToObject<float>() ?? 0.0f,
                a = colorJson?["a"]?.ToObject<float>() ?? 0.0f
            };
        }

        private static Dictionary<CharacterSlotType, ItemData> ExtractCosmetics(JObject requestBody)
        {
            Dictionary<CharacterSlotType, ItemData> cosmetics = new Dictionary<CharacterSlotType, ItemData>();

            foreach (CharacterSlotType slotType in Enum.GetValues(typeof(CharacterSlotType)))
            {
                if (slotType != CharacterSlotType.None)
                {
                    string slotName = slotType.ToString();
                    var itemData = ExtractItemData(requestBody, slotName);
                    if (itemData != null)
                    {
                        cosmetics[slotType] = itemData;
                    }
                }
            }

            return cosmetics;
        }

        private static ItemData ExtractItemData(JObject requestBody, string slotName)
        {
            var itemDataJson = requestBody["Cosmetics"]?[slotName];

            if (itemDataJson != null)
            {
                return new ItemData
                (
                    itemDataJson["Id"]?.ToString(),
                    itemDataJson["Prefab"]?.ToString(),
                    ExtractColorProperties(itemDataJson["ColorProps"]),
                    itemDataJson["Health"]?.ToObject<float>() ?? 0.0f
                );
            }

            return null;
        }

        private static ColorProperties ExtractColorProperties(JToken colorPropsJson)
        {
            return new ColorProperties
            {
                PrimaryColor = ExtractColor(colorPropsJson?["PrimaryColor"]),
                SecondaryColor = ExtractColor(colorPropsJson?["SecondaryColor"]),
                TertiaryColor = ExtractColor(colorPropsJson?["TertiaryColor"]),
                SpecColor = ExtractColor(colorPropsJson?["SpecColor"])
            };
        }
    }
}
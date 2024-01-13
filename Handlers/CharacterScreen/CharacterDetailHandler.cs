using NetCoreServer;
using Npgsql;
using Newtonsoft.Json.Linq;
using WorldsAdriftServer.Helper.CharacterSelection;
using System.Drawing;
using System.Reflection;
using WorldsAdriftServer.Objects.CharacterSelection;
using WorldsAdriftServer.Objects.UnityObjects;

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

                    string insertCharacterSql = $@"
                        INSERT INTO characterdetails
                        (id, userKey, characterUid, name, server, serverIdentifier, isMale, seenIntro, skippedTutorial, 
                        universalColorsHairColorR, universalColorsHairColorG, universalColorsHairColorB, universalColorsHairColorA,
                        universalColorsSkinColorR, universalColorsSkinColorG, universalColorsSkinColorB, universalColorsSkinColorA,
                        universalColorsLipColorR, universalColorsLipColorG, universalColorsLipColorB, universalColorsLipColorA)
                        VALUES
                        ({character.Id}, '{userKey}', '{character.characterUid}', '{character.Name}', '{character.Server}', '{character.serverIdentifier}', {character.isMale}, {character.seenIntro}, {character.skippedTutorial},
                        {character.UniversalColors.HairColor.r}, {character.UniversalColors.HairColor.g}, {character.UniversalColors.HairColor.b}, {character.UniversalColors.HairColor.a},
                        {character.UniversalColors.SkinColor.r}, {character.UniversalColors.SkinColor.g}, {character.UniversalColors.SkinColor.b}, {character.UniversalColors.SkinColor.a},
                        {character.UniversalColors.LipColor.r}, {character.UniversalColors.LipColor.g}, {character.UniversalColors.LipColor.b}, {character.UniversalColors.LipColor.a})
                    ";

                    using (NpgsqlCommand insertCharacterCommand = new NpgsqlCommand(insertCharacterSql, connection))
                    {
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

//{
//    "Id": 0,
//  "characterUid": "c26a54a7-3e82-4ecb-86b6-e90efc3eb90e",
//  "Name": "warborne",
//  "Server": "serverName?",
//  "serverIdentifier": "community_server",
//  "Cosmetics": {
//        "Head": {
//            "Id": "hair_cropped",
//      "Prefab": "hair_cropped",
//      "ColorProps": {
//                "PrimaryColor": {
//                    "r": 0.698039234,
//          "g": 0.321568638,
//          "b": 0.321568638,
//          "a": 1.0
//                },
//        "SecondaryColor": {
//                    "r": 0.7411765,
//          "g": 0.7019608,
//          "b": 0.533333361,
//          "a": 1.0
//        },
//        "TertiaryColor": {
//                    "r": 0.0,
//          "g": 0.0,
//          "b": 0.0,
//          "a": 0.0
//        },
//        "SpecColor": {
//                    "r": 0.0,
//          "g": 0.0,
//          "b": 0.0,
//          "a": 0.0
//        }
//            },
//      "Health": 0.0
//        },
//    "Body": {
//            "Id": "torso_squireVariantA",
//      "Prefab": "torso_squireVariantA",
//      "ColorProps": {
//                "PrimaryColor": {
//                    "r": 0.5294118,
//          "g": 0.3372549,
//          "b": 0.282352954,
//          "a": 1.0
//                },
//        "SecondaryColor": {
//                    "r": 0.333333343,
//          "g": 0.423529416,
//          "b": 0.368627459,
//          "a": 1.0
//        },
//        "TertiaryColor": {
//                    "r": 0.490740776,
//          "g": 0.623529434,
//          "b": 0.542701542,
//          "a": 1.0
//        },
//        "SpecColor": {
//                    "r": 0.0,
//          "g": 0.0,
//          "b": 0.0,
//          "a": 0.0
//        }
//            },
//      "Health": 0.0
//    },
//    "Feet": {
//            "Id": "legs_boots",
//      "Prefab": "legs_boots",
//      "ColorProps": {
//                "PrimaryColor": {
//                    "r": 0.5137255,
//          "g": 0.380392164,
//          "b": 0.392156869,
//          "a": 1.0
//                },
//        "SecondaryColor": {
//                    "r": 0.333333343,
//          "g": 0.423529416,
//          "b": 0.368627459,
//          "a": 1.0
//        },
//        "TertiaryColor": {
//                    "r": 0.490740776,
//          "g": 0.623529434,
//          "b": 0.542701542,
//          "a": 1.0
//        },
//        "SpecColor": {
//                    "r": 0.0,
//          "g": 0.0,
//          "b": 0.0,
//          "a": 0.0
//        }
//            },
//      "Health": 0.0
//    },
//    "Face": {
//            "Id": "face_D",
//      "Prefab": "face_D",
//      "ColorProps": {
//                "PrimaryColor": {
//                    "r": 0.0,
//          "g": 0.0,
//          "b": 0.0,
//          "a": 0.0
//                },
//        "SecondaryColor": {
//                    "r": 0.0,
//          "g": 0.0,
//          "b": 0.0,
//          "a": 0.0
//        },
//        "TertiaryColor": {
//                    "r": 0.0,
//          "g": 0.0,
//          "b": 0.0,
//          "a": 0.0
//        },
//        "SpecColor": {
//                    "r": 0.0,
//          "g": 0.0,
//          "b": 0.0,
//          "a": 0.0
//        }
//            },
//      "Health": 0.0
//    },
//    "FacialHair": {
//            "Id": "facialhair_vikingbeard",
//      "Prefab": "facialhair_vikingbeard",
//      "ColorProps": {
//                "PrimaryColor": {
//                    "r": 0.0,
//          "g": 0.0,
//          "b": 0.0,
//          "a": 0.0
//                },
//        "SecondaryColor": {
//                    "r": 0.0,
//          "g": 0.0,
//          "b": 0.0,
//          "a": 0.0
//        },
//        "TertiaryColor": {
//                    "r": 0.0,
//          "g": 0.0,
//          "b": 0.0,
//          "a": 0.0
//        },
//        "SpecColor": {
//                    "r": 0.0,
//          "g": 0.0,
//          "b": 0.0,
//          "a": 0.0
//        }
//            },
//      "Health": 0.0
//    }
//    },
//  "UniversalColors": {
//        "HairColor": {
//            "r": 0.9843137,
//      "g": 0.9843137,
//      "b": 0.9843137,
//      "a": 1.0
//        },
//    "SkinColor": {
//            "r": 1.0,
//      "g": 0.8235294,
//      "b": 0.6666667,
//      "a": 1.0
//    },
//    "LipColor": {
//            "r": 1.0,
//      "g": 0.7647059,
//      "b": 0.6156863,
//      "a": 1.0
//    }
//    },
//  "isMale": true,
//  "seenIntro": true,
//  "skippedTutorial": true
//}

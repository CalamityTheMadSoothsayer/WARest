using NetCoreServer;
using Npgsql;
using Newtonsoft.Json.Linq;
using WorldsAdriftServer.Helper.CharacterSelection;
using System.Drawing;
using System.Reflection;

namespace WorldsAdriftServer.Handlers.CharacterScreen
{
    internal class CharacterDetailHandler
    {
        internal static void SaveDetails(HttpSession session, string userKey, HttpRequest request)
        {
            JObject requestBody = JObject.Parse(request.Body);
            try
            {
                var characterUid = requestBody["characterUid"]?.ToString();
                var name = requestBody["Name"]?.ToString();
                var server = requestBody["Server"]?.ToString();
                var serverIdentifier = requestBody["serverIdentifier"]?.ToString();
                var isMale = requestBody["isMale"]?.ToObject<bool>() ?? false;
                var seenIntro = requestBody["seenIntro"]?.ToObject<bool>() ?? false;
                var skippedTutorial = requestBody["skippedTutorial"]?.ToObject<bool>() ?? false;

                // Extract Cosmetics properties
                var headId = requestBody["Cosmetics"]?["Head"]?["Id"]?.ToString();
                var headPrefab = requestBody["Cosmetics"]?["Head"]?["Prefab"]?.ToString();

                // Extract ColorProps for head
                var headColorProps = requestBody["Cosmetics"]?["Head"]?["ColorProps"];
                var headColorPrimary = headColorProps?["PrimaryColor"];
                var headColorSecondary = headColorProps?["SecondaryColor"];
                var headColorTertiary = headColorProps?["TertiaryColor"];
                var headColorSpec = headColorProps?["SpecColor"];

                var headColorPrimaryR = headColorPrimary?["r"]?.ToObject<float>() ?? 0.0;
                var headColorPrimaryG = headColorPrimary?["g"]?.ToObject<float>() ?? 0.0;
                var headColorPrimaryB = headColorPrimary?["b"]?.ToObject<float>() ?? 0.0;
                var headColorPrimaryA = headColorPrimary?["a"]?.ToObject<float>() ?? 0.0;

                var headColorSecondaryR = headColorSecondary?["r"]?.ToObject<float>() ?? 0.0;
                var headColorSecondaryG = headColorSecondary?["g"]?.ToObject<float>() ?? 0.0;
                var headColorSecondaryB = headColorSecondary?["b"]?.ToObject<float>() ?? 0.0;
                var headColorSecondaryA = headColorSecondary?["a"]?.ToObject<float>() ?? 0.0;

                var headColorTertiaryR = headColorTertiary?["r"]?.ToObject<float>() ?? 0.0;
                var headColorTertiaryG = headColorTertiary?["g"]?.ToObject<float>() ?? 0.0;
                var headColorTertiaryB = headColorTertiary?["b"]?.ToObject<float>() ?? 0.0;
                var headColorTertiaryA = headColorTertiary?["a"]?.ToObject<float>() ?? 0.0;

                var headColorSpecR = headColorSpec?["r"]?.ToObject<float>() ?? 0.0;
                var headColorSpecG = headColorSpec?["g"]?.ToObject<float>() ?? 0.0;
                var headColorSpecB = headColorSpec?["b"]?.ToObject<float>() ?? 0.0;
                var headColorSpecA = headColorSpec?["a"]?.ToObject<float>() ?? 0.0;

                var headHealth = requestBody["Cosmetics"]?["Head"]?["Health"]?.ToObject<float>() ?? 0.0;

                var bodyId = requestBody["Cosmetics"]?["Body"]?["Id"]?.ToString();
                var bodyPrefab = requestBody["Cosmetics"]?["Body"]?["Prefab"]?.ToString();

                // Extract ColorProps for body
                var bodyColorProps = requestBody["Cosmetics"]?["Body"]?["ColorProps"];
                var bodyColorPrimary = bodyColorProps?["PrimaryColor"];
                var bodyColorSecondary = bodyColorProps?["SecondaryColor"];
                var bodyColorTertiary = bodyColorProps?["TertiaryColor"];
                var bodyColorSpec = bodyColorProps?["SpecColor"];

                var bodyColorPrimaryR = bodyColorPrimary?["r"]?.ToObject<float>() ?? 0.0;
                var bodyColorPrimaryG = bodyColorPrimary?["g"]?.ToObject<float>() ?? 0.0;
                var bodyColorPrimaryB = bodyColorPrimary?["b"]?.ToObject<float>() ?? 0.0;
                var bodyColorPrimaryA = bodyColorPrimary?["a"]?.ToObject<float>() ?? 0.0;

                var bodyColorSecondaryR = bodyColorSecondary?["r"]?.ToObject<float>() ?? 0.0;
                var bodyColorSecondaryG = bodyColorSecondary?["g"]?.ToObject<float>() ?? 0.0;
                var bodyColorSecondaryB = bodyColorSecondary?["b"]?.ToObject<float>() ?? 0.0;
                var bodyColorSecondaryA = bodyColorSecondary?["a"]?.ToObject<float>() ?? 0.0;

                var bodyColorTertiaryR = bodyColorTertiary?["r"]?.ToObject<float>() ?? 0.0;
                var bodyColorTertiaryG = bodyColorTertiary?["g"]?.ToObject<float>() ?? 0.0;
                var bodyColorTertiaryB = bodyColorTertiary?["b"]?.ToObject<float>() ?? 0.0;
                var bodyColorTertiaryA = bodyColorTertiary?["a"]?.ToObject<float>() ?? 0.0;

                var bodyColorSpecR = bodyColorSpec?["r"]?.ToObject<float>() ?? 0.0;
                var bodyColorSpecG = bodyColorSpec?["g"]?.ToObject<float>() ?? 0.0;
                var bodyColorSpecB = bodyColorSpec?["b"]?.ToObject<float>() ?? 0.0;
                var bodyColorSpecA = bodyColorSpec?["a"]?.ToObject<float>() ?? 0.0;

                var bodyHealth = requestBody["Cosmetics"]?["Body"]?["Health"]?.ToObject<float>() ?? 0.0;

                var feetId = requestBody["Cosmetics"]?["Feet"]?["Id"]?.ToString();
                var feetPrefab = requestBody["Cosmetics"]?["Feet"]?["Prefab"]?.ToString();

                // Extract ColorProps for feet
                var feetColorProps = requestBody["Cosmetics"]?["Feet"]?["ColorProps"];
                var feetColorPrimary = feetColorProps?["PrimaryColor"];
                var feetColorSecondary = feetColorProps?["SecondaryColor"];
                var feetColorTertiary = feetColorProps?["TertiaryColor"];
                var feetColorSpec = feetColorProps?["SpecColor"];

                var feetColorPrimaryR = feetColorPrimary?["r"]?.ToObject<float>() ?? 0.0;
                var feetColorPrimaryG = feetColorPrimary?["g"]?.ToObject<float>() ?? 0.0;
                var feetColorPrimaryB = feetColorPrimary?["b"]?.ToObject<float>() ?? 0.0;
                var feetColorPrimaryA = feetColorPrimary?["a"]?.ToObject<float>() ?? 0.0;

                var feetColorSecondaryR = feetColorSecondary?["r"]?.ToObject<float>() ?? 0.0;
                var feetColorSecondaryG = feetColorSecondary?["g"]?.ToObject<float>() ?? 0.0;
                var feetColorSecondaryB = feetColorSecondary?["b"]?.ToObject<float>() ?? 0.0;
                var feetColorSecondaryA = feetColorSecondary?["a"]?.ToObject<float>() ?? 0.0;

                var feetColorTertiaryR = feetColorTertiary?["r"]?.ToObject<float>() ?? 0.0;
                var feetColorTertiaryG = feetColorTertiary?["g"]?.ToObject<float>() ?? 0.0;
                var feetColorTertiaryB = feetColorTertiary?["b"]?.ToObject<float>() ?? 0.0;
                var feetColorTertiaryA = feetColorTertiary?["a"]?.ToObject<float>() ?? 0.0;

                var feetColorSpecR = feetColorSpec?["r"]?.ToObject<float>() ?? 0.0;
                var feetColorSpecG = feetColorSpec?["g"]?.ToObject<float>() ?? 0.0;
                var feetColorSpecB = feetColorSpec?["b"]?.ToObject<float>() ?? 0.0;
                var feetColorSpecA = feetColorSpec?["a"]?.ToObject<float>() ?? 0.0;

                var feetHealth = requestBody["Cosmetics"]?["Feet"]?["Health"]?.ToObject<float>() ?? 0.0;

                var faceId = requestBody["Cosmetics"]?["Face"]?["Id"]?.ToString();
                var facePrefab = requestBody["Cosmetics"]?["Face"]?["Prefab"]?.ToString();

                // Extract ColorProps for face
                var faceColorProps = requestBody["Cosmetics"]?["Face"]?["ColorProps"];
                var faceColorPrimary = faceColorProps?["PrimaryColor"];
                var faceColorSecondary = faceColorProps?["SecondaryColor"];
                var faceColorTertiary = faceColorProps?["TertiaryColor"];
                var faceColorSpec = faceColorProps?["SpecColor"];

                var faceColorPrimaryR = faceColorPrimary?["r"]?.ToObject<float>() ?? 0.0;
                var faceColorPrimaryG = faceColorPrimary?["g"]?.ToObject<float>() ?? 0.0;
                var faceColorPrimaryB = faceColorPrimary?["b"]?.ToObject<float>() ?? 0.0;
                var faceColorPrimaryA = faceColorPrimary?["a"]?.ToObject<float>() ?? 0.0;

                var faceColorSecondaryR = faceColorSecondary?["r"]?.ToObject<float>() ?? 0.0;
                var faceColorSecondaryG = faceColorSecondary?["g"]?.ToObject<float>() ?? 0.0;
                var faceColorSecondaryB = faceColorSecondary?["b"]?.ToObject<float>() ?? 0.0;
                var faceColorSecondaryA = faceColorSecondary?["a"]?.ToObject<float>() ?? 0.0;

                var faceColorTertiaryR = faceColorTertiary?["r"]?.ToObject<float>() ?? 0.0;
                var faceColorTertiaryG = faceColorTertiary?["g"]?.ToObject<float>() ?? 0.0;
                var faceColorTertiaryB = faceColorTertiary?["b"]?.ToObject<float>() ?? 0.0;
                var faceColorTertiaryA = faceColorTertiary?["a"]?.ToObject<float>() ?? 0.0;

                var faceColorSpecR = faceColorSpec?["r"]?.ToObject<float>() ?? 0.0;
                var faceColorSpecG = faceColorSpec?["g"]?.ToObject<float>() ?? 0.0;
                var faceColorSpecB = faceColorSpec?["b"]?.ToObject<float>() ?? 0.0;
                var faceColorSpecA = faceColorSpec?["a"]?.ToObject<float>() ?? 0.0;

                var faceHealth = requestBody["Cosmetics"]?["Face"]?["Health"]?.ToObject<float>() ?? 0.0;

                var facialHairId = requestBody["Cosmetics"]?["FacialHair"]?["Id"]?.ToString();
                var facialHairPrefab = requestBody["Cosmetics"]?["FacialHair"]?["Prefab"]?.ToString();

                // Extract ColorProps for facial hair
                var facialHairColorProps = requestBody["Cosmetics"]?["FacialHair"]?["ColorProps"];
                var facialHairColorPrimary = facialHairColorProps?["PrimaryColor"];
                var facialHairColorSecondary = facialHairColorProps?["SecondaryColor"];
                var facialHairColorTertiary = facialHairColorProps?["TertiaryColor"];
                var facialHairColorSpec = facialHairColorProps?["SpecColor"];

                var facialHairColorPrimaryR = facialHairColorPrimary?["r"]?.ToObject<float>() ?? 0.0;
                var facialHairColorPrimaryG = facialHairColorPrimary?["g"]?.ToObject<float>() ?? 0.0;
                var facialHairColorPrimaryB = facialHairColorPrimary?["b"]?.ToObject<float>() ?? 0.0;
                var facialHairColorPrimaryA = facialHairColorPrimary?["a"]?.ToObject<float>() ?? 0.0;

                var facialHairColorSecondaryR = facialHairColorSecondary?["r"]?.ToObject<float>() ?? 0.0;
                var facialHairColorSecondaryG = facialHairColorSecondary?["g"]?.ToObject<float>() ?? 0.0;
                var facialHairColorSecondaryB = facialHairColorSecondary?["b"]?.ToObject<float>() ?? 0.0;
                var facialHairColorSecondaryA = facialHairColorSecondary?["a"]?.ToObject<float>() ?? 0.0;

                var facialHairColorTertiaryR = facialHairColorTertiary?["r"]?.ToObject<float>() ?? 0.0;
                var facialHairColorTertiaryG = facialHairColorTertiary?["g"]?.ToObject<float>() ?? 0.0;
                var facialHairColorTertiaryB = facialHairColorTertiary?["b"]?.ToObject<float>() ?? 0.0;
                var facialHairColorTertiaryA = facialHairColorTertiary?["a"]?.ToObject<float>() ?? 0.0;

                var facialHairColorSpecR = facialHairColorSpec?["r"]?.ToObject<float>() ?? 0.0;
                var facialHairColorSpecG = facialHairColorSpec?["g"]?.ToObject<float>() ?? 0.0;
                var facialHairColorSpecB = facialHairColorSpec?["b"]?.ToObject<float>() ?? 0.0;
                var facialHairColorSpecA = facialHairColorSpec?["a"]?.ToObject<float>() ?? 0.0;

                var facialHairHealth = requestBody["Cosmetics"]?["FacialHair"]?["Health"]?.ToObject<float>() ?? 0.0;

                // Extract UniversalColors properties
                var hairColor = requestBody["UniversalColors"]?["HairColor"];
                var hairColorR = hairColor?["r"]?.ToObject<float>() ?? 0.0;
                var hairColorG = hairColor?["g"]?.ToObject<float>() ?? 0.0;
                var hairColorB = hairColor?["b"]?.ToObject<float>() ?? 0.0;
                var hairColorA = hairColor?["a"]?.ToObject<float>() ?? 1.0;

                var skinColor = requestBody["UniversalColors"]?["SkinColor"];
                var skinColorR = skinColor?["r"]?.ToObject<float>() ?? 0.0;
                var skinColorG = skinColor?["g"]?.ToObject<float>() ?? 0.0;
                var skinColorB = skinColor?["b"]?.ToObject<float>() ?? 0.0;
                var skinColorA = skinColor?["a"]?.ToObject<float>() ?? 1.0;

                var lipColor = requestBody["UniversalColors"]?["LipColor"];
                var lipColorR = lipColor?["r"]?.ToObject<float>() ?? 0.0;
                var lipColorG = lipColor?["g"]?.ToObject<float>() ?? 0.0;
                var lipColorB = lipColor?["b"]?.ToObject<float>() ?? 0.0;
                var lipColorA = lipColor?["a"]?.ToObject<float>() ?? 1.0;


                using (NpgsqlConnection connection = new NpgsqlConnection(DataHandler.DataStorage.connectionString))
                {
                    connection.Open();

                    string insertCharacterSql = $@"
                        INSERT INTO CharacterDetails (userKey, characterUid, name, server, serverIdentifier, isMale, seenIntro, skippedTutorial,
                        headId, headPrefab, hairColorR, headColorPrimaryR, headColorPrimaryG, headColorPrimaryB, headColorPrimaryA,
                        headColorSecondaryR, headColorSecondaryG, headColorSecondaryB, headColorSecondaryA, 
                        headColorTertiaryR, headColorTertiaryG, headColorTertiaryB, headColorTertiaryA,
                        headColorSpecR, headColorSpecG, headColorSpecB, headColorSpecA,
                        headHealth, bodyId, bodyPrefab, bodyColorPrimaryR, bodyColorPrimaryG, bodyColorPrimaryB, bodyColorPrimaryA,
                        bodyColorSecondaryR, bodyColorSecondaryG, bodyColorSecondaryB, bodyColorSecondaryA, 
                        bodyColorTertiaryR, bodyColorTertiaryG, bodyColorTertiaryB, bodyColorTertiaryA,
                        bodyColorSpecR, bodyColorSpecG, bodyColorSpecB, bodyColorSpecA,
                        bodyHealth, feetId, feetPrefab, feetColorPrimaryR, feetColorPrimaryG, feetColorPrimaryB, feetColorPrimaryA,
                        feetColorSecondaryR, feetColorSecondaryG, feetColorSecondaryB, feetColorSecondaryA, 
                        feetColorTertiaryR, feetColorTertiaryG, feetColorTertiaryB, feetColorTertiaryA,
                        feetColorSpecR, feetColorSpecG, feetColorSpecB, feetColorSpecA,
                        feetHealth, faceId, facePrefab, faceColorPrimaryR, faceColorPrimaryG, faceColorPrimaryB, faceColorPrimaryA,
                        faceColorSecondaryR, faceColorSecondaryG, faceColorSecondaryB, faceColorSecondaryA, 
                        faceColorTertiaryR, faceColorTertiaryG, faceColorTertiaryB, faceColorTertiaryA,
                        faceColorSpecR, faceColorSpecG, faceColorSpecB, faceColorSpecA,
                        faceHealth, facialHairId, facialHairPrefab, facialHairColorPrimaryR, facialHairColorPrimaryG, facialHairColorPrimaryB, facialHairColorPrimaryA,
                        facialHairColorSecondaryR, facialHairColorSecondaryG, facialHairColorSecondaryB, facialHairColorSecondaryA, 
                        facialHairColorTertiaryR, facialHairColorTertiaryG, facialHairColorTertiaryB, facialHairColorTertiaryA,
                        facialHairColorSpecR, facialHairColorSpecG, facialHairColorSpecB, facialHairColorSpecA,
                        facialHairHealth, hairColorR AS UniversalHairColorR, hairColorG AS UniversalHairColorG, hairColorB AS UniversalHairColorB, hairColorA AS UniversalHairColorA,
                        skinColorR AS UniversalSkinColorR, skinColorG AS UniversalSkinColorG, skinColorB AS UniversalSkinColorB, skinColorA AS UniversalSkinColorA,
                        lipColorR AS UniversalLipColorR, lipColorG AS UniversalLipColorG, lipColorB AS UniversalLipColorB, lipColorA AS UniversalLipColorA)
                        VALUES ('{userKey}', '{characterUid}', '{name}', '{server}', '{serverIdentifier}', '{isMale}', '{seenIntro}', '{skippedTutorial}',
                        '{headId}', '{headPrefab}', '{hairColorR}', 
                        '{headColorPrimaryR}', '{headColorPrimaryG}', '{headColorPrimaryB}', '{headColorPrimaryA}',
                        '{headColorSecondaryR}', '{headColorSecondaryG}', '{headColorSecondaryB}', '{headColorSecondaryA}', 
                        '{headColorTertiaryR}', '{headColorTertiaryG}', '{headColorTertiaryB}', '{headColorTertiaryA}',
                        '{headColorSpecR}', '{headColorSpecG}', '{headColorSpecB}', '{headColorSpecA}',
                        '{headHealth}', '{bodyId}', '{bodyPrefab}', '{bodyColorPrimaryR}', '{bodyColorPrimaryG}', '{bodyColorPrimaryB}', '{bodyColorPrimaryA}',
                        '{bodyColorSecondaryR}', '{bodyColorSecondaryG}', '{bodyColorSecondaryB}', '{bodyColorSecondaryA}', 
                        '{bodyColorTertiaryR}', '{bodyColorTertiaryG}', '{bodyColorTertiaryB}', '{bodyColorTertiaryA}',
                        '{bodyColorSpecR}', '{bodyColorSpecG}', '{bodyColorSpecB}', '{bodyColorSpecA}',
                        '{bodyHealth}', '{feetId}', '{feetPrefab}', '{feetColorPrimaryR}', '{feetColorPrimaryG}', '{feetColorPrimaryB}', '{feetColorPrimaryA}',
                        '{feetColorSecondaryR}', '{feetColorSecondaryG}', '{feetColorSecondaryB}', '{feetColorSecondaryA}', 
                        '{feetColorTertiaryR}', '{feetColorTertiaryG}', '{feetColorTertiaryB}', '{feetColorTertiaryA}',
                        '{feetColorSpecR}', '{feetColorSpecG}', '{feetColorSpecB}', '{feetColorSpecA}',
                        '{feetHealth}', '{faceId}', '{facePrefab}', '{faceColorPrimaryR}', '{faceColorPrimaryG}', '{faceColorPrimaryB}', '{faceColorPrimaryA}',
                        '{faceColorSecondaryR}', '{faceColorSecondaryG}', '{faceColorSecondaryB}', '{faceColorSecondaryA}', 
                        '{faceColorTertiaryR}', '{faceColorTertiaryG}', '{faceColorTertiaryB}', '{faceColorTertiaryA}',
                        '{faceColorSpecR}', '{faceColorSpecG}', '{faceColorSpecB}', '{faceColorSpecA}',
                        '{faceHealth}', '{facialHairId}', '{facialHairPrefab}', '{facialHairColorPrimaryR}', '{facialHairColorPrimaryG}', '{facialHairColorPrimaryB}', '{facialHairColorPrimaryA}',
                        '{facialHairColorSecondaryR}', '{facialHairColorSecondaryG}', '{facialHairColorSecondaryB}', '{facialHairColorSecondaryA}', 
                        '{facialHairColorTertiaryR}', '{facialHairColorTertiaryG}', '{facialHairColorTertiaryB}', '{facialHairColorTertiaryA}',
                        '{facialHairColorSpecR}', '{facialHairColorSpecG}', '{facialHairColorSpecB}', '{facialHairColorSpecA}',
                        '{facialHairHealth}', '{hairColorR}', '{hairColorG}', '{hairColorB}', '{hairColorA}', '{skinColorR}', '{skinColorG}', '{skinColorB}', '{skinColorA}', '{lipColorR}', '{lipColorG}', '{lipColorB}', '{lipColorA}')
                    ";


                    using (NpgsqlCommand insertCharacterCommand = new NpgsqlCommand(insertCharacterSql, connection))
                    {
                        insertCharacterCommand.ExecuteNonQuery();
                    }
                }

                // Set response status to success (200)
                HttpResponse httpResponse = new HttpResponse();
                httpResponse.SetBegin(200);
                session.SendResponseAsync(httpResponse);
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

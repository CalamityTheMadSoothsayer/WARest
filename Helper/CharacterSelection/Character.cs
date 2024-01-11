using WorldsAdriftServer.Handlers;
using WorldsAdriftServer.Objects.CharacterSelection;

namespace WorldsAdriftServer.Helper.CharacterSelection
{
    internal static class Character
    {
        internal static CharacterCreationData GenerateRandomCharacter(string serverIdentifier, string characterName)
        {
            Dictionary<CharacterSlotType, ItemData> cosmetics = new Dictionary<CharacterSlotType, ItemData>();
            CharacterUniversalColors colors = new CharacterUniversalColors();

            // stopping random character creation, interfering with character comparison

            colors.SkinColor = CustomisationSettings.skinColors[0];
            colors.LipColor = CustomisationSettings.lipColors[0];
            colors.HairColor = CustomisationSettings.hairColors[0];

            cosmetics.Add(CharacterSlotType.Head, new ItemData(
                                                                "1",
                                                                CustomisationSettings.starterHeadItems.Keys.ToList<string>()[0],
                                                                new ColorProperties(
                                                                                    CustomisationSettings.clothingColors[0],
                                                                                    CustomisationSettings.clothingColors[0]),
                                                                100f));
            cosmetics.Add(CharacterSlotType.Body, new ItemData(
                                                                "2",
                                                                CustomisationSettings.starterTorsoItems.Keys.ToList<string>()[0],
                                                                new ColorProperties(
                                                                                    CustomisationSettings.clothingColors[0],
                                                                                    CustomisationSettings.clothingColors[0]),
                                                                100f));
            cosmetics.Add(CharacterSlotType.Feet, new ItemData(
                                                                "3",
                                                                CustomisationSettings.starterLegItems.Keys.ToList<string>()[0],
                                                                new ColorProperties(
                                                                                    CustomisationSettings.clothingColors[0],
                                                                                    CustomisationSettings.clothingColors[0]),
                                                                100f));
            cosmetics.Add(CharacterSlotType.Face, new ItemData(
                                                                "4",
                                                                CustomisationSettings.starterFaceItems.Keys.ToList<string>()[0],
                                                                default(ColorProperties),
                                                                100f));
            cosmetics.Add(CharacterSlotType.FacialHair, new ItemData(
                                                                "5",
                                                                CustomisationSettings.starterFacialHairItems.Keys.ToList<string>()[0],
                                                                default(ColorProperties),
                                                                100f));

            return new CharacterCreationData(1, RequestRouterHandler.sessionId.ToString(), characterName, "serverName?", serverIdentifier, cosmetics, colors, true, false, false);
        }
        /*
         * generates a character without cosmetics which reflects as an empty slot in the character select screen.
         * characterName is not applied, instead the SteamAuthRequestToken.screenName is used.
         */
        internal static CharacterCreationData GenerateNewCharacter(string serverIdentifier, string characterName)
        {
            Dictionary<CharacterSlotType, ItemData> cosmetics = new Dictionary<CharacterSlotType, ItemData>();
            CharacterUniversalColors colors = new CharacterUniversalColors();

            Random r = new Random();
            int num = r.Next(0, CustomisationSettings.skinColors.Length);

            colors.SkinColor = CustomisationSettings.skinColors[num];
            colors.LipColor = CustomisationSettings.lipColors[num];
            colors.HairColor = CustomisationSettings.hairColors[r.Next(0, CustomisationSettings.hairColors.Length)];

            // generate character uid
            RequestRouterHandler.characterUID = Guid.NewGuid().ToString();
            // find a way to let server owners set servername
            return new CharacterCreationData(1, RequestRouterHandler.characterUID, characterName, "serverName?", serverIdentifier, null, colors, true, true, true);
        }
    }
}

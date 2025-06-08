using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Terraria;
using Terraria.ModLoader.Config;

namespace CharCreationPreset;

public static class Utils
{
    static readonly int[] _validClothStyles = [
        0,
        2,
        1,
        3,
        8,
        4,
        6,
        5,
        7,
        9
    ];

    public static string PlayerSetAsJson(Player player)
    {
        int[] itemVanityTypes = new int[8];
        for (int n = 0; n < 8; n++)
            itemVanityTypes[n] = player.armor[10 + n].vanity ? player.armor[10 + n].type : player.armor[n].vanity ? player.armor[n].type : 0;
        return JsonConvert.SerializeObject(new Dictionary<string, object> {
            { "version", 1 },
            { "hairStyle", player.hair },
            { "clothingStyle", player.skinVariant },
            { "hairColor", GetHexText(player.hairColor) },
            { "eyeColor", GetHexText(player.eyeColor) },
            { "skinColor", GetHexText(player.skinColor) },
            { "shirtColor", GetHexText(player.shirtColor) },
            { "underShirtColor", GetHexText(player.underShirtColor) },
            { "pantsColor", GetHexText(player.pantsColor) },
            { "shoeColor", GetHexText(player.shoeColor) },
            { "vanityHead",new ItemDefinition(itemVanityTypes[0]) },
            { "vanityBody",new ItemDefinition(itemVanityTypes[1]) },
            { "vanityLeg",new ItemDefinition(itemVanityTypes[2]) },
            { "vanityAcc1",new ItemDefinition(itemVanityTypes[3]) },
            { "vanityAcc2",new ItemDefinition(itemVanityTypes[4]) },
            { "vanityAcc3",new ItemDefinition(itemVanityTypes[5]) },
            { "vanityAcc4",new ItemDefinition(itemVanityTypes[6]) },
            { "vanityAcc5",new ItemDefinition(itemVanityTypes[7]) },
            { "vanityPet",new ItemDefinition(player.miscEquips[0].type) },
            { "vanityHairDye",player.hairDye },
            { "dyeHead",new ItemDefinition(player.dye[0].type)},
            { "dyeBody",new ItemDefinition(player.dye[1].type)},
            { "dyeLeg",new ItemDefinition(player.dye[2].type)},
            { "dyeAcc1",new ItemDefinition(player.dye[3].type)},
            { "dyeAcc2",new ItemDefinition(player.dye[4].type)},
            { "dyeAcc3",new ItemDefinition(player.dye[5].type)},
            { "dyeAcc4",new ItemDefinition(player.dye[6].type)},
            { "dyeAcc5",new ItemDefinition(player.dye[7].type)},
            { "dyePet",new ItemDefinition(player.miscDyes[0].type)}
        }, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
            Formatting = Formatting.Indented
        });
    }

    public static void ApplyPlayerSetFromJson(string content, Player player)
    {
        int num = content.IndexOf('{');
        if (num == -1)
            return;

        content = content.Substring(num);
        int num2 = content.LastIndexOf('}');
        if (num2 == -1)
            return;

        content = content[..(num2 + 1)];
        Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>((string)content);
        if (dictionary == null)
            return;

        Dictionary<string, object> dictionary2 = [];
        foreach (KeyValuePair<string, object> item in dictionary)
        {
            dictionary2[item.Key.ToLower()] = item.Value;
        }

        if (dictionary2.TryGetValue("version", out var value2))
            _ = (long)value2;

        if (dictionary2.TryGetValue("hairstyle", out value2))
        {
            int num3 = (int)(long)value2;
            if (Main.Hairstyles.AvailableHairstyles.Contains(num3))
                player.hair = num3;
        }

        if (dictionary2.TryGetValue("clothingstyle", out value2))
        {
            int num4 = (int)(long)value2;
            if (_validClothStyles.Contains(num4))
                player.skinVariant = num4;
        }

        if (dictionary2.TryGetValue("haircolor", out value2) && GetHexColor((string)value2, out var hsl))
            player.hairColor = ScaledHslToRgb(hsl);

        if (dictionary2.TryGetValue("eyecolor", out value2) && GetHexColor((string)value2, out hsl))
            player.eyeColor = ScaledHslToRgb(hsl);

        if (dictionary2.TryGetValue("skincolor", out value2) && GetHexColor((string)value2, out hsl))
            player.skinColor = ScaledHslToRgb(hsl);

        if (dictionary2.TryGetValue("shirtcolor", out value2) && GetHexColor((string)value2, out hsl))
            player.shirtColor = ScaledHslToRgb(hsl);

        if (dictionary2.TryGetValue("undershirtcolor", out value2) && GetHexColor((string)value2, out hsl))
            player.underShirtColor = ScaledHslToRgb(hsl);

        if (dictionary2.TryGetValue("pantscolor", out value2) && GetHexColor((string)value2, out hsl))
            player.pantsColor = ScaledHslToRgb(hsl);

        if (dictionary2.TryGetValue("shoecolor", out value2) && GetHexColor((string)value2, out hsl))
            player.shoeColor = ScaledHslToRgb(hsl);

        ItemDefinition definition = new();

        if (dictionary2.TryGetValue("vanityhead", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.armor[10] = new Item(definition.Type);
        }
        else
            player.armor[10] = new Item();

        if (dictionary2.TryGetValue("vanitybody", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.armor[11] = new Item(definition.Type);
        }
        else
            player.armor[11] = new Item();

        if (dictionary2.TryGetValue("vanityleg", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.armor[12] = new Item(definition.Type);
        }
        else
            player.armor[12] = new Item();

        if (dictionary2.TryGetValue("vanityacc1", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.armor[13] = new Item(definition.Type);
        }
        else
            player.armor[13] = new Item();

        if (dictionary2.TryGetValue("vanityacc2", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.armor[14] = new Item(definition.Type);
        }
        else
            player.armor[14] = new Item();

        if (dictionary2.TryGetValue("vanityacc3", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.armor[15] = new Item(definition.Type);
        }
        else
            player.armor[15] = new Item();

        if (dictionary2.TryGetValue("vanityacc4", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.armor[16] = new Item(definition.Type);
        }
        else
            player.armor[16] = new Item();

        if (dictionary2.TryGetValue("vanityacc5", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.armor[17] = new Item(definition.Type);
        }
        else
            player.armor[17] = new Item();

        if (dictionary2.TryGetValue("vanitypet", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.miscEquips[0] = new Item(definition.Type);
        }
        else
            player.miscEquips[0] = new Item();

        if (dictionary2.TryGetValue("vanityhairdye", out value2))
            player.hairDye = Convert.ToInt32(value2);
        else
            player.hairDye = 0;

        if (dictionary2.TryGetValue("dyehead", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.dye[0] = new Item(definition.Type);
        }
        else
            player.dye[0] = new Item();
        if (dictionary2.TryGetValue("dyebody", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.dye[1] = new Item(definition.Type);
        }
        else
            player.dye[1] = new Item();
        if (dictionary2.TryGetValue("dyeleg", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.dye[2] = new Item(definition.Type);
        }
        else
            player.dye[2] = new Item();
        if (dictionary2.TryGetValue("dyeacc1", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.dye[3] = new Item(definition.Type);
        }
        else
            player.dye[3] = new Item();
        if (dictionary2.TryGetValue("dyeacc2", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.dye[4] = new Item(definition.Type);
        }
        else
            player.dye[4] = new Item();
        if (dictionary2.TryGetValue("dyeacc3", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.dye[5] = new Item(definition.Type);
        }
        else
            player.dye[5] = new Item();
        if (dictionary2.TryGetValue("dyeacc4", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.dye[6] = new Item(definition.Type);
        }
        else
            player.dye[6] = new Item();
        if (dictionary2.TryGetValue("dyeacc5", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.dye[7] = new Item(definition.Type);
        }
        else
            player.dye[7] = new Item();
        if (dictionary2.TryGetValue("dyepet", out value2))
        {
            JsonConvert.PopulateObject(value2.ToString(), definition);
            player.miscDyes[0] = new Item(definition.Type);
        }
        else
            player.miscDyes[0] = new Item();
    }

    static string GetHexText(Color pendingColor) => "#" + pendingColor.Hex3().ToUpper();

    static Color ScaledHslToRgb(Vector3 hsl) => ScaledHslToRgb(hsl.X, hsl.Y, hsl.Z);

    static Color ScaledHslToRgb(float hue, float saturation, float luminosity) => Main.hslToRgb(hue, saturation, luminosity * 0.85f + 0.15f);

    static bool GetHexColor(string hexString, out Vector3 hsl)
    {
        if (hexString.StartsWith("#"))
            hexString = hexString.Substring(1);

        if (hexString.Length <= 6 && uint.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var result))
        {
            uint b = result & 0xFFu;
            uint g = (result >> 8) & 0xFFu;
            uint r = (result >> 16) & 0xFFu;
            hsl = RgbToScaledHsl(new Color((int)r, (int)g, (int)b));
            return true;
        }

        hsl = Vector3.Zero;
        return false;
    }

    static Vector3 RgbToScaledHsl(Color color)
    {
        Vector3 value = Main.rgbToHsl(color);
        value.Z = (value.Z - 0.15f) / 0.85f;
        return Vector3.Clamp(value, Vector3.Zero, Vector3.One);
    }
}
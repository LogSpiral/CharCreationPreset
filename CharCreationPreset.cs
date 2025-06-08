using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.Config;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader.UI;
using ReLogic.OS;
using Microsoft.Xna.Framework.Graphics;
using static System.Net.Mime.MediaTypeNames;
using Terraria.GameInput;

namespace CharCreationPreset;

// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
public class CharCreationPreset : Mod
{
    public override void Load()
    {
        On_UICharacterCreation.BuildPage += BuildPresetList;
        On_UICharacterCreation.MakeCharPreview += MakePetPreview;
        On_UICharacterListItem.AddTmlElements += AddCopyButton;
        base.Load();
    }

    private static void AddCopyButton(On_UICharacterListItem.orig_AddTmlElements orig, UICharacterListItem self, Terraria.IO.PlayerFileData data)
    {
        orig.Invoke(self, data);

        UIImageButton uIImageButton5 = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Copy"))
        {
            VAlign = 1f,
            HAlign = 1f,
            Left = StyleDimension.FromPixels(-40)
        };

        uIImageButton5.OnLeftClick += delegate
        {
            string text = Utils.PlayerSetAsJson(self._playerPanel._player);
            PlayerInput.PrettyPrintProfiles(ref text);
            Platform.Get<IClipboard>().Value = text;
            SoundEngine.PlaySound(SoundID.Research);
            SoundEngine.PlaySound(SoundID.ResearchComplete);
        };

        uIImageButton5.OnMouseOver += delegate
        {
            self._deleteButtonLabel.SetText(Language.GetTextValue("Mods.CharCreationPreset.UI.CopyHint"));
            self._deleteButtonLabel.Left.Set(-75, 0);

        };
        uIImageButton5.OnMouseOut += delegate
        {
            self._deleteButtonLabel.SetText("");
            self._deleteButtonLabel.Left.Set(-30, 0);

        };
        self.Append(uIImageButton5);
    }

    private void MakePetPreview(On_UICharacterCreation.orig_MakeCharPreview orig, UICharacterCreation self, UIPanel container)
    {
        UICharacter element = currentPreviewChar = new UICharacter(self._player, animated: true, hasBackPanel: false, 1.5f)
        {
            Width = StyleDimension.FromPixels(80f),
            Height = StyleDimension.FromPixelsAndPercent(80f, 0f),
            Top = StyleDimension.FromPixelsAndPercent(-70, 0f),
            VAlign = 0f,
            HAlign = 0.5f
        };

        container.Append(element);
    }

    static void AddSaveButton(UICharacterCreation self)
    {
        UITextPanel<LocalizedText> SavePresetButton = new(Language.GetText("Mods.CharCreationPreset.UI.SavePreset"), 0.7f, true)
        {
            Width = StyleDimension.FromPixels(240f),
            Height = StyleDimension.FromPixels(60),
            Top = StyleDimension.FromPixels(575f),
            Left = StyleDimension.FromPixels(-130),
            HAlign = 0.5f,
            VAlign = 0f
        };
        SavePresetButton.OnMouseOut += FadedMouseOut;
        SavePresetButton.OnMouseOver += FadedMouseOver;
        SavePresetButton.OnLeftClick += delegate
        {
            SoundEngine.PlaySound(SoundID.ResearchComplete);
            SoundEngine.PlaySound(SoundID.Research);

            var content = Utils.PlayerSetAsJson(self._player);
            var mainPath = Path.Combine(Main.SavePath, "Mods", nameof(CharCreationPreset));
            Directory.CreateDirectory(mainPath);
            int counter = 1;
            while (System.IO.File.Exists(Path.Combine(mainPath, $"Preset_{counter}.json")))
                counter++;
            var filePath = Path.Combine(mainPath, $"Preset_{counter}.json");
            System.IO.File.WriteAllText(filePath, content);

            SetupPresetGrid(self);
            SetupScrollBar(self, PresetScrollbar, PresetGrid);
        };
        self.Append(SavePresetButton);


        UITextPanel<LocalizedText> OpenFolderButton = new(Language.GetText("Mods.CharCreationPreset.UI.OpenFolder"), 0.7f, true)
        {
            Width = StyleDimension.FromPixels(240f),
            Height = StyleDimension.FromPixels(60),
            Top = StyleDimension.FromPixels(575f),
            Left = StyleDimension.FromPixels(130),
            HAlign = 0.5f,
            VAlign = 0f
        };
        OpenFolderButton.OnMouseOut += FadedMouseOut;
        OpenFolderButton.OnMouseOver += FadedMouseOver;
        OpenFolderButton.OnLeftClick += delegate
        {
            Terraria.Utils.OpenFolder(Path.Combine(Main.SavePath, "Mods", nameof(CharCreationPreset)));
        };
        self.Append(OpenFolderButton);
    }
    static void AddPresetGrid(UICharacterCreation self)
    {
        var bounds = Main.instance.Window.ClientBounds;
        float width = Math.Min(400, bounds.Width * .5f - 340f);
        float offsetX = -(400 - width) * .5f;
        float height = Math.Min(600, bounds.Height - 180);
        
        UIPanel presetContainer = new()
        {
            Width = StyleDimension.FromPixels(width),
            Height = StyleDimension.FromPixels(height),
            Top = StyleDimension.FromPixels(150f),
            Left = StyleDimension.FromPixels(480f  + offsetX),
            HAlign = .5f,
            VAlign = 0f,
        };
        self.Append(presetContainer);

        PresetGrid = new UIGrid()
        {
            Width = StyleDimension.FromPercent(1),
            Height = StyleDimension.FromPercent(1)
        };
        presetContainer.Append(PresetGrid);
        SetupPresetGrid(self);
        PresetGrid.OnUpdate += delegate
        {
            var bar = PresetGrid._scrollbar;
            var top = PresetGrid._innerList.Top;
            PresetGrid.Recalculate();
        };
        PresetScrollbar = new UIScrollbar()
        {
            Width = StyleDimension.FromPixels(32),
            Height = StyleDimension.FromPixels(height),
            Top = StyleDimension.FromPixels(150),
            Left = StyleDimension.FromPixels(700 +offsetX * 2),
            HAlign = .5f,
            VAlign = 0f,

        };
        SetupScrollBar(self, PresetScrollbar, PresetGrid);
        PresetGrid.SetScrollbar(PresetScrollbar);


        PresetSearchBar = new UIFocusInputTextField(Language.GetTextValue("Mods.CharCreationPreset.UI.SearchHint"))
        {
            Width = StyleDimension.FromPixels(400f),
            Height = StyleDimension.FromPixels(40),
            Top = StyleDimension.FromPixels(120f),
            Left = StyleDimension.FromPixels(480f),
            HAlign = .5f,
            VAlign = 0f,
        };
        PresetSearchBar.OnTextChange += delegate
        {
            pendingUpdatePreset = true;
        };
        self.Append(PresetSearchBar);
    }
    static void AddVanityGrid(UICharacterCreation self)
    {
        var bounds = Main.instance.Window.ClientBounds;
        float width = Math.Min(400, bounds.Width * .5f - 340f);
        float offsetX = (400f - width) * .5f;
        float height = Math.Min(600, bounds.Height - 180);

        UIPanel vanityContainer = new()
        {
            Width = StyleDimension.FromPixels(width),
            Height = StyleDimension.FromPixels(height),
            Top = StyleDimension.FromPixels(150f),
            Left = StyleDimension.FromPixels(-480f + offsetX),
            HAlign = .5f,
            VAlign = 0f,
        };
        self.Append(vanityContainer);

        VanityGrid = new UIGrid()
        {
            Width = StyleDimension.FromPercent(1),
            Height = StyleDimension.FromPercent(1)
        };
        vanityContainer.Append(VanityGrid);
        //SetupVanityGrid(self);
        VanityGrid.OnUpdate += delegate
        {
            var bar = VanityGrid._scrollbar;
            var top = VanityGrid._innerList.Top;
            VanityGrid.Recalculate();
        };
        VanityScrollbar = new UIScrollbar()
        {
            Width = StyleDimension.FromPixels(32),
            Height = StyleDimension.FromPixels(height),
            Top = StyleDimension.FromPixels(150),
            Left = StyleDimension.FromPixels(-700 + offsetX * 2),
            HAlign = .5f,
            VAlign = 0f,

        };
        SetupScrollBar(self, VanityScrollbar, VanityGrid);
        VanityGrid.SetScrollbar(VanityScrollbar);

        VanitySearchBar = new UIFocusInputTextField(Language.GetTextValue("Mods.CharCreationPreset.UI.SearchHint"))
        {
            Width = StyleDimension.FromPixels(400f),
            Height = StyleDimension.FromPixels(40),
            Top = StyleDimension.FromPixels(120f),
            Left = StyleDimension.FromPixels(-480f + offsetX * 2),
            HAlign = .5f,
            VAlign = 0f,
        };
        VanitySearchBar.OnTextChange += delegate
        {
            pendingUpdateVanity = true;
        };
        self.Append(VanitySearchBar);
    }
    static void AddItemSlots(UICharacterCreation self)
    {
        var bounds = Main.instance.Window.ClientBounds;
        float h = Math.Min(125f, bounds.Height - 670f);
        float factor = h / 125f;
        float height = MathHelper.Lerp(40,125, factor);
        float yOffset = MathHelper.Lerp(-20,0,factor);
        UIPanel basePanel = ItemPanel = new()
        {
            Width = StyleDimension.FromPixels(500f),
            Height = StyleDimension.FromPixels(height),
            Top = StyleDimension.FromPixels(650f +yOffset),
            HAlign = 0.5f,
            VAlign = 0f
        };
        self.Append(basePanel);
        SetupItemPanel(self);
    }
    static void PastePreset(UICharacterCreation self)
    {
        string value = Platform.Get<IClipboard>().Value;
        Utils.ApplyPlayerSetFromJson(value, self._player);
        currentPreviewChar.PreparePetProjectiles();
        SetupItemPanel(self);
    }
    static void HookUpdate(UICharacterCreation self)
    {
        self.OnUpdate += delegate
        {
            if (pendingUpdatePreset)
            {
                pendingUpdatePreset = false;
                SetupPresetGrid(self);
                SetupScrollBar(self, PresetScrollbar, PresetGrid);
            }
            if (pendingUpdateVanity)
            {
                pendingUpdateVanity = false;
                SetupVanityGrid(self);
                SetupScrollBar(self, VanityScrollbar, VanityGrid);
            }
        };
    }
    static void BuildPresetList(On_UICharacterCreation.orig_BuildPage orig, UICharacterCreation self)
    {
        orig.Invoke(self);
        AddSaveButton(self);
        AddPresetGrid(self);
        AddVanityGrid(self);
        AddItemSlots(self);
        PastePreset(self);
        HookUpdate(self);

    }
    static UICharacter currentPreviewChar;

    static UIGrid PresetGrid;
    static UIScrollbar PresetScrollbar;
    static UIFocusInputTextField PresetSearchBar;

    static UIGrid VanityGrid;
    static UIScrollbar VanityScrollbar;
    static UIFocusInputTextField VanitySearchBar;

    static UIPanel ItemPanel;
    static ItemDefinitionOptionElement CurrentOption
    {
        get;
        set
        {
            if (field != null)
                field.BackgroundTexture = TextureAssets.InventoryBack9;
            field = value;
            value.BackgroundTexture = TextureAssets.InventoryBack10;
        }
    }

    static VanityState VanityState;
    static VanityState DyeState;

    static bool pendingUpdatePreset;
    static bool pendingUpdateVanity;

    static void SetupPresetGrid(UICharacterCreation UICharacterCreation)
    {
        if (PresetGrid == null) return;

        PresetGrid.Clear();
        string searchText = PresetSearchBar?.CurrentString;
        bool skip = string.IsNullOrEmpty(searchText);
        var mainPath = Path.Combine(Main.SavePath, "Mods", nameof(CharCreationPreset));
        Directory.CreateDirectory(mainPath);
        var files = Directory.GetFiles(mainPath);
        int counter = 0;
        foreach (var file in files)
        {
            counter++;
            if (Path.GetExtension(file) != ".json") continue;
            if (!skip && !Path.GetFileNameWithoutExtension(file).ToLower().Contains(searchText.ToLower())) continue;
            var characterBox = new UICharacterBox(file);

            characterBox.OnLeftClick += delegate
            {
                var content = System.IO.File.ReadAllText(file);
                Utils.ApplyPlayerSetFromJson(content, UICharacterCreation._player);
                currentPreviewChar.PreparePetProjectiles();
                SetupItemPanel(UICharacterCreation);
                SoundEngine.PlaySound(SoundID.Research);
                SoundEngine.PlaySound(SoundID.ResearchComplete);
            };
            characterBox.OnMouseOut += FadedMouseOut;
            characterBox.OnMouseOver += FadedMouseOver;
            PresetGrid.Add(characterBox);
            var lst = PresetGrid._items;
        }
        UICharacterCreation.Recalculate();
    }

    static void SetupVanityGrid(UICharacterCreation UICharacterCreation)
    {
        if (VanityGrid == null) return;

        VanityGrid.Clear();
        string searchText = VanitySearchBar?.CurrentString;
        bool skip = string.IsNullOrEmpty(searchText);
        Item dummyItem = new Item();
        for (int n = 0; n < ItemLoader.ItemCount; n++)
        {
            dummyItem.SetDefaults(n);
            if (dummyItem.type != n) continue;
            if (!VanityCheck(VanityState, dummyItem))
                continue;
            if (!skip && !dummyItem.Name.ToLower().Contains(searchText.ToLower()))
                continue;
            ItemDefinition itemDefinition = new(n);
            ItemDefinitionOptionElement itemDefinitionOption = new(itemDefinition, .8f);
            VanityGrid.Add(itemDefinitionOption);
            Item clone = dummyItem.Clone();
            itemDefinitionOption.OnLeftClick += delegate
            {
                VanitySet(VanityState, UICharacterCreation._player, clone, DyeState);
                SoundEngine.PlaySound(SoundID.Research);
                SoundEngine.PlaySound(SoundID.ResearchComplete);
                CurrentOption?.SetItem(new ItemDefinition(clone.type));
            };
        }
        UICharacterCreation.Recalculate();
    }

    static void SetupScrollBar(UICharacterCreation UICharacterCreation, UIScrollbar bar, UIGrid grid, bool resetViewPosition = true)
    {
        float height = grid.GetInnerDimensions().Height;
        float totalHeight = grid.GetTotalHeight();
        bar.SetView(height, totalHeight);
        if (resetViewPosition)
            bar.ViewPosition = 0f;

        bar.Remove();
        if (height < totalHeight)
            UICharacterCreation.Append(bar);

    }

    static void SetupItemPanel(UICharacterCreation UICharacterCreation)
    {
        var bounds = Main.instance.Window.ClientBounds;
        float h = Math.Min(125f, bounds.Height - 670f);
        float factor = h / 125f;
        ItemPanel.RemoveAllChildren();
        var player = UICharacterCreation._player;
        for (int n = 0; n < 10; n++)
        {
            ItemDefinition itemDefinition = new(n switch
            {
                < 8 => player.armor[n + 10].type,
                8 => player.miscEquips[0].type,
                9 or _ => GameShaders.Hair._shaderLookupDictionary.FirstOrDefault(pair => pair.Value == player.hairDye).Key
            });
            ItemDefinitionOptionElement itemDefinitionOptionElement = new(itemDefinition)
            {
                Left = StyleDimension.FromPixels(n * 45),
                Top = StyleDimension.FromPixels(MathHelper.Lerp(-40,10,factor))
            };
            int k = n;
            itemDefinitionOptionElement.OnUpdate += delegate
            {
                if (itemDefinitionOptionElement.Item.type != ItemID.None) return;
                itemDefinitionOptionElement.SetItem(new ItemDefinition(k switch
                {
                    0 => ModContent.ItemType<VanityHeadDummy>(),
                    1 => ModContent.ItemType<VanityBodyDummy>(),
                    2 => ModContent.ItemType<VanityLegDummy>(),
                    8 => ModContent.ItemType<VanityPetDummy>(),
                    9 => ModContent.ItemType<VanityHairDyeDummy>(),
                    _ => ModContent.ItemType<VanityAccDummy>()
                }));
            };
            ItemPanel.Append(itemDefinitionOptionElement);

            itemDefinitionOptionElement.OnLeftClick += delegate
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                VanityState = (VanityState)k;
                CurrentOption = itemDefinitionOptionElement;
                SetupVanityGrid(UICharacterCreation);
                SetupScrollBar(UICharacterCreation, VanityScrollbar, VanityGrid);
            };

            if (n == 0)
            {
                CurrentOption = itemDefinitionOptionElement;
                SetupVanityGrid(UICharacterCreation);
                SetupScrollBar(UICharacterCreation, VanityScrollbar, VanityGrid);
            }
        }

        for (int n = 0; n < 9; n++)
        {
            ItemDefinition itemDefinition = new(n switch
            {
                < 8 => player.dye[n].type,
                8 or _ => player.miscDyes[0].type
            });
            ItemDefinitionOptionElement itemDefinitionOptionElement = new(itemDefinition)
            {
                Left = StyleDimension.FromPixels(n * 45),
                Top = StyleDimension.FromPixels(MathHelper.Lerp(-10, 65, factor))
            };
            ItemPanel.Append(itemDefinitionOptionElement);
            int k = n;
            itemDefinitionOptionElement.OnUpdate += delegate
            {
                if (itemDefinitionOptionElement.Item.type != ItemID.None) return;
                itemDefinitionOptionElement.SetItem(new ItemDefinition(ModContent.ItemType<VanityDyeDummy>()));
            };
            itemDefinitionOptionElement.OnLeftClick += delegate
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                VanityState = VanityState.Dye;
                DyeState = (VanityState)k;
                CurrentOption = itemDefinitionOptionElement;
                SetupVanityGrid(UICharacterCreation);
                SetupScrollBar(UICharacterCreation, VanityScrollbar, VanityGrid);
            };
        }
    }

    static bool VanityCheck(VanityState state, Item targetItem)
    {
        if (targetItem.type == ItemID.None)
            return true;
        bool isPet = Main.vanityPet[targetItem.buffType];
        bool isHairDye = targetItem.hairDye != -1;
        bool isDye = targetItem.dye != 0;
        if (!targetItem.vanity && !isPet && !isHairDye && !isDye) return false;
        return state switch
        {
            VanityState.Head => targetItem.headSlot != -1,
            VanityState.Body => targetItem.bodySlot != -1,
            VanityState.Leg => targetItem.legSlot != -1,
            VanityState.Pet => isPet,
            VanityState.Dye => isDye,
            VanityState.HairDye => isHairDye,
            _ => targetItem.accessory
        };
    }

    static void VanitySet(VanityState state, Player player, Item targetItem, VanityState dyeState)
    {
        switch (state)
        {
            case VanityState.Pet:
                player.miscEquips[0] = targetItem;
                currentPreviewChar.PreparePetProjectiles();
                break;
            case VanityState.HairDye:
                player.hairDye = targetItem.hairDye;
                break;
            case VanityState.Dye:
                switch (dyeState)
                {
                    case VanityState.Pet:
                        player.miscDyes[0] = targetItem;
                        break;
                    case VanityState.HairDye:
                        break;
                    default:
                        player.dye[(int)dyeState] = targetItem;
                        break;
                }
                break;
            default:
                player.armor[10 + (int)state] = targetItem;
                break;
        }
    }

    static void FadedMouseOver(UIMouseEvent evt, UIElement listeningElement)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        if (evt.Target is not UIPanel panel) return;
        panel.BackgroundColor = new Color(73, 94, 171);
        panel.BorderColor = Colors.FancyUIFatButtonMouseOver;
    }

    static void FadedMouseOut(UIMouseEvent evt, UIElement listeningElement)
    {
        if (evt.Target is not UIPanel panel) return;
        panel.BackgroundColor = new Color(63, 82, 151) * 0.8f;
        panel.BorderColor = Color.Black;
    }
}





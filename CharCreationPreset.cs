using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReLogic.OS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.GameInput;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using System.Reflection;
using MonoMod.Cil;
using System.Collections;

namespace CharCreationPreset;

// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
public class CharCreationPreset : Mod
{
    public override object Call(params object[] args)
    {
        if (args[0] is not string methodName)
            throw new ArgumentException("The first argument should be the name of the method");
        switch (methodName)
        {
            case nameof(MakePetPreview):
                {
                    if (args[1] is not UIElement container || args[2] is not Player player)
                        throw new ArgumentException("Type mismatch");
                    return MakePetPreviewInternal(container, player);
                }

            case nameof(BuildPresetList):
                {
                    if (args[1] is not UIElement uiCharacterCreation || args[2] is not Player player)
                        throw new ArgumentException("Type mismatch");
                    BuildPresetListInternal(uiCharacterCreation, player);
                    return false;
                }

            case "GetPresetUIs":
                return (_presetGrid, _presetScrollbar, _presetSearchBar);
            case "GetVanityUIs":
                return (_vanityGrid, _vanityScrollbar, _vanitySearchBar);
            case "GetItemPanel":
                return _itemPanel;

            case "RegisterSetPlayerCallback":
                if (args[1] is not string callBackName || args[2] is not Action<UICharacter, Player> callBack)
                    throw new ArgumentException("Type mismatch");
                _playerSetCallBacks[callBackName] = callBack;
                return true;

            case "RegisterSaveExtraData":
                if (args[1] is not string callBackNameSave || args[2] is not Action<Dictionary<string, object>, Player> callBackSave)
                    throw new ArgumentException("Type mismatch");
                PlayerSaveCallBacks[callBackNameSave] = callBackSave;
                return true;


            case "RegisterReadExtraData":
                if (args[1] is not string callBackNameRead || args[2] is not Action<Dictionary<string, object>, Player> callBackRead)
                    throw new ArgumentException("Type mismatch");
                PlayerReadCallBacks[callBackNameRead] = callBackRead;
                return true;
        }
        return false;
    }
    private static readonly Dictionary<string, Action<UICharacter, Player>> _playerSetCallBacks = [];
    public static readonly Dictionary<string, Action<Dictionary<string, object>, Player>> PlayerSaveCallBacks = [];
    public static readonly Dictionary<string, Action<Dictionary<string, object>, Player>> PlayerReadCallBacks = [];

    static void MrPlagueRacesSupport()
    {
        if (!ModContent.TryFind<ModSystem>("MrPlagueRaces", "UIRedirectionSystem", out var system)) 
            return;
        var assembly = system.GetType().Assembly;

        var MrPlagueUICharacterCreationType = (from type in assembly.GetTypes() where type.Name == "MrPlagueUICharacterCreation" select type).First();
        var _playerInfo = MrPlagueUICharacterCreationType.GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic);
        MonoModHooks.Modify(MrPlagueUICharacterCreationType.GetMethod("MakeCharPreview", BindingFlags.NonPublic | BindingFlags.Instance), il => 
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(i => i.MatchRet())) return;
            int index = cursor.Index;
            cursor.Index = 0;
            cursor.RemoveRange(index);
            cursor.EmitLdarg1();
            cursor.EmitLdarg0();
            cursor.EmitLdfld(_playerInfo);
            cursor.EmitDelegate<Action<UIElement,Player>>((element,player) => MakePetPreviewInternal(element,player));
        });

        MonoModHooks.Modify(system.GetType().GetMethod("InterceptCharacterCreationMenu", BindingFlags.NonPublic | BindingFlags.Instance), il =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(i => i.MatchLdcI4(888))) 
                return;
            cursor.Index += 6;
            cursor.EmitDelegate<Action>(() =>
            {
                BuildPresetListInternal(Main.MenuUI.CurrentState, Main.PendingPlayer);
            });
        });



        PlayerSaveCallBacks["MrPlagueRacesSave"] = (dict, player) =>
        {
            dynamic mplr = player.GetModPlayer(ModContent.Find<ModPlayer>("MrPlagueRaces/MrPlagueRacesPlayer"));
            dict["MrPlagueRaces/Race"] = mplr.race.FullName;
            Color detailColor = mplr.detailColor;
            dict["MrPlagueRaces/detailColorR"] = detailColor.R;
            dict["MrPlagueRaces/detailColorG"] = detailColor.G;
            dict["MrPlagueRaces/detailColorB"] = detailColor.B;
        };
        var raceLoaderType = (from type in assembly.GetTypes()where type.Name == "RaceLoader" select type).First();
        var methods = (from method in raceLoaderType.GetMethods() where method.Name == "TryGetRace" && method.GetParameters()[0].ParameterType == typeof(string) select method).First();
        var mplrInstance = ModContent.Find<ModPlayer>("MrPlagueRaces/MrPlagueRacesPlayer");
        var raceFldInfo = mplrInstance.GetType().GetField("race", BindingFlags.Instance | BindingFlags.Public);
        PlayerReadCallBacks["MrPlagueRacesSave"] = (dict, player) =>
        {
            dynamic mplr = player.GetModPlayer(mplrInstance);
            if (dict.TryGetValue("MrPlagueRaces/Race", out var nameObject) && nameObject is string raceName)
            {
                object[] paraList = [raceName, null];
                var flag = methods?.Invoke(null, paraList);
                var type = paraList[1].GetType();
                if (flag is true)
                    raceFldInfo.SetValue(mplr, paraList[1]);

            }
            Color detailColor = Color.White;

            if (dict.TryGetValue("MrPlagueRaces/detailColorR", out object obj))
                detailColor.R = (byte)(long)obj;

            if (dict.TryGetValue("MrPlagueRaces/detailColorG", out obj))
                detailColor.G = (byte)(long)obj;

            if (dict.TryGetValue("MrPlagueRaces/detailColorB", out obj))
                detailColor.B = (byte)(long)obj;

            mplr.detailColor = detailColor;
        };
    }

    public override void Load()
    {
        On_UICharacterCreation.BuildPage += BuildPresetList;
        On_UICharacterCreation.MakeCharPreview += MakePetPreview;
        On_UICharacterListItem.AddTmlElements += AddCopyButton;
        SetupFavoritePresets();

        base.Load();
    }
    public override void PostSetupContent()
    {
        MrPlagueRacesSupport();
        base.PostSetupContent();
    }
    public static void UpdatePets(UICharacter character)
    {
        if (character._player.miscEquips[0].type != ItemID.None)
            character.PreparePetProjectiles();
        else
            character._petProjectiles = UICharacter.NoPets;
    }

    private static void BuildPresetListInternal(UIElement self, Player player)
    {
        AddSaveButton(self, player);
        AddPresetGrid(self, player);
        AddVanityGrid(self, player);
        AddItemSlots(self, player);
        PastePreset(self, player);
        HookUpdate(self, player);
    }

    private static UICharacter MakePetPreviewInternal(UIElement container, Player player)
    {
        var element = _currentPreviewChar = new UICharacter(player, animated: true, hasBackPanel: false, 1.5f)
        {
            Width = StyleDimension.FromPixels(80f),
            Height = StyleDimension.FromPixelsAndPercent(80f, 0f),
            Top = StyleDimension.FromPixelsAndPercent(-70, 0f),
            VAlign = 0f,
            HAlign = 0.5f
        };

        container.Append(element);

        return element;
    }

    private static void AddCopyButton(On_UICharacterListItem.orig_AddTmlElements orig, UICharacterListItem self, Terraria.IO.PlayerFileData data)
    {
        orig.Invoke(self, data);

        var uIImageButton5 = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Copy"))
        {
            VAlign = 1f,
            HAlign = 1f,
            Left = StyleDimension.FromPixels(-40)
        };

        uIImageButton5.OnLeftClick += delegate
        {
            var text = Utils.PlayerSetAsJson(self._playerPanel._player);
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

    private static void MakePetPreview(On_UICharacterCreation.orig_MakeCharPreview orig, UICharacterCreation self, UIPanel container)
        => MakePetPreviewInternal(container, self._player);

    private static void AddSaveButton(UIElement self, Player player)
    {
        UITextPanel<LocalizedText> savePresetButton = new(Language.GetText("Mods.CharCreationPreset.UI.SavePreset"), 0.7f, true)
        {
            Width = StyleDimension.FromPixels(240f),
            Height = StyleDimension.FromPixels(60),
            Top = StyleDimension.FromPixels(575f),
            Left = StyleDimension.FromPixels(-130),
            HAlign = 0.5f,
            VAlign = 0f
        };
        savePresetButton.OnMouseOut += FadedMouseOut;
        savePresetButton.OnMouseOver += FadedMouseOver;
        savePresetButton.OnLeftClick += delegate
        {
            SoundEngine.PlaySound(SoundID.ResearchComplete);
            SoundEngine.PlaySound(SoundID.Research);

            var content = Utils.PlayerSetAsJson(player);
            var mainPath = Path.Combine(Main.SavePath, "Mods", nameof(CharCreationPreset));
            Directory.CreateDirectory(mainPath);
            var counter = 1;
            while (System.IO.File.Exists(Path.Combine(mainPath, $"Preset_{counter}.json")))
                counter++;
            var filePath = Path.Combine(mainPath, $"Preset_{counter}.json");
            System.IO.File.WriteAllText(filePath, content);

            SetupPresetGrid(self, player);
            SetupScrollBar(self, _presetScrollbar, _presetGrid);
        };
        self.Append(savePresetButton);


        UITextPanel<LocalizedText> openFolderButton = new(Language.GetText("Mods.CharCreationPreset.UI.OpenFolder"), 0.7f, true)
        {
            Width = StyleDimension.FromPixels(240f),
            Height = StyleDimension.FromPixels(60),
            Top = StyleDimension.FromPixels(575f),
            Left = StyleDimension.FromPixels(130),
            HAlign = 0.5f,
            VAlign = 0f
        };
        openFolderButton.OnMouseOut += FadedMouseOut;
        openFolderButton.OnMouseOver += FadedMouseOver;
        openFolderButton.OnLeftClick += delegate
        {
            Terraria.Utils.OpenFolder(Path.Combine(Main.SavePath, "Mods", nameof(CharCreationPreset)));
        };
        self.Append(openFolderButton);
    }

    private static void AddPresetGrid(UIElement self, Player player)
    {
        //var bounds = Main.instance.Window.ClientBounds;
        var width = Math.Min(400, Main.screenWidth * .5f - 340f);
        var offsetX = -(400 - width) * .5f;
        float height = Math.Min(600, Main.screenHeight - 180);

        UIPanel presetContainer = new()
        {
            Width = StyleDimension.FromPixels(width),
            Height = StyleDimension.FromPixels(height),
            Top = StyleDimension.FromPixels(150f),
            Left = StyleDimension.FromPixels(480f + offsetX),
            HAlign = .5f,
            VAlign = 0f,
        };
        self.Append(presetContainer);

        _presetGrid = new UIGrid()
        {
            Width = StyleDimension.FromPercent(1),
            Height = StyleDimension.FromPercent(1)
        };
        presetContainer.Append(_presetGrid);
        SetupPresetGrid(self, player);
        _presetGrid.OnUpdate += delegate
        {
            var bar = _presetGrid._scrollbar;
            var top = _presetGrid._innerList.Top;
            _presetGrid.Recalculate();
        };
        _presetScrollbar = new UIScrollbar()
        {
            Width = StyleDimension.FromPixels(32),
            Height = StyleDimension.FromPixels(height),
            Top = StyleDimension.FromPixels(150),
            Left = StyleDimension.FromPixels(700 + offsetX * 2),
            HAlign = .5f,
            VAlign = 0f,

        };
        SetupScrollBar(self, _presetScrollbar, _presetGrid);
        _presetGrid.SetScrollbar(_presetScrollbar);


        _presetSearchBar = new UIFocusInputTextField(Language.GetTextValue("Mods.CharCreationPreset.UI.SearchHint"))
        {
            Width = StyleDimension.FromPixels(400f),
            Height = StyleDimension.FromPixels(40),
            Top = StyleDimension.FromPixels(120f),
            Left = StyleDimension.FromPixels(480f),
            HAlign = .5f,
            VAlign = 0f,
        };
        _presetSearchBar.OnTextChange += delegate
        {
            PendingUpdatePreset = true;
        };
        self.Append(_presetSearchBar);
    }

    private static void AddVanityGrid(UIElement self, Player player)
    {
        //var bounds = Main.instance.Window.ClientBounds;
        var width = Math.Min(400, Main.screenWidth * .5f - 340f);
        var offsetX = (400f - width) * .5f;
        float height = Math.Min(600, Main.screenHeight - 180);

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

        _vanityGrid = new UIGrid()
        {
            Width = StyleDimension.FromPercent(1),
            Height = StyleDimension.FromPercent(1)
        };
        vanityContainer.Append(_vanityGrid);
        //SetupVanityGrid(self);
        _vanityGrid.OnUpdate += delegate
        {
            var bar = _vanityGrid._scrollbar;
            var top = _vanityGrid._innerList.Top;
            _vanityGrid.Recalculate();
        };
        _vanityScrollbar = new UIScrollbar()
        {
            Width = StyleDimension.FromPixels(32),
            Height = StyleDimension.FromPixels(height),
            Top = StyleDimension.FromPixels(150),
            Left = StyleDimension.FromPixels(-700 + offsetX * 2),
            HAlign = .5f,
            VAlign = 0f,

        };
        SetupScrollBar(self, _vanityScrollbar, _vanityGrid);
        _vanityGrid.SetScrollbar(_vanityScrollbar);

        _vanitySearchBar = new UIFocusInputTextField(Language.GetTextValue("Mods.CharCreationPreset.UI.SearchHint"))
        {
            Width = StyleDimension.FromPixels(400f),
            Height = StyleDimension.FromPixels(40),
            Top = StyleDimension.FromPixels(120f),
            Left = StyleDimension.FromPixels(-480f + offsetX * 2),
            HAlign = .5f,
            VAlign = 0f,
        };
        _vanitySearchBar.OnTextChange += delegate
        {
            _pendingUpdateVanity = true;
        };
        self.Append(_vanitySearchBar);
    }

    private static void AddItemSlots(UIElement self, Player player)
    {
        //var bounds = Main.instance.Window.ClientBounds;
        var h = Math.Min(125f, Main.screenHeight - 670f);
        var factor = h / 125f;
        var height = MathHelper.Lerp(40, 125, factor);
        var yOffset = MathHelper.Lerp(-20, 0, factor);
        var basePanel = _itemPanel = new()
        {
            Width = StyleDimension.FromPixels(500f),
            Height = StyleDimension.FromPixels(height),
            Top = StyleDimension.FromPixels(650f + yOffset),
            HAlign = 0.5f,
            VAlign = 0f
        };
        self.Append(basePanel);
        SetupItemPanel(self, player);
    }

    private static void PastePreset(UIElement self, Player player)
    {
        var value = Platform.Get<IClipboard>().Value;
        try
        {
            Utils.ApplyPlayerSetFromJson(value, player);
            UpdatePets(_currentPreviewChar);
            foreach (var pair in _playerSetCallBacks)
                pair.Value?.Invoke(_currentPreviewChar, player);
            SetupItemPanel(self, player);
        }
        catch { }

    }

    private static void HookUpdate(UIElement self, Player player)
    {
        self.OnUpdate += delegate
        {
            if (PendingUpdatePreset)
            {
                PendingUpdatePreset = false;
                SetupPresetGrid(self, player);
                SetupScrollBar(self, _presetScrollbar, _presetGrid);
            }
            if (_pendingUpdateVanity)
            {
                _pendingUpdateVanity = false;
                SetupVanityGrid(self, player);
                SetupScrollBar(self, _vanityScrollbar, _vanityGrid);
            }
        };
    }

    private static void BuildPresetList(On_UICharacterCreation.orig_BuildPage orig, UICharacterCreation self)
    {
        orig.Invoke(self);

        BuildPresetListInternal(self, self._player);
    }

    private static UICharacter _currentPreviewChar;

    private static UIGrid _presetGrid;
    private static UIScrollbar _presetScrollbar;
    private static UIFocusInputTextField _presetSearchBar;

    private static UIGrid _vanityGrid;
    private static UIScrollbar _vanityScrollbar;
    private static UIFocusInputTextField _vanitySearchBar;

    private static UIPanel _itemPanel;

    private static ItemDefinitionOptionElement CurrentOption
    {
        get;
        set
        {
            field?.BackgroundTexture = TextureAssets.InventoryBack9;
            field = value;
            value.BackgroundTexture = TextureAssets.InventoryBack10;
        }
    }

    private static VanityState _vanityState;
    private static VanityState _dyeState;

    public static bool PendingUpdatePreset { get; set; }
    private static bool _pendingUpdateVanity;

    public static readonly HashSet<string> FavoritePresets = [];

    private static void SetupFavoritePresets()
    {
        FavoritePresets.Clear();
        var mainPath = Path.Combine(Main.SavePath, "Mods", nameof(CharCreationPreset));
        var fileFullName = Path.Combine(mainPath, "FavoritePresets.txt");
        if (!System.IO.File.Exists(fileFullName)) return;
        var list = System.IO.File.ReadAllLines(fileFullName);
        foreach (var item in list)
            FavoritePresets.Add(item);
    }
    public static void SaveFavoritePresets()
    {
        var mainPath = Path.Combine(Main.SavePath, "Mods", nameof(CharCreationPreset));
        System.IO.File.WriteAllLines(Path.Combine(mainPath, "FavoritePresets.txt"), FavoritePresets);
    }

    private static void SetupPresetGrid(UIElement uiCharacterCreation, Player player)
    {
        if (_presetGrid == null) return;

        _presetGrid.Clear();
        var searchText = _presetSearchBar?.CurrentString;
        var skip = string.IsNullOrEmpty(searchText);
        var mainPath = Path.Combine(Main.SavePath, "Mods", nameof(CharCreationPreset));
        Directory.CreateDirectory(mainPath);
        var files = Directory.GetFiles(mainPath);
        foreach (var file in files)
        {
            if (Path.GetExtension(file) != ".json") continue;
            if (!skip && !Path.GetFileNameWithoutExtension(file).ToLower().Contains(searchText.ToLower())) continue;
            var characterBox = new UICharacterBox(file);

            characterBox.OnLeftClick += (evt, elem) =>
            {
                if (evt.Target is UIFocusInputTextField || evt.Target is UIImageButton) return;
                var content = System.IO.File.ReadAllText(file);
                Utils.ApplyPlayerSetFromJson(content, player);
                UpdatePets(_currentPreviewChar);
                foreach (var pair in _playerSetCallBacks)
                    pair.Value?.Invoke(_currentPreviewChar, player);
                SetupItemPanel(uiCharacterCreation, player);
                SoundEngine.PlaySound(SoundID.Research);
                SoundEngine.PlaySound(SoundID.ResearchComplete);
            };
            _presetGrid.Add(characterBox);
            var lst = _presetGrid._items;
        }
        uiCharacterCreation.Recalculate();
    }

    private static void SetupVanityGrid(UIElement uiCharacterCreation, Player player)
    {
        if (_vanityGrid == null) return;

        _vanityGrid.Clear();
        var searchText = _vanitySearchBar?.CurrentString;
        var skip = string.IsNullOrEmpty(searchText);
        var dummyItem = new Item();
        for (var n = 0; n < ItemLoader.ItemCount; n++)
        {
            dummyItem.SetDefaults(n);
            if (dummyItem.type != n) continue;
            if (!VanityCheck(_vanityState, dummyItem))
                continue;
            if (!skip && !dummyItem.Name.ToLower().Contains(searchText.ToLower()))
                continue;
            ItemDefinition itemDefinition = new(n);
            ItemDefinitionOptionElement itemDefinitionOption = new(itemDefinition, .8f);
            _vanityGrid.Add(itemDefinitionOption);
            var clone = dummyItem.Clone();
            itemDefinitionOption.OnLeftClick += delegate
            {
                VanitySet(_vanityState, player, clone, _dyeState);
                SoundEngine.PlaySound(SoundID.Research);
                SoundEngine.PlaySound(SoundID.ResearchComplete);
                CurrentOption?.SetItem(new ItemDefinition(clone.type));
            };
        }
        uiCharacterCreation.Recalculate();
    }

    private static void SetupScrollBar(UIElement uiCharacterCreation, UIScrollbar bar, UIGrid grid, bool resetViewPosition = false)
    {
        var height = grid.GetInnerDimensions().Height;
        var totalHeight = grid.GetTotalHeight();
        bar.SetView(height, totalHeight);
        if (resetViewPosition)
            bar.ViewPosition = 0f;

        bar.Remove();
        if (height < totalHeight)
            uiCharacterCreation.Append(bar);

    }

    private static void SetupItemPanel(UIElement uiCharacterCreation, Player player)
    {
        var bounds = Main.instance.Window.ClientBounds;
        var h = Math.Min(125f, bounds.Height - 670f);
        var factor = h / 125f;
        _itemPanel.RemoveAllChildren();
        for (var n = 0; n < 10; n++)
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
                Top = StyleDimension.FromPixels(MathHelper.Lerp(-40, 10, factor))
            };
            var k = n;
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
            _itemPanel.Append(itemDefinitionOptionElement);

            itemDefinitionOptionElement.OnLeftClick += delegate
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                _vanityState = (VanityState)k;
                CurrentOption = itemDefinitionOptionElement;
                SetupVanityGrid(uiCharacterCreation, player);
                SetupScrollBar(uiCharacterCreation, _vanityScrollbar, _vanityGrid);
            };

            if (n == 0)
            {
                CurrentOption = itemDefinitionOptionElement;
                SetupVanityGrid(uiCharacterCreation, player);
                SetupScrollBar(uiCharacterCreation, _vanityScrollbar, _vanityGrid);
            }
        }

        for (var n = 0; n < 9; n++)
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
            _itemPanel.Append(itemDefinitionOptionElement);
            var k = n;
            itemDefinitionOptionElement.OnUpdate += delegate
            {
                if (itemDefinitionOptionElement.Item.type != ItemID.None) return;
                itemDefinitionOptionElement.SetItem(new ItemDefinition(ModContent.ItemType<VanityDyeDummy>()));
            };
            itemDefinitionOptionElement.OnLeftClick += delegate
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                _vanityState = VanityState.Dye;
                _dyeState = (VanityState)k;
                CurrentOption = itemDefinitionOptionElement;
                SetupVanityGrid(uiCharacterCreation, player);
                SetupScrollBar(uiCharacterCreation, _vanityScrollbar, _vanityGrid);
            };
        }
    }

    private static bool VanityCheck(VanityState state, Item targetItem)
    {
        if (targetItem.type == ItemID.None)
            return true;
        var isPet = Main.vanityPet[targetItem.buffType];
        var isHairDye = targetItem.hairDye != -1;
        var isDye = targetItem.dye != 0;
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

    private static void VanitySet(VanityState state, Player player, Item targetItem, VanityState dyeState)
    {
        switch (state)
        {
            case VanityState.Pet:
                player.miscEquips[0] = targetItem;
                UpdatePets(_currentPreviewChar);
                break;
            case VanityState.HairDye:
                player.hairDye = targetItem.hairDye;
                if (player.hairDye == -1)
                    player.hairDye = 0;
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

    private static void FadedMouseOver(UIMouseEvent evt, UIElement listeningElement)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        if (evt.Target is not UIPanel panel) return;
        panel.BackgroundColor = new Color(73, 94, 171);
        panel.BorderColor = Colors.FancyUIFatButtonMouseOver;
    }

    private static void FadedMouseOut(UIMouseEvent evt, UIElement listeningElement)
    {
        if (evt.Target is not UIPanel panel) return;
        panel.BackgroundColor = new Color(63, 82, 151) * 0.8f;
        panel.BorderColor = Color.Black;
    }
}





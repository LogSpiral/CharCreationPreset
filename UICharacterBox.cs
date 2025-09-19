using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace CharCreationPreset;
public partial class UICharacterBox : UIElement
{
    private readonly UIPanel _container;
    private readonly UIFocusInputTextField _fileName;
    private readonly UIImageButton _deleteButton;

    private string _oldName;

    private bool _isFavorite;
    private Asset<Texture2D> DeleteIcon { get; } = Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete");
    public UICharacterBox(string filePath)
    {

        Width = StyleDimension.FromPercent(.239f);
        Height = StyleDimension.FromPixels(100);
        var content = File.ReadAllText(filePath);

        var player = new Player();

        Utils.ApplyPlayerSetFromJson(content, player);

        _container = new UIPanel()
        {
            Width = StyleDimension.FromPercent(1),
            Height = StyleDimension.FromPercent(1),
            BackgroundColor = Color.Black * .25f,
            BorderColor = default
        };
        Append(_container);
        _oldName = Path.GetFileNameWithoutExtension(filePath);
        _fileName = new UIFocusInputTextField("")
        {
            Width = StyleDimension.FromPercent(1),
            Height = StyleDimension.FromPixels(30),
            Top = StyleDimension.FromPixels(-5),
            HAlign = .5f
        };
        _isFavorite = CharCreationPreset.FavoritePresets.Contains(_oldName);
        _fileName.SetText(_oldName);
        var path = Path.GetDirectoryName(filePath) ?? "";
        _fileName.OnUnfocus += delegate
        {
            if (_fileName.CurrentString.Length == 0 || Path.GetInvalidFileNameChars().Intersect(_fileName.CurrentString).Any())
            {
                _fileName.CurrentString = _oldName;
                SoundEngine.PlaySound(SoundID.Item62);
                return;
            }


            if (_oldName == _fileName.CurrentString) return;
            if (Directory.GetFiles(path).Any(fileName => Path.GetFileNameWithoutExtension(fileName) == _fileName.CurrentString))
            {
                _fileName.CurrentString = _oldName;
                SoundEngine.PlaySound(SoundID.Item62);
                return;
            }
            SoundEngine.PlaySound(SoundID.ResearchComplete);
            File.Move(Path.Combine(path, _oldName + ".json"), Path.Combine(path, _fileName.CurrentString + ".json"));
            if (CharCreationPreset.FavoritePresets.Remove(_oldName))
                CharCreationPreset.FavoritePresets.Add(_fileName.CurrentString);
            CharCreationPreset.SaveFavoritePresets();
            _oldName = _fileName.CurrentString;
            CharCreationPreset.PendingUpdatePreset = true;
        };
        _container.Append(_fileName);

        var character = new UICharacter(player, true, false, 1f);
        CharCreationPreset.UpdatePets(character);
        _container.Append(character);
        character.VAlign = 1f;


        _deleteButton = new UIImageButton(DeleteIcon)
        {
            HAlign = 1,
            VAlign = 1
        };
        _deleteButton.OnLeftClick += delegate
        {
            SoundEngine.PlaySound(SoundID.Dig);
            File.Delete(Path.Combine(path, _fileName.CurrentString + ".json"));
            CharCreationPreset.PendingUpdatePreset = true;

            if (CharCreationPreset.FavoritePresets.Remove(_fileName.CurrentString))
                CharCreationPreset.SaveFavoritePresets();

        };

        _container.Append(_deleteButton);



        var separator = new UIHorizontalSeparator
        {
            HAlign = .5f,
            Top = StyleDimension.FromPixels(20),
            Width = StyleDimension.FromPercent(1f),
            Color = Color.Lerp(Color.White, new Color(63, 65, 151, 255), 0.85f) * 0.9f
        };

        _container.Append(separator);
    }

    public override string ToString() => _fileName?.CurrentString;

    public override int CompareTo(object obj)
    {
        if (obj is not UICharacterBox otherBox || otherBox.ToString() is null) return 0;

        var name = ToString();
        if (!(_isFavorite ^ otherBox._isFavorite)) goto label;
        else if (_isFavorite && !otherBox._isFavorite) return -1;
        else return 1;
        label:
        return ExtractNumber(name) > ExtractNumber(otherBox.ToString()) ? 1 : -1;

        static int ExtractNumber(string path)
        {
            if (path == null) return 0;
            var fileName = Path.GetFileNameWithoutExtension(path);
            var match = MatchNumber().Match(fileName);
            return match.Success ? int.Parse(match.Value) : 0;
        }
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex MatchNumber();

    public override void MouseOut(UIMouseEvent evt)
    {
        _container.BackgroundColor = Color.Black * .25f;
        _container.BorderColor = default;
        base.MouseOut(evt);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        _container.BackgroundColor = Color.Black * .1f;
        _container.BorderColor = Colors.FancyUIFatButtonMouseOver;
        base.MouseOver(evt);
    }

    public override void MiddleClick(UIMouseEvent evt)
    {
        if (evt.Target != _fileName && evt.Target != _deleteButton)
        {
            if (!CharCreationPreset.FavoritePresets.Add(_oldName))
            {
                CharCreationPreset.FavoritePresets.Remove(_oldName);
                _isFavorite = false;
            }
            else
            {
                _isFavorite = true;
            }
            CharCreationPreset.PendingUpdatePreset = true;
            SoundEngine.PlaySound(SoundID.ResearchComplete);
            CharCreationPreset.SaveFavoritePresets();
        }
        base.MiddleClick(evt);
    }
    public override void DrawChildren(SpriteBatch spriteBatch)
    {
        base.DrawChildren(spriteBatch);
        if (!_isFavorite) return;
        var dimension = GetDimensions();
        spriteBatch.Draw(TextureAssets.Cursors[3].Value, dimension.Position(), Color.White * .5f);
    }
}


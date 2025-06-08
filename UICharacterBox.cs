using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    readonly UIPanel _container;
    readonly UICharacter _character;
    readonly UIFocusInputTextField _fileName;
    readonly UIImageButton _deleteButton;
    readonly Player _player;

    string _oldName;

    bool isFavorited;

    public UICharacterBox(string filePath)
    {
        Width = StyleDimension.FromPixels(90);
        Height = StyleDimension.FromPixels(100);

        var content = File.ReadAllText(filePath);

        _player = new Player();

        Utils.ApplyPlayerSetFromJson(content, _player);

        _container = new UIPanel()
        {
            Width = StyleDimension.FromPercent(1),
            Height = StyleDimension.FromPercent(1)
        };
        Append(_container);
        _oldName = Path.GetFileNameWithoutExtension(filePath);
        _fileName = new UIFocusInputTextField("")
        {
            Width = StyleDimension.FromPercent(1),
            Height = StyleDimension.FromPixels(30),
            Top = StyleDimension.FromPixels(-5),
            HAlign = .5f,

        };
        isFavorited = CharCreationPreset.FavoritedPresets.Contains(_oldName);
        _fileName.SetText(_oldName);
        string path = Path.GetDirectoryName(filePath);
        _fileName.OnUnfocus += delegate
        {
            if (_fileName.CurrentString.Length == 0 || Path.GetInvalidFileNameChars().Intersect(_fileName.CurrentString).Any())
            {
                _fileName.CurrentString = _oldName;
                SoundEngine.PlaySound(SoundID.Item62);
            }
            else
            {
                if (_oldName != _fileName.CurrentString)
                {
                    SoundEngine.PlaySound(SoundID.ResearchComplete);
                    File.Move(Path.Combine(path, _oldName + ".json"), Path.Combine(path, _fileName.CurrentString + ".json"));
                    CharCreationPreset.FavoritedPresets.Remove(_oldName);
                    CharCreationPreset.FavoritedPresets.Add(_fileName.CurrentString);
                    CharCreationPreset.SaveFavoritePresets();
                    _oldName = _fileName.CurrentString;
                    CharCreationPreset.pendingUpdatePreset = true;

                }
                else
                    if (_fileName.Focused)
                    SoundEngine.PlaySound(SoundID.MenuClose);

            }
        };
        _container.Append(_fileName);

        _character = new UICharacter(_player, true, false, 1f);
        CharCreationPreset.UpdatePets(_character);
        _container.Append(_character);
        _character.VAlign = 1f;


        _deleteButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete"))
        {
            HAlign = 1,
            VAlign = 1
        };
        _deleteButton.OnLeftClick += delegate
        {
            SoundEngine.PlaySound(SoundID.Dig);
            File.Delete(Path.Combine(path, _fileName.CurrentString + ".json"));
            CharCreationPreset.pendingUpdatePreset = true;

            if (CharCreationPreset.FavoritedPresets.Remove(_fileName.CurrentString))
                CharCreationPreset.SaveFavoritePresets();

        };

        _container.Append(_deleteButton);
    }

    public override string ToString() => _fileName?.CurrentString;

    public override int CompareTo(object obj)
    {
        if (obj is not UICharacterBox otherBox || otherBox.ToString() is null) return 0;

        var name = ToString();
        if (!(isFavorited ^ otherBox.isFavorited)) goto label;
        else if (isFavorited && !otherBox.isFavorited) return -1;
        else return 1;
        static int ExtractNumber(string path)
        {
            if (path == null) return 0;
            string fileName = Path.GetFileNameWithoutExtension(path);
            var match = MatchNumber().Match(fileName);
            return match.Success ? int.Parse(match.Value) : 0;
        }
    label:
        return ExtractNumber(name) > ExtractNumber(otherBox.ToString()) ? 1 : -1;
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex MatchNumber();

    public override void MouseOut(UIMouseEvent evt)
    {
        _container.BackgroundColor = new Color(63, 82, 151) * 0.8f;
        _container.BorderColor = Color.Black;
        base.MouseOut(evt);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        _container.BackgroundColor = new Color(73, 94, 171);
        _container.BorderColor = Colors.FancyUIFatButtonMouseOver;
        base.MouseOver(evt);
    }

    public override void MiddleClick(UIMouseEvent evt)
    {
        if (evt.Target != _fileName && evt.Target != _deleteButton)
        {
            if (!CharCreationPreset.FavoritedPresets.Add(_oldName))
            {
                CharCreationPreset.FavoritedPresets.Remove(_oldName);
                isFavorited = false;
            }
            else
            {
                isFavorited = true;
            }
            CharCreationPreset.pendingUpdatePreset = true;
            SoundEngine.PlaySound(SoundID.ResearchComplete);
            CharCreationPreset.SaveFavoritePresets();
        }
        base.MiddleClick(evt);
    }
    public override void DrawChildren(SpriteBatch spriteBatch)
    {
        base.DrawChildren(spriteBatch);
        if (isFavorited)
        {
            var dimension = GetDimensions();
            spriteBatch.Draw(TextureAssets.Cursors[3].Value, dimension.Position(), Color.White);
        }
    }
}


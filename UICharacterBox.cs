using Microsoft.Xna.Framework;
using System.IO;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace CharCreationPreset;
public partial class UICharacterBox : UIElement
{
    UIPanel _container;
    UICharacter _character;
    UIText _fileName;
    Player _player;

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

        _fileName = new UIText(Path.GetFileNameWithoutExtension(filePath));
        _container.Append(_fileName);

        _character = new UICharacter(_player, true, false, 1f);
        _character.PreparePetProjectiles();
        _container.Append(_character);
        _character.VAlign = 1f;
    }

    public override string ToString() => _fileName?.Text;

    public override int CompareTo(object obj)
    {
        if (obj is not UICharacterBox otherBox || otherBox.ToString() is null) return 0;
        static int ExtractNumber(string path)
        {
            if (path == null) return 0;
            string fileName = Path.GetFileNameWithoutExtension(path);
            var match = MatchNumber().Match(fileName);
            return match.Success ? int.Parse(match.Value) : 0;
        }

        return ExtractNumber(ToString()) > ExtractNumber(otherBox.ToString()) ? 1 : -1;
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
}


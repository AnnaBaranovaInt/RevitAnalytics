using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.SAPConnector
{
    public enum SectionColor
    {
        PrussianBlue,
        OrangeCrayola,
        OliveDrab,
        SoftViolet,
        Coral,
        Pistachio,
        PaynesGray,
        TealBlue,
        Redwood,
        CharcoalTeal,
        SlateGray,
        ForestGreen,
        Amethyst,
        DarkRaspberry,
        Zomp,
        DarkCyan,
        Plum,
        Cerulean,
        SteelBlue,
        CarrotOrange,
        BlueSapphire,
        DustyPink,
        FernGreen,
        MutedPurple,
        Rosewood,
        BrickRed,
        DeepSeaBlue,
        SageGreen,
        Firebrick,
        MossGreen,
        OldRose,
        MutedTeal,
        MutedIndigo,
        ImperialRed,
    }
    class Colors
    {

        private static int _colorIndex = 0;

        private static readonly Dictionary<SectionColor, int> ColorMap = new Dictionary<SectionColor, int>
        {
            { SectionColor.ImperialRed,   RgbToBgr(0xF94144) },
            { SectionColor.OrangeCrayola, RgbToBgr(0xF3722C) },
            { SectionColor.CarrotOrange,  RgbToBgr(0xF8961E) },
            { SectionColor.Coral,         RgbToBgr(0xF9844A) },
            { SectionColor.Redwood,       RgbToBgr(0xD1495B) },
            { SectionColor.DarkRaspberry, RgbToBgr(0xB23A48) },
            { SectionColor.Firebrick,     RgbToBgr(0x9E2A2B) },
            { SectionColor.Pistachio,     RgbToBgr(0x90BE6D) },
            { SectionColor.Zomp,          RgbToBgr(0x43AA8B) },
            { SectionColor.DarkCyan,      RgbToBgr(0x4D908E) },
            { SectionColor.FernGreen,     RgbToBgr(0x588157) },
            { SectionColor.OliveDrab,     RgbToBgr(0x3A6B35) },
            { SectionColor.CharcoalTeal,  RgbToBgr(0x264653) },
            { SectionColor.PaynesGray,    RgbToBgr(0x577590) },
            { SectionColor.Cerulean,      RgbToBgr(0x277DA1) },
            { SectionColor.PrussianBlue,  RgbToBgr(0x1D3557) },
            { SectionColor.SteelBlue,     RgbToBgr(0x457B9D) },
            { SectionColor.BlueSapphire,  RgbToBgr(0x2A6F97) },
            { SectionColor.TealBlue,      RgbToBgr(0x1C7293) },
            { SectionColor.DustyPink,       RgbToBgr(0xD291BC) },
            { SectionColor.MutedPurple,     RgbToBgr(0x9D7CBF) },
            { SectionColor.Rosewood,        RgbToBgr(0x65000B) },
            { SectionColor.BrickRed,        RgbToBgr(0x8B3A3A) },
            { SectionColor.SlateGray,       RgbToBgr(0x708090) },
            { SectionColor.DeepSeaBlue,     RgbToBgr(0x083D77) },
            { SectionColor.ForestGreen,     RgbToBgr(0x2E8B57) },
            { SectionColor.SageGreen,       RgbToBgr(0x9CAF88) },
            { SectionColor.MossGreen,       RgbToBgr(0x6D8346) },
            { SectionColor.MutedTeal,       RgbToBgr(0x3B7A6F) },
            { SectionColor.OldRose,         RgbToBgr(0xC08081) },
            { SectionColor.SoftViolet,      RgbToBgr(0xA29BFE) },
            { SectionColor.Amethyst,        RgbToBgr(0x9966CC) },
            { SectionColor.MutedIndigo,     RgbToBgr(0x4B0082) },
            { SectionColor.Plum,            RgbToBgr(0x8E4585) }


        };

        private static int RgbToBgr(int rgb)
        {
            int r = (rgb & 0xFF0000) >> 16;
            int g = (rgb & 0x00FF00);
            int b = (rgb & 0x0000FF) << 16;
            return b | g | r;
        }

        // Obtiene el valor int (BGR) de un color por nombre
        public static int GetColor(SectionColor color)
        {
            return ColorMap[color];
        }

        // Obtiene el nombre del color a partir de su valor int (BGR)
        public static SectionColor? GetColorName(int bgr)
        {
            foreach (var kvp in ColorMap)
            {
                if (kvp.Value == bgr)
                    return kvp.Key;
            }
            return null;
        }

        // Obtiene el siguiente color de la paleta (cíclico)
        public static int GetNextSectionColor()
        {
            var values = (SectionColor[])Enum.GetValues(typeof(SectionColor));
            var color = values[_colorIndex % values.Length];
            _colorIndex++;
            return GetColor(color);
        }
    }

}


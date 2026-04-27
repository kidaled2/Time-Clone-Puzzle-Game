using UnityEngine;

public enum MechanicColorVariant
{
    Cyan,
    Magenta,
    Amber
}

public static class MechanicColorPalette
{
    private static readonly Color FrameDark = new Color(0.035f, 0.045f, 0.075f, 1f);
    private static readonly Color PanelBase = new Color(0.12f, 0.16f, 0.22f, 1f);

    public static MechanicColorVariant InferFromId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return MechanicColorVariant.Cyan;
        }

        for (int i = id.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(id[i]))
            {
                continue;
            }

            return id[i] switch
            {
                '2' => MechanicColorVariant.Magenta,
                '3' => MechanicColorVariant.Amber,
                _ => MechanicColorVariant.Cyan
            };
        }

        return MechanicColorVariant.Cyan;
    }

    public static Color GetAccent(MechanicColorVariant variant)
    {
        return variant switch
        {
            MechanicColorVariant.Magenta => new Color(1f, 0.18f, 0.95f, 1f),
            MechanicColorVariant.Amber => new Color(1f, 0.72f, 0.06f, 1f),
            _ => new Color(0f, 1f, 1f, 1f)
        };
    }

    public static Color GetFrameColor()
    {
        return FrameDark;
    }

    public static Color GetDoorPanelColor(MechanicColorVariant variant)
    {
        return Color.Lerp(PanelBase, GetAccent(variant), 0.45f);
    }

    public static Color GetPlateInactiveColor(MechanicColorVariant variant)
    {
        return Color.Lerp(PanelBase, GetAccent(variant), 0.28f);
    }

    public static Color GetPlateActiveColor(MechanicColorVariant variant)
    {
        return GetAccent(variant);
    }
}

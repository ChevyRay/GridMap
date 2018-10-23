using UnityEngine;
using UnityEditor;

public static class GridMapPrefs
{
    [PreferenceItem("Grid Map")]
    static void OnPreferenceGUI()
    {
        drawRoomBounds = EditorGUILayout.Toggle("Draw Room Bounds", drawRoomBounds);
        roomBoundsColor = EditorGUILayout.ColorField("Room Bounds Color", roomBoundsColor);
    }

    public static bool drawRoomBounds
    {
        get { return EditorPrefs.GetBool("GridMapWindow_drawRoomBounds", true); }
        set { EditorPrefs.SetBool("GridMapWindow_drawRoomBounds", value); }
    }

    public static Color32 roomBoundsColor
    {
        get
        {
            uint uhex = 0xffff00ff;
            int hex = EditorPrefs.GetInt("GridMapWindow_roomBoundsColor", (int)uhex);
            return new Color32(
                (byte)((hex >> 24) & 0xff),
                (byte)((hex >> 16) & 0xff),
                (byte)((hex >> 8) & 0xff),
                (byte)(hex & 0xff)
            );
        }
        set
        {
            int hex = (value.r << 24) | (value.g << 16) | (value.b << 8) | value.a;
            EditorPrefs.SetInt("GridMapWindow_roomBoundsColor", hex);
        }
    }
}

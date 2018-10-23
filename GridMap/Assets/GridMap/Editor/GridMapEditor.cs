using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;

[CustomEditor(typeof(GridMap))]
public class GridMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Can only edit maps from the map window.", MessageType.Info);
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GridMap/Gizmos/GridMapButton Icon.png");
        var cont = new GUIContent(" Open Map Window", icon);
        if (GUILayout.Button(cont))
            GridMapWindow.CreateWindow();
    }
}

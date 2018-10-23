using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class Build
{
    [MenuItem("Build/Grid Map")]
    public static void BuildPackage()
    {
        var guids = AssetDatabase.FindAssets("", new string[]{
            "Assets/GridMap",
            "Assets/Gizmos"
        });

        var assets = new string[guids.Length];
        for (int i = 0; i < guids.Length; ++i)
            assets[i] = AssetDatabase.GUIDToAssetPath(guids[i]);

        var file = EditorUtility.SaveFilePanel("Export Package", "Assets/../..", "GridMap", "unitypackage");
        if (!string.IsNullOrEmpty(file))
            AssetDatabase.ExportPackage(assets, file, ExportPackageOptions.Default);
    }
}

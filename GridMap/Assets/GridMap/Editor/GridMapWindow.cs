using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;

public class GridMapWindow : EditorWindow
{
    [MenuItem("Window/Grid Map")]
    public static void CreateWindow()
    {
        var type = mouseOverWindow.GetType();
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GridMap/Gizmos/GridMapWindow Icon.png");
        var win = GetWindow<GridMapWindow>("Grid Map", true, type);
        win.titleContent = new GUIContent("Grid Map", icon);
        win.minSize = new Vector2(254f, 0f);
        win.Show();
    }

    void OnEnable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
    }

    void OnSelectionChange()
    {
        Repaint();
    }

    void OnSceneGUI(SceneView view)
    {
        if (!GridMapPrefs.drawRoomBounds)
            return;

        var map = Selection.activeObject as GridMap;
        string mapPath = null;
        string mapDir = null;
        var scene = SceneManager.GetActiveScene();
        SceneAsset sceneAsset = null;

        //Find what current map is selected (either by room or by a selected asset)
        if (map != null)
        {
            mapPath = AssetDatabase.GetAssetPath(map);
            if (!string.IsNullOrEmpty(mapPath))
            {
                mapDir = Path.Combine(Path.GetDirectoryName(mapPath), map.name);
                sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            }
            else
                return;
        }
        else
        {
            sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            if (sceneAsset == null)
                return;
            mapDir = Path.GetDirectoryName(scene.path);
            mapPath = mapDir + ".asset";
            map = AssetDatabase.LoadAssetAtPath<GridMap>(mapPath);
        }

        if (map == null)
            return;

        var axis = map.roomBoundsAxis;
        if (axis == Vector3.zero)
            axis = Vector3.up;

        var pos = map.roomBounds.center;
        var scale = map.roomBounds.size;
        var rot = Quaternion.FromToRotation(Vector3.forward, axis.normalized);
        Handles.matrix = Matrix4x4.Translate(pos) * Matrix4x4.Rotate(rot) * Matrix4x4.Scale(scale);
        Handles.color = GridMapPrefs.roomBoundsColor;
        Handles.RectangleHandleCap(0, Vector3.zero, Quaternion.identity, 0.5f, EventType.Repaint);
        Handles.RectangleHandleCap(0, Vector3.zero, Quaternion.identity, 0.51f, EventType.Repaint);
    }

    void OnGUI()
    {
        EditorGUILayout.Space();

        var map = Selection.activeObject as GridMap;
        string mapPath = null;
        string mapDir = null;
        var scene = SceneManager.GetActiveScene();
        var scenePos = new Vector2Int(-1, -1);
        SceneAsset sceneAsset = null;

        //Find what current map is selected (either by room or by a selected asset)
        if (map != null)
        {
            mapPath = AssetDatabase.GetAssetPath(map);
            if (!string.IsNullOrEmpty(mapPath))
            {
                mapDir = Path.Combine(Path.GetDirectoryName(mapPath), map.name);
                sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            }
            else
                map = null;
        }
        else
        {
            sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            if (sceneAsset != null)
            {
                mapDir = Path.GetDirectoryName(scene.path);
                mapPath = mapDir + ".asset";
                map = AssetDatabase.LoadAssetAtPath<GridMap>(mapPath);
            }
        }

        if (map != null)
        {
            //Get the scene position
            var split = scene.name.Split('x');
            if (split.Length == 2)
            {
                int sx, sy;
                Debug.Assert(int.TryParse(split[0], out sx));
                Debug.Assert(int.TryParse(split[1], out sy));
                scenePos.x = sx;
                scenePos.y = sy;
            }
        }

        //Get a list of all the map assets and their names
        var maps = new List<GridMap>();
        AssetDatabase.Refresh();
        foreach (var guid in AssetDatabase.FindAssets("t:GridMap"))
        {
            var m = AssetDatabase.LoadAssetAtPath<GridMap>(AssetDatabase.GUIDToAssetPath(guid));
            if (m != null)
                maps.Add(m);
        }
        maps.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        var names = new string[maps.Count];
        for (int i = 0; i < names.Length; ++i)
            names[i] = maps[i].name;

        //The selected map
        int currIndex = maps.IndexOf(map);
        var newIndex = EditorGUILayout.Popup("Map", currIndex, names);
        if (newIndex != currIndex)
            EditorApplication.delayCall += () => Selection.activeObject = maps[newIndex];

        var sceneIcon = EditorGUIUtility.IconContent("SceneAsset Icon").image;

        //The open scene
        EditorGUILayout.BeginHorizontal();
        var sceneName = scene.name;
        if (string.IsNullOrEmpty(sceneName))
            sceneName = "Untitled";
        EditorGUILayout.LabelField(new GUIContent("Scene"), new GUIContent(" " + sceneName, sceneIcon), EditorStyles.boldLabel);
        if (sceneAsset != null)
        {
            var viewIcon = EditorGUIUtility.IconContent("ViewToolZoom").image;
            if (GUILayout.Button(new GUIContent("", viewIcon), EditorStyles.miniButton, GUILayout.Width(20f), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight)))
            {
                Selection.activeObject = sceneAsset;
                EditorGUIUtility.PingObject(sceneAsset);
            }
        }
        EditorGUILayout.EndHorizontal();

        //If no map is selected, don't render any more
        if (map == null)
            return;

        //Map size
        var newSize = Mathf.Max(1, EditorGUILayout.IntField("Size", map.size));
        if (newSize != map.size)
            ResizeMap(map, mapDir, newSize);

        //Room bounds
        EditorGUI.BeginChangeCheck();
        var newBounds = EditorGUILayout.RectField("Room Bounds", map.roomBounds);
        var newAxis = EditorGUILayout.Vector3Field("Room Bounds Axis", map.roomBoundsAxis);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(map, "change bounds");
            map.roomBounds = newBounds;
            map.roomBoundsAxis = newAxis;
        }

        //Shift map arrows
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Shift All Rooms");
        GUI.enabled = CanShiftMap(map, mapDir, 1, 0);
        if (GUILayout.Button("→"))
            ShiftMap(map, sceneAsset, scenePos, mapDir, 1, 0);
        GUI.enabled = CanShiftMap(map, mapDir, -1, 0);
        if (GUILayout.Button("←"))
            ShiftMap(map, sceneAsset, scenePos, mapDir, -1, 0);
        GUI.enabled = CanShiftMap(map, mapDir, 0, 1);
        if (GUILayout.Button("↓"))
            ShiftMap(map, sceneAsset, scenePos, mapDir, 0, 1);
        GUI.enabled = CanShiftMap(map, mapDir, 0, -1);
        if (GUILayout.Button("↑"))
            ShiftMap(map, sceneAsset, scenePos, mapDir, 0, -1);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        //Shift room arrows
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Shift Current Room");
        GUI.enabled = sceneAsset != null && CanShiftRoom(map, scenePos, scene.path, 1, 0);
        if (GUILayout.Button("→"))
            ShiftRoom(map, scenePos, scene.path, 1, 0);
        GUI.enabled = sceneAsset != null && CanShiftRoom(map, scenePos, scene.path, -1, 0);
        if (GUILayout.Button("←"))
            ShiftRoom(map, scenePos, scene.path, -1, 0);
        GUI.enabled = sceneAsset != null && CanShiftRoom(map, scenePos, scene.path, 0, 1);
        if (GUILayout.Button("↓"))
            ShiftRoom(map, scenePos, scene.path, 0, 1);
        GUI.enabled = sceneAsset != null && CanShiftRoom(map, scenePos, scene.path, 0, -1);
        if (GUILayout.Button("↑"))
            ShiftRoom(map, scenePos, scene.path, 0, -1);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        //Render all the map room buttons
        var viewWidth = EditorGUIUtility.currentViewWidth - 10f;
        var rect = GUILayoutUtility.GetRect(viewWidth, viewWidth);
        rect.x += 4f;
        var tile = new Rect(rect.x, rect.y, viewWidth / map.size, viewWidth / map.size);
        var pos = new Vector2Int();
        for (pos.y = 0; pos.y < map.size; ++pos.y)
        {
            for (pos.x = 0; pos.x < map.size; ++pos.x)
            {
                tile.x = rect.x + tile.width * pos.x;
                tile.y = rect.y + tile.height * pos.y;
                var btnRect = new Rect(tile.x, tile.y, tile.width + 1f, tile.height + 1f);

                var posName = pos.x + "x" + pos.y;
                var posPath = mapDir + "/" + posName + ".unity";
                var posAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(posPath);

                if (posAsset != null && pos == scenePos)
                {
                    GUI.enabled = false;
                    GUI.color = Color.green;
                }
                else if (posAsset != null)
                    GUI.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                else
                    GUI.color = Color.white;
                
                if (GUI.Button(btnRect, string.Empty))
                {
                    var targ = Selection.activeObject as GridMap;
                    if (posAsset != null)
                    {
                        EditorApplication.delayCall += () => {
                            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                                return;
                            EditorSceneManager.OpenScene(posPath);
                            if (targ != null)
                                Selection.activeObject = targ;
                        };
                    }
                    else
                    {
                        EditorApplication.delayCall += () => {
                            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                                return;
                            if (!AssetDatabase.IsValidFolder(mapDir))
                                AssetDatabase.CreateFolder("Assets/Maps", map.name);
                            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
                            EditorSceneManager.SaveScene(newScene, posPath);
                            if (targ != null)
                                Selection.activeObject = targ;
                            else
                                Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(newScene.path);
                        };
                    }
                }

                GUI.enabled = true;

                if (posAsset != null)
                {
                    if (pos == scenePos)
                        GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(btnRect.x + 4f, btnRect.y + 4f, btnRect.width - 8f, btnRect.height - 8f), sceneIcon);
                }
            }
        }
        GUI.color = Color.white;
        
        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.enabled = sceneAsset != null && scenePos.x >= 0;
        if (GUILayout.Button("Delete Room"))
        {
            EditorApplication.delayCall += () => {
                bool del = EditorUtility.DisplayDialog("Delete Scene", "Are you sure you want to delete this scene?", "Yes", "Cancel");
                if (del)
                {
                    AssetDatabase.DeleteAsset(scene.path);
                    Selection.activeObject = map;
                }
            };
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    void ResizeMap(GridMap map, string mapDir, int s)
    {
        if (s < map.size)
            for (int x = s; x < map.size; ++x)
                for (int y = 0; y < map.size; ++y)
                    if (File.Exists(Path.Combine(mapDir, x + "x" + y + ".unity")))
                        return;

        if (s < map.size)
            for (int y = s; y < map.size; ++y)
                for (int x = 0; x < map.size; ++x)
                    if (File.Exists(Path.Combine(mapDir, x + "x" + y + ".unity")))
                        return;

        Undo.RecordObject(map, "resize map");
        map.size = s;
    }

    bool CanShiftRoom(GridMap map, Vector2Int scenePos, string path, int mx, int my)
    {
        int xx = scenePos.x + mx;
        int yy = scenePos.y + my;
        if (xx < 0 || yy < 0 || xx >= map.size || yy >= map.size)
            return false;

        var newPath = Path.Combine(Path.GetDirectoryName(path), xx + "x" + yy + ".unity");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(newPath) != null)
            return false;

        return true;
    }

    void ShiftRoom(GridMap map, Vector2Int scenePos, string path, int mx, int my)
    {
        var targ = Selection.activeObject as GridMap;

        EditorApplication.delayCall += () =>
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            int xx = scenePos.x + mx;
            int yy = scenePos.y + my;
            if (xx < 0 || yy < 0 || xx >= map.size || yy >= map.size)
                return;
                
            var newPath = Path.Combine(Path.GetDirectoryName(path), xx + "x" + yy + ".unity");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(newPath) != null)
                return;

            File.Move(path, newPath);
            File.Move(path + ".meta", newPath + ".meta");
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(newPath);

            if (targ != null)
                Selection.activeObject = targ;
        };
    }

    bool CanShiftMap(GridMap map, string dir, int mx, int my)
    {
        for (int y = 0; y < map.size; ++y)
        {
            for (int x = 0; x < map.size; ++x)
            {
                var path = Path.Combine(dir, x + "x" + y + ".unity");
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (asset != null)
                {
                    var xx = x + mx;
                    var yy = y + my;
                    if (xx < 0 || yy < 0 || xx >= map.size || yy >= map.size)
                        return false;
                }
            }
        }
        return true;
    }

    void ShiftMap(GridMap map, SceneAsset sceneAsset, Vector2Int scenePos, string dir, int mx, int my)
    {
        var targ = Selection.activeObject as GridMap;

        EditorApplication.delayCall += () =>
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var tempPaths = new Dictionary<string, string>();
            var pathChanges = new Dictionary<string, string>();
            for (int y = 0; y < map.size; ++y)
            {
                for (int x = 0; x < map.size; ++x)
                {
                    var path = Path.Combine(dir, x + "x" + y + ".unity");
                    var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    if (asset != null)
                    {
                        var xx = x + mx;
                        var yy = y + my;
                        if (xx < 0 || yy < 0 || xx >= map.size || yy >= map.size)
                            return;
                        var tempPath = Path.Combine(dir, x + "x" + y + "_temp.unity");
                        tempPaths[path] = tempPath;
                        pathChanges[tempPath] = Path.Combine(dir, xx + "x" + yy + ".unity");
                    }
                }
            }

            //Give all the scenes temp paths
            foreach (var pair in tempPaths)
            {
                File.Move(pair.Key, pair.Value);
                File.Move(pair.Key + ".meta", pair.Value + ".meta");
            }

            //Once they all have temp paths, give them their new names
            foreach (var pair in pathChanges)
            {
                File.Move(pair.Key, pair.Value);
                File.Move(pair.Key + ".meta", pair.Value + ".meta");
            }

            AssetDatabase.Refresh();

            //If we were in a scene, re-load it at its new position
            if (sceneAsset != null)
            {
                int xx = scenePos.x + mx;
                int yy = scenePos.y + my;
                var path = Path.Combine(dir, xx + "x" + yy + ".unity");
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                    EditorSceneManager.OpenScene(path);
            }

            if (targ != null)
                Selection.activeObject = targ;
        };
    }

    public Event ev
    {
        get { return Event.current; }
    }
}

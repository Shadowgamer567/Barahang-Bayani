using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Creates the badge sprites and installs editable popup components in build scenes.</summary>
[InitializeOnLoad]
public static class AchievementPopupSceneInstaller
{
    private const string IconFolder = "Assets/Resources/AchievementIcons";
    private const string FontPath = "Assets/Fonts/Junicode SDF.asset";
    private const string InstalledKey = "AchievementPopupSceneInstaller.Installed.v2";

    static AchievementPopupSceneInstaller()
    {
        EditorApplication.delayCall += InstallAutomaticallyOnce;
    }

    private static void InstallAutomaticallyOnce()
    {
        if (SessionState.GetBool(InstalledKey, false)) return;
        SessionState.SetBool(InstalledKey, true);
        InstallAllScenes();
    }

    [MenuItem("Tools/Achievements/Install Popups In Build Scenes")]
    public static void InstallAllScenes()
    {
        string sceneToRestore = SceneManager.GetActiveScene().path;
        GenerateAchievementIcons();
        AssetDatabase.Refresh();
        foreach (string iconPath in new[] { IconFolder + "/FirstSteps.png", IconFolder + "/TripleElimination.png", IconFolder + "/MasterHealer.png" })
        {
            TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || !importer.isReadable))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
            AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }
        foreach (string scenePath in GetBuildScenePaths())
            InstallInScene(scenePath);
        if (!string.IsNullOrEmpty(sceneToRestore) && File.Exists(sceneToRestore))
            EditorSceneManager.OpenScene(sceneToRestore, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("Achievement popups and badge icons are installed in all enabled build scenes.");
    }

    private static IEnumerable<string> GetBuildScenePaths()
    {
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                yield return scene.path;
    }

    private static void InstallInScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        AchievementPopupUI popup = Object.FindFirstObjectByType<AchievementPopupUI>();
        if (popup == null)
        {
            GameObject host = new GameObject("Achievement Popup UI");
            SceneManager.MoveGameObjectToScene(host, scene);
            popup = host.AddComponent<AchievementPopupUI>();
        }

        SerializedObject serializedPopup = new SerializedObject(popup);
        SerializedProperty font = serializedPopup.FindProperty("font");
        if (font != null) font.objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontPath);

        SerializedProperty achievements = serializedPopup.FindProperty("achievements");
        string[] ids = { "complete_tutorial", "triple_elimination", "master_healer" };
        string[] iconNames = { "FirstSteps", "TripleElimination", "MasterHealer" };
        if (achievements != null)
        {
            for (int i = 0; i < achievements.arraySize && i < ids.Length; i++)
            {
                SerializedProperty entry = achievements.GetArrayElementAtIndex(i);
                SerializedProperty id = entry.FindPropertyRelative("id");
                SerializedProperty icon = entry.FindPropertyRelative("icon");
                if (id != null) id.stringValue = ids[i];
                if (icon != null)
                    icon.objectReferenceValue = LoadIconSprite(IconFolder + "/" + iconNames[i] + ".png");
            }
        }
        serializedPopup.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(popup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Sprite LoadIconSprite(string assetPath)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (UnityEngine.Object asset in assets)
            if (asset is Sprite sprite)
                return sprite;
        return null;
    }

    private static void GenerateAchievementIcons()
    {
        Directory.CreateDirectory(IconFolder);
        SaveIcon("FirstSteps", DrawScrollAndStar);
        SaveIcon("TripleElimination", DrawThreeSwords);
        SaveIcon("MasterHealer", DrawHealingVial);
    }

    private static void SaveIcon(string name, System.Action<IconPainter> draw)
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.SetPixels(new Color[size * size]);
        IconPainter painter = new IconPainter(texture, size);
        draw(painter);
        texture.SetPixels(painter.Pixels);
        texture.Apply();
        File.WriteAllBytes(IconFolder + "/" + name + ".png", texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static void DrawScrollAndStar(IconPainter p)
    {
        Color parchment = new Color(1f, 0.84f, 0.48f, 1f);
        Color ink = new Color(0.22f, 0.16f, 0.10f, 1f);
        p.Line(67, 52, 154, 52, parchment, 12); p.Line(67, 52, 67, 194, parchment, 12);
        p.Line(67, 194, 149, 194, parchment, 12); p.Line(154, 52, 154, 110, parchment, 12);
        p.Line(86, 91, 132, 91, ink, 7); p.Line(86, 112, 130, 112, ink, 7); p.Line(86, 133, 119, 133, ink, 7);
        p.Circle(174, 166, 44, new Color(0.29f, 0.68f, 0.87f, 1f));
        p.Star(174, 166, 31, 14, parchment, 5);
    }

    private static void DrawThreeSwords(IconPainter p)
    {
        Color blade = new Color(1f, 0.88f, 0.62f, 1f);
        Color hilt = new Color(0.25f, 0.13f, 0.09f, 1f);
        p.Sword(76, 66, 164, 190, blade, hilt, 11);
        p.Sword(76, 190, 164, 66, blade, hilt, 11);
        p.Sword(52, 128, 204, 128, blade, hilt, 10);
        p.Circle(128, 128, 19, new Color(0.85f, 0.32f, 0.27f, 1f));
        p.Circle(128, 128, 8, new Color(1f, 0.88f, 0.62f, 1f));
    }

    private static void DrawHealingVial(IconPainter p)
    {
        Color glass = new Color(0.78f, 0.94f, 0.84f, 1f);
        Color green = new Color(0.26f, 0.74f, 0.50f, 1f);
        Color gold = new Color(1f, 0.84f, 0.48f, 1f);
        p.Polygon(new[] { new Vector2(101, 44), new Vector2(155, 44), new Vector2(155, 69), new Vector2(145, 82), new Vector2(170, 112), new Vector2(177, 182), new Vector2(160, 204), new Vector2(96, 204), new Vector2(79, 182), new Vector2(86, 112), new Vector2(111, 82), new Vector2(101, 69) }, glass);
        p.Rect(100, 35, 56, 15, gold);
        p.Polygon(new[] { new Vector2(91, 143), new Vector2(165, 143), new Vector2(170, 182), new Vector2(158, 194), new Vector2(98, 194), new Vector2(86, 182) }, green);
        p.Line(128, 103, 128, 161, Color.white, 13); p.Line(99, 132, 157, 132, Color.white, 13);
    }

    private sealed class IconPainter
    {
        private readonly Texture2D texture;
        private readonly int size;
        private readonly Color[] pixels;
        public Color[] Pixels => pixels;
        public IconPainter(Texture2D texture, int size) { this.texture = texture; this.size = size; pixels = texture.GetPixels(); }
        private void Put(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < size && y < size) pixels[y * size + x] = c; }
        public void Circle(float cx, float cy, float radius, Color c) { int r = Mathf.CeilToInt(radius); for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++) if (x * x + y * y <= radius * radius) Put(Mathf.RoundToInt(cx) + x, Mathf.RoundToInt(cy) + y, c); }
        public void Rect(int x0, int y0, int x1, int y1, Color c) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Put(x, y, c); }
        public void Line(float x0, float y0, float x1, float y1, Color c, float width)
        {
            float dx = x1 - x0, dy = y1 - y0, length2 = dx * dx + dy * dy, radius = width * 0.5f;
            int minX = Mathf.FloorToInt(Mathf.Min(x0, x1) - radius), maxX = Mathf.CeilToInt(Mathf.Max(x0, x1) + radius);
            int minY = Mathf.FloorToInt(Mathf.Min(y0, y1) - radius), maxY = Mathf.CeilToInt(Mathf.Max(y0, y1) + radius);
            for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
            { float t = length2 < 0.001f ? 0 : Mathf.Clamp01(((x - x0) * dx + (y - y0) * dy) / length2); float ax = x0 + t * dx - x, ay = y0 + t * dy - y; if (ax * ax + ay * ay <= radius * radius) Put(x, y, c); }
        }
        public void Polygon(Vector2[] points, Color c)
        {
            int minY = size, maxY = 0; foreach (Vector2 point in points) { minY = Mathf.Min(minY, Mathf.FloorToInt(point.y)); maxY = Mathf.Max(maxY, Mathf.CeilToInt(point.y)); }
            for (int y = minY; y <= maxY; y++) for (int x = 0; x < size; x++)
            { bool inside = false; for (int i = 0, j = points.Length - 1; i < points.Length; j = i++) if ((points[i].y > y) != (points[j].y > y) && x < (points[j].x - points[i].x) * (y - points[i].y) / (points[j].y - points[i].y) + points[i].x) inside = !inside; if (inside) Put(x, y, c); }
        }
        public void Star(float cx, float cy, float outer, float inner, Color c, int points)
        { Vector2[] vertices = new Vector2[points * 2]; for (int i = 0; i < vertices.Length; i++) { float a = Mathf.PI * 0.5f + i * Mathf.PI / points; float radius = i % 2 == 0 ? outer : inner; vertices[i] = new Vector2(cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius); } Polygon(vertices, c); }
        public void Sword(float x0, float y0, float x1, float y1, Color blade, Color hilt, float width)
        { Line(x0, y0, x1, y1, blade, width); float dx = x1 - x0, dy = y1 - y0, len = Mathf.Sqrt(dx * dx + dy * dy); if (len < 1) return; float px = -dy / len, py = dx / len; Line(x0 + px * 13, y0 + py * 13, x0 - px * 13, y0 - py * 13, hilt, width + 5); Line(x0 - (x1 - x0) / len * 9, y0 - (y1 - y0) / len * 9, x0 - (x1 - x0) / len * 31, y0 - (y1 - y0) / len * 31, hilt, width + 3); }
    }
}

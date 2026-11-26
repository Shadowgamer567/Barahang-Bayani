using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>Combines the achievement badges into one named TextMesh Pro sprite asset.</summary>
public static class AchievementInlineIcons
{
    private static TMP_SpriteAsset spriteAsset;

    public static void Apply(TMP_Text text)
    {
        if (text == null) return;
        EnsureSpriteAsset();
        if (spriteAsset != null)
            text.spriteAsset = spriteAsset;
    }

    private static void EnsureSpriteAsset()
    {
        if (spriteAsset != null) return;

        string[] names = { "FirstSteps", "TripleElimination", "MasterHealer" };
        Texture2D[] sourceTextures = new Texture2D[names.Length];
        int width = 0;
        int height = 0;
        for (int i = 0; i < names.Length; i++)
        {
            sourceTextures[i] = Resources.Load<Texture2D>("AchievementIcons/" + names[i]);
            if (sourceTextures[i] == null)
            {
                Debug.LogWarning("Achievement icon texture is missing: " + names[i]);
                return;
            }
            width = Mathf.Max(width, sourceTextures[i].width);
            height = Mathf.Max(height, sourceTextures[i].height);
        }

        // A single atlas avoids TMP fallback lookup differences between sprite assets.
        Texture2D atlas = new Texture2D(width * names.Length, height, TextureFormat.RGBA32, false)
        {
            name = "Achievement Inline Icon Atlas"
        };
        List<TMP_SpriteCharacter> characters = new List<TMP_SpriteCharacter>();
        List<TMP_SpriteGlyph> glyphs = new List<TMP_SpriteGlyph>();

        spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        spriteAsset.name = "Achievement Inline Icons";
        spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(spriteAsset.name);
        spriteAsset.spriteSheet = atlas;

        for (int i = 0; i < names.Length; i++)
        {
            Texture2D source = sourceTextures[i];
            int x = i * width;
            atlas.SetPixels(x, 0, source.width, source.height, source.GetPixels());

            Rect rect = new Rect(x, 0, source.width, source.height);
            Sprite sprite = Sprite.Create(atlas, rect, new Vector2(0.5f, 0.5f), 100f);
            TMP_SpriteGlyph glyph = new TMP_SpriteGlyph((uint)i,
                new GlyphMetrics(source.width, source.height, 0f, source.height, source.width),
                new GlyphRect(x, 0, source.width, source.height), 1f, 0)
            {
                sprite = sprite
            };
            TMP_SpriteCharacter character = new TMP_SpriteCharacter(0xE000u + (uint)i, glyph)
            {
                name = names[i],
                scale = 1.35f
            };
            glyphs.Add(glyph);
            characters.Add(character);
        }
        atlas.Apply(false, false);

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(TMP_SpriteAsset).GetField("m_SpriteCharacterTable", flags)
            ?.SetValue(spriteAsset, characters);
        typeof(TMP_SpriteAsset).GetField("m_GlyphTable", flags)
            ?.SetValue(spriteAsset, glyphs);
        typeof(TMP_Asset).GetField("m_Version", flags)
            ?.SetValue(spriteAsset, "1.1.0");

        Shader spriteShader = Shader.Find("TextMeshPro/Sprite");
        if (spriteShader != null)
        {
            spriteAsset.material = new Material(spriteShader)
            {
                name = "Achievement Inline Icons Material",
                mainTexture = atlas
            };
        }
        spriteAsset.UpdateLookupTables();
    }
}

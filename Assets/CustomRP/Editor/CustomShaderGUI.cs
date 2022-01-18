using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
    private MaterialEditor editor;
    private Object[] materials;
    private MaterialProperty[] properties;

    private bool showPresets;
    
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);

        this.editor = materialEditor;
        this.materials = materialEditor.targets;
        this.properties = properties;
        
        EditorGUILayout.Space();
        showPresets = EditorGUILayout.Foldout(showPresets, "Presets", true);
        if (showPresets)
        {
            OpaquePreset();
            ClipPreset();
            FadePreset();
            TransparentPreset();
        }
    }

    private void SetProperty(string name, float value)
    {
        FindProperty(name, properties).floatValue = value;
    }

    private void SetKeyword(string keyword, bool enabled)
    {
        if (enabled)
        {
            foreach (var o in materials)
            {
                var material = (Material)o;
                material.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (var o in materials)
            {
                var material = (Material)o;
                material.DisableKeyword(keyword);
            }
        }
    }

    private void SetProperty(string name, string keyword, bool value)
    {
        SetProperty(name, value ? 1f : 0f);
        SetKeyword(keyword, value);
    }

    private bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }
    
    private bool PremultiplyAlpha
    {
        set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
    }
    
    private BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float)value);
    }
    
    private BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float)value);
    }
    
    private bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1f : 0f); 
    }

    RenderQueue RenderQueue
    {
        set
        {
            foreach (Object material in materials)
            {
                var m = material as Material;
                m.renderQueue = (int)value;
            }
        }
    }

    bool PresetButton(string name)
    {
        if (GUILayout.Button(name))
        {
            editor.RegisterPropertyChangeUndo(name);
            return true;
        }

        return false;
    }

    void OpaquePreset()
    {
        if (PresetButton("Opaque"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }
    
    void ClipPreset()
    {
        if (PresetButton("Clip"))
        {
            Clipping = true;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }
    
    void FadePreset()
    {
        if (PresetButton("Fade"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
    
    void TransparentPreset()
    {
        if (PresetButton("Transparent"))
        {
            Clipping = false;
            PremultiplyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
}
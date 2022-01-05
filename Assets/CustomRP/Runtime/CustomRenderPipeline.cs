using System.Collections;
using System.Collections.Generic;
using CustomRP.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    private CameraRenderer cameraRenderer = new CameraRenderer();

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            cameraRenderer.Render(context, camera);
        }
    }
}

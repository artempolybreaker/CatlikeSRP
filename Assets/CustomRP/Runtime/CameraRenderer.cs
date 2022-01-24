using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public partial class CameraRenderer
    {
        private const string BUFFER_NAME = "My Render Camera";

        private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

        
        private CommandBuffer buffer = new CommandBuffer { name = BUFFER_NAME };
        private Lighting lighting = new Lighting();

        private CullingResults cullingResults;
        private ScriptableRenderContext context;
        private Camera camera;

        public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching,
            bool useGPUInstancing, ShadowSettings shadowSettings)
        {
            this.context = context;
            this.camera = camera;
            
            PrepareBuffer();
            PrepareForSceneWindow();
            if (!Cull(shadowSettings.maxDistance))
            {
                return;
            }
            
            Setup();
            lighting.Setup(context, cullingResults, shadowSettings);
            DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
            DrawUnsupportedShaders();
            DrawGizmos();
            lighting.Cleanup();
            Submit();
        }

        private void Setup()
        {
            context.SetupCameraProperties(camera);
            CameraClearFlags flags = camera.clearFlags;
            buffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags == CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
            buffer.BeginSample(SampleName);
            ExecuteBuffer();
        }

        private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
        {
            // opaque
            var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
            var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing,
            };
            drawingSettings.SetShaderPassName(1, litShaderTagId);

            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            
            context.DrawSkybox(camera);
            
            // transparent
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private void Submit()
        {
            buffer.EndSample(SampleName);
            ExecuteBuffer();
            context.Submit();
        }

        private void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        private bool Cull(float shadowSettingsMaxDistance)
        {
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
            {
                cullingParameters.shadowDistance = Mathf.Min(shadowSettingsMaxDistance, camera.farClipPlane);
                cullingResults = context.Cull(ref cullingParameters);
                return true;
            }

            return false;
        }
    }
}
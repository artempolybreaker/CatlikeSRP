using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Shadows
    {
        private struct ShadowedDirLight
        {
            public int visibleLightIndex;
        }

        private const string BUFFER_NAME = "Shadows";
        private const uint MAX_SHADOWED_DIR_LIGHT_COUNT = 1;

        private static int dirShadowAtlasId = Shader.PropertyToID("_DirShadowAtlas");

        private CommandBuffer buffer = new CommandBuffer { name = BUFFER_NAME };
        private ShadowedDirLight[] shadowedDirLights = new ShadowedDirLight[MAX_SHADOWED_DIR_LIGHT_COUNT];
        private uint shadowedDirLightCount = 0;

        private ScriptableRenderContext context;
        private CullingResults cullingResults;
        private ShadowSettings shadowSettings;

        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
        {
            this.context = context;
            this.cullingResults = cullingResults;
            this.shadowSettings = shadowSettings;
            shadowedDirLightCount = 0;
        }

        void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public void ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (shadowedDirLightCount >= MAX_SHADOWED_DIR_LIGHT_COUNT ||
                light.shadows == LightShadows.None ||
                Mathf.Approximately(light.shadowStrength, 0f) ||
                !cullingResults.GetShadowCasterBounds(visibleLightIndex, out var shadowCasterBounds))
            {
                return;
            }

            shadowedDirLights[shadowedDirLightCount++] =
                new ShadowedDirLight { visibleLightIndex = visibleLightIndex };
        }

        public void Render()
        {
            if (shadowedDirLightCount > 0)
            {
                RenderDirectionalShadows();
            }
            else
            {
                // note: if there are no shadows we still want claim a dummy texture for shadowmap so it's compatible with WebGL 2.0
                // (because of the way WebGL 2.0 binds textures and samplers together)
                buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            }
        }

        private void RenderDirectionalShadows()
        {
            int atlasSize = (int)shadowSettings.directional.atlasSize;
            buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.ClearRenderTarget(true, false, Color.clear);
            ExecuteBuffer();
        }

        public void Cleanup()
        {
            buffer.ReleaseTemporaryRT(dirShadowAtlasId);
            ExecuteBuffer();
        }
    }
}
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
    }
}
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Lighting
    {
        private const int MAX_DIR_LIGHT_COUNT = 4;
        private const string BUFFER_NAME = "Lighting";

        private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
        private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
        private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

        private Vector4[] dirLightColors = new Vector4[MAX_DIR_LIGHT_COUNT];
        private Vector4[] dirLightDirections = new Vector4[MAX_DIR_LIGHT_COUNT];

        private CullingResults cullingResults;

        private CommandBuffer buffer = new CommandBuffer { name = BUFFER_NAME };
        private Shadows shadows;

        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
        {
            this.cullingResults = cullingResults;
            buffer.BeginSample(BUFFER_NAME);
            shadows.Setup(context, cullingResults, shadowSettings);
            SetupLights();
            shadows.Render();
            buffer.EndSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        private void SetupLights()
        {
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            var dirLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                if (visibleLight.lightType == LightType.Directional)
                {
                    SetupDirectionalLight(dirLightCount++, ref visibleLight);
                    if (dirLightCount >= MAX_DIR_LIGHT_COUNT)
                    {
                        break;
                    }
                }
            }

            buffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
            buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
        }

        private void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
        {
            dirLightColors[index] = visibleLight.finalColor;
            dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
            shadows.ReserveDirectionalShadows(visibleLight.light, index);
        }

        public void Cleanup()
        {
            shadows.Cleanup();
        }
    }
}
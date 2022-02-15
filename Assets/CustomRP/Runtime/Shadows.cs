using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Shadows
    {
        private struct ShadowedDirLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
        }

        private const string BUFFER_NAME = "Shadows";
        private const uint MAX_SHADOWED_DIR_LIGHT_COUNT = 4;
        private const uint MAX_CASCADES = 4;

        private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        private static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
        private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
        private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
        private static int cascadeDataId = Shader.PropertyToID("_CascadeData");
        private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

        private static Matrix4x4[] dirShadowMatrices = new Matrix4x4[MAX_SHADOWED_DIR_LIGHT_COUNT * MAX_CASCADES];
        private static Vector4[] cascadeCullingSpheres = new Vector4[MAX_CASCADES];
        private static Vector4[] cascadeData = new Vector4[MAX_CASCADES];

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

        public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (shadowedDirLightCount < MAX_SHADOWED_DIR_LIGHT_COUNT &&
                light.shadows != LightShadows.None && light.shadowStrength > 0f &&
                cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)
            )
            {
                shadowedDirLights[shadowedDirLightCount] =
                    new ShadowedDirLight { visibleLightIndex = visibleLightIndex, slopeScaleBias = light.shadowBias };
                return new Vector3(light.shadowStrength, shadowSettings.directional.cascadeCount * shadowedDirLightCount++, light.shadowNormalBias);
            }
            
            return Vector3.zero;
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
            buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear,
                RenderTextureFormat.Shadowmap);
            buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.ClearRenderTarget(true, false, Color.clear);
            buffer.BeginSample(BUFFER_NAME);
            ExecuteBuffer();

            int tiles = (int)shadowedDirLightCount * shadowSettings.directional.cascadeCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;

            for (int i = 0; i < shadowedDirLightCount; i++)
            {
                RenderDirectionalShadows(i, split, tileSize);
            }

            buffer.SetGlobalInt(cascadeCountId, shadowSettings.directional.cascadeCount);
            buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
            buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
            buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
            float f = 1f - shadowSettings.directional.cascadeFade;
            buffer.SetGlobalVector(
                shadowDistanceFadeId,
                new Vector4(
                    1f / shadowSettings.maxDistance,
                    1f / shadowSettings.distanceFade,
                    1 / (1f - f * f)));
            buffer.EndSample(BUFFER_NAME);
            ExecuteBuffer();
        }

        private void RenderDirectionalShadows(int index, int split, int tileSize)
        {
            ShadowedDirLight light = shadowedDirLights[index];
            var shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
            int cascadeCount = shadowSettings.directional.cascadeCount;
            int tileOffset = index * cascadeCount;
            Vector3 ratios = shadowSettings.directional.CascadeRatios;

            for (int i = 0; i < cascadeCount; i++)
            {
                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    light.visibleLightIndex, 
                    i, 
                    cascadeCount,
                    ratios,
                    tileSize,
                    0f,
                    out var viewMatrix,
                    out var projMatrix,
                    out var splitData);

                shadowDrawingSettings.splitData = splitData;
                // we only need to do this for the first light, as the cascades of all lights are equivalent.
                if (index == 0)
                {
                    SetCascadeData(i, splitData.cullingSphere, tileSize);
                }

                int tileIndex = tileOffset + i;
                // dirShadowMatrices[tileIndex] = projMatrix * viewMatrix; // -> conversion matrix from world space to light space
                dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                    projMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split
                );

                buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
                
                buffer.SetGlobalDepthBias(0, light.slopeScaleBias);
                ExecuteBuffer();
                context.DrawShadows(ref shadowDrawingSettings);
                buffer.SetGlobalDepthBias(0f, 0f);
            }
        }

        private Vector2 SetTileViewport(int index, int split, float tileSize)
        {
            Vector2 offset = new Vector2(index % split, index / split);
            buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
            return offset;
        }

        private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

            // clip space is defined inside a cube with with coordinates going from −1 to 1, with zero at its center.
            // But textures coordinates and depth go from zero to one. We can bake this conversion into the matrix by
            // scaling and offsetting the XYZ dimensions by half
            float scale = 1f / split;
            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);

            return m;
        }

        private void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
        {
            float texelSize = (2f * cullingSphere.w) / tileSize;
            cullingSphere.w *= cullingSphere.w;
            cascadeCullingSpheres[index] = cullingSphere;
            cascadeData[index] = new Vector4(
                1f / cullingSphere.w,
                texelSize * 1.4142136f); // -> In the worst case we end up having to offset along the square's diagonal, so let's scale it by √2. 
        }
        
        public void Cleanup()
        {
            buffer.ReleaseTemporaryRT(dirShadowAtlasId);
            ExecuteBuffer();
        }
    }
}
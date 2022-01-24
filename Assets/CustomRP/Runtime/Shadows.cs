using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Shadows
    {
        private const string BUFFER_NAME = "Shadows";

        private CommandBuffer buffer = new CommandBuffer { name = BUFFER_NAME };

        private ScriptableRenderContext context;
        private CullingResults cullingResults;
        private ShadowSettings shadowSettings;
        
        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
        {
            this.context = context;
            this.cullingResults = cullingResults;
            this.shadowSettings = shadowSettings;
        }

        void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
    }
}
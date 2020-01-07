namespace CoreEngine.Graphics
{
    public readonly struct PipelineState
    {
        public PipelineState(uint pipelineStateId)
        {
            this.PipelineStateId = pipelineStateId;
        }

        public readonly uint PipelineStateId { get; }
    }
}
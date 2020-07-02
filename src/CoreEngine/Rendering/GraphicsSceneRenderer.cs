using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
    public struct RenderPassConstants
    {
        public Matrix4x4 ViewMatrix { get; set; }
        public Matrix4x4 ProjectionMatrix { get; set; }
    }

    public struct ObjectProperties
    {
        public Matrix4x4 WorldMatrix { get; set; }
    }

    internal struct ShaderBoundingBox
    {
        public Vector3 MinPoint;
        public Vector3 MaxPoint;
        public float Reserved1;
        public float Reserved2;
    }

    internal struct ShaderBoundingFrustum
    {
        public ShaderBoundingFrustum(BoundingFrustum boundingFrustum)
        {
            this.LeftPlane = new Vector4(boundingFrustum.LeftPlane.Normal, boundingFrustum.LeftPlane.D);
            this.RightPlane = new Vector4(boundingFrustum.RightPlane.Normal, boundingFrustum.RightPlane.D);
            this.TopPlane = new Vector4(boundingFrustum.TopPlane.Normal, boundingFrustum.TopPlane.D);
            this.BottomPlane = new Vector4(boundingFrustum.BottomPlane.Normal, boundingFrustum.BottomPlane.D);
            this.NearPlane = new Vector4(boundingFrustum.NearPlane.Normal, boundingFrustum.NearPlane.D);
            this.FarPlane = new Vector4(boundingFrustum.FarPlane.Normal, boundingFrustum.FarPlane.D);
        }

        public Vector4 LeftPlane;
        public Vector4 RightPlane;
        public Vector4 TopPlane;
        public Vector4 BottomPlane;
        public Vector4 NearPlane;
        public Vector4 FarPlane;
    }

    internal struct ShaderCamera
    {
        public int DepthBufferTextureIndex;
        public Vector3 WorldPosition;
        public Matrix4x4 ViewMatrix;
        public Matrix4x4 ProjectionMatrix;
        public Matrix4x4 ViewProjectionMatrix;
        public Matrix4x4 ViewProjectionMatrixInverse;
        public ShaderBoundingFrustum BoundingFrustum;
        public int OpaqueCommandListIndex;
        public int OpaqueDepthCommandListIndex;
        public int TransparentCommandListIndex;
        public int TransparentDepthCommandListIndex;
        public bool DepthOnly;
        public bool AlreadyProcessed;
        public byte Reserved2;
        public byte Reserved3;
        public float MinDepth;
        public float MaxDepth;
        public int MomentShadowMapIndex;
        public int OcclusionDepthTextureIndex;
        public int OcclusionDepthCommandListIndex;
        public int Reserved4;
        public int Reserved5;
    }

    internal struct ShaderLight
    {
        public Vector3 WorldSpacePosition;
        public Vector3 Color;
        public int Camera1;
        public int Camera2;
        public int Camera3;
        public int Camera4;
        public int LightType;
    }

    internal struct ShaderMaterial
    {
        public int MaterialBufferIndex;
        public int MaterialTextureOffset;
        public bool IsTransparent;
        public byte Reserved1;
        public byte Reserved2;
        public byte Reserved3;
    }

    internal struct ShaderSceneProperties
    {
        public int ActiveCameraIndex;
        public int DebugCameraIndex;
        public int LightCount;
        public bool IsDebugCameraActive;
    }

    internal struct ShaderGeometryPacket
    {
        public int VertexBufferIndex;
        public int IndexBufferIndex;
    }

    internal struct ShaderGeometryInstance
    {
        public int GeometryPacketIndex;
        public int StartIndex;
        public int IndexCount;
        public int MaterialIndex;
        public Matrix4x4 WorldMatrix;
        public ShaderBoundingBox WorldBoundingBox;
    }

    public abstract class GraphicsPipelineParameter
    {
        protected GraphicsPipelineParameter(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
        public abstract T Evaluate<T>();
    }

    public class ResourceGraphicsPipelineParameter : GraphicsPipelineParameter
    {
        public ResourceGraphicsPipelineParameter(string name, IGraphicsResource graphicsResource) : base(name)
        {
            this.GraphicsResource = graphicsResource;
        }

        public IGraphicsResource GraphicsResource { get; }

        // TODO: Do something better here
        public override T Evaluate<T>()
        {
            return (T)this.GraphicsResource;
        }
    }

    public class Vector4GraphicsPipelineParameter : GraphicsPipelineParameter
    {
        public Vector4GraphicsPipelineParameter(string name, Vector4 value) : base(name)
        {
            this.Value = value;
        }

        public Vector4 Value { get; }

        // TODO: Do something better here
        public override T Evaluate<T>()
        {
            return (T)(object)this.Value;
        }
    }

    public class IntGraphicsPipelineParameter : GraphicsPipelineParameter
    {
        public IntGraphicsPipelineParameter(string name, int value) : base(name)
        {
            this.Value = value;
        }

        public int Value { get; }

        // TODO: Do something better here
        public override T Evaluate<T>()
        {
            return (T)(object)this.Value;
        }
    }

    public class BindingGraphicsPipelineParameter<T> : GraphicsPipelineParameter
    {
        public BindingGraphicsPipelineParameter(string name, GraphicsPipelineParameterBinding<T> value) : base(name)
        {
            this.Value = value;
        }

        public GraphicsPipelineParameterBinding<T> Value { get; }

        // TODO: Do something better here
        public override TReturn Evaluate<TReturn>()
        {
            return (TReturn)(object)this.Value;
        }
    }

    public abstract class GraphicsPipelineResourceDeclaration
    {
        protected GraphicsPipelineResourceDeclaration(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }

    public class GraphicsPipelineTextureResourceDeclaration : GraphicsPipelineResourceDeclaration
    {
        public GraphicsPipelineTextureResourceDeclaration(string name, TextureFormat textureFormat, IRenderPipelineParameterBinding<int> width, IRenderPipelineParameterBinding<int> height) : base(name)
        {
            this.TextureFormat = textureFormat;
            this.Width = width;
            this.Height = height;
        }

        public TextureFormat TextureFormat { get; }
        public IRenderPipelineParameterBinding<int> Width { get; }
        public IRenderPipelineParameterBinding<int> Height { get; }
    }

    public class GraphicsPipelineResourceBinding
    {
        public GraphicsPipelineResourceBinding(IRenderPipelineParameterBinding<IGraphicsResource> pipelineResource)
        {
            this.PipelineResource = pipelineResource;
        }

        public IRenderPipelineParameterBinding<IGraphicsResource> PipelineResource { get; }
    }

    public class DepthGraphicsPipelineResourceBinding : GraphicsPipelineResourceBinding
    {
        public DepthGraphicsPipelineResourceBinding(IRenderPipelineParameterBinding<IGraphicsResource> pipelineResource, IRenderPipelineParameterBinding<DepthBufferOperation> depthBufferOperation) : base(pipelineResource)
        {
            this.DepthBufferOperation = depthBufferOperation;
        }

        public IRenderPipelineParameterBinding<DepthBufferOperation> DepthBufferOperation { get; }
    }

    public class IndirectCommandBufferGraphicsPipelineResourceBinding : GraphicsPipelineResourceBinding
    {
        public IndirectCommandBufferGraphicsPipelineResourceBinding(IRenderPipelineParameterBinding<IGraphicsResource> pipelineResource, IRenderPipelineParameterBinding<int> maxCommandCount) : base(pipelineResource)
        {
            this.MaxCommandCount = maxCommandCount;
        }

        public IRenderPipelineParameterBinding<int> MaxCommandCount { get; }
    }

    public class RenderTargetGraphicsPipelineResourceBinding : GraphicsPipelineResourceBinding
    {
        public RenderTargetGraphicsPipelineResourceBinding(IRenderPipelineParameterBinding<IGraphicsResource> pipelineResource, IRenderPipelineParameterBinding<int> bindingSlot, IRenderPipelineParameterBinding<Vector4>? clearColor, IRenderPipelineParameterBinding<BlendOperation>? blendOperation = null) : base(pipelineResource)
        {
            this.BindingSlot = bindingSlot;
            this.ClearColor = clearColor;
            this.BlendOperation = blendOperation;
        }

        public IRenderPipelineParameterBinding<int> BindingSlot { get; }
        public IRenderPipelineParameterBinding<Vector4>? ClearColor { get; }
        public IRenderPipelineParameterBinding<BlendOperation>? BlendOperation { get; }
    }

    public class ShaderGraphicsPipelineResourceBinding : GraphicsPipelineResourceBinding
    {
        public ShaderGraphicsPipelineResourceBinding(IRenderPipelineParameterBinding<IGraphicsResource> pipelineResource, IRenderPipelineParameterBinding<int> shaderBindingSlot) : base(pipelineResource)
        {
            this.ShaderBindingSlot = shaderBindingSlot;
        }

        public IRenderPipelineParameterBinding<int> ShaderBindingSlot { get; }
    }

    public interface IRenderPipelineParameterBinding<T>
    {
        T Evaluate(GraphicsPipeline pipeline);
    }

    // TODO: Split that class into 2 separate classes?
    public class GraphicsPipelineParameterBinding<T> : IRenderPipelineParameterBinding<T>
    {
        public GraphicsPipelineParameterBinding(IGraphicsResource pipelineResource, string? property = null)
        {
            this.PipelineResource = pipelineResource;
            this.Property = property;
        }

        public GraphicsPipelineParameterBinding(string resourceName, string? property = null)
        {
            this.ResourceName = resourceName;
            this.Property = property;
        }

        public IGraphicsResource? PipelineResource { get; }
        public string? ResourceName { get; }
        public string? Property { get; }

        // TODO: Replace pipeline by pipeline context so we can run multiple pipelines concurrently
        public T Evaluate(GraphicsPipeline pipeline)
        {
            IGraphicsResource? resource;

            if (this.PipelineResource != null)
            {
                resource = this.PipelineResource;
            }

            else if (this.ResourceName != null)
            {
                resource = pipeline.ResolveResource(this.ResourceName);
            }

            else
            {
                throw new InvalidOperationException("Cannot evaluate parameter binding.");
            }

            if (this.Property == null)
            {
                if (resource != null)
                {
                    return (T)resource;
                }

                // TODO: Avoid reflection
                if (typeof(T) == typeof(Vector4))
                {
                    return (T)(object)pipeline.ResolveVector4(this.ResourceName);
                }

                else if (typeof(T) == typeof(int))
                {
                    return (T)(object)pipeline.ResolveInt(this.ResourceName);
                }

                throw new InvalidOperationException($"Could not resolve parameter '{this.ResourceName}'");
            }

            else if (resource != null && resource.ResourceType == GraphicsResourceType.Texture)
            {
                // TODO: Use switch expression
                if (this.Property == "Width")
                {
                    // TODO: Avoid that!
                    return (T)(object)((Texture)resource).Width;
                }

                else if (this.Property == "Height")
                {
                    // TODO: Avoid that!
                    return (T)(object)((Texture)resource).Height;
                }
            }

            throw new InvalidOperationException("Cannot evaluate parameter binding.");
        }
    }

    // TODO: Performance issue here because the instance will be boxed to interface type
    public readonly struct ConstantPipelineParameterBinding<T> : IRenderPipelineParameterBinding<T> where T : struct
    {
        public ConstantPipelineParameterBinding(T value)
        {
            this.Value = value;
        }

        public T Value { get; }

        public T Evaluate(GraphicsPipeline pipeline)
        {
            return this.Value;
        }
    }

    // TODO: Add an execute pipeline step
    public abstract class GraphicsPipelineStep
    {
        protected GraphicsPipelineStep(string name, string? shaderPath = null, ReadOnlyMemory<GraphicsPipelineResourceBinding>? inputs = null, ReadOnlyMemory<GraphicsPipelineResourceBinding>? outputs = null)
        {
            this.Name = name;
            this.ShaderPath = shaderPath;
            this.Inputs = inputs;
            this.Outputs = outputs;
        }

        public string Name { get; }
        public string? ShaderPath { get; }
        public ReadOnlyMemory<GraphicsPipelineResourceBinding>? Inputs { get; }
        public ReadOnlyMemory<GraphicsPipelineResourceBinding>? Outputs { get; }
        internal Shader? Shader { get; set; }
        internal CommandBuffer? CommandBuffer { get; set; }

        // TODO: Avoid virtual call by implementing an interface?
        public abstract CommandList Process(GraphicsPipeline pipeline, GraphicsManager graphicsManager, CommandList[] commandListsToWait);
    }

    public class ExecutePipelineStep : GraphicsPipelineStep
    {
        private readonly GraphicsPipeline pipelineToExecute;
        private readonly ReadOnlyMemory<GraphicsPipelineParameter> parameters;

        public ExecutePipelineStep(string name, GraphicsPipeline pipelineToExecute, ReadOnlyMemory<GraphicsPipelineParameter> parameters) : base(name)
        {
            this.pipelineToExecute = pipelineToExecute;
            this.parameters = parameters;
        }

        public override CommandList Process(GraphicsPipeline pipeline, GraphicsManager graphicsManager, CommandList[] commandListsToWait)
        {
            var resolvedParameters = new GraphicsPipelineParameter[this.parameters.Length];

            for (var i = 0; i < this.parameters.Length; i++)
            {
                var parameter = this.parameters.Span[i];
                
                switch(parameter)
                {
                    case BindingGraphicsPipelineParameter<IGraphicsResource> resourceValue:
                        resolvedParameters[i] = new ResourceGraphicsPipelineParameter(parameter.Name, resourceValue.Value.Evaluate(pipeline));
                        break;

                    case BindingGraphicsPipelineParameter<int> intValue:
                        resolvedParameters[i] = new IntGraphicsPipelineParameter(parameter.Name, intValue.Value.Evaluate(pipeline));
                        break;
                }
            }

            // TODO: Refactor the parameter system globally
            return this.pipelineToExecute.Process(commandListsToWait, resolvedParameters);
        }
    }

    public class ComputeMinMaxPipelineStep : GraphicsPipelineStep
    {
        private readonly Shader computeMinMaxDepthInitialShader;
        private readonly Shader computeMinMaxDepthStepShader;

        public ComputeMinMaxPipelineStep(string name, ResourcesManager resourcesManager, ReadOnlyMemory<GraphicsPipelineResourceBinding> inputs, ReadOnlyMemory<GraphicsPipelineResourceBinding> outputs) : base(name, null, inputs, outputs)
        {
            this.computeMinMaxDepthInitialShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeMinMaxDepth.shader", "ComputeMinMaxDepthInitial");
            this.computeMinMaxDepthStepShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeMinMaxDepth.shader", "ComputeMinMaxDepthStep");
        }

        public override CommandList Process(GraphicsPipeline pipeline, GraphicsManager graphicsManager, CommandList[] commandListsToWait)
        {
            if (this.CommandBuffer == null)
            {
                throw new InvalidOperationException($"Command buffer for step '{this.Name}' doesn't exist.");
            }

            // TODO: Aquire 3 resources: Depth Buffer, CamerasBuffer, Local Compute buffer

            graphicsManager.ResetCommandBuffer(this.CommandBuffer.Value);

            var computeCommandList = graphicsManager.CreateComputeCommandList(this.CommandBuffer.Value, "ComputeMinMaxDepthInitial");

            graphicsManager.WaitForCommandLists(computeCommandList, commandListsToWait);

            graphicsManager.SetShader(computeCommandList, this.computeMinMaxDepthInitialShader);

            GraphicsBuffer? camerasBuffer = null;
            Texture? depthBuffer = null;

            if (this.Inputs != null)
            {
                for (var i = 0; i < this.Inputs.Value.Length; i++)
                {
                    var resourceBinding = this.Inputs.Value.Span[i];
                    var pipelineResource = resourceBinding.PipelineResource.Evaluate(pipeline);

                    switch (resourceBinding)
                    {
                        case ShaderGraphicsPipelineResourceBinding shaderResourceBinding when pipelineResource.ResourceType == GraphicsResourceType.Texture:
                            depthBuffer = (Texture)pipelineResource;
                            graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                            break;

                        case ShaderGraphicsPipelineResourceBinding shaderResourceBinding when pipelineResource.ResourceType == GraphicsResourceType.Buffer:
                            var slot = shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline);

                            if (slot == 1)
                            {
                                camerasBuffer = (GraphicsBuffer)pipelineResource;
                            }

                            graphicsManager.SetShaderBuffer(computeCommandList, (GraphicsBuffer)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                            break;
                    }
                }
            }

            if (this.Outputs != null)
            {
                for (var i = 0; i < this.Outputs.Value.Length; i++)
                {
                    var resourceBinding = this.Outputs.Value.Span[i];
                    var pipelineResource = resourceBinding.PipelineResource.Evaluate(pipeline);
                    
                    switch (resourceBinding)
                    {
                        case ShaderGraphicsPipelineResourceBinding shaderResourceBinding when pipelineResource.ResourceType == GraphicsResourceType.Texture:
                            graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                            break;

                        case ShaderGraphicsPipelineResourceBinding shaderResourceBinding when pipelineResource.ResourceType == GraphicsResourceType.Buffer:
                            var slot = shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline);

                            if (slot == 1)
                            {
                                camerasBuffer = (GraphicsBuffer)pipelineResource;
                            }

                            graphicsManager.SetShaderBuffer(computeCommandList, (GraphicsBuffer)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                            break;
                    }
                }
            }

            if (depthBuffer == null)
            {
                throw new ArgumentNullException(nameof(depthBuffer));
            }
          
            var threadGroupSize = graphicsManager.DispatchThreads(computeCommandList, (uint)depthBuffer.Width, (uint)depthBuffer.Height, 1);

            graphicsManager.CommitComputeCommandList(computeCommandList);

            // Logger.WriteMessage("==============================");
            var itemCountToProcessWidth = (uint)depthBuffer.Width;
            var itemCountToProcessHeight = (uint)depthBuffer.Height;
            var itemCountToProcess = (uint)(MathF.Ceiling((float)itemCountToProcessWidth / threadGroupSize.X) * MathF.Ceiling((float)itemCountToProcessHeight / threadGroupSize.Y));

            var previousCommandList = computeCommandList;

            while (itemCountToProcess > 1)
            {
                itemCountToProcessWidth = itemCountToProcess;
                itemCountToProcessHeight = 1;

                var computeCommandStepList = graphicsManager.CreateComputeCommandList(this.CommandBuffer.Value, "ComputeMinMaxDepthStep");

                graphicsManager.WaitForCommandList(computeCommandStepList, previousCommandList);

                // Logger.WriteMessage($"Items to process: {itemCountToProcess} {itemCountToProcessWidth} {itemCountToProcessHeight}");

                // TODO: Use a indirect command buffer to dispatch the correct number of threads
                graphicsManager.SetShader(computeCommandStepList, this.computeMinMaxDepthStepShader);
                
                if (this.Inputs != null)
                {
                    for (var i = 0; i < this.Inputs.Value.Length; i++)
                    {
                        var resourceBinding = this.Inputs.Value.Span[i];
                        var pipelineResource = resourceBinding.PipelineResource.Evaluate(pipeline);

                        switch (resourceBinding)
                        {
                            case ShaderGraphicsPipelineResourceBinding shaderResourceBinding when pipelineResource.ResourceType == GraphicsResourceType.Texture:
                                graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                                break;

                            case ShaderGraphicsPipelineResourceBinding shaderResourceBinding when pipelineResource.ResourceType == GraphicsResourceType.Buffer:
                                graphicsManager.SetShaderBuffer(computeCommandList, (GraphicsBuffer)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                                break;
                        }
                    }
                }

                if (this.Outputs != null)
                {
                    for (var i = 0; i < this.Outputs.Value.Length; i++)
                    {
                        var resourceBinding = this.Outputs.Value.Span[i];
                        var pipelineResource = resourceBinding.PipelineResource.Evaluate(pipeline);
                        
                        switch (resourceBinding)
                        {
                            case ShaderGraphicsPipelineResourceBinding shaderResourceBinding when pipelineResource.ResourceType == GraphicsResourceType.Texture:
                                graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                                break;

                            case ShaderGraphicsPipelineResourceBinding shaderResourceBinding when pipelineResource.ResourceType == GraphicsResourceType.Buffer:
                                graphicsManager.SetShaderBuffer(computeCommandList, (GraphicsBuffer)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                                break;
                        }
                    }
                }

                threadGroupSize = graphicsManager.DispatchThreads(computeCommandStepList, itemCountToProcess, 1, 1);

                graphicsManager.CommitComputeCommandList(computeCommandStepList);

                previousCommandList = computeCommandStepList;

                itemCountToProcess = (uint)(MathF.Ceiling((float)itemCountToProcessWidth / threadGroupSize.X) * MathF.Ceiling((float)itemCountToProcessHeight / threadGroupSize.Y));
                // Logger.WriteMessage($"Items to process: {itemCountToProcess}");
            }

            var copyCommandList = graphicsManager.CreateCopyCommandList(this.CommandBuffer.Value, "CopyCamera");

            graphicsManager.WaitForCommandList(copyCommandList, previousCommandList);

            graphicsManager.CopyGraphicsBufferDataToCpu(copyCommandList, camerasBuffer!.Value, Marshal.SizeOf<ShaderCamera>());
            graphicsManager.CommitCopyCommandList(copyCommandList);

            graphicsManager.ExecuteCommandBuffer(this.CommandBuffer.Value);

            return previousCommandList;
        }
    }

    public class RenderIndirectCommandBufferPipelineStep : GraphicsPipelineStep
    {
        private readonly bool backfaceCulling;

        public RenderIndirectCommandBufferPipelineStep(string name, string shader, ReadOnlyMemory<GraphicsPipelineResourceBinding> inputs, ReadOnlyMemory<GraphicsPipelineResourceBinding> outputs, bool backfaceCulling = true) : base(name, shader, inputs, outputs)
        {
            this.backfaceCulling = backfaceCulling;
        }

        public override CommandList Process(GraphicsPipeline pipeline, GraphicsManager graphicsManager, CommandList[] commandListsToWait)
        {
            if (this.CommandBuffer == null)
            {
                throw new InvalidOperationException($"Command buffer for step '{this.Name}' doesn't exist.");
            }

            // TODO: Throw an error if more that one depth buffer was bound to inputs
            // TODO: Throw an error if no indirect command buffer was bound to inputs
            // TODO: Refactor because a lot of code is shared for render steps

            Texture? depthBufferTexture = null;
            var depthBufferOperation = DepthBufferOperation.None;

            IndirectCommandBuffer? indirectCommandBuffer = null;
            var maxCommandCount = 0;

            RenderTargetDescriptor? renderTarget0 = null;
            RenderTargetDescriptor? renderTarget1 = null;

            if (this.Inputs != null)
            {
                for (var i = 0; i < this.Inputs.Value.Length; i++)
                {
                    var input = this.Inputs.Value.Span[i];

                    // TODO: Recheck the performance of C# pattern matching
                    switch (input)
                    {
                        case DepthGraphicsPipelineResourceBinding depthBufferInput:
                            depthBufferTexture = depthBufferInput.PipelineResource.Evaluate(pipeline) as Texture;
                            depthBufferOperation = depthBufferInput.DepthBufferOperation.Evaluate(pipeline);
                            break;

                        case IndirectCommandBufferGraphicsPipelineResourceBinding indirectCommandBufferInput:
                            indirectCommandBuffer = (IndirectCommandBuffer)indirectCommandBufferInput.PipelineResource.Evaluate(pipeline);
                            maxCommandCount = indirectCommandBufferInput.MaxCommandCount.Evaluate(pipeline);
                            break;
                    }
                }
            }

            if (this.Outputs != null)
            {
                for (var i = 0; i < this.Outputs.Value.Length; i++)
                {
                    var output = this.Outputs.Value.Span[i];

                    // TODO: Recheck the performance of C# pattern matching
                    switch (output)
                    {
                        case DepthGraphicsPipelineResourceBinding depthBufferInput:
                            depthBufferTexture = depthBufferInput.PipelineResource.Evaluate(pipeline) as Texture;
                            depthBufferOperation = depthBufferInput.DepthBufferOperation.Evaluate(pipeline);
                            break;

                        case RenderTargetGraphicsPipelineResourceBinding renderTargetoutput:
                            var clearColor = renderTargetoutput.ClearColor?.Evaluate(pipeline);
                            var blendOperation = renderTargetoutput.BlendOperation?.Evaluate(pipeline);
                            var bindingSlot = renderTargetoutput.BindingSlot.Evaluate(pipeline);

                            // TODO: Implement other binding slots
                            if (bindingSlot == 0)
                            {
                                renderTarget0 = new RenderTargetDescriptor(renderTargetoutput.PipelineResource.Evaluate(pipeline) as Texture, clearColor, blendOperation ?? BlendOperation.None);
                            }

                            else if (bindingSlot == 1)
                            {
                                renderTarget1 = new RenderTargetDescriptor(renderTargetoutput.PipelineResource.Evaluate(pipeline) as Texture, clearColor, blendOperation ?? BlendOperation.None);
                            }
                            break;
                    }
                }
            }

            if (indirectCommandBuffer == null)
            {
                throw new InvalidOperationException("Indirect Command Buffer");
            }

            graphicsManager.ResetCommandBuffer(this.CommandBuffer.Value);

            // TODO: Add Backface Culling parameter
            // TODO: Add BlendOperation parameter

            var renderPassDescriptor = new RenderPassDescriptor(renderTarget0, renderTarget1, depthBufferTexture, depthBufferOperation, this.backfaceCulling);
            var renderCommandList = graphicsManager.CreateRenderCommandList(this.CommandBuffer.Value, renderPassDescriptor, this.Name);

            graphicsManager.WaitForCommandLists(renderCommandList, commandListsToWait);

            graphicsManager.SetShader(renderCommandList, this.Shader);
            graphicsManager.ExecuteIndirectCommandBuffer(renderCommandList, indirectCommandBuffer.Value, maxCommandCount);
            graphicsManager.CommitRenderCommandList(renderCommandList);
            graphicsManager.ExecuteCommandBuffer(this.CommandBuffer.Value);

            return renderCommandList;
        }
    }

    public class ComputeGraphicsPipelineStep : GraphicsPipelineStep
    {
        public ComputeGraphicsPipelineStep(string name, string shader, ReadOnlyMemory<GraphicsPipelineResourceBinding> inputs, ReadOnlyMemory<GraphicsPipelineResourceBinding> outputs, GraphicsPipelineParameterBinding<int>[] threads) : base(name, shader, inputs, outputs)
        {
            this.Threads = threads;
        }

        public GraphicsPipelineParameterBinding<int>[] Threads { get; }

        public override CommandList Process(GraphicsPipeline pipeline, GraphicsManager graphicsManager, CommandList[] commandListsToWait)
        {
            if (this.CommandBuffer == null)
            {
                throw new InvalidOperationException($"Command buffer for step '{this.Name}' doesn't exist.");
            }

            graphicsManager.ResetCommandBuffer(this.CommandBuffer.Value);

            var computeCommandList = graphicsManager.CreateComputeCommandList(this.CommandBuffer.Value, this.Name);

            graphicsManager.WaitForCommandLists(computeCommandList, commandListsToWait);
            graphicsManager.SetShader(computeCommandList, this.Shader);

            if (this.Inputs != null)
            {
                for (var i = 0; i < this.Inputs.Value.Length; i++)
                {
                    var resourceBinding = this.Inputs.Value.Span[i];
                    var pipelineResource = resourceBinding.PipelineResource.Evaluate(pipeline);
                    
                    // TODO: Evaluate is performance is it using reflection? Will it cause issues with CoreRT?
                    if (resourceBinding is ShaderGraphicsPipelineResourceBinding shaderResourceBinding && pipelineResource.ResourceType == GraphicsResourceType.Texture)
                    {
                        graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                    }
                }
            }

            if (this.Outputs != null)
            {
                for (var i = 0; i < this.Outputs.Value.Length; i++)
                {
                    var resourceBinding = this.Outputs.Value.Span[i];
                    var pipelineResource = resourceBinding.PipelineResource.Evaluate(pipeline);
                    
                    if (resourceBinding is ShaderGraphicsPipelineResourceBinding shaderResourceBinding && pipelineResource.ResourceType == GraphicsResourceType.Texture)
                    {
                        graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                    }
                }
            }

            // TODO: avoid allocation
            var threads = new uint[3];

            for (var i = 0; i < this.Threads.Length; i++)
            {
                threads[i] = (uint)this.Threads[i].Evaluate(pipeline);
            }

            graphicsManager.DispatchThreads(computeCommandList, threads[0], threads[1], 1);
            graphicsManager.CommitComputeCommandList(computeCommandList);
            graphicsManager.ExecuteCommandBuffer(this.CommandBuffer.Value);

            return computeCommandList;
        }
    }

    // TODO: Try to split the graph handling (parameters evaluation, etc) and the actual render graph
    public class GraphicsPipeline
    {
        private readonly GraphicsManager graphicsManager;

        // TODO: Move that to context class so that each context has his own privates resources
        // TODO: Move the command buffers for each steps to the context class
        // TODO: Add the parent pipeline to the context class
        private IDictionary<string, IGraphicsResource> resources;
        private IDictionary<string, IGraphicsResource> localResources;
        private IDictionary<string, Vector4> localVector4List;
        private IDictionary<string, int> localIntList;

        public GraphicsPipeline(GraphicsManager graphicsManager, ResourcesManager resourcesManager, ReadOnlyMemory<GraphicsPipelineResourceDeclaration> resourceDeclarations, ReadOnlyMemory<GraphicsPipelineStep> steps)
        {
            this.graphicsManager = graphicsManager;
            this.ResourceDeclarations = resourceDeclarations;
            this.Steps = steps;

            for (var i = 0; i < this.Steps.Length; i++)
            {
                var step = this.Steps.Span[i];
                
                if (step.ShaderPath != null)
                {
                    var shaderParts = step.ShaderPath.Split('@');
                    step.Shader = resourcesManager.LoadResourceAsync<Shader>(shaderParts[0], shaderParts.Length > 1 ? shaderParts[1] : null);
                }
                
                if (step.GetType() == typeof(RenderIndirectCommandBufferPipelineStep))
                {
                    step.CommandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Render, step.Name);
                }

                else
                {
                    step.CommandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Compute, step.Name);
                }
            }

            this.resources = new Dictionary<string, IGraphicsResource>();
            this.localResources = new Dictionary<string, IGraphicsResource>();
            this.localVector4List = new Dictionary<string, Vector4>();
            this.localIntList = new Dictionary<string, int>();
        }

        public ReadOnlyMemory<GraphicsPipelineResourceDeclaration> ResourceDeclarations { get; }
        public ReadOnlyMemory<GraphicsPipelineStep> Steps { get; }

        public CommandList Process(CommandList[] commandListsToWait, ReadOnlyMemory<GraphicsPipelineParameter> parameters)
        {
            this.resources.Clear();
            this.localVector4List.Clear();
            this.localIntList.Clear();

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters.Span[i];

                switch (parameter)
                {
                    case ResourceGraphicsPipelineParameter:
                        this.resources.Add(parameter.Name, parameter.Evaluate<IGraphicsResource>());
                        break;

                    case Vector4GraphicsPipelineParameter:
                        this.localVector4List.Add(parameter.Name, parameter.Evaluate<Vector4>());
                        break;

                    case IntGraphicsPipelineParameter:
                        this.localIntList.Add(parameter.Name, parameter.Evaluate<int>());
                        break;
                }
            }

            for (var i = 0; i < this.ResourceDeclarations.Length; i++)
            {
                var resourceDeclaration = this.ResourceDeclarations.Span[i];

                if (!this.localResources.ContainsKey(resourceDeclaration.Name))
                {
                    if (resourceDeclaration is GraphicsPipelineTextureResourceDeclaration textureDeclaration)
                    {
                        Logger.WriteMessage($"Creating pipeline resource '{textureDeclaration.Name}'...");

                        var texture = this.graphicsManager.CreateTexture(textureDeclaration.TextureFormat, textureDeclaration.Width.Evaluate(this), textureDeclaration.Height.Evaluate(this), 1, 1, 1, true, isStatic: true, label: textureDeclaration.Name);
                        this.localResources.Add(textureDeclaration.Name, texture);
                    }
                }

                else
                {
                    if (resourceDeclaration is GraphicsPipelineTextureResourceDeclaration textureDeclaration)
                    {
                        var texture = (Texture)this.localResources[resourceDeclaration.Name];
                        var width = textureDeclaration.Width.Evaluate(this);
                        var height = textureDeclaration.Height.Evaluate(this);

                        if (texture.Width != width || texture.Height != height)
                        {
                            Logger.WriteMessage($"Resizing pipeline resource '{textureDeclaration.Name}'...");
                            this.graphicsManager.ResizeTexture(texture, width, height);
                        }
                    }
                }
            }

            CommandList? commandList = null;

            for (var i = 0; i < this.Steps.Span.Length; i++)
            {
                var step = this.Steps.Span[i];
                commandList = step.Process(this, this.graphicsManager, commandList == null ? commandListsToWait : new CommandList[] { commandList.Value });
            }

            return commandList!.Value;
        }

        // TODO: Abstract the resolve parameter to take resources or constants
        public IGraphicsResource? ResolveResource(string resourceName)
        {
            if (this.resources.ContainsKey(resourceName))
            {
                return this.resources[resourceName];
            }

            else if (this.localResources.ContainsKey(resourceName))
            {
                return this.localResources[resourceName];
            }

            return null;
        }

        public int ResolveInt(string resourceName)
        {
            if (this.localIntList.ContainsKey(resourceName))
            {
                return this.localIntList[resourceName];
            }

            return 0;
        }

        public Vector4 ResolveVector4(string resourceName)
        {
            if (this.localVector4List.ContainsKey(resourceName))
            {
                return this.localVector4List[resourceName];
            }

            return Vector4.Zero;
        }
    }

    // TODO: Add a render pipeline system to have a data oriented configuration of the render pipeline
    public class GraphicsSceneRenderer
    {
        private readonly RenderManager renderManager;
        private readonly GraphicsManager graphicsManager;
        private readonly DebugRenderer debugRenderer;
        private readonly GraphicsSceneQueue sceneQueue;

        private Shader drawMeshInstancesComputeShader;
        private Shader computeLightCamerasShader;
        private Shader convertToMomentShadowMapShader;

        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private RenderPassConstants renderPassConstants;

        private Texture cubeMap;
        private Texture irradianceCubeMap;

        private readonly int multisampleCount = 1;

        // Compute shaders data structures
        private GraphicsBuffer scenePropertiesBuffer;
        private GraphicsBuffer camerasBuffer;
        private GraphicsBuffer lightsBuffer;
        private GraphicsBuffer materialsBuffer;
        private GraphicsBuffer geometryPacketsBuffer;
        private GraphicsBuffer geometryInstancesBuffer;
        private GraphicsBuffer indirectCommandBufferCounters;

        private GraphicsBuffer minMaxDepthComputeBuffer;

        private CommandBuffer copyCommandBuffer;
        private CommandBuffer resetIcbCommandBuffer;
        private CommandBuffer generateIndirectCommandsCommandBuffer;
        private CommandBuffer generateIndirectCommandsCommandBuffer2;
        private CommandBuffer[] generateDepthBufferCommandBuffers;
        private CommandBuffer[] convertToMomentShadowMapCommandBuffers;
        private CommandBuffer computeLightsCamerasCommandBuffer;

        private GraphicsPipeline depthGraphicsPipeline;
        private GraphicsPipeline graphicsPipeline;

        public GraphicsSceneRenderer(RenderManager renderManager, GraphicsManager graphicsManager, GraphicsSceneQueue sceneQueue, ResourcesManager resourcesManager)
        {
            if (renderManager == null)
            {
                throw new ArgumentNullException(nameof(renderManager));
            }

            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            this.renderManager = renderManager;
            this.graphicsManager = graphicsManager;
            this.debugRenderer = new DebugRenderer(graphicsManager, renderManager, resourcesManager);
            this.sceneQueue = sceneQueue;

            this.drawMeshInstancesComputeShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeGenerateIndirectCommands.shader", "GenerateIndirectCommands");
            this.computeLightCamerasShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeLightCameras.shader", "ComputeLightCameras");
            this.convertToMomentShadowMapShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ConvertToMomentShadowMap.shader");

            this.minMaxDepthComputeBuffer = this.graphicsManager.CreateGraphicsBuffer<Vector2>(10000, isStatic: true, isWriteOnly: true, label: "ComputeMinMaxDepthWorkingBuffer");

            this.renderPassConstants = new RenderPassConstants();
            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants>(1, isStatic: false, isWriteOnly: true, label: "RenderPassConstantBufferOld");

            this.cubeMap = resourcesManager.LoadResourceAsync<Texture>("/BistroV4/san_giuseppe_bridge_4k_cubemap.texture");
            this.irradianceCubeMap = resourcesManager.LoadResourceAsync<Texture>("/BistroV4/san_giuseppe_bridge_4k_irradiance_cubemap.texture");

            // Compute buffers
            this.scenePropertiesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderSceneProperties>(1, isStatic: false, isWriteOnly: true, label: "ComputeSceneProperties");
            this.camerasBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderCamera>(10000, isStatic: false, isWriteOnly: false, label: "ComputeCameras");
            this.lightsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderLight>(10000, isStatic: false, isWriteOnly: true, label: "ComputeLights");
            this.materialsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderLight>(10000, isStatic: false, isWriteOnly: true, label: "ComputeMaterials");
            this.geometryPacketsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryPacket>(10000, isStatic: false, isWriteOnly: true, label: "ComputeGeometryPackets");
            this.geometryInstancesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryInstance>(100000, isStatic: false, isWriteOnly: true, label: "ComputeGeometryInstances");
            this.indirectCommandBufferCounters = this.graphicsManager.CreateGraphicsBuffer<uint>(100, isStatic: false, isWriteOnly: false, label: "ComputeIndirectCommandBufferCounters");

            // Command Buffers
            this.copyCommandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Copy, "CopySceneDataToGpu");
            this.resetIcbCommandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Copy, "ResetIndirectCommandBuffers");
            this.generateIndirectCommandsCommandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Compute, "GenerateIndirectCommands");
            this.generateIndirectCommandsCommandBuffer2 = this.graphicsManager.CreateCommandBuffer(CommandListType.Compute, "GenerateIndirectCommands");

            this.generateDepthBufferCommandBuffers = new CommandBuffer[5];
            this.convertToMomentShadowMapCommandBuffers = new CommandBuffer[5];

            for (var i = 0; i < 5; i++)
            {
                this.generateDepthBufferCommandBuffers[i] = this.graphicsManager.CreateCommandBuffer(CommandListType.Render, "GenerateDepthBuffer");
                this.convertToMomentShadowMapCommandBuffers[i] = this.graphicsManager.CreateCommandBuffer(CommandListType.Render, "ConvertToMomentShadowMap");
            }

            this.computeLightsCamerasCommandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Compute, "ComputeLightsCameras");

            // TEST Pipeline definition

            // TODO: Define parameters so the pipeline can check what is passed to the process method
            // var graphicsPipelineParameters = new GraphicsPipelineParameter[]
            // {
            //     new 
            // };

            var depthGraphicsPipelineResourceDeclarations = new GraphicsPipelineResourceDeclaration[]
            {

            };

            var depthGraphicsPipelineSteps = new GraphicsPipelineStep[]
            {
                new RenderIndirectCommandBufferPipelineStep("GenerateDepthBufferOpaque",
                                                            "/System/Shaders/RenderMeshInstanceDepth.shader",
                                                            new GraphicsPipelineResourceBinding[]
                                                            {
                                                                new IndirectCommandBufferGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthIndirectCommandBuffer"), new GraphicsPipelineParameterBinding<int>("GeometryInstanceCount"))
                                                            },
                                                            new GraphicsPipelineResourceBinding[]
                                                            {
                                                                new DepthGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthBuffer"), new ConstantPipelineParameterBinding<DepthBufferOperation>(DepthBufferOperation.ClearWrite)),
                                                            }),
                new RenderIndirectCommandBufferPipelineStep("GenerateDepthBufferTransparent",
                                                            "/System/Shaders/RenderMeshInstanceTransparentDepth.shader",
                                                            new GraphicsPipelineResourceBinding[]
                                                            {
                                                                new IndirectCommandBufferGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraTransparentDepthIndirectCommandBuffer"), new GraphicsPipelineParameterBinding<int>("GeometryInstanceCount"))
                                                            },
                                                            new GraphicsPipelineResourceBinding[]
                                                            {
                                                                new DepthGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthBuffer"), new ConstantPipelineParameterBinding<DepthBufferOperation>(DepthBufferOperation.Write)),
                                                            },
                                                            backfaceCulling: false)
            };

            this.depthGraphicsPipeline = new GraphicsPipeline(this.graphicsManager, resourcesManager, depthGraphicsPipelineResourceDeclarations, depthGraphicsPipelineSteps);

            var graphicsPipelineResourceDeclarations = new GraphicsPipelineResourceDeclaration[]
            {
                new GraphicsPipelineTextureResourceDeclaration("MainCameraDepthBuffer", TextureFormat.Depth32Float, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
                new GraphicsPipelineTextureResourceDeclaration("OpaqueHdrRenderTarget", TextureFormat.Rgba16Float, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
                new GraphicsPipelineTextureResourceDeclaration("TransparentHdrRenderTarget", TextureFormat.Rgba16Float, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
                new GraphicsPipelineTextureResourceDeclaration("TransparentRevealageRenderTarget", TextureFormat.R16Float, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
                new GraphicsPipelineTextureResourceDeclaration("ResolveRenderTarget", TextureFormat.Rgba16Float, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height"))
            };

            var graphicsPipelineSteps = new GraphicsPipelineStep[]
            {
                new ExecutePipelineStep("GenerateDepthBuffer",
                                        this.depthGraphicsPipeline,
                                        new GraphicsPipelineParameter[]
                                        {
                                            new BindingGraphicsPipelineParameter<IGraphicsResource>("MainCameraDepthBuffer", new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthBuffer")),
                                            new BindingGraphicsPipelineParameter<IGraphicsResource>("MainCameraDepthIndirectCommandBuffer", new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthIndirectCommandBuffer")),
                                            new BindingGraphicsPipelineParameter<IGraphicsResource>("MainCameraTransparentDepthIndirectCommandBuffer", new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraTransparentDepthIndirectCommandBuffer")),
                                            new BindingGraphicsPipelineParameter<int>("GeometryInstanceCount", new GraphicsPipelineParameterBinding<int>("GeometryInstanceCount"))
                                        }),
                new ComputeMinMaxPipelineStep("ComputeMinMaxDepth",
                                                            resourcesManager,
                                                            new GraphicsPipelineResourceBinding[]
                                                            {
                                                                new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthBuffer"), new ConstantPipelineParameterBinding<int>(0)),
                                                                new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MinMaxDepthComputeBuffer"), new ConstantPipelineParameterBinding<int>(2))
                                                            },
                                                            new GraphicsPipelineResourceBinding[]
                                                            {
                                                                new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("CamerasBuffer"), new ConstantPipelineParameterBinding<int>(1))
                                                            }),
                new RenderIndirectCommandBufferPipelineStep("RenderOpaqueGeometry",
                                                            "/System/Shaders/RenderMeshInstance.shader",
                                                            new GraphicsPipelineResourceBinding[]
                                                            {
                                                                new DepthGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthBuffer"), new ConstantPipelineParameterBinding<DepthBufferOperation>(DepthBufferOperation.CompareEqual)),
                                                                new IndirectCommandBufferGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraIndirectCommandBuffer"), new GraphicsPipelineParameterBinding<int>("GeometryInstanceCount"))
                                                            },
                                                            new GraphicsPipelineResourceBinding[]
                                                            {
                                                                new RenderTargetGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("OpaqueHdrRenderTarget"), new ConstantPipelineParameterBinding<int>(0), new GraphicsPipelineParameterBinding<Vector4>("ClearColor"))
                                                            }),
                new RenderIndirectCommandBufferPipelineStep("RenderTransparentGeometry",
                                                            "/System/Shaders/RenderMeshInstanceTransparent.shader",
                                                            new GraphicsPipelineResourceBinding[]
                                                            {
                                                                new DepthGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthBuffer"), new ConstantPipelineParameterBinding<DepthBufferOperation>(DepthBufferOperation.CompareLess)),
                                                                new IndirectCommandBufferGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraTransparentIndirectCommandBuffer"), new GraphicsPipelineParameterBinding<int>("GeometryInstanceCount"))
                                                            },
                                                            new GraphicsPipelineResourceBinding[]
                                                            {
                                                                new RenderTargetGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("TransparentHdrRenderTarget"), new ConstantPipelineParameterBinding<int>(0), new ConstantPipelineParameterBinding<Vector4>(new Vector4(0, 0, 0, 0)), new ConstantPipelineParameterBinding<BlendOperation>(BlendOperation.AddOneOne)),
                                                                new RenderTargetGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("TransparentRevealageRenderTarget"), new ConstantPipelineParameterBinding<int>(1), new ConstantPipelineParameterBinding<Vector4>(new Vector4(1, 0, 0, 0)), new ConstantPipelineParameterBinding<BlendOperation>(BlendOperation.AddOneMinusSourceColor))
                                                            },
                                                            backfaceCulling: false),
                new ComputeGraphicsPipelineStep("Resolve", 
                                                "/System/Shaders/ResolveCompute.shader@Resolve",
                                                new GraphicsPipelineResourceBinding[]
                                                {
                                                    new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("OpaqueHdrRenderTarget"), new ConstantPipelineParameterBinding<int>(0)),
                                                    new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("TransparentHdrRenderTarget"), new ConstantPipelineParameterBinding<int>(1)),
                                                    new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("TransparentRevealageRenderTarget"), new ConstantPipelineParameterBinding<int>(2))
                                                }, 
                                                new GraphicsPipelineResourceBinding[]
                                                {
                                                    new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("ResolveRenderTarget"), new ConstantPipelineParameterBinding<int>(3))
                                                }, 
                                                new GraphicsPipelineParameterBinding<int>[]
                                                {  
                                                    new GraphicsPipelineParameterBinding<int>("ResolveRenderTarget", "Width"),
                                                    new GraphicsPipelineParameterBinding<int>("ResolveRenderTarget", "Height")
                                                }),
                new ComputeGraphicsPipelineStep("ToneMap", 
                                                "/System/Shaders/ToneMapCompute.shader@ToneMap",
                                                new GraphicsPipelineResourceBinding[]
                                                {
                                                    new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("ResolveRenderTarget"), new ConstantPipelineParameterBinding<int>(0))
                                                }, 
                                                new GraphicsPipelineResourceBinding[]
                                                {
                                                    new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainRenderTarget"), new ConstantPipelineParameterBinding<int>(1))
                                                }, 
                                                new GraphicsPipelineParameterBinding<int>[]
                                                {  
                                                    new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"),
                                                    new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")
                                                })
            };

            this.graphicsPipeline = new GraphicsPipeline(this.graphicsManager, resourcesManager, graphicsPipelineResourceDeclarations, graphicsPipelineSteps);
        }

        public CommandList Render()
        {
            var scene = this.sceneQueue.WaitForNextScene();
            var camera = scene.DebugCamera;

            if (camera == null)
            {
                camera = scene.ActiveCamera;
            }

            if (camera != null)
            {
                SetupCamera(camera);
            }

            // TODO: Move that to render pipeline
            this.debugRenderer.ClearDebugLines();

            InitializeGpuData(scene);

            return RunRenderPipeline();
        }

        ShaderSceneProperties sceneProperties = new ShaderSceneProperties();

        GraphicsBuffer[] graphicsBufferList = new GraphicsBuffer[10000];
        int currentGraphicsBufferIndex = 0;

        ShaderCamera[] cameraList = new ShaderCamera[10000];
        int currentCameraIndex = 0;

        ShaderLight[] lightList = new ShaderLight[10000];
        int currentLightIndex = 0;

        ShaderMaterial[] materialList = new ShaderMaterial[10000];
        Dictionary<uint, int> materialListIndexes = new Dictionary<uint, int>();
        int currentMaterialIndex = 0;

        ShaderGeometryPacket[] geometryPacketList = new ShaderGeometryPacket[10000];
        int currentGeometryPacketIndex = 0;

        ShaderGeometryInstance[] geometryInstanceList = new ShaderGeometryInstance[100000];
        int currentGeometryInstanceIndex = 0;

        Texture[] textureList = new Texture[10000];
        int currentTextureIndex = 0;

        Texture[] cubeTextureList = new Texture[10000];
        int currentCubeTextureIndex = 0;

        Texture[] shadowMaps = new Texture[100];
        int currentShadowMapIndex = 0;

        IndirectCommandBuffer[] indirectCommandBufferList = new IndirectCommandBuffer[100];
        int currentIndirectCommandBufferIndex = 0;

        private int AddIndirectCommandBuffer()
        {
            if (this.indirectCommandBufferList[this.currentIndirectCommandBufferIndex].GraphicsResourceId == 0)
            {
                this.indirectCommandBufferList[this.currentIndirectCommandBufferIndex] = this.graphicsManager.CreateIndirectCommandBuffer(1000, isStatic: false, label: "ComputeIndirectLightCommandList");
                Logger.WriteMessage("Create Indirect Buffer");
            }

            return this.currentIndirectCommandBufferIndex++;
        }

        private int AddTexture(Texture texture)
        {
            this.textureList[this.currentTextureIndex] = texture;
            return this.currentTextureIndex++;
        }

        private int AddCubeTexture(Texture texture)
        {
            this.cubeTextureList[this.currentCubeTextureIndex] = texture;
            return this.currentCubeTextureIndex++;
        }

        private int AddShadowMap(int shadowMapSize, bool isMomentShadowMap)
        {
            // TODO: Use resource aliasing
            if (this.shadowMaps[this.currentShadowMapIndex] == null)
            {
                if (!isMomentShadowMap)
                {
                    Logger.WriteMessage("Create Shadow map");
                    this.shadowMaps[this.currentShadowMapIndex] = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, shadowMapSize, shadowMapSize, 1, 1, 1, true, isStatic: false, label: "ShadowMapDepth");
                }

                else
                {
                    Logger.WriteMessage("Create Moment Shadow map");
                    this.shadowMaps[this.currentShadowMapIndex] = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Unorm, shadowMapSize, shadowMapSize, 1, 1, 1, true, isStatic: false, label: "ShadowMapMoment");
                }
            }

            return this.currentShadowMapIndex++;
        }

        private int AddCamera(ShaderCamera camera, Texture? depthTexture, Texture? momentShadowMap, Texture? occlusionDepthTexture)
        {
            this.cameraList[this.currentCameraIndex] = camera;

            if (!Matrix4x4.Invert(this.cameraList[this.currentCameraIndex].ViewProjectionMatrix, out this.cameraList[this.currentCameraIndex].ViewProjectionMatrixInverse))
            {
                //Logger.WriteMessage($"Camera Error");
            }

            if (depthTexture != null)
            {
            this.cameraList[this.currentCameraIndex].DepthBufferTextureIndex = AddTexture(depthTexture);
            }

            if (momentShadowMap != null)
            {
                this.cameraList[this.currentCameraIndex].MomentShadowMapIndex = AddTexture(momentShadowMap);
            }

            if (occlusionDepthTexture != null)
            {
                this.cameraList[this.currentCameraIndex].OcclusionDepthTextureIndex = AddTexture(occlusionDepthTexture);
            }

            this.cameraList[this.currentCameraIndex].OpaqueDepthCommandListIndex = AddIndirectCommandBuffer();
            this.cameraList[this.currentCameraIndex].TransparentDepthCommandListIndex = AddIndirectCommandBuffer();
            this.cameraList[this.currentCameraIndex].OcclusionDepthCommandListIndex = AddIndirectCommandBuffer();

            if (!camera.DepthOnly)
            {
                this.cameraList[this.currentCameraIndex].OpaqueCommandListIndex = AddIndirectCommandBuffer();
                this.cameraList[this.currentCameraIndex].TransparentCommandListIndex = AddIndirectCommandBuffer();
            }

            return this.currentCameraIndex++;
        }

        private int AddLight(ShaderLight light)
        {
            if (light.LightType == 1)
            {
                // var shadowMapSize = 1024;
                var shadowMapSize = 2048;
                var cascadeCount = 4;

                for (var i = 0; i < cascadeCount; i++)
                {
                    ref var lightCameraIndex = ref light.Camera1;

                    switch (i)
                    {
                        case 1:
                            lightCameraIndex = ref light.Camera2;
                            break;
                        case 2:
                            lightCameraIndex = ref light.Camera3;
                            break;
                        case 3:
                            lightCameraIndex = ref light.Camera4;
                            break;
                    }

                    var lightCamera = new ShaderCamera();
                    lightCamera.DepthOnly = true;
                    lightCamera.WorldPosition = light.WorldSpacePosition;

                    var shadowMapIndex = AddShadowMap(shadowMapSize, false);
                    var momentShadowMapIndex = AddShadowMap(shadowMapSize, true);

                    lightCameraIndex = AddCamera(lightCamera, this.shadowMaps[shadowMapIndex], this.shadowMaps[momentShadowMapIndex], null);

                    //this.debugRenderer.DrawBoundingFrustum(lightCamera1.BoundingFrustum, new Vector3(0, 1, 0));
                }
            }

            this.lightList[this.currentLightIndex] = light;
            return this.currentLightIndex++;
        }

        private int AddMaterial(Material material)
        {
            if (material.MaterialData == null)
            {
                return -1;
            }

            if (this.materialListIndexes.ContainsKey(material.ResourceId))
            {
                return this.materialListIndexes[material.ResourceId];
            }

            this.materialList[this.currentMaterialIndex].MaterialBufferIndex = AddGraphicsBuffer(material.MaterialData.Value);
            this.materialList[this.currentMaterialIndex].MaterialTextureOffset = this.currentTextureIndex;
            this.materialList[this.currentMaterialIndex].IsTransparent = material.IsTransparent;

            var textureList = material.TextureList.Span;

            for (var i = 0; i < textureList.Length; i++)
            {
                AddTexture(textureList[i]);
            }

            this.materialListIndexes.Add(material.ResourceId, this.currentMaterialIndex);
            return this.currentMaterialIndex++;
        }

        private int AddGraphicsBuffer(GraphicsBuffer graphicsBuffer)
        {
            this.graphicsBufferList[this.currentGraphicsBufferIndex] = graphicsBuffer;
            return this.currentGraphicsBufferIndex++;
        }

        private int AddGeometryPacket(GeometryPacket geometryPacket)
        {
            var shaderGeometryPacket = new ShaderGeometryPacket()
            {
                VertexBufferIndex = AddGraphicsBuffer(geometryPacket.VertexBuffer),
                IndexBufferIndex = AddGraphicsBuffer(geometryPacket.IndexBuffer)
            };

            this.geometryPacketList[this.currentGeometryPacketIndex] = shaderGeometryPacket;
            return this.currentGeometryPacketIndex++;
        }

        private void InitializeGpuData(GraphicsScene scene)
        {
            this.currentGraphicsBufferIndex = 0;
            this.currentGeometryPacketIndex = 0;
            this.currentCameraIndex = 0;
            this.currentLightIndex = 0;
            this.currentMaterialIndex = 0;
            this.materialListIndexes.Clear();
            this.currentGeometryInstanceIndex = 0;

            this.currentTextureIndex = 0;
            this.currentCubeTextureIndex = 0;
            this.currentShadowMapIndex = 0;

            this.currentIndirectCommandBufferIndex = 0;

            var currentMeshMaterialIndex = -1;
            var currentGeometryInstanceMaterialIndex = -1;
            var currentVertexBufferId = (uint)0;

            // Fill Data
            if (scene.ActiveCamera != null)
            {
                if (scene.DebugCamera != null)
                {
                    var camera = scene.DebugCamera;

                    var shaderCamera = new ShaderCamera()
                    {
                        WorldPosition = camera.WorldPosition,
                        ViewMatrix = camera.ViewMatrix,
                        ProjectionMatrix = camera.ProjectionMatrix,
                        ViewProjectionMatrix = camera.ViewProjectionMatrix,
                        BoundingFrustum = new ShaderBoundingFrustum(scene.ActiveCamera.BoundingFrustum),
                        MinDepth = camera.NearPlaneDistance,
                        MaxDepth = camera.FarPlaneDistance
                    };

                    sceneProperties.DebugCameraIndex = AddCamera(shaderCamera, null, null, null);
                }

                else
                {
                    var camera = scene.ActiveCamera;

                    var shaderCamera = new ShaderCamera()
                    {
                        WorldPosition = camera.WorldPosition,
                        ViewMatrix = camera.ViewMatrix,
                        ProjectionMatrix = camera.ProjectionMatrix,
                        ViewProjectionMatrix = camera.ViewProjectionMatrix,
                        BoundingFrustum = new ShaderBoundingFrustum(camera.BoundingFrustum),
                        MinDepth = camera.NearPlaneDistance,
                        MaxDepth = camera.FarPlaneDistance
                    };

                    sceneProperties.ActiveCameraIndex = AddCamera(shaderCamera, null, null, null);
                }
            }

            sceneProperties.IsDebugCameraActive = (scene.DebugCamera != null);

            // TEST LIGHT BUFFER
            AddCubeTexture(this.cubeMap);
            AddCubeTexture(this.irradianceCubeMap);

            // var lightDirection = Vector3.Normalize(-new Vector3(0.172f, -0.818f, -0.549f));
            // shaderLight.WorldSpacePosition = Vector3.Normalize(new Vector3(-0.2f, 1.0f, -0.05f));
            // shaderLight.Color = new Vector3(0.95f, 0.91f, 0.84f);

            sceneProperties.LightCount = scene.Lights.Count;

            for (var i = 0; i < scene.Lights.Count; i++)
            {
                var light = scene.Lights[i];

                var shaderLight = new ShaderLight();
    
                shaderLight.WorldSpacePosition = (light.LightType == LightType.Directional) ? Vector3.Normalize(light.WorldPosition) : light.WorldPosition;
                shaderLight.Color = light.Color;
                shaderLight.LightType = (int)light.LightType;

                if (light.LightType == LightType.Point)
                {
                    this.debugRenderer.DrawSphere(light.WorldPosition, 2.0f, 50, new Vector3(1, 0.2f, 0));
                }

                AddLight(shaderLight);
            }

            for (var i = 0; i < scene.MeshInstances.Count; i++)
            {
                var meshInstance = scene.MeshInstances[i];
                var mesh = meshInstance.Mesh;

                for (var j = 0; j < meshInstance.WorldBoundingBoxList.Count; j++)
                {
                    //this.debugRenderer.DrawBoundingBox(meshInstance.WorldBoundingBoxList[j], new Vector3(0, 1, 0));
                }

                if (meshInstance.Material != null)
                {
                    currentMeshMaterialIndex = AddMaterial(meshInstance.Material);
                }

                for (var j = 0; j < mesh.GeometryInstances.Count; j++)
                {
                    if (this.currentGeometryInstanceIndex > 2000)
                    {
                        break;
                    }

                    var geometryInstance = mesh.GeometryInstances[j];
                    var geometryPacket = geometryInstance.GeometryPacket;

                    if (geometryInstance.Material != null)
                    {
                        currentGeometryInstanceMaterialIndex = AddMaterial(geometryInstance.Material);
                    }

                    if (currentVertexBufferId != geometryPacket.VertexBuffer.GraphicsResourceId)
                    {
                        currentVertexBufferId = geometryPacket.VertexBuffer.GraphicsResourceId;
                        AddGeometryPacket(geometryPacket);
                    }

                    var worldBoundingBox = new ShaderBoundingBox();

                    if (meshInstance.WorldBoundingBoxList.Count > j)
                    {
                        worldBoundingBox = new ShaderBoundingBox()
                        {
                            MinPoint = meshInstance.WorldBoundingBoxList[j].MinPoint,
                            MaxPoint = meshInstance.WorldBoundingBoxList[j].MaxPoint
                        };
                    }

                    var shaderGeometryInstance = new ShaderGeometryInstance()
                    {
                        GeometryPacketIndex = this.currentGeometryPacketIndex - 1,
                        StartIndex = geometryInstance.StartIndex,
                        IndexCount = geometryInstance.IndexCount,
                        MaterialIndex = (meshInstance.Material != null) ? currentMeshMaterialIndex : ((geometryInstance.Material != null) ? (currentGeometryInstanceMaterialIndex) : -1),
                        WorldMatrix = meshInstance.WorldMatrix,
                        WorldBoundingBox = worldBoundingBox
                    };

                    geometryInstanceList[this.currentGeometryInstanceIndex++] = shaderGeometryInstance;
                }
            }
        }

        private Queue<uint> previousCopyGpuDataIds = new Queue<uint>();

        private CommandList CopyGpuData()
        {
            // Copy buffers
            var counters = this.graphicsManager.ReadGraphicsBufferData<uint>(this.indirectCommandBufferCounters);
            var opaqueCounterIndex = this.cameraList[0].OpaqueCommandListIndex;
            var transparentCounterIndex = this.cameraList[0].TransparentCommandListIndex;

            if (counters[opaqueCounterIndex] > 0)
            {
                this.renderManager.CulledGeometryInstancesCount = (int)counters[opaqueCounterIndex];
            }

            var mainCamera = this.graphicsManager.ReadGraphicsBufferData<ShaderCamera>(this.camerasBuffer);
            this.renderManager.MainCameraDepth = new Vector2(mainCamera[0].MinDepth, mainCamera[0].MaxDepth);

            this.graphicsManager.ResetCommandBuffer(copyCommandBuffer);

            var copyCommandList = this.graphicsManager.CreateCopyCommandList(copyCommandBuffer, "SceneComputeCopyCommandList");
            this.previousCopyGpuDataIds.Enqueue(copyCommandBuffer.GraphicsResourceId);

            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderCamera>(copyCommandList, this.camerasBuffer, this.cameraList.AsSpan().Slice(0, this.currentCameraIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderSceneProperties>(copyCommandList, this.scenePropertiesBuffer, new ShaderSceneProperties[] { this.sceneProperties });
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderLight>(copyCommandList, this.lightsBuffer, this.lightList.AsSpan().Slice(0, this.currentLightIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderMaterial>(copyCommandList, this.materialsBuffer, this.materialList.AsSpan().Slice(0, this.currentMaterialIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderGeometryPacket>(copyCommandList, this.geometryPacketsBuffer, this.geometryPacketList.AsSpan().Slice(0, this.currentGeometryPacketIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderGeometryInstance>(copyCommandList, this.geometryInstancesBuffer, geometryInstanceList.AsSpan().Slice(0, this.currentGeometryInstanceIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.indirectCommandBufferCounters, new uint[100].AsSpan());
            this.graphicsManager.UploadDataToGraphicsBuffer<RenderPassConstants>(copyCommandList, this.renderPassParametersGraphicsBuffer, new RenderPassConstants[] { this.renderPassConstants });

            this.graphicsManager.CommitCopyCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandBuffer(copyCommandBuffer);

            this.renderManager.GeometryInstancesCount = this.currentGeometryInstanceIndex;
            this.renderManager.MaterialsCount = this.currentMaterialIndex;
            this.renderManager.TexturesCount = this.currentTextureIndex;
            this.renderManager.LightsCount = this.currentLightIndex;

            return copyCommandList;
        }
        
        private CommandList ResetIndirectCommandBuffers(CommandList commandListToWait)
        {
            this.graphicsManager.ResetCommandBuffer(resetIcbCommandBuffer);

            var resetIcbCommandList = this.graphicsManager.CreateCopyCommandList(resetIcbCommandBuffer, "ResetIndirectCommandBuffers");

            this.graphicsManager.WaitForCommandList(resetIcbCommandList, commandListToWait);

            for (var i = 0; i < this.currentIndirectCommandBufferIndex; i++)
            {
                this.graphicsManager.ResetIndirectCommandBuffer(resetIcbCommandList, this.indirectCommandBufferList[i], this.currentGeometryInstanceIndex);
            }

            this.graphicsManager.CommitCopyCommandList(resetIcbCommandList);
            this.graphicsManager.ExecuteCommandBuffer(resetIcbCommandBuffer);

            return resetIcbCommandList;
        }

        private CommandList GenerateIndirectCommands(uint cameraCount, CommandList commandListToWait)
        {
            if (this.currentGeometryInstanceIndex == 0)
            {
                return commandListToWait;
            }

            var commandBuffer = (cameraCount == 1) ? this.generateIndirectCommandsCommandBuffer : this.generateIndirectCommandsCommandBuffer2;
            this.graphicsManager.ResetCommandBuffer(commandBuffer);

            // Encore indirect command lists
            var computeCommandList = this.graphicsManager.CreateComputeCommandList(commandBuffer, "GenerateIndirectCommands");

            this.graphicsManager.WaitForCommandList(computeCommandList, commandListToWait);

            this.graphicsManager.SetShader(computeCommandList, this.drawMeshInstancesComputeShader);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.scenePropertiesBuffer, 0);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.camerasBuffer, 1);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.lightsBuffer, 2);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.materialsBuffer, 3);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.geometryPacketsBuffer, 4);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.geometryInstancesBuffer, 5);
            this.graphicsManager.SetShaderBuffers(computeCommandList, this.graphicsBufferList.AsSpan().Slice(0, this.currentGraphicsBufferIndex), 6);
            this.graphicsManager.SetShaderTextures(computeCommandList, this.textureList.AsSpan().Slice(0, this.currentTextureIndex), 10006);
            this.graphicsManager.SetShaderTextures(computeCommandList, this.cubeTextureList.AsSpan().Slice(0, this.currentCubeTextureIndex), 20006);
            this.graphicsManager.SetShaderIndirectCommandBuffers(computeCommandList, this.indirectCommandBufferList.AsSpan().Slice(0, this.currentIndirectCommandBufferIndex), 30006);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.indirectCommandBufferCounters, 30106);

            this.graphicsManager.DispatchThreads(computeCommandList, (uint)this.currentGeometryInstanceIndex, cameraCount, 1);
            this.graphicsManager.CommitComputeCommandList(computeCommandList);

            // Optimize indirect command lists pass
            var copyCommandList = this.graphicsManager.CreateCopyCommandList(commandBuffer, "ComputeOptimizeRenderCommandList");

            this.graphicsManager.WaitForCommandList(copyCommandList, computeCommandList);

            if (cameraCount == 1)
            {
                this.graphicsManager.OptimizeIndirectCommandBuffer(copyCommandList, this.indirectCommandBufferList[0], this.currentGeometryInstanceIndex);
            }

            else
            {
                for (var i = 1; i < this.currentIndirectCommandBufferIndex; i++)
                {
                    this.graphicsManager.OptimizeIndirectCommandBuffer(copyCommandList, this.indirectCommandBufferList[i], this.currentGeometryInstanceIndex);
                }
            }

            this.graphicsManager.CopyGraphicsBufferDataToCpu(copyCommandList, this.indirectCommandBufferCounters, 4 * Marshal.SizeOf<uint>());
            this.graphicsManager.CopyGraphicsBufferDataToCpu(copyCommandList, this.camerasBuffer, Marshal.SizeOf<ShaderCamera>());
            this.graphicsManager.CommitCopyCommandList(copyCommandList);

            this.graphicsManager.ExecuteCommandBuffer(commandBuffer);

            return copyCommandList;
        }

        int currentDepthCommandBuffer = 0;

        // private CommandList GenerateDepthBuffer(ShaderCamera camera, CommandList commandListToWait)
        // {
        //     this.graphicsManager.ResetCommandBuffer(generateDepthBufferCommandBuffers[currentDepthCommandBuffer]);

        //     var depthRenderPassDescriptor = new RenderPassDescriptor(null, this.textureList[camera.DepthBufferTextureIndex], DepthBufferOperation.ClearWrite, true);
        //     var opaqueCommandList = this.graphicsManager.CreateRenderCommandList(generateDepthBufferCommandBuffers[currentDepthCommandBuffer], depthRenderPassDescriptor, "GenerateDepthBuffer_Opaque");

        //     this.graphicsManager.WaitForCommandList(opaqueCommandList, commandListToWait);

        //     this.graphicsManager.SetShader(opaqueCommandList, this.renderMeshInstancesDepthShader);
        //     this.graphicsManager.ExecuteIndirectCommandBuffer(opaqueCommandList, this.indirectCommandBufferList[camera.OpaqueDepthCommandListIndex], this.currentGeometryInstanceIndex);
        //     this.graphicsManager.CommitRenderCommandList(opaqueCommandList);

        //     depthRenderPassDescriptor = new RenderPassDescriptor(null, this.textureList[camera.DepthBufferTextureIndex], DepthBufferOperation.Write, false);
        //     var transparentCommandList = this.graphicsManager.CreateRenderCommandList(generateDepthBufferCommandBuffers[currentDepthCommandBuffer], depthRenderPassDescriptor, "GenerateDepthBuffer_Transparent");
        //     this.graphicsManager.WaitForCommandList(transparentCommandList, opaqueCommandList);

        //     this.graphicsManager.SetShader(transparentCommandList, this.renderMeshInstancesTransparentDepthShader);
        //     this.graphicsManager.ExecuteIndirectCommandBuffer(transparentCommandList, this.indirectCommandBufferList[camera.TransparentDepthCommandListIndex], this.currentGeometryInstanceIndex);
        //     this.graphicsManager.CommitRenderCommandList(transparentCommandList);

        //     this.graphicsManager.ExecuteCommandBuffer(generateDepthBufferCommandBuffers[currentDepthCommandBuffer]);
        //     this.currentDepthCommandBuffer++;
        //     return transparentCommandList;
        // }

        int currentMomentCommandBuffer = 0;

        private CommandList ConvertToMomentShadowMap(ShaderCamera camera, CommandList commandListToWait)
        {
            this.graphicsManager.ResetCommandBuffer(convertToMomentShadowMapCommandBuffers[currentMomentCommandBuffer]);

            var renderTarget = new RenderTargetDescriptor(this.textureList[camera.MomentShadowMapIndex], null, BlendOperation.None);
            var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            var commandList = this.graphicsManager.CreateRenderCommandList(convertToMomentShadowMapCommandBuffers[currentMomentCommandBuffer], hdrTransferRenderPassDescriptor, "ConvertToMomentShadowMap");

            this.graphicsManager.WaitForCommandList(commandList, commandListToWait);

            this.graphicsManager.SetShader(commandList, this.convertToMomentShadowMapShader);
            this.graphicsManager.SetShaderTexture(commandList, this.textureList[camera.DepthBufferTextureIndex], 0);

            this.graphicsManager.DrawPrimitives(commandList, PrimitiveType.TriangleStrip, 0, 4);

            this.graphicsManager.CommitRenderCommandList(commandList);
            this.graphicsManager.ExecuteCommandBuffer(convertToMomentShadowMapCommandBuffers[currentMomentCommandBuffer]);

            currentMomentCommandBuffer++;

            return commandList;
        }

        private CommandList ComputeLightCameras(CommandList commandListToWait)
        {
            this.graphicsManager.ResetCommandBuffer(computeLightsCamerasCommandBuffer);

            var computeCommandList = this.graphicsManager.CreateComputeCommandList(computeLightsCamerasCommandBuffer, "ComputeLightCameras");

            this.graphicsManager.WaitForCommandList(computeCommandList, commandListToWait);

            this.graphicsManager.SetShader(computeCommandList, this.computeLightCamerasShader);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.lightsBuffer, 0);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.camerasBuffer, 1);

            this.graphicsManager.DispatchThreads(computeCommandList, 1, 4, 1);
            this.graphicsManager.CommitComputeCommandList(computeCommandList);

            this.graphicsManager.ExecuteCommandBuffer(computeLightsCamerasCommandBuffer);

            return computeCommandList;
        }

        private CommandList RunRenderPipeline()
        {
            var mainCamera = this.cameraList[0];
            this.currentDepthCommandBuffer = 0;
            this.currentMomentCommandBuffer = 0;

            var commandList = CopyGpuData();
            commandList = ResetIndirectCommandBuffers(commandList);

            // Generate Main Camera Depth Buffer
            commandList = GenerateIndirectCommands(1, commandList);

            //commandList = GenerateDepthBuffer(mainCamera, commandList);

            // Generate Lights Depth Buffers
            // commandList = ComputeMinMaxDepth(commandList);
            // commandList = ComputeLightCameras(commandList);

            // commandList = GenerateIndirectCommands((uint)this.currentCameraIndex, commandList);
            // var depthCommandLists = new CommandList[this.currentCameraIndex - 1];

            // for (var i = 1; i < this.currentCameraIndex; i++)
            // {
            //     var camera = this.cameraList[i];
            //     depthCommandLists[i - 1] = GenerateDepthBuffer(camera, commandList);
            //     depthCommandLists[i - 1] = ConvertToMomentShadowMap(camera, depthCommandLists[i - 1]);
            // }

            var graphicsPipelineParameters = new GraphicsPipelineParameter[]
            {
                new ResourceGraphicsPipelineParameter("MainRenderTarget", this.renderManager.MainRenderTargetTexture),
                new ResourceGraphicsPipelineParameter("MainCameraIndirectCommandBuffer", this.indirectCommandBufferList[mainCamera.OpaqueCommandListIndex]),
                new ResourceGraphicsPipelineParameter("MainCameraDepthIndirectCommandBuffer", this.indirectCommandBufferList[mainCamera.OpaqueDepthCommandListIndex]),
                new ResourceGraphicsPipelineParameter("MainCameraTransparentIndirectCommandBuffer", this.indirectCommandBufferList[mainCamera.TransparentCommandListIndex]),
                new ResourceGraphicsPipelineParameter("MainCameraTransparentDepthIndirectCommandBuffer", this.indirectCommandBufferList[mainCamera.TransparentDepthCommandListIndex]),
                new Vector4GraphicsPipelineParameter("ClearColor", new Vector4(65 * 50, 135 * 50, 255 * 50, 1.0f) / 255.0f),
                new IntGraphicsPipelineParameter("GeometryInstanceCount", this.currentGeometryInstanceIndex),
                new ResourceGraphicsPipelineParameter("MinMaxDepthComputeBuffer", this.minMaxDepthComputeBuffer),
                new ResourceGraphicsPipelineParameter("CamerasBuffer", this.camerasBuffer)
            };

            commandList = this.graphicsPipeline.Process(new CommandList[] { commandList }, graphicsPipelineParameters);
            
            commandList = this.debugRenderer.Render(this.renderPassParametersGraphicsBuffer, this.graphicsPipeline.ResolveResource("MainCameraDepthBuffer") as Texture, commandList);

            var debugXOffset = this.graphicsManager.GetRenderSize().X - 256;

            // this.renderManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(debugXOffset, 0), new Vector2(debugXOffset + 256, 256), this.shadowMaps[1], true);
            // this.renderManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(debugXOffset, 256), new Vector2(debugXOffset + 256, 512), this.shadowMaps[3], true);
            // this.renderManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(debugXOffset, 512), new Vector2(debugXOffset + 256, 768), this.shadowMaps[5], true);
            // this.renderManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(debugXOffset, 768), new Vector2(debugXOffset + 256, 1024), this.shadowMaps[7], true);
            //this.graphicsManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(0, 0), new Vector2(this.graphicsManager.GetRenderSize().X, this.graphicsManager.GetRenderSize().Y), this.occlusionDepthTexture, true);

            return commandList;
        }

        private void SetupCamera(Camera camera)
        {
            this.renderPassConstants.ViewMatrix = camera.ViewMatrix;
            this.renderPassConstants.ProjectionMatrix = camera.ProjectionMatrix;
        }
    }
}
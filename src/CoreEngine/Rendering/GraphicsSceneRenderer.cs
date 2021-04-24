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
        public GraphicsPipelineTextureResourceDeclaration(string name, TextureFormat textureFormat, TextureUsage usage, IRenderPipelineParameterBinding<int> width, IRenderPipelineParameterBinding<int> height) : base(name)
        {
            this.TextureFormat = textureFormat;
            this.Usage = usage;
            this.Width = width;
            this.Height = height;
        }

        public TextureFormat TextureFormat { get; }
        public TextureUsage Usage { get; }
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
        public string? Property { get; }

        // TODO: Replace pipeline by pipeline context so we can run multiple pipelines concurrently
        public T Evaluate(GraphicsPipeline pipeline)
        {
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

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

        // TODO: Avoid virtual call by implementing an interface?
        public abstract ReadOnlySpan<CommandList> BuildCommandLists(GraphicsPipeline pipeline, RenderManager renderManager, GraphicsManager graphicsManager);
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

        public override ReadOnlySpan<CommandList> BuildCommandLists(GraphicsPipeline pipeline, RenderManager renderManager, GraphicsManager graphicsManager)
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
            return this.pipelineToExecute.BuildCommandLists(resolvedParameters);
        }
    }

    public class ComputeMinMaxPipelineStep : GraphicsPipelineStep
    {
        private readonly Shader computeMinMaxDepthInitialShader;
        private readonly Shader computeMinMaxDepthStepShader;

        public ComputeMinMaxPipelineStep(string name, ResourcesManager resourcesManager, ReadOnlyMemory<GraphicsPipelineResourceBinding> inputs, ReadOnlyMemory<GraphicsPipelineResourceBinding> outputs) : base(name, null, inputs, outputs)
        {
            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.computeMinMaxDepthInitialShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeMinMaxDepth.shader", "ComputeMinMaxDepthInitial");
            this.computeMinMaxDepthStepShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeMinMaxDepth.shader", "ComputeMinMaxDepthStep");
        }

        public override ReadOnlySpan<CommandList> BuildCommandLists(GraphicsPipeline pipeline, RenderManager renderManager, GraphicsManager graphicsManager)
        {
            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            if (renderManager == null)
            {
                throw new ArgumentNullException(nameof(renderManager));
            }

            var result = new List<CommandList>();

            // TODO: Aquire 3 resources: Depth Buffer, CamerasBuffer, Local Compute buffer

            var computeCommandList = graphicsManager.CreateCommandList(renderManager.ComputeCommandQueue, "ComputeMinMaxDepthInitial");

            var startQueryIndex = renderManager.InsertQueryTimestamp(computeCommandList);

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
                throw new InvalidOperationException("Depth buffer is null");
            }
          
            var threadGroupSize = graphicsManager.DispatchThreads(computeCommandList, (uint)depthBuffer.Width, (uint)depthBuffer.Height, 1);

            graphicsManager.CommitCommandList(computeCommandList);
            result.Add(computeCommandList);

            // Logger.WriteMessage("==============================");
            var itemCountToProcessWidth = (uint)depthBuffer.Width;
            var itemCountToProcessHeight = (uint)depthBuffer.Height;
            var itemCountToProcess = (uint)(MathF.Ceiling((float)itemCountToProcessWidth / threadGroupSize.X) * MathF.Ceiling((float)itemCountToProcessHeight / threadGroupSize.Y));

            var previousCommandList = computeCommandList;

            while (itemCountToProcess > 1)
            {
                itemCountToProcessWidth = itemCountToProcess;
                itemCountToProcessHeight = 1;

                var computeCommandStepList = graphicsManager.CreateCommandList(renderManager.ComputeCommandQueue, "ComputeMinMaxDepthStep");

                // TODO: Insert a MemoryBarrier/ResourceBarrier here
                // graphicsManager.WaitForCommandList(computeCommandStepList, previousCommandList);

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

                previousCommandList = computeCommandStepList;

                itemCountToProcess = (uint)(MathF.Ceiling((float)itemCountToProcessWidth / threadGroupSize.X) * MathF.Ceiling((float)itemCountToProcessHeight / threadGroupSize.Y));
                // Logger.WriteMessage($"Items to process: {itemCountToProcess}");

                if (itemCountToProcess < 1)
                {
                    var endQueryIndex = renderManager.InsertQueryTimestamp(computeCommandStepList);
                    renderManager.AddGpuTiming(this.Name, QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);
                }

                graphicsManager.CommitCommandList(computeCommandStepList);
                result.Add(computeCommandStepList);
            }

            // var copyCommandList = graphicsManager.CreateCommandList(renderManager.CopyCommandQueue, "CopyCamera");

            // //graphicsManager.CopyGraphicsBufferDataToCpu(copyCommandList, camerasBuffer!.Value, Marshal.SizeOf<ShaderCamera>());
            
            // graphicsManager.CommitCommandList(copyCommandList);

            // result.Add(copyCommandList);

            // TODO: Avoid the array copy
            return result.ToArray();
        }
    }

    public class RenderIndirectCommandBufferPipelineStep : GraphicsPipelineStep
    {
        private readonly bool backfaceCulling;

        public RenderIndirectCommandBufferPipelineStep(string name, string shader, ReadOnlyMemory<GraphicsPipelineResourceBinding> inputs, ReadOnlyMemory<GraphicsPipelineResourceBinding> outputs, bool backfaceCulling = true) : base(name, shader, inputs, outputs)
        {
            this.backfaceCulling = backfaceCulling;
        }

        public override ReadOnlySpan<CommandList> BuildCommandLists(GraphicsPipeline pipeline, RenderManager renderManager, GraphicsManager graphicsManager)
        {
            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            if (renderManager == null)
            {
                throw new ArgumentNullException(nameof(renderManager));
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

            var renderCommandList = graphicsManager.CreateCommandList(renderManager.RenderCommandQueue, this.Name);

            // TODO: Add Backface Culling parameter
            // TODO: Add BlendOperation parameter

            var renderPassDescriptor = new RenderPassDescriptor(renderTarget0, renderTarget1, depthBufferTexture, depthBufferOperation, this.backfaceCulling, PrimitiveType.Triangle);
            graphicsManager.BeginRenderPass(renderCommandList, renderPassDescriptor);
            var startQueryIndex = renderManager.InsertQueryTimestamp(renderCommandList);

            graphicsManager.SetShader(renderCommandList, this.Shader);
            graphicsManager.ExecuteIndirectCommandBuffer(renderCommandList, indirectCommandBuffer.Value, maxCommandCount);

            var endQueryIndex = renderManager.InsertQueryTimestamp(renderCommandList);
            graphicsManager.EndRenderPass(renderCommandList);
            graphicsManager.CommitCommandList(renderCommandList);

            renderManager.AddGpuTiming(this.Name, QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);

            return new CommandList[] { renderCommandList };
        }
    }

    public class ComputeGraphicsPipelineStep : GraphicsPipelineStep
    {
        public ComputeGraphicsPipelineStep(string name, string shader, ReadOnlyMemory<GraphicsPipelineResourceBinding> inputs, ReadOnlyMemory<GraphicsPipelineResourceBinding> outputs, GraphicsPipelineParameterBinding<int>[] threads) : base(name, shader, inputs, outputs)
        {
            this.Threads = threads;
        }

        private GraphicsPipelineParameterBinding<int>[] Threads { get; }

        public override ReadOnlySpan<CommandList> BuildCommandLists(GraphicsPipeline pipeline, RenderManager renderManager, GraphicsManager graphicsManager)
        {
            if (renderManager == null)
            {
                throw new ArgumentNullException(nameof(renderManager));
            }

            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            var computeCommandList = graphicsManager.CreateCommandList(renderManager.ComputeCommandQueue, this.Name);

            var startQueryIndex = renderManager.InsertQueryTimestamp(computeCommandList);
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
                        graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline), isReadOnly: false);
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
            var endQueryIndex = renderManager.InsertQueryTimestamp(computeCommandList);
            graphicsManager.CommitCommandList(computeCommandList);
            renderManager.AddGpuTiming(this.Name, QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);

            return new CommandList[] { computeCommandList };
        }
    }

    // TODO: Try to split the graph handling (parameters evaluation, etc) and the actual render graph
    public class GraphicsPipeline
    {
        private readonly RenderManager renderManager;
        private readonly GraphicsManager graphicsManager;

        // TODO: Move that to context class so that each context has his own privates resources
        // TODO: Move the command buffers for each steps to the context class
        // TODO: Add the parent pipeline to the context class
        private IDictionary<string, IGraphicsResource> resources;
        private IDictionary<string, IGraphicsResource> localResources;
        private IDictionary<string, Vector4> localVector4List;
        private IDictionary<string, int> localIntList;

        public GraphicsPipeline(RenderManager renderManager, GraphicsManager graphicsManager, ResourcesManager resourcesManager, ReadOnlyMemory<GraphicsPipelineResourceDeclaration> resourceDeclarations, ReadOnlyMemory<GraphicsPipelineStep> steps)
        {
            if (renderManager == null)
            {
                throw new ArgumentNullException(nameof(renderManager));
            }

            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.renderManager = renderManager;
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
            }

            this.resources = new Dictionary<string, IGraphicsResource>();
            this.localResources = new Dictionary<string, IGraphicsResource>();
            this.localVector4List = new Dictionary<string, Vector4>();
            this.localIntList = new Dictionary<string, int>();
        }

        public ReadOnlyMemory<GraphicsPipelineResourceDeclaration> ResourceDeclarations { get; }
        public ReadOnlyMemory<GraphicsPipelineStep> Steps { get; }

        public Fence Process(ReadOnlyMemory<GraphicsPipelineParameter> parameters, Fence fenceToWait)
        {
            var commandLists = BuildCommandLists(parameters);

            // Process commandLists
            CommandList? commandListToExecute = null;

            for (var i = 0; i < commandLists.Length; i++)
            {
                var commandList = commandLists[i];

                if (i == 0)
                {
                    this.graphicsManager.WaitForCommandQueue(commandList.CommandQueue, fenceToWait);
                }

                if (commandListToExecute != null)
                {
                    // TODO: Put an UAV Barrier here if both compute command list access the same resources

                    var isAwaitable = (commandListToExecute.Value.CommandQueue.NativePointer != commandList.CommandQueue.NativePointer);
                    var fence = this.graphicsManager.ExecuteCommandLists(commandListToExecute.Value.CommandQueue, new CommandList[] { commandListToExecute.Value }, isAwaitable);

                    if (isAwaitable)
                    {
                        this.graphicsManager.WaitForCommandQueue(commandList.CommandQueue, fence);
                    }
                }

                commandListToExecute = commandList;
            }

            return this.graphicsManager.ExecuteCommandLists(commandListToExecute!.Value.CommandQueue, new CommandList[] { commandListToExecute!.Value }, isAwaitable: true);
        }

        internal ReadOnlySpan<CommandList> BuildCommandLists(ReadOnlyMemory<GraphicsPipelineParameter> parameters)
        {
            this.resources.Clear();
            this.localVector4List.Clear();
            this.localIntList.Clear();
            this.localResources.Clear();

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

                if (resourceDeclaration is GraphicsPipelineTextureResourceDeclaration textureDeclaration)
                {
                    var texture = this.graphicsManager.CreateTexture(GraphicsHeapType.TransientGpu, textureDeclaration.TextureFormat, textureDeclaration.Usage, textureDeclaration.Width.Evaluate(this), textureDeclaration.Height.Evaluate(this), 1, 1, 1, isStatic: true, label: textureDeclaration.Name);
                    this.localResources.Add(textureDeclaration.Name, texture);
                }
            }

            // Build the commands lists
            var commandLists = new List<CommandList>();

            for (var i = 0; i < this.Steps.Span.Length; i++)
            {
                var step = this.Steps.Span[i];

                // TODO: Avoid copy here
                commandLists.AddRange(step.BuildCommandLists(this, this.renderManager, this.graphicsManager).ToArray());
            }

            // TODO: Remove the ToArray here
            return new ReadOnlySpan<CommandList>(commandLists.ToArray());
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
        private Shader computeDirectTransferShader;

        private GraphicsBuffer cpuRenderPassParametersGraphicsBuffer;
        private GraphicsBuffer renderPassParametersGraphicsBuffer;

        private Texture cubeMap;
        private Texture irradianceCubeMap;

        // Compute shaders data structures
        private GraphicsBuffer cpuScenePropertiesBuffer;
        private GraphicsBuffer scenePropertiesBuffer;
        private GraphicsBuffer cpuCamerasBuffer;
        private GraphicsBuffer readBackCamerasBuffer;
        private GraphicsBuffer camerasBuffer;
        private GraphicsBuffer cpuLightsBuffer;
        private GraphicsBuffer lightsBuffer;
        private GraphicsBuffer cpuMaterialsBuffer;
        private GraphicsBuffer materialsBuffer;
        private GraphicsBuffer cpuGeometryPacketsBuffer;
        private GraphicsBuffer geometryPacketsBuffer;
        private GraphicsBuffer cpuGeometryInstancesBuffer;
        private GraphicsBuffer geometryInstancesBuffer;
        private GraphicsBuffer cpuIndirectCommandBufferCounters;
        private GraphicsBuffer readBackIndirectCommandBufferCounters;
        private GraphicsBuffer indirectCommandBufferCounters;

        private GraphicsBuffer minMaxDepthComputeBuffer;

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
            this.computeDirectTransferShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeDirectTransfer.shader");

            this.minMaxDepthComputeBuffer = this.graphicsManager.CreateGraphicsBuffer<Vector2>(GraphicsHeapType.Gpu, 10000, isStatic: true, label: "ComputeMinMaxDepthWorkingBuffer");

            this.cpuRenderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants>(GraphicsHeapType.Upload, 1, isStatic: false, label: "RenderPassConstantBufferOld");
            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants>(GraphicsHeapType.Gpu, 1, isStatic: false, label: "RenderPassConstantBufferOld");

            this.cubeMap = resourcesManager.LoadResourceAsync<Texture>("/BistroV4/san_giuseppe_bridge_4k_cubemap.texture");
            this.irradianceCubeMap = resourcesManager.LoadResourceAsync<Texture>("/BistroV4/san_giuseppe_bridge_4k_irradiance_cubemap.texture");

            // Compute buffers
            this.cpuScenePropertiesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderSceneProperties>(GraphicsHeapType.Upload, 1, isStatic: false, label: "ComputeSceneProperties");
            this.scenePropertiesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderSceneProperties>(GraphicsHeapType.Gpu, 1, isStatic: false, label: "ComputeSceneProperties");
            this.cpuCamerasBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderCamera>(GraphicsHeapType.Upload, 10000, isStatic: false, label: "ComputeCameras");
            this.readBackCamerasBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderCamera>(GraphicsHeapType.ReadBack, 10000, isStatic: false, label: "ComputeCameras");
            this.camerasBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderCamera>(GraphicsHeapType.Gpu, 10000, isStatic: false, label: "ComputeCameras");
            this.cpuLightsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderLight>(GraphicsHeapType.Upload, 10000, isStatic: false, label: "ComputeLights");
            this.lightsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderLight>(GraphicsHeapType.Gpu, 10000, isStatic: false, label: "ComputeLights");
            this.cpuMaterialsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderMaterial>(GraphicsHeapType.Upload, 10000, isStatic: false, label: "ComputeMaterials");
            this.materialsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderMaterial>(GraphicsHeapType.Gpu, 10000, isStatic: false, label: "ComputeMaterials");
            this.cpuGeometryPacketsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryPacket>(GraphicsHeapType.Upload, 10000, isStatic: false, label: "ComputeGeometryPackets");
            this.geometryPacketsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryPacket>(GraphicsHeapType.Gpu, 10000, isStatic: false, label: "ComputeGeometryPackets");
            this.cpuGeometryInstancesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryInstance>(GraphicsHeapType.Upload, 100000, isStatic: false, label: "ComputeGeometryInstances");
            this.geometryInstancesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryInstance>(GraphicsHeapType.Gpu, 100000, isStatic: false, label: "ComputeGeometryInstances");
            this.cpuIndirectCommandBufferCounters = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.Upload, 100, isStatic: false, label: "UploadICBCounters");
            this.readBackIndirectCommandBufferCounters = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.ReadBack, 100, isStatic: false, label: "ReadBackICBCounters");
            this.indirectCommandBufferCounters = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.Gpu, 100, isStatic: false, label: "GpuICBCounters");

            // TEST Pipeline definition

            // TODO: Define parameters so the pipeline can check what is passed to the process method
            // var graphicsPipelineParameters = new GraphicsPipelineParameter[]
            // {
            //     new 
            // };

            var depthGraphicsPipelineResourceDeclarations = Array.Empty<GraphicsPipelineResourceDeclaration>();

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

            this.depthGraphicsPipeline = new GraphicsPipeline(this.renderManager, this.graphicsManager, resourcesManager, depthGraphicsPipelineResourceDeclarations, depthGraphicsPipelineSteps);

            var graphicsPipelineResourceDeclarations = new GraphicsPipelineResourceDeclaration[]
            {
                new GraphicsPipelineTextureResourceDeclaration("MainCameraDepthBuffer", TextureFormat.Depth32Float, TextureUsage.RenderTarget, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
                new GraphicsPipelineTextureResourceDeclaration("OpaqueHdrRenderTarget", TextureFormat.Rgba16Float, TextureUsage.RenderTarget, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
                new GraphicsPipelineTextureResourceDeclaration("TransparentHdrRenderTarget", TextureFormat.Rgba16Float, TextureUsage.RenderTarget, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
                new GraphicsPipelineTextureResourceDeclaration("TransparentRevealageRenderTarget", TextureFormat.R16Float, TextureUsage.RenderTarget, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
                new GraphicsPipelineTextureResourceDeclaration("ResolveRenderTarget", TextureFormat.Rgba16Float, TextureUsage.ShaderWrite, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
                new GraphicsPipelineTextureResourceDeclaration("ToneMapRenderTarget", TextureFormat.Rgba16Float, TextureUsage.ShaderWrite, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height"))
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
                // new ComputeMinMaxPipelineStep("ComputeMinMaxDepth",
                //                                 resourcesManager,
                //                                 new GraphicsPipelineResourceBinding[]
                //                                 {
                //                                     new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthBuffer"), new ConstantPipelineParameterBinding<int>(0)),
                //                                     new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MinMaxDepthComputeBuffer"), new ConstantPipelineParameterBinding<int>(2))
                //                                 },
                //                                 new GraphicsPipelineResourceBinding[]
                //                                 {
                //                                     new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("CamerasBuffer"), new ConstantPipelineParameterBinding<int>(1))
                //                                 }),
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
                                                                new DepthGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthBuffer"), new ConstantPipelineParameterBinding<DepthBufferOperation>(DepthBufferOperation.CompareGreater)),
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
                                                    new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("ToneMapRenderTarget"), new ConstantPipelineParameterBinding<int>(1))
                                                }, 
                                                new GraphicsPipelineParameterBinding<int>[]
                                                {  
                                                    new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"),
                                                    new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")
                                                })
            };

            this.graphicsPipeline = new GraphicsPipeline(this.renderManager, this.graphicsManager, resourcesManager, graphicsPipelineResourceDeclarations, graphicsPipelineSteps);
        }

        public void Render(Texture mainRenderTargetTexture)
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

            if (this.renderManager.logFrameTime)
            {
                Logger.BeginAction("InitGpuData");
            }

            InitializeGpuData(scene);
            
            if (this.renderManager.logFrameTime)
            {
                Logger.EndAction();
                Logger.BeginAction("RunPipeline");
            }

            RunRenderPipeline(mainRenderTargetTexture);

            if (this.renderManager.logFrameTime)
            {
                Logger.EndAction();
            }
        }

        GraphicsBuffer[] graphicsBufferList = new GraphicsBuffer[10000];
        int currentGraphicsBufferIndex;

        int currentCameraIndex;
        int currentLightIndex;
        int currentMaterialIndex;
        int currentGeometryPacketIndex;
        int currentGeometryInstanceIndex;

        ShaderCamera mainCamera;

        Dictionary<uint, int> materialListIndexes = new Dictionary<uint, int>();
        Texture[] textureList = new Texture[10000];
        int currentTextureIndex;

        Texture[] cubeTextureList = new Texture[10000];
        int currentCubeTextureIndex;

        Texture[] shadowMaps = new Texture[100];
        int currentShadowMapIndex;

        IndirectCommandBuffer[] indirectCommandBufferList = new IndirectCommandBuffer[100];
        int currentIndirectCommandBufferIndex;

        private int AddIndirectCommandBuffer(string label)
        {
            if (this.indirectCommandBufferList[this.currentIndirectCommandBufferIndex].NativePointer == IntPtr.Zero)
            {
                this.indirectCommandBufferList[this.currentIndirectCommandBufferIndex] = this.graphicsManager.CreateIndirectCommandBuffer(1000, isStatic: false, label: label);
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
            if (!isMomentShadowMap)
            {
                this.shadowMaps[this.currentShadowMapIndex] = this.graphicsManager.CreateTexture(GraphicsHeapType.TransientGpu, TextureFormat.Depth32Float, TextureUsage.RenderTarget, shadowMapSize, shadowMapSize, 1, 1, 1, isStatic: true, label: "ShadowMapDepth");
            }

            else
            {
                this.shadowMaps[this.currentShadowMapIndex] = this.graphicsManager.CreateTexture(GraphicsHeapType.TransientGpu, TextureFormat.Rgba16Unorm, TextureUsage.ShaderWrite, shadowMapSize, shadowMapSize, 1, 1, 1, isStatic: true, label: "ShadowMapMoment");
            }

            return this.currentShadowMapIndex++;
        }

        private int AddCamera(ref ShaderCamera camera, Texture? depthTexture, Texture? momentShadowMap, Texture? occlusionDepthTexture)
        {
            if (!Matrix4x4.Invert(camera.ViewProjectionMatrix, out camera.ViewProjectionMatrixInverse))
            {
                //Logger.WriteMessage($"Camera Error");
            }

            if (depthTexture != null)
            {
                camera.DepthBufferTextureIndex = AddTexture(depthTexture);
            }

            if (momentShadowMap != null)
            {
                camera.MomentShadowMapIndex = AddTexture(momentShadowMap);
            }

            if (occlusionDepthTexture != null)
            {
                camera.OcclusionDepthTextureIndex = AddTexture(occlusionDepthTexture);
            }

            camera.OpaqueDepthCommandListIndex = AddIndirectCommandBuffer("OpaqueDepthICB");
            camera.TransparentDepthCommandListIndex = AddIndirectCommandBuffer("TransparentDepthICB");
            camera.OcclusionDepthCommandListIndex = AddIndirectCommandBuffer("OcclusionDepthICB");

            if (!camera.DepthOnly)
            {
                camera.OpaqueCommandListIndex = AddIndirectCommandBuffer("OpaqueICB");
                camera.TransparentCommandListIndex = AddIndirectCommandBuffer("TransparentICB");
            }

            var cameraList = this.graphicsManager.GetCpuGraphicsBufferPointer<ShaderCamera>(this.cpuCamerasBuffer);
            cameraList[this.currentCameraIndex] = camera;

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

                    lightCameraIndex = AddCamera(ref lightCamera, this.shadowMaps[shadowMapIndex], this.shadowMaps[momentShadowMapIndex], null);

                    //this.debugRenderer.DrawBoundingFrustum(lightCamera1.BoundingFrustum, new Vector3(0, 1, 0));
                }
            }

            var lightList = this.graphicsManager.GetCpuGraphicsBufferPointer<ShaderLight>(this.cpuLightsBuffer);
            lightList[this.currentLightIndex] = light;
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

            var materialList = this.graphicsManager.GetCpuGraphicsBufferPointer<ShaderMaterial>(this.cpuMaterialsBuffer);

            materialList[this.currentMaterialIndex].MaterialBufferIndex = AddGraphicsBuffer(material.MaterialData.Value);
            materialList[this.currentMaterialIndex].MaterialTextureOffset = this.currentTextureIndex;
            materialList[this.currentMaterialIndex].IsTransparent = material.IsTransparent;

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

            var geometryPacketList = this.graphicsManager.GetCpuGraphicsBufferPointer<ShaderGeometryPacket>(this.cpuGeometryPacketsBuffer);
            geometryPacketList[this.currentGeometryPacketIndex] = shaderGeometryPacket;
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
            var currentVertexBufferId = IntPtr.Zero;

            var scenePropertyObject = new ShaderSceneProperties();

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

                    scenePropertyObject.DebugCameraIndex = AddCamera(ref shaderCamera, null, null, null);
                    this.mainCamera = shaderCamera;
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
                        MaxDepth = camera.FarPlaneDistance,
                    };

                    scenePropertyObject.ActiveCameraIndex = AddCamera(ref shaderCamera, null, null, null);
                    this.mainCamera = shaderCamera;
                }
            }

            scenePropertyObject.IsDebugCameraActive = (scene.DebugCamera != null);

            // TEST LIGHT BUFFER
            AddCubeTexture(this.cubeMap);
            AddCubeTexture(this.irradianceCubeMap);

            // var lightDirection = Vector3.Normalize(-new Vector3(0.172f, -0.818f, -0.549f));
            // shaderLight.WorldSpacePosition = Vector3.Normalize(new Vector3(-0.2f, 1.0f, -0.05f));
            // shaderLight.Color = new Vector3(0.95f, 0.91f, 0.84f);

            scenePropertyObject.LightCount = scene.Lights.Count;

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
                    this.debugRenderer.DrawBoundingBox(meshInstance.WorldBoundingBoxList[j], new Vector3(0, 1, 0));
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

                    if (currentVertexBufferId != geometryPacket.VertexBuffer.NativePointer)
                    {
                        currentVertexBufferId = geometryPacket.VertexBuffer.NativePointer;
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

                    var geometryInstanceList = this.graphicsManager.GetCpuGraphicsBufferPointer<ShaderGeometryInstance>(this.cpuGeometryInstancesBuffer);
                    geometryInstanceList[this.currentGeometryInstanceIndex++] = shaderGeometryInstance;
                }
            }

            var sceneProperties = this.graphicsManager.GetCpuGraphicsBufferPointer<ShaderSceneProperties>(this.cpuScenePropertiesBuffer);
            sceneProperties[0] = scenePropertyObject;
        }

        private Queue<IntPtr> previousCopyGpuDataIds = new Queue<IntPtr>();

        private CommandList CreateCopyCommandList()
        {
            // Copy buffers
            var counters = this.graphicsManager.GetCpuGraphicsBufferPointer<uint>(this.readBackIndirectCommandBufferCounters);
            var opaqueCounterIndex = this.mainCamera.OpaqueCommandListIndex;
            var transparentCounterIndex = this.mainCamera.TransparentCommandListIndex;

            this.renderManager.CulledGeometryInstancesCount = (int)counters[opaqueCounterIndex];

            var indirectCounters = this.graphicsManager.GetCpuGraphicsBufferPointer<uint>(this.cpuIndirectCommandBufferCounters);
            indirectCounters.Fill(0);

            this.renderManager.MainCameraDepth = new Vector2(this.mainCamera.MinDepth, this.mainCamera.MaxDepth);

            var commandListName = "CopySceneDataToGpu";
            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, commandListName);

            this.previousCopyGpuDataIds.Enqueue(copyCommandList.NativePointer);
            var startQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);

            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderCamera>(copyCommandList, this.camerasBuffer, this.cpuCamerasBuffer, this.currentCameraIndex);
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderSceneProperties>(copyCommandList, this.scenePropertiesBuffer, this.cpuScenePropertiesBuffer, 1);
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderLight>(copyCommandList, this.lightsBuffer, this.cpuLightsBuffer, this.currentLightIndex);
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderMaterial>(copyCommandList, this.materialsBuffer, this.cpuMaterialsBuffer, this.currentMaterialIndex);
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderGeometryPacket>(copyCommandList, this.geometryPacketsBuffer, this.cpuGeometryPacketsBuffer, this.currentGeometryPacketIndex);
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderGeometryInstance>(copyCommandList, this.geometryInstancesBuffer, this.cpuGeometryInstancesBuffer, this.currentGeometryInstanceIndex);
            this.graphicsManager.CopyDataToGraphicsBuffer<uint>(copyCommandList, this.indirectCommandBufferCounters, this.cpuIndirectCommandBufferCounters, 100);
            this.graphicsManager.CopyDataToGraphicsBuffer<RenderPassConstants>(copyCommandList, this.renderPassParametersGraphicsBuffer, this.cpuRenderPassParametersGraphicsBuffer, 1);

            var endQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);
            this.graphicsManager.CommitCommandList(copyCommandList);

            this.renderManager.AddGpuTiming(commandListName, QueryBufferType.CopyTimestamp, startQueryIndex, endQueryIndex);

            this.renderManager.GeometryInstancesCount = this.currentGeometryInstanceIndex;
            this.renderManager.MaterialsCount = this.currentMaterialIndex;
            this.renderManager.TexturesCount = this.currentTextureIndex;
            this.renderManager.LightsCount = this.currentLightIndex;

            return copyCommandList;
        }
        
        private CommandList CreateResetIcbCommandList()
        {
            var commandListName = "ResetIndirectCommands";
            var resetIcbCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, commandListName);

            var startQueryIndex = this.renderManager.InsertQueryTimestamp(resetIcbCommandList);

            for (var i = 0; i < this.currentIndirectCommandBufferIndex; i++)
            {
                this.graphicsManager.ResetIndirectCommandBuffer(resetIcbCommandList, this.indirectCommandBufferList[i], this.currentGeometryInstanceIndex);
            }

            var endQueryIndex = this.renderManager.InsertQueryTimestamp(resetIcbCommandList);
            this.graphicsManager.CommitCommandList(resetIcbCommandList);
            this.renderManager.AddGpuTiming(commandListName, QueryBufferType.CopyTimestamp, startQueryIndex, endQueryIndex);

            return resetIcbCommandList;
        }

        private CommandList CreateGenerateIcbCommandList(uint cameraCount)
        {
            var commandListName = "GenerateIndirectCommands";
            var computeCommandList = this.graphicsManager.CreateCommandList(this.renderManager.ComputeCommandQueue, commandListName);

            var startQueryIndex = this.renderManager.InsertQueryTimestamp(computeCommandList);

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
            
            var endQueryIndex = this.renderManager.InsertQueryTimestamp(computeCommandList);
            this.graphicsManager.CommitCommandList(computeCommandList);
            this.renderManager.AddGpuTiming("GenerateIndirectCommands", QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);
            
            return computeCommandList;
        }

        private CommandList CreateOptimizeIcbCommandList(uint cameraCount)
        {
            var commandListName = "OptimizeIndirectCommands";
            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, commandListName);
            var startQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);

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

            this.graphicsManager.CopyDataToGraphicsBuffer<uint>(copyCommandList, this.readBackIndirectCommandBufferCounters, this.indirectCommandBufferCounters, 4);
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderCamera>(copyCommandList, this.readBackCamerasBuffer, this.camerasBuffer, 1);
            var endQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);

            this.graphicsManager.CommitCommandList(copyCommandList);
            this.renderManager.AddGpuTiming(commandListName, QueryBufferType.CopyTimestamp, startQueryIndex, endQueryIndex);

            return copyCommandList;
        }

        // int currentDepthCommandBuffer = 0;

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

        // int currentMomentCommandBuffer;

        // private CommandList ConvertToMomentShadowMap(ShaderCamera camera, CommandList commandListToWait)
        // {
        //     this.graphicsManager.ResetCommandBuffer(convertToMomentShadowMapCommandLists[currentMomentCommandBuffer]);

        //     var renderTarget = new RenderTargetDescriptor(this.textureList[camera.MomentShadowMapIndex], null, BlendOperation.None);
        //     var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true, PrimitiveType.TriangleStrip);
        //     var commandList = this.graphicsManager.CreateRenderCommandList(convertToMomentShadowMapCommandLists[currentMomentCommandBuffer], hdrTransferRenderPassDescriptor, "ConvertToMomentShadowMap");

        //     this.graphicsManager.WaitForCommandList(commandList, commandListToWait);

        //     this.graphicsManager.SetShader(commandList, this.convertToMomentShadowMapShader);
        //     this.graphicsManager.SetShaderTexture(commandList, this.textureList[camera.DepthBufferTextureIndex], 0);

        //     this.graphicsManager.DrawPrimitives(commandList, PrimitiveType.TriangleStrip, 0, 4);

        //     this.graphicsManager.CommitRenderCommandList(commandList);
        //     this.graphicsManager.ExecuteCommandBuffer(convertToMomentShadowMapCommandLists[currentMomentCommandBuffer]);

        //     currentMomentCommandBuffer++;

        //     return commandList;
        // }

        // private CommandList ComputeLightCameras(CommandList commandListToWait)
        // {
        //     this.graphicsManager.ResetCommandBuffer(computeLightsCamerasCommandList);

        //     var computeCommandList = this.graphicsManager.CreateComputeCommandList(computeLightsCamerasCommandList, "ComputeLightCameras");

        //     this.graphicsManager.WaitForCommandList(computeCommandList, commandListToWait);

        //     this.graphicsManager.SetShader(computeCommandList, this.computeLightCamerasShader);
        //     this.graphicsManager.SetShaderBuffer(computeCommandList, this.lightsBuffer, 0);
        //     this.graphicsManager.SetShaderBuffer(computeCommandList, this.camerasBuffer, 1);

        //     this.graphicsManager.DispatchThreads(computeCommandList, 1, 4, 1);
        //     this.graphicsManager.CommitComputeCommandList(computeCommandList);

        //     this.graphicsManager.ExecuteCommandBuffer(computeLightsCamerasCommandList);

        //     return computeCommandList;
        // }

        private void RunRenderPipeline(Texture mainRenderTargetTexture)
        {
            if (this.currentGeometryInstanceIndex == 0)
            {
                return;
            }
            
            // this.currentDepthCommandBuffer = 0;
            // this.currentMomentCommandBuffer = 0;

            var copyCommandList = CreateCopyCommandList();
            var resetIcbCommandList = CreateResetIcbCommandList();
            var generateIcbCommandList = CreateGenerateIcbCommandList(1);
            var optimizeIcbCommandList = CreateOptimizeIcbCommandList(1);

            var copyFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList, resetIcbCommandList }, isAwaitable: true);

            this.graphicsManager.WaitForCommandQueue(this.renderManager.ComputeCommandQueue, copyFence);
            var computeFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.ComputeCommandQueue, new CommandList[] { generateIcbCommandList }, true);
            
            this.graphicsManager.WaitForCommandQueue(this.renderManager.CopyCommandQueue, computeFence);
            var optimizeFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { optimizeIcbCommandList }, true);

            // Generate Main Camera Depth Buffer
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
                new ResourceGraphicsPipelineParameter("MainRenderTarget", mainRenderTargetTexture),
                new ResourceGraphicsPipelineParameter("MainCameraIndirectCommandBuffer", this.indirectCommandBufferList[mainCamera.OpaqueCommandListIndex]),
                new ResourceGraphicsPipelineParameter("MainCameraDepthIndirectCommandBuffer", this.indirectCommandBufferList[mainCamera.OpaqueDepthCommandListIndex]),
                new ResourceGraphicsPipelineParameter("MainCameraTransparentIndirectCommandBuffer", this.indirectCommandBufferList[mainCamera.TransparentCommandListIndex]),
                new ResourceGraphicsPipelineParameter("MainCameraTransparentDepthIndirectCommandBuffer", this.indirectCommandBufferList[mainCamera.TransparentDepthCommandListIndex]),
                new Vector4GraphicsPipelineParameter("ClearColor", new Vector4(65 * 50, 135 * 50, 255 * 50, 1.0f) / 255.0f),
                new IntGraphicsPipelineParameter("GeometryInstanceCount", this.currentGeometryInstanceIndex),
                new ResourceGraphicsPipelineParameter("MinMaxDepthComputeBuffer", this.minMaxDepthComputeBuffer),
                new ResourceGraphicsPipelineParameter("CamerasBuffer", this.camerasBuffer)
            };

            var pipelineFence = this.graphicsPipeline.Process(graphicsPipelineParameters, optimizeFence);
            var toneMapRenderTarget = this.graphicsPipeline.ResolveResource("ToneMapRenderTarget") as Texture;

            var transferCommandList = TransferTextureToRenderTarget(toneMapRenderTarget, mainRenderTargetTexture);

            // TODO: Skip the wait if the last step of the pipeline was on the render queue
            this.graphicsManager.WaitForCommandQueue(this.renderManager.RenderCommandQueue, pipelineFence);
            this.graphicsManager.ExecuteCommandLists(this.renderManager.RenderCommandQueue, new CommandList[] { transferCommandList }, isAwaitable: false);

            this.debugRenderer.Render(this.renderPassParametersGraphicsBuffer, mainRenderTargetTexture, this.graphicsPipeline.ResolveResource("MainCameraDepthBuffer") as Texture);

            // var debugXOffset = this.graphicsManager.GetRenderSize().X - 256;

            // this.renderManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(debugXOffset, 0), new Vector2(debugXOffset + 256, 256), this.shadowMaps[1], true);
            // this.renderManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(debugXOffset, 256), new Vector2(debugXOffset + 256, 512), this.shadowMaps[3], true);
            // this.renderManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(debugXOffset, 512), new Vector2(debugXOffset + 256, 768), this.shadowMaps[5], true);
            // this.renderManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(debugXOffset, 768), new Vector2(debugXOffset + 256, 1024), this.shadowMaps[7], true);
            //this.graphicsManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(0, 0), new Vector2(this.graphicsManager.GetRenderSize().X, this.graphicsManager.GetRenderSize().Y), this.occlusionDepthTexture, true);
        }

        private CommandList TransferTextureToRenderTarget(Texture sourceTexture, Texture destinationTexture)
        {
            var renderCommandList = this.graphicsManager.CreateCommandList(this.renderManager.RenderCommandQueue, "TransferTextureCommandList");

            var renderTarget = new RenderTargetDescriptor(destinationTexture, null, BlendOperation.None);
            var renderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, backfaceCulling: true, PrimitiveType.TriangleStrip);

            this.graphicsManager.BeginRenderPass(renderCommandList, renderPassDescriptor);
            var startQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);

            this.graphicsManager.SetShader(renderCommandList, this.computeDirectTransferShader);
            this.graphicsManager.SetShaderTexture(renderCommandList, sourceTexture, 0);
            this.graphicsManager.DrawPrimitives(renderCommandList, PrimitiveType.TriangleStrip, 0, 4);

            var endQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);
            this.graphicsManager.EndRenderPass(renderCommandList);

            this.graphicsManager.CommitCommandList(renderCommandList);

            this.renderManager.AddGpuTiming("TransferTextureToMainRT", QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);
            return renderCommandList;
        }

        private void SetupCamera(Camera camera)
        {
            var renderPassConstants = this.graphicsManager.GetCpuGraphicsBufferPointer<RenderPassConstants>(this.cpuRenderPassParametersGraphicsBuffer);
            var value = new RenderPassConstants();

            value.ViewMatrix = camera.ViewMatrix;
            value.ProjectionMatrix = camera.ProjectionMatrix;

            renderPassConstants[0] = value;
        }
    }
}
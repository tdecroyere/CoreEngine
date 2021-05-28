using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
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
                            // graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                            break;

                        case ShaderGraphicsPipelineResourceBinding shaderResourceBinding when pipelineResource.ResourceType == GraphicsResourceType.Buffer:
                            var slot = shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline);

                            if (slot == 1)
                            {
                                camerasBuffer = (GraphicsBuffer)pipelineResource;
                            }

                            // graphicsManager.SetShaderBuffer(computeCommandList, (GraphicsBuffer)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
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
                            // graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                            break;

                        case ShaderGraphicsPipelineResourceBinding shaderResourceBinding when pipelineResource.ResourceType == GraphicsResourceType.Buffer:
                            var slot = shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline);

                            if (slot == 1)
                            {
                                camerasBuffer = (GraphicsBuffer)pipelineResource;
                            }

                            // graphicsManager.SetShaderBuffer(computeCommandList, (GraphicsBuffer)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                            break;
                    }
                }
            }

            if (depthBuffer == null)
            {
                throw new InvalidOperationException("Depth buffer is null");
            }
          
          /*
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
                                // graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                                break;

                            case ShaderGraphicsPipelineResourceBinding shaderResourceBinding when pipelineResource.ResourceType == GraphicsResourceType.Buffer:
                                // graphicsManager.SetShaderBuffer(computeCommandList, (GraphicsBuffer)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
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
                                // graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
                                break;

                            case ShaderGraphicsPipelineResourceBinding shaderResourceBinding when pipelineResource.ResourceType == GraphicsResourceType.Buffer:
                                // graphicsManager.SetShaderBuffer(computeCommandList, (GraphicsBuffer)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
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
*/
            // TODO: Avoid the array copy
            return result.ToArray();
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
                        // graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline));
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
                        // graphicsManager.SetShaderTexture(computeCommandList, (Texture)pipelineResource, shaderResourceBinding.ShaderBindingSlot.Evaluate(pipeline), isReadOnly: false);
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
}
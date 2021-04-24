using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using System.Runtime.Loader;
using System.Runtime.InteropServices;

namespace CoreEngine.Tools.Compiler.ResourceCompilers.Scenes
{
    public abstract class SceneResourceCompiler : ResourceCompiler
    {
        protected SceneResourceCompiler()
        {

        }
        
        public override string Name
        {
            get
            {
                return "Scene Resource Compiler";
            }
        }

        public override IList<string> SupportedSourceExtensions
        {
            get
            {
                return new string[] { ".cescene" };
            }
        }

        public override string DestinationExtension
        {
            get
            {
                return ".scene";
            }
        }

        public override async Task<ReadOnlyMemory<ResourceCompilerOutput>> CompileAsync(ReadOnlyMemory<byte> sourceData, CompilerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var version = 1;

            var sceneDescription = await ParseSceneFileAsync(sourceData);
            Logger.WriteMessage($"Scene Entity Count: {sceneDescription.Entities.Count}", LogMessageTypes.Debug);

            var entityManager = new EntityManager();
            var componentLayouts = new List<ComponentLayout>();
            CreateEntities(sceneDescription, entityManager, componentLayouts);

            var destinationMemoryStream = new MemoryStream();

            using var streamWriter = new BinaryWriter(destinationMemoryStream);
            streamWriter.Write(new char[] { 'S', 'C', 'E', 'N', 'E' });
            streamWriter.Write(version);

            streamWriter.Write(componentLayouts.Count);
            streamWriter.Write(sceneDescription.Entities.Count);

            foreach (var componentLayout in componentLayouts)
            {
                streamWriter.Write(componentLayout.Components.Count);

                foreach (var component in componentLayout.Components)
                {
                    var componentHash = component.Hash.ToArray();

                    streamWriter.Write(componentHash.Length);
                    streamWriter.Write(componentHash);
                    streamWriter.Write(component.SizeInBytes);
                }
            }

            Logger.BeginAction("Writing Scene data");

            foreach (var entity in sceneDescription.Entities)
            {
                streamWriter.Write(entity.Name);
                streamWriter.Write(entity.EntityLayoutIndex);
                streamWriter.Write(entity.Components.Count);

                foreach (var component in entity.Components)
                {
                    streamWriter.Write(component.ComponentType);
                    streamWriter.Write(component.Data?.Count ?? 0);

                    if (component.Data != null)
                    {
                        foreach (var componentValue in component.Data)
                        {
                            streamWriter.Write(componentValue.Key);
                            streamWriter.Write(componentValue.Value.GetType().ToString());

                            if (componentValue.Value.GetType() == typeof(string))
                            {
                                streamWriter.Write((string)componentValue.Value);
                            }

                            else if (componentValue.Value.GetType() == typeof(bool))
                            {
                                streamWriter.Write((bool)componentValue.Value);
                            }

                            else if (componentValue.Value.GetType() == typeof(float))
                            {
                                streamWriter.Write((float)componentValue.Value);
                            }

                            else if (componentValue.Value.GetType() == typeof(float[]))
                            {
                                var floatArray = (float[])componentValue.Value;

                                streamWriter.Write(floatArray.Length);

                                foreach (var floatValue in floatArray)
                                {
                                    streamWriter.Write(floatValue);
                                }
                            }
                        }
                    }
                }
            }

            Logger.EndAction();

            streamWriter.Flush();
            destinationMemoryStream.Flush();

            var resourceData = new Memory<byte>(destinationMemoryStream.GetBuffer(), 0, (int)destinationMemoryStream.Length);
            var resourceEntry = new ResourceCompilerOutput($"{Path.GetFileNameWithoutExtension(context.SourceFilename)}{this.DestinationExtension}", resourceData);

            return new ReadOnlyMemory<ResourceCompilerOutput>(new ResourceCompilerOutput[] { resourceEntry });
        }

        private static void CreateEntities(SceneDescription sceneDescription, EntityManager entityManager, List<ComponentLayout> componentLayouts)
        {
            foreach (var entityLayout in sceneDescription.EntityLayouts)
            {
                var componentLayout = entityManager.CreateComponentLayout();
                componentLayouts.Add(componentLayout);

                foreach (var type in entityLayout.Types)
                {
                    var componentType = Type.GetType(type, (assemblyName) =>
                    {
                        foreach (var assemblyLoadContext in AssemblyLoadContext.All)
                        {
                            var assembly = assemblyLoadContext.Assemblies.Where(assembly => assembly.GetName().Name == assemblyName.Name).FirstOrDefault();

                            if (assembly != null)
                            {
                                return assembly;
                            }
                        }

                        return null;
                    }, null);

                    if (componentType == null)
                    {
                        Logger.WriteMessage($"Cannot resolve component type: {type}", LogMessageTypes.Warning);
                        continue;
                    }

                    Logger.WriteMessage($"Found Type: {componentType}");

                    var component = (IComponentData)Activator.CreateInstance(componentType)!;

                    component.SetDefaultValues();
                    var componentSize = Marshal.SizeOf(component);

                    var componentInitialData = new byte[componentSize];
                    var ptr = Marshal.AllocHGlobal(componentSize);
                    // Copy object byte-to-byte to unmanaged memory.
                    Marshal.StructureToPtr(component, ptr, false);
                    // Copy data from unmanaged memory to managed buffer.
                    Marshal.Copy(ptr, componentInitialData, 0, componentSize);
                    // Release unmanaged memory.
                    Marshal.FreeHGlobal(ptr);

                    entityManager.RegisterComponentLayoutComponent(componentLayout, component.GetComponentHash(), componentSize, componentInitialData);
                }
            }
        }

        protected abstract Task<SceneDescription> ParseSceneFileAsync(ReadOnlyMemory<byte> sourceData);
    }
}
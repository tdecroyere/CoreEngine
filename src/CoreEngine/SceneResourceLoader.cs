using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace CoreEngine
{
    public class SceneResourceLoader : ResourceLoader
    {
        public SceneResourceLoader(ResourcesManager resourcesManager) : base(resourcesManager)
        {

        }

        public override string Name => "Scene Loader";
        public override string FileExtension => ".scene";

        public override Resource CreateEmptyResource(uint resourceId, string path)
        {
            return new Scene(resourceId, path);
        }

        public override Resource LoadResourceData(Resource resource, byte[] data)
        {
            if (resource is not Scene scene)
            {
                throw new ArgumentException("Resource is not a Scene resource.", nameof(resource));
            }

            var resourcePrefix = "resource:";
            scene.Reset();

            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var meshSignature = reader.ReadChars(5);
            var meshVersion = reader.ReadInt32();

            if (meshSignature.ToString() != "SCENE" && meshVersion != 1)
            {
                Logger.WriteMessage($"ERROR: Wrong signature or version for scene '{scene.Path}'");
                return resource;
            }

            //Logger.WriteMessage("Scene Loading");

            var componentLayoutCount = reader.ReadInt32();
            var entitiesCount = reader.ReadInt32();

            //Logger.WriteMessage($"Loading {entitiesCount} entities (Layouts: {entityLayoutsCount})");

            var componentLayouts = new ComponentLayout[componentLayoutCount];

            for (var i = 0; i < componentLayoutCount; i++)
            {
                var componentLayout = scene.EntityManager.CreateComponentLayout();
                componentLayouts[i] = componentLayout;

                var componentCount = reader.ReadInt32();

                for (var j = 0; j < componentCount; j++)
                {
                    var componentHashLength = reader.ReadInt32();
                    var componentHash = reader.ReadBytes(componentHashLength);
                    var componentSizeInBytes = reader.ReadInt32();

                    componentLayout.RegisterComponent(new ComponentHash(componentHash), componentSizeInBytes, null);
                }
            }

            var entitiesMapping = new Dictionary<string, Entity>();
            var entitiesToResolve = new Dictionary<string, List<(Entity entity, Type componentType, IComponentData component, PropertyInfo property)>>();

            for (var i = 0; i < entitiesCount; i++)
            {
                // TODO: Try to not use strings
                var entityName = reader.ReadString();
                var entityLayoutIndex = reader.ReadInt32();
                var componentsCount = reader.ReadInt32();

                Logger.BeginAction($"Create Entity '{entityName}'");
                // Logger.WriteMessage($"Components Count: {componentsCount}");
                // Logger.WriteMessage($"EntityLayoutIndex: {entityLayoutIndex}");

                var entityLayout = componentLayouts[entityLayoutIndex];
                var entity = scene.EntityManager.CreateEntity(entityLayout);
                entitiesMapping.Add(entityName, entity);

                for (var j = 0; j < componentsCount; j++)
                {
                    var componentTypeName = reader.ReadString();
                    var componentValuesCount = reader.ReadInt32();

                    var componentType = FindType(componentTypeName);
                    Logger.BeginAction($"Reading Component Type '{componentType}'");

                    IComponentData? component = null;

                    if (componentType != null)
                    {
                        component = (IComponentData)Activator.CreateInstance(componentType)!;
                        component.SetDefaultValues();
                    }

                    for (var k = 0; k < componentValuesCount; k++)
                    {
                        var componentKey = reader.ReadString();
                        var componentValueType = reader.ReadString();

                        PropertyInfo? propertyInfo = null;

                        if (component != null && componentType != null)
                        {
                            propertyInfo = componentType.GetProperty(componentKey);

                            if (propertyInfo != null)
                            {
                                // Logger.WriteMessage($"PropertyInfo: {propertyInfo.ToString()}");
                            }
                        }

                        if (Type.GetType(componentValueType) == typeof(string))
                        {
                            var stringValue = reader.ReadString();

                            if (propertyInfo != null)
                            {
                                var resourcePathIndex = stringValue.IndexOf(resourcePrefix, StringComparison.InvariantCulture);

                                if (resourcePathIndex != -1)
                                {
                                    var resourcePath = stringValue[(resourcePathIndex + resourcePrefix.Length)..];
                                    
                                    var componentResource = this.ResourcesManager.LoadResourceAsync<Resource>(resourcePath);
                                    scene.DependentResources.Add(componentResource);

                                    propertyInfo.SetValue(component, componentResource.ResourceId);
                                }

                                else if (propertyInfo.PropertyType == typeof(Entity) || propertyInfo.PropertyType == typeof(Entity?))
                                {
                                    if (!entitiesToResolve.ContainsKey(stringValue))
                                    {
                                        entitiesToResolve.Add(stringValue, new List<(Entity entity, Type componentType, IComponentData component, PropertyInfo property)>());
                                    }

                                    if (componentType != null && component != null)
                                    {
                                        entitiesToResolve[stringValue].Add((entity, componentType, component, propertyInfo));
                                    }
                                }

                                else
                                {
                                    propertyInfo.SetValue(component, stringValue);
                                }
                            }
                        }

                        else if (Type.GetType(componentValueType) == typeof(bool))
                        {
                            var boolValue = reader.ReadBoolean();

                            if (propertyInfo != null)
                            {
                                propertyInfo.SetValue(component, boolValue);
                            }
                        }

                        else if (Type.GetType(componentValueType) == typeof(float))
                        {
                            var floatValue = reader.ReadSingle();

                            if (propertyInfo != null)
                            {
                                propertyInfo.SetValue(component, floatValue);
                            }
                        }

                        else if (Type.GetType(componentValueType) == typeof(float[]))
                        {
                            var floatArrayLength = reader.ReadInt32();
                            // TODO: Use ArrayPool
                            var floatArrayValue = new float[floatArrayLength];

                            for (var l = 0; l < floatArrayLength; l++)
                            {
                                floatArrayValue[l] = reader.ReadSingle();
                            }

                            if (propertyInfo != null)
                            {
                                if (floatArrayLength == 2)
                                {
                                    var value = new Vector2(floatArrayValue[0], floatArrayValue[1]);
                                    propertyInfo.SetValue(component, value);
                                }

                                else if (floatArrayLength == 3)
                                {
                                    var value = new Vector3(floatArrayValue[0], floatArrayValue[1], floatArrayValue[2]);
                                    propertyInfo.SetValue(component, value);
                                }

                                else if (floatArrayLength == 4)
                                {
                                    var value = new Vector4(floatArrayValue[0], floatArrayValue[1], floatArrayValue[2], floatArrayValue[3]);
                                    propertyInfo.SetValue(component, value);
                                }

                                else
                                {
                                    Logger.WriteMessage("Unknown array float type");
                                }
                            }
                        }
                    }
                
                    if (component != null && componentType != null)
                    {
                        var size = Marshal.SizeOf(component);
                        // Both managed and unmanaged buffers required.
                        var bytes = new byte[size];
                        var ptr = Marshal.AllocHGlobal(size);
                        // Copy object byte-to-byte to unmanaged memory.
                        Marshal.StructureToPtr(component, ptr, false);
                        // Copy data from unmanaged memory to managed buffer.
                        Marshal.Copy(ptr, bytes, 0, size);
                        // Release unmanaged memory.
                        Marshal.FreeHGlobal(ptr);

                        scene.EntityManager.SetComponentData(entity, component.GetComponentHash(), bytes);
                    }

                    Logger.EndAction();
                }

                Logger.EndAction();
            }

            Logger.BeginAction($"Resolving entities");

            foreach (var entry in entitiesToResolve)
            {
                Logger.BeginAction($"Resolving entity: {entry.Key}");

                if (entitiesMapping.ContainsKey(entry.Key))
                {
                    var resolvedEntity = entitiesMapping[entry.Key];

                    foreach (var value in entry.Value)
                    {
                        // Logger.WriteMessage($"Found entity with Id: {value.entity.EntityId}");

                        value.property.SetValue(value.component, resolvedEntity);

                        var size = Marshal.SizeOf(value.component);
                        // Both managed and unmanaged buffers required.
                        var bytes = new byte[size];
                        var ptr = Marshal.AllocHGlobal(size);
                        // Copy object byte-to-byte to unmanaged memory.
                        Marshal.StructureToPtr(value.component, ptr, false);
                        // Copy data from unmanaged memory to managed buffer.
                        Marshal.Copy(ptr, bytes, 0, size);
                        // Release unmanaged memory.
                        Marshal.FreeHGlobal(ptr);

                        scene.EntityManager.SetComponentData(value.entity, value.component.GetComponentHash(), bytes);
                    }
                }

                else
                {
                    Logger.WriteMessage($"Entity '{entry.Key}' was not found", LogMessageTypes.Warning);
                }

                Logger.EndAction();
            }

            Logger.EndAction();

            return scene;
        }

        private static Type? FindType(string fullTypeName)
        {
            return Type.GetType(fullTypeName, (assemblyName) =>
            {
                foreach (var assemblyLoadContext in AssemblyLoadContext.All)
                {
                    foreach (var assembly in assemblyLoadContext.Assemblies)
                    {
                        if (assembly.GetName().Name == assemblyName.Name)
                        {
                            return assembly;
                        }
                    }
                }

                return null;
            }, null);
        }
    }
}
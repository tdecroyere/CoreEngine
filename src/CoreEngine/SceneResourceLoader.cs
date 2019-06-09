using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

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

        public override Task<Resource> LoadResourceDataAsync(Resource resource, byte[] data)
        {
            var scene = resource as Scene;
            var resourcePrefix = "resource:";

            if (scene == null)
            {
                throw new ArgumentException("Resource is not a Scene resource.", nameof(resource));
            }

            scene.Reset();

            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var meshSignature = reader.ReadChars(5);
            var meshVersion = reader.ReadInt32();

            if (meshSignature.ToString() != "SCENE" && meshVersion != 1)
            {
                Logger.WriteMessage($"ERROR: Wrong signature or version for scene '{resource.Path}'");
                return Task.FromResult(resource);
            }

            Logger.WriteMessage("Scene Loading");

            var entityLayoutsCount = reader.ReadInt32();
            var entitiesCount = reader.ReadInt32();

            Logger.WriteMessage($"Loading {entitiesCount} entities (Layouts: {entityLayoutsCount})");

            var sceneEntityLayoutsList = new List<EntityComponentLayout?>();

            for (var i = 0; i < entityLayoutsCount; i++)
            {
                var typesCount = reader.ReadInt32();
                var layoutTypes = new Type[typesCount];
                var isLayoutComplete = true;

                for (var j = 0; j < typesCount; j++)
                {
                    var typeFullName = reader.ReadString();
                    var type = FindType(typeFullName);
                    Logger.WriteMessage($"Found Type: {type}");

                    if (type == null)
                    {
                        isLayoutComplete = false;
                    }

                    else
                    {
                        layoutTypes[j] = type;
                    }
                }

                if (isLayoutComplete)
                {
                    var entityLayout = scene.EntityManager.CreateEntityComponentLayout(layoutTypes);
                    sceneEntityLayoutsList.Add(entityLayout);
                }

                else
                {
                    sceneEntityLayoutsList.Add(null);
                }
            }

            for (var i = 0; i < entitiesCount; i++)
            {
                var entityLayoutIndex = reader.ReadInt32();
                var componentsCount = reader.ReadInt32();

                Logger.WriteMessage($"EntityLayoutIndex: {entityLayoutIndex}");

                var entityLayout = sceneEntityLayoutsList[entityLayoutIndex];
                Entity? entity = null;

                if (entityLayout != null)
                {
                    Logger.WriteMessage($"Create Entity (Components Count: {componentsCount})");
                    entity = scene.EntityManager.CreateEntity(entityLayout.Value);
                }

                for (var j = 0; j < componentsCount; j++)
                {
                    var componentTypeName = reader.ReadString();
                    var componentValuesCount = reader.ReadInt32();

                    var componentType = FindType(componentTypeName);
                    Logger.WriteMessage($"Component Type: {componentType}");

                    IComponentData? component = null;

                    if (componentType != null)
                    {
                        component = (IComponentData)Activator.CreateInstance(componentType);
                        component.SetDefaultValues();
                        Logger.WriteMessage($"Component: {component.ToString()}");
                    }

                    for (var k = 0; k < componentValuesCount; k++)
                    {
                        var componentKey = reader.ReadString();
                        var componentValueType = reader.ReadString();

                        FieldInfo? fieldInfo = null;

                        if (component != null && componentType != null)
                        {
                            fieldInfo = componentType.GetField(componentKey);

                            if (fieldInfo != null)
                            {
                                Logger.WriteMessage($"FieldInfo: {fieldInfo.ToString()}");
                            }
                        }

                        if (Type.GetType(componentValueType) == typeof(string))
                        {
                            var stringValue = reader.ReadString();

                            if (fieldInfo != null)
                            {
                                var resourcePathIndex = stringValue.IndexOf(resourcePrefix, StringComparison.InvariantCulture);

                                if (resourcePathIndex != -1)
                                {
                                    var resourcePath = stringValue.Substring(resourcePathIndex + resourcePrefix.Length);
                                    
                                    var componentResource = this.ResourcesManager.LoadResourceAsync<Resource>(resourcePath);
                                    resource.DependentResources.Add(componentResource);

                                    fieldInfo.SetValue(component, componentResource.ResourceId);
                                    Logger.WriteMessage("Set resource Id value OK");
                                }

                                else
                                {
                                    fieldInfo.SetValue(component, stringValue);
                                    Logger.WriteMessage("Set string raw value OK");
                                }
                            }
                        }

                        else if (Type.GetType(componentValueType) == typeof(float))
                        {
                            var floatValue = reader.ReadSingle();

                            if (fieldInfo != null)
                            {
                                fieldInfo.SetValue(component, floatValue);
                                Logger.WriteMessage("Set float OK");
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

                            if (fieldInfo != null)
                            {
                                if (floatArrayLength == 2)
                                {
                                    var value = new Vector2(floatArrayValue[0], floatArrayValue[1]);
                                    fieldInfo.SetValue(component, value);
                                    Logger.WriteMessage("Set Vector2 OK");
                                }

                                else if (floatArrayLength == 3)
                                {
                                    var value = new Vector3(floatArrayValue[0], floatArrayValue[1], floatArrayValue[2]);
                                    fieldInfo.SetValue(component, value);
                                    Logger.WriteMessage("Set Vector3 OK");
                                }

                                else if (floatArrayLength == 4)
                                {
                                    var value = new Vector4(floatArrayValue[0], floatArrayValue[1], floatArrayValue[2], floatArrayValue[3]);
                                    fieldInfo.SetValue(component, value);
                                    Logger.WriteMessage("Set Vector4 OK");
                                }

                                else
                                {
                                    Logger.WriteMessage("Unknown array float type");
                                }
                            }
                        }
                    }
                
                    if (entity != null && component != null && componentType != null)
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

                        scene.EntityManager.SetComponentData(entity.Value, componentType, bytes);
                    }
                }
            }

            return Task.FromResult((Resource)scene);
        }

        private static Type? FindType(string fullTypeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (var i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                var type = assembly.GetType(fullTypeName);

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
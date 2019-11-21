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

            var sceneEntityLayoutsList = new List<ComponentLayout?>();

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
                    var entityLayout = scene.EntityManager.CreateComponentLayout(layoutTypes);
                    sceneEntityLayoutsList.Add(entityLayout);
                }

                else
                {
                    sceneEntityLayoutsList.Add(null);
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
                Logger.WriteMessage($"Components Count: {componentsCount}");
                Logger.WriteMessage($"EntityLayoutIndex: {entityLayoutIndex}");

                var entityLayout = sceneEntityLayoutsList[entityLayoutIndex];
                Entity? entity = null;

                if (entityLayout != null)
                {
                    entity = scene.EntityManager.CreateEntity(entityLayout.Value);
                    entitiesMapping.Add(entityName, entity.Value);
                }

                for (var j = 0; j < componentsCount; j++)
                {
                    var componentTypeName = reader.ReadString();
                    var componentValuesCount = reader.ReadInt32();

                    var componentType = FindType(componentTypeName);
                    Logger.BeginAction($"Reading Component Type '{componentType}'");

                    IComponentData? component = null;

                    if (componentType != null)
                    {
                        component = (IComponentData?)Activator.CreateInstance(componentType);

                        if (component == null)
                        {
                            throw new InvalidOperationException("Cannot create component type.");
                        }

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
                                Logger.WriteMessage($"PropertyInfo: {propertyInfo.ToString()}");
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
                                    var resourcePath = stringValue.Substring(resourcePathIndex + resourcePrefix.Length);
                                    
                                    var componentResource = this.ResourcesManager.LoadResourceAsync<Resource>(resourcePath);
                                    resource.DependentResources.Add(componentResource);

                                    propertyInfo.SetValue(component, componentResource.ResourceId);
                                }

                                else if (propertyInfo.PropertyType == typeof(Entity) || propertyInfo.PropertyType == typeof(Entity?))
                                {
                                    if (!entitiesToResolve.ContainsKey(stringValue))
                                    {
                                        entitiesToResolve.Add(stringValue, new List<(Entity entity, Type componentType, IComponentData component, PropertyInfo property)>());
                                    }

                                    if (entity != null && componentType != null && component != null)
                                    {
                                        entitiesToResolve[stringValue].Add((entity.Value, componentType, component, propertyInfo));
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
                
                    if (entity != null && component != null && componentType != null)
                    {
                        scene.EntityManager.SetComponentData(entity.Value, componentType, component);
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
                        Logger.WriteMessage($"Found entity with Id: {value.entity.EntityId}");

                        value.property.SetValue(value.component, resolvedEntity);
                        scene.EntityManager.SetComponentData(value.entity, value.componentType, value.component);
                    }
                }

                else
                {
                    Logger.WriteMessage($"Entity '{entry.Key}' was not found", LogMessageTypes.Warning);
                }

                Logger.EndAction();
            }

            Logger.EndAction();

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
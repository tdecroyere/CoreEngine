using System;
using System.Collections.Generic;

namespace CoreEngine
{
    public class SystemManagerContainer
    {
        private readonly CoreEngineApp coreEngineApp;
        private IDictionary<Type, SystemManager> systemManagerList = new Dictionary<Type, SystemManager>();

        public SystemManagerContainer(CoreEngineApp coreEngineApp)
        {
            this.coreEngineApp = coreEngineApp;
        }

        public void RegisterSystemManager<T>(T systemManager) where T : SystemManager
        {
            if (this.systemManagerList.ContainsKey(typeof(T)))
            {
                throw new ArgumentException($"System manager with type '{typeof(T).ToString()}' has already been added.");
            }

            this.systemManagerList.Add(typeof(T), systemManager);
        }

        public T GetSystemManager<T>() where T : SystemManager
        {
            if (!this.systemManagerList.ContainsKey(typeof(T)))
            {
                throw new ArgumentException($"System manager with type '{typeof(T).ToString()}' has not been registered.");
            }

            return (T)this.systemManagerList[typeof(T)];
        }

        public T CreateInstance<T>()
        {
            var type = typeof(T);
            var constructorsInfo = type.GetConstructors();

            if (constructorsInfo.Length == 0 || constructorsInfo[0].IsPublic == false)
            {
                throw new ArgumentException($"Type '{typeof(T).ToString()}' has no public constructor.");
            }

            var constructorInfo = constructorsInfo[0];
            var parameters = constructorInfo.GetParameters();
            var resolvedParameters = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (!this.systemManagerList.ContainsKey(parameter.ParameterType))
                {
                    throw new InvalidOperationException($"The parameter '{parameter.ParameterType.ToString()}' is not registered.");
                }

                resolvedParameters[i] = this.systemManagerList[parameter.ParameterType];
            }

            return (T)Activator.CreateInstance(typeof(T), resolvedParameters);
        }

        public void PreUpdateSystemManagers()
        {
            // TODO: Performance issue here?
            foreach (var manager in this.systemManagerList)
            {
                manager.Value.PreUpdate();
            }
        }

        public void PostUpdateSystemManagers()
        {
            // TODO: Performance issue here?
            foreach (var manager in this.systemManagerList)
            {
                manager.Value.PostUpdate();
            }
        }
    }
}
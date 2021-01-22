using System;
using System.Collections.Generic;

namespace CoreEngine
{
    public class SystemManagerContainer
    {
        private readonly IDictionary<Type, SystemManager> systemManagerList = new Dictionary<Type, SystemManager>();

        public SystemManagerContainer()
        {
        }

        public void RegisterSystemManager<T>(T systemManager) where T : SystemManager
        {
            if (this.systemManagerList.ContainsKey(typeof(T)))
            {
                throw new ArgumentException($"System manager with type '{typeof(T)}' has already been added.");
            }

            this.systemManagerList.Add(typeof(T), systemManager);
        }

        public T GetSystemManager<T>() where T : SystemManager
        {
            if (!this.systemManagerList.ContainsKey(typeof(T)))
            {
                throw new ArgumentException($"System manager with type '{typeof(T)}' has not been registered.");
            }

            return (T)this.systemManagerList[typeof(T)];
        }

        public T CreateInstance<T>()
        {
            return CreateInstance<T>(typeof(T));
        }

        public T CreateInstance<T>(Type type)
        {
            var constructorsInfo = type.GetConstructors();

            if (constructorsInfo.Length == 0 || constructorsInfo[0].IsPublic == false)
            {
                throw new ArgumentException($"Type '{type}' has no public constructor.");
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

            var instance = Activator.CreateInstance(type, resolvedParameters);

            if (instance == null)
            {
                throw new InvalidOperationException("Cannot create instance type.");
            }

            return (T)instance;
        }

        public void PreUpdateSystemManagers(CoreEngineContext context)
        {
            // TODO: Performance issue here?
            foreach (var manager in this.systemManagerList)
            {
                manager.Value.PreUpdate(context);
            }
        }

        public void PostUpdateSystemManagers(CoreEngineContext context)
        {
            // TODO: Performance issue here?
            foreach (var manager in this.systemManagerList)
            {
                manager.Value.PostUpdate(context);
            }
        }
    }
}
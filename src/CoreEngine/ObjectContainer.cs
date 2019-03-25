using System;
using System.Collections.Generic;

namespace CoreEngine
{
    // TODO: Remove static?
    public static class ObjectContainer
    {
        private static IDictionary<Type, Manager> managerList = new Dictionary<Type, Manager>();

        public static void RegisterManager<T>(T manager) where T : Manager
        {
            if (managerList.ContainsKey(typeof(T)))
            {
                throw new ArgumentException($"Manager with type '{typeof(T).ToString()}' has already been added.");
            }

            managerList.Add(typeof(T), manager);
        }

        public static T CreateInstance<T>()
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

                if (!managerList.ContainsKey(parameter.ParameterType))
                {
                    throw new InvalidOperationException($"The parameter '{parameter.ParameterType.ToString()}' is not registered.");
                }

                resolvedParameters[i] = managerList[parameter.ParameterType];
            }

            return (T)Activator.CreateInstance(typeof(T), resolvedParameters);
        }

        public static void UpdateManagers()
        {
            // TODO: Performance issue here?
            foreach (var manager in managerList)
            {
                manager.Value.Update();
            }
        }
    }
}
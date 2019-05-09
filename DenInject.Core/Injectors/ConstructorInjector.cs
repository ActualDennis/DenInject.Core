using DenInject.Core.Activators;
using DenInject.Core.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DenInject.Core.Injectors
{
    public class ConstructorInjector
    {
        public ConstructorInjector(DiConfiguration config)
        {
            this.activator = activator;
            activator = new DiActivator(config, this);
        }

        private DiActivator activator { get; set; }

        public object[] GetResolvedConstructorParameters(Type implementationType, Type interfaceType, bool IsOpenGenerics)
        {
            var constructorDependencies = GetConstructorDependencies(implementationType, interfaceType, IsOpenGenerics);

            var parametersToPass = new object[constructorDependencies.Count()];

            for (int dependency = 0; dependency < constructorDependencies.Count(); ++dependency)
            {
                parametersToPass[dependency] = activator.Activate(constructorDependencies[dependency]);
            }

            return parametersToPass;
        }

        private List<Type> GetConstructorDependencies(Type classType, Type interfaceType, bool IsOpenGenerics)
        {
            var constructor = ReflectionHelper.GetConstructor(classType);

            if (constructor == null)
                return null;

            var dependencies = constructor
            .GetParameters()
            .Select(x => x.ParameterType)
            .ToList();

            if (!IsOpenGenerics)
                return dependencies;

            //Fill open generics constructor dependencies.

            var result = new List<Type>();

            foreach (var dependency in dependencies)
            {
                if (!dependency.IsGenericParameter)
                {
                    result.Add(dependency);
                    continue;
                }

                var resolvedArgs = interfaceType.GetGenericArguments();
                var genericArgs = interfaceType.GetGenericTypeDefinition().GetGenericArguments();
                int index = 0;

                if ((index = Array.FindIndex(genericArgs, x => x.Name == dependency.Name)) == -1)
                    throw new ArgumentException("Dependency in constructor was not present in the interface generic arguments.");

                var constraints = genericArgs[index].GetGenericParameterConstraints();

                result.Add(constraints[0]);
            }

            return result;
        }
    }
}

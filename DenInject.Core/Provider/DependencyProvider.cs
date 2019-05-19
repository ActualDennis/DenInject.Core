using DenInject.Core.Extensions;
using DenInject.Core.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DenInject.Core {

    /// <summary>
    /// Represents a DI container.
    /// To support <see cref="Lazy{T}"/> initialization(weak reference),
    /// Set instance field of <see cref="DependencyProvider"/> to desired <see cref="DependencyProvider"/>
    /// </summary>
    public class DependencyProvider {
        private DiConfiguration m_Configuration { get; set; }

        private DiValidator Validator { get; set; }

        private List<CreatedObject> SingletonObjects { get; set; }

        public static DependencyProvider Instance { get; set; }

        public static ILazyExpressionTreesCompiler<Func<object>> LazyExpressionTreesCompiler { get; set; }

        public DependencyProvider(DiConfiguration config)
        {
            m_Configuration = config;
            SingletonObjects = new List<CreatedObject>();
            Validator = new DiValidator(config.Configuration);
            LazyExpressionTreesCompiler = new LazyExpressionTreesCompiler(new LazyExpressionTreesCache());
            Instance = this;
        }

        /// <summary>
        /// Resolves object earlier registered in DiConfiguration.
        /// Supports passing a IEnumerable<typeparamref name="TInterface"/> 
        /// , which will return all dependencies of <typeparamref name="TInterface"/>
        /// </summary>
        /// <typeparam name="TInterface">Type of object.</typeparam>
        /// <returns></returns>
        public TInterface Resolve<TInterface>()
        {
            return (TInterface)ResolveCore(typeof(TInterface));
        }

        public void ValidateConfig()
        {
            foreach(var entity in m_Configuration.Configuration)
            {
                foreach(var impl in entity.Implementations)
                {
                    Validator.Validate(impl.ImplType);
                }
            }
        }

        /// <summary>
        /// Non-generic version of <see cref="Resolve{TInterface}"/>
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public Object ResolveCore(Type interfaceType)
        {
            bool IsAllImplementationsRequested = false;

            ContainerEntity containerEntity = m_Configuration.GetRegisteredEntity(interfaceType);

            if(containerEntity == null)
            {
                var genericTypeDefinition = interfaceType.GetGenericTypeDefinition();

                if (genericTypeDefinition == typeof(IEnumerable<>))
                {
                    //This is request for all implementatios
                    
                    interfaceType = interfaceType.GetGenericArguments()[0];

                    IsAllImplementationsRequested = true;

                    containerEntity = m_Configuration.GetRegisteredEntity(interfaceType);
                }
                else if (genericTypeDefinition == typeof(Lazy<>))
                {
                    return CreateLazy(interfaceType);
                }
                else
                {
                    //Probably we are using open generics.
                    containerEntity = m_Configuration.GetRegisteredEntity(genericTypeDefinition);
                }
            }

            if (containerEntity == null)
            {
                throw new InvalidOperationException($"Dependency {interfaceType.ToString()} was not registered in container.");
            }

            var result = new List<Object>();

            var implCount = containerEntity.Implementations?.Count();


            for (int implementation = 0; implCount != null && implementation < implCount; ++implementation)
            {
                result.Add(CreateObjectRecursive(containerEntity.Implementations[implementation].ImplType, interfaceType, containerEntity.InterfaceType.IsGenericTypeDefinition));
            }

            if (IsAllImplementationsRequested)
                return result.ListObjToEnumerableType(interfaceType);
            else
                return result.First();
        }


        private Object CreateLazy(Type lazyType)
        {
            // var x = new Lazy<Object>(() => CreateLazy(null));
            return LazyExpressionTreesCompiler.Compile(lazyType).Invoke();
        }

        private Object CreateObjectRecursive(Type implementationType, Type interfaceType, bool IsOpenGenerics)
        {
            var parametersToPass = GetResolvedConstructorParameters(implementationType, interfaceType, IsOpenGenerics);

            var lifeTime = m_Configuration.GetObjectLifeTime(implementationType);

            switch (lifeTime)
            {
                case ObjLifetime.Transient:
                    {
                        if (IsOpenGenerics)
                            return CreateObject(parametersToPass, implementationType.MakeGenericType(interfaceType.GenericTypeArguments), interfaceType, lifeTime);

                        return CreateObject(parametersToPass, implementationType, interfaceType, lifeTime);

                    }
                case ObjLifetime.Singleton:
                    {
                        if (IsObjectCreated(implementationType))
                            return GetCreatedObject(implementationType);

                        if (IsOpenGenerics)
                            return CreateObject(parametersToPass, implementationType.MakeGenericType(interfaceType.GenericTypeArguments), interfaceType, lifeTime);

                        return CreateObject(parametersToPass, implementationType, interfaceType, lifeTime);
                    }
                case ObjLifetime.SingletonInstance:
                    {
                        return m_Configuration.GetSingletonInstance(interfaceType);
                    }
                default:
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        private Object CreateObject(object[] constructorParams, Type implementationType, Type interfaceType, ObjLifetime lifetime)
        {
            object createdObj;

            if (constructorParams.Length.Equals(0))
            {
                createdObj = Activator.CreateInstance(implementationType);
            }
            else
            {
                createdObj = ReflectionHelper.GetConstructor(implementationType)?.Invoke(constructorParams);
            }

            if(lifetime == ObjLifetime.Singleton)
            {
                SingletonObjects.Add(new CreatedObject()
                {
                    ObjType = implementationType,
                    Interface = interfaceType,
                    SingletonInstance = createdObj
                });
            }

            return createdObj;
        }

        private object[] GetResolvedConstructorParameters(Type implementationType, Type interfaceType, bool IsOpenGenerics)
        {
            var constructorDependencies = GetConstructorDependencies(implementationType, interfaceType, IsOpenGenerics);

            var parametersToPass = new object[constructorDependencies.Count()];

            for (int dependency = 0; dependency < constructorDependencies.Count(); ++dependency)
            {
                parametersToPass[dependency] = ResolveCore(constructorDependencies[dependency]);
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

            foreach(var dependency in dependencies)
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
        private bool IsObjectCreated(Type t)
        {
            return SingletonObjects.Find(x => x.ObjType == t) != null;
        }

        private Object GetCreatedObject(Type t)
        {
            return SingletonObjects.Find(x => x.ObjType == t)?.SingletonInstance;
        }
    }
}

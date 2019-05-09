using DenInject.Core.Cache;
using DenInject.Core.Expressions;
using DenInject.Core.Extensions;
using DenInject.Core.Injectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DenInject.Core.Activators
{
    public class DiActivator
    {
        public static ILazyExpressionTreesCompiler<Func<object>> LazyExpressionTreesCompiler { get; set; }

        public ICache ResolvedInstances { get; set; }

        private List<CreatedObject> SingletonObjects { get; set; }

        private DiConfiguration m_Configuration { get; set; }

        private ConstructorInjector injector { get; set; }

        public DiActivator(DiConfiguration configuration, ConstructorInjector injector)
        {
            LazyExpressionTreesCompiler = new LazyExpressionTreesCompiler(new LazyExpressionTreesCache());
            SingletonObjects = new List<CreatedObject>();
            m_Configuration = configuration;
            this.injector = injector;
            ResolvedInstances = new DependenciesCache();
        }

        /// <summary>
        /// Non-generic version of <see cref="Resolve{TInterface}"/>
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public Object Activate(Type interfaceType)
        {
            if (ResolvedInstances.HasDefinitionFor(interfaceType))
            {
                return ResolvedInstances.GetResolvedInstance(interfaceType);
            }

            bool IsAllImplementationsRequested = false;

            ContainerEntity containerEntity = m_Configuration.GetRegisteredEntity(interfaceType);

            if (containerEntity == null)
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
            {
                var res = result.ListObjToEnumerableType(interfaceType);
                ResolvedInstances.NewInstance(interfaceType, res);
                return res;
            }
            else
            {
                ResolvedInstances.NewInstance(interfaceType, result.First());
                return result.First();
            }
        }


        private Object CreateLazy(Type lazyType)
        {
            return LazyExpressionTreesCompiler.Compile(lazyType).Invoke();
        }

        private Object CreateObjectRecursive(Type implementationType, Type interfaceType, bool IsOpenGenerics)
        {
            var parametersToPass = injector.GetResolvedConstructorParameters(implementationType, interfaceType, IsOpenGenerics);

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
            object createdObj = ReflectionHelper.CreateObject(constructorParams, implementationType);

            if (lifetime.Equals(ObjLifetime.Singleton))
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

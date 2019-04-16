using DenInject.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DenInject.Core {
    public class DependencyProvider {
        private DiConfiguration m_Configuration { get; set; }

        private DiValidator Validator { get; set; }

        private List<CreatedObject> SingletonObjects { get; set; }

        public DependencyProvider(DiConfiguration config)
        {
            m_Configuration = config;
            SingletonObjects = new List<CreatedObject>();
            Validator = new DiValidator(config.Configuration);
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

        private Object ResolveCore(Type interfaceType)
        {
            bool IsAllImplementationsRequested = false;

            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                //check if this is request for all implementations / only one 
                //i.e IEnumerable<T> => T implementations or IEnumerable<T> implementation

                var IsenumerableRegistered = 
                m_Configuration
                .Configuration  //IEnumerable<T> implementation exists
                .Any(x => x.InterfaceType == interfaceType);

                if (!IsenumerableRegistered)
                {
                    interfaceType = interfaceType.GetGenericArguments()[0];
                    IsAllImplementationsRequested = true;
                }
            }

            var entity = 
                m_Configuration
                .Configuration
                .Find(x => x.InterfaceType == interfaceType);

            //if no type was found, probably we are using open generics. If not, entity was not registered.
            if (entity == null && interfaceType.IsGenericType)
                entity = m_Configuration
                        .Configuration
                        .Find(x => x.InterfaceType == interfaceType.GetGenericTypeDefinition());



            if (entity == null)
                throw new InvalidOperationException($"Dependency {interfaceType.ToString()} was not registered in container.");

            var result = new List<Object>();

            var implCount = entity.Implementations?.Count();


            for (int implementation = 0; implCount != null && implementation < implCount; ++implementation)
            {
                result.Add(CreateObjectRecursive(entity.Implementations[implementation].ImplType, interfaceType, entity.InterfaceType.IsGenericTypeDefinition));
            }

            if (IsAllImplementationsRequested)
                return result.ListObjToEnumerableType(interfaceType);
            else
                return result.First();
        }

        private Object CreateObjectRecursive(Type classType, Type interfaceType, bool IsOpenGenerics)
        {
            var constructorDependencies = GetConstructorDependencies(classType, interfaceType, IsOpenGenerics);

            if (constructorDependencies.Count().Equals(0))
              return CreateObjectCore(null, classType, interfaceType, IsOpenGenerics);

            var parametersToPass = new object[constructorDependencies.Count()];

            for (int dependency = 0; dependency < constructorDependencies.Count(); ++dependency)
            {
                parametersToPass[dependency] = ResolveCore(constructorDependencies[dependency]);
            }

            return CreateObjectCore(parametersToPass, classType, interfaceType, IsOpenGenerics);
        }

        private Object CreateObjectCore(object[] constructorParams, Type implementationType, Type interfaceType, bool IsOpenGenerics)
        {
            var lifeTime = m_Configuration.GetObjectLifeTime(implementationType);

            switch (lifeTime)
            {
                case ObjLifetime.Transient:
                    {
                         if (IsOpenGenerics)
                            return CreateGenericObject(constructorParams, implementationType, interfaceType, lifeTime);

                        return CreateObjInternal(constructorParams, implementationType, interfaceType, lifeTime);

                    }
                case ObjLifetime.Singleton:
                    {
                        if (IsObjectCreated(implementationType))
                            return GetCreatedObject(implementationType);

                        if (IsOpenGenerics)
                            return CreateGenericObject(constructorParams, implementationType, interfaceType, lifeTime);

                        return CreateObjInternal(constructorParams, implementationType, interfaceType, lifeTime);
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

        private Object CreateGenericObject(object[] constructorParams, Type implementationType, Type interfaceType, ObjLifetime lifetime)
        {
            //Set object type arguments to ones mentioned in container.
            return CreateObjInternal(constructorParams, implementationType.MakeGenericType(interfaceType.GenericTypeArguments), interfaceType, lifetime);
        }

        private Object CreateObjInternal(object[] constructorParams, Type implementationType, Type interfaceType, ObjLifetime lifetime)
        {
            object createdObj;

            if (constructorParams == null)
            {
                createdObj = Activator.CreateInstance(implementationType);
            }
            else
            {
                createdObj = GetConstructor(implementationType).Invoke(constructorParams);
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

        private bool IsObjectCreated(Type t)
        {
            return SingletonObjects.Find(x => x.ObjType == t) != null;
        }

        private Object GetCreatedObject(Type t)
        {
            return SingletonObjects.Find(x => x.ObjType == t).SingletonInstance;
        }

        private List<Type> GetConstructorDependencies(Type classType, Type interfaceType, bool IsOpenGenerics)
        {
            var constructor = GetConstructor(classType);

            if (constructor == null)
                return null;

            var dependencies = constructor
            .GetParameters()
            .Select(x => x.ParameterType)
            .ToList();

            if (!IsOpenGenerics)
                return dependencies;

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

        private ConstructorInfo GetConstructor(Type classType)
        {
            var constructors = classType.GetConstructors();
            return constructors.Length == 0 ? null : constructors[0];
        }
    }
}

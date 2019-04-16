using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DenInject.Core {
    public class DiConfiguration {

        public DiConfiguration()
        {
            Configuration = new List<ContainerEntity>();
        }

        internal List<ContainerEntity> Configuration { get; private set; }

        /// <summary>
        /// Overload for Open generics i.e RegisterTransient(typeof(IService<>), typeof(SomeService<>));
        /// Resolving: provider.Resolve<IService<SomeRepository>>()
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="implementationType"></param>
        public void RegisterTransient(Type interfaceType, Type implementationType)
        {
            if (interfaceType.IsValueType || implementationType.IsValueType)
                throw new InvalidOperationException("Both implementation and interface should be reference types.");

            RegisterCore(interfaceType, implementationType, ObjLifetime.Transient);
        }

        /// <summary>
        /// Overload for open generics: i.e RegisterTransient(typeof(IService<>), typeof(SomeService<>));
        /// Resolving: provider.Resolve<IService<SomeRepository>>()
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="implementationType"></param>
        public void RegisterSingleton(Type interfaceType, Type implementationType)
        {
            if (interfaceType.IsValueType || implementationType.IsValueType)
                throw new InvalidOperationException("Both implementation and interface should be reference types.");

            RegisterCore(interfaceType, implementationType, ObjLifetime.Singleton);
        }

        /// <summary>
        ///  Registers an object as transient instance.
        ///  This means that new object'll be created for every call.
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        public void RegisterTransient<TInterface, TImplementation>() 
            where TInterface : class
            where TImplementation : class
        {
            RegisterCore(typeof(TInterface), typeof(TImplementation),ObjLifetime.Transient);
        }

        /// <summary>
        ///  Registers an object as singleton.
        ///  It'll be created only once.
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        public void RegisterSingleton<TInterface, TImplementation>()
            where TInterface : class 
            where TImplementation : class
        {
            RegisterCore(typeof(TInterface), typeof(TImplementation), ObjLifetime.Singleton);
        }

        /// <summary>
        /// Registers an object as singleton instance. 
        /// For each call will return the same instance, as passed in parameter.
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="instance"></param>
        public void RegisterSingleton<TInterface>(object instance)
        {
            ValidateInterfaceAndImplementation(typeof(TInterface), instance.GetType());

            RegisterSingletonInstance(typeof(TInterface), instance);
        }

        private void RegisterCore(Type interfaceType, Type implementationType,  ObjLifetime lifetime)
        {
            //Registration 'as-self'
            if(interfaceType == implementationType)
            {
                RegisterAsSelf(implementationType, lifetime);
                return;
            }

            ValidateInterfaceAndImplementation(interfaceType, implementationType);

            var entity =
                from x in Configuration
                where x.InterfaceType == interfaceType
                select x;

            if (entity.Count().Equals(0))
            {
                Configuration.Add(new ContainerEntity()
                {
                    Implementations = new List<Implementation>()
                    {
                        new Implementation()
                        {
                            ImplType = implementationType,
                            LifeTime = lifetime
                        }
                    },
                    InterfaceType = interfaceType,
                });

                return;
            }

            if (entity.First().Implementations.FirstOrDefault(x => x.ImplType == implementationType) != null)
                return;

            entity.First().Implementations.Add(new Implementation() { ImplType = implementationType, LifeTime = lifetime });
        }

        private void RegisterSingletonInstance(Type interfaceType, object instance)
        {
            var entity =
               from x in Configuration
               where x.InterfaceType == interfaceType
               select x;

            if (entity.Count().Equals(0))
            {
                Configuration.Add(new ContainerEntity()
                {
                    Implementations = new List<Implementation>()
                    {
                        new Implementation()
                        {
                            ImplType = instance.GetType(),
                            LifeTime = ObjLifetime.SingletonInstance,
                            SingletonInstance = instance
                        }
                    },
                    InterfaceType = interfaceType,
                    
                });

                return;
            }

            throw new InvalidOperationException($"{interfaceType.Name} is already registered.");
        }

        private void RegisterAsSelf(Type implementationType, ObjLifetime lifetime)
        {
            var Entity =
                   from x in Configuration
                   where x.InterfaceType == implementationType
                   select x;

            if (implementationType.IsInterface)
                throw new InvalidOperationException("Cannot register interface as-self.");

            if (Entity.Count().Equals(0))
            {
                Configuration.Add(new ContainerEntity()
                {
                    InterfaceType = implementationType,
                    Implementations = new List<Implementation>()
                    {
                        new Implementation()
                        {
                            ImplType = implementationType,
                            LifeTime = lifetime
                        }
                    }
                });

                return;
            }

            throw new InvalidOperationException($"{implementationType.Name} is already registered.");
        }

        internal ObjLifetime GetObjectLifeTime(Type implType)
        {
            return (from conf in Configuration from impl in conf.Implementations where impl.ImplType == implType select impl.LifeTime).First();
        }

        internal object GetSingletonInstance(Type interfaceType)
        {
            
            return (from conf in Configuration
                    where conf.InterfaceType == interfaceType
                    from impl in conf.Implementations
                    select impl.SingletonInstance).First();
        }

        private void ValidateInterfaceAndImplementation(Type interfaceType, Type implementationType)
        {
            if (implementationType.GetInterfaces().FirstOrDefault(x => x.Name == interfaceType.Name) == null)
                throw new InvalidOperationException($"Type {implementationType.ToString()} is not assignable from {interfaceType.ToString()}");

            if (implementationType.IsAbstract || implementationType.IsInterface)
                throw new InvalidOperationException($"Type {implementationType.ToString()} couldn't be abstract or interface.");
        }


    }
}

using DenInject.Core.Extensions;
using DenInject.Core.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DenInject.Core.Cache;
using DenInject.Core.Activators;
using DenInject.Core.Injectors;

namespace DenInject.Core {

    /// <summary>
    /// Represents a DI container.
    /// To support <see cref="Lazy{T}"/> initialization(weak reference),
    /// Set instance field of <see cref="DependencyProvider"/> to desired <see cref="DependencyProvider"/>
    /// </summary>
    public class DependencyProvider {
        private DiConfiguration m_Configuration { get; set; }

        private DiValidator Validator { get; set; }

        /// <summary>
        /// Used by <see cref="LazyExpressionTreesCompiler"/> to resolve <see cref="Lazy{T}"/>
        /// </summary>
        public static DiActivator activator { get; set; }

        public DependencyProvider(DiConfiguration config)
        {
            m_Configuration = config;
            Validator = new DiValidator(config.Configuration);
            activator = new DiActivator(config, new ConstructorInjector(config));
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
            return (TInterface)activator.Activate(typeof(TInterface));
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

    }
}

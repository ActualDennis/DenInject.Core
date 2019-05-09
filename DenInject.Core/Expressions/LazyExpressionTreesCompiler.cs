using DenInject.Core.Activators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DenInject.Core.Expressions
{
    public class LazyExpressionTreesCompiler : ILazyExpressionTreesCompiler<Func<object>>
    {
        public LazyExpressionTreesCompiler(ILazyExpressionTreesCache<Func<object>> cache)
        {
            this.cache = cache;
        }

        private ILazyExpressionTreesCache<Func<object>> cache { get; set; }

        public Func<object> Compile(Type lazyType)
        {
            if (cache.ContainsCacheFor(lazyType))
            {
                return cache.GetCompiledExpression(lazyType);
            }

            Type lazyClass = lazyType.GetGenericArguments()[0];

            var newObjectExpression =
                Expression.New
                (
                    lazyType.GetConstructor
                    (
                        new Type[1]
                        {
                            typeof(Func<>).MakeGenericType(lazyClass)
                        }
                    ),
                    new Expression[1]
                    {
                    Expression.Lambda(

                            Expression.Convert
                            (
                                Expression.Call
                                (
                                    Expression.Constant(DependencyProvider.activator, typeof(DiActivator)),
                                    typeof(DependencyProvider)
                                    .GetProperty("activator")
                                        .PropertyType
                                            .GetMethod("Activate"),
                                    Expression.Constant(lazyClass)
                                ),
                                lazyClass
                            )
                        )
                    }
                );

            var compiled = Expression.Lambda<Func<object>>(newObjectExpression).Compile();

            cache.AddCompiledExpression(compiled, lazyType);

            return compiled;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DenInject.Core.Expressions
{
    public class LazyExpressionTreesCache : ILazyExpressionTreesCache<Func<object>>
    {
        public LazyExpressionTreesCache()
        {
            Cache = new Dictionary<Type, Func<object>>();
        }

        private Dictionary<Type, Func<object>> Cache { get; set; }


        public bool ContainsCacheFor(Type t)
        {
            return Cache.ContainsKey(t);
        }

        public void AddCompiledExpression(Func<object> value, Type t)
        {
            if (!ContainsCacheFor(t))
            {
                Cache.Add(t, value);
            }
        }

        public Func<object> GetCompiledExpression(Type t)
        {
            return Cache[t];
        }
    }
}

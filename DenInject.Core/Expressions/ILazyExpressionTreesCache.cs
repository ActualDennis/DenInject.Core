using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DenInject.Core.Expressions
{
    public interface ILazyExpressionTreesCache<T>
    {
        T GetCompiledExpression(Type t);

        bool ContainsCacheFor(Type t);

        void AddCompiledExpression(T value, Type t);
    }
}

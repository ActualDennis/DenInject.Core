using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DenInject.Core.Expressions
{
    public interface ILazyExpressionTreesCompiler<T>
    {
        T Compile(Type lazyType);
    }
}

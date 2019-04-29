using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DenInject.Core.Expressions
{
    internal static class ReflectionHelper
    {
        public static ConstructorInfo GetConstructor(Type classType)
        {
            var constructors = classType.GetConstructors();
            return constructors.Length == 0 ? null : constructors[0];
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DenInject.Core.Cache
{
    public interface ICache
    {
        bool HasDefinitionFor(Type t);

        Object GetResolvedInstance(Type t);

        void NewInstance(Type t, Object instance);
    }
}

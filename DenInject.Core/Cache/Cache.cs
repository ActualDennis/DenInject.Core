using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DenInject.Core.Cache
{
    public class DependenciesCache : ICache
    {
        public DependenciesCache()
        {
            Resolved = new Dictionary<Type, object>();
        }

        private Dictionary<Type, Object> Resolved { get; set; }

        public object GetResolvedInstance(Type t)
        {
            if (Resolved.ContainsKey(t))
            {
                return Resolved[t];
            }

            return null;
        }
        public bool HasDefinitionFor(Type t)
        {
            return Resolved.ContainsKey(t);
        }

        public void NewInstance(Type t, object instance)
        {
            if (!HasDefinitionFor(t))
            {
                Resolved.Add(t, instance);
            }
        }
    }
}

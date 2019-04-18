using System;
using System.Collections.Generic;
using System.Text;

namespace DenInject.Core {
    public class Implementation {
        public Type ImplType { get; set; }

        public object SingletonInstance { get; set; }

        public ObjLifetime LifeTime { get; set; }
    }
}

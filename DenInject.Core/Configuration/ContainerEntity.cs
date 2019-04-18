using System;
using System.Collections.Generic;
using System.Text;

namespace DenInject.Core {
    public class ContainerEntity {
        public Type InterfaceType { get; set; }

        public List<Implementation> Implementations { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DenInject.Tests.TestData.Generics {
    public class SomeRepository : IRepository {
        public void AddTo(int index) { }
        public void DeleteAt(int index) { }

        public int field { get; set; }
    }
}

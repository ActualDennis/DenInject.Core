using System;
using System.Collections.Generic;
using System.Text;

namespace DenInject.Tests.TestData.Generics {
    public class SomeService<TRepository> : IService<TRepository> where TRepository : IRepository{
        public SomeService(TRepository Repository)
        {
            this.Repository = Repository;
        }
        public TRepository Repository { get; set; }
    }
}

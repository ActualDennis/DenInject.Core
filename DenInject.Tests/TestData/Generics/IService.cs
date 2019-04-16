using System;
using System.Collections.Generic;
using System.Text;

namespace DenInject.Tests.TestData.Generics {
    public interface IService<TRepository> where TRepository : IRepository {
        
        TRepository Repository { get; set; }

    }
}

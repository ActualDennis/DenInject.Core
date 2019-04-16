using System;
using System.Collections.Generic;
using System.Text;

namespace DenInject.Tests.TestData {
    public class FirstProduct : IProduct {
        public IUser user { get; set; }

        public FirstProduct(IUser user)
        {
            this.user = user;
        }
        public void Sell()
        {
            //do something
        }
    }
}

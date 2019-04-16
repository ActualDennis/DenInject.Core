using DenInject.Core;
using DenInject.Tests.TestData;
using DenInject.Tests.TestData.Generics;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests {
    public class Tests {
        DiConfiguration config;
        DependencyProvider provider;
        [SetUp]
        public void Setup()
        {
            config = new DiConfiguration();
        }

        [Test]
        public void BasicTest()
        { 
            config.RegisterSingleton<IProduct, FirstProduct>();
            config.RegisterTransient<IUser, User>();

            provider = new DependencyProvider(config);

            IProduct item = null;

            Assert.DoesNotThrow(() => provider.ValidateConfig());

            Assert.DoesNotThrow(() => item = provider.Resolve<IProduct>());

            Assert.NotNull(item);
        }

        [Test]
        public void ManyTest()
        {
            config.RegisterSingleton<IProduct, FirstProduct>();
            config.RegisterSingleton<IProduct, SecondProduct>();
            config.RegisterTransient<IUser, User>();

            provider = new DependencyProvider(config);

            IEnumerable<IProduct> items = null;

            Assert.DoesNotThrow(() => provider.ValidateConfig());

            Assert.DoesNotThrow(() => items = provider.Resolve<IEnumerable<IProduct>>());

            Assert.NotNull(items);

            Assert.That(items.Count() == 2);
        }

        [Test]
        public void OpenGenericsTest()
        {
            config.RegisterTransient<IRepository, SomeRepository>();
            config.RegisterTransient(typeof(IService<>), typeof(SomeService<>));
            provider = new DependencyProvider(config);

            IService<SomeRepository> item = null;

            Assert.DoesNotThrow( () => provider.ValidateConfig());

            Assert.DoesNotThrow( () => item = provider.Resolve<IService<SomeRepository>>());

            Assert.NotNull(item);
        }

        [Test]
        public void GenericsTest()
        {
            config.RegisterTransient<IRepository, SomeRepository>();
            config.RegisterTransient<IService<IRepository>, SomeService<IRepository>>();
            provider = new DependencyProvider(config);

            IService<IRepository> item = null;

            Assert.DoesNotThrow(() => provider.ValidateConfig());

            Assert.DoesNotThrow(() => item = provider.Resolve<IService<IRepository>>());

            Assert.NotNull(item);
        }

        [Test]
        public void AsSelfTest()
        {
            config.RegisterTransient<SomeRepository, SomeRepository>();
            provider = new DependencyProvider(config);
            SomeRepository item = null;

            Assert.DoesNotThrow(() => provider.ValidateConfig());

            Assert.DoesNotThrow(() => item = provider.Resolve<SomeRepository>());

            Assert.NotNull(item);
        }

        [Test]

        public void IenumerableTest()
        {
            Assert.Throws<InvalidOperationException>(() => config.RegisterTransient<IEnumerable<IRepository>, IEnumerable<IRepository>>());
        }

        [Test]
        public void IenumerableTest2()
        {
            config.RegisterTransient<IRepository, SomeRepository>();
            config.RegisterTransient<IEnumerable<IRepository>, List<IRepository>>();
            provider = new DependencyProvider(config);

            IEnumerable<IRepository> item = null;

            Assert.DoesNotThrow(() => provider.ValidateConfig());

            Assert.DoesNotThrow(() => item = provider.Resolve<IEnumerable<IRepository>>());

            Assert.That(item.GetType() == typeof(List<IRepository>));

            Assert.NotNull(item);
        }

        [Test] 
        public void SingletonInstanceTest()
        {
            config.RegisterSingleton<IUser>(new User() { Balance = 5427 });

            provider = new DependencyProvider(config);

            IUser item = null;

            Assert.DoesNotThrow(() => provider.ValidateConfig());

            Assert.DoesNotThrow(() => item = provider.Resolve<IUser>());

            Assert.That(item.GetBalance() == 5427);

            Assert.NotNull(item);
        }
    }
}
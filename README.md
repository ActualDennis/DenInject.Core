# DenInject.Core
If you have no idea what DI is and what it's for, see [this](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.2)

## What can it do?

1. Transient / singleton / singleton instance dependencies registration.

``` csharp
var config = new DiConfiguration();

//New call - new instance of SomeRepository
config.RegisterTransient<SomeRepository, SomeRepository>();

//New call - same instance of FirstProduct
config.RegisterSingleton<IProduct, FirstProduct>();

//Singleton, but with manual object creation - Singleton Instance
config.RegisterSingleton<IUser>(new User() { Balance = 5427 });
```

2. One to many registration.

``` csharp

config.RegisterTransient<IProduct, FirstProduct>();
config.RegisterTransient<IProduct, SecondProduct>();
... 

//This will return every implementation: FirstProduct, SecondProduct etc.
provider.Resolve<IEnumerable<IProduct>>();

//this will also work

 config.RegisterTransient<IRepository, SomeRepository>();
 config.RegisterTransient<IEnumerable<IRepository>, List<IRepository>>();
 
 //returns List<IRepository>
 provider.Resolve<IEnumerable<IRepository>>()

```

3. Open generics:
``` csharp

config.RegisterTransient<IRepository, SomeRepository>();
config.RegisterTransient(typeof(IService<>), typeof(SomeService<>));

//Fill parameters at runtime

provider.Resolve<IService<SomeRepository>>()

```

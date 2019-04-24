# DenInject.Core
If you have no idea what DI is and what it's for, see [this](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.2)

## What can it do?

### 1. Transient / singleton / singleton instance dependencies registration.

Based on constructor dependencies [injection](https://en.wikipedia.org/wiki/Dependency_injection#Constructor_injection_comparison) 

``` csharp
var config = new DiConfiguration();

//New call - new instance of SomeRepository
config.RegisterTransient<SomeRepository, SomeRepository>();

//New call - same instance of FirstProduct
config.RegisterSingleton<IProduct, FirstProduct>();

//Singleton, but with manual object creation - Singleton Instance
config.RegisterSingleton<IUser>(new User() { Balance = 5427 });
```

### 2. One to many registration.

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

### 3. Open generics:
``` csharp

config.RegisterTransient<IRepository, SomeRepository>();
config.RegisterTransient(typeof(IService<>), typeof(SomeService<>));

//Fill parameters at runtime

provider.Resolve<IService<SomeRepository>>()

```

### 4. Weak references:

Let's say we have these two constructors:
``` csharp
 public FirstProduct(IUser user)// FirstProduct implements IProduct
 {
     this.user = user;
 }

public User(IProduct product)//User implements IUser
{
    x = product;
}
```

We'll be unable to create user or product, because they rely on each other. To fix this, wrap either IUser or IProduct in Lazy<T> class like this:
 
 ``` csharp
//creates with no issues
public User(Lazy<IProduct> product)
{
    x = product;
}
```

## Usage

1. Registering dependencies:

``` csharp

var config = new DiConfiguration();
config.RegisterTransient<Interface, Implementation>();
config.RegisterSingleton<Interface1, Implementation1>();
...
```

2.Creating Dependency Provider:

```csharp

//should be created once in app lifecycle.

var provider = new DependencyProvider(config);

```

3. Resolving dependencies:

```csharp

var x = provider.Resolve<Interface>();
//x - Implementation.
```

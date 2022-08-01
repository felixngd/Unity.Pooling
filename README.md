# Unity.Pooling

## Dependencies

- [com.cysharp.unitask](https://github.com/Cysharp/UniTask)
- [com.laicasaane.collections.pooled](https://github.com/laicasaane/Collections.Pooled)

## Addressables support

The related modules will be enabled automatically when the [com.unity.addressables](https://docs.unity3d.com/Packages/com.unity.addressables@1.19/manual/index.html) package is installed.


# Installation

## Install via Open UPM

You can install this package from the [Open UPM](https://openupm.com/packages/com.laicasaane.unity.pooling/) registry.

More details [here](https://github.com/openupm/openupm-cli#installation).

```
openupm add com.laicasaane.unity.pooling
```

## Install via Package Manager

Or, you can add this package by opening the **Package Manager** window and entering

```
https://github.com/laicasaane/Unity.Pooling.git?path=Packages/com.laicasaane.unity.pooling
```

from the `Add package from git URL` option.


# System.Pooling

## Foundation

The entire package is built upon these 2 interfaces:
- `IPool<T>` defines synchronous APIs, mostly for pooling C# objects.
- `IAsyncPool<T>` defines asynchronous APIs, mostly for pooling Unity objects.

## Pools

These are the basic pools that implement the most generic functions for pooling C# objects:

- `Pool<T, TInstantiator>`
- `AsyncPool<T, T Instantiator>`
- `Pool<T>`
- `AsyncPool<T>`

For basic pooling, `Pool<T>` and `AsyncPool<T>` are ready-to-use.

## Instantiators

It should be noted that `Pool<T>` and `AsyncPool<T>` will create instances of `T` through [`Activator.CreateInstance`](https://docs.microsoft.com/en-us/dotnet/api/system.activator.createinstance) by using these instantiators:

- `ActivatorInstantiator<T>`
- `AsyncActivatorInstantiator<T>`

This module also provides instantiators that use default constructor:

- `DefaultConstructorInstantiator<T>`
- `AsyncDefaultConstructorInstantiator<T>`

However the type `T` must satisfy this constraint on the instantiators

```cs
where T : class, new()
```

- `T` must be a reference type
- `T` must have a public parameterless constructor (ie. default constructor)

**Example usage**

```cs
public class MyClass { }
...
var pool = new Pool<MyClass, DefaultConstructorInstantiator<MyClass>>(); // Works
...
var pool = new Pool<string, DefaultConstructorInstantiator<string>>(); // Error!
```

### User-defined instantiators

Alternately, users can define custom instantiators for specific types by implement `IInstantiable<T>`:

```cs
public struct MyClassInstantiator : IInstantiable<MyClass>
{
    public MyClass Instantiate() => new MyClass();
}
...
var pool = new Pool<MyClass, MyClassInstantiator>();
```

## Shared pools

`SharedPool.Of<T>` will return a shared instance (aka singleton) of any type that satisfy this constraint:

```cs
where T : IPool, IShareable, new()
```

- `T` must implement `IPool` and `IShareable`
- `T` must have a public parameterless constructor (ie. default constructor)

**Example usage**

```cs
var myClassPool = SharedPool.Of< Pool<MyClass> >();
var listPool    = SharedPool.Of< ListPool<int> >();
```

## Disposable context

Leverage `IDisposable` interface to automatically return instances to the pool at the end of their usage.

1. Get the context by calling `.DisposableContext()` (extension method) on the pool.
2. Apply `using` when renting from the context so the disposing mechanism can be automated.

**Example usage**

```cs
var pool           = SharedPool.Of< ListPool<int> >();
var disposablePool = pool.DisposableContext();         // Get the context
using var instance = disposablePool.Rent();            // Rent from the context with `using`

using (var otherInstance = disposablePool.Rent())     // Explicit `using` scope
{
    // work with `otherInstance`

} // `otherInstance` will be returned to the pool automatically

// End of the Method
// `instance` will be returned to the pool automatically
```

# Collection Pools

Are the pools for collection instances (such as `List`, `Dictionary` and so on).

- `ListPool<T>`
- `HashSetPool<T>`
- `QueuePool<T>`
- `StackPool<T>`
- `DictionaryPool<TKey, TValue>`

They are implemented the same in these 2 modules:

- `System.Collections.Generic.Pooling`
- `Collections.Pooled.Generic.Pooling`

```cs
var listInst    = SharedPool.Of< ListPool<int> >().Rent();
var myClassInst = SharedPool.Of< Pool<MyClass> >().Rent();

var dictPool = new DictionaryPool<int, string>();
var dictInst = dictPool.Rent();
```

# Unity.Pooling

This module provides basic facility to pooling Unity objects, especially `GameObject` and `Component`:

- `UnityPool<T, TPrefab>` is the base for Unity object pools.
- `IPrefab` to implement custom Unity object instantiators.
- `IPrepooler<T, TPrefab, TPool>` to implement custom prepooling mechanism.
- `UnityPoolBehaviour<T, ...>` is the base for pool components attached on a `GameObject`.

## Pools

All the pools implemented in this module are ready-to-use.

- `UnityPool<T, TPrefab>`
- `GameObjectPool<TPrefab>`
- `GameObjectPool`
- `ComponentPool<T, TPrefab>`
- `ComponentPool<T>`

```cs
var goPool       = new GameObjectPool();
var colliderPool = new ComponentPool<BoxCollider>();
```

## Prefabs

Prefab is a way to change how pools would instantiate or release their objects.

- `UnityPrefab<T, TSource>`
- `GameObjectPrefab`
- `ComponentPrefab<T>`

If customization is needed, a subtype of `UnityPrefab<T, TSource>` must be implemented then passed to the generic pool that accept `TPrefab`.

```cs
public class CustomGameObjectPrefab : UnityPrefab<GameObject, GameObject>
{
    protected override async UniTask<GameObject> Instantiate(GameObject source, Transform parent)
    {
        // implement Instiatate method
    }

    public override void Release(GameObject instance)
    {
        // implement Release method
    }
}
...
var poolA = new GameObjectPool<CustomGameObjectPrefab>();
var poolB = new UnityPool<GameObject, CustomGameObjectPrefab>()
```

Alternately any type implements `IPrefab<T>` works the same way.

```cs
public class SimpleGameObjectPrefab : IPrefab<GameObject>
{
    public async UniTask<GameObject> Instantiate()
    {
        // implement Instiatate method
    }

    public void Release(GameObject instance)
    {
        // implement Release method
    }
}
...
var poolA = new GameObjectPool<SimpleGameObjectPrefab>();
var poolB = new UnityPool<GameObject, SimpleGameObjectPrefab>()
```

## Behaviours

These are wrappers of the pools, derived from `MonoBehaviour`:

- `PoolBehaviour<T, TPool>`
- `UnityPoolBehaviour<T, TPrefab, TPool>`
- `GameObjectPoolBehaviour`
- `ComponentPoolBehaviour<T>`

Only `GameObjectPoolBehaviour` is ready-to-use.

Others require declaring non-generic subtypes:

```cs
public class BoxColliderPoolBehaviour
    : ComponentPoolBehaviour<BoxCollider>
{ }

public class SpritePoolBehaviour
    : UnityPoolBehaviour<SpriteRenderer, SpritePrefab, ComponentPool<SpriteRenderer>>
{ }
```

# Unity.Pooling.Addressables

This module offers 2 ways to work with Addressables:
- `Address*` requires a `string` address to load the asset
- `AssetRef*` requires an `AssetReference`

## Pools

All the pools implemented in this module are ready-to-use.

- `AddressComponentPool<T, TPrefab>`
- `AddressComponentPool<T>`
- `AddressGameObjectPool`
- `AddressGameObjectPool<TPrefab>`
- `AssetRefComponentPool<T>`
- `AssetRefGameObjectPool`

## Prefabs

- `AddressPrefab<T>`
- `AddressComponentPrefab<T>`
- `AddressGameObjectPrefab`
- `AssetRefPrefab<T, TAssetRef>`
- `AssetRefComponentPrefab<T>`
- `AssetRefGameObjectPrefab`

It should be noted that the prefabs

# Unity.Pooling.Scriptables

# Unity.Pooling.Scriptables.Addressables


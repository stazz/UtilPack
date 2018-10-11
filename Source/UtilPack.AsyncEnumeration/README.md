# UtilPack.AsyncEnumeration

While [async enumerables](https://github.com/dotnet/csharplang/issues/43) are still incoming (old link: https://github.com/dotnet/roslyn/issues/261), this project aims to provide API to work with async enumerables.
In the context of this project, async enumerables may be e.g. results of SQL query, or a collection of objects deserialized from e.g. network stream.
The link above seems to discuss also about scenarios like sending query syntax tree of the code enumerating async enumerable to server, which will evaluate it, but this kind of asynchrony is out of the scope of this project.

The best way to start is `UtilPack.AsyncEnumeration.AsyncEnumerationFactory` class.
As the name suggests, it can be used to create classes implementing various `IAsyncEnumerable<T>` and `IAsyncEnumerator<T>` interfaces.
Just like the synchronous `System.Collections.Generic.IEnumerator<T>` interface, the enumerator starts with being positioned just before first element.

# IAsyncEnumerable
This is just like its synchronous counterpart `IEnumerable<T>` interface, except for asynchronous environment.
One should use `EnumerateSequentiallyAsync` extension method to enumerate the items of `IAsyncEnumerable<T>`, as the proper language syntax is still lacking.

# aLINQ
This library also provides a number of methods that correspond to the ones available as LINQ methods to normal `IEnumerable<T>` interface.
These methods accept both synchronous and asynchronous calllbacks.

# Observability
The `IAsyncEnumerable<T>` may also be converted to its observable counterpart using `AsObservable` extension methods.
The extension methods make sure not to wrap too many times.

The observable events are listed below.
* The `BeforeEnumerationStart` event occurs just before starting enumeration in __initial__ `WaitForNextAsync` call.
* The `AfterEnumerationStart` event occurs just after starting enumeration in __initial__ `WaitForNextAsync` call.
* The `AfterEnumerationItemEncountered` event occurs just after next item is successfully fetched by `TryGetNext` call.
* The `BeforeEnumerationEnd` event occurs just before `Dispose` call.
* The `AfterEnumerationEnd` events occurs just after `Dispose` call.

# Creation
Typically instances of `IAsyncEnumerable<T>` are obtained by using e.g. [CBAM framework](https://github.com/stazz/CBAM).
The framework uses the `AsyncEnumerationFactory` static class provided by this library, in order to create various instances of `IAsyncEnumerable<T>`, which may be used by other libraries as well.

# Distribution
See [NuGet package](http://www.nuget.org/packages/UtilPack.AsyncEnumeration) for binary distribution.
# UtilPack.AsyncEnumeration

While [async enumerables](https://github.com/dotnet/csharplang/issues/43) are still incoming (old link: https://github.com/dotnet/roslyn/issues/261), this project aims to provide API to work with async enumerables.
In the context of this project, async enumerables may be e.g. results of SQL query, or a collection of objects deserialized from e.g. network stream.
The link above seems to discuss also about scenarios like sending query syntax tree of the code enumerating async enumerable to server, which will evaluate it, but this kind of asynchrony is out of the scope of this project.

The best way to start is `UtilPack.AsyncEnumeration.AsyncEnumeratorFactory` class.
As the name suggests, it can be used to create classes implementing various `UtilPack.AsyncEnumeration.AsyncEnumerator` interfaces.
Note that there is no `AsyncEnumerable`, only `AsyncEnumerator` - the common pattern to enumerate the `AsyncEnumerator` is exposed via `EnumerateAsync` extension method.
Just like the synchronous `System.Collections.Generic.IEnumerator<T>` interface, the enumerator starts with being positioned just before first element.

# AsyncEnumerator
There are two variations of `AsyncEnumerator` interface - with one generic type parameter, and with two generic type parameters.
The variation with one generic type parameter is the most simple - the type parameter is the type of each item in the asynchronously fetched sequence of items.
The second variation adds one additional generic type parameter, which is the type for `metadata` object.
This metadata object can be anything that is passed to the factory method - e.g. SQL statement from which this enumerator was obtained.

The members of `AsyncEnumerator` are listed below.
* The `MoveNextAsync` method mimics the `MoveNext` method of the `System.Collections.Generic.IEnumerator<T>` interface - it will return one-time-use integer token if next item is encountered, and `null` otherwise.
* The `OneTimeRetrieve` method will consume the integer token returned by `MoveNextAsync` and return the corresponding encountered item. Will return the `default` if the token is invalid or used up.
* The `TryResetAsync` method will try to reset the enumerator to its initial state. It will return `false` if enumerator is already in its initial state, or if used concurrently.

Note that generally, one shouldn't use `MoveNextAsync` directly, but instead use the `EnumerateSequentiallyAsync` and `EnumerateInParallelAsync` extension methods defined for `AsyncEnumerator` interface.

# AsyncEnumeratorObservable
Just like the `AsyncEnumerator`, there are two variations of `AsyncEnumeratorObservable` interface - with one generic type parameter, and with two generic type parameters.
The variation with two generic parameters, similarly to `AsyncEnumerator`, adds a type for the `metadata` object.
As the name suggests, the `AsyncEnumeratorObservable` interfaces expose events which are invoked at various stages when the `AsyncEnumeratorObservable` is enumerated.
The events are listed below.
* The `BeforeEnumerationStart` event occurs just before starting enumeration in __initial__ `MoveNextAsync` call.
* The `AfterEnumerationStart` event occurs just after starting enumeration in __initial__ `MoveNextAsync` call.
* The `AfterEnumerationItemEncountered` event occurs just after next item is asynchronously fetched in `MoveNextAsync` call.
* The `BeforeEnumerationEnd` and `AfterEnumerationEnd` events both occur just after enumeration end is detected in `MoveNextAsync` call. The `BeforeEnumerationEnd` is invoked first. If enumeration end includes asynchronous disposing (e.g. sending some acknowledgement message to the server), the disposing is done after `BeforeEnumerationEnd` event, but before `AfterEnumerationEnd` event. The disposing is otherwise not visible to the outside.

# Distribution
See [NuGet package](http://www.nuget.org/packages/UtilPack.AsyncEnumeration) for binary distribution.
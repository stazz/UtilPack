# UtilPack.JSON
This project provides a way to (de)serialize [Newtonsoft.JSON](https://github.com/JamesNK/Newtonsoft.Json) `JToken`s with `PotentiallyAsyncReader` and `PotentiallyAsyncWriter` types from [UtilPack](../UtilPack) project.
The `JTokenStreamReader` class in this project implements `PotentiallyAsyncReaderLogic<JToken, MemorizingPotentiallyAsyncReader<Char?, Char>>` interface, allowing deserialization of `JToken`s from any reader implementing `MemorizingPotentiallyAsyncReader<Char?, Char>` interface.
For serialization, this project provides `CreateJTokenWriter` extension method for `PotentiallyAsyncWriterLogic<IEnumerable<Char>, TSink>` type, returning `PotentiallyAsyncWriterAndObservable<JToken>` that can be used to serialize `JToken`s into a `IEnumerable<char>`.

See [NuGet package](http://www.nuget.org/packages/UtilPack.JSON) for binary distribution.
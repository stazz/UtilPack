# UtilPack.TabularData
This project contains interfaces and skeleton classes for querying and handling synchronous and asynchronous tabular data.
By tabular, it is meant that each datum is a row with 0 or more columns.
This project does not limit that all datums have same amount of columns, instead each row knows its own columns, allowing for more dynamic approach for special case scenarios.

Currently, synchronous API is not there - only asynchronous is implemented.

Using API of this library most usually starts with `AsyncDataRow` interface, which acts as entrypoint for asynchronous API.
It contains methods to access the columns (`AsyncDataColumn` interface) that the row contains, and also to get the `DataRowMetaData` object that tells about the data row itself.
Each column exposes methods to query the value (`TryGetValueAsync` and `ReadBytesAsync`), and also to get the `AsyncDataColumnMetaData` object that tells about the column itself and the value it contains.
Furthermore, the `AsyncDataRow` interface has multiple extension methods defined to make its use more easy.

Implementing the functionality of this library involves extending `AbstractAsyncDataColumnMetaData` or `DataColumnSUKS` (SUKS means Stream Unseekable and with Known Size) classes and implementing their abstract methods.
The documentation on these abstract methods should be used as guidance, but the main idea is that these methods should implement the actual logic that involves using asynchronous IO to obtain the value.

# Distribution
See [NuGet package](http://www.nuget.org/packages/UtilPack.TabularData) for binary distribution.
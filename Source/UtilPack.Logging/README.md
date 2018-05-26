# UtilPack.Logging

This is compact library aimed to solve three things:

* Adding log messages - by the code performing some logic that needs to be traced,
* Reacting to log messages - writing to file, or sending log information somewhere elsewhere, and
* Bootstrapping logging environment - dynamically configuring what will happen when the log event occurs.

All logging classes contain generic argument, which represents type that describes the log event.
Typically this type is an enumeration like this: `Debug`, `Info`, `Warning`, and `Error`.

Bootstrapping is done using the `UtilPack.Logging.Bootstrap.LogRegistration` class, and passing `UtilPack.Logging.Consume.LogConsumer` instances to it in order to construct which consumers will react to log events.
Then, typically the `CreateHandlerFromCurrentRegistrations` method is called on `LogRegistration`, in order to generate a `UtilPack.Logging.Publish.LogPublisher`.
This `LogPublisher` is then passed around to the code which will have a need to log various events.
The logging to `LogPublisher` can either `await` for potentially asynchronous logging to complete before proceeding (by using `PublishAsync` method), or continue right away without awaiting (by using `Publish` method), but both methods will always invoke all underlying `LogConsumer` instances.

This library provides `TextWriterLogger` that implements `LogConsumer` and writes to the given `System.IO.TextWriter`.
Furthermore, `ConsoleLoggerFactory` class is provided for other frameworks than .NET Standard 1.0-1.2, in order to provide easy-to-use way to log to console output and error streams.
The other `LogConumser` implementations are left for the other libraries to implement.

# Distribution

See [NuGet package](http://www.nuget.org/packages/UtilPack.Logging) for binary distribution.
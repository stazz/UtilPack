# UtilPack.ProcessMonitor

This project contains library which exposes `ProcessMonitor` class, that can be used to monitor other process, along with some extra functionality.
The `ProcessMonitor` class exposes one method, `KeepMonitoringAsync`, which will start the process, and keep monitoring it until the given `CancellationToken` is canceled, or process shuts down.

# Features of process monitoring
The `ProcessMonitor` class has support for graceful target process shutdown and restart.
Both of shutdown and restart functionalities are implemented using semaphores.
The target process must have support for these functionalities as well, as only the names of the semaphores are given to the process.

## Graceful shutdown
In order to support graceful shutdown, the target process __must__ accept command line parameter, which will hold the name of the global semaphore that signals the shutdown.
This semaphore will be released by the monitoring process (the one that starts target process by using `ProcessMonitor` class), and target process __must not__ release it.
Instead, the target process should check periodically whether this semaphore has been released.
If the process detects that semaphore has been released, it should then shut down itself.

## Graceful restart
In order to support graceful restart, the target process __must__ accept command line parameter, which will hold the name of the global semaphore that signals the restart.
This semaphore will be monitored by the monitoring process (the one that starts target process by using `ProcessMonitor` class), and the target process __must__ release it in situations when it detects that it should be restarted.
One such situation is when .NET Core process uses dynamic assembly loading, and it detects a change in assembly which was dynamically loaded.
After the target process releases the semaphore, it should perform shutdown.
Then, the monitoring process will start a new process.

# Configuration
The `MonitoringConfiguration` interface (implemented by `DefaultMonitoringConfiguration` class) has static configuration for the `ProcessMonitor` class.
The properties are explained below.
* The `ToolPath` is optional parameter, and should specify the executable to run instead of the directly running the deployed entrypoint assembly. In case of .NET Core executable assembly, this should be a path to a `dotnet` tool.
* The `ProcessArgumentPrefix` is optional parameter, that should contain string that process parameters will be prefixed with. By default, the value of this is `/`.
* The `ShutdownSemaphoreProcessArgument` is optional parameter, that should be the name of the process parameter that accepts shutdown semaphore name. This semaphore will be used to implement graceful process shutdown. By default, this parameter will not be used. Read more about shutdown semaphores in [UtilPack.ProcessMonitor project documentation](../UtilPack.ProcessMonitor).
* The `ShutdownSemaphoreWaitTime is optional parameter containing the `TimeSpan` for how long to wait for process graceful shutdown. This parameter will only be used if `ShutdownSemaphoreProcessArgument` is specified. The default value is one second.
* The `RestartSemaphoreProcessArgument` is optional parameter, that should be the name of the process parameter that accepts restart semaphore name. This semaphore will be used to implemented graceful process restart. By default, this parameter will not be used. Read more about shutdown semaphores in [UtilPack.ProcessMonitor project documentation](../UtilPack.ProcessMonitor).
* The `RestartWaitTime` is optional parameter containing the `TimeSpan` for how long to wait before restarting the process in the graceful restart situation. This parameter will only be used if `RestartSemaphoreProcessArgument` is specified. The default value is zero seconds, that is, no wait at all.

# Distribution
See [NuGet package](http://www.nuget.org/packages/UtilPack.ProcessMonitor) for binary distribution.
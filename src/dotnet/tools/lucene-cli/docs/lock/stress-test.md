# stress-test

### Name

`lock-stress-test` - Simple tool that forever acquires and releases a lock using a specific `LockFactory`.

### Synopsis

```console
lucene lock stress-test <ID> <VERIFIER_HOST> <VERIFIER_PORT> <LOCK_FACTORY_TYPE> <LOCK_DIRECTORY> <SLEEP_TIME_MS> <TRIES> [?|-h|--help]
```

### Description

You should run multiple instances of this process, each with its own unique ID, and each pointing to the same lock directory, to verify that locking is working correctly. Make sure you are first running [verify-server](verify-server.md).

### Arguments

`ID`

An integer from 0 - 255 (should be unique for test process).

`VERIFIER_HOST`

Hostname or IP address that [verify-server](verify-server.md) is listening on.

`VERIFIER_PORT`

Port that [verify-server](verify-server.md) is listening on.

`LOCK_FACTORY_TYPE`

The primary LockFactory implementation that we will use.

`LOCK_DIRECTORY`

The path to the lock directory (only utilized if `LOCK_FACTORY_TYPE` is set to `SimpleFSLockFactory` or `NativeFSLockFactory`).

`SLEEP_TIME_MS`

Milliseconds to pause between each lock obtain/release.

`TRIES`

Number of locking tries.

### Options

`?|-h|--help`

Prints out a short help for the command.

### Example

Run the client (stress test), connecting to the server on IP address `127.0.0.4` and port `54464` using the ID 3, the `NativeFSLockFactory`, specifying the lock directory as `F:\temp`, sleep for 50 milliseconds, and try to obtain a lock up to 10 times:

```console
lucene lock stress-test 3 127.0.0.4 54464 NativeFSLockFactory F:\temp 50 10
```

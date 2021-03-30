# verify-server

### Name

`lock-verify-server` - Server that must be running when you use VerifyingLockFactory (or [stress-test](stress-test.md)).

### Synopsis

```console
lucene lock verify-server <IP_HOSTNAME> <MAX_CLIENTS> [?|-h|--help]
```

### Description

This server simply verifies that at most one process holds the lock at a time.

### Arguments

`IP_HOSTNAME`

Hostname or IP address that [verify-server](verify-server.md) will listen on.

`MAX_CLIENTS`

The maximum number of threads that are observing the lock from within the verify-server process. When using [stress-test](stress-test.md), each thread will be used by a single connected client and the server won't start running until this number of clients is reached.

### Options

`?|-h|--help`

Prints out a short help for the command.

### Example

Run the server on IP `127.0.0.4` with a 10 connected clients:

```console
lucene lock verify-server 127.0.0.4 10
```

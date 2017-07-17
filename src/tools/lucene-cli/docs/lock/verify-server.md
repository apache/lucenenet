# verify-server

### Name

`lock-verify-server` - Server that must be running when you use VerifyingLockFactory (or [stress-test](stress-test.md)).

### Synopsis

<code>dotnet lucene-cli.dll lock verify-server <IP_HOSTNAME> <MAX_CLIENTS> [?|-h|--help]</code>

### Description

This server simply verifies that at most one process holds the lock at a time.

### Arguments

`IP_HOSTNAME`

Hostname or IP address that [verify-server](verify-server.md) will listen on.

`MAX_CLIENTS`

The maximum number of connected clients.

### Options

`?|-h|--help`

Prints out a short help for the command.

### Example

Run the server on IP `127.0.0.4` with a maximum of 100 connected clients allowed:

<code>dotnet lucene-cli.dll lock verify-server 127.0.0.4 100</code>

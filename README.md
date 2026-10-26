GStage is a pet project to emulate the "Global Stage" IRC server made by QuarterDeck (a company purchased by Symantec). It is actually geared specifically NOT to be RFC-compliant, as the original server existed.

It will also feature a few notable exploits, including a buffer overflow problem dealing with extra-long lines that prompted an entirely new version of mIRC.

It's being written as a hobby and out of nostalgia, as this was the server run by the SciFi (now Syfy) channel at irc.scifi.com for a number of years before ultimately being shut down due to hemorrhaging users (see the "history" section on http://www.malkier.net/ for the long drama leading up to it). As it's just a hobby project, it should probably never be used in a production setting, but if the SciFi channel can get away with it, hey, maybe you can too.

--------------------------------------------------------------------------------
Windows 11 Native Edition
--------------------------------------------------------------------------------

This project has been modernized into a clean, high-performance, asynchronous C# codebase designed to run natively on Windows 11 without requiring Perl, CPAN, or external runtimes.

Key Features & Preserved Global Stage Quirks:
  - 100% Native Windows 11: Standalone executable (bin\GStage.exe) that runs immediately.
  - Asynchronous Socket Engine: Built on async/await and non-blocking I/O.
  - The Famous '-k' Op Exploit: Any user who knows the channel key can run "MODE <channel> -k <key>" to be granted channel operator (+o) status!
  - Nickname Change Lock: Registered users are blocked from changing nicknames (432 ERR_ERRONEOUSNICKNAME). Only supervisors can change nick!
  - Supervisor Immunity: If someone tries to de-op (-o) a supervisor, the server immediately re-ops them automatically.
  - 180-second Bozo Bans: Kicking a user automatically adds their IP to the channel's 180s Bozo ban list (+b).
  - 4-Tier Reversed-Octet Hostmasking: Client IPv4 octets are reversed and hashed against authentic wordlists from etc/hostmasks.1-4 into <m0>.<m1>.<m2>.<m3>.this.chat.server.
  - Extended ASCII Spoof Table: Channel matching uses the QuarterDeck 256-byte spoof lookup table, faithfully recreating the original memory-leak collision quirks.
  - SciFi Channel Dominion MOTD: Features the authentic ASCII art banner welcoming users to #dominion.

Quick Start:
  1. Simply run or double-click:
       bin\GStage.exe
  2. Connect your favorite Windows IRC client (e.g. mIRC) to:
       Server: 127.0.0.1 (or localhost)
       Port:   6667
  3. Join #dominion and chat!

Command Line Options:
  bin\GStage.exe [options]
    -p, --port <port>       Port to listen on (default: 6667)
    -b, --bind <ip>         IP address to bind to (default: 0.0.0.0)
    -n, --name <name>       Server name (default: GStage)
        --net <network>     Network name (default: local)
        --pass <password>   Supervisor password (default: god)
        --motd <file>       Custom MOTD file path
    -h, --help              Show help message

Building from Source:
  - Using Windows built-in C# compiler (zero external dependencies needed):
      .\build.bat
      or
      powershell -ExecutionPolicy Bypass -File .\build.ps1
  - Or using the modern .NET SDK:
      dotnet build GStage.csproj -c Release -o bin

Testing:
  - In-Memory Unit Test Suite:
      .\bin\TestRunner.exe

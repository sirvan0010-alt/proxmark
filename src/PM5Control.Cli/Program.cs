// PM5 Control Center — Ubuntu/Linux CLI
// Purpose: thin cross-platform command surface over PM5Control.Core.
// Hardware note: this initial shell performs no device I/O. Real PM5
// transport and read-only inspection are added only after hardware
// protocol behavior is verified.

using System;

namespace PM5Control.Cli;

internal static class Program
{
    private const string Version = "0.1.0-dev";

    private static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            PrintHelp();
            return 0;
        }

        return args[0] switch
        {
            "version" or "--version" => PrintVersion(),
            "device" => HandleDevice(args[1..]),
            "inspect" => NotReady("inspect"),
            _ => UnknownCommand(args[0])
        };
    }

    private static int HandleDevice(string[] args)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            Console.WriteLine("pm5ctl device info    Read-only device information (hardware implementation pending)");
            return 0;
        }

        return args[0] switch
        {
            "info" => NotReady("device info"),
            _ => UnknownCommand($"device {args[0]}")
        };
    }

    private static int NotReady(string command)
    {
        Console.Error.WriteLine($"Command '{command}' is defined, but physical PM5 transport is not enabled yet.");
        Console.Error.WriteLine("No hardware I/O was attempted. Connect a real PM5 only after the read-only transport is verified.");
        return 2;
    }

    private static int PrintVersion()
    {
        Console.WriteLine($"PM5 Control Center CLI {Version}");
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("PM5 Control Center CLI (pm5ctl)");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  pm5ctl help");
        Console.WriteLine("  pm5ctl version");
        Console.WriteLine("  pm5ctl device info");
        Console.WriteLine("  pm5ctl inspect");
        Console.WriteLine();
        Console.WriteLine("The CLI is intentionally transport-neutral. Device I/O will be implemented through PM5Control.Core.");
    }
}

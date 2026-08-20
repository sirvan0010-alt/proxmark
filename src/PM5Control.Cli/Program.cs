// PM5 Control Center — Ubuntu/Linux CLI
// Purpose: thin cross-platform command surface over PM5Control.Core.
// This CLI must remain transport-neutral: hardware I/O belongs in Core.
// See docs/DIAGNOSTIC_SCHEMA.md, docs/DIAGNOSTIC_RUNTIME_SCHEMA.json and
// docs/AI_PROGRESS_WORKFLOW.md before extending this file.

using System;
using System.IO;

namespace PM5Control.Cli;

internal static class Program
{
    private const string Version = "0.1.0-dev";
    private const string RuntimeSchemaPath = "docs/DIAGNOSTIC_RUNTIME_SCHEMA.json";

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
            "export-schema" => ExportSchema(args[1..]),
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

    private static int ExportSchema(string[] args)
    {
        var path = args.Length switch
        {
            0 => RuntimeSchemaPath,
            1 => args[0],
            _ => null
        };

        if (path is null)
        {
            Console.Error.WriteLine("Usage: pm5ctl export-schema [output-path]");
            return 1;
        }

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Runtime diagnostic schema not found: {path}");
            Console.Error.WriteLine("Run this command from the repository root, or provide the schema path explicitly.");
            return 2;
        }

        Console.Write(File.ReadAllText(path));
        return 0;
    }

    private static int NotReady(string command)
    {
        Console.Error.WriteLine($"Command '{command}' is defined, but physical PM5 transport is not enabled yet.");
        Console.Error.WriteLine("No hardware I/O was attempted. PM5-specific transport must be verified before read-only inspection is enabled.");
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
        Console.WriteLine("  pm5ctl export-schema [output-path]");
        Console.WriteLine("  pm5ctl device info");
        Console.WriteLine("  pm5ctl inspect");
        Console.WriteLine();
        Console.WriteLine("The CLI is intentionally transport-neutral. Device I/O will be implemented through PM5Control.Core.");
    }
}

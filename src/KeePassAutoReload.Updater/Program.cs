using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace KeePassAutoReload.Updater
{
    internal static class Program
    {
        internal const int ExitSuccess = 0;
        internal const int ExitInvalidArguments = 1;
        internal const int ExitUpdateFailed = 2;
        internal const int ExitRestartFailed = 3;

        internal static int Main(string[] args)
        {
            int processId = 0;
            string source = null;
            string destination = null;
            string restart = null;

            for (int i = 0; i < args.Length; i++)
            {
                string current = args[i];
                if (i + 1 >= args.Length) continue;
                string value = args[++i];

                if (string.Equals(current, "--process-id", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(value, out processId) || processId < 0)
                    {
                        Console.Error.WriteLine("Invalid process ID.");
                        return ExitInvalidArguments;
                    }
                }
                else if (string.Equals(current, "--source", StringComparison.OrdinalIgnoreCase))
                {
                    source = value;
                }
                else if (string.Equals(current, "--destination", StringComparison.OrdinalIgnoreCase))
                {
                    destination = value;
                }
                else if (string.Equals(current, "--restart", StringComparison.OrdinalIgnoreCase))
                {
                    restart = value;
                }
            }

            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
            {
                Console.Error.WriteLine("Usage: KeePassAutoReload.Updater --source <path> --destination <path> [--process-id <pid>] [--restart <path>]");
                return ExitInvalidArguments;
            }

            if (!source.EndsWith(".new", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Source file must have a .new extension.");
                return ExitInvalidArguments;
            }

            if (!destination.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Destination file must have a .dll extension.");
                return ExitInvalidArguments;
            }

            if (!File.Exists(source))
            {
                Console.Error.WriteLine("Source file does not exist: " + source);
                return ExitInvalidArguments;
            }

            try
            {
                if (processId > 0)
                {
                    try
                    {
                        using (Process process = Process.GetProcessById(processId))
                        {
                            process.WaitForExit();
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Process already exited.
                    }
                }

                Thread.Sleep(1000);

                string destinationDirectory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrWhiteSpace(destinationDirectory) && !Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                File.Copy(source, destination, overwrite: true);
                File.Delete(source);

                if (string.IsNullOrWhiteSpace(restart)) return ExitSuccess;

                if (!File.Exists(restart))
                {
                    Console.Error.WriteLine("KeePass executable not found: " + restart);
                    return ExitRestartFailed;
                }

                Process.Start(restart);
                return ExitSuccess;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Update failed: " + ex.Message);
                return ExitUpdateFailed;
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace KeePassAutoReload.Updater
{
    internal static class Program
    {
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
                    int.TryParse(value, out processId);
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
                return 1;
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

                File.Copy(source, destination, overwrite: true);
                File.Delete(source);

                if (!string.IsNullOrWhiteSpace(restart) && File.Exists(restart))
                {
                    Process.Start(restart);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Update failed: " + ex.Message);
                return 2;
            }
        }
    }
}

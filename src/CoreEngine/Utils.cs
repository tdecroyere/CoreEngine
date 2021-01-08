using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreEngine
{
    public static class Utils
    {
        public static ulong AlignValue(ulong value, ulong alignment)
        {
            return (value + (alignment - (value % alignment)) % alignment);
        }

        public static float BytesToMegaBytes(ulong value)
        {
            return (float)value / 1024.0f / 1024.0f;
        }

        public static ulong MegaBytesToBytes(ulong value)
        {
            return value * 1024 * 1024;
        }

        public static ulong GigaBytesToBytes(ulong value)
        {
            return value * 1024 * 1024 * 1024;
        }

        public static string[] GetCommandLineArguments()
        {
            var commandLine = Environment.CommandLine;

            var startIndex = commandLine.IndexOf("\"", StringComparison.InvariantCulture);
            var lastIndex = commandLine.LastIndexOf('\"');

            if (startIndex != -1 && lastIndex != -1)
            {
                commandLine = commandLine.Remove(startIndex, 1);
                commandLine = commandLine.Remove(lastIndex - 1);
            }

            var args = new List<string>(commandLine.Split(' '));
            args.RemoveAt(0);

            if (args.Count > 0 && args[0] == "Compiler")
            {
                args.RemoveAt(0);
            }

            return args.ToArray();
        }
    }
}
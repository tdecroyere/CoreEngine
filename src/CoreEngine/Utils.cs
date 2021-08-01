namespace CoreEngine
{
    public static class Utils
    {
        public static ulong AlignValue(ulong value, ulong alignment)
        {
            return value + (alignment - (value % alignment)) % alignment;
        }

        public static float BytesToMegaBytes(ulong value)
        {
            return (float)value / 1024.0f / 1024.0f;
        }

        public static ulong KiloBytesToBytes(ulong value)
        {
            return value * 1024;
        }

        public static ulong MegaBytesToBytes(ulong value)
        {
            return value * 1024 * 1024;
        }

        public static ulong GigaBytesToBytes(ulong value)
        {
            return value * 1024 * 1024 * 1024;
        }

        public static string FormatBigNumber(ulong number)
        {
            if (number == 0)
            {
                return "0";
            }

            int mag = (int)(Math.Floor(Math.Log10(number)) / 3); // Truncates to 6, divides to 2
            double divisor = Math.Pow(10, mag * 3);

            double shortNumber = number / divisor;

            string suffix = string.Empty;
            switch (mag)
            {
                case 0:
                    suffix = string.Empty;
                    break;
                case 1:
                    suffix = "k";
                    break;
                case 2:
                    suffix = "M";
                    break;
                case 3:
                    suffix = "G";
                    break;
            }
            return shortNumber.ToString("N1", CultureInfo.InvariantCulture) + " " + suffix;
        }

        public static string FormatDurationInMs(double duration)
        {
            return $"{duration.ToString("0.00", CultureInfo.InvariantCulture)} ms";
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


            var index = args.IndexOf("--vulkan");
            
            if (index != -1)
            {
                args.RemoveAt(index);
            }

            return args.ToArray();
        }
    }
}
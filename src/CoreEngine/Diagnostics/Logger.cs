using System;
using System.Diagnostics;

namespace CoreEngine.Diagnostics
{
    public static class Logger
    {
        public static void WriteMessage(string message, LogMessageTypes messageType = LogMessageTypes.Normal)
        {
            if ((messageType & LogMessageTypes.Success) != 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }

            else if ((messageType & LogMessageTypes.Action) != 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }

            else if ((messageType & LogMessageTypes.Error) != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            else if ((messageType & LogMessageTypes.Important) != 0)
            {
                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.WriteLine(message);
            Debug.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void WriteLine()
        {
            Console.WriteLine();
        }
    }
}
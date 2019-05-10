using System;
using System.Diagnostics;

namespace CoreEngine.Diagnostics
{
    public static class Logger
    {
        public static void WriteMessage(string message, LogMessageType messageType = LogMessageType.Normal)
        {
            if ((messageType & LogMessageType.Success) != 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }

            else if ((messageType & LogMessageType.Action) != 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }

            else if ((messageType & LogMessageType.Error) != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            else if ((messageType & LogMessageType.Important) != 0)
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
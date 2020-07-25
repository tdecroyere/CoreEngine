using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Globalization;

namespace CoreEngine.Diagnostics
{
    public static class Logger
    {
        // TODO: This code is not thread-safe!
        private static Stack<string> messageStack = new Stack<string>();
        private static Stopwatch globalStopwatch = new Stopwatch();
        private static Stack<long> elapsedTimeStack = new Stack<long>();
        private static int currentLevel;

        public static void WriteMessage(string message, LogMessageTypes messageType = LogMessageTypes.Normal)
        {
            if ((messageType & LogMessageTypes.Success) != 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }

            else if ((messageType & LogMessageTypes.Action) != 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }

            else if ((messageType & LogMessageTypes.Warning) != 0)
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

            for (var i = 0; i < currentLevel; i++)
            {
                Console.Write(" ");
            }

            if (messageType != LogMessageTypes.Normal && messageType != LogMessageTypes.Action && messageType != LogMessageTypes.Success)
            {
                message = $"{messageType.ToString()}: " + message;
            }

            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void WriteLine()
        {
            Console.WriteLine();
        }

        public static void BeginAction(string message)
        {
            if (!globalStopwatch.IsRunning)
            {
                globalStopwatch.Start();
            }

            elapsedTimeStack.Push(globalStopwatch.ElapsedTicks);

            var absoluteTime = (double)(globalStopwatch.ElapsedTicks) / (double)Stopwatch.Frequency * 1000.0;

            messageStack.Push(message);

            globalStopwatch.Stop();
            WriteMessage($"{message}... ({absoluteTime.ToString("0.00", CultureInfo.InvariantCulture)})", LogMessageTypes.Action);
            globalStopwatch.Start();
            currentLevel++;
        }

        public static void EndAction()
        {
            if (messageStack.TryPop(out var message))
            {
                currentLevel--;
                var startTime = elapsedTimeStack.Pop();
                var endTime = globalStopwatch.ElapsedTicks;

                var duration = (double)(endTime - startTime) / (double)Stopwatch.Frequency * 1000.0;
                var absoluteTime = (double)(globalStopwatch.ElapsedTicks) / (double)Stopwatch.Frequency * 1000.0;

                globalStopwatch.Stop();
                WriteMessage($"{message} done. (Elapsed: {duration.ToString("0.00", CultureInfo.InvariantCulture)} ms, {absoluteTime.ToString("0.00", CultureInfo.InvariantCulture)})", LogMessageTypes.Success);
                globalStopwatch.Start();
            }
        }

        public static void EndActionError()
        {
            if (messageStack.TryPop(out var message))
            {
                currentLevel--;
                elapsedTimeStack.Pop();
                WriteMessage($"{message} failed.", LogMessageTypes.Error);
            }
        }

        public static void EndActionWarning(string message)
        {
            if (messageStack.TryPop(out var stackMessage))
            {
                currentLevel--;
                elapsedTimeStack.Pop();

                WriteMessage($"{message}.", LogMessageTypes.Warning);
            }
        }
    }
}
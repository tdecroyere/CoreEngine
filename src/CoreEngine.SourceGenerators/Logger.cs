using System.IO;
using System.Text;

namespace CoreEngine.SourceGenerators
{
    public static class Logger
    {
        private const string path = "c:\\temp\\debug.txt";

        static Logger()
        {
            File.WriteAllText(path, string.Empty);
        }

        public static void WriteMessage(string message)
        {
            File.AppendAllText(path, $"{message}\n");
        }
    }
}
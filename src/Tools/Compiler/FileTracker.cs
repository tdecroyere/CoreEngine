using System;
using System.Collections.Generic;
using System.IO;

namespace CoreEngine.Tools.Compiler
{
    public class FileTracker
    {
        private Dictionary<string, long> fileTracker;
        private Dictionary<string, string[]> destinationFiles;

        public FileTracker()
        {
            this.fileTracker = new Dictionary<string, long>();
            this.destinationFiles = new Dictionary<string, string[]>();
        }

        public bool HasFileChanged(string path)
        {
            var lastWriteTime = File.GetLastWriteTime(path).ToBinary();

            if (this.fileTracker.ContainsKey(path) && lastWriteTime <= (this.fileTracker[path]))
            {
                return false;
            }

            if (!this.fileTracker.ContainsKey(path))
            {
                this.fileTracker.Add(path, lastWriteTime);
            }

            else
            {
                this.fileTracker[path] = lastWriteTime;
            }

            return true;
        }

        public void AddDestinationFiles(string path, string[] destinationFiles)
        {
            if (this.destinationFiles.ContainsKey(path))
            {
                this.destinationFiles.Remove(path);
            }

            this.destinationFiles.Add(path, destinationFiles);
        }

        public string[] GetDestinationFiles(string path)
        {
            if (this.destinationFiles.ContainsKey(path))
            {
                return this.destinationFiles[path];
            }

            return Array.Empty<string>();
        }

        public void ReadFile(string path)
        {
            if (File.Exists(path))
            {
                this.fileTracker.Clear();

                using var stream = new FileStream(path, FileMode.Open);
                using var reader = new BinaryReader(stream);

                var count = reader.ReadInt32();

                for (var i = 0; i < count; i++)
                {
                    var key = reader.ReadString();
                    var value = reader.ReadInt64();

                    if (File.Exists(key))
                    {
                        this.fileTracker.Add(key, value);
                    }
                }

                count = reader.ReadInt32();

                for (var i = 0; i < count; i++)
                {
                    var key = reader.ReadString();

                    var valueCount = reader.ReadInt32();
                    var values = new string[valueCount];

                    for (var j = 0; j < valueCount; j++)
                    {
                        values[j] = reader.ReadString();
                    }

                    this.destinationFiles.Add(key, values);
                }
            }
        }

        public void WriteFile(string path)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);
            
            writer.Write(this.fileTracker.Count);

            foreach (var item in this.fileTracker)
            {
                writer.Write(item.Key);
                writer.Write(item.Value);
            }

            writer.Write(this.destinationFiles.Count);

            foreach (var item in this.destinationFiles)
            {
                writer.Write(item.Key);
                writer.Write(item.Value.Length);

                foreach (var value in item.Value)
                {
                    writer.Write(value);
                }
            }

            writer.Flush();
        }
    }
}
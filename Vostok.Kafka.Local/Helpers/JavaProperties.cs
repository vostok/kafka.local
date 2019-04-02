using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Vostok.Kafka.Local.Helpers
{
    internal class JavaProperties
    {
        private readonly Dictionary<string, string> properties = new Dictionary<string, string>();

        public void SetProperty(string key, string value)
        {
            properties[key] = value;
        }

        public void Save(string fileName)
        {
            File.WriteAllText(fileName, BuildFile());
        }
        
        private string BuildFile()
        {
            return string.Join("\n", properties.Select(kv => $"{kv.Key}={EscapeValue(kv.Value)}"));
        }

        private string EscapeValue(string value)
        {
            return value.Replace("\\", "\\\\");
        }
    }
}
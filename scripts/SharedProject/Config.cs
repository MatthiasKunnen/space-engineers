using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript {
    class Config {
        IDictionary<string, string> dictionary;

        public Config(string configData) {
            this.dictionary = configData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('='))
                .ToDictionary(split => split[0].ToLower(), split => split[1]);
        }

        public T? Get<T>(string key, T? defaultValue = null) where T : struct {
            string value;
            return dictionary.TryGetValue(key.ToLower(), out value)
                ? (T)Convert.ChangeType(value, typeof(T))
                : defaultValue;
        }

        public string Get(string key, string defaultValue = null) {
            string value;
            return dictionary.TryGetValue(key.ToLower(), out value) ? value : defaultValue;
        }

        // Set if key exists
        public void Set<T>(ref T var, string key) where T : struct {
            var = this.Get<T>(key) ?? var;
        }

        public void Set(ref string var, string key) {
            var = this.Get(key) ?? var;
        }
    }
}

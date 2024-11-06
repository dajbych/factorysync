using IniParser;
using IniParser.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Dajbych.FactorySync.Core {

    public class Configuration {

        const string syncDirKey = "dir";

        readonly string config;

        public Configuration(string config) {
            this.config = config;
        }

        public bool Read(string name, out string dir) {
            dir = null;
            if (!File.Exists(config)) return false;
            var parser = new FileIniDataParser();
            var data = parser.ReadFile(config);
            if (!data.Sections.Contains(name)) return false;
            if (!data.Sections[name].ContainsKey(syncDirKey)) return false;
            dir = data.Sections[name][syncDirKey];
            return true;
        }

        public void Add(string name, string dir) {
            var configDir = Path.GetDirectoryName(config);
            if (configDir == null) return;
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
            var parser = new FileIniDataParser();
            var data = File.Exists(config) ? parser.ReadFile(config) : new IniData();
            if (!data.Sections.Contains(name)) data.Sections.Add(name);
            var section = data.Sections[name];
            if (!section.ContainsKey(syncDirKey)) section.AddKey(syncDirKey);
            section[syncDirKey] = dir;
            parser.WriteFile(config, data);
        }

        public bool Remove(string name) {
            var parser = new FileIniDataParser();
            var data = parser.ReadFile(config);
            if (!data.Sections.Contains(name)) return false;
            data.Sections.RemoveSection(name);
            parser.WriteFile(config, data);
            return true;
        }

        public IReadOnlyList<(string name, string dir)> Files {
            get {
                if (File.Exists(config)) {
                    var parser = new FileIniDataParser();
                    var data = parser.ReadFile(config);
                    return data.Sections.Where(s => s.Keys.ContainsKey(syncDirKey)).Select(s => (s.SectionName, s.Keys[syncDirKey])).ToList();
                } else {
                    return new List<(string name, string dir)>();
                }
            }
        }

    }
}
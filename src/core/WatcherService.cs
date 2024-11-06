using System;
using System.Collections.Generic;
using System.IO;

namespace Dajbych.FactorySync.Core {

    public class WatcherService : IDisposable {

        public event EventHandler<string> ChangeDetected = delegate { };

        readonly string savesDir;
        readonly Configuration config;
        readonly List<FileSystemWatcher> watcherList = new List<FileSystemWatcher>();

        public WatcherService(string savesDir, Configuration config) {
            this.savesDir = savesDir;
            this.config = config;
            Init();
        }

        public void Restart() {
            Dispose();
            Init();
        }

        private void Init() {
            foreach (var (name, dir) in config.Files) {
                var filename = name + ".zip";
                Create(filename, savesDir);
                Create(filename, dir);
            }
        }

        private void Create(string filename, string dir) {
            var watcher = new FileSystemWatcher(dir, filename);
            watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite;
            watcher.EnableRaisingEvents = true;
            watcher.Changed += OnFileChanged;
            watcherList.Add(watcher);
        }

        public void Dispose() {
            foreach (var watcher in watcherList) {
                watcher.Changed -= OnFileChanged;
                watcher.Dispose();
            }
            watcherList.Clear();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e) {
            ChangeDetected.Invoke(this, e.FullPath);
        }

    }
}
﻿using ChinhDo.Transactions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Transactions;

namespace Dajbych.FactorySync.Core {

    public sealed class SynchronizationService {

        readonly string savesDir;
        readonly Configuration config;
        readonly HashSet<string> changedFiles = new HashSet<string>();
        readonly object sync = new object();

        public SynchronizationService(string savesDir, Configuration config) {
            this.savesDir = savesDir;
            this.config = config;
        }

        public void Enqueue(string file) {
            lock (sync) {
                changedFiles.Add(file);
            }
        }

        public void Remove(string file) {
            lock (sync) {
                changedFiles.Remove(file);
            }
        }

        public void ProcessAllFiles() {
            foreach (var (name, dir) in config.Files) {
                var filename = name + ".zip";
                var localFile = Path.Combine(savesDir, filename);
                var cloudFile = Path.Combine(dir, filename);
                ProccessSingleFile(localFile);
                ProccessSingleFile(cloudFile);
            }
        }

        public void ProcessEnquedFiles() {
            do {
                string filepath = null;
                lock (sync) {
                    filepath = changedFiles.FirstOrDefault();
                }
                if (filepath != null) {
                    ProccessSingleFile(filepath);
                }
            } while (changedFiles.Count > 0);
        }

        private void ProccessSingleFile(string sourceFile) {
            try {

                // the file changed event may be raised during writing to a file
                // this method tries to obtain an exclusive lock to a file, ensuring that writing is done
                WaitToObtainExclusiveLock(sourceFile);

            } finally {

                // filepath is removed from the queue in the moment it obtains an access to the file,
                // or after an unsuccessful trial
                lock (sync) {
                    changedFiles.Remove(sourceFile);
                }

            }

            // obtain a directory where the save is synchronized to
            var name = Path.GetFileNameWithoutExtension(sourceFile);
            if (!config.Read(name, out var syncDir)) return;

            // determining if the file was changed in the synchronized directory or the directory with game saves
            string targetFile;
            if (sourceFile.StartsWith(savesDir, StringComparison.Ordinal)) {
                targetFile = Path.Combine(syncDir, Path.GetFileName(sourceFile));
            } else if (sourceFile.StartsWith(syncDir, StringComparison.Ordinal)) {
                targetFile = Path.Combine(savesDir, Path.GetFileName(sourceFile));
            } else {
                throw new Exception("File is in an unexpected directory");
            }

            // when the target file does not exists yet, just copy it
            if (!File.Exists(targetFile)) {
                File.Copy(sourceFile, targetFile, false);
                return;
            }

            // parse the game save and read the game time
            // this step may trigger downloading the file content from the cloud storage
            var sourceGameSave = FactorioSaveGame.Open(sourceFile);
            var targetGameSave = FactorioSaveGame.Open(targetFile);

            // when target exists, replace it if AND ONLY IF the source game is newer
            if (sourceGameSave.GameTime > targetGameSave.GameTime) {              

                // make a backup of the game when the save of the older game version is going to be overwritten
                if (targetGameSave.Version.IsOlderButRevision(sourceGameSave.Version)) {
                    var backupFile = Path.Combine(syncDir, $"{name}[backup_{targetGameSave.Version.ToString(3)}].zip");
                    if (!File.Exists(backupFile)) {
                        Copy(targetFile, backupFile, false);
                    }
                }

                // overwriting the old game file ⚠
                Copy(sourceFile, targetFile, true);
                
            }

        }

        private static void WaitToObtainExclusiveLock(string sourceFile, int attempts = 10, int waitTimeBetweenAttemptsInMilliseconds = 1000) {
            // there is no other way than try & error
            for (var attempt = 1; true; attempt++) {
                try {
                    using (var stream = File.Open(sourceFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) {
                        return;
                    }
                } catch (FileNotFoundException) {
                    throw;
                } catch (DirectoryNotFoundException) {
                    throw;
                } catch (PathTooLongException) {
                    throw;
                } catch (IOException) when (Thread.CurrentThread.IsBackground) {
                    // more specific exception types are catched earlier
                    if (attempt < attempts) {
                        // next attempt
                        Thread.Sleep(waitTimeBetweenAttemptsInMilliseconds);
                    } else {
                        throw;
                    }
                } catch {
                    throw;
                }
            }
        } 

        /// <seealso cref="https://en.wikipedia.org/wiki/Transactional_NTFS"/>
        /// <seealso cref="https://learn.microsoft.com/en-us/windows/win32/fileio/transactional-ntfs-portal"/>
        private static void Copy(string sourceFileName, string destFileName, bool overwrite) {
            // Transactional NTFS APIs may not be available in future versions of Windows,
            // however Environment.OSVersion lies about the actual OS version
            var tx = new TxFileManager();
            using (var scope = new TransactionScope()) {
                tx.Copy(sourceFileName, destFileName, overwrite);
                scope.Complete();
            }
        }

    }
}
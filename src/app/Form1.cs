using Dajbych.FactorySync.Core;
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace Dajbych.FactorySync {

#if NET10_0
// TODO https://github.com/dotnet/winforms/issues/4649
#endif

    internal partial class Form1 : Form {

        const string savesLocation = @"%AppData%\Factorio\saves";
        const string configLocation = @"%AppData%\FactorySync\config.ini";
        const string oneDriveLocation = @"%UserProfile%\OneDrive";

        public readonly static string savesDir = Environment.ExpandEnvironmentVariables(savesLocation);
        public readonly static string configFile = Environment.ExpandEnvironmentVariables(configLocation);
        public readonly static string oneDrive = Environment.ExpandEnvironmentVariables(oneDriveLocation);

        static readonly Configuration config = new Configuration(configFile);
        readonly WatcherService watcher = new WatcherService(savesDir, config);
        readonly SynchronizationService synchronizer = new SynchronizationService(savesDir, config);

        bool allFilesProcessed; // Sync all files only once
        bool allowVisible; // ContextMenu's Show command used
        bool allowClose; // ContextMenu's Exit command used

        public Form1() {
            InitializeComponent();
            notifyIcon.ContextMenuStrip = contextMenu;
            watcher.ChangeDetected += GameFileChangeDetected;
            backgroundWorker.RunWorkerAsync(); // Sync all files when app starts
        }

        private void GameFileChangeDetected(object sender, string file) {
            synchronizer.Enqueue(file);
            if (!backgroundWorker.IsBusy) backgroundWorker.RunWorkerAsync();
        }

        protected override void SetVisibleCore(bool value) {
            if (!allowVisible) {
                value = false;
                if (!IsHandleCreated) CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            if (!allowClose) {
                Hide();
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        private void OnContextMenuShowClick(object sender, EventArgs e) {
            allowVisible = true;
            Show();
        }

        private void OnContextMenuAboutClick(object sender, EventArgs e) {
            System.Diagnostics.Process.Start("https://github.com/dajbych/factorysync");
        }

        private void OnContextMenuExitClick(object sender, EventArgs e) {
            allowClose = true;
            Application.Exit();
        }

        private void OnNotifyIconMouseClick(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                allowVisible = true;
                Show();
            }
        }

        private void OnNotificationClicked(object sender, EventArgs e) {
            allowVisible = true;
            Show();
        }

        private void OnLoad(object sender, EventArgs e) {
            try {
                if (!Directory.Exists(savesDir)) {
                    ShowErrorBar("Cannot find a folder with Factorio game saves.");
                    return;
                }

                var filenames = Directory.GetFileSystemEntries(savesDir);
                listSaves.Items.Clear();
                foreach (var filepath in filenames) {
                    if (Path.GetExtension(filepath) != ".zip") continue;
                    var name = Path.GetFileNameWithoutExtension(filepath);
                    listSaves.Items.Add(name);
                }

                if (panelWarning.BackColor != Color.Firebrick) {
                    ShowDefaultBar();
                }
            } catch (Exception ex) {
                ShowErrorBar(ex.Message);
            }
        }

        private void OnListSelectionChanged(object sender, EventArgs e) {
            if (listSaves.SelectedItem is string name) {
                try {
                    var filepath = Path.Combine(savesDir, name + ".zip");
                    var fileInfo = new FileInfo(filepath);
                    using (var archive = ZipFile.OpenRead(filepath)) {
                        var preview = new FactorioSavePreview(archive);
                        try {
                            var save = FactorioSaveGame.Open(archive);
                            ShowGameSave(save.Version, save.Scenario, save.GameTime.ToGameTime(), fileInfo.FileSize(), preview.Image);
                            ShowSyncStatus(name, true, null);
                        } catch (FactorioParsingException ex) {
                            ShowGameSave(ex.Version, preview: preview.Image);
                            ShowSyncStatus(name, false, ex.Message);
                        } catch (FactorioUnsupportedException ex) {
                            ShowGameSave(preview: preview.Image);
                            ShowSyncStatus(name, false, ex.Message);
                        } catch (Exception ex) {
                            ShowGameSave();
                            ShowSyncStatus(name, false, ex.Message);
                        }
                    }
                } catch (Exception ex) {
                    ShowErrorBar(ex.Message);
                }
            }
        }

        private void ShowGameSave(Version ver = null, string scenario = "-", string gameTime = "-", string fileSize = "-", Bitmap preview = null) {
            label5.Text = ver != null ? ver.ToGameVersion() : "-";
            label8.Text = scenario;
            label6.Text = gameTime;
            label7.Text = fileSize;
            pictureBox.Image = preview;
            pictureBox.Visible = preview != null;
        }

        private void ShowSyncStatus(string name, bool successfullyLoaded, string errorMessage) {
            if (config.Read(name, out var dir)) {
                label11.Text = dir;
                button.Text = "Stop";
                button.Visible = true;
                if (successfullyLoaded) {
                    ShowInfoBar(name, dir);
                } else {
                    ShowErrorBar(errorMessage);
                }
            } else {
                label11.Text = "-";
                if (successfullyLoaded) {
                    button.Text = "Select";
                    button.Visible = true;
                    ShowUnconfiguredGameBar();
                } else {
                    button.Visible = false;
                    ShowErrorBar(errorMessage);
                }
            }
        }

        private void ShowInfoBar(string name, string dir) {
            var filename = name + ".zip";
            var file1 = new FileInfo(Path.Combine(dir, filename));
            var file2 = new FileInfo(Path.Combine(savesDir, filename));
            if (file1.Exists && file2.Exists && file1.Length == file2.Length) {
                labelMessage.Text = "The game is synchronized successfully";
                panelWarning.BackColor = Color.ForestGreen;
            } else if (!Directory.Exists(dir)) {
                labelMessage.Text = "Directory does not exists";
                panelWarning.BackColor = Color.DarkOrange;
            } else {
                labelMessage.Text = "The game is out of sync";
                panelWarning.BackColor = Color.DarkOrange;
            }
        }

        private void ShowUnconfiguredGameBar() {
            labelMessage.Text = "Select the folder you want to sync the game to";
            panelWarning.BackColor = Color.Chocolate;
        }

        private void ShowDefaultBar() {
            labelMessage.Text = "Select the game you want to sync";
            panelWarning.BackColor = Color.Chocolate;
        }

        private void ShowErrorBar(string message) {
            labelMessage.Text = message;
            panelWarning.BackColor = Color.Firebrick;
        }

        private void OnButtonClick(object sender, EventArgs e) {
            try {
                if (listSaves.SelectedItem is string name) {
                    if (button.Text == "Stop") {
                        if (config.Remove(name)) {
                            synchronizer.Remove(name);
                            watcher.Restart();
                            OnListSelectionChanged(sender, e);
                        }
                    } else if (button.Text == "Select") {
                        var dialog = new FolderBrowserDialog();
                        if (Directory.Exists(oneDrive)) dialog.SelectedPath = oneDrive;
                        if (dialog.ShowDialog() == DialogResult.OK) {
                            config.Add(name, dialog.SelectedPath);
                            GameFileChangeDetected(sender, Path.Combine(savesDir, name + ".zip"));
                            watcher.Restart();
                            OnListSelectionChanged(sender, e);
                        }
                    }
                }
            } catch (Exception ex) {
                ShowErrorBar(ex.Message);
            }
        }

        private void OnBackgroundWorkerStarted(object sender, System.ComponentModel.DoWorkEventArgs e) {
            try {
                if (!allFilesProcessed) {
                    synchronizer.ProcessAllFiles();
                    allFilesProcessed = true;
                } else {
                    synchronizer.ProcessEnquedFiles();
                }
            } catch (Exception ex) {
                e.Result = ex;
            }
        }

        private void OnBackgroundWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e) {
            if (e.Result is Exception ex) {
                notifyIcon.BalloonTipText = "Game synchronization failed!";
                notifyIcon.ShowBalloonTip(10 * 1000);
                ShowErrorBar(ex.Message);
            }
            OnListSelectionChanged(sender, e);
        }

    }
}
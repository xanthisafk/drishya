using FFMpegCore;
using LibVLCSharp.Shared;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Drishya.Properties;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Drishya.Views
{
    public partial class NormalSortingView : Page
    {
        #region Fields
        private string _defaultVideoFolder;
        private string _defaultScreenshotFolder;
        private double _screenshotInterval;
        private int _defaultVolume;
        private bool _shouldAutoLoop;
        private bool _isPlaying;
        private bool _isScreenshotting = false;
        private DispatcherTimer _screenshotTimer;

        private List<string> _allVideoPaths;
        private int _currentIndex;

        private LibVLC _libVLC;
        private MediaPlayer _player;
        private DispatcherTimer _timer;
        #endregion

        #region Initialization
        public NormalSortingView()
        {
            InitializeComponent();
            InitializeFields();
            InitializeTimer();
            UpdateComponents();
        }

        private void InitializeFields()
        {
            // Initialize video folder
            _defaultVideoFolder = Settings.Default.DefaultVideoFolder;
            if (string.IsNullOrEmpty(_defaultVideoFolder))
            {
                _defaultVideoFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                Settings.Default.DefaultVideoFolder = _defaultVideoFolder;
                Settings.Default.Save();
            }

            // Initialize screenshot folder
            _defaultScreenshotFolder = Settings.Default.DefaultScreenshotFolder;
            if (string.IsNullOrEmpty(_defaultScreenshotFolder))
            {
                _defaultScreenshotFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                Settings.Default.DefaultScreenshotFolder = _defaultScreenshotFolder;
                Settings.Default.Save();
            }

            // Initialize screenshot interval
            _screenshotInterval = Settings.Default.DefaultScreenshotInterval;
            if (_screenshotInterval == 0)
            {
                _screenshotInterval = 1.0;
                Settings.Default.DefaultScreenshotInterval = 1;
                Settings.Default.Save();
            }

            // Initialize volume
            _defaultVolume = Settings.Default.DefaultVolume;
            if (_defaultVolume == 0)
            {
                _defaultVolume = 75;
                Settings.Default.DefaultVolume = 75;
                Settings.Default.Save();
            }

            _shouldAutoLoop = Settings.Default.ShouldAutoLoop;
            _isPlaying = false;
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void UpdateComponents()
        {
            FolderPathContainerTB.Text = _defaultVideoFolder;
            ScreenshotPathContainerTB.Text = _defaultScreenshotFolder;
            foreach (ComboBoxItem item in ScreenshotIntervalCB.Items)
            {
                if (Convert.ToDouble(item.Tag) == _screenshotInterval)
                {
                    ScreenshotIntervalCB.SelectedItem = item;
                    break;
                }
            }
            VolumeSlider.Value = _defaultVolume;

            Core.Initialize();
            _libVLC = new LibVLC();
            _player = new MediaPlayer(_libVLC);
            _player.EndReached += Player_EndReached;
            VideoPlayerVW.MediaPlayer = _player;
        }

        private void UnloadVideo()
        {
            if (_player?.Media != null)
            {
                _player.Stop();
                _player.Media.Dispose();
                _player.Media = null;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void CleanupMediaResources(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            UnloadVideo();
        }
        #endregion

        #region Video Playback Control
        private void Player_EndReached(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _isPlaying = false;
                PlayButton_Click(PlayButton, new RoutedEventArgs());
            });
            // No matter what I do, looping wont work!
            return;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_player != null && _player.IsPlaying)
            {
                ElapsedTimeLabel.Text = TimeSpan.FromMilliseconds(_player.Time).ToString(@"mm\:ss");
                TotalTimeLabel.Text = TimeSpan.FromMilliseconds(_player.Length).ToString(@"mm\:ss");

                // Only update when not seeking
                if (!SeekVideoSlider.IsMouseCaptureWithin)
                {
                    SeekVideoSlider.Minimum = 0;
                    SeekVideoSlider.Maximum = _player.Length;
                    SeekVideoSlider.Value = _player.Time;
                }
            }
        }

        private void SeekVideoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_player != null && SeekVideoSlider.IsMouseCaptureWithin)
            {
                _player.Time = (long)SeekVideoSlider.Value;
                // Update labels immediately after seeking
                ElapsedTimeLabel.Text = TimeSpan.FromMilliseconds(_player.Time).ToString(@"mm\:ss");
            }
        }

        static string RemoveEmojiFromFilename(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);

            // Regex pattern to match emojis
            string emojiPattern = @"[\p{Cs}\p{So}]";
            string cleanFileName = Regex.Replace(fileName, emojiPattern, "");

            string newFilePath = Path.Combine(directory, cleanFileName);
            if (File.Exists(filePath))
            {
                File.Move(filePath, newFilePath);
            }

            return newFilePath;
        }

        private void ChangeVideo()
        {
            try
            {
                _allVideoPaths[_currentIndex] = RemoveEmojiFromFilename(_allVideoPaths[_currentIndex]);
                string currentVideo = _allVideoPaths[_currentIndex];
                _isPlaying = false;
                _player.Media?.Dispose();
                Media media = new Media(_libVLC, new Uri(currentVideo, UriKind.Absolute));
                _player.Media = media;
                media.Dispose();
                PlayButton_Click(PlayButton, new RoutedEventArgs());
                UpdateMetadata();
            }
            catch (IndexOutOfRangeException)
            {
                MessageBox.Show("No more videos in the current directory.\nRescanning from start...", "End of List");
                _currentIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading video: {ex.Message}", "Playback Error");
                SearchButton_Click(null, null);
            }
        }
        #endregion

        #region Media Controls
        public void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                _player.Pause();
                _isPlaying = false;
                ((Button)sender).Content = "▶";
            }
            else
            {
                _player.Play();
                _isPlaying = true;
                ((Button)sender).Content = "⏸";
            }
        }

        public void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _player.Stop();
            _isPlaying = false;
        }

        public void NextButton_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex++;
            UpdateView();
        }

        public void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            _shouldAutoLoop = !_shouldAutoLoop;
            ((Button)sender).Content = _shouldAutoLoop ? "🔁" : "➡";
            Settings.Default.ShouldAutoLoop = _shouldAutoLoop;
            Settings.Default.Save();
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_player.Volume == 0)
            {
                MuteButton.Content = "🔊";
                _player.Volume = _defaultVolume;
                VolumeSlider.Value = _defaultVolume;
            }
            else
            {
                MuteButton.Content = "🔇";
                _player.Volume = 0;
                VolumeSlider.Value = 0;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int volume = Convert.ToInt32(VolumeSlider.Value);
            _defaultVolume = volume;
            Settings.Default.DefaultVolume = volume;
            Settings.Default.Save();

            if (_player != null)
            {
                _player.Volume = volume;
            }

            if (MuteButton != null)
            {
                MuteButton.Content = volume <= 0 ? "🔊" : "🔇";
            }
        }
        #endregion

        #region File Management
        private void GetAllVideoPaths()
        {
            _allVideoPaths = Directory
                .GetFiles(_defaultVideoFolder)
                .Where(IsVideoFile)
                .ToList();
        }

        static bool IsVideoFile(string filePath)
        {
            string[] videoExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mpeg", ".mpg", ".m4v", ".3gp" };
            string extension = Path.GetExtension(filePath)?.ToLower();
            return videoExtensions.Contains(extension) && !videoExtensions.Contains("_Drishya");
        }

        private async void UpdateMetadata()
        {
            string path = _allVideoPaths[_currentIndex];
            string title = Path.GetFileName(path);

            var mediaInfo = await FFProbe.AnalyseAsync(path);
            MetadataTitleTB.Text = title;
            MetadataLocationTB.Text = path;

            CountLabel.Text = $"{_currentIndex}/{_allVideoPaths.Count}";
        }

        private void UpdateView()
        {
            ChangeVideo();
        }
        #endregion

        #region UI Events
        public void ProcessVideo_Click(object sender, RoutedEventArgs e)
        {
            string newName = ControlNameTB.Text;
            if (string.IsNullOrEmpty(newName) )
            {
                ControlNameTB.Focus();
                MessageBox.Show("New video title can't be empty.", "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Dispose the media and wait for GC to complete
            _player.Media?.Dispose();
            _player.Media = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            string videoFile = _allVideoPaths[_currentIndex];
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string parentFolder = Path.GetDirectoryName(videoFile);
            string extension = Path.GetExtension(videoFile).ToLower();
            string newFileName = Path.Combine(parentFolder, $"{newName}_{timestamp}_Drishya{extension}");
            
            if (File.Exists(videoFile))
            {
                File.Move(videoFile, newFileName);
                _allVideoPaths[_currentIndex] = newFileName;
            }
            _currentIndex++;
            ChangeVideo();
        }



        public void SkipVideo_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex++;
            UpdateView();
        }
        
        public void PreviousVideo_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex--;
            UpdateView();
        }

        public void DeleteVideo_Click(object sender, RoutedEventArgs e)
        {
            string path = _allVideoPaths[_currentIndex];
            UnloadVideo();
            GC.Collect();
            MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete ${path}?", "Delete Video?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    FileSystem.DeleteFile(
                        path,
                        UIOption.AllDialogs,
                        RecycleOption.SendToRecycleBin
                    );
                    StatusLabel.Text = $"Successfully deleted: {path}";
                    _currentIndex++;
                    ChangeVideo();
                }
                catch (Exception ex)
                {
                    {
                        MessageBox.Show(ex.Message, "An error occured!", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusLabel.Text = $"Error deleting: {path}";
                        ChangeVideo();
                    }
                }
            }
        }

        private void VideoFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFolderDialog
            {
                Title = "Select source folder",
                InitialDirectory = _defaultVideoFolder
            };

            if (ofd.ShowDialog() == true)
            {
                _defaultVideoFolder = ofd.FolderName;
                Settings.Default.DefaultVideoFolder = _defaultVideoFolder;
                Settings.Default.Save();
                FolderPathContainerTB.Text = _defaultVideoFolder;
            }
        }

        private void ScreenshotFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFolderDialog
            {
                Title = "Select folder to save screenshots",
                InitialDirectory = _defaultScreenshotFolder
            };

            if (ofd.ShowDialog() == true)
            {
                _defaultScreenshotFolder = ofd.FolderName;
                Settings.Default.DefaultScreenshotFolder = _defaultScreenshotFolder;
                Settings.Default.Save();
                ScreenshotPathContainerTB.Text = _defaultScreenshotFolder;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex = 0;
            GetAllVideoPaths();
            UpdateView();
        }

        private void ScreenshotFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", _defaultScreenshotFolder);
        }

        private void VideoFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", _defaultVideoFolder);
        }
        #endregion

        #region Screenshots
        private void TakeScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (_player == null || !_player.IsPlaying) return;

            if (!Directory.Exists(_defaultScreenshotFolder)) Directory.CreateDirectory(_defaultScreenshotFolder);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string videoFileName = Path.GetFileName(_allVideoPaths[_currentIndex]);
            string filename = Path.Combine(_defaultScreenshotFolder, $"{Path.GetFileNameWithoutExtension(videoFileName)}_screenshot_{timestamp}_Drishya.jpg");
            _player.TakeSnapshot(0, filename, 0, 0);
        }

        private async void TakeMultipleScreenshots_Click(object sender, RoutedEventArgs e)
        {
            if (_player == null) return;

            _isScreenshotting = true;
            if (!Directory.Exists(_defaultScreenshotFolder)) Directory.CreateDirectory(_defaultScreenshotFolder);
                
            string inputPath = _allVideoPaths[_currentIndex];
            string videoFileName = Path.GetFileName(inputPath);
            TimeSpan totalDuration = FFProbe.Analyse(inputPath).Duration;
            int totalScreenshots = (int)(totalDuration.TotalSeconds / _screenshotInterval);
            int successfulScreenshots = 0;
            ScreenshotStatusLabel.Text = $"Taking screenshots every {_screenshotInterval} seconds. {successfulScreenshots}/{totalScreenshots} taken...";
            TakeMultipleScreenshotsButton.IsEnabled = false;
            try
            {
                for (int i = 0; i <= totalScreenshots; i++)
                {
                    TimeSpan currentTime = TimeSpan.FromSeconds(i * _screenshotInterval);

                    if (currentTime > totalDuration) break;

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string outputPath = Path.Combine(
                        _defaultScreenshotFolder,
                        $"{Path.GetFileNameWithoutExtension(videoFileName)}_screenshot_{timestamp}_{i}_Drishya.jpg"
                    );

                    await FFMpeg.SnapshotAsync(
                        inputPath,
                        outputPath,
                        captureTime: currentTime
                    );

                    successfulScreenshots++;
                    ScreenshotStatusLabel.Text = $"Taking screenshots every {_screenshotInterval} seconds. {successfulScreenshots}/{totalScreenshots} taken...";
                }
            }
            catch ( Exception ex ) 
            {
                MessageBox.Show(ex.Message, "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
                Clipboard.SetText( ex.Message );
            }
            finally
            {
                _isScreenshotting = false;
                TakeMultipleScreenshotsButton.IsEnabled = true;
                ScreenshotStatusLabel.Text = $"{successfulScreenshots} screenshots taken.";
            }

        }

        

        private void ScreenshotIntervalCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = ScreenshotIntervalCB.SelectedItem as ComboBoxItem;
            _screenshotInterval = Convert.ToDouble(selected?.Tag);
            Settings.Default.DefaultScreenshotInterval = _screenshotInterval;
            Settings.Default.Save();
        }
        #endregion
    }
}
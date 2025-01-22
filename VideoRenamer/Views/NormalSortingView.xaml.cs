using Drishya.Properties;
using FFMpegCore;
using LibVLCSharp.Shared;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Drishya.Views
{
    public partial class NormalSortingView : Page
    {
        #region Fields
        private string _defaultVideoFolder;
        private string _defaultScreenshotFolder;
        private string _defaultDestinationFolder;
        private double _screenshotInterval;
        private int _defaultVolume;
        private bool _shouldAutoLoop;
        private bool _isPlaying;
        private bool _includeAlreadySorted;
        private bool _shouldMoveOnSort;
        private bool _shouldCreateScreenshotSubfolder;
        private bool _debugMode = false;
        public string SortedVideoPostfix;

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
            
            // Initialize destination folder
            _defaultDestinationFolder = Settings.Default.DefaultDestinationFolder;
            if (string.IsNullOrEmpty(_defaultDestinationFolder))
            {
                _defaultDestinationFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                Settings.Default.DefaultDestinationFolder = _defaultDestinationFolder;
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

            // Initialize postfix
            SortedVideoPostfix = Settings.Default.SortingPostfix;
            if (string.IsNullOrEmpty(SortedVideoPostfix))
            {
                SortedVideoPostfix = "Drishya";
                Settings.Default.SortingPostfix = SortedVideoPostfix;
                Settings.Default.Save();
            }

            _shouldCreateScreenshotSubfolder = Settings.Default.ShouldCreateScreenshotSubfolder;
            _shouldMoveOnSort = Settings.Default.ShouldMoveOnSort;
            _includeAlreadySorted = Settings.Default.IncludeAlreadySorted;
            _shouldAutoLoop = Settings.Default.ShouldAutoLoop;
            _isPlaying = false;
        }

        private void UpdateComponents()
        {
            FolderPathContainerTB.Text = _defaultVideoFolder;
            DestinationPathContainerTB.Text = _defaultDestinationFolder;
            ScreenshotPathContainerTB.Text = _defaultScreenshotFolder;
            PostfixTB.Text = SortedVideoPostfix;
            foreach (ComboBoxItem item in ScreenshotIntervalCB.Items)
            {
                if (Convert.ToDouble(item.Tag) == _screenshotInterval)
                {
                    ScreenshotIntervalCB.SelectedItem = item;
                    break;
                }
            }

            DebugLabel.Text = $"Default volume ${_defaultVolume}";
            VolumeSlider.Value = _defaultVolume;
            IncludeAlreadySortedCheckbox.IsChecked = _includeAlreadySorted;
            MoveSortedVideoCheckbox.IsChecked = _shouldMoveOnSort;
            ShouldScreenshotSubfolderCheckbox.IsChecked = _shouldCreateScreenshotSubfolder;

            Core.Initialize();
            _libVLC = new LibVLC("--input-repeat=9999");    // Loop for 9999 times.
            _player = new MediaPlayer(_libVLC);
            VideoPlayerVW.MediaPlayer = _player;
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(5);
            _timer.Tick += Timer_Tick;
            _timer.Start();
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

        #region File Management
        private void GetAllVideoPaths()
        {
            _allVideoPaths = Directory
                .GetFiles(_defaultVideoFolder)
                .Where(IsVideoFile)
                .ToList();
        }

        bool IsVideoFile(string filePath)
        {
            string[] videoExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mpeg", ".mpg", ".m4v", ".3gp" };
            string extension = Path.GetExtension(filePath)?.ToLower();
            bool includeAlreadySorted = true;
            if (!_includeAlreadySorted)
            {
                includeAlreadySorted = !filePath.Contains($"_{SortedVideoPostfix}");
            }
            return videoExtensions != null
                && videoExtensions.Contains(extension)
                && includeAlreadySorted;
        }

        private async void UpdateMetadata()
        {
            string path = _allVideoPaths[_currentIndex];
            string title = Path.GetFileName(path);

            var mediaInfo = await FFProbe.AnalyseAsync(path);
            MetadataTitleTB.Text = title;
            MetadataLocationLabel.Text = path;

            CountLabel.Text = $"{_currentIndex}/{_allVideoPaths.Count}";
        }

        private void UpdateView() => ChangeVideo();

        #endregion

        #region Video Playback Control

        private void Timer_Tick(object? sender, EventArgs e)
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
            if (_allVideoPaths.Count <= 0)
            {
                MessageBox.Show("No videos found in that folder.", "Search Results", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
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

        private BitmapImage GetImageIconBitmap(string image)
        {
            string template = $"pack://application:,,,/Resources/{image}.png";
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(template);
            img.EndInit();
            return img;
        }

        public void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Image img = new Image();
            if (_isPlaying)
            {
                _player.Pause();
                _isPlaying = false;
                img.Source = GetImageIconBitmap("play");
                ((Button)sender).Content = img;
                ((Button)sender).ToolTip = "Play";
            }
            else
            {
                _player.Play();
                _isPlaying = true;
                img.Source = GetImageIconBitmap("pause");
                ((Button)sender).Content = img;
                ((Button)sender).ToolTip = "Pause";
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
            Image img = new Image();
            img.Source = _shouldAutoLoop
                ? GetImageIconBitmap("repeat_on")
                : GetImageIconBitmap("repeat_off");
            ((Button)sender).Content = img;
            ((Button)sender).ToolTip = _shouldAutoLoop
                ? "Turn repeat off"
                : "Turn repeat on";
            Settings.Default.ShouldAutoLoop = _shouldAutoLoop;
            Settings.Default.Save();
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            Image img = new Image();
            if (_player.Volume == 0)
            {
                img.Source = GetImageIconBitmap("volume_on");
                MuteButton.Content = img;
                _player.Volume = 30;
                VolumeSlider.Value = 30;
            }
            else
            {
                img.Source = GetImageIconBitmap("volume_off");
                MuteButton.Content = img;
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
                Image img = new Image();
                img.Source = volume <= 0
                    ? GetImageIconBitmap("volume_off")
                    : GetImageIconBitmap("volume_on");
                MuteButton.Content = img;
            }
        }

        #endregion

        #region Screenshots

        private string GenerateProperDirectoryForScreenshots(string file)
        {
            if (!Directory.Exists(_defaultScreenshotFolder))
            {
                Directory.CreateDirectory(_defaultScreenshotFolder);
            }

            string destinationFolder = _defaultScreenshotFolder;
            if (_shouldCreateScreenshotSubfolder)
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                destinationFolder = Path.Combine(destinationFolder, filename);

                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }
            }
            return destinationFolder;
        }

        private void TakeScreenshot_Click(object sender, RoutedEventArgs e)
        {
            // Return if player has nothing
            if (_player == null || !_player.IsPlaying) return;
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string videoFileName = Path.GetFileName(_allVideoPaths[_currentIndex]);
            string parentDir = GenerateProperDirectoryForScreenshots(videoFileName);
            string filename = Path.Combine(parentDir, $"{Path.GetFileNameWithoutExtension(videoFileName)}_screenshot_{timestamp}_{SortedVideoPostfix}.jpg");
            _player.TakeSnapshot(0, filename, 0, 0);
        }

        private async void TakeMultipleScreenshots_Click(object sender, RoutedEventArgs e)
        {
            if (_player == null) return;

            string inputPath = _allVideoPaths[_currentIndex];
            string parentDir = GenerateProperDirectoryForScreenshots(inputPath);
            string videoFileName = Path.GetFileName(inputPath);

            var videoInfo = FFProbe.Analyse(inputPath);
            TimeSpan totalDuration = videoInfo.Duration;

            int totalScreenshots;
            double interval;

            string dot = "";

            if (_screenshotInterval == 255.0)
            {
                double frameRate = videoInfo.PrimaryVideoStream.FrameRate;
                totalScreenshots = (int)(totalDuration.TotalSeconds * frameRate);
                interval = 1.0 / frameRate; // Time between frames
            }
            else
            {
                totalScreenshots = (int)(totalDuration.TotalSeconds / _screenshotInterval);
                interval = _screenshotInterval;
            }

            int successfulScreenshots = 0;
            string statusMessage = _screenshotInterval == 255.0
                ? $"Taking screenshots of every frame. {successfulScreenshots}/{totalScreenshots} taken{dot}"
                : $"Taking screenshots every {_screenshotInterval} seconds. {successfulScreenshots}/{totalScreenshots} taken{dot}";

            ScreenshotStatusLabel.Text = statusMessage;
            TakeMultipleScreenshotsButton.IsEnabled = false;

            string originalFileName = Path.GetFileNameWithoutExtension(videoFileName);

            try
            {
                for (int i = 0; i <= totalScreenshots; i++)
                {
                    TimeSpan currentTime = TimeSpan.FromSeconds(i * interval);
                    if (currentTime > totalDuration) break;

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string outputPath = Path.Combine(
                        parentDir,
                        $"{originalFileName}_screenshot_{timestamp}_{i}_{SortedVideoPostfix}.jpg"
                    );

                    await FFMpeg.SnapshotAsync(
                        inputPath,
                        outputPath,
                        captureTime: currentTime
                    );

                    successfulScreenshots++;
                    dot = dot == "..." ? "" : dot + ".";
                    statusMessage = _screenshotInterval == 255.0
                        ? $"Taking screenshots of every frame. {successfulScreenshots}/{totalScreenshots} taken{dot}"
                        : $"Taking screenshots every {_screenshotInterval} seconds. {successfulScreenshots}/{totalScreenshots} taken{dot}";
                    ScreenshotStatusLabel.Text = statusMessage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Error);
                Clipboard.SetText(ex.Message);
            }
            finally
            {
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

        /*
         * UI Events
         */


        public void ProcessVideo_Click(object sender, RoutedEventArgs e)
        {
            UnloadVideo();

            if (!Directory.Exists(_defaultDestinationFolder))
            {
                Directory.CreateDirectory(_defaultDestinationFolder);
            }

            string videoFile = _allVideoPaths[_currentIndex];
            string newName = string.IsNullOrEmpty(ControlNameTB.Text)
                ? Path.GetFileName(videoFile)
                : ControlNameTB.Text;
            if (!string.IsNullOrEmpty(videoFile) && !File.Exists(videoFile))
            {
                MessageBox.Show($"Video file at '{videoFile}' does not exist", "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string parentFolder = Path.GetDirectoryName(videoFile)!;
            string extension = Path.GetExtension(videoFile).ToLower();
            string newFileName = Path.Combine(
                _shouldMoveOnSort ? _defaultDestinationFolder : parentFolder,
                $"{newName}_{timestamp}_{SortedVideoPostfix}{extension}");

            if (File.Exists(videoFile))
            {
                File.Move(videoFile, newFileName);
                _allVideoPaths[_currentIndex] = newFileName;
                StatusLabel.Text = $"Successfully updated {videoFile}";
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

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete ${path}?", "Delete Video?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    UnloadVideo();
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
            UnloadVideo();
            _currentIndex = 0;
            GetAllVideoPaths();
            UpdateView();
        }

        private void OpenInExplorer(string path)
        {
            if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", path);
            }
        }

        private void ScreenshotFolderMenuItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenInExplorer(_defaultScreenshotFolder);
        }

        private void VideoFolderMenuItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenInExplorer(_defaultVideoFolder);
        }
        
        private void DestinationFolderMenuItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenInExplorer(_defaultDestinationFolder);
        }

        private void UpdateIncludeAlreadySortedParameters(bool newvalue)
        {
            _includeAlreadySorted = newvalue;
            Settings.Default.IncludeAlreadySorted = newvalue;
            Settings.Default.Save();
        }

        private void IncludeAlreadySortedCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateIncludeAlreadySortedParameters(true);
        }

        private void IncludeAlreadySortedCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateIncludeAlreadySortedParameters(false);
        }

        private void HighlightInFolder(string filename)
        {
            if (!string.IsNullOrEmpty(filename) && File.Exists(filename))
            {
                Process.Start("explorer.exe", $"/select, \"{filename}\"");
            }
            
        }
       
        private void CurrentVideoLabel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string filename = MetadataLocationLabel.Text;
            HighlightInFolder(filename);
        }

        private void UpdateMoveOnSortParameters(bool newvalue)
        {
            _shouldMoveOnSort = newvalue;
            Settings.Default.ShouldMoveOnSort = _shouldMoveOnSort;
            Settings.Default.Save();
        }

        private void MoveSortedVideoCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateMoveOnSortParameters(true);
        }

        private void MoveSortedVideoCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateMoveOnSortParameters(false);
        }

        private void DestinationFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFolderDialog
            {
                Title = "Select destination folder",
                InitialDirectory = _defaultDestinationFolder
            };

            if (ofd.ShowDialog() == true)
            {
                _defaultDestinationFolder = ofd.FolderName;
                Settings.Default.DefaultDestinationFolder = _defaultDestinationFolder;
                Settings.Default.Save();
                DestinationPathContainerTB.Text = _defaultDestinationFolder;
            }
        }

        private void UpdateSubfolderDestinationParameter(bool newvalue)
        {
            _shouldCreateScreenshotSubfolder = newvalue;
            Settings.Default.ShouldCreateScreenshotSubfolder = _shouldCreateScreenshotSubfolder;
            Settings.Default.Save();
        }

        private void ShouldScreenshotSubfolderCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateSubfolderDestinationParameter(true);
        }

        private void ShouldScreenshotSubfolderCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateSubfolderDestinationParameter(false);
        }

        private void PostfixTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            string newText = PostfixTB.Text;
            if (string.IsNullOrEmpty(newText))
            {
                MessageBox.Show("Postfix can't be empty.", "Invalid operation", MessageBoxButton.OK, MessageBoxImage.Warning);
                PostfixTB.Text = SortedVideoPostfix;
                return;
            }

            SortedVideoPostfix = newText;
            Settings.Default.SortingPostfix = SortedVideoPostfix;
            Settings.Default.Save();
        }

        private void ToggleDebugMode()
        {
            _debugMode = !_debugMode;
            DebugStatusBarItem.Visibility = _debugMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Page_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F12)
            {
                ToggleDebugMode();
            }
        }
    }
}
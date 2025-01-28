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
using System.Windows.Input;
using FFMpegCore.Exceptions;
using System.Text;
using System.ComponentModel;
using FFMpegCore.Enums;

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
        private string _defaultTextCase;
        public string SortedVideoPostfix;

        private List<string> _allVideoPaths;
        private int _currentIndex;

        private bool _reverseVideoOnSave = false;
        private bool _muteVideoOnSave = false;

        private bool _isScreenshottingField;
        private bool _isScreenshotting
        {
            get => _isScreenshottingField;
            set
            {
                _isScreenshottingField = value;
                ProcessButton.IsEnabled = !value;
                TakeMultipleScreenshotsButton.IsEnabled = !value;
                SingleScreenshotButton.IsEnabled = !value;
                SkipButton.IsEnabled = !value;
                NextButton.IsEnabled = !value;
                PreviousButton.IsEnabled = !value;
                DeleteButton.IsEnabled = !value;
                _loggerWindow.LogInfo($"_isScreenshotting updated to {value}");
            }
        }

        private LibVLC _libVLC;
        private MediaPlayer _player;
        private DispatcherTimer _timer;

        private LoggerWindow _loggerWindow;
        #endregion

        #region Initialization

        public NormalSortingView()
        {
            _loggerWindow = new LoggerWindow();
            InitializeComponent();
            InitializeFields();
            InitializeTimer();
            UpdateComponents();
            
        }

        private void InitializeFields()
        {
            _loggerWindow.LogInfo("Initializing fields..");
            // Initialize video folder
            _defaultVideoFolder = Settings.Default.DefaultVideoFolder;
            if (string.IsNullOrEmpty(_defaultVideoFolder))
            {
                _loggerWindow.LogInfo("Default video folder is null or empty");
                _defaultVideoFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                Settings.Default.DefaultVideoFolder = _defaultVideoFolder;
                Settings.Default.Save();
                _loggerWindow.LogInfo($"Default video folder is now set to {_defaultVideoFolder}");
            }

            // Initialize screenshot folder
            _defaultScreenshotFolder = Settings.Default.DefaultScreenshotFolder;
            if (string.IsNullOrEmpty(_defaultScreenshotFolder))
            {
                _loggerWindow.LogInfo("Default screenshot folder is null or empty");
                _defaultScreenshotFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                Settings.Default.DefaultScreenshotFolder = _defaultScreenshotFolder;
                Settings.Default.Save();
                _loggerWindow.LogInfo($"Default video folder is now set to {_defaultScreenshotFolder}");
            }
            
            // Initialize destination folder
            _defaultDestinationFolder = Settings.Default.DefaultDestinationFolder;
            if (string.IsNullOrEmpty(_defaultDestinationFolder))
            {
                _loggerWindow.LogInfo("Default destination folder is null or empty");
                _defaultDestinationFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                Settings.Default.DefaultDestinationFolder = _defaultDestinationFolder;
                Settings.Default.Save();
                _loggerWindow.LogInfo($"Default video folder is now set to {_defaultDestinationFolder}");
            }

            // Initialize default text case
            _defaultTextCase = Settings.Default.DefaultTextCase;
            if (string.IsNullOrEmpty(_defaultTextCase))
            {
                _loggerWindow.LogInfo("Default text case is not set");
                _defaultTextCase = "None";
                Settings.Default.DefaultTextCase = _defaultTextCase;
                Settings.Default.Save();
                _loggerWindow.LogInfo($"Default text case is now set to {_defaultTextCase}");
            }

            // Initialize screenshot interval
            _screenshotInterval = Settings.Default.DefaultScreenshotInterval;

            // Initialize volume
            _defaultVolume = Settings.Default.DefaultVolume;

            // Initialize postfix
            SortedVideoPostfix = Settings.Default.SortingPostfix;
            if (string.IsNullOrEmpty(SortedVideoPostfix))
            {
                _loggerWindow.LogInfo("Default postfix is null or empty");
                SortedVideoPostfix = "Drishya";
                Settings.Default.SortingPostfix = SortedVideoPostfix;
                Settings.Default.Save();
                _loggerWindow.LogInfo($"Default postfix is now set to '{SortedVideoPostfix}'");
            }

            _shouldCreateScreenshotSubfolder = Settings.Default.ShouldCreateScreenshotSubfolder;
            _shouldMoveOnSort = Settings.Default.ShouldMoveOnSort;
            _includeAlreadySorted = Settings.Default.IncludeAlreadySorted;
            _shouldAutoLoop = Settings.Default.ShouldAutoLoop;
            _isPlaying = false;
            _loggerWindow.LogInfo("Initializing fields complete.");
        }

        private void UpdateComponents()
        {
            _loggerWindow.LogInfo("Updating components..");
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

            switch(_defaultTextCase)
            {
                case "None":
                    TextCaseNoneRB.IsChecked = true;
                    break;
                case "Sentence":
                    TextCaseSentenceRB.IsChecked = true;
                    break;
                case "Title":
                    TextCaseTitleRB.IsChecked = true;
                    break;
                case "Upper":
                    TextCaseUpperRB.IsChecked = true;
                    break;
                case "Lower":
                    TextCaseLowerRB.IsChecked = true;
                    break;
                default:
                    TextCaseNoneRB.IsChecked = true;
                    Settings.Default.DefaultTextCase = "None";
                    Settings.Default.Save();
                    break;
            }

            VolumeSlider.Value = _defaultVolume;
            IncludeAlreadySortedCheckbox.IsChecked = _includeAlreadySorted;
            MoveSortedVideoCheckbox.IsChecked = _shouldMoveOnSort;
            ShouldScreenshotSubfolderCheckbox.IsChecked = _shouldCreateScreenshotSubfolder;

            _loggerWindow.LogInfo($"Default video folder: {_defaultVideoFolder}");
            _loggerWindow.LogInfo($"Default screenshot folder: {_defaultScreenshotFolder}");
            _loggerWindow.LogInfo($"Default destination folder: {_defaultDestinationFolder}");

            Core.Initialize();
            _libVLC = new LibVLC("--input-repeat=9999");    // Loop for 9999 times.
            _player = new MediaPlayer(_libVLC);
            VideoPlayerVW.MediaPlayer = _player;
            _player.EnableHardwareDecoding = true;
            _loggerWindow.LogSuccess("Initializing components complete.");
        }

        private void InitializeTimer()
        {
            _loggerWindow.LogInfo("Initializing timer..");
            _timer = new DispatcherTimer();
            double interval = 5;
            _loggerWindow.LogInfo($"Timer interval set to {interval}ms");
            _timer.Interval = TimeSpan.FromMilliseconds(interval);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            _loggerWindow.LogSuccess("Initializing complete.");
        }

        private void UnloadVideo()
        {
            CountLabel.ToolTip = "--/--";
            _loggerWindow.LogInfo("Unloading video started..");
            if (_player?.Media != null)
            {
                _loggerWindow.LogInfo("Player media is not null.");
                _player.Stop();
                _loggerWindow.LogInfo("Player stopped.");
                _player.Media.Dispose();
                _player.Media = null;
                _loggerWindow.LogInfo("Media disposed.");
            }
            GC.Collect();
            _loggerWindow.LogInfo("Garbage Collecter collecting. Now waiting.");
            GC.WaitForPendingFinalizers();
            _loggerWindow.LogSuccess("Video unloaded.");

        }

        public async void CleanupMediaResources(object? sender, CancelEventArgs e)
        {
            _loggerWindow.LogDebug("CleanupMediaResources");
            if (_debugMode)
            {
                await _loggerWindow.WriteLogToDiskAsync();
            }
            _loggerWindow.CloseLogger();
            UnloadVideo();
            _timer.Stop();
            _player?.Dispose();
            _libVLC.Dispose();
        }

        #endregion

        #region File Management
        private void GetAllVideoPaths()
        {
            _loggerWindow.LogInfo("Getting video paths..");
            if (!Directory.Exists(_defaultVideoFolder))
            {
                _loggerWindow.LogError($"_defaultVideoFolder points to a non-existant location. {_defaultVideoFolder}.");
                OpenFolderDialog ofd = new OpenFolderDialog()
                {
                    Title = "Select source folder",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                };
                if (ofd.ShowDialog() == true)
                {
                    _defaultVideoFolder = ofd.FolderName;
                }
                else
                {
                    _defaultVideoFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                    _loggerWindow.LogInfo($"File dialog returned false. Falling back to MyVideos.");
                }
                _loggerWindow.LogInfo($"_defaultVideoFolder updated to {_defaultVideoFolder}.");
                Settings.Default.DefaultVideoFolder = _defaultVideoFolder;
                Settings.Default.Save();
                _loggerWindow.LogInfo($"Preference _defaultVideoFolder saved to disk.");
                FolderPathContainerTB.Text = _defaultVideoFolder;
            }

            _loggerWindow.LogInfo($"Scanning directory for videos.");
            _allVideoPaths = Directory
                .GetFiles(_defaultVideoFolder)
                .Where(IsVideoFile)
                .ToList();
            _loggerWindow.LogInfo($"Found {_allVideoPaths.Count} videos in {_defaultVideoFolder}");
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
            _loggerWindow.LogInfo($"Updating metadata..");
            string path = _allVideoPaths[_currentIndex];
            string title = Path.GetFileName(path);
            try
            {
                _loggerWindow.LogInfo($"Using FFProbe on {path}");
                var mediaInfo = await FFProbe.AnalyseAsync(path);
                MetadataTitleTB.Text = title;
                MetadataLocationLabel.Text = path;

                CountLabel.Text = $"{_currentIndex}/{_allVideoPaths.Count}";
                CountLabel.ToolTip = "Current video/Total videos";

                _loggerWindow.LogInfo($"Updating metadata complete.");
            } catch (FFMpegException ex)
            {
                _loggerWindow.LogWarning($"FFMpegException occured in UpdateMetadata.");
                if (_debugMode) LogExceptionToFile(ex);
                string content = $"Unable to open {title}.\nWould you like me to delete it?\nLocation: {path}";
                if (MessageBox.Show(content, "Error opening video", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    DeleteVideo_Click(new Button(), new RoutedEventArgs());
                } else
                {
                    NextButton_Click(new Button(), new RoutedEventArgs());
                }
            }
            catch (Exception ex)
            {
                _loggerWindow.LogWarning($"Unknown Exception occured in UpdateMetadata.");
                HandleExceptions(ex);
            }
        }

        private void HandleExceptions(Exception ex, string title = "An Error Occurred", MessageBoxImage img = MessageBoxImage.Error)
        {
            _loggerWindow.LogInfo($"Handling Exception..");
            try
            {
                string content = $"Error: {ex.Message}";
                MessageBoxButton btn = MessageBoxButton.OK;

                if (_debugMode)
                {
                    content += $"\nSource: {ex.Source}\nMethod: {ex.TargetSite?.Name}";
                    btn = MessageBoxButton.YesNo;
                }

                MessageBoxResult res = MessageBox.Show(content, title, btn, img);
                _loggerWindow.LogInfo($"Exception shown.");

                if (_debugMode && res == MessageBoxResult.Yes)
                {
                    LogExceptionToFile(ex);
                }
            }
            catch (Exception logEx)
            {
                // Fallback logging if primary exception handling fails
                _loggerWindow.LogCritical($"Failed to handle original exception. Log error: {logEx.Message}. Attempting to write to disk..");
                File.AppendAllText("error_log.txt",
                    $"{DateTime.Now}: Failed to handle original exception. Log error: {logEx.Message}\n");
                _loggerWindow.LogCritical($"error_log.txt written to disk.");
            }
        }

        private void LogExceptionToFile(Exception ex)
        {
            try
            {
                string logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "ApplicationLogs"
                );

                Directory.CreateDirectory(logDir);
                _loggerWindow.LogInfo($"Ensured directory exists: {logDir}");

                string logFilePath = Path.Combine(logDir,
                    $"Exception_{DateTime.Now:yyyyMMddHHmmss}.log");

                string logContent = new StringBuilder()
                    .AppendLine($"Timestamp: {DateTime.Now}")
                    .AppendLine($"Message: {ex.Message}")
                    .AppendLine($"Type: {ex.GetType().FullName}")
                    .AppendLine($"Source: {ex.Source}")
                    .AppendLine($"Method: {ex.TargetSite?.Name}")
                    .AppendLine($"Stack Trace:\n{ex.StackTrace}")
                    .ToString();

                File.WriteAllText(logFilePath, logContent);
                _loggerWindow.LogSuccess($"Exception written to disk: {logFilePath}");
            }
            catch
            {
                // Last-resort logging mechanism
                File.AppendAllText("emergency_log.txt",
                    $"{DateTime.Now}: Critical error logging failed\n");
                _loggerWindow.LogCritical($"Failed to write exception to disk. emergency_log.txt created");
            }
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

        private string RemoveEmojiFromFilename(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);

            // Regex pattern to match emojis
            string emojiPattern = @"[\p{Cs}\p{So}]";
            string cleanFileName = Regex.Replace(fileName, emojiPattern, "");

            if (fileName ==  cleanFileName)
            {
                return filePath;
            }

            string newFilePath = Path.Combine(directory, cleanFileName);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Unrecognizable character(s) were found in the video file name. Video player wont be able to play this video but you could still take screenshots. Would you like to rename it?");
            sb.AppendLine($"Old file name: {fileName}");
            sb.AppendLine($"New file name: {cleanFileName}");
            sb.AppendLine($"Old file location: {filePath}");
            sb.AppendLine($"New file location: {newFilePath}");
            

            if (MessageBox.Show(sb.ToString(), "Invalid operation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (File.Exists(filePath))
                {
                    File.Move(filePath, newFilePath);
                    
                }
                _loggerWindow.LogSuccess($"Renamed filename with emoji to {newFilePath}");
                return newFilePath;
            }
            else
            {
                _loggerWindow.LogWarning($"Keeping the old filename: {fileName}");
                return "-1";
            }
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
                if (_currentIndex >= _allVideoPaths.Count || _currentIndex < 0)
                {
                    _currentIndex = 0;
                }

                string res = RemoveEmojiFromFilename(_allVideoPaths[_currentIndex]);
                if (res != "-1")
                {
                    _allVideoPaths[_currentIndex] = res;
                }

                string currentVideo = _allVideoPaths[_currentIndex];
                _loggerWindow.LogInfo($"Loading file: {res}");

                _isPlaying = false;
                _player.Media?.Dispose();
                _loggerWindow.LogInfo($"Player media disposed.");
                
                Media media = new Media(_libVLC, new Uri(currentVideo, UriKind.Absolute));
                _loggerWindow.LogInfo($"New media created: {currentVideo}");
                
                _player.Media = media;
                _loggerWindow.LogInfo($"Media attached to player.");
                
                media.Dispose();
                _loggerWindow.LogInfo($"Local media disposed.");
                
                PlayButton_Click(PlayButton, new RoutedEventArgs());
                UpdateMetadata();
            }
            catch (IndexOutOfRangeException)
            {
                _loggerWindow.LogWarning($"Index out of range.");
                MessageBox.Show("No more videos in the current directory.\nRescanning from start...", "End of List");
                _currentIndex = 0;
            }
            catch (Exception ex)
            {
                HandleExceptions(ex);
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
                _loggerWindow.LogInfo($"Video player paused.");
            }
            else
            {
                _player.Play();
                _isPlaying = true;
                img.Source = GetImageIconBitmap("pause");
                ((Button)sender).Content = img;
                ((Button)sender).ToolTip = "Pause";
                _loggerWindow.LogInfo($"Video player playing.");
            }
        }

        public void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _player.Stop();
            _isPlaying = false;
            _loggerWindow.LogInfo($"Video player stopped.");
        }

        public void NextButton_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex++;
            _loggerWindow.LogInfo($"Moved to next video.");
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
            _loggerWindow.LogInfo($"Volume set to {_player.Volume}");
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
                _loggerWindow.LogWarning($"Default screenshot folder does not exist.");
                Directory.CreateDirectory(_defaultScreenshotFolder);
                _loggerWindow.LogInfo($"New folder created: {_defaultScreenshotFolder}");
            }

            string destinationFolder = _defaultScreenshotFolder;
            if (_shouldCreateScreenshotSubfolder)
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                destinationFolder = Path.Combine(destinationFolder, filename);

                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                    _loggerWindow.LogInfo($"Subfolder created: {destinationFolder}");
                }
            }
            return destinationFolder;
        }

        private void TakeScreenshot_Click(object sender, RoutedEventArgs e)
        {
            // Return if player has nothing
            if (_player == null || !_player.IsPlaying)
            {
                _loggerWindow.LogWarning($"Media player is either null or not playing. Ignoring click.");
                return;
            }
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string videoFileName = Path.GetFileName(_allVideoPaths[_currentIndex]);
            string parentDir = GenerateProperDirectoryForScreenshots(videoFileName);
            string filename = Path.Combine(parentDir, $"{Path.GetFileNameWithoutExtension(videoFileName)}_screenshot_{timestamp}_{SortedVideoPostfix}.jpg");
            _player.TakeSnapshot(0, filename, 0, 0);
            _loggerWindow.LogInfo($"Single screenshot saved to {filename}");
        }

        private async void TakeMultipleScreenshots_Click(object sender, RoutedEventArgs e)
        {
            await TakeMultipleScreenshots();
        }

        private async Task<bool> TakeMultipleScreenshots()
        {
            _loggerWindow.LogInfo($"Taking multiple screenshots..");
            if (_player == null)
            {
                _loggerWindow.LogWarning($"Media player is null. Ignoring click.");
                return false;
            }

            string inputPath = _allVideoPaths[_currentIndex];
            _loggerWindow.LogInfo($"Video file: {inputPath}");

            string parentDir = GenerateProperDirectoryForScreenshots(inputPath);
            string videoFileName = Path.GetFileName(inputPath);
            
            var videoInfo = FFProbe.Analyse(inputPath);
            TimeSpan totalDuration = videoInfo.Duration;
            _loggerWindow.LogInfo($"Video duration: {totalDuration}");

            int totalScreenshots;
            double interval;

            string dot = "";

            if (_screenshotInterval == 0)
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

            _loggerWindow.LogInfo($"Number of screenshots to be taken: {totalScreenshots}");
            _loggerWindow.LogInfo($"Interval between screenshots: {interval}");

            int successfulScreenshots = 0;
            string statusMessage = _screenshotInterval == 0
                ? $"Taking screenshots of every frame. {successfulScreenshots}/{totalScreenshots} taken{dot}"
                : $"Taking screenshots every {_screenshotInterval} seconds. {successfulScreenshots}/{totalScreenshots} taken{dot}";

            ScreenshotStatusLabel.Text = statusMessage;
            _isScreenshotting = true;
            _loggerWindow.LogInfo($"_isScreenshotting flag set to true.");
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
                    _loggerWindow.LogInfo($"Screenshot saved: {outputPath}");
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
                HandleExceptions(ex);
            }
            finally
            {
                _isScreenshotting = false;
                _loggerWindow.LogInfo($"_isScreenshotting flag set to false.");
                ScreenshotStatusLabel.Text = $"{successfulScreenshots} screenshots taken.";
                _loggerWindow.LogSuccess($"Multiple screenshot complete.");
            }

            return true;
        }

        private void ScreenshotIntervalCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = ScreenshotIntervalCB.SelectedItem as ComboBoxItem;
            _screenshotInterval = Convert.ToDouble(selected?.Tag);
            Settings.Default.DefaultScreenshotInterval = _screenshotInterval;
            Settings.Default.Save();
            _loggerWindow.LogSuccess($"Screenshot interval updated to ${_screenshotInterval} and saved.");
        }
        #endregion

        /*
         * UI Events
         */

        private async Task<string> ModifyVideo(string path)
        {
            if (!_muteVideoOnSave && _reverseVideoOnSave) return path;

            ProcessingBlockerGrid.Visibility = Visibility.Visible;

            string directory = Path.GetDirectoryName(path);
            string extension = Path.GetExtension(path);
            string tempFilename = Path.Combine(directory, $"modded_video_{Guid.NewGuid()}{extension}");

            try
            {
                if (_muteVideoOnSave && _reverseVideoOnSave)
                {
                    await FFMpegArguments
                        .FromFileInput(path)
                        .OutputToFile(tempFilename, false, options => options
                            .WithCustomArgument("-an -vf reverse"))
                        .ProcessAsynchronously();

                    path = tempFilename;
                }
                else if (_muteVideoOnSave)
                {
                    await FFMpegArguments
                        .FromFileInput(path)
                        .OutputToFile(tempFilename, false, options => options
                            .WithCustomArgument("-an"))
                        .ProcessAsynchronously();

                    path = tempFilename;
                }
                else if (_reverseVideoOnSave)
                {
                    await FFMpegArguments
                        .FromFileInput(path)
                        .OutputToFile(tempFilename, false, options => options
                            .WithCustomArgument("-vf reverse"))
                        .ProcessAsynchronously();

                    path = tempFilename;
                }
                return path;
            }
            catch (Exception ex)
            {
                if (File.Exists(tempFilename))
                    File.Delete(tempFilename);

                throw new Exception($"Error modifying video: {ex.Message}", ex);
            }
            finally
            {
                ProcessingBlockerGrid.Visibility = Visibility.Collapsed;
            }

        }

        public async void ProcessVideo_Click(object sender, RoutedEventArgs e)
        {
            _loggerWindow.LogInfo($"Processing video started..");
            UnloadVideo();

            if (!Directory.Exists(_defaultDestinationFolder))
            {
                _loggerWindow.LogInfo($"Default destination folder does not exist. Creating..");
                Directory.CreateDirectory(_defaultDestinationFolder);
                _loggerWindow.LogInfo($"Destination folder created: {_defaultDestinationFolder}");
            }

            string videoFile = _allVideoPaths[_currentIndex];
            _loggerWindow.LogInfo($"Video path: {videoFile}");
            
            string newName = string.IsNullOrEmpty(ControlNameTB.Text)
                ? Path.GetFileName(videoFile)
                : ControlNameTB.Text;

            if (!string.IsNullOrEmpty(newName))
            {
                switch (_defaultTextCase)
                {
                    case "Upper":
                        newName = newName.ToUpper();
                        break;
                    case "Lower":
                        newName = newName.ToLower();
                        break;
                    case "Sentence":
                        newName = char.ToUpper(newName[0]) + newName.Substring(1).ToLower();
                        break;
                    case "Title":
                        string[] items = newName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < items.Length; i++)
                        {
                            items[i] = char.ToUpper(items[i][0]) + items[i].Substring(1).ToLower();
                        }
                        newName = string.Join(" ", items);
                        break;
                    default:
                        break;
                }
            }

            _loggerWindow.LogInfo($"New filename: {newName}");

            if (!string.IsNullOrEmpty(videoFile) && !File.Exists(videoFile))
            {
                string msg = $"Video file at '{videoFile}' does not exist";
                _loggerWindow.LogWarning($"{msg}. Processing stopped.");
                MessageBox.Show(msg, "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
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
                string moddedVideoFile = await ModifyVideo(videoFile);
                File.Move(moddedVideoFile, newFileName);
                _allVideoPaths[_currentIndex] = newFileName;
                StatusLabel.Text = $"Successfully updated {videoFile}";
                _loggerWindow.LogSuccess($"File updated to: {newFileName}");
            }
            _currentIndex++;
            _loggerWindow.LogInfo($"Moving to next video.");
            ChangeVideo();
            _loggerWindow.LogSuccess($"Processing complete.");
        }


        public void SkipVideo_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex++;
            _loggerWindow.LogInfo($"Moving to next video.");
            UpdateView();
        }

        public void PreviousVideo_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex--;
            _loggerWindow.LogInfo($"Moving to previous video.");
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
                    _loggerWindow.LogSuccess($"Delete and moved to recycle bin: {path}");
                    StatusLabel.Text = $"Successfully deleted: {path}";
                    _currentIndex++;
                    ChangeVideo();
                }
                catch (Exception ex)
                {
                    {
                        HandleExceptions(ex);
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
                _loggerWindow.LogSuccess($"Video folder updated to: {_defaultVideoFolder}");
            }
            SearchButton_Click(sender, e);
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
                _loggerWindow.LogSuccess($"Screenshot folder updated to: {_defaultScreenshotFolder}");
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _loggerWindow.LogInfo($"Search Button pressed.");
            UnloadVideo();
            _currentIndex = 0;
            _loggerWindow.LogInfo($"Video list reset.");
            GetAllVideoPaths();
            UpdateView();
        }

        private void OpenInExplorer(string path)
        {
            if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", path);
                _loggerWindow.LogSuccess($"Opened in explorer: {path}");
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
            _loggerWindow.LogDebug($"_includeAlreadySorted updated to {newvalue}");
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
                _loggerWindow.LogInfo($"Highlighting file in folder: {filename}");
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
            _loggerWindow.LogDebug($"_shouldMoveOnSort updated to {newvalue}");
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
                _loggerWindow.LogSuccess($"Destination folder updated and saved: {_defaultDestinationFolder}");
            }
        }

        private void UpdateSubfolderDestinationParameter(bool newvalue)
        {
            _shouldCreateScreenshotSubfolder = newvalue;
            Settings.Default.ShouldCreateScreenshotSubfolder = _shouldCreateScreenshotSubfolder;
            Settings.Default.Save();
            _loggerWindow.LogDebug($"_shouldCreateScreenshotSubFolder flag updated to {_shouldCreateScreenshotSubfolder}");
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
            _loggerWindow.LogInfo($"Postfix updated and saved: {SortedVideoPostfix}");
        }

        private void ToggleDebugMode()
        {
            _debugMode = !_debugMode;
            if (_debugMode)
            {
                _loggerWindow.Show();
            }
            else
            {
                _loggerWindow.Hide();
            }
            _loggerWindow.LogDebug($"Debug mode set to {_debugMode}");
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F12:
                    ToggleDebugMode();
                    e.Handled = true;
                    break;
                case Key.F11:
                    if (!_debugMode) break;
                    if (_loggerWindow.IsVisible)
                    {
                        _loggerWindow.Focus();
                    }
                    else
                    {
                        _loggerWindow.Show();
                    }
                    e.Handled = true;
                    break;
                case Key.Enter:
                    PressProcessButtonIfTBIsFocused();
                    e.Handled = true;
                    break;
                case Key.Delete:
                    PressDeleteButtonIfTBIsFocused();
                    e.Handled = true;
                    break;
                case Key.PageDown:
                    NextButton_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.PageUp:
                    PreviousVideo_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.P:
                    HandleScreenshotPress();
                    break;
                default:
                    break;
            }
            
        }

        private async void HandleScreenshotPress()
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))

            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    await TakeMultipleScreenshots();
                    return;
                }

                TakeScreenshot_Click(new Button(), new RoutedEventArgs());
            }
        }

        private async void PressProcessButtonIfTBIsFocused()
        {
            if (ControlNameTB.IsFocused)
            {
                Button btn = new Button();
                RoutedEventArgs args = new RoutedEventArgs();
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    await TakeMultipleScreenshots();
                }
                ProcessVideo_Click(btn, args);
            }
        }

        public void PressDeleteButtonIfTBIsFocused()
        {
            if (ControlNameTB.IsFocused)
            {
                DeleteVideo_Click(new Button(), new RoutedEventArgs());
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            _loggerWindow.LogInfo($"Drop recieved.");
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                _loggerWindow.LogWarning($"No data found.");
                return;
            }

            string[] videoExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mpeg", ".mpg", ".m4v", ".3gp" };

            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            var validVideos = new List<string>();

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    validVideos.AddRange(
                        Directory.GetFiles(path, "*.*", System.IO.SearchOption.AllDirectories)
                            .Where(file => videoExtensions.Contains(Path.GetExtension(file).ToLower()))
                            .Where(File.Exists)
                    );
                }
                else if (File.Exists(path) && videoExtensions.Contains(Path.GetExtension(path).ToLower()))
                {
                    validVideos.Add(path);
                }
            }

            if (!validVideos.Any())
            {
                _loggerWindow.LogWarning($"No valid videos found.");
                return;
            }

            
            _allVideoPaths = validVideos;
            FolderPathContainerTB.Text = "Drag n Drop mode!";
            _currentIndex = 0;

            _loggerWindow.LogSuccess($"{_allVideoPaths.Count} videos found.");

            UpdateView();
            
        }

        private void TextCaseRB_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            string tag = rb.Tag.ToString()!;
            Settings.Default.DefaultTextCase = tag;
            Settings.Default.Save();
            _defaultTextCase = tag;
            _loggerWindow.LogDebug($"Default Text case set to {tag}");
        }

        private void MuteVideoCB_Checked(object sender, RoutedEventArgs e)
        {
            _muteVideoOnSave = true;
        }

        private void MuteVideoCB_Unchecked(object sender, RoutedEventArgs e)
        {
            _muteVideoOnSave = false;
        }

        private void ReverseVideoCB_Checked(object sender, RoutedEventArgs e)
        {
            _reverseVideoOnSave = true;
        }

        private void ReverseVideoCB_Unchecked(object sender, RoutedEventArgs e)
        {
            _reverseVideoOnSave = false;
        }
    }
}
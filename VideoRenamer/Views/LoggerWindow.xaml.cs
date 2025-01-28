using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Drishya.Views
{
    /// <summary>
    /// Interaction logic for LoggerWindow.xaml
    /// </summary>
    public partial class LoggerWindow : Window
    {
        private DispatcherTimer _updateTimer;
        private PerformanceCounter _cpuCounter;
        private Process _currentProcess;

        private readonly ConcurrentQueue<(string message, MessageLogLevel level)> _messageQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _processQueueTask;

        public ObservableCollection<MessageLog> Messages { get; private set; }

        public LoggerWindow()
        {
            InitializeComponent();
            DataContext = this;
            Messages = new ObservableCollection<MessageLog>();

            _messageQueue = new ConcurrentQueue<(string message, MessageLogLevel level)>();
            _cancellationTokenSource = new CancellationTokenSource();

            InitializePerformanceMonitoring();
            IdLabel.Text = $"ID: {Process.GetCurrentProcess().Id}";
            Closing += LoggerWindow_Closing;

            _processQueueTask = Task.Run(ProcessMessageQueueAsync);
        }

        private async Task ProcessMessageQueueAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_messageQueue.TryDequeue(out var messageInfo))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var ml = new MessageLog(messageInfo.message, messageInfo.level);
                        Messages.Add(ml);
                        MainSV.ScrollToEnd();
                    });
                }
                else
                {
                    await Task.Delay(50);
                }
            }
        }

        private void LoggerWindow_Closing(object? sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        public void CloseLogger()
        {
            _cancellationTokenSource.Cancel();
            _processQueueTask.Wait(1000);
            Closing -= LoggerWindow_Closing;
            Close();
        }

        private void InitializePerformanceMonitoring()
        {
            _currentProcess = Process.GetCurrentProcess();

            _cpuCounter = new PerformanceCounter("Process", "% Processor Time", _currentProcess.ProcessName);

            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(1); // Update every second
            _updateTimer.Tick += UpdatePerformanceMetrics;
            _updateTimer.Start();
        }

        private void UpdatePerformanceMetrics(object? sender, EventArgs e)
        {
            try
            {
                float cpuUsage = _cpuCounter.NextValue() / Environment.ProcessorCount; // Normalize CPU usage
                float memoryUsageMB = _currentProcess.WorkingSet64 / (1024f * 1024f);

                Process process = Process.GetCurrentProcess();
                CpuLabel.Text = $"CPU: {cpuUsage:F1}%";
                MemoryLabel.Text = $"RAM: {memoryUsageMB:F1} MB";
                ThreadLabel.Text = $"Threads: {process.Threads.Count}";
                HandleLabel.Text = $"Handles: {process.HandleCount}";
                StackDepthLabel.Text = $"Stack depth: {new StackTrace().FrameCount}";
                GCLabel.Text = $"GC: {GC.GetTotalMemory(false) / (1024 * 1024)} MB";
                GC0Label.Text = $"GC0: {GC.CollectionCount(0)}";
                GC1Label.Text = $"GC1: {GC.CollectionCount(1)}";
                GC2Label.Text = $"GC2: {GC.CollectionCount(2)}";

            }
            catch (Exception ex)
            {
                LogError($"Error updating performance: {ex.Message}");
            }
        }

        private void UpdateParent(string message, MessageLogLevel level)
        {
            MessageLog ml = new MessageLog(message, level);
            Messages.Add(ml);
            MainSV.ScrollToEnd();
        }

        public void LogInfo(string message)
        {
            _messageQueue.Enqueue((message, MessageLogLevel.Information));
        }

        public void LogWarning(string message)
        {
            _messageQueue.Enqueue((message, MessageLogLevel.Warning));
        }

        public void LogError(string message)
        {
            _messageQueue.Enqueue((message, MessageLogLevel.Error));
        }

        public void LogDebug(string message)
        {
            _messageQueue.Enqueue((message, MessageLogLevel.Debug));
        }

        public void LogSuccess(string message)
        {
            _messageQueue.Enqueue((message, MessageLogLevel.Success));
        }

        public void LogTrace(string message)
        {
            _messageQueue.Enqueue((message, MessageLogLevel.Trace));
        }

        public void LogCritical(string message)
        {
            _messageQueue.Enqueue((message, MessageLogLevel.Critical));
        }

        public string GenerateLogAsString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"Drishya log generated at {DateTime.Now}");
            foreach (MessageLog item in Messages)
            {
                stringBuilder.AppendLine($"{item.Date_Time} | {item.Level} | {item.Message}");
            }
            return stringBuilder.ToString();
        }

        public async Task WriteLogToDiskAsync()
        {
            string dir = Directory.GetCurrentDirectory();
            dir = Path.Combine(dir, "logs");
            Directory.CreateDirectory(dir);
            string fn = Path.Combine(dir, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await File.WriteAllTextAsync(fn, GenerateLogAsString());
            LogSuccess($"Saved to {fn}");
        }

        public async void SaveToDisk_Click(object sender, RoutedEventArgs e)
        {
            await WriteLogToDiskAsync();
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FontSize = (double)e.NewValue;
        }
    }

    public class MessageLog
    {
        public string Message { get; set; }

        private MessageLogLevel _level { get; set; }
        public string Level { get; set; }
        public string Date_Time { get; set; }
        public Brush Brush { get; set; }

        public MessageLog(string message, MessageLogLevel level)
        {
            Message = message;
            Date_Time = DateTime.Now.ToString();
            
            _level = level;
            switch (level)
            {
                case MessageLogLevel.Error:
                    Level = "ERRO";
                    Brush = Brushes.Red;
                    break;
                case MessageLogLevel.Warning:
                    Level = "INFO";
                    Brush = Brushes.Yellow;
                    break;
                case MessageLogLevel.Critical:
                    Level = "CRIT";
                    Brush = Brushes.DarkRed;
                    break;
                case MessageLogLevel.Debug:
                    Level = "DBUG";
                    Brush = Brushes.Blue;
                    break;
                case MessageLogLevel.Trace:
                    Level = "TRAC";
                    Brush = Brushes.Magenta;
                    break;
                case MessageLogLevel.Success:
                    Level = "SUCC";
                    Brush = Brushes.DarkGreen;
                    break;
                default:
                    Level = "INFO";
                    Brush = Brushes.Black;
                    break;
                
            }
        }
    }

    public enum MessageLogLevel
    {
        Information, Success, Error, Warning, Debug, Critical, Trace
    }
}

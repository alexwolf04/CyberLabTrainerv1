using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberLabTrainer
{
    public partial class MainWindow : Window
    {
        private FileSystemWatcher watcher;
        private CancellationTokenSource attackTokenSource;
        private int score = 0;
        private ListBox fileEvents;
        private TextBlock scoreDisplay;

        // For process challenge
        private HashSet<string> suspiciousProcesses = new HashSet<string>
        {
            "badprocess.exe",
            "malware123.exe",
            "eviltool.exe"
        };
        private ListBox processListBox;
        private TextBlock processScoreDisplay;
        private int processScore = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ClearMainContent()
        {
            MainContent.Children.Clear();
            StopSimulatedAttack();
            processScore = 0;
            processScoreDisplay = null;
            processListBox = null;
        }

        private void ShowPowerShellUI(object sender, RoutedEventArgs e)
        {
            ClearMainContent();

            var input = new TextBox
            {
                Height = 30,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 46)),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                Padding = new Thickness(5)
            };

            var runBtn = new Button
            {
                Content = "Run PowerShell",
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5),
                Background = (Brush)new BrushConverter().ConvertFrom("#4A90E2"),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var output = new TextBox
            {
                Margin = new Thickness(5),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Height = 400,
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 40)),
                Foreground = Brushes.LightGreen,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                IsReadOnly = true,
                Padding = new Thickness(8)
            };

            runBtn.Click += (s, e2) =>
            {
                try
                {
                    using (PowerShell ps = PowerShell.Create())
                    {
                        ps.AddScript(input.Text);
                        StringBuilder result = new StringBuilder();
                        foreach (var item in ps.Invoke())
                        {
                            result.AppendLine(item.ToString());
                        }
                        output.Text = result.ToString();
                    }
                }
                catch (Exception ex)
                {
                    output.Text = "Error running PowerShell script:\n" + ex.Message;
                }
            };

            MainContent.Children.Add(input);
            MainContent.Children.Add(runBtn);
            MainContent.Children.Add(output);
        }

        private void ShowEventLogUI(object sender, RoutedEventArgs e)
        {
            ClearMainContent();

            var logBox = new ListBox
            {
                Margin = new Thickness(5),
                Height = 700,
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 40)),
                Foreground = Brushes.LightGray,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13
            };

            try
            {
                EventLog securityLog = new EventLog("Security");
                foreach (EventLogEntry entry in securityLog.Entries)
                {
                    string message = entry.Message.Length > 200 ? entry.Message.Substring(0, 200) : entry.Message;
                    logBox.Items.Add($"[{entry.TimeGenerated}] {entry.Source} - {message}...");
                }
            }
            catch (Exception ex)
            {
                logBox.Items.Add("Failed to load Security Event Log. Try running as Administrator.\n" + ex.Message);
            }

            MainContent.Children.Add(logBox);
        }

        private void ShowFileMonitorUI(object sender, RoutedEventArgs e)
        {
            ClearMainContent();
            score = 0;

            var instructions = new TextBlock
            {
                Text = "🛡️ Challenge: Detect and respond to unusual file creations in Temp folder.\nFiles with .tmp extensions will appear.\nClick 'Respond' to delete the file and gain points.",
                Margin = new Thickness(10),
                Foreground = Brushes.LightGray,
                FontSize = 16
            };

            scoreDisplay = new TextBlock
            {
                Text = "Score: 0",
                Margin = new Thickness(10),
                Foreground = Brushes.LightGreen,
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };

            fileEvents = new ListBox
            {
                Margin = new Thickness(5),
                Height = 500,
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 40)),
                Foreground = Brushes.LightGray,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13
            };

            MainContent.Children.Add(instructions);
            MainContent.Children.Add(scoreDisplay);
            MainContent.Children.Add(fileEvents);

            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            watcher = new FileSystemWatcher
            {
                Path = Path.GetTempPath(),
                Filter = "*.tmp",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            watcher.Created += (s, ev) =>
            {
                Dispatcher.Invoke(() =>
                {
                    StackPanel itemPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    TextBlock fileText = new TextBlock
                    {
                        Text = $"[ALERT] {ev.Name}",
                        Width = 500,
                        Foreground = Brushes.OrangeRed,
                        FontWeight = FontWeights.SemiBold
                    };

                    Button respondButton = new Button
                    {
                        Content = "Respond",
                        Tag = ev.FullPath,
                        Margin = new Thickness(5, 0, 0, 0),
                        Background = (Brush)new BrushConverter().ConvertFrom("#E94B3C"),
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        BorderThickness = new Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Padding = new Thickness(6, 2, 6, 2)
                    };
                    respondButton.Click += RespondToThreat;

                    itemPanel.Children.Add(fileText);
                    itemPanel.Children.Add(respondButton);
                    fileEvents.Items.Insert(0, itemPanel);
                });
            };

            watcher.EnableRaisingEvents = true;
            StartSimulatedAttack();
        }

        private void RespondToThreat(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string path && File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    score += 10;
                    scoreDisplay.Text = $"Score: {score}";
                    btn.Content = "✅ Removed";
                    btn.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void StartSimulatedAttack()
        {
            attackTokenSource = new CancellationTokenSource();
            CancellationToken token = attackTokenSource.Token;
            string tempPath = Path.GetTempPath();

            Task.Run(() =>
            {
                Random rnd = new Random();
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        string filename = Path.Combine(tempPath, $"malicious_{rnd.Next(1000, 9999)}.tmp");
                        File.WriteAllText(filename, "malicious content");
                        Thread.Sleep(rnd.Next(3000, 7000));
                    }
                    catch { }
                }
            }, token);
        }

        private void StopSimulatedAttack()
        {
            if (attackTokenSource != null)
            {
                attackTokenSource.Cancel();
                attackTokenSource.Dispose();
                attackTokenSource = null;
            }
        }

        // NEW PROCESS MONITORING CHALLENGE MODULE

        private void ShowProcessMonitorUI(object sender, RoutedEventArgs e)
        {
            ClearMainContent();
            processScore = 0;

            var instructions = new TextBlock
            {
                Text = "🛡️ Challenge: Identify and mark suspicious processes.\nClick 'Mark as Malicious' on suspicious processes to earn points!",
                Margin = new Thickness(10),
                Foreground = Brushes.LightGray,
                FontSize = 16
            };

            processScoreDisplay = new TextBlock
            {
                Text = "Score: 0",
                Margin = new Thickness(10),
                Foreground = Brushes.LightGreen,
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };

            processListBox = new ListBox
            {
                Margin = new Thickness(5),
                Height = 600,
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 40)),
                Foreground = Brushes.LightGray,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13
            };

            MainContent.Children.Add(instructions);
            MainContent.Children.Add(processScoreDisplay);
            MainContent.Children.Add(processListBox);

            LoadProcesses();
        }

        private void LoadProcesses()
        {
            processListBox.Items.Clear();

            Process[] processes = Process.GetProcesses();

            Random rnd = new Random();

            // Add some simulated suspicious processes by injecting names in the list randomly
            List<string> fakeSuspicious = new List<string> { "badprocess.exe", "malware123.exe", "eviltool.exe" };

            foreach (var proc in processes)
            {
                string procName = proc.ProcessName + ".exe";
                bool isSuspicious = suspiciousProcesses.Contains(procName);

                // Randomly add fake suspicious processes too
                if (rnd.NextDouble() < 0.005) // small chance to add fake suspicious process
                {
                    procName = fakeSuspicious[rnd.Next(fakeSuspicious.Count)];
                    isSuspicious = true;
                }

                StackPanel itemPanel = new StackPanel { Orientation = Orientation.Horizontal };

                TextBlock procText = new TextBlock
                {
                    Text = procName,
                    Width = 450,
                    Foreground = isSuspicious ? Brushes.OrangeRed : Brushes.LightGray,
                    FontWeight = isSuspicious ? FontWeights.Bold : FontWeights.Normal
                };

                Button markBtn = new Button
                {
                    Content = "Mark as Malicious",
                    Tag = new Tuple<string, bool>(procName, isSuspicious),
                    Margin = new Thickness(5, 0, 0, 0),
                    Background = (Brush)new BrushConverter().ConvertFrom("#E94B3C"),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Padding = new Thickness(6, 2, 6, 2)
                };
                markBtn.Click += MarkProcessAsMalicious;

                itemPanel.Children.Add(procText);
                itemPanel.Children.Add(markBtn);

                processListBox.Items.Add(itemPanel);
            }
        }

        private void MarkProcessAsMalicious(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Tuple<string, bool> data)
            {
                string procName = data.Item1;
                bool isSuspicious = data.Item2;

                if (isSuspicious)
                {
                    processScore += 15;
                    processScoreDisplay.Text = $"Score: {processScore}";
                    btn.Content = "✅ Correct!";
                    btn.IsEnabled = false;
                }
                else
                {
                    MessageBox.Show($"{procName} is not suspicious.", "Try Again", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}

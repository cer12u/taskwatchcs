using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TaskManager
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private DateTime startTime;
        private bool isRunning = false;
        private ObservableCollection<TaskItem> tasks;
        private ObservableCollection<TaskItem> completedTasks;
        private readonly string taskSaveFile = "tasks.json";
        private readonly string completedTaskSaveFile = "completed_tasks.json";
        private TaskLogger logger;
        private TaskItem otherTask;
        private DateTime? lastTickTime;
        private TimeSpan baseElapsedTime;

        public MainWindow()
        {
            InitializeComponent();
            InitializeStopwatch();
            InitializeTasks();
            LoadTasks();
            InitializeLogger();

            TaskList.MouseDoubleClick += TaskList_MouseDoubleClick;
            
            // Create "other" task
            otherTask = new TaskItem("その他", "選択されていないときの作業時間", TimeSpan.FromHours(24));
        }

        private void InitializeLogger()
        {
            logger = new TaskLogger();
        }

        private void InitializeStopwatch()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100); // Update for 1 digit milliseconds
            timer.Tick += Timer_Tick;
            StopButton.IsEnabled = false;
            UpdateCurrentTaskDisplay();
        }

        private void InitializeTasks()
        {
            tasks = new ObservableCollection<TaskItem>();
            completedTasks = new ObservableCollection<TaskItem>();
            TaskList.ItemsSource = tasks;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            TimeSpan currentElapsed = now - startTime;
            TimeSpan totalElapsed = baseElapsedTime + currentElapsed;
            
            // Update display with total elapsed time
            StopwatchDisplay.Text = totalElapsed.ToString(@"hh\:mm\:ss");
            StopwatchMilliseconds.Text = $".{(totalElapsed.Milliseconds / 100)}";
            
            lastTickTime = now;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
            {
                startTime = DateTime.Now;
                lastTickTime = startTime;
                timer.Start();
                isRunning = true;
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;

                var currentTask = TaskList.SelectedItem as TaskItem ?? otherTask;
                logger.LogTaskStart(currentTask);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                StopTimer();
            }
        }

        private void StopTimer()
        {
            if (isRunning)
            {
                timer.Stop();
                isRunning = false;
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;

                var currentTask = TaskList.SelectedItem as TaskItem ?? otherTask;
                var duration = DateTime.Now - startTime;
                currentTask.AddElapsedTime(duration);
                baseElapsedTime = currentTask.ElapsedTime;
                logger.LogTaskStop(currentTask, duration);
                lastTickTime = null;
                SaveTasks();

                // Update display with final time
                StopwatchDisplay.Text = baseElapsedTime.ToString(@"hh\:mm\:ss");
                StopwatchMilliseconds.Text = $".{(baseElapsedTime.Milliseconds / 100)}";
            }
        }

        private void TaskList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isRunning)
            {
                StopTimer(); // タスク切り替え時にストップウォッチを停止
            }

            var currentTask = TaskList.SelectedItem as TaskItem ?? otherTask;
            baseElapsedTime = currentTask.ElapsedTime;
            StopwatchDisplay.Text = baseElapsedTime.ToString(@"hh\:mm\:ss");
            StopwatchMilliseconds.Text = $".{(baseElapsedTime.Milliseconds / 100)}";

            UpdateCurrentTaskDisplay();
        }

        private void UpdateCurrentTaskDisplay()
        {
            var currentTask = TaskList.SelectedItem as TaskItem;
            CurrentTaskName.Text = currentTask != null 
                ? $"選択タスク: {currentTask.Name}"
                : "選択タスク: その他";
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskItem task)
            {
                tasks.Remove(task);
                SaveTasks();
            }
        }

        private void CompleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskItem task)
            {
                if (isRunning && TaskList.SelectedItem == task)
                {
                    StopTimer(); // 完了するタスクが現在実行中なら停止
                }

                task.Complete();
                tasks.Remove(task);
                completedTasks.Add(task);
                logger.LogTaskComplete(task);
                SaveTasks();
                SaveCompletedTasks();
            }
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var inputWindow = new TaskInputWindow
            {
                Owner = this
            };

            if (inputWindow.ShowDialog() == true && inputWindow.CreatedTask != null)
            {
                tasks.Add(inputWindow.CreatedTask);
                SaveTasks();
            }
        }

        private void TaskList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TaskList.SelectedItem is TaskItem selectedTask)
            {
                var dialog = new TaskEditDialog(selectedTask.Name, selectedTask.Memo)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true)
                {
                    selectedTask.Name = dialog.TaskName;
                    selectedTask.Memo = dialog.Memo;
                    SaveTasks();
                }
            }
        }

        private void TopMostMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Topmost = TopMostMenuItem.IsChecked;
        }

        private void ShowCompletedTasks_Click(object sender, RoutedEventArgs e)
        {
            var message = new System.Text.StringBuilder();
            message.AppendLine("完了済みタスク:");
            
            foreach (var task in completedTasks)
            {
                message.AppendLine($"- {task.Name}");
                message.AppendLine($"  完了日時: {task.CompletedAt:yyyy/MM/dd HH:mm:ss}");
                message.AppendLine($"  合計時間: {task.ElapsedTime:hh\\:mm\\:ss}");
                if (!string.IsNullOrWhiteSpace(task.Memo))
                {
                    message.AppendLine($"  メモ: {task.Memo}");
                }
                message.AppendLine();
            }

            MessageBox.Show(message.ToString(), "完了済みタスク一覧", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isRunning)
            {
                StopTimer(); // アプリ終了時にタイマーを停止
            }
            SaveTasks();
            SaveCompletedTasks();
        }

        private void SaveTasks_Click(object sender, RoutedEventArgs e)
        {
            SaveTasks();
            SaveCompletedTasks();
            MessageBox.Show("タスクを保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadTasks_Click(object sender, RoutedEventArgs e)
        {
            LoadTasks();
            MessageBox.Show("タスクを読み込みました。", "読み込み完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveTasks()
        {
            try
            {
                var json = JsonSerializer.Serialize(tasks);
                File.WriteAllText(taskSaveFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"タスクの保存中にエラーが発生しました。\n{ex.Message}", 
                              "エラー", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private void SaveCompletedTasks()
        {
            try
            {
                var json = JsonSerializer.Serialize(completedTasks);
                File.WriteAllText(completedTaskSaveFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"完了済みタスクの保存中にエラーが発生しました。\n{ex.Message}", 
                              "エラー", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private void LoadTasks()
        {
            LoadActiveTasks();
            LoadCompletedTasks();
        }

        private void LoadActiveTasks()
        {
            try
            {
                if (File.Exists(taskSaveFile))
                {
                    var json = File.ReadAllText(taskSaveFile);
                    var loadedTasks = JsonSerializer.Deserialize<ObservableCollection<TaskItem>>(json);
                    if (loadedTasks != null)
                    {
                        tasks.Clear();
                        foreach (var task in loadedTasks)
                        {
                            tasks.Add(task);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"タスクの読み込み中にエラーが発生しました。\n{ex.Message}", 
                              "エラー", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private void LoadCompletedTasks()
        {
            try
            {
                if (File.Exists(completedTaskSaveFile))
                {
                    var json = File.ReadAllText(completedTaskSaveFile);
                    var loaded = JsonSerializer.Deserialize<ObservableCollection<TaskItem>>(json);
                    if (loaded != null)
                    {
                        completedTasks.Clear();
                        foreach (var task in loaded)
                        {
                            completedTasks.Add(task);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"完了済みタスクの読み込み中にエラーが発生しました。\n{ex.Message}", 
                              "エラー", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }
    }
}
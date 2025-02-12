﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Toolkit.Uwp.Notifications;

namespace TaskManager
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer timer = new();
        private readonly DispatcherTimer resetCheckTimer = new();
        private readonly DispatcherTimer inactiveCheckTimer = new();
        private readonly DispatcherTimer notificationTimer = new();
        private static readonly TimeSpan InactiveDuration = TimeSpan.FromHours(72);
        private DateTime startTime;
        private bool isRunning = false;
        private readonly ObservableCollection<TaskItem> inProgressTasks = new();
        private readonly ObservableCollection<TaskItem> pendingTasks = new();
        private readonly ObservableCollection<TaskItem> completedTasks = new();
        private readonly string taskSaveFile = "tasks.json";
        private readonly TaskLogger logger;
        private readonly TaskItem otherTask;
        private DateTime? lastTickTime;
        private TimeSpan baseElapsedTime;
        private TaskItem? runningTask;
        private string? currentNotificationId = null;
        private DateTime? lastTaskSwitchTime = null;
        private readonly TimeSpan taskSwitchGracePeriod = TimeSpan.FromSeconds(10);
        private TaskItem? previousRunningTask = null;
        private TaskItem? lastRunningTask = null;
        private TimeSpan lastTaskElapsed = TimeSpan.Zero;

        public MainWindow()
        {
            InitializeComponent();
            logger = new TaskLogger();
            otherTask = new TaskItem("その他", "選択されていないときの作業時間", TimeSpan.FromHours(24));

            InitializeStopwatch();
            InitializeResetTimer();
            InitializeInactiveCheckTimer();
            InitializeNotificationTimer();
            InitializeTasks();
            LoadTasks();
            CheckAndArchiveTasks();
            CheckInactiveTasks();

            InProgressList.PreviewMouseDown += ListBox_PreviewMouseDown;
            PendingList.PreviewMouseDown += ListBox_PreviewMouseDown;
            CompletedList.PreviewMouseDown += ListBox_PreviewMouseDown;

            InProgressList.PreviewKeyDown += ListBox_PreviewKeyDown;
            PendingList.PreviewKeyDown += ListBox_PreviewKeyDown;
            CompletedList.PreviewKeyDown += ListBox_PreviewKeyDown;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
            }
        }

        private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
            }
        }

        private void InitializeStopwatch()
        {
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            UpdateCurrentTaskDisplay();
            UpdateTimerControls();
        }

        private void InitializeResetTimer()
        {
            resetCheckTimer.Interval = TimeSpan.FromMinutes(1);
            resetCheckTimer.Tick += ResetCheckTimer_Tick;
            resetCheckTimer.Start();
        }

        private void InitializeInactiveCheckTimer()
        {
            inactiveCheckTimer.Interval = TimeSpan.FromHours(1);
            inactiveCheckTimer.Tick += InactiveCheckTimer_Tick;
            inactiveCheckTimer.Start();
        }

        private void InitializeNotificationTimer()
        {
            // 既存の通知タイマーの初期化は不要になったため、空にする
        }

        private void InactiveCheckTimer_Tick(object? sender, EventArgs e)
        {
            CheckInactiveTasks();
        }

        private void CheckInactiveTasks()
        {
            var settings = Settings.Instance;
            if (settings.InactiveTasksEnabled)
            {
                var inactiveTasks = inProgressTasks
                    .Where(task => task.IsInactive(InactiveDuration))
                    .ToList();

                foreach (var task in inactiveTasks)
                {
                    inProgressTasks.Remove(task);
                    task.SetPending();
                    pendingTasks.Add(task);
                    logger.LogTaskStop(task, TimeSpan.Zero);
                }

                if (inactiveTasks.Any())
                {
                    SaveTasks();
                }
            }
        }

        private void ResetCheckTimer_Tick(object? sender, EventArgs e)
        {
            CheckAndArchiveTasks();
            CheckInactiveTasks();
        }

        private void CheckAndArchiveTasks()
        {
            var settings = Settings.Instance;
            if (settings.NeedsReset())
            {
                // 完了済みタスクをアーカイブする
                var tasksToArchive = completedTasks.ToList();
                foreach (var task in tasksToArchive)
                {
                    completedTasks.Remove(task);
                }

                // アーカイブしたタスクをファイルに保存
                var archiveFile = "completed_tasks.json";
                var existingArchived = new List<TaskItem>();
                if (File.Exists(archiveFile))
                {
                    var json = File.ReadAllText(archiveFile);
                    existingArchived = JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
                }
                existingArchived.AddRange(tasksToArchive);
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(archiveFile, JsonSerializer.Serialize(existingArchived, options));

                settings.LastResetTime = DateTime.Now;
                settings.Save();
                SaveTasks();
            }
        }

        private void InitializeTasks()
        {
            InProgressList.ItemsSource = inProgressTasks;
            PendingList.ItemsSource = pendingTasks;
            CompletedList.ItemsSource = completedTasks;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            var currentTask = GetSelectedTask();
            TimeSpan currentElapsed = now - startTime;

            if (currentTask == runningTask)
            {
                // 選択中のタスクが実行中のタスクと同じ場合は時間を加算
                TimeSpan totalElapsed = baseElapsedTime + currentElapsed;
                StopwatchDisplay.Text = totalElapsed.ToString(@"hh\:mm\:ss");
                StopwatchMilliseconds.Text = $".{(totalElapsed.Milliseconds / 100)}";
            }
            else
            {
                // 別のタスクを表示中の場合は、そのタスクの経過時間をそのまま表示
                StopwatchDisplay.Text = currentTask?.ElapsedTime.ToString(@"hh\:mm\:ss") ?? "00:00:00";
                StopwatchMilliseconds.Text = $".{(currentTask?.ElapsedTime.Milliseconds ?? 0) / 100}";
            }
            
            lastTickTime = now;
        }

        private void UpdateTimerControls()
        {
            var selectedTask = GetSelectedTask();
            bool canStart = selectedTask == null || selectedTask.Status == TaskStatus.InProgress;
            
            StartStopButton.Content = isRunning ? "停止" : "開始";
            StartStopButton.Style = isRunning ? 
                FindResource("DangerButton") as Style : 
                FindResource("SuccessButton") as Style;
            StartStopButton.IsEnabled = !isRunning || canStart;
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                StopTimer();
            }
            else
            {
                StartTimer();
            }
        }

        private void StartTimer()
        {
            var selectedTask = GetSelectedTask();
            if (selectedTask != null && selectedTask.Status != TaskStatus.InProgress)
            {
                MessageBox.Show("進行中のタスクのみ時間を記録できます。",
                              "警告",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            // 猶予時間中に新しいタスクを開始した場合、前のタスクの時間を確定
            if (lastRunningTask != null && lastTaskElapsed > TimeSpan.Zero)
            {
                lastRunningTask.AddElapsedTime(lastTaskElapsed);
                lastRunningTask.IsProcessing = false;
                if (currentNotificationId != null)
                {
                    ToastNotificationManagerCompat.History.Remove(currentNotificationId);
                    currentNotificationId = null;
                }
                SaveTasks();
            }

            startTime = DateTime.Now;
            lastTickTime = startTime;
            timer.Start();
            isRunning = true;
            UpdateTimerControls();
            runningTask = selectedTask;

            // 前のタスクの情報をクリア
            lastRunningTask = null;
            lastTaskElapsed = TimeSpan.Zero;
            lastTaskSwitchTime = null;
            previousRunningTask = null;
            
            if (selectedTask != null)
            {
                selectedTask.IsProcessing = true;
                ScheduleNotification(selectedTask);
            }
            
            logger.LogTaskStart(selectedTask ?? otherTask);
        }

        private void StopTimer()
        {
            if (isRunning)
            {
                timer.Stop();
                isRunning = false;
                UpdateTimerControls();
                var duration = DateTime.Now - startTime;

                // 実行中の通知をキャンセル
                if (currentNotificationId != null)
                {
                    ToastNotificationManagerCompat.History.Remove(currentNotificationId);
                    currentNotificationId = null;
                }

                if (runningTask != null)
                {
                    runningTask.IsProcessing = false;
                }

                if (runningTask == null)
                {
                    var today = DateTime.Now;
                    var taskName = $"その他 ({today:MM/dd})";
                    var existingTask = inProgressTasks.FirstOrDefault(t => t.Name == taskName);

                    if (existingTask != null)
                    {
                        existingTask.Memo += $"\n{today:HH:mm} - {duration:hh\\:mm} の作業";
                        existingTask.AddElapsedTime(duration);
                    }
                    else
                    {
                        var newTask = new TaskItem(taskName, $"{today:HH:mm} - {duration:hh\\:mm} の作業", TimeSpan.FromHours(24));
                        newTask.AddElapsedTime(duration);
                        inProgressTasks.Add(newTask);
                    }
                }
                else
                {
                    runningTask.AddElapsedTime(duration);
                }

                baseElapsedTime = (runningTask ?? otherTask).ElapsedTime;
                logger.LogTaskStop(runningTask ?? otherTask, duration);
                lastTickTime = null;
                runningTask = null;
                SaveTasks();

                StopwatchDisplay.Text = baseElapsedTime.ToString(@"hh\:mm\:ss");
                StopwatchMilliseconds.Text = $".{(baseElapsedTime.Milliseconds / 100)}";
            }
        }

        private void ScheduleNotification(TaskItem task)
        {
            var notificationId = Guid.NewGuid().ToString();
            currentNotificationId = notificationId;

            // 30分後の通知をスケジュール
            var scheduledTime = DateTime.Now.AddMinutes(30);
            
            var builder = new ToastContentBuilder()
                .AddText($"タスク: {task.Name}")
                .AddText("開始から30分が経過しました。")
                .SetToastScenario(ToastScenario.Default);

            // 通知をスケジュール
            builder.Schedule(scheduledTime, toast =>
            {
                toast.Tag = notificationId;
                toast.Group = "TaskManager";
            });
        }

        private void ListBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                var item = listBox.InputHitTest(e.GetPosition(listBox));
                if (item == listBox)
                {
                    listBox.SelectedItem = null;
                    e.Handled = true;
                }
            }
        }

        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                var item = listBox.InputHitTest(e.GetPosition(listBox));
                if (item == listBox)
                {
                    e.Handled = true;
                }
            }
        }

        private void TaskList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                if (listBox != InProgressList) InProgressList.SelectedItem = null;
                if (listBox != PendingList) PendingList.SelectedItem = null;
                if (listBox != CompletedList) CompletedList.SelectedItem = null;

                if (e.AddedItems.Count == 0)
                {
                    listBox.SelectedItem = null;
                }
            }

            if (isRunning)
            {
                var currentTask = GetSelectedTask();
                if (currentTask != runningTask)
                {
                    if (lastTaskSwitchTime == null)
                    {
                        lastTaskSwitchTime = DateTime.Now;
                        previousRunningTask = runningTask;
                        lastRunningTask = runningTask;
                        lastTaskElapsed = DateTime.Now - startTime;
                        // ボタンを開始状態に変更
                        isRunning = false;
                        UpdateTimerControls();
                    }
                    else
                    {
                        var timeSinceSwitch = DateTime.Now - lastTaskSwitchTime.Value;
                        if (timeSinceSwitch > taskSwitchGracePeriod)
                        {
                            StopTimer();
                            lastTaskSwitchTime = null;
                            previousRunningTask = null;
                            lastRunningTask = null;
                            lastTaskElapsed = TimeSpan.Zero;
                        }
                        else if (currentTask == previousRunningTask)
                        {
                            lastTaskSwitchTime = null;
                            previousRunningTask = null;
                            // 元のタスクに戻った場合は実行中状態に戻す
                            isRunning = true;
                            UpdateTimerControls();
                        }
                    }
                }
            }

            var selectedTask = GetSelectedTask();
            if (selectedTask != null)
            {
                baseElapsedTime = selectedTask.ElapsedTime;
            }
            else
            {
                baseElapsedTime = TimeSpan.Zero;
            }

            StopwatchDisplay.Text = baseElapsedTime.ToString(@"hh\:mm\:ss");
            StopwatchMilliseconds.Text = $".{(baseElapsedTime.Milliseconds / 100)}";

            UpdateCurrentTaskDisplay();
            UpdateTimerControls();
        }

        private void EditTask_Click(object sender, EventArgs e)
        {
            TaskItem? selectedTask = null;

            if (e is MouseButtonEventArgs && sender is ListBox listBox)
            {
                selectedTask = listBox.SelectedItem as TaskItem;
            }
            else if (sender is FrameworkElement element)
            {
                selectedTask = element.DataContext as TaskItem;
            }

            if (selectedTask != null)
            {
                logger.LogTrace($"タスク編集開始: {selectedTask.Name}");
                logger.LogTrace($"編集前の値: EstimatedTime={selectedTask.EstimatedTime}, ElapsedTime={selectedTask.ElapsedTime}, Priority={selectedTask.Priority}");

                var dialog = new TaskEditDialog(
                    selectedTask.Name,
                    selectedTask.Memo ?? "",
                    selectedTask.EstimatedTime,
                    selectedTask.ElapsedTime,
                    selectedTask.Priority)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true && dialog.TaskName != null)
                {
                    logger.LogTrace($"タスク編集の保存: {dialog.TaskName}");
                    logger.LogTrace($"変更後の値: EstimatedTime={dialog.EstimatedTime}, ElapsedTime={dialog.ElapsedTime}, Priority={dialog.Priority}");

                    selectedTask.Name = dialog.TaskName;
                    selectedTask.Memo = dialog.Memo ?? "";
                    selectedTask.EstimatedTime = dialog.EstimatedTime;
                    selectedTask.ElapsedTime = dialog.ElapsedTime;
                    selectedTask.Priority = dialog.Priority;
                    SaveTasks();

                    InProgressList.Items.Refresh();
                    PendingList.Items.Refresh();
                    CompletedList.Items.Refresh();
                }
                else
                {
                    logger.LogTrace("タスク編集がキャンセルされました");
                }
            }
        }

        private TaskItem? GetSelectedTask()
        {
            return InProgressList.SelectedItem as TaskItem ??
                   PendingList.SelectedItem as TaskItem ??
                   CompletedList.SelectedItem as TaskItem;
        }

        private void UpdateCurrentTaskDisplay()
        {
            var currentTask = GetSelectedTask();
            CurrentTaskName.Text = currentTask != null 
                ? $"選択タスク: {currentTask.Name}"
                : "選択タスク: その他";
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskItem task)
            {
                switch (task.Status)
                {
                    case TaskStatus.InProgress:
                        inProgressTasks.Remove(task);
                        break;
                    case TaskStatus.Pending:
                        pendingTasks.Remove(task);
                        break;
                    case TaskStatus.Completed:
                        completedTasks.Remove(task);
                        break;
                }
                SaveTasks();
            }
        }

        private void CompleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskItem task)
            {
                if (isRunning && GetSelectedTask() == task)
                {
                    StopTimer();
                }

                switch (task.Status)
                {
                    case TaskStatus.InProgress:
                        inProgressTasks.Remove(task);
                        break;
                    case TaskStatus.Pending:
                        pendingTasks.Remove(task);
                        break;
                }

                task.Complete();
                completedTasks.Add(task);
                logger.LogTaskComplete(task);
                SaveTasks();
                UpdateTimerControls();
            }
        }

        private void SetPendingTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskItem task)
            {
                if (isRunning && GetSelectedTask() == task)
                {
                    StopTimer();
                }

                inProgressTasks.Remove(task);
                task.SetPending();
                pendingTasks.Add(task);
                logger.LogTaskStop(task, TimeSpan.Zero);
                SaveTasks();
                UpdateTimerControls();
            }
        }

        private void SetInProgressTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskItem task)
            {
                switch (task.Status)
                {
                    case TaskStatus.Pending:
                        pendingTasks.Remove(task);
                        break;
                    case TaskStatus.Completed:
                        completedTasks.Remove(task);
                        break;
                }

                task.SetInProgress();
                inProgressTasks.Add(task);
                SaveTasks();
                UpdateTimerControls();
            }
        }

        private void DeselectTask_Click(object sender, RoutedEventArgs e)
        {
            InProgressList.SelectedItem = null;
            PendingList.SelectedItem = null;
            CompletedList.SelectedItem = null;
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var wasRunning = isRunning;
            var previousTask = runningTask;
            DateTime? switchTime = null;

            if (wasRunning)
            {
                switchTime = DateTime.Now;
            }

            var inputWindow = new TaskInputWindow { Owner = this };

            if (inputWindow.ShowDialog() == true && inputWindow.CreatedTask != null)
            {
                inProgressTasks.Add(inputWindow.CreatedTask);
                SaveTasks();
            }

            if (wasRunning && switchTime.HasValue)
            {
                var timeSinceSwitch = DateTime.Now - switchTime.Value;
                if (timeSinceSwitch <= taskSwitchGracePeriod)
                {
                    // タイマーを再開
                    startTime = startTime.Add(timeSinceSwitch);
                    runningTask = previousTask;
                    if (runningTask != null)
                    {
                        runningTask.IsProcessing = true;
                    }
                }
                else
                {
                    StopTimer();
                }
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                CheckAndArchiveTasks();
            }
        }

        private void TopMostMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Topmost = TopMostMenuItem.IsChecked;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (isRunning)
            {
                StopTimer();
            }
            resetCheckTimer.Stop();
            inactiveCheckTimer.Stop();
            SaveTasks();
            CheckAndArchiveTasks();
        }

        private void SaveTasks_Click(object sender, RoutedEventArgs e)
        {
            SaveTasks();
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
                var allTasks = new
                {
                    InProgress = inProgressTasks,
                    Pending = pendingTasks,
                    Completed = completedTasks
                };

                var json = JsonSerializer.Serialize(allTasks);
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

        private void LoadTasks()
        {
            try
            {
                if (File.Exists(taskSaveFile))
                {
                    var json = File.ReadAllText(taskSaveFile);
                    var data = JsonSerializer.Deserialize<TaskData>(json);
                    if (data != null)
                    {
                        inProgressTasks.Clear();
                        pendingTasks.Clear();
                        completedTasks.Clear();

                        if (data.InProgress != null)
                            foreach (var task in data.InProgress)
                                inProgressTasks.Add(task);

                        if (data.Pending != null)
                            foreach (var task in data.Pending)
                                pendingTasks.Add(task);

                        if (data.Completed != null)
                            foreach (var task in data.Completed)
                                completedTasks.Add(task);
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

        private class TaskData
        {
            public TaskItem[]? InProgress { get; set; }
            public TaskItem[]? Pending { get; set; }
            public TaskItem[]? Completed { get; set; }
        }
    }
}

using System;
using System.Collections.ObjectModel;
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
        private static readonly TimeSpan InactiveDuration = TimeSpan.FromHours(72);
        private readonly ObservableCollection<TaskItem> inProgressTasks = new();
        private readonly ObservableCollection<TaskItem> pendingTasks = new();
        private readonly ObservableCollection<TaskItem> completedTasks = new();
        private readonly TaskLogger logger;
        private readonly TaskItem otherTask;
        private string? currentNotificationId = null;
        private readonly TimerState timerState = new();
        private TaskManagerService taskManager { get; }

        // タイマー状態管理
        private class TimerState
        {
            public DateTime StartTime { get; private set; }
            public TaskItem? ActiveTask { get; private set; }
            public bool IsRunning { get; private set; }

            public TimerState()
            {
                Reset();
            }

            public void Start(TaskItem? task)
            {
                StartTime = DateTime.Now;
                ActiveTask = task;
                IsRunning = true;
            }

            public void Stop()
            {
                if (IsRunning && ActiveTask != null)
                {
                    var elapsed = DateTime.Now - StartTime;
                    ActiveTask.AddElapsedTime(elapsed);
                    ActiveTask.IsProcessing = false;
                }
                Reset();
            }

            public void Reset()
            {
                StartTime = DateTime.Now;
                ActiveTask = null;
                IsRunning = false;
            }

            public TimeSpan GetDisplayTime(TaskItem? selectedTask)
            {
                if (!IsRunning)
                {
                    return selectedTask?.ElapsedTime ?? TimeSpan.Zero;
                }

                if (selectedTask == ActiveTask)
                {
                    return (selectedTask?.ElapsedTime ?? TimeSpan.Zero) + (DateTime.Now - StartTime);
                }

                return selectedTask?.ElapsedTime ?? TimeSpan.Zero;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            logger = new TaskLogger();
            otherTask = new TaskItem("その他", "", TimeSpan.Zero, TaskPriority.Medium);
            taskManager = new TaskManagerService(
                inProgressTasks,
                pendingTasks,
                completedTasks,
                logger
            );

            try
            {
                InitializeApplication();
            }
            catch (Exception ex)
            {
                logger.LogError("アプリケーションの初期化中にエラーが発生しました", ex);
                ShowErrorMessage("アプリケーションの起動に失敗しました", ex);
                Application.Current.Shutdown();
            }
        }

        private void InitializeApplication()
        {
            InitializeStopwatch();
            InitializeResetTimer();
            InitializeInactiveCheckTimer();
            InitializeTasks();
            LoadTasks();
        }

        private void SafeExecute(string operation, Action action)
        {
            try
            {
                action();
            }
            catch (TaskManagerException ex)
            {
                logger.LogError($"操作失敗: {operation}", ex);
                ShowErrorMessage(ex.Message, ex);
            }
            catch (Exception ex)
            {
                logger.LogError($"予期せぬエラー: {operation}", ex);
                ShowErrorMessage("予期せぬエラーが発生しました", ex);
            }
        }

        private void ShowErrorMessage(string message, Exception? ex = null)
        {
            var details = ex != null ? $"\n\n詳細: {ex.Message}" : "";
            MessageBox.Show(
                $"{message}{details}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
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

            // 既に実行中のタスクがあれば停止して保存
            if (timerState.IsRunning && timerState.ActiveTask != selectedTask)
            {
                var currentTask = timerState.ActiveTask;
                var elapsed = DateTime.Now - timerState.StartTime;
                
                // 実行中のタスクの時間を記録
                if (currentTask != null)
                {
                    currentTask.AddElapsedTime(elapsed);
                    currentTask.IsProcessing = false;
                    logger.LogTaskStop(currentTask, elapsed);
                }
                else
                {
                    // その他タスクとして記録
                    logger.LogOtherActivity(elapsed);
                }
                SaveTasks();
            }

            // 新しいタスクを開始
            timerState.Start(selectedTask);
            timer.Start();

            if (selectedTask != null)
            {
                selectedTask.IsProcessing = true;
                ScheduleNotification(selectedTask);
                logger.LogTaskStart(selectedTask);
            }
            else
            {
                logger.LogTaskStart(otherTask);
            }

            UpdateTimerControls();
        }

        private void StopTimer()
        {
            if (timerState.IsRunning)
            {
                timer.Stop();

                // 通知をキャンセル
                if (currentNotificationId != null)
                {
                    ToastNotificationManagerCompat.History.Remove(currentNotificationId);
                    currentNotificationId = null;
                }

                var runningTask = timerState.ActiveTask;
                var elapsed = DateTime.Now - timerState.StartTime;
                timerState.Stop();

                // タスク未選択（その他）の場合の処理
                if (runningTask == null)
                {
                    var otherTaskName = $"その他 ({DateTime.Now:MM/dd})";
                    var existingOtherTask = inProgressTasks.FirstOrDefault(t => t.Name == otherTaskName);

                    if (existingOtherTask == null)
                    {
                        // その他タスクが存在しない場合は新規作成
                        existingOtherTask = new TaskItem(otherTaskName, "自動作成", TimeSpan.Zero);
                        inProgressTasks.Add(existingOtherTask);
                    }

                    existingOtherTask.AddElapsedTime(elapsed);
                    logger.LogOtherActivity(elapsed);
                }
                else
                {
                    logger.LogTaskStop(runningTask, elapsed);
                }

                var selectedTask = GetSelectedTask();
                UpdateDisplayTime(selectedTask?.ElapsedTime ?? TimeSpan.Zero);
                UpdateTimerControls();
                
                SaveTasks();
            }
        }

        private void ScheduleNotification(TaskItem task)
        {
            // 既存の通知があれば削除
            if (currentNotificationId != null)
            {
                ToastNotificationManagerCompat.History.Remove(currentNotificationId);
                currentNotificationId = null;
            }

            var notificationId = Guid.NewGuid().ToString();
            currentNotificationId = notificationId;

            // 基本の通知
            var builder = new ToastContentBuilder()
                .AddText($"タスク: {task.Name}")
                .AddText("開始から30分が経過しました。")
                .SetToastScenario(ToastScenario.Default);

            // 予定時間を超過している場合は警告を追加
            if (task.ElapsedTime > task.EstimatedTime)
            {
                builder.AddText($"予定時間を{(task.ElapsedTime - task.EstimatedTime).TotalMinutes:0}分超過しています。");
            }

            // 通知を即時表示ではなくタイマーで管理
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(30)
            };

            timer.Tick += (s, e) =>
            {
                if (timerState.IsRunning && timerState.ActiveTask == task)
                {
                    builder.Show();
                    timer.Stop();
                }
                else
                {
                    timer.Stop();
                }
            };

            timer.Start();
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
                if (settings.AutoArchiveEnabled)
                {
                    ArchiveCompletedTasks(DateTime.Now.AddDays(-1));
                }
                settings.UpdateLastResetTime();
            }
        }

        private void ArchiveCompletedTasks(DateTime date)
        {
            try
            {
                var tasksToArchive = completedTasks
                    .Where(t => t.CompletedAt?.Date <= date.Date)
                    .ToList();

                if (tasksToArchive.Any())
                {
                    var archiveFile = Settings.GetArchiveFilePath(date);
                    var json = JsonSerializer.Serialize(tasksToArchive);
                    File.WriteAllText(archiveFile, json);

                    foreach (var task in tasksToArchive)
                    {
                        completedTasks.Remove(task);
                    }

                    SaveTasks();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"タスクのアーカイブ中にエラーが発生しました。\n{ex.Message}",
                              "エラー",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
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
            var selectedTask = GetSelectedTask();
            UpdateDisplayTime(timerState.GetDisplayTime(selectedTask));
        }

        private void UpdateDisplayTime(TimeSpan time = default)
        {
            StopwatchDisplay.Text = time.ToString(@"hh\:mm\:ss");
            StopwatchMilliseconds.Text = $".{(time.Milliseconds / 100)}";
        }

        private void UpdateTimerControls()
        {
            var selectedTask = GetSelectedTask();
            bool canStart = selectedTask == null || selectedTask.Status == TaskStatus.InProgress;
            
            if (selectedTask == timerState.ActiveTask && timerState.IsRunning)
            {
                StartStopButton.Content = "停止";
                StartStopButton.Style = FindResource("DangerButton") as Style;
            }
            else
            {
                StartStopButton.Content = "開始";
                StartStopButton.Style = FindResource("SuccessButton") as Style;
            }
            
            StartStopButton.IsEnabled = !timerState.IsRunning || canStart;
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (timerState.IsRunning)
            {
                StopTimer();
            }
            else
            {
                StartTimer();
            }
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

            var selectedTask = GetSelectedTask();
            UpdateDisplayTime(timerState.GetDisplayTime(selectedTask));
            UpdateCurrentTaskDisplay();
            UpdateTimerControls();
        }

        private void EditTask_Click(object sender, EventArgs e)
        {
            SafeExecute("タスク編集", () =>
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
            });
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
                if (timerState.IsRunning && GetSelectedTask() == task)
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

                task.SetCompleted(); // Changed from Complete() to SetCompleted()
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
                if (timerState.IsRunning && GetSelectedTask() == task)
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
            if (timerState.IsRunning)
            {
                StopTimer();
            }

            var inputWindow = new TaskInputWindow { Owner = this };

            if (inputWindow.ShowDialog() == true && inputWindow.CreatedTask != null)
            {
                inProgressTasks.Add(inputWindow.CreatedTask);
                SaveTasks();
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
            SafeExecute("アプリケーション終了処理", () =>
            {
                try
                {
                    // 通知をクリア
                    ToastNotificationManagerCompat.History.Clear();
                    if (currentNotificationId != null)
                    {
                        ToastNotificationManagerCompat.History.Remove(currentNotificationId);
                        currentNotificationId = null;
                    }

                    // アプリケーションと関連付けられた通知も完全にクリア
                    ToastNotificationManagerCompat.Uninstall();
                }
                catch (Exception ex)
                {
                    logger.LogError("通知のクリア中にエラーが発生", ex);
                }

                // 各種タイマーを停止
                resetCheckTimer.Stop();
                inactiveCheckTimer.Stop();

                // タスク状態を保存
                var result = taskManager.SaveTasks();
                if (!result.Success)
                {
                    logger.LogError("タスクの保存に失敗", result.Exception);
                    ShowErrorMessage("タスクの保存に失敗しました", result.Exception);
                }
                
                CheckAndArchiveTasks();
            });
        }

        private void SaveTasks_Click(object sender, RoutedEventArgs e)
        {
            var result = taskManager.SaveTasks();
            if (result.Success)
            {
                MessageBox.Show("タスクを保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ShowErrorMessage($"タスクの保存中にエラーが発生しました。\n{result.Message}", result.Exception);
            }
        }

        private void LoadTasks_Click(object sender, RoutedEventArgs e)
        {
            var result = taskManager.LoadTasks();
            if (result.Success)
            {
                MessageBox.Show("タスクを読み込みました。", "読み込み完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ShowErrorMessage($"タスクの読み込み中にエラーが発生しました。\n{result.Message}", result.Exception);
            }
        }

        private void SaveTasks()
        {
            var result = taskManager.SaveTasks();
            if (!result.Success)
            {
                logger.LogError("タスクの保存中にエラーが発生しました", result.Exception);
                ShowErrorMessage($"タスクの保存中にエラーが発生しました。\n{result.Message}", result.Exception);
            }
        }

        private void LoadTasks()
        {
            var result = taskManager.LoadTasks();
            if (!result.Success)
            {
                logger.LogError("タスクの読み込み中にエラーが発生しました", result.Exception);
                ShowErrorMessage($"タスクの読み込み中にエラーが発生しました。\n{result.Message}", result.Exception);
            }
        }
    }
}

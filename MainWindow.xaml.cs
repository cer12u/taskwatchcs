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
using TaskManager.Services;

namespace TaskManager
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer resetCheckTimer = new();
        private readonly DispatcherTimer inactiveCheckTimer = new();
        private readonly ObservableCollection<TaskItem> inProgressTasks = new();
        private readonly ObservableCollection<TaskItem> pendingTasks = new();
        private readonly ObservableCollection<TaskItem> completedTasks = new();
        private readonly TaskLogger logger;
        private TaskManagerService taskManager { get; }
        private readonly TimerService timerService;
        private readonly TaskService taskService;
        private readonly ArchiveService archiveService;
        private readonly InactiveTaskService inactiveTaskService;
        private readonly ExceptionHandlingService exceptionHandler;
        private readonly SettingsService settingsService;

        public MainWindow()
        {
            InitializeComponent();
            logger = new TaskLogger();
            exceptionHandler = new ExceptionHandlingService(logger);
            settingsService = new SettingsService(logger);
            taskManager = new TaskManagerService(
                inProgressTasks,
                pendingTasks,
                completedTasks,
                logger
            );
            timerService = new TimerService(logger);
            taskService = new TaskService(
                inProgressTasks, 
                pendingTasks, 
                completedTasks, 
                logger,
                taskManager);
            archiveService = new ArchiveService(
                completedTasks,
                logger,
                taskManager,
                settingsService);
            inactiveTaskService = new InactiveTaskService(
                inProgressTasks,
                pendingTasks,
                logger,
                taskManager,
                settingsService);
            timerService.TimerTick += TimerService_TimerTick;
            timerService.TimerStateChanged += TimerService_TimerStateChanged;

            try
            {
                InitializeApplication();
            }
            catch (Exception ex)
            {
                exceptionHandler.HandleException(
                    "アプリケーションの初期化",
                    ex,
                    "アプリケーションの起動に失敗しました");
                Application.Current.Shutdown();
            }
        }

        private void InitializeApplication()
        {
            InitializeResetTimer();
            InitializeInactiveCheckTimer();
            InitializeTasks();
            LoadTasks();
            UpdateTimerControls();
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

        private void TimerService_TimerTick(object? sender, TimeSpan displayTime)
        {
            UpdateDisplayTime(displayTime);
        }

        private void TimerService_TimerStateChanged(object? sender, EventArgs e)
        {
            UpdateTimerControls();
            UpdateCurrentTaskDisplay();
            var selectedTask = GetSelectedTask();
            UpdateDisplayTime(timerService.GetDisplayTime(selectedTask));
        }

        private void UpdateDisplayTime(TimeSpan time)
        {
            StopwatchDisplay.Text = time.ToString(@"hh\:mm\:ss");
            StopwatchMilliseconds.Text = $".{(time.Milliseconds / 100)}";
        }

        private void UpdateTimerControls()
        {
            var selectedTask = GetSelectedTask();
            bool canStart = selectedTask == null || selectedTask.Status == TaskStatus.InProgress;
            
            if (selectedTask == timerService.ActiveTask && timerService.IsRunning)
            {
                StartStopButton.Content = "停止";
                StartStopButton.Style = FindResource("DangerButton") as Style;
            }
            else
            {
                StartStopButton.Content = "開始";
                StartStopButton.Style = FindResource("SuccessButton") as Style;
            }
            
            StartStopButton.IsEnabled = !timerService.IsRunning || canStart;
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (timerService.IsRunning)
                {
                    timerService.Stop(inProgressTasks);
                    SaveTasks();
                }
                else
                {
                    timerService.Start(GetSelectedTask());
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                // タスクが切り替わった場合、タイマーを停止
                if (e.RemovedItems.Count > 0 && timerService.IsRunning)
                {
                    timerService.Stop(inProgressTasks);
                    SaveTasks();
                }
            }

            var selectedTask = GetSelectedTask();
            UpdateDisplayTime(timerService.GetDisplayTime(selectedTask));
            UpdateCurrentTaskDisplay();
            UpdateTimerControls();
        }

        private void EditTask_Click(object sender, EventArgs e)
        {
            exceptionHandler.SafeExecute("タスク編集", () =>
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
                taskService.DeleteTask(task);
            }
        }

        private void CompleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskItem task)
            {
                if (timerService.IsRunning && GetSelectedTask() == task)
                {
                    timerService.Stop(inProgressTasks);
                }

                taskService.CompleteTask(task);
                UpdateTimerControls();
            }
        }

        private void SetPendingTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskItem task)
            {
                if (timerService.IsRunning && GetSelectedTask() == task)
                {
                    timerService.Stop(inProgressTasks);
                }

                taskService.SetPendingTask(task);
                UpdateTimerControls();
            }
        }

        private void SetInProgressTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskItem task)
            {
                taskService.SetInProgressTask(task);
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
            if (timerService.IsRunning)
            {
                timerService.Stop(inProgressTasks);
            }

            var inputWindow = new TaskInputWindow { Owner = this };

            if (inputWindow.ShowDialog() == true && inputWindow.CreatedTask != null)
            {
                taskService.AddTask(inputWindow.CreatedTask);
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
                archiveService.CheckAndArchiveTasks();
            }
        }

        private void TopMostMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var settings = settingsService.GetSettings();
            settings.IsTopMost = TopMostMenuItem.IsChecked;
            settingsService.SaveSettings(settings);
            Topmost = TopMostMenuItem.IsChecked;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            exceptionHandler.SafeExecute("アプリケーション終了処理", () =>
            {
                try
                {
                    // 通知をクリア
                    ToastNotificationManagerCompat.History.Clear();
                    // アプリケーションと関連付けられた通知も完全にクリア
                    ToastNotificationManagerCompat.Uninstall();
                }
                catch (Exception ex)
                {
                    logger.LogError("通知のクリア中にエラーが発生", ex);
                }

                // タイマーを停止
                if (timerService.IsRunning)
                {
                    timerService.Stop(inProgressTasks);
                }
                resetCheckTimer.Stop();
                inactiveCheckTimer.Stop();

                // タスク状態を保存
                var result = taskManager.SaveTasks();
                if (!result.Success)
                {
                    throw new TaskManagerException(
                        "タスクの保存に失敗しました", 
                        result.Exception ?? new Exception(result.Message));
                }
                
                archiveService.CheckAndArchiveTasks();
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
                exceptionHandler.HandleException(
                    "タスクの保存",
                    new TaskManagerException("タスクの保存中にエラーが発生しました。", result.Exception ?? new Exception(result.Message)));
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
                exceptionHandler.HandleException(
                    "タスクの読み込み",
                    new TaskManagerException("タスクの読み込み中にエラーが発生しました。", result.Exception ?? new Exception(result.Message)));
            }
        }

        private void SaveTasks()
        {
            var result = taskManager.SaveTasks();
            if (!result.Success)
            {
                exceptionHandler.HandleException(
                    "タスクの保存",
                    new TaskManagerException("タスクの保存中にエラーが発生しました。", result.Exception ?? new Exception(result.Message)));
            }
        }

        private void LoadTasks()
        {
            var result = taskManager.LoadTasks();
            if (!result.Success)
            {
                exceptionHandler.HandleException(
                    "タスクの読み込み",
                    new TaskManagerException("タスクの読み込み中にエラーが発生しました。", result.Exception ?? new Exception(result.Message)));
            }

            // 設定の復元
            var settings = settingsService.GetSettings();
            TopMostMenuItem.IsChecked = settings.IsTopMost;
            Topmost = settings.IsTopMost;
        }

        private void InactiveCheckTimer_Tick(object? sender, EventArgs e)
        {
            inactiveTaskService.CheckInactiveTasks();
        }

        private void ResetCheckTimer_Tick(object? sender, EventArgs e)
        {
            archiveService.CheckAndArchiveTasks();
            inactiveTaskService.CheckInactiveTasks();
        }

        private void InitializeTasks()
        {
            InProgressList.ItemsSource = inProgressTasks;
            PendingList.ItemsSource = pendingTasks;
            CompletedList.ItemsSource = completedTasks;
        }
    }
}

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
        private readonly InteractionService interactionService;
        private readonly UIUpdateService uiUpdateService;
        private readonly DialogService dialogService;
        private readonly NotificationService notificationService;

        public MainWindow()
        {
            InitializeComponent();
            logger = new TaskLogger();
            exceptionHandler = new ExceptionHandlingService(logger);
            settingsService = new SettingsService(logger);
            interactionService = new InteractionService(exceptionHandler);
            uiUpdateService = new UIUpdateService(exceptionHandler);
            dialogService = new DialogService(exceptionHandler, logger);
            notificationService = new NotificationService(settingsService, logger);
            taskManager = new TaskManagerService(
                inProgressTasks,
                pendingTasks,
                completedTasks,
                logger
            );
            timerService = new TimerService(logger, notificationService);
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
            interactionService.HandlePreviewKeyDown(e);
        }

        private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            interactionService.HandlePreviewKeyDown(e);
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
            uiUpdateService.UpdateDisplayTime(StopwatchDisplay, StopwatchMilliseconds, time);
        }

        private void UpdateTimerControls()
        {
            var selectedTask = GetSelectedTask();
            uiUpdateService.UpdateTimerControls(
                StartStopButton,
                selectedTask,
                timerService.ActiveTask,
                timerService.IsRunning,
                Resources);
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
                interactionService.HandleListBoxMouseDown(listBox, e);
            }
        }

        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                interactionService.HandleListBoxPreviewMouseDown(listBox, e);
            }
        }

        private void TaskList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                interactionService.HandleListSelectionChanged(listBox, InProgressList, PendingList, CompletedList, e);

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
                    if (dialogService.ShowTaskEditDialog(this, selectedTask, out var result) && result != null)
                    {
                        selectedTask.Name = result.Name;
                        selectedTask.Memo = result.Memo;
                        selectedTask.EstimatedTime = result.EstimatedTime;
                        selectedTask.ElapsedTime = result.ElapsedTime;
                        selectedTask.Priority = result.Priority;
                        SaveTasks();

                        interactionService.RefreshLists(InProgressList, PendingList, CompletedList);
                    }
                }
            });
        }

        private TaskItem? GetSelectedTask()
        {
            return interactionService.GetSelectedTask(InProgressList, PendingList, CompletedList);
        }

        private void UpdateCurrentTaskDisplay()
        {
            var currentTask = GetSelectedTask();
            uiUpdateService.UpdateCurrentTaskDisplay(CurrentTaskName, currentTask);
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
            interactionService.DeselectAllTasks(InProgressList, PendingList, CompletedList);
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            if (timerService.IsRunning)
            {
                timerService.Stop(inProgressTasks);
            }

            if (dialogService.ShowTaskInputDialog(this, out var createdTask) && createdTask != null)
            {
                taskService.AddTask(createdTask);
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            if (dialogService.ShowSettingsDialog(this))
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
                notificationService.ClearAllNotifications();

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
                uiUpdateService.ShowSuccessMessage("保存完了", "タスクを保存しました。");
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
                uiUpdateService.ShowSuccessMessage("読み込み完了", "タスクを読み込みました。");
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
            uiUpdateService.InitializeListBoxSources(
                InProgressList,
                PendingList,
                CompletedList,
                inProgressTasks,
                pendingTasks,
                completedTasks);
        }
    }
}

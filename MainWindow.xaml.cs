using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TaskManager
{
    /// <summary>
    /// タスク管理アプリケーションのメインウィンドウ。
    /// タスクの一覧表示、時間管理、データの永続化を担当します。
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer timer = new();
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

        /// <summary>
        /// メインウィンドウのコンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            logger = new TaskLogger();
            otherTask = new TaskItem("その他", "選択されていないときの作業時間", TimeSpan.FromHours(24));

            InitializeStopwatch();
            InitializeTasks();
            LoadTasks();
        }

        /// <summary>
        /// ストップウォッチの初期化
        /// </summary>
        private void InitializeStopwatch()
        {
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            StopButton.IsEnabled = false;
            UpdateCurrentTaskDisplay();
            UpdateTimerControls();
        }

        /// <summary>
        /// タスクコレクションの初期化
        /// </summary>
        private void InitializeTasks()
        {
            InProgressList.ItemsSource = inProgressTasks;
            PendingList.ItemsSource = pendingTasks;
            CompletedList.ItemsSource = completedTasks;
        }

        /// <summary>
        /// タイマーのTick毎の処理
        /// 経過時間の表示を更新します
        /// </summary>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            TimeSpan currentElapsed = now - startTime;
            TimeSpan totalElapsed = baseElapsedTime + currentElapsed;
            
            // 表示を更新
            StopwatchDisplay.Text = totalElapsed.ToString(@"hh\:mm\:ss");
            StopwatchMilliseconds.Text = $".{(totalElapsed.Milliseconds / 100)}";
            
            lastTickTime = now;
        }

        /// <summary>
        /// タイマーコントロールの状態を更新
        /// </summary>
        private void UpdateTimerControls()
        {
            var selectedTask = GetSelectedTask();
            bool canStart = selectedTask == null || selectedTask.Status == TaskStatus.InProgress;
            
            StartButton.IsEnabled = !isRunning && canStart;
            StopButton.IsEnabled = isRunning;
        }

        /// <summary>
        /// 開始ボタンクリック時の処理
        /// </summary>
        private void StartButton_Click(object sender, RoutedEventArgs e)
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

            if (!isRunning)
            {
                startTime = DateTime.Now;
                lastTickTime = startTime;
                timer.Start();
                isRunning = true;
                UpdateTimerControls();

                var currentTask = selectedTask ?? otherTask;
                logger.LogTaskStart(currentTask);
            }
        }

        /// <summary>
        /// 停止ボタンクリック時の処理
        /// </summary>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                StopTimer();
            }
        }

        /// <summary>
        /// タイマーを停止し、経過時間を記録
        /// </summary>
        private void StopTimer()
        {
            if (isRunning)
            {
                timer.Stop();
                isRunning = false;
                UpdateTimerControls();

                var currentTask = GetSelectedTask() ?? otherTask;
                var duration = DateTime.Now - startTime;
                currentTask.AddElapsedTime(duration);
                baseElapsedTime = currentTask.ElapsedTime;
                logger.LogTaskStop(currentTask, duration);
                lastTickTime = null;
                SaveTasks();

                // 最終時間を表示
                StopwatchDisplay.Text = baseElapsedTime.ToString(@"hh\:mm\:ss");
                StopwatchMilliseconds.Text = $".{(baseElapsedTime.Milliseconds / 100)}";
            }
        }

        /// <summary>
        /// タスク選択変更時の処理
        /// </summary>
        private void TaskList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (isRunning)
            {
                StopTimer();
            }

            var currentTask = GetSelectedTask() ?? otherTask;
            baseElapsedTime = currentTask.ElapsedTime;
            StopwatchDisplay.Text = baseElapsedTime.ToString(@"hh\:mm\:ss");
            StopwatchMilliseconds.Text = $".{(baseElapsedTime.Milliseconds / 100)}";

            UpdateCurrentTaskDisplay();
            UpdateTimerControls();
        }

        /// <summary>
        /// 現在選択中のタスクを取得
        /// </summary>
        private TaskItem? GetSelectedTask()
        {
            return InProgressList.SelectedItem as TaskItem ??
                   PendingList.SelectedItem as TaskItem ??
                   CompletedList.SelectedItem as TaskItem;
        }

        /// <summary>
        /// 現在選択中のタスク名を表示更新
        /// </summary>
        private void UpdateCurrentTaskDisplay()
        {
            var currentTask = GetSelectedTask();
            CurrentTaskName.Text = currentTask != null 
                ? $"選択タスク: {currentTask.Name}"
                : "選択タスク: その他";
        }

        /// <summary>
        /// タスク削除ボタンクリック時の処理
        /// </summary>
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

        /// <summary>
        /// タスク完了ボタンクリック時の処理
        /// </summary>
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

        /// <summary>
        /// タスクを保留状態に変更
        /// </summary>
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

        /// <summary>
        /// タスクを進行中状態に変更
        /// </summary>
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

        /// <summary>
        /// タスク追加ボタンクリック時の処理
        /// </summary>
        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var inputWindow = new TaskInputWindow
            {
                Owner = this
            };

            if (inputWindow.ShowDialog() == true && inputWindow.CreatedTask != null)
            {
                inProgressTasks.Add(inputWindow.CreatedTask);
                SaveTasks();
            }
        }

        /// <summary>
        /// 最前面表示切り替え時の処理
        /// </summary>
        private void TopMostMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Topmost = TopMostMenuItem.IsChecked;
        }

        /// <summary>
        /// アプリケーション終了時の処理
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isRunning)
            {
                StopTimer();
            }
            SaveTasks();
        }

        /// <summary>
        /// タスク保存メニュー選択時の処理
        /// </summary>
        private void SaveTasks_Click(object sender, RoutedEventArgs e)
        {
            SaveTasks();
            MessageBox.Show("タスクを保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// タスク読み込みメニュー選択時の処理
        /// </summary>
        private void LoadTasks_Click(object sender, RoutedEventArgs e)
        {
            LoadTasks();
            MessageBox.Show("タスクを読み込みました。", "読み込み完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// タスクを保存
        /// </summary>
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

        /// <summary>
        /// タスクを読み込み
        /// </summary>
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

        /// <summary>
        /// JSON保存用のデータ構造
        /// </summary>
        private class TaskData
        {
            public TaskItem[]? InProgress { get; set; }
            public TaskItem[]? Pending { get; set; }
            public TaskItem[]? Completed { get; set; }
        }
    }
}
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
    /// <summary>
    /// タスク管理アプリケーションのメインウィンドウ。
    /// タスクの一覧表示、時間管理、データの永続化を担当します。
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;                 // ストップウォッチ用タイマー
        private DateTime startTime;                    // 計測開始時刻
        private bool isRunning = false;               // タイマー実行状態
        private ObservableCollection<TaskItem> tasks;  // アクティブなタスクのコレクション
        private ObservableCollection<TaskItem> completedTasks;  // 完了済みタスクのコレクション
        private readonly string taskSaveFile = "tasks.json";  // タスク保存ファイル
        private readonly string completedTaskSaveFile = "completed_tasks.json";  // 完了済みタスク保存ファイル
        private TaskLogger logger;                    // 作業ログ管理
        private TaskItem otherTask;                   // その他の作業用タスク
        private DateTime? lastTickTime;               // 前回のタイマー更新時刻
        private TimeSpan baseElapsedTime;            // タスクの累積時間

        /// <summary>
        /// メインウィンドウのコンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            InitializeStopwatch();
            InitializeTasks();
            LoadTasks();
            InitializeLogger();

            TaskList.MouseDoubleClick += TaskList_MouseDoubleClick;
            
            // その他の作業用タスクを作成
            otherTask = new TaskItem("その他", "選択されていないときの作業時間", TimeSpan.FromHours(24));
        }

        /// <summary>
        /// ログ管理の初期化
        /// </summary>
        private void InitializeLogger()
        {
            logger = new TaskLogger();
        }

        /// <summary>
        /// ストップウォッチの初期化
        /// </summary>
        private void InitializeStopwatch()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            StopButton.IsEnabled = false;
            UpdateCurrentTaskDisplay();
        }

        /// <summary>
        /// タスクコレクションの初期化
        /// </summary>
        private void InitializeTasks()
        {
            tasks = new ObservableCollection<TaskItem>();
            completedTasks = new ObservableCollection<TaskItem>();
            TaskList.ItemsSource = tasks;
        }

        /// <summary>
        /// タイマーのTick毎の処理
        /// 経過時間の表示を更新します
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
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
        /// 開始ボタンクリック時の処理
        /// </summary>
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
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;

                var currentTask = TaskList.SelectedItem as TaskItem ?? otherTask;
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
        private void TaskList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isRunning)
            {
                StopTimer();
            }

            var currentTask = TaskList.SelectedItem as TaskItem ?? otherTask;
            baseElapsedTime = currentTask.ElapsedTime;
            StopwatchDisplay.Text = baseElapsedTime.ToString(@"hh\:mm\:ss");
            StopwatchMilliseconds.Text = $".{(baseElapsedTime.Milliseconds / 100)}";

            UpdateCurrentTaskDisplay();
        }

        /// <summary>
        /// 現在選択中のタスク名を表示更新
        /// </summary>
        private void UpdateCurrentTaskDisplay()
        {
            var currentTask = TaskList.SelectedItem as TaskItem;
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
                tasks.Remove(task);
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
                if (isRunning && TaskList.SelectedItem == task)
                {
                    StopTimer();
                }

                task.Complete();
                tasks.Remove(task);
                completedTasks.Add(task);
                logger.LogTaskComplete(task);
                SaveTasks();
                SaveCompletedTasks();
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
                tasks.Add(inputWindow.CreatedTask);
                SaveTasks();
            }
        }

        /// <summary>
        /// タスクダブルクリック時の処理（編集）
        /// </summary>
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

        /// <summary>
        /// 最前面表示切り替え時の処理
        /// </summary>
        private void TopMostMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Topmost = TopMostMenuItem.IsChecked;
        }

        /// <summary>
        /// 完了済みタスク一覧表示
        /// </summary>
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
            SaveCompletedTasks();
        }

        /// <summary>
        /// タスク保存メニュー選択時の処理
        /// </summary>
        private void SaveTasks_Click(object sender, RoutedEventArgs e)
        {
            SaveTasks();
            SaveCompletedTasks();
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
        /// アクティブなタスクを保存
        /// </summary>
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

        /// <summary>
        /// 完了済みタスクを保存
        /// </summary>
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

        /// <summary>
        /// すべてのタスクを読み込み
        /// </summary>
        private void LoadTasks()
        {
            LoadActiveTasks();
            LoadCompletedTasks();
        }

        /// <summary>
        /// アクティブなタスクを読み込み
        /// </summary>
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

        /// <summary>
        /// 完了済みタスクを読み込み
        /// </summary>
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
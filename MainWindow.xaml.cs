﻿using System;
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
            timer.Interval = TimeSpan.FromMilliseconds(50); // Update more frequently for milliseconds
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
            TimeSpan elapsed = now - startTime;
            
            // Update display
            StopwatchDisplay.Text = elapsed.ToString(@"hh\:mm\:ss");
            StopwatchMilliseconds.Text = $".{elapsed.Milliseconds:D3}";

            // Add elapsed time to current task
            if (lastTickTime.HasValue)
            {
                var tickDuration = now - lastTickTime.Value;
                var currentTask = TaskList.SelectedItem as TaskItem ?? otherTask;
                currentTask.AddElapsedTime(tickDuration);
            }
            
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
                lastTickTime = null;

                var currentTask = TaskList.SelectedItem as TaskItem ?? otherTask;
                var duration = DateTime.Now - startTime;
                logger.LogTaskStop(currentTask, duration);
            }
        }

        private void TaskList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isRunning)
            {
                // Log stop for previous task
                var previousTask = e.RemovedItems.Count > 0 ? e.RemovedItems[0] as TaskItem : otherTask;
                var duration = DateTime.Now - startTime;
                logger.LogTaskStop(previousTask, duration);

                // Log start for new task
                var currentTask = TaskList.SelectedItem as TaskItem ?? otherTask;
                logger.LogTaskStart(currentTask);

                // Reset timer for new task
                startTime = DateTime.Now;
                lastTickTime = startTime;
            }

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
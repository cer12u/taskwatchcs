using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace TaskManager.Services
{
    public class UIUpdateService
    {
        private readonly ExceptionHandlingService exceptionHandler;

        public UIUpdateService(ExceptionHandlingService exceptionHandler)
        {
            this.exceptionHandler = exceptionHandler;
        }

        public void UpdateDisplayTime(TextBlock display, TextBlock milliseconds, TimeSpan time)
        {
            display.Text = time.ToString(@"hh\:mm\:ss");
            milliseconds.Text = $".{(time.Milliseconds / 100)}";
        }

        public void UpdateTimerControls(
            Button startStopButton, 
            TaskItem? selectedTask, 
            TaskItem? activeTask, 
            bool isTimerRunning,
            ResourceDictionary resources)
        {
            bool canStart = selectedTask == null || selectedTask.Status == TaskStatus.InProgress;
            
            if (selectedTask == activeTask && isTimerRunning)
            {
                startStopButton.Content = "停止";
                startStopButton.Style = resources["DangerButton"] as Style;
            }
            else
            {
                startStopButton.Content = "開始";
                startStopButton.Style = resources["SuccessButton"] as Style;
            }
            
            startStopButton.IsEnabled = !isTimerRunning || canStart;
        }

        public void UpdateCurrentTaskDisplay(TextBlock display, TaskItem? currentTask)
        {
            display.Text = currentTask != null 
                ? $"選択タスク: {currentTask.Name}"
                : "選択タスク: その他";
        }

        public void InitializeListBoxSources(
            ListBox inProgressList,
            ListBox pendingList,
            ListBox completedList,
            ObservableCollection<TaskItem> inProgressTasks,
            ObservableCollection<TaskItem> pendingTasks,
            ObservableCollection<TaskItem> completedTasks)
        {
            inProgressList.ItemsSource = inProgressTasks;
            pendingList.ItemsSource = pendingTasks;
            completedList.ItemsSource = completedTasks;
        }

        public void ShowSuccessMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
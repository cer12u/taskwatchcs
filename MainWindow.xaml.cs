using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace TaskManager
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private DateTime startTime;
        private bool isRunning = false;
        private ObservableCollection<TaskItem> tasks;
        private int taskCounter = 1;

        public MainWindow()
        {
            InitializeComponent();
            InitializeStopwatch();
            InitializeTasks();
        }

        private void InitializeStopwatch()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            StopButton.IsEnabled = false;
        }

        private void InitializeTasks()
        {
            tasks = new ObservableCollection<TaskItem>
            {
                new TaskItem("タスク1"),
                new TaskItem("タスク2"),
                new TaskItem("タスク3")
            };
            taskCounter = 4; // Start counter after initial tasks
            TaskList.ItemsSource = tasks;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - startTime;
            StopwatchDisplay.Text = elapsed.ToString(@"hh\:mm\:ss");
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
            {
                startTime = DateTime.Now;
                timer.Start();
                isRunning = true;
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                timer.Stop();
                isRunning = false;
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
            }
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskItem task)
            {
                tasks.Remove(task);
            }
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var newTask = new TaskItem($"タスク{taskCounter}");
            tasks.Add(newTask);
            taskCounter++;
        }
    }
}
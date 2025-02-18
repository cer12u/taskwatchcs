using System;
using System.Windows.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Collections.ObjectModel;
using System.Linq;

namespace TaskManager.Services
{
    public class TimerService
    {
        private readonly DispatcherTimer timer = new();
        private readonly TimerState timerState = new();
        private readonly TaskLogger logger;
        private readonly TaskItem otherTask;
        private readonly ExceptionHandlingService exceptionHandler;
        private readonly NotificationService notificationService;

        public event EventHandler<TimeSpan>? TimerTick;
        public event EventHandler? TimerStateChanged;

        public TimerService(TaskLogger logger, NotificationService notificationService)
        {
            this.logger = logger;
            this.notificationService = notificationService;
            this.exceptionHandler = new ExceptionHandlingService(logger);
            otherTask = new TaskItem("その他", "", TimeSpan.Zero, TaskPriority.Medium);
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            TimerTick?.Invoke(this, timerState.GetDisplayTime(timerState.ActiveTask));
        }

        public void Start(TaskItem? selectedTask)
        {
            exceptionHandler.SafeExecute("タイマーの開始", () =>
            {
                if (selectedTask != null && selectedTask.Status != TaskStatus.InProgress)
                {
                    throw new TaskManagerException("進行中のタスクのみ時間を記録できます。");
                }

                // 既に実行中のタスクがあれば停止
                if (timerState.IsRunning)
                {
                    Stop();
                }

                // 新しいタスクを開始
                timerState.Start(selectedTask);
                timer.Start();

                if (selectedTask != null)
                {
                    selectedTask.IsProcessing = true;
                    notificationService.ScheduleTaskNotification(selectedTask, true);
                    logger.LogTaskStart(selectedTask);
                }
                else
                {
                    logger.LogTaskStart(otherTask);
                }

                TimerStateChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        public void Stop()
        {
            exceptionHandler.SafeExecute("タイマーの停止", () =>
            {
                if (timerState.IsRunning)
                {
                    timer.Stop();
                    notificationService.ClearCurrentNotification();

                    var runningTask = timerState.ActiveTask;
                    var elapsed = DateTime.Now - timerState.StartTime;
                    timerState.Stop();

                    if (runningTask == null)
                    {
                        logger.LogOtherActivity(elapsed);
                    }
                    else
                    {
                        logger.LogTaskStop(runningTask, elapsed);
                    }

                    TimerStateChanged?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        public void Stop(ObservableCollection<TaskItem> inProgressTasks)
        {
            exceptionHandler.SafeExecute("タイマーの停止", () =>
            {
                if (timerState.IsRunning)
                {
                    timer.Stop();
                    notificationService.ClearCurrentNotification();

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

                    TimerStateChanged?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        public TimeSpan GetDisplayTime(TaskItem? selectedTask)
        {
            return timerState.GetDisplayTime(selectedTask);
        }

        public bool IsRunning => timerState.IsRunning;
        public TaskItem? ActiveTask => timerState.ActiveTask;

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
    }
}
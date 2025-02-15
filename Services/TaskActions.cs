using System;
using System.Windows;

namespace TaskManager
{
    public class TaskActions
    {
        private readonly TaskManagerService taskManager;
        private readonly TimerState timerState;
        private readonly TaskLogger logger;
        private readonly IDialogService dialogService;

        public TaskActions(
            TaskManagerService taskManager,
            TimerState timerState,
            TaskLogger logger,
            IDialogService dialogService)
        {
            this.taskManager = taskManager;
            this.timerState = timerState;
            this.logger = logger;
            this.dialogService = dialogService;
        }

        private void HandleTaskManagerError(TaskManagerResult result)
        {
            if (!result.Success)
            {
                dialogService.ShowError(result.Exception?.Message ?? result.Message);
            }
        }

        private TaskEditDialog ShowTaskEditDialog(TaskItem task)
        {
            var dialog = new TaskEditDialog(
                task.Name,
                task.Memo ?? "",
                task.EstimatedTime,
                task.ElapsedTime,
                task.Priority)
            {
                Owner = Application.Current.MainWindow
            };
            return dialog;
        }

        private void UpdateTaskFromDialog(TaskItem task, TaskEditDialog dialog)
        {
            try
            {
                logger.LogTrace($"タスク編集の保存: {dialog.TaskName}");
                logger.LogTrace($"変更後の値: EstimatedTime={dialog.EstimatedTime}, ElapsedTime={dialog.ElapsedTime}, Priority={dialog.Priority}");

                task.Name = dialog.TaskName ?? task.Name;
                task.Memo = dialog.Memo ?? "";
                task.EstimatedTime = dialog.EstimatedTime;
                task.ElapsedTime = dialog.ElapsedTime;
                task.Priority = dialog.Priority;

                var result = taskManager.SaveTasks();
                HandleTaskManagerError(result);
            }
            catch (Exception ex)
            {
                logger.LogTrace($"タスク編集中にエラーが発生しました: {ex.Message}");
                dialogService.ShowError("タスクの編集中にエラーが発生しました。\n" + ex.Message);
            }
        }

        public void EditTask(TaskItem? task)
        {
            if (task == null)
            {
                dialogService.ShowWarning("編集するタスクが選択されていません。");
                return;
            }

            try
            {
                logger.LogTrace($"タスク編集開始: {task.Name}");
                logger.LogTrace($"編集前の値: EstimatedTime={task.EstimatedTime}, ElapsedTime={task.ElapsedTime}, Priority={task.Priority}");

                var dialog = ShowTaskEditDialog(task);
                if (dialogService.ShowDialog(dialog) == true)
                {
                    UpdateTaskFromDialog(task, dialog);
                }
                else
                {
                    logger.LogTrace("タスク編集がキャンセルされました");
                }
            }
            catch (Exception ex)
            {
                logger.LogTrace($"タスク編集ダイアログでエラーが発生しました: {ex.Message}");
                dialogService.ShowError("タスク編集ダイアログの表示中にエラーが発生しました。\n" + ex.Message);
            }
        }

        private TaskInputWindow ShowTaskInputDialog()
        {
            return new TaskInputWindow
            {
                Owner = Application.Current.MainWindow
            };
        }

        public TaskItem? AddNewTask()
        {
            try
            {
                var inputWindow = ShowTaskInputDialog();
                if (dialogService.ShowDialog(inputWindow) == true)
                {
                    var newTask = inputWindow.CreatedTask;
                    if (newTask != null)
                    {
                        var result = taskManager.MoveTaskToState(newTask, TaskStatus.InProgress);
                        HandleTaskManagerError(result);
                        return result.Success ? newTask : null;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.LogTrace($"新規タスク作成中にエラーが発生しました: {ex.Message}");
                dialogService.ShowError("新規タスクの作成中にエラーが発生しました。\n" + ex.Message);
                return null;
            }
        }

        public void ChangeTaskState(TaskItem? task, TaskStatus newStatus)
        {
            if (task == null)
            {
                dialogService.ShowWarning("状態を変更するタスクが選択されていません。");
                return;
            }

            try
            {
                if (timerState.IsRunning && timerState.ActiveTask == task)
                {
                    timerState.Stop();
                }

                var result = taskManager.MoveTaskToState(task, newStatus);
                HandleTaskManagerError(result);
            }
            catch (Exception ex)
            {
                logger.LogTrace($"タスク状態の変更中にエラーが発生しました: {ex.Message}");
                dialogService.ShowError("タスク状態の変更中にエラーが発生しました。\n" + ex.Message);
            }
        }

        public void DeleteTask(TaskItem? task)
        {
            if (task == null)
            {
                dialogService.ShowWarning("削除するタスクが選択されていません。");
                return;
            }

            try
            {
                if (timerState.IsRunning && timerState.ActiveTask == task)
                {
                    timerState.Stop();
                }

                var result = taskManager.RemoveTaskFromCurrentCollection(task);
                if (result.Success)
                {
                    var saveResult = taskManager.SaveTasks();
                    HandleTaskManagerError(saveResult);
                }
                else
                {
                    HandleTaskManagerError(result);
                }
            }
            catch (Exception ex)
            {
                logger.LogTrace($"タスクの削除中にエラーが発生しました: {ex.Message}");
                dialogService.ShowError("タスクの削除中にエラーが発生しました。\n" + ex.Message);
            }
        }

        public void CompleteTask(TaskItem? task) => ChangeTaskState(task, TaskStatus.Completed);
        public void SetPendingTask(TaskItem? task) => ChangeTaskState(task, TaskStatus.Pending);
        public void SetInProgressTask(TaskItem? task) => ChangeTaskState(task, TaskStatus.InProgress);
    }
}
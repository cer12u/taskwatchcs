using System;
using System.IO;
using System.Text;

namespace TaskManager
{
    /// <summary>
    /// タスクの作業ログを管理するクラス。
    /// 日付ごとにログファイルを作成し、タスクの開始、停止、完了を記録します。
    /// </summary>
    public class TaskLogger
    {
        private readonly string logFile;
        private static readonly object lockObj = new object();

        /// <summary>
        /// TaskLoggerのコンストラクタ
        /// </summary>
        /// <param name="logDirectory">ログファイルを保存するディレクトリ</param>
        public TaskLogger(string logDirectory = "logs")
        {
            // ログディレクトリが存在しない場合は作成
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // 日付ごとのログファイル名を生成
            string timestamp = DateTime.Now.ToString("yyyyMMdd");
            logFile = Path.Combine(logDirectory, $"tasklog_{timestamp}.txt");
        }

        /// <summary>
        /// タスク開始時のログを記録
        /// </summary>
        /// <param name="task">開始するタスク</param>
        public void LogTaskStart(TaskItem task)
        {
            LogActivity($"タスク開始: {task.Name}");
        }

        /// <summary>
        /// タスク停止時のログを記録
        /// </summary>
        /// <param name="task">停止するタスク</param>
        /// <param name="duration">作業時間</param>
        public void LogTaskStop(TaskItem task, TimeSpan duration)
        {
            LogActivity($"タスク停止: {task.Name}, 経過時間: {FormatTimeSpan(duration)}");
        }

        /// <summary>
        /// タスク完了時のログを記録
        /// </summary>
        /// <param name="task">完了したタスク</param>
        public void LogTaskComplete(TaskItem task)
        {
            LogActivity($"タスク完了: {task.Name}, 合計時間: {FormatTimeSpan(task.ElapsedTime)}");
        }

        /// <summary>
        /// その他の作業時間のログを記録
        /// </summary>
        /// <param name="duration">作業時間</param>
        public void LogOtherActivity(TimeSpan duration)
        {
            LogActivity($"その他の作業: {duration:hh\\:mm\\:ss\\.fff}");
        }

        /// <summary>
        /// ログファイルにアクティビティを記録
        /// スレッドセーフな実装のためlockを使用
        /// </summary>
        /// <param name="message">記録するメッセージ</param>
        private void LogActivity(string message, bool isTrace = false)
        {
            try
            {
                lock (lockObj)
                {
                    var prefix = isTrace ? "[TRACE]" : "[INFO]";
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {prefix} - {message}";
                    File.AppendAllText(logFile, logEntry + Environment.NewLine, Encoding.UTF8);
                    System.Diagnostics.Debug.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ログ書き込みエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// デバッグ用のトレースログを記録
        /// </summary>
        public void LogTrace(string message)
        {
            LogActivity(message, true);
        }

        /// <summary>
        /// タスク編集時のデータ変更をログに記録
        /// </summary>
        public void LogTaskEdit(TaskItem task, string fieldName, string oldValue, string newValue)
        {
            LogTrace($"タスク編集: {task.Name}, フィールド: {fieldName}, 変更前: {oldValue}, 変更後: {newValue}");
        }

        /// <summary>
        /// 入力値の検証結果をログに記録
        /// </summary>
        public void LogValidation(string context, bool isValid, string message)
        {
            LogTrace($"入力検証 [{context}] - {(isValid ? "成功" : "失敗")}: {message}");
        }

        public void LogTaskError(string operation, TaskItem? task, Exception? ex = null)
        {
            var taskName = task?.Name ?? "不明なタスク";
            var errorMessage = ex?.Message ?? "詳細不明";
            LogActivity($"タスク操作エラー - 操作: {operation}, タスク: {taskName}, エラー: {errorMessage}", true);
        }

        public void LogTaskStateChange(TaskItem task, TaskStatus oldState, TaskStatus newState)
        {
            LogActivity($"タスク状態変更: {task.Name}, {oldState} → {newState}");
        }

        public void LogTimerAction(string action, TaskItem? task)
        {
            var taskName = task?.Name ?? "その他の作業";
            LogActivity($"タイマー {action}: {taskName}");
        }

        public void LogTaskOperation(string operation, TaskItem task, string details = "")
        {
            var logMessage = $"タスク {operation}: {task.Name}";
            if (!string.IsNullOrEmpty(details))
            {
                logMessage += $", {details}";
            }
            LogActivity(logMessage);
        }

        private string FormatTimeSpan(TimeSpan time)
        {
            return time.ToString(@"hh\:mm\:ss\.fff");
        }
    }
}
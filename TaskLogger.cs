using System;
using System.IO;
using System.Text;

namespace TaskManager
{
    public enum LogLevel
    {
        Trace,
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// タスクの作業ログを管理するクラス。
    /// 日付ごとにログファイルを作成し、タスクの開始、停止、完了を記録します。
    /// </summary>
    public class TaskLogger
    {
        private readonly string logFile;
        private static readonly object lockObj = new object();
        private readonly string logDirectory;

        /// <summary>
        /// TaskLoggerのコンストラクタ
        /// </summary>
        /// <param name="logDirectory">ログファイルを保存するディレクトリ</param>
        public TaskLogger(string logDirectory = "logs")
        {
            this.logDirectory = logDirectory;
            try
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
            catch (Exception ex)
            {
                // 致命的なエラーの場合はデスクトップにフォールバック
                string fallbackPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "taskmanager_error.log"
                );
                logFile = fallbackPath;
                LogError("Logger initialization failed", ex);
            }
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
            LogActivity($"その他: {duration:hh\\:mm\\:ss\\.fff}");
        }

        /// <summary>
        /// ログファイルにアクティビティを記録
        /// スレッドセーフな実装のためlockを使用
        /// </summary>
        /// <param name="message">記録するメッセージ</param>
        public void LogActivity(string message, LogLevel level = LogLevel.Info)
        {
            try
            {
                lock (lockObj)
                {
                    var prefix = GetLogLevelPrefix(level);
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {prefix} - {message}";
                    File.AppendAllText(logFile, logEntry + Environment.NewLine, Encoding.UTF8);
                    System.Diagnostics.Debug.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                // ログ書き込みに失敗した場合、フォールバックとしてデバッグ出力を使用
                System.Diagnostics.Debug.WriteLine($"Failed to write log: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Original message: {message}");
            }
        }

        private string GetLogLevelPrefix(LogLevel level) => level switch
        {
            LogLevel.Trace => "[TRACE]",
            LogLevel.Info => "[INFO]",
            LogLevel.Warning => "[WARN]",
            LogLevel.Error => "[ERROR]",
            _ => "[INFO]"
        };

        /// <summary>
        /// デバッグ用のトレースログを記録
        /// </summary>
        public void LogTrace(string message) => LogActivity(message, LogLevel.Trace);

        /// <summary>
        /// 情報ログを記録
        /// </summary>
        public void LogInfo(string message) => LogActivity(message, LogLevel.Info);

        /// <summary>
        /// 警告ログを記録
        /// </summary>
        public void LogWarning(string message) => LogActivity(message, LogLevel.Warning);

        /// <summary>
        /// エラーログを記録
        /// </summary>
        public void LogError(string message, Exception? ex = null)
        {
            var errorMessage = ex != null
                ? $"{message}\nException: {ex.GetType().Name}\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}"
                : message;
            LogActivity(errorMessage, LogLevel.Error);
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
            var errorMessage = $"タスク操作エラー - 操作: {operation}, タスク: {taskName}";
            LogError(errorMessage, ex);
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

        public void LogExceptionWithContext(string context, Exception ex)
        {
            var errorMessage = $"コンテキスト: {context}\n" +
                             $"例外タイプ: {ex.GetType().Name}\n" +
                             $"メッセージ: {ex.Message}\n" +
                             $"スタックトレース: {ex.StackTrace}";
            LogError(errorMessage);
        }

        private string FormatTimeSpan(TimeSpan time)
        {
            return time.ToString(@"hh\:mm\:ss\.fff");
        }
    }
}
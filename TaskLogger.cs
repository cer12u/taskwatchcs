using System;
using System.IO;
using System.Text;

namespace TaskManager
{
    public class TaskLogger
    {
        private readonly string logFile;
        private static readonly object lockObj = new object();

        public TaskLogger(string logDirectory = "logs")
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd");
            logFile = Path.Combine(logDirectory, $"tasklog_{timestamp}.txt");
        }

        public void LogTaskStart(TaskItem task)
        {
            LogActivity($"タスク開始: {task.Name}");
        }

        public void LogTaskStop(TaskItem task, TimeSpan duration)
        {
            LogActivity($"タスク停止: {task.Name}, 経過時間: {duration:hh\\:mm\\:ss\\.fff}");
        }

        public void LogTaskComplete(TaskItem task)
        {
            LogActivity($"タスク完了: {task.Name}, 合計時間: {task.ElapsedTime:hh\\:mm\\:ss\\.fff}");
        }

        public void LogOtherActivity(TimeSpan duration)
        {
            LogActivity($"その他の作業: {duration:hh\\:mm\\:ss\\.fff}");
        }

        private void LogActivity(string message)
        {
            try
            {
                lock (lockObj)
                {
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
                    File.AppendAllText(logFile, logEntry + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                // ログ書き込みエラーは無視（アプリケーションの動作に影響を与えない）
                System.Diagnostics.Debug.WriteLine($"ログ書き込みエラー: {ex.Message}");
            }
        }
    }
}
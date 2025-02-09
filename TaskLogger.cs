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
            LogActivity($"タスク停止: {task.Name}, 経過時間: {duration:hh\\:mm\\:ss\\.fff}");
        }

        /// <summary>
        /// タスク完了時のログを記録
        /// </summary>
        /// <param name="task">完了したタスク</param>
        public void LogTaskComplete(TaskItem task)
        {
            LogActivity($"タスク完了: {task.Name}, 合計時間: {task.ElapsedTime:hh\\:mm\\:ss\\.fff}");
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
        private void LogActivity(string message)
        {
            try
            {
                lock (lockObj)
                {
                    // タイムスタンプ付きでログを記録
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
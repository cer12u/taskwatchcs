using System;
using System.Collections.Generic;

namespace TaskManager.Services
{
    /// <summary>
    /// タスクのバリデーションを担当するインターフェース
    /// </summary>
    public interface ITaskValidator
    {
        /// <summary>
        /// タスクの検証を実行
        /// </summary>
        ValidationResult Validate(TaskItem task);

        /// <summary>
        /// カスタムバリデーションルールの追加
        /// </summary>
        void AddValidationRule(ITaskValidationRule rule);
    }

    /// <summary>
    /// カスタムバリデーションルールのインターフェース
    /// </summary>
    public interface ITaskValidationRule
    {
        /// <summary>
        /// バリデーションを実行
        /// </summary>
        ValidationError? Validate(TaskItem task);
    }

    /// <summary>
    /// バリデーション結果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<ValidationError> Errors { get; } = new();

        public void AddError(string propertyName, string message)
        {
            Errors.Add(new ValidationError(propertyName, message));
        }

        public static ValidationResult Success() => new();

        public static ValidationResult Failed(string propertyName, string message)
        {
            var result = new ValidationResult();
            result.AddError(propertyName, message);
            return result;
        }
    }

    /// <summary>
    /// バリデーションエラー
    /// </summary>
    public class ValidationError
    {
        public string PropertyName { get; }
        public string Message { get; }

        public ValidationError(string propertyName, string message)
        {
            PropertyName = propertyName;
            Message = message;
        }
    }

    /// <summary>
    /// 標準バリデーションルール
    /// </summary>
    public static class StandardValidationRules
    {
        public static ITaskValidationRule TaskNameRequired => new TaskNameRequiredRule();
        public static ITaskValidationRule TaskNameLength => new TaskNameLengthRule(100);
        public static ITaskValidationRule MemoLength => new MemoLengthRule(1000);
        public static ITaskValidationRule EstimatedTimeRange => new EstimatedTimeRangeRule(TimeSpan.FromHours(24));
    }

    // 具体的なバリデーションルールの実装例
    internal class TaskNameRequiredRule : ITaskValidationRule
    {
        public ValidationError? Validate(TaskItem task)
        {
            return string.IsNullOrWhiteSpace(task.Name)
                ? new ValidationError(nameof(TaskItem.Name), "タスク名は必須です")
                : null;
        }
    }

    internal class TaskNameLengthRule : ITaskValidationRule
    {
        private readonly int maxLength;

        public TaskNameLengthRule(int maxLength)
        {
            this.maxLength = maxLength;
        }

        public ValidationError? Validate(TaskItem task)
        {
            return task.Name?.Length > maxLength
                ? new ValidationError(nameof(TaskItem.Name), $"タスク名は{maxLength}文字以内で入力してください")
                : null;
        }
    }

    internal class MemoLengthRule : ITaskValidationRule
    {
        private readonly int maxLength;

        public MemoLengthRule(int maxLength)
        {
            this.maxLength = maxLength;
        }

        public ValidationError? Validate(TaskItem task)
        {
            return task.Memo?.Length > maxLength
                ? new ValidationError(nameof(TaskItem.Memo), $"メモは{maxLength}文字以内で入力してください")
                : null;
        }
    }

    internal class EstimatedTimeRangeRule : ITaskValidationRule
    {
        private readonly TimeSpan maxTime;

        public EstimatedTimeRangeRule(TimeSpan maxTime)
        {
            this.maxTime = maxTime;
        }

        public ValidationError? Validate(TaskItem task)
        {
            return task.EstimatedTime > maxTime
                ? new ValidationError(nameof(TaskItem.EstimatedTime), $"予定時間は{maxTime.TotalHours}時間以内で設定してください")
                : null;
        }
    }
}
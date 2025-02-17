using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskManager.Services
{
    public class TaskValidationService : ITaskValidator
    {
        private readonly List<ITaskValidationRule> rules = new();
        private readonly TaskLogger logger;

        public TaskValidationService(TaskLogger logger)
        {
            this.logger = logger;
            InitializeDefaultRules();
        }

        public ValidationResult Validate(TaskItem task)
        {
            if (task == null)
            {
                return ValidationResult.Failed("Task", "タスクが指定されていません");
            }

            var result = new ValidationResult();
            foreach (var rule in rules)
            {
                var error = rule.Validate(task);
                if (error != null)
                {
                    result.AddError(error.PropertyName, error.Message);
                }
            }

            LogValidationResult(task, result);
            return result;
        }

        public void AddValidationRule(ITaskValidationRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            rules.Add(rule);
            logger.LogTrace($"バリデーションルールを追加: {rule.GetType().Name}");
        }

        private void InitializeDefaultRules()
        {
            rules.Add(StandardValidationRules.TaskNameRequired);
            rules.Add(StandardValidationRules.TaskNameLength);
            rules.Add(StandardValidationRules.MemoLength);
            rules.Add(StandardValidationRules.EstimatedTimeRange);
            logger.LogTrace("デフォルトのバリデーションルールを初期化しました");
        }

        private void LogValidationResult(TaskItem task, ValidationResult result)
        {
            if (result.IsValid)
            {
                logger.LogTrace($"タスクのバリデーションが成功: {task.Name}");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => $"{e.PropertyName}: {e.Message}"));
                logger.LogTrace($"タスクのバリデーションが失敗: {task.Name}, エラー: {errors}");
            }
        }
    }
}
using System;
using System.Collections.Generic;

namespace TaskManager.Services
{
    public class TaskStateTransition
    {
        private readonly Dictionary<TaskStatus, HashSet<TaskStatus>> _allowedTransitions;

        public TaskStateTransition()
        {
            _allowedTransitions = new Dictionary<TaskStatus, HashSet<TaskStatus>>
            {
                [TaskStatus.InProgress] = new HashSet<TaskStatus> { TaskStatus.Pending, TaskStatus.Completed },
                [TaskStatus.Pending] = new HashSet<TaskStatus> { TaskStatus.InProgress, TaskStatus.Completed },
                [TaskStatus.Completed] = new HashSet<TaskStatus> { TaskStatus.InProgress, TaskStatus.Pending }
            };
        }

        public bool IsTransitionAllowed(TaskStatus from, TaskStatus to)
        {
            return _allowedTransitions.TryGetValue(from, out var allowedStates) && 
                   allowedStates.Contains(to);
        }

        public string GetTransitionErrorMessage(TaskStatus from, TaskStatus to)
        {
            return $"遷移が許可されていません: {from} -> {to}";
        }
    }
}

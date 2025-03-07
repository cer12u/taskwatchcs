using System;

namespace TaskManager.Services
{
    public class TaskManagerException : Exception
    {
        public TaskManagerException(string message) : base(message)
        {
        }

        public TaskManagerException(string message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
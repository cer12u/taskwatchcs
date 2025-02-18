using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TaskManager.Services
{
    public class InteractionService
    {
        private readonly ExceptionHandlingService exceptionHandler;

        public InteractionService(ExceptionHandlingService exceptionHandler)
        {
            this.exceptionHandler = exceptionHandler;
        }

        public void HandlePreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
            }
        }

        public void HandleListBoxMouseDown(ListBox listBox, MouseButtonEventArgs e)
        {
            var item = listBox.InputHitTest(e.GetPosition(listBox));
            if (item == listBox)
            {
                listBox.SelectedItem = null;
                e.Handled = true;
            }
        }

        public void HandleListBoxPreviewMouseDown(ListBox listBox, MouseButtonEventArgs e)
        {
            var item = listBox.InputHitTest(e.GetPosition(listBox));
            if (item == listBox)
            {
                e.Handled = true;
            }
        }

        public void HandleListSelectionChanged(
            ListBox changedList,
            ListBox inProgressList,
            ListBox pendingList,
            ListBox completedList,
            SelectionChangedEventArgs e)
        {
            if (changedList != inProgressList) inProgressList.SelectedItem = null;
            if (changedList != pendingList) pendingList.SelectedItem = null;
            if (changedList != completedList) completedList.SelectedItem = null;

            if (e.AddedItems.Count == 0)
            {
                changedList.SelectedItem = null;
            }
        }

        public void DeselectAllTasks(ListBox inProgressList, ListBox pendingList, ListBox completedList)
        {
            inProgressList.SelectedItem = null;
            pendingList.SelectedItem = null;
            completedList.SelectedItem = null;
        }

        public TaskItem? GetSelectedTask(ListBox inProgressList, ListBox pendingList, ListBox completedList)
        {
            return inProgressList.SelectedItem as TaskItem ??
                   pendingList.SelectedItem as TaskItem ??
                   completedList.SelectedItem as TaskItem;
        }

        public void RefreshLists(ListBox inProgressList, ListBox pendingList, ListBox completedList)
        {
            if (inProgressList.Items is ItemCollection inProgressItems)
            {
                inProgressItems.Refresh();
            }
            if (pendingList.Items is ItemCollection pendingItems)
            {
                pendingItems.Refresh();
            }
            if (completedList.Items is ItemCollection completedItems)
            {
                completedItems.Refresh();
            }
        }
    }
}
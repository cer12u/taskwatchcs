using System;
using System.Windows;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager
{
    /// <summary>
    /// 設定ダイアログのインタラクションロジック
    /// </summary>
    public partial class SettingsDialog : Window
    {
        private readonly SettingsService settingsService;
        private readonly Settings settings;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SettingsDialog(SettingsService settingsService)
        {
            InitializeComponent();
            this.settingsService = settingsService;
            this.settings = settingsService.GetSettings();
            
            // 設定値をUIに反映
            InactiveTasksEnabledCheckBox.IsChecked = settings.InactiveTasksEnabled;
            AutoArchiveEnabledCheckBox.IsChecked = settings.AutoArchiveEnabled;
            NotificationsEnabledCheckBox.IsChecked = settings.NotificationsEnabled;
            EstimatedTimeNotificationCheckBox.IsChecked = settings.EstimatedTimeNotificationEnabled;

            // 通知間隔の初期化
            InitializeNotificationInterval();

            // リセット時刻の初期化
            InitializeResetTime();
        }

        private void InitializeNotificationInterval()
        {
            NotificationIntervalComboBox.Items.Clear();
            for (int i = 5; i <= 60; i += 5)
            {
                NotificationIntervalComboBox.Items.Add(i);
            }
            NotificationIntervalComboBox.SelectedItem = settings.NotificationInterval;
        }

        private void InitializeResetTime()
        {
            HoursComboBox.Items.Clear();
            MinutesComboBox.Items.Clear();

            for (int i = 0; i < 24; i++)
            {
                HoursComboBox.Items.Add(i);
            }
            for (int i = 0; i < 60; i += 5)
            {
                MinutesComboBox.Items.Add(i);
            }

            HoursComboBox.SelectedItem = settings.ResetTime.Hours;
            MinutesComboBox.SelectedItem = settings.ResetTime.Minutes;
        }

        /// <summary>
        /// OKボタンクリック時の処理
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (NotificationIntervalComboBox.SelectedItem is int interval && interval > 0)
            {
                settings.NotificationInterval = interval;
            }
            else
            {
                MessageBox.Show("通知間隔を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (HoursComboBox.SelectedItem is int hours && MinutesComboBox.SelectedItem is int minutes)
            {
                settings.ResetTime = new TimeSpan(hours, minutes, 0);
            }
            else
            {
                MessageBox.Show("リセット時刻を選択してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            settings.InactiveTasksEnabled = InactiveTasksEnabledCheckBox.IsChecked ?? true;
            settings.AutoArchiveEnabled = AutoArchiveEnabledCheckBox.IsChecked ?? true;
            settings.NotificationsEnabled = NotificationsEnabledCheckBox.IsChecked ?? true;
            settings.EstimatedTimeNotificationEnabled = EstimatedTimeNotificationCheckBox.IsChecked ?? true;
            
            settingsService.SaveSettings(settings);
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// キャンセルボタンクリック時の処理
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
using System;
using System.Windows;

namespace TaskManager
{
    /// <summary>
    /// 設定ダイアログのインタラクションロジック
    /// </summary>
    public partial class SettingsDialog : Window
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SettingsDialog()
        {
            InitializeComponent();
            InitializeTimeComboBoxes();
            LoadCurrentSettings();
        }

        /// <summary>
        /// 時間選択コンボボックスの初期化
        /// </summary>
        private void InitializeTimeComboBoxes()
        {
            // 時間の選択肢を設定（0-23時間）
            for (int i = 0; i <= 23; i++)
            {
                HoursComboBox.Items.Add(i);
            }

            // 分の選択肢を設定（0-55分、5分刻み）
            for (int i = 0; i <= 55; i += 5)
            {
                MinutesComboBox.Items.Add(i);
            }
        }

        /// <summary>
        /// 現在の設定を読み込み
        /// </summary>
        private void LoadCurrentSettings()
        {
            var settings = Settings.Instance;
            HoursComboBox.SelectedItem = settings.ResetTime.Hours;
            MinutesComboBox.SelectedItem = settings.ResetTime.Minutes - (settings.ResetTime.Minutes % 5);
        }

        /// <summary>
        /// OKボタンクリック時の処理
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (HoursComboBox.SelectedItem == null || MinutesComboBox.SelectedItem == null)
            {
                MessageBox.Show("時刻を選択してください。", 
                              "エラー", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return;
            }

            var hours = (int)HoursComboBox.SelectedItem;
            var minutes = (int)MinutesComboBox.SelectedItem;

            var settings = Settings.Instance;
            settings.ResetTime = new TimeSpan(hours, minutes, 0);
            settings.Save();

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
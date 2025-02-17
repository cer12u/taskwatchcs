# TaskManager リファクタリング作業ドキュメント

## 実施済みの改善内容

### 1. エラーハンドリングの統一
- TaskManagerServiceに一元化された例外処理の実装
- エラー発生時のログ記録と通知の標準化
- 業務例外（TaskManagerException）の導入

### 2. タスクデータ管理の改善
- TaskManagerServiceにデータ操作の責務を集約
- SaveTasks/LoadTasksの実装を移管
- タスク状態変更の一貫性を確保

### 3. その他タスク機能の実装
- タスク未選択時の自動記録
- 日付ベースの「その他」タスク自動生成
  - 命名規則：「その他 (MM/dd)」
  - 自動作成されたタスクは進行中リストに追加

### 4. ログ機能の強化
- LogLevel導入（Trace, Info, Warning, Error）
- スレッドセーフな実装（lockによる排他制御）
- 詳細なコンテキスト情報の記録
- エラー発生時のフォールバック機能

## 今後の改善検討事項

1. パフォーマンス最適化
   - コレクション操作の効率化
   - 大量データ時のメモリ使用量検証

2. UIの改善可能性
   - ユーザー操作のフィードバック強化
   - エラーメッセージの表示方法の統一

3. データ永続化
   - バックアップ機能の追加検討
   - データ復旧機能の実装

4. テスト強化
   - 単体テストの追加
   - 異常系テストケースの拡充

## 開発ガイドライン

### 例外処理
```csharp
// 推奨パターン
public TaskManagerResult SomeOperation()
{
    return ExecuteOperation("操作名", () =>
    {
        try
        {
            // 処理内容
            return TaskManagerResult.Succeeded("成功メッセージ");
        }
        catch (Exception ex)
        {
            logger.LogError("エラーメッセージ", ex);
            throw new TaskManagerException("ユーザー向けメッセージ", ex);
        }
    });
}
```

### ログ記録
```csharp
// 操作開始・終了
logger.LogInfo("操作開始: {操作名}");

// デバッグ情報
logger.LogTrace($"詳細情報: {details}");

// エラー記録
logger.LogError("エラーの説明", exception);
```

### UI操作
- タスク選択時は必ずUpdateTimerControlsを呼び出す
- タスク状態変更後はSaveTasksとUpdateTimerControlsを実行

### その他タスクの処理
- タイマー停止時に自動生成
- 既存のその他タスクがある場合は時間を加算
- 日付が変わった場合は新規作成

## コードレビューのポイント

1. 例外処理の漏れがないか
2. ログ記録が適切か
3. UIの状態更新が漏れていないか
4. タスクの状態変更が一貫しているか
5. その他タスクの処理が正しく行われているか

## 重要な実装パターン例

### タスク状態変更のパターン
```csharp
public TaskManagerResult MoveTaskToState(TaskItem task, TaskStatus newStatus)
{
    return ExecuteOperation("タスクの状態変更", () =>
    {
        // 1. 現在のコレクションから削除
        var removeResult = RemoveTaskFromCurrentCollection(task);
        if (!removeResult.Success) return removeResult;

        // 2. 状態を変更
        var oldStatus = task.Status;
        switch (newStatus)
        {
            case TaskStatus.InProgress:
                task.SetInProgress();
                inProgressTasks.Add(task);
                break;
            // ... その他の状態
        }

        // 3. ログを記録
        logger.LogTaskStateChange(task, oldStatus, newStatus);

        // 4. 変更を保存
        var saveResult = SaveTasks();
        return saveResult;
    });
}
```

### その他タスクの自動生成パターン
```csharp
private void HandleOtherTask(TimeSpan elapsed)
{
    var otherTaskName = $"その他 ({DateTime.Now:MM/dd})";
    var existingOtherTask = inProgressTasks.FirstOrDefault(t => t.Name == otherTaskName);

    if (existingOtherTask == null)
    {
        existingOtherTask = new TaskItem(otherTaskName, "自動作成", TimeSpan.Zero);
        inProgressTasks.Add(existingOtherTask);
    }

    existingOtherTask.AddElapsedTime(elapsed);
    logger.LogOtherActivity(elapsed);
}
```

### UIの状態更新パターン
```csharp
private void UpdateUIState()
{
    var selectedTask = GetSelectedTask();
    UpdateDisplayTime(timerState.GetDisplayTime(selectedTask));
    UpdateCurrentTaskDisplay();
    UpdateTimerControls();
}
```

### トランザクション的な操作のパターン
```csharp
private void ExecuteTaskOperation(string operationName, Action operation)
{
    try
    {
        if (timerState.IsRunning)
        {
            StopTimer();
        }

        operation();
        SaveTasks();
        UpdateUIState();
    }
    catch (Exception ex)
    {
        logger.LogError($"{operationName}中にエラーが発生しました", ex);
        ShowErrorMessage($"{operationName}に失敗しました", ex);
    }
}
```

## アーキテクチャの概要

```
TaskManager/
├── Services/
│   └── TaskManagerService.cs  # タスク操作の中核機能
├── Models/
│   └── TaskItem.cs           # タスクのドメインモデル
├── MainWindow.xaml(.cs)      # メインUI
└── TaskLogger.cs            # ログ機能

主要な責務の分離:
- TaskManagerService: データ操作とビジネスロジック
- TaskItem: ドメインモデルとバリデーション
- MainWindow: UI制御とユーザーインタラクション
- TaskLogger: ログ記録と監査
```

## 品質管理のポイント

### バリデーション
1. タスク名の必須チェックと長さ制限（100文字）
2. メモの長さ制限（1000文字）
3. 予定時間の範囲チェック（0-24時間）
4. 経過時間の非負チェック

### スレッド安全性
1. ログ書き込み時のlock使用
2. UIスレッドでの更新処理の集約
3. タイマー処理の同期制御

### エラー回復
1. ログ書き込みエラー時のフォールバック
2. データ保存エラー時の再試行なし（即時エラー通知）
3. UI操作エラー時のロールバック
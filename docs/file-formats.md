# TaskManager ファイル形式仕様

## タスクファイル (.tsk)

タスクデータを保存するJSONファイル形式です。

```json
{
  "id": "タスクの一意のID (UUID)",
  "title": "タスクのタイトル",
  "description": "タスクの説明",
  "createdDate": "作成日時 (ISO 8601形式)",
  "dueDate": "期限日時 (ISO 8601形式、オプション)",
  "status": "タスクの状態 (NotStarted/InProgress/Completed)",
  "priority": "優先度 (Low/Medium/High)",
  "tags": ["タグ1", "タグ2"]  // タグの配列（オプション）
}
```

## タスクリストファイル (.tsklist)

複数のタスクをグループ化するためのJSONファイル形式です。

```json
{
  "name": "リスト名",
  "description": "リストの説明",
  "tasks": [
    {
      "id": "タスクID",
      "filePath": "タスクファイルへの相対パス"
    }
  ],
  "created": "作成日時 (ISO 8601形式)",
  "modified": "最終更新日時 (ISO 8601形式)"
}
```

## 設定ファイル (settings.json)

アプリケーションの設定を保存するJSONファイル形式です。

```json
{
  "defaultTaskDirectory": "デフォルトのタスク保存ディレクトリパス",
  "defaultListDirectory": "デフォルトのリスト保存ディレクトリパス",
  "autoSaveInterval": "自動保存の間隔（分）",
  "theme": "アプリケーションのテーマ設定",
  "language": "インターフェース言語設定"
}
```

## アーカイブファイル (.tskarchive)

完了したタスクをアーカイブするためのJSONファイル形式です。

```json
{
  "archiveDate": "アーカイブ作成日時 (ISO 8601形式)",
  "description": "アーカイブの説明",
  "archivedTasks": [
    {
      "originalTaskId": "元のタスクID",
      "taskData": {
        // タスクファイル(.tsk)と同じ形式のタスクデータ
      },
      "archiveReason": "アーカイブ理由",
      "archiveDate": "タスクがアーカイブされた日時 (ISO 8601形式)",
      "originalFilePath": "元のタスクファイルパス"
    }
  ],
  "tags": ["アーカイブ1", "2023年"] // アーカイブの分類用タグ（オプション）
}
```

## ファイル保存とバックアップ

### 自動保存
- タスクファイル(.tsk)は変更から{autoSaveInterval}分後に自動保存されます
- 保存前のファイルは`.tmp`拡張子で一時保存されます

### バックアップファイル (.tskbak)
タスクファイルの自動バックアップは以下の形式で保存されます：
```json
{
  "originalFile": "元のファイルパス",
  "backupDate": "バックアップ作成日時 (ISO 8601形式)",
  "version": "バックアップバージョン番号",
  "data": {
    // 元のファイルの完全なコピー
  }
}
```

### 保存ディレクトリ構造
```
TaskManager/
├── Tasks/           # タスクファイル (.tsk)
├── Lists/           # タスクリストファイル (.tsklist)
├── Archives/        # アーカイブファイル (.tskarchive)
└── Backups/        # バックアップファイル (.tskbak)
```

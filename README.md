# redmine_utility

redmineのREST APIを使っていろいろバックグラウンド処理をするツール。

現状のメイン機能は、あるredmineで条件を満たしたチケットを、別のRedmineのチケットと同期する機能。

## 前提条件

両方のredmineにカスタムフィールド「キーワード」（テキスト）があり、そこにスペース区切りで複数のキーワードを書く、という運用が可能であること。

ツールは、「キーワード」欄に相手方のredmineのチケット番号を書く。

## プロジェクト

### RedmineUtility

処理の本体。外部から認証鍵などを受け取り、redmineの読み書きをする。

### RedmineUtilityConsoleApp

RedmineUtilityをコンソールアプリとして使うためのアプリ。コマンドライン引数と.configの情報を使って処理を行う。

### Test～

頭にTestとつくのは、対象プロジェクトの単体テスト。


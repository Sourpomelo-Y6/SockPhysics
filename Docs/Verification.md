# 検証と開発環境

[ドキュメント一覧](README.md) / [プロジェクトREADME](../README.md)

## Unity Editorで検証する

1. Unity 2021.3.45f2で対象プロジェクトを開き、コンパイルの完了を待つ。
2. Playを停止し、編集したシーンを保存する。
3. **Sock Physics → Verify Contact Sock Physics** を実行する。
4. Consoleで `CONTACT_SOCK_VERIFICATION_PASSED` を確認する。例外が表示された場合は、その条件を確認する。

ContactInsertion・ContactStraight・ContactBendの物理挙動を検証します。シーンは検証中に開き直され、終了後に元の保存済みシーン構成とPhysics2D設定を復元します。空の未保存シーンから実行した場合は空シーンへ戻します。

主な確認対象は、接触した入口への挿入、引き抜き後の閉鎖、布の継ぎ目と食い込み、曲がりの抵抗・クリア、再挑戦です。詳細と基準設定での実測値は[Contact Sockの検証記録](ContactSock_Physics.md#検証)を参照してください。

`Verify Step N Physics` は該当する段階の旧シーンを確認するメニューです。Contactの検証メニューだけでは段階1〜8の検証を一括実行しません。

## バッチで実行する

同じプロジェクトをUnity Editorで開いている場合は、バッチ実行に別の検証用コピーを使います。コピーには `Assets`・`Packages`・`ProjectSettings` を用意し、元と同じコード・シーン・設定を検証してください。`Library`などは検証側で生成できます。

以下はWindows PowerShellの例です。Unityの実行ファイルのパスとプロジェクトのパスを自分の環境に合わせて変更します。コマンドは説明用で、実行前にパスを設定してください。

```powershell
$unityEditor = 'C:/Program Files/Unity/Hub/Editor/2021.3.45f2/Editor/Unity.exe'
$projectPath = 'C:/work/SockPhysicsValidation'
$logPath = Join-Path $projectPath 'Logs/ContactVerification.log'
New-Item -ItemType Directory -Force (Join-Path $projectPath 'Logs') | Out-Null
& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod SockPhysics.Editor.ContactSockSetup.VerifyPhysics -logFile $logPath -quit
```

起動コマンドが戻っただけで検証完了とは判断せず、ログ内の成功マーカーとUnityの正常終了を確認します。

## 描画と段階1〜8の回帰確認

`SockPhysics.Editor.ContactSockSetup.CaptureAndVerify` は、Contactシーンの物理検証・PNG保存の後に段階1〜8の物理検証を実行します。グラフィックスを使うため `-nographics` は指定しません。

```powershell
$logPath = Join-Path $projectPath 'Logs/ContactPreviewAndRegression.log'
& $unityEditor -batchmode -projectPath $projectPath -executeMethod SockPhysics.Editor.ContactSockSetup.CaptureAndVerify -logFile $logPath -quit
```

実行するプロジェクト内の `Logs` に `ContactInsertionClosed.png`、`ContactInsertionOpen.png`、`ContactInsertionWithdrawn.png`、`ContactBendClear.png` などが出力されます。Contactの成功マーカーと `STEP1_PHYSICS_VERIFICATION_PASSED`〜`STEP8_PHYSICS_VERIFICATION_PASSED`、正常終了を確認してください。

カメラのPNGにはOnGUIの案内表示や実際の入力操作は含まれません。Gameビューで脚をつかむ、Q/Eで曲げる、Rやボタンで再挑戦する操作は別途確認します。

2026-09-11の開発時は、Git管理外の `Builds/Step7Validation` を検証用コピーに使用しました。このフォルダ名は必須ではなく、クローンに含まれるものでもありません。過去の各段階ドキュメントに記載した `Logs` のファイルもローカルの実行記録です。

## 検証と再生成の違い

| メソッド | 用途 |
| --- | --- |
| `ContactSockSetup.VerifyPhysics` | 保存済みContactシーンの物理検証 |
| `ContactSockSetup.CaptureAndVerify` | 保存済みシーンの検証・描画・段階1〜8の回帰確認 |
| `ContactSockSetup.CreateAndVerify` | **Contactの3シーンを再生成してから検証。手編集した内容を上書きする** |

通常の動作確認ではVerifyを使います。サイズ・接続点・物理設定を変更したシーンへ、再生成を行わないでください。既存検証の操作経路は基準サイズ用なので、大幅な変更後は経路と判定条件の見直しも必要です。

## フォントと別環境での準備

画像などのファイルには [.gitattributes](../.gitattributes) でGit LFSを指定しています。クローン後に実体が取得されていない場合は `git lfs pull` を実行してください。

フォントは [.gitignore](../.gitignore) に従い、元ファイル・生成アセット・対応する.metaをGit管理から除外しています。対象にはFontsフォルダ、TMPのFonts & Materialsフォルダ、TTF/OTFなどのフォント、SDFフォントアセットが含まれます。

Contactシーンの操作案内はUnity標準GUIを使います。TMPの標準・サンプルフォントを使う場合は、その用途に応じたEssential Resources / Examples & Extrasを再インポートしてください。元環境の `Assets/Fonts` のNoto Sans JPと生成SDFを使う場合は別途用意します。既存参照を引き継ぐ場合は元の.metaも含めて復元し、再生成した場合はTMP設定やシーンの参照を割り当て直します。

フォントを除外したクローンだけでは、既存TMPサンプルの文字表示は保証されません。新しく生成するフォント関連アセットも除外対象のフォルダで管理してください。

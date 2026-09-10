# Sock Physics

足を差し込んで靴下を履かせる、Unity製の2D物理プロトタイプです。閉じた上下の布を足で押し広げ、引き抜くと再び閉じます。曲がった靴下では、押す・引き戻す・足首を曲げる操作で攻略します。

現在はUnity Editorで動作を確認する開発段階です。開始画面やステージ選択、配布用ビルドはまだ用意していません。

## はじめる

必要な環境:

- **Unity 2021.3.45f2**（プロジェクト指定バージョン）
- **Git LFS**（画像などの取得に使用）
- パッケージ取得のためのネットワーク接続。URP 12.1.15などの依存関係は `Packages` で管理しています。

1. Git LFSを導入してから、このリポジトリをクローンする。クローン済みの場合は、リポジトリ内で `git lfs install`、`git lfs pull` を実行する。
2. Unity Hubで、`Assets`・`Packages`・`ProjectSettings` があるフォルダをプロジェクトとして追加する。
3. Unity 2021.3.45f2で開き、初回インポートとコンパイルの完了を待つ。
4. **Sock Physics → Open Contact Insertion Scene** を選び、Playする。

Gameビューは16:9、960×540以上が目安です。現在の操作案内はUnity標準GUIによる英語表示です。

## シーンと操作

| Sock Physicsメニュー | 内容 |
| --- | --- |
| **Open Contact Insertion Scene** | 短い直線で抜き差しを試す。クリアによる操作ロックなし |
| **Open Contact Straight Scene** | 長い直線の靴下へ足を収めてクリアする |
| **Open Contact Bend Scene** | 押し戻しで抵抗を下げ、足首を曲げて履く |

| 操作 | 動作 |
| --- | --- |
| 明るい緑の脚を左ドラッグ | 脚と足を移動 |
| Q / E | 足首を曲げる / 戻す |
| R | 脚・足・布・攻略状態をリセット |

まず短い直線で、右へゆっくり挿入し、左へ完全に抜いてみてください。リセットせずに布が閉じ戻ります。曲がりでは `PUSH RIGHT` / `PULL LEFT` に従って4回押し戻し、抵抗が0になったら角度と位置を合わせます。詳しい手順は[接触する靴下のガイド](Docs/ContactSock_Physics.md)を参照してください。

`Open Step 1〜8 Scene` は段階別の比較用シーンです。最新の開閉動作は上記のContactシーンで確認できます。

## 調整・開発

| 目的 | ドキュメント |
| --- | --- |
| 布の柔らかさ・開閉を調整する | [Contact Sockの物理と操作](Docs/ContactSock_Physics.md) |
| 足・脚を大きくする、めり込みや接続位置を調整する | [サイズ・当たり判定・足首の接続](Docs/FootLeg_SizeAndCollision.md) |
| 自動検証・描画確認を実行する | [検証ガイド](Docs/Verification.md) |
| 実装状況と次の作業を確認する | [実装計画](Docs/SockPhysics_ImplementationPlan.md) |
| 設計意図や開発経緯を読む | [ドキュメント一覧](Docs/README.md) |

サイズ変更では **Sprite RendererのSizeとCapsule Collider 2DのSizeを合わせる**必要があります。長さを変えた場合は足首の接続点・開始位置・クリア判定も調整します。保存済みシーンを手編集した後は、シーンを再生成する `CreateAndVerify` を実行すると編集が上書きされます。

## 実装状況と制限

接触する布の開閉、直線・曲がりのクリア、押し戻し抵抗、再挑戦を実装しています。基準サイズでは、2026-09-11にContactシーンと段階1〜8の物理検証・カメラ描画確認を実施しました。実際のマウス操作やGUIの確認は、自動検証とは別にGameビューで行います。

大幅なサイズ変更後の動作は未保証です。詳細なかかとの形状、ルーズソックス、完成版グラフィック、画面遷移などは今後の調整・追加対象です。[設計仕様](Docs/SockPhysics_DesignSpec.md)には未実装の構想も含まれます。

Build Settingsは初期のSampleSceneのままです。現在はContactシーンを直接開いてPlayしてください。

## リポジトリ構成

```text
Assets/Scenes/          Contactシーンと段階別シーン
Assets/SockPhysics/
  Scripts/              操作・布・抵抗・クリア判定
  Editor/               シーン生成と物理検証
  Art/                  仮画像・マテリアル
Docs/                   操作・調整・検証・設計記録
Packages/               Unityパッケージ定義
ProjectSettings/        Unityプロジェクト設定
```

フォントアセットはGit管理対象外です。Contactシーンの標準GUIは除外したTMPフォントを使いませんが、TMPサンプルや日本語フォントを使う場合は別途復元が必要です。詳細は[フォントと別環境での準備](Docs/Verification.md#フォントと別環境での準備)を参照してください。`Library`・`Logs`・`Builds`などの生成物も管理対象外です。

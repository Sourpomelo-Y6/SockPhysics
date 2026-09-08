# 段階1: 足の操作プロトタイプ

実装日: 2026-09-08

## 起動と操作

1. Unity 2021.3.45f2でプロジェクトを開く。
2. `Assets/Scenes/FootPrototype.unity`を開く。メニューの `Sock Physics > Open Step 1 Scene` からも開ける。
3. Playを押し、Gameビューで緑色の足を左クリックしてドラッグする。
4. 青い壁へ押し当て、左へ引き戻す。上下にも動かせる。
5. マウスを離すと減速して止まる。`R`キーまたは画面左上の `Reset foot [R]` ボタンで初期状態へ戻る。

Gameビューは16:9、960×540以上を目安にする。操作案内はUnity標準GUIの英語表示で、Git除外対象の日本語フォントやTMPフォントに依存しない。

## 実装内容

- 足は横長の仮カプセル。Rigidbody2DとCapsuleCollider2Dで構成する。
- 重力を無効化し、回転を固定する。足首操作は段階5で追加予定。
- マウスで足をつかんだ位置を保ちながら、目標位置へ力で追従する。移動はFixedUpdateで行い、通常操作でTransformを直接移動しない。
- 追従力・減衰・最大力・最大速度はFootのFootControllerから調整できる。
- 足は連続衝突判定を使用する。中央右の検証用壁と四方の境界は静的BoxCollider2D。
- リセットは位置・回転・速度・ドラッグ状態を初期化する。アプリのフォーカス喪失時にもドラッグを解除する。
- 仮画像、マテリアル、壁、カメラ、HUDはシーンに保存済み。実行時のシーン生成は不要。

SampleSceneとBuild Settingsは維持している。今回の検証ではFootPrototypeを直接開いてPlayする。

## 検証

Unityのバッチ実行でコンパイルと2D物理の自動検証を実施し、`STEP1_PHYSICS_VERIFICATION_PASSED`を確認した。

検証対象:

- 背景クリックでは足をつかまない。
- 足をつかみ、目標位置へ前後上下に移動できる。
- 遠い目標へ500ステップ押し続けても検証用壁を越えない。
- 壁から引き戻せる。
- 高速ドラッグ相当の遠い目標でも上の境界を越えない。
- 速度上限を維持し、解放後に減速する。
- リセットと再ドラッグを5回繰り返しても壁を越えず、初期位置と速度を復元できる。

固定時間刻み0.02秒のPhysics2D.Simulateを使い、実際のシーンのColliderと操作用の物理処理を検証する。再実行はPlay停止中に `Sock Physics > Verify Step 1 Physics` を選ぶ。検証は一時的にシーンを開き直すため、保存確認に従って編集を保存する。

バッチ実行例（PowerShell）:

```powershell
& 'C:/Program Files/Unity 2021.3.45f2/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/hi-wa/unity_workspase/SockPhysics' -executeMethod SockPhysics.Editor.FootPrototypeSetup.VerifyPhysics -logFile 'C:/Users/hi-wa/unity_workspase/SockPhysics/Logs/Step1Verification.log' -quit
```

自動検証はマウスの画面座標変換、GUIボタンの実クリック、描画の目視確認を含まない。UnityのGameビューで操作感と表示を確認すること。物理パラメータを大きく変更した場合は再検証する。

## 次の段階

靴下の上下2列の物理ポイント、隣接Joint、仮表示を追加する。今回の足と壁のシーンを土台に、布の変形と安定性を検証する。

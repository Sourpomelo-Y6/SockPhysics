# 足・脚の大きさと当たり判定の調整

[ドキュメント一覧](README.md) / [プロジェクトREADME](../README.md)

作成日: 2026-09-11

対象はContactInsertion、ContactStraight、ContactBend。Playを停止して対象シーンを開き、変更後に保存する。Play中の変更は停止すると元に戻る。シーンごとの設定なので、別シーンへは自動反映されない。

## 見た目と押しのける範囲

Hierarchyの `Leg (drag here)` が明るい緑の脚、`Foot (Q E ankle)` が濃い緑の足。

- `Sprite Renderer > Size`：見た目のサイズ。
- `Capsule Collider 2D > Size`：靴下を物理的に押しのける範囲。
- `Capsule Collider 2D > Offset`：当たり判定の中心のずれ。基準はX=0、Y=0。

基本はSprite RendererとCapsule Collider 2DのSizeを同じ値にする。Colliderは有効、Is Triggerはオフ、DirectionはHorizontalのままにする。SceneビューでGizmosを有効にして対象を選択し、必要ならEdit Colliderで輪郭を確認する。見た目だけ大きくすると、布は小さいColliderまで入り込めるため、足・脚へめり込んで見える。

2026-09-11のローカル調整時には、ContactBendのSprite Rendererが脚1.80×0.48、足1.92×0.60へ変更され、Capsule Collider 2Dが脚1.50×0.40、足1.60×0.50のままという不一致が見つかった。これは当時の手編集の事例であり、リポジトリの基準設定を示すものではない。同様の場合はColliderのSizeも変更後の見た目に合わせる。Play中や未保存の状態は保存済みファイルと異なる場合がある。

## 厚みを20％増やす例

| 対象 | 基準Size（X, Y） | 変更後Size（X, Y） |
| --- | --- | --- |
| 脚 | 1.50, 0.40 | 1.50, 0.48 |
| 足 | 1.60, 0.50 | 1.60, 0.60 |

それぞれSprite RendererとCapsule Collider 2Dの両方へ同じ値を入れる。長さは変わらないので足首の接続位置は基準値のまま試せる。これは調整例であり、この変更後サイズでの攻略は未検証。

## 全体を20％増やす例と足首の接続

| 対象 | Sprite RendererとCapsule Collider 2DのSize（X, Y） |
| --- | --- |
| 脚 | 1.80, 0.48 |
| 足 | 1.92, 0.60 |

足にある `Hinge Joint 2D` も合わせる。Connected Rigidbodyには `Leg (drag here)` のRigidbody2Dを指定する。Anchorは足の中心から見た支点、Connected Anchorは脚の中心から見た支点で、この2点がワールド座標で一致するように配置する。

| 項目 | 基準 | 20％拡大後 |
| --- | --- | --- |
| Anchor | -0.65, 0 | -0.78, 0 |
| Connected Anchor | 0.75, 0 | 0.90, 0 |

Auto Configure Connected Anchorはオフ。TransformのScaleは基準の1,1,1を維持し、SizeとAnchorで調整する。Scaleも変更すると寸法がさらに拡大される。

開始時の足首角度が0度なら、足の中心を脚の中心より右へ1.68に配置する。基準の中心間隔は1.40なので、脚を動かさなければ足を右へ0.28動かす。ContactBendの基準開始位置なら脚(-4,-0.25)、足(-2.32,-0.25)となる。関節の両側の接続点が同じ場所になるようにし、開始時に関節が急に位置を補正することを避ける。

任意の角度では、足の中心位置 = 脚の接続点のワールド座標 − 足のAnchorの回転・拡縮後のベクトル。足のTransformを移動し、両Anchorの表示が一致することを確認する。

水平時の関係は「脚の中心 → 0.90 → 接続点 → 0.78 → 足の中心」。Play開始時に跳ねないこと、Q/Eで接続部を中心に足が回ることを確認する。Anchorは回転の支点であり、接続部で布を押しのける範囲は足・脚のCapsule Collider 2Dが決める。

## Sizeを合わせてもめり込む場合

まず「描画だけの重なり」か「Colliderの輪郭にも食い込んでいるか」をSceneビューで区別する。

1. ColliderのSize、Offset、TransformのScaleを確認する。Spriteだけを大きくしていないかを優先して確認する。
2. 足を靴下の外へ置いてからPlayする。拡大後の足が初期状態ですでに布と重ならないようにする。
3. ゆっくり押して確認する。新シーンの脚のFootControllerはMax Speed=2、Max Force=30が基準。高速時だけ食い込む場合はMax Speedを下げて原因を切り分ける。押す力を増やすことは、当たり範囲を増やす操作ではない。
4. 足が大きく、布が開き切らない場合は、Contact SockのContactSockでClosing Strength（基準4）を少し下げる。必要ならShape Strength（基準60）も少し下げるが、靴下の形が流れやすくなるので1項目ずつ試す。これは布の抵抗の調整であり、Colliderの不一致を直す代わりにはならない。
5. 大幅な拡大では、曲がりの半径・布の点数・つま先の位置も再設計する。元サイズ用の靴下へ任意の大きさの足が入ることは保証されない。

Colliderを見た目よりわずかに大きくすると接触を早めることはできるが、大きすぎると足と布の間が浮いて見える。まず同じSizeで確認する。布のCircleCollider2DのRadiusを一括で大きくする方法は、閉じた初期状態の上下を重ねてしまうため、初期間隔や点配置とセットで再調整が必要。

物理計算の精度を調整する段階では、StagePhysicsSettingsがPlay中のPosition/Velocity Iterationsを両方12へ設定していることにも注意する。Project Settingsだけを変更しても、このシーンでは開始時に上書きされる。現状はスクリプト内の固定値なので、精度変更にはコードの調整が必要。

## 正式にサイズ変更する際の関連箇所

Inspectorのサイズ変更だけでは、クリア位置や足の内部判定用の位置は自動更新されない。

- `Assets/SockPhysics/Scripts/FitEvaluator.cs`：足先(0.8,0)、かかと(-0.7,-0.2)、足首(-0.65,0)のローカル座標。全体を1.2倍にするなら、それぞれ(0.96,0)、(-0.84,-0.24)、(-0.78,0)が対応する値。厚みだけの変更でも、かかとのYは見直す。
- シーンのFitEvaluator：Toe/Heel/Ankle/Leg Targetなど、目標姿勢に応じたワールド座標。
- `Assets/SockPhysics/Scripts/ContactSock.cs`：ContainsFootの前・中央・後ろの確認位置（現在は足の中心から±0.5）。
- `Assets/SockPhysics/Editor/ContactSockSetup.cs`：生成時の形状、目標位置、検証で使う足首・脚の位置と移動経路。

これらのコードには複数シーンで共有する値があるため、1シーンだけ拡大する際に固定値を一律変更すると、旧シーンの判定も変わる。正式対応では寸法と評価位置を設定としてまとめ、サイズごとに確認する。

再検証では、入口への挿入、足首の回転、引き抜き後の閉鎖、曲がりの押し戻し、クリア・再挑戦を確認する。既存の自動検証は元サイズ用の操作経路を使うため、サイズ変更後に失敗した場合は原因を確認し、経路・目標と物理設定を合わせて更新する。シーンを再生成するCreateAndVerifyは手編集を上書きするため、今回の調整中は実行しない。

開閉する布の構造・調整項目は [ContactSock_Physics.md](ContactSock_Physics.md) を参照。

# Sock Physics

## 1. Project Overview

**Project Name:** Sock Physics  
**Engine:** Unity  
**Game Type:** 2D Physics Action / Physics Puzzle  
**Development Stage:** Prototype

「Sock Physics」は、2D断面図で足と靴下を表示し、
柔らかく変形する靴下へ足を入れて履くことを目的とした物理ゲーム。

現実の靴下を完全にシミュレーションするのではなく、
「押し込む」「引き戻す」「角度を変える」「押し広げる」といった操作を
ゲームとして楽しめるよう、実際の靴下とは異なる挙動やルールを取り入れる。

---

# 2. Core Concept

プレイヤーは足・脚を操作し、
閉じた状態に近い柔らかい靴下の内部へ足を押し込んでいく。

靴下は2D断面として表示する。

初期状態では靴下の上下の布同士の間隔は非常に狭く、
足を入れることで物理的に押し広げられる。

基本的なプレイ感は以下。

1. 靴下の入口へ足を入れる
2. 足を奥へ押す
3. 靴下が押し広げられる
4. 狭い場所などで足が引っかかる
5. 少し足を戻す
6. 位置や角度を調整する
7. 再び押し込む
8. 足首・かかとを通過させる
9. 足を靴下の先端まで入れる
10. 正しく履けたらクリア

単純に一方向へ押し続けるのではなく、

**Push → Stuck → Pull Back → Adjust → Push Again**

という反復をゲームプレイの中心とする。

---

# 3. Visual Representation

ゲーム画面は横から見た2D断面図を基本とする。

参考図では以下のような構成。

- Green: 足・脚
- Blue: 靴下の外側
- Yellow: 靴下内部または靴下の形状を示す部分
- Purple: 足首・かかと周辺などの重要ポイント

実際のゲームでは必ずしもこの色分けを使用する必要はない。

重要なのは、

- 足
- 靴下の上側
- 靴下の下側
- 靴下内部

の位置関係がプレイヤーから分かりやすいこと。

---

# 4. Sock Physics

## 4.1 Basic Structure

靴下を単純なSpriteとして変形させるのではなく、
複数の物理ポイントによって構成された柔らかい物体として扱う。

候補となるUnityコンポーネント：

- Rigidbody2D
- Collider2D
- DistanceJoint2D
- SpringJoint2D

靴下の上側と下側を、それぞれ複数の物理ポイントで構成する。

Example:

Upper:

O---O---O---O---O---O

Lower:

O---O---O---O---O---O

各ポイントをJointで接続する。

さらに必要に応じて上下の対応するポイントにも
Jointや復元力を設定する。

---

# 5. Closed Sock State

重要な仕様として、

**靴下は最初から大きく開いているのではなく、
上下の布の間がほぼ閉じた状態から開始する。**

足のCollider2Dを靴下内部へ押し込むことで、
上下の物理ポイントが押し広げられる。

足がなくなった場所については、
Jointや独自の復元力によって再び閉じようとする。

これによって、

「閉じた柔らかい袋へ足を押し込んでいる」

感覚を表現する。

---

# 6. Sock Sections

靴下全体を同一の物理特性にする必要はない。

部位によって形状や物理特性を変更する。

## 6.1 Leg Section

脚を通す直線部分。

特徴：

- 比較的簡単
- 上下の布が平行に近い
- 足によって簡単に押し広げられる
- チュートリアル的な役割を持つ

ここまでは比較的スムーズに足を入れられる。

---

## 6.2 Ankle Section

ゲーム性が本格的に発生する部分。

特徴：

- 少し狭い
- 足が引っかかる
- 足の角度調整が必要
- 押すだけでは通らない場合がある

「少し戻してもう一度入れる」という操作を発生させる。

---

## 6.3 Heel Section

かかとは脚部分とは異なる形状が必要になる可能性が高い。

単純な上下平行構造ではなく、

- 膨らみ
- 曲線
- 局所的な伸び
- 足首方向への曲がり

などを表現する。

プロトタイプ初期では完全な靴下形状を再現せず、
単純な形状から開始する。

---

## 6.4 Toe Section

靴下の先端。

袋状に閉じた形にする。

足先がここまで到達することを
クリア条件の一つとして利用できる。

---

# 7. Foot Physics

足と脚も単純な一枚のSpriteではなく、
物理的な形状を持たせる。

最低限、

- Leg
- Foot

の2パーツに分割することを想定。

足首部分をJointで接続する。

これにより、

- 足をまっすぐにする
- 足首を曲げる

という操作を可能にする。

必要なら将来的に、

- つま先
- 足裏
- かかと
- 脚

などへさらに分割できる。

ただし最初のプロトタイプでは複雑化させない。

---

# 8. Sock Pattern A: Straight

靴下の脚部分と足部分がほぼ一直線になっている状態。

普通の靴下を正面方向から履いているイメージ。

Difficulty: Easy

Gameplay:

1. 足を入口へ入れる
2. 奥へ押す
3. 狭い部分で引っかかる
4. 少し戻す
5. 再び押す
6. 徐々に奥へ入る
7. 足先まで入ればクリア

基本操作を学ぶための最初のステージとして使用する。

---

# 9. Sock Pattern B: 90 Degree Bend

靴下の脚部分と足部分が約90度曲がっている状態。

横から見たとき、
足が靴下の足部分へ入りづらくなっている状態を
ゲームとして誇張したもの。

Difficulty: Normal / Hard

最初に入ってくる足と脚はほぼ一直線。

そのため、そのまま押しても曲がった靴下の奥へ進めない。

Gameplay:

1. 足を直線部分へ入れる
2. 足先が90度の曲がり部分へ到達
3. 足を曲がり部分へ押し当てる
4. 一度戻す
5. 再び押し当てる
6. これを数回繰り返す
7. 曲げるための抵抗が減少
8. 足首を曲げる
9. 足部分へ足を押し込む
10. かかとを合わせる
11. クリア

---

# 10. Bend Resistance

90度パターンでは、
完全な物理演算だけでゲーム性を作らなくてもよい。

ゲーム用パラメータとして、

`BendResistance`

を用意する案がある。

Example:

100
↓
75
↓
50
↓
25
↓
0

足を曲がり部分へ適切に押し当てることで減少する。

0になると、

- 靴下が曲がりやすくなる
- 足首が曲げやすくなる
- Jointの制約が弱くなる
- 靴下のSoftnessが増える

などの変化を発生させる。

物理演算とゲームルールを組み合わせることで、
プレイヤーが意図を理解しやすい挙動を作る。

---

# 11. Physics + Game Parameters

すべてを純粋な物理演算だけで実装しない。

ゲームとして制御しやすくするため、
内部パラメータを併用する。

候補：

## Insertion

足がどこまで入っているか。

## Friction

足と靴下の動きにくさ。

## Stretch

靴下がどれだけ伸びているか。

## Softness

靴下の柔らかさ。

## BendResistance

曲がり部分を攻略するための抵抗値。

## AnkleAngle

現在の足首角度。

## Fit

足と靴下の位置がどれくらい合っているか。

---

# 12. Player Controls

操作方法はプロトタイプを作りながら検討する。

候補：

## Mouse Drag

足・脚をドラッグして、

- Push
- Pull
- Move Up
- Move Down

を行う。

## Ankle Control

別操作によって足首角度を変更する。

候補：

- Mouse Wheel
- Right Drag
- Keyboard
- Dedicated UI

最初のプロトタイプでは操作を単純にする。

---

# 13. Important Gameplay Principle

プレイヤーに、

「強く押し続ければクリアできる」

と思わせないようにする。

重要なのは、

**押す**
↓
**引っかかる**
↓
**戻す**
↓
**角度を変える**
↓
**もう一度押す**

という操作。

引き戻すこと自体を攻略行動にする。

---

# 14. Clear Conditions

単純に足先が奥まで到達しただけでは
クリアにしないことも検討する。

例えば以下をチェックする。

- Toe Position
- Heel Position
- Ankle Position
- Leg Insertion
- Sock Stretch
- Overall Fit

これらからFit Scoreを計算する。

Example:

Fit: 92%

一定以上ならクリア。

将来的には完成度によってランクを付けてもよい。

---

# 15. Prototype Scope

最初から完全な靴下シミュレーションを作らない。

## Prototype 1

目的：

「閉じた柔らかい2D物体へ足を押し込めるか」

を確認する。

実装：

- Straight Sock
- Sock Upper Physics Points
- Sock Lower Physics Points
- Foot Collider
- Mouse Drag
- Jointによる靴下復元

グラフィックは仮素材でよい。

---

## Prototype 2

追加：

- Leg + Foot
- Ankle Joint
- 足首角度変更
- 引っかかり
- 摩擦調整

---

## Prototype 3

追加：

- Heel Shape
- Toe Shape
- Clear Detection
- Fit Detection

---

## Prototype 4

90度パターンを追加。

- Bend Section
- BendResistance
- Softness Change
- Repeated Push Mechanic

---

# 16. Suggested Unity Architecture

初期案。

## SockController

靴下全体を管理。

Responsibilities:

- Sock state
- Stretch
- Softness
- Fit
- Clear detection

---

## SockPhysicsPoint

靴下を構成する個々の物理ポイント。

Components:

- Rigidbody2D
- Collider2D
- Joint

---

## SockSection

靴下の部位。

Possible types:

- Leg
- Ankle
- Heel
- Foot
- Toe

部位ごとに異なる物理パラメータを設定できるようにする。

---

## FootController

プレイヤーの足を管理。

Responsibilities:

- Movement
- Push / Pull
- Ankle control

---

## FootPart

足の各物理パーツ。

Prototypeでは、

- Leg
- Foot

程度から開始。

---

## BendController

90度ステージ用。

Responsibilities:

- BendResistance
- Impact detection
- Resistance reduction
- Softness modification

---

## FitEvaluator

足と靴下の位置関係を評価。

Responsibilities:

- Toe check
- Heel check
- Ankle check
- Leg check
- Fit score
- Clear condition

---

# 17. Data Driven Design

可能であれば靴下の特性を
ScriptableObjectなどで変更できるようにする。

Example:

SockData

- SockName
- Friction
- Softness
- Stretchiness
- LegWidth
- AnkleWidth
- BendResistance
- RecoveryStrength

これによってコードを書き換えずに
異なる靴下を作れるようにする。

---

# 18. Future Sock Variations

将来的には以下のようなバリエーションを検討できる。

- Wide Sock
- Tight Sock
- High Friction Sock
- Very Soft Sock
- Short Sock
- Long Sock
- Twisted Sock
- 90 Degree Sock

物理パラメータと初期形状の違いによって
ステージを増やす。

---

# 19. Future Feature: Loose Socks

後から実装する予定の要素。

**Loose Socks / ルーズソックス**

通常の靴下より長い。

足を完全に入れた後、
余った靴下を下方向へ寄せる操作を追加する。

Possible gameplay flow:

Insert Foot
↓
Match Heel
↓
Pull Sock
↓
Gather Excess Fabric
↓
Create Folds
↓
Evaluate Appearance

通常のSock Physicsとは別の
第2ゲームフェーズとして実装できる。

最初のプロトタイプには含めない。

---

# 20. Development Philosophy

このゲームでは、
現実の布シミュレーションを完全再現することを目的としない。

優先順位：

1. 操作して面白い
2. 柔らかく変形して気持ちいい
3. プレイヤーが状態を理解できる
4. 攻略方法が存在する
5. 物理的にそれらしく見える
6. 現実の靴下として正確

Unityの物理演算で不安定になる部分については、
独自パラメータや補正処理を積極的に使用する。

---

# 21. First Development Goal

まず以下だけを実装する。

**「ほぼ閉じている2D靴下へ、
マウス操作した足を押し込むと、
靴下が柔らかく上下へ開いて足を受け入れる」**

この挙動が成立することを確認してから、
足首・かかと・90度パターンなどを追加する。

最初から完成形の靴下を実装しないこと。

---

# 22. Codex Initial Task

最初の実装では以下を優先する。

1. Unity 2Dで新規プロトタイプシーンを作る
2. 上下2列のSockPhysicsPointを配置する
3. 隣接ポイントをJointで接続する
4. 上下が閉じようとする復元力を作る
5. Foot用Rigidbody2D + Collider2Dを作る
6. マウスドラッグでFootを操作できるようにする
7. FootをSockへ押し込むと上下が開くようにする
8. Footを抜くとSockが元の閉じた形へ戻るようにする
9. Inspectorから物理パラメータを調整できるようにする

この段階では、

- 90度パターン
- 詳細なかかと
- Fit Score
- Loose Socks
- 完成版グラフィック

は実装しない。

まず物理挙動の検証を最優先する。
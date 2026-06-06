# test1 场景与 Prefab 搭建说明

## 目标

`Assets/test1` 这一版只提供规则逻辑和最小场景绑定脚本，不再由代码生成整套 UI。美术和场景由人工搭建，代码只负责：发牌、出牌、比牌、计分、前三轮看牌换牌、结算，以及把状态刷新到你手动指定的 UI 物体上。

旧版 `Assets/BoardGame` 可以保留在工程中，但新场景不要挂载旧版脚本，避免两套逻辑同时运行。

## 文件结构

```text
Assets/test1/
  Scripts/Core/
    Test1GameDefinitions.cs
    Test1GameRuleConfig.cs
    Test1BoardGameSession.cs
    Test1BoardGameController.cs
  Scripts/Rules/
    Test1DealService.cs
    Test1RuleEngine.cs
    Test1TurnManager.cs
  Scripts/View/
    Test1CardView.cs
    Test1BoardGameView.cs
  Docs/
    SceneAndPrefabSetup.md
```

## 推荐场景层级

```text
Test1GameScene
  Main Camera
  Directional Light
  EventSystem
  Canvas
    BoardRoot
      PlayerAInfoPanel
      PlayerAHandRoot
      PlayerAPlaySlot
      RemovedCardSlot
      PlayerBPlaySlot
      PlayerBHandRoot
      ScorePanel
      HistoryPanel
      ControlPanel
      PassDevicePanel
      FinalResultPanel
  Test1GameRoot
```

## 必须挂载的脚本

| 物体 | 脚本 | 必填组件 / 引用 |
|---|---|---|
| `Test1GameRoot` | `Test1BoardGameController` | 可选绑定 `Test1GameRuleConfig` |
| `Test1GameRoot` 或 `BoardRoot` | `Test1BoardGameView` | 绑定 `Controller`、卡牌 Prefab、手牌区、出牌槽、文本、按钮 |
| `CardView.prefab` 根节点 | `Test1CardView` | `Button`、`Image`、数字文本、颜色文本、选中框 |

`Test1BoardGameController` 是权威逻辑入口。`Test1BoardGameView` 只是把逻辑状态刷新到人工搭好的 UI 上，可以按项目 UI 风格替换或扩展。

## 规则配置

可以不创建配置资源，`Test1BoardGameController` 会自动使用默认规则。若需要可视化调整：

1. 在 Project 面板右键选择 `Create > Test1 > Board Game Rule Config`。
2. 命名为 `Test1GameRuleConfig.asset`。
3. 将资源拖到 `Test1BoardGameController.RuleConfig`。

默认配置如下：

| 字段 | 默认值 | 说明 |
|---|---|---|
| `RoundScores` | `10, 10, 20, 20, 30, 30, 20` | 7 轮分值 |
| `Colors` | `Red, Green, Blue` | 三种公开颜色 |
| `Numbers` | `1, 2, 3, 4, 5` | 每种颜色 5 张牌 |
| `MaxDealRetryCount` | `1000` | 发牌不满足约束时的最大重试次数 |
| `TurnPolicy` | `RandomFirstThenAlternate` | 第一轮随机，之后轮流先手 |
| `EnablePeekSwap` | `true` | 第三轮后启用看牌换牌 |
| `RequireEachPlayerHasOneAndFive` | `true` | 双方手牌都必须至少有 1 张 `1` 和 1 张 `5` |

## CardView.prefab 搭建

`CardView.prefab` 的外观完全由人工设计。脚本只会填充文本、改背景色、处理点击。

推荐结构：

```text
CardView.prefab
  Root (Button + Image + Test1CardView)
    ColorText (Text)
    NumberText (Text)
    OwnerText (Text，可选)
    SelectionFrame (Image，可选，默认隐藏)
    SelectionText (Text，可选)
```

在 `Test1CardView` 上绑定：

| 字段 | 绑定对象 |
|---|---|
| `Button` | 根节点 `Button` |
| `BackgroundImage` | 根节点或卡面背景 `Image` |
| `SelectionFrame` | 选中高亮框，可为空 |
| `NumberText` | 显示数字的 `Text` |
| `ColorText` | 显示颜色的 `Text` |
| `OwnerText` | 调试用归属文本，可为空 |
| `SelectionText` | 调试用选中文本，可为空 |

隐藏数字时，脚本会显示 `?`，但仍保留颜色信息，符合“颜色公开、数字隐藏”的规则。

## Test1BoardGameView 引用绑定

在 `Test1BoardGameView` 上至少绑定以下引用：

| 字段 | 建议物体 | 说明 |
|---|---|---|
| `Controller` | `Test1GameRoot` | 挂有 `Test1BoardGameController` 的物体 |
| `CardViewPrefab` | `CardView.prefab` | 运行时用于显示手牌、出牌、移除牌 |
| `PlayerAHandRoot` | `PlayerAHandRoot` | 玩家 A 手牌容器，建议加 `HorizontalLayoutGroup` |
| `PlayerBHandRoot` | `PlayerBHandRoot` | 玩家 B 手牌容器，建议加 `HorizontalLayoutGroup` |
| `PlayerAPlaySlot` | `PlayerAPlaySlot` | 玩家 A 本轮出牌槽 |
| `PlayerBPlaySlot` | `PlayerBPlaySlot` | 玩家 B 本轮出牌槽 |
| `RemovedCardSlot` | `RemovedCardSlot` | 移除牌槽，平时只显示颜色 |
| `RoundText` | 轮次文本 | 显示当前第几轮 |
| `PhaseText` | 阶段文本 | 显示当前状态机阶段 |
| `ScoreText` | 分数文本 | 显示双方分数 |
| `PromptText` | 提示文本 | 显示当前应由谁操作 |
| `RemovedCardText` | 移除牌文本 | 显示移除牌公开信息 |
| `RoundResultText` | 回合结果文本 | 显示当前回合结算 |
| `HistoryText` | 历史文本 | 显示已完成轮次 |
| `FinalResultText` | 最终结算文本 | 显示最终胜负 |

按钮绑定：

| 字段 | 按钮用途 | 绑定方法 |
|---|---|---|
| `StartButton` | 开始 / 再来一局 | 脚本自动绑定 `StartNewGame()` |
| `ConfirmButton` | 确认出牌 | 脚本自动绑定 `ConfirmSelectedCard()` |
| `ContinueButton` | 回合结算后继续 | 脚本自动绑定 `ContinueAfterRound()` |
| `KeepRemovedButton` | 看牌后不换牌 | 脚本自动绑定 `KeepRemovedCard()` |
| `SwapModeButton` | 进入换牌选择 | 脚本自动绑定 `EnterSwapSelection()` |
| `ConfirmSwapButton` | 确认换牌 | 脚本自动绑定 `ConfirmSwapSelectedCard()` |

按钮不需要再手动配置 `OnClick`，只要拖到 `Test1BoardGameView` 对应字段即可。

## 热座隐私面板

本游戏有隐藏信息，建议搭建 `PassDevicePanel`，避免当前玩家直接看到下一名玩家手牌。

推荐结构：

```text
PassDevicePanel
  MaskImage
  TipText
  ReadyButton
```

在 `Test1BoardGameView` 上绑定：

| 字段 | 绑定对象 |
|---|---|
| `UsePassDevicePanel` | 勾选 |
| `PassDevicePanel` | 遮罩面板根节点 |
| `PassDeviceText` | 面板提示文本 |
| `PassDeviceButton` | “我已准备好”按钮 |

脚本会在需要私密操作时自动显示该面板。玩家点击 Ready 后，才会显示当前玩家手牌数字或移除牌数字。

## 运行流程

1. 点击 `StartButton` 开始对局。
2. 当前先手玩家在自己的手牌中选择 1 张牌。
3. 点击 `ConfirmButton` 确认出牌，牌会进入出牌槽并隐藏数字。
4. 后手玩家通过隐私面板确认后选择 1 张牌。
5. 后手确认后，两张牌翻开并自动结算本轮分数。
6. 点击 `ContinueButton` 进入下一轮。
7. 第 3 轮结束后，如果有看牌权，会进入 `PeekDecision`：
   - 点击 `KeepRemovedButton`：不换牌，进入第 4 轮。
   - 点击 `SwapModeButton`：选择 1 张手牌，再点击 `ConfirmSwapButton` 完成交换。
8. 第 7 轮结算后，显示最终胜负。

## 逻辑规则覆盖

当前实现覆盖以下规则：

| 规则 | 实现位置 |
|---|---|
| 生成 15 张牌 | `Test1DealService.CreateFullDeck` |
| 随机移除 1 张牌 | `Test1DealService.Deal` |
| 按颜色拆分并发 2/2/3 手牌 | `Test1DealService` |
| 每人至少 1 张 `1` 和 1 张 `5` | `Test1DealService.IsValidDeal` |
| 数字大者胜 | `Test1RuleEngine.CompareCards` |
| `1` 胜 `5` | `Test1RuleEngine.CompareCards` |
| 同数字平局 | `Test1RuleEngine.CompareCards` |
| 7 轮分数 | `Test1GameRuleConfig.RoundScores` |
| 前三轮看牌权 | `Test1RuleEngine.ResolvePeekOwner` |
| 看牌后可换牌 | `Test1BoardGameSession.SwapRemovedWithHandCard` |
| 最终胜负 / 平局 | `Test1RuleEngine.ResolveGameResult` |

## 注意事项

- 不要在新场景中挂载 `Assets/BoardGame` 下的旧版脚本。
- `Test1BoardGameView` 会实例化你提供的 `CardView.prefab`，但不会创建 Canvas、布局、按钮或美术资源。
- 如果想完全自定义 UI，可以只使用 `Test1BoardGameController` 和 `Test1BoardGameSession`，监听 `OnStateChanged` 后自行刷新界面。
- 如果需要调试，可以勾选 `Test1BoardGameView.ShowAllNumbersForDebug`，正式体验时应关闭。

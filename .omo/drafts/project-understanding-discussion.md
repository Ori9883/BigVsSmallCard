# Draft: Project Understanding Discussion

## Requirements (confirmed)
- 用户要求："分析项目代码和里面的文档，自己了解这个unity游戏工程的细节，然后我们开始讨论"

## Technical Decisions
- 当前阶段只做只读理解与讨论准备，不实施代码修改。
- 后续如用户提出具体目标，再生成 `.omo/plans/*.md` 决策完整工作计划。

## Research Findings
- Unity 版本：`ProjectSettings/ProjectVersion.txt` 指向 Unity 6000.3.17f1，URP 项目。
- 主玩法场景：`Assets/Scenes/FirstViewScene.unity`，但 `EditorBuildSettings` 当前只包含 `SampleScene.unity`，构建配置存在风险。
- 核心入口：`Assets/FirstView/Scripts/FirstViewScene.cs`，负责开始界面、发牌布局、事件 wiring、AI 难度选择和流程编排。
- 核心玩法：`GameSession.cs` 状态机 + `Deck.cs` 发牌 + `ScoreSystem.cs` 比牌规则 + `EnemyAI.cs` 三档 AI。
- 关键规则文档：`Assets/ArtAsset/board_game_rules.md`，完整描述 15 张牌、7 回合、1 克 5、第三轮后看牌/换牌机制。
- AI 设计文档：`Assets/ArtAsset/board_game_ai_design.md`，规划 5 档 AI、BeliefModel、EvaluateMove、DifficultyProfile 等架构。
- 测试：`com.unity.test-framework` 已安装，但项目内没有任何实际测试文件。
- CI/构建脚本：未发现 GitHub Actions/GitLab CI/Jenkinsfile/Makefile/build script。

## Open Questions
- 下一步讨论优先级：补齐规则机制、完善 AI、补 UI/结算流程、搭建测试/构建，还是先做整体产品方向梳理？
- 用户新方向：借鉴《恶魔轮盘》用道具把随机赌博机制变得更有策略和戏剧性；希望围绕先手、后手、其他时机脑爆道具。

## Design Brainstorm Notes
- 核心设计目标：道具不只是“变强”，而是制造信息差、读心、反制、下注升级、风险交换和局势逆转。
- 道具分类建议：信息类、控牌类、比大小规则类、分数筹码类、回合顺序类、心理欺骗类、反制类、残局爆发类。
- 用户确认选中 12 个道具：随机线索、盖牌烟雾、下注加码、锁色、道具封锁、王牌化、最小即最大、平局获胜、赢家诅咒、手铐、放大镜、啤酒。
- 已生成道具系统设计草案：`.omo/drafts/item-system-design.md`。
- 用户澄清道具使用时机：只保留先手前、后手响应、揭示前、任意；删除结算前和回合间。

## Scope Boundaries
- INCLUDE: 项目代码和文档理解、风险梳理、讨论方向准备。
- EXCLUDE: 直接修改 Unity 代码、运行构建、创建测试或 CI。

# 技术选择与修复决策记录

按时间顺序记录关键决策，每条含「决策—理由」。

## D1. 开发验证在现有工作工程进行，仓库只归档工程子集
- 决策：代码开发与 Play Mode 验证在 `F:\Unity Projects\2D自走棋`（Unity 6000.6.4f1 工作工程）完成；仓库 `project/` 只归档可运行子集：`Assets/Scripts`、`Assets/Scenes`、`Assets/Settings`（URP 2D 管线资产，场景依赖）、`Assets/TextMesh Pro`（TMP Essentials，运行必需）、`Packages/manifest.json`、`Packages/packages-lock.json`、`ProjectSettings/` 全部。
- 理由：复用已连接 Codely Bridge 的运行中编辑器做 Play Mode 断言；排除 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`.codely-cli/` 等本地数据，保持仓库 8MB 内。

## D2. UI 使用 TextMeshPro，Essentials 由 eval 直接导入
- 决策：UI 文本用 TMP（符合本机全局偏好）。工程初建无 TMP Essentials，通过 Bridge eval 调 `AssetDatabase.ImportPackage` 导入 ugui 包内 `Package Resources/TMP Essential Resources.unitypackage`（Unity 6 的 TMP 内嵌于 `com.unity.ugui` 2.6.0，无独立 `com.unity.textmeshpro` 包，故未按 TMPChineseFont 技能默认路径查找）。
- 理由：用户指令要求全程不得停下询问，技术决策自行做出并记录。导入一次成功，验证 `TMP Settings.asset` 存在。
- 附带决策：UI 文案用英文/数字（`Gold: 77` 等），避免 TMP 默认 LiberationSans 缺中文字形；中文提示语（「金币不足」）保存在内部字段用于断言，UI 显示对应英文。

## D3. 棋子为纯数据对象
- 决策：`ChessPiece`/`PieceConfig` 是 `[Serializable]` 数据类，`bench` 为 `List<ChessPiece>`，不实例化 GameObject。
- 理由：切片无战斗与棋盘，省去 GameObject 生命周期管理；Inspector 直接可见；合成只是列表操作，状态断言简单可靠。

## D4. 2 星属性用 double 计算 floor(x×1.8)
- 决策：`(int)Math.Floor(config.baseHp * 1.8)`（double）而非 `Mathf.FloorToInt(x * 1.8f)`。
- 理由：float 的 1.8 有正负 epsilon，`200*1.8f = 359.9999…` 会错误下取整到 359；double 下 `200*1.8 = 360.00000000000004` 正确得到 360，`101*1.8 = 181.800…4` 得 181。全部整型输入安全。

## D5. 利息按结算前金币计算
- 决策：`EndRound()` 先记 `goldBefore` 算利息，再按「基础收入→利息→连胜/连败奖励」顺序入账。
- 理由：验收 B1 要求 gold=49 利息 4、gold=50 利息 5，即利息基数是本回合结算开始时的存款（与金铲铲规则一致）。

## D6. 单场景运行时装配 + 协程验收驱动
- 决策：场景只含一个挂 `GameBootstrap` 的空对象，Awake 时创建 `GameRoot`（挂 EconomyManager/ShopManager/BenchManager）与 UI Canvas（TMP 文本 + 2 按钮）；`PhaseRunner` 在 Play 开始后按协程顺序执行阶段 1→2→3 全部断言，逐条追加写 `results/playmode_log.txt`，静态字段 `finished/passedCount/failedCount` 供外部（Bridge eval）轮询。
- 理由：无需手工搭场景、可重复执行；一次 Play 出全量证据；eval 在 Play/Editor 域切换场景下读静态字段稳定。

## D7. 商店刷新随机填充、验证用 ForceSetSlot 固定输入
- 决策：`FillSlots()` 每槽 50/50 随机两种棋子，不扣卡池（切片边界）；验证器通过 `ForceSetSlot(i, id)` 固定槽位内容再调用 `TryBuyShopSlot`。
- 理由：购买/合成逻辑断言需要确定性输入，随机性仅保留在真实刷新路径中。

## D8. 修复记录（验收运行前）
- `GameBootstrap.cs` 中 `Button.onClick.AddListener(shop.TryRefreshShop)` 编译失败 CS0407（`UnityEvent` 需要 void 委托），改为 `() => shop.TryRefreshShop()`。发生在任何验收运行之前。
- PhaseRunner B3 账本初版存在「连胜奖励 +1 记账错误」与「校准步使断言同义反复」两处模型错误，在首次运行前重写为严格期望模型（streak=1 奖励 0；每步 gold/ones/twos/bench 全量核对）。此为开发期迭代，未产生 FAIL 断言记录。

## D9. Play Mode 触发与编辑器控制
- 决策：编译用 `tuanjie command editor refresh/compile`（等待导入与编译完成并返回诊断），进 Play 用 `tuanjie command editor play`（等待域重载稳定），结果用 eval 读 `PhaseRunner` 静态字段。
- 理由：后台编辑器不处理文件监视事件，必须显式 refresh；eval 在域切换瞬间会把 Play 退出到 Editor 域，故先 play 等稳定再轮询。

## D10. 仓库身份与远端
- 决策：仓库局部身份 `qiwuC <qiwuC@users.noreply.github.com>`（与全局一致，仍做局部配置）；远端 `https://github.com/qiwuC/jcc-eval-slice-economy-merge.git`，分支 main。
- 理由：评测任务指定。

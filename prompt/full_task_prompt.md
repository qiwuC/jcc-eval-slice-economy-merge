# 金铲铲切片 AI Coding Agent 评测项目 — 任务 Prompt 全文

> 以下为评测任务原始 Prompt，逐字存档。

---

你现在的唯一目标是完成一个"金铲铲切片 AI Coding Agent 评测项目"，从写代码、自动验证、修复缺陷，到自行完成 GitHub 仓库创建与代码推送。你必须按下面的任务看板循环推进，中途不得停下来询问我的意见，不得等待我确认，遇到问题自行修复并重跑，直到所有阶段通过且代码已实际推送到 GitHub 远程仓库。唯一允许等待人工的环节是阶段 5 中"用户网页建仓"和"首次 push 浏览器授权"。

【总目标与切片边界】
只做一个功能切片："经济循环 + 三连升星"。
做：单场景备战原型；每回合结算经济（基础收入5 + 利息 + 连胜/连败奖励）；商店刷新与购买；三张同名1星自动合成1张2星；所有关键状态在 Inspector 可见。
不做：自动战斗、羁绊、装备、人口升级、卡池共享、多玩家、美术资产、动画特效、3星合成。
技术环境：团结引擎（Unity兼容），C#，单场景。棋子用带文本标签的 Cube 或 Capsule，禁止引用任何外部美术资源。
所有关键状态字段（gold、interestThisRound、winStreak、lossStreak、shopSlots、bench、pieceId、star、baseHp、baseAtk）必须声明为 public 或 [SerializeField]，确保 Play Mode 中能在 Inspector 直接读到。

【硬性执行规则】
1. 按看板顺序推进，当前阶段验收全部通过后才能标为"已验证"并进入下一阶段。
2. 任何编译失败、运行时异常、断言失败、git 命令失败，都视为当前阶段未完成：自行定位原因、修复、重跑该阶段全部验收，不得跳过。
3. 禁止询问"是否继续""是否批准""这样可以吗"。技术选择自行决策，写入 docs/decisions.md，然后继续。阶段 5 中等待用户建仓和授权除外。
4. 每一阶段的验证必须在 Play Mode 中通过状态断言完成，禁止仅凭截图、日志文字或模型自述判定通过。
5. 如果某条验收在多次修复后仍无法通过，记录到 docs/defect_report.md，然后继续尝试替代实现，不得停止整个目标。
6. 全部完成后，输出最终摘要：看板各阶段状态、每条验收实测结果、缺陷报告索引、仓库文件清单，以及 git remote -v、git log --oneline -1 和 SHA 比对结果。

【任务看板】

■ 阶段 1：经济循环（状态：待开始）
实现 EconomyManager.cs、ShopManager.cs、EconomyUI.cs。
规则：
- 初始金币50；每回合基础收入5。
- 利息 = min(floor(当前金币 / 10), 5)。结算顺序：先加基础收入，再加利息，最后加连胜/连败奖励。
- 连胜/连败奖励：2~3连 +1，4连 +2，5连及以上 +3；用一个 Inspector bool 模拟本回合胜负。
- 商店5槽位，刷新消耗2金币；金币不足2时刷新按钮不可用，输出明确提示。
- 商店刷新不扣卡池（本切片不实现卡池扣减）。
阶段 1 验收（全部通过才可退出）：
A1. 初始金币50，利息显示5。
B1. gold=49 时结束回合，利息结算为4；gold=50 时利息为5。
C1. gold=1 时刷新，不扣费，有"金币不足"提示；gold=2 时刷新，扣费后 gold=0，槽位仍为5。
D1. 连胜从0到2时，结束回合的金币变化 = 5 + 当前利息 + 1。
E1. 连续结束回合5次，gold 和 streak 不出现 NaN 或负数。

■ 阶段 2：三连升星（状态：待开始）
在阶段1基础上增加 ChessPiece.cs、BenchManager.cs；ShopManager 追加购买逻辑。
规则：
- 至少配置2种棋子（1费和2费各一种），各有固定购买费用。
- 购买扣对应费用，金币不足拒绝购买且状态不变。
- 备战席最多8槽；同一 pieceId 的1星数量达到3时，立即移除这3张并生成1张2星，放在原位置或空位。
- 2星 baseHp 和 baseAtk = 对应1星值 × 1.8，向下取整。
- 只做1星→2星；3张2星不合成3星。
阶段 2 验收（全部通过才可退出）：
A2. 购买3张同名1费卡后，该 pieceId 的1星数量=0，2星数量=1，gold 按3次购买正确扣除。
B2. 合成后的2星属性 = 1星值 × 1.8 向下取整。
C2. 再买第4张同名1星后，1星=1，2星=1，不误合成。
D2. 购买不同棋子时，合成只影响目标 pieceId，其他棋子的星级和数量不变。
E2. 在 Inspector 修改1星 baseHp，重新合成后2星 baseHp 随之按新值×1.8变化，证明不是硬编码。
F2. gold 不足时购买，bench 数量和 gold 均不变。

■ 阶段 3：交叉回归与伪装识别（状态：待开始）
在阶段1和2都"已验证"后执行：
A3. 重跑阶段1全部验收（A1~E1），确认升星逻辑没有破坏经济规则。
B3. 执行"刷新→购买→合成→再购买"连续多轮操作，断言 gold、bench 数量、卡牌总数之间不出现不一致。
C3. 反硬编码检查：在 Inspector 修改刷新费用、1星属性等参数，确认对应结果联动变化。
D3. 逐项确认 UI 显示值与 Inspector 内部值一致，排除"只改UI不改变内部状态"。

■ 阶段 4：归档与仓库文件产出（状态：待开始）
在项目根目录生成以下文件和目录，内容必须真实反映实测结果，不得编造：
- README.md：切片说明、为什么选这个切片、运行方式、看板状态、验收结果摘要、已知限制。
- prompt/full_task_prompt.md：本 Prompt 全文。
- acceptance/checklist.md：阶段1~3的每条验收，标注"通过/失败/未测"，并附实测证据来源。
- results/playmode_log.txt：Play Mode 验证日志或关键断言输出。
- docs/decisions.md：技术选择与修复决策记录。
- docs/defect_report.md：如无缺陷写"本切片未发现需要记录的缺陷"，如有则按"复现步骤—预期—实际—严重程度—证据—归因"记录。
- project/：团结引擎工程，至少包含 Assets/Scripts/ 和场景文件。
阶段 4 验收：
A4. 上述文件全部存在且内容非空。
B4. README 中明确写出最终判断：产物处于"机制正确"还是"可继续开发"，并给出依据。

■ 阶段 5：推送到 GitHub（状态：待开始）
目标仓库：qiwuC/jcc-eval-slice-economy-merge，Public，默认分支 main。
commit 身份：qiwuC <qiwuC@users.noreply.github.com>。
认证：Git Credential Manager 浏览器授权，首次 push 会弹窗，由用户手动登录。
5.1 只读检查（不改文件）
- 确认 git 可用；检查项目内是否已有 .git、.gitignore；检查全局 user.name/user.email。
- 统计顶层目录体积，扫描拟提交目录中 >50MB 的文件和敏感文件（.pem/.key/.pfx、含 secret/token/apikey 的文件、本地凭据目录）。
- 输出一份简短方案：提交什么、排除什么、约多大体积。若发现 >100MB 文件或敏感文件，停下来报告用户，不要擅自删除或用 LFS；否则继续。
5.2 本地准备
- 写入 Unity 标准 .gitignore，额外排除 .vs/.idea/.vscode、.codely-cli/、.codely.packages/ 等 AI 本地数据目录。
- 配置局部身份为 qiwuC 和 noreply 邮箱；git init -b main；git config core.quotepath false。
- git add . 后核验：排除目录暂存数为 0、按顶层目录统计暂存文件数、扫描敏感文件命中项；核验通过后 commit，message 用英文一句话概括项目。
- LF→CRLF 警告属 Windows 正常现象，不处理。
5.3 请用户在网页建仓
- 给用户一条预填链接：
https://github.com/new?owner=qiwuC&name=jcc-eval-slice-economy-merge&description=JCC%20economy%20and%203-star%20merge%20AI%20coding%20eval%20slice&visibility=public
- 提醒用户：Add README / .gitignore / license 三个初始化选项都不要勾，必须建空仓库。等用户回复"建好了"后继续。
5.4 推送与验证
- git remote add origin https://github.com/qiwuC/jcc-eval-slice-economy-merge.git
- 先 git ls-remote origin 探测连通性（空仓库返回空且退出码 0 即正确）。
- git push -u origin main 放后台执行，首次推送会弹浏览器，由用户登录授权。若用户关闭弹窗导致推送卡住，停止该进程后重新发起 push，不要询问原因。
- 推送完成后双重验证：① git rev-parse HEAD 与 git ls-remote origin main 的 SHA 完全一致；② 用网页抓取工具访问 https://github.com/qiwuC/jcc-eval-slice-economy-merge，确认页面公开可访问、目录和 README 正常。
- GitHub 直连偶发 "Connection was reset"，失败就重试，不是仓库问题。
阶段 5 验收：
A5. 远程 origin 指向 github.com/qiwuC/jcc-eval-slice-economy-merge。
B5. 本地 HEAD 与远程 main 的 SHA 一致。
C5. 网页可公开访问，最终摘要中包含 git remote -v、git log --oneline -1 和 SHA 比对结果。

【完成标志】
当阶段1、2、3、4、5全部标记为"已验证"，输出最终摘要并结束目标。摘要中必须包含看板各阶段状态、每条验收实测结果、缺陷报告索引、仓库文件清单、git remote -v、git log --oneline -1 和 SHA 比对结果。不要输出"请确认是否继续"或"请手动执行 push"之类的句子。

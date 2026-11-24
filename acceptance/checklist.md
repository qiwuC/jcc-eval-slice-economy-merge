# 验收清单（阶段 1~3）

证据来源统一为 `results/playmode_log.txt`（三轮 Play Mode 会话追加记录，最终轮 `passed=73 failed=0`）。
断言均为 Play Mode 中对 Inspector 可见内部状态字段的直接读取比较，无截图/日志自述判定。

## 阶段 1：经济循环（13/13 通过）

| 编号 | 验收内容 | 结果 | 实测证据（log 行） |
| --- | --- | --- | --- |
| A1 | 初始金币 50 | 通过 | `PASS A1 initial gold == 50 \| gold=50` |
| A1 | 利息显示 5 | 通过 | `PASS A1 initial interestThisRound == 5 \| interest=5` |
| B1 | gold=49 结束回合利息=4 | 通过 | `PASS B1 gold=49 EndRound interest == 4` |
| B1 | gold=49 结算后 gold=49+5+4=58 | 通过 | `PASS B1 gold=49 EndRound gold == 49+5+4 \| gold=58` |
| B1 | gold=50 结束回合利息=5 | 通过 | `PASS B1 gold=50 EndRound interest == 5` |
| C1 | gold=1 刷新被拒 | 通过 | `PASS C1 gold=1 refresh rejected \| ret=False` |
| C1 | gold=1 刷新不扣费 | 通过 | `PASS C1 gold=1 no charge \| gold=1` |
| C1 | 「金币不足」提示 | 通过 | `PASS C1 insufficient-gold message contains 金币不足` |
| C1 | gold=2 刷新扣费后 gold=0 | 通过 | `PASS C1 gold=2 refresh succeeds and charges \| gold=0` |
| C1 | 刷新后槽位仍 5 | 通过 | `PASS C1 shopSlots count == 5 after refresh \| count=5` |
| D1 | 连胜 0→1 | 通过 | `PASS D1 winStreak becomes 1 after first win` |
| D1 | 连胜 1→2 结算增量=5+利息+1 | 通过 | `PASS D1 second win EndRound delta == 5 + interest + 1 \| delta=11 expected=11` |
| E1 | 连续 5 回合无 NaN/负数 | 通过 | `PASS E1 5 consecutive EndRounds stay finite and non-negative` |

## 阶段 2：三连升星（21/21 通过）

| 编号 | 验收内容 | 结果 | 实测证据（log 行） |
| --- | --- | --- | --- |
| A2 | 3 张 1 费卡购买成功 | 通过 | `PASS A2 three 1-cost buys succeed` |
| A2 | 合成后 1 星数量=0 | 通过 | `PASS A2 melee 1-star count == 0 after immediate merge` |
| A2 | 2 星数量=1 | 通过 | `PASS A2 melee 2-star count == 1` |
| A2 | gold 按 3 次购买扣减=47 | 通过 | `PASS A2 gold == 50 - 3*1 == 47` |
| A2 | 备战席数量=1 | 通过 | `PASS A2 bench count == 1` |
| B2 | 2 星 baseHp=floor(101×1.8)=181 | 通过 | `PASS B2 2-star baseHp == floor(101*1.8) == 181 \| hp=181` |
| B2 | 2 星 baseAtk=floor(11×1.8)=19 | 通过 | `PASS B2 2-star baseAtk == floor(11*1.8) == 19 \| atk=19` |
| C2 | 第 4 张后 1 星=1 | 通过 | `PASS C2 after 4th buy melee 1-star == 1` |
| C2 | 2 星仍=1 不误合成 | 通过 | `PASS C2 melee 2-star stays 1` |
| C2 | gold=46 | 通过 | `PASS C2 gold == 47 - 1 == 46` |
| D2 | 2 费合成 2 星=1 | 通过 | `PASS D2 ranger 2-star == 1` |
| D2 | 其他棋子星级数量不变 | 通过 | `PASS D2 melee unchanged (ones=1 twos=1)` |
| D2 | gold=46-3×2=40 | 通过 | `PASS D2 gold == 46 - 3*2 == 40` |
| E2 | 改配置后新 2 星=floor(200×1.8)=360 | 通过 | `PASS E2 new 2-star uses new config… \| hps=363,360` |
| E2 | 旧 2 星保持 363（非硬编码） | 通过 | `PASS E2 old 2-star keeps floor(202*1.8) == 363` |
| E2 | ranger 1星=0、2星=2 | 通过 | `PASS E2 ranger ones == 0 twos == 2` |
| E2 | gold=40-6=34 | 通过 | `PASS E2 gold == 40 - 3*2 == 34` |
| F2 | 余额不足购买被拒 | 通过 | `PASS F2 buy rejected when gold insufficient` |
| F2 | bench 数量不变 | 通过 | `PASS F2 bench count unchanged \| before=4 after=4` |
| F2 | gold 不变 | 通过 | `PASS F2 gold unchanged (== 0)` |
| F2 | 购买「金币不足」提示 | 通过 | `PASS F2 insufficient purchase message contains 金币不足` |

## 阶段 3：交叉回归与伪装识别（39/39 通过）

| 编号 | 验收内容 | 结果 | 实测证据（log 行） |
| --- | --- | --- | --- |
| A3 | 升星合入后 A1~E1 重跑全过 | 通过 | `PASS A3 re-run of A1~E1 after merge logic: all still pass \| newFailures=0`（其前 13 条 A1~E1 重跑明细均 PASS） |
| B3 | 4 轮混合操作账本逐步一致（刷新/购买/EndRound 共 10 个账本核对点） | 通过 | `PASS B3 ledger after …` 全部 `gold/ones/twos/bench` 实测=期望（48/47/56/54/53/51/50/48/47/56） |
| B3 | 第 3 轮购买触发合成 | 通过 | `PASS B3 merge fired in round3 \| merges=1` |
| B3 | 最终 gold 等于账本 | 通过 | `PASS B3 final gold equals ledger \| gold=56 expected=56` |
| B3 | 备战席不超容 | 通过 | `PASS B3 bench within capacity` |
| C3 | 刷新费用 2→4：gold=4 刷新扣 4 | 通过 | `PASS C3 refreshCost=4: gold 4 refresh succeeds and charges 4` |
| C3 | 刷新费用 4：gold=3 被拒 | 通过 | `PASS C3 refreshCost=4: gold 3 refresh rejected` |
| C3 | 拒绝提示含「金币不足」 | 通过 | `PASS C3 rejected message contains 金币不足` |
| C3 | 1星 baseHp=300 → 合成 2星=540 | 通过 | `PASS C3 1-star baseHp=300 -> merged 2-star hp == floor(300*1.8) == 540` |
| D3 | UI gold=内部值（77） | 通过 | `PASS D3 UI gold matches internal` |
| D3 | UI interest=内部值（5） | 通过 | `PASS D3 UI interest matches internal` |
| D3 | UI winStreak=内部值（3） | 通过 | `PASS D3 UI winStreak matches internal` |
| D3 | UI lossStreak=内部值（1） | 通过 | `PASS D3 UI lossStreak matches internal` |
| D3 | UI 备战席数=内部值 | 通过 | `PASS D3 UI bench count matches internal` |
| D3 | UI 合成数=内部值 | 通过 | `PASS D3 UI merges matches internal` |
| D3 | UI 提示语=内部字段 | 通过 | `PASS D3 UI message matches internal` |
| D3 | UI 商店槽位数=5 | 通过 | `PASS D3 UI shop slot count == 5` |

## 补充：阶段 4/5 状态

- A4（归档文件齐全非空）：通过 —— 本仓库即产物，`README.md`、`prompt/full_task_prompt.md`、`acceptance/checklist.md`、`results/playmode_log.txt`、`docs/decisions.md`、`docs/defect_report.md`、`project/`（Scripts 7 个 .cs + 场景 + URP 设置 + TMP Essentials + Packages 清单 + ProjectSettings）全部存在且非空。
- B4（README 最终判断）：通过 —— README「最终判断」节给出「机制正确（已验证），具备继续开发条件」及 4 条依据。
- A5/B5/C5（推送与远端验证）：见 git 提交后验证记录（commit 摘要与 SHA 比对见 README 看板状态及最终摘要）。

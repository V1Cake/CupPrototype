# Interaction Core — SPEC

Status: Product Review PASS

Next Stage: Ready for Architecture

Date: 2026-09-09

Source of Truth: [00_GRILL.md](00_GRILL.md) 的 Confirmed Decisions，包含 Capacity、Jigger Secondary Held、Measurement 无 Target Line 及 RMB 优先级的最终补充。

本文仅规定玩家交互、可观察结果及验收标准，不规定代码结构或实现方式。本轮产品评审补充 OPEN SHAKER 和 Ice Repeat Rule 后通过；本状态不授权本次创建 Architecture 或编码。P5-3 Selection Outline 保持 PASS / Frozen Prototype，未经明确 Ticket 不修改视觉实现。

## 1. Scope

覆盖 Acquire、Held / Put Down、Bottle Pour、Jigger Measurement、Jigger → Shaker、Reusable Shaker、Process Ice、Close / Open Shaker、Shake Gesture、Taste、Remake、Cup Selection、Serve Ice、Shaker → Cup、Service / Submit。

测试配方使用上述 Shake 主流程。本文中的状态名称表示玩家可理解的制作阶段，不是代码状态机设计。Deferred 见第 11 节。

## 2. Core Interaction Rules

### 2.1 Normal / Flair Acquire

| 当前对象与阶段 | 玩家输入 | 系统响应及完成状态 | Gameplay 变化 |
| --- | --- | --- | --- |
| 可取得的 Bottle / Jigger | 对象上单击 | 普通拿取，进入 Held | 改变持有关系，不改变液体内容 |
| 可进行 Acquire Flair 的对象 | 从对象上起笔画 Gesture，识别成功 | 播放对应 Flair，进入与普通取得相同的对象完成状态 | 不因花式额外改变饮品数据 |
| Acquire Gesture 识别失败 | 完成一次失败的取得手势 | 降级为 Normal Acquire | 仍取得对象，不阻断拿取 |
| Rest 状态的 Shaker | 普通取得或从对象起笔 Acquire Flair | 同一套 Shaker 到 Prep Position，Open / Preparing，结束后不占用 Held | 进入准备阶段 |
| Shake Complete 后 Cup Rack 中的杯子 | 普通取得或从杯子起笔 Acquire Flair | Cup 自动到固定 Serve Position | 记录实际 Selected Glass，进入出杯准备 |
| 已离开 Cup Rack 的 Cup | 普通取得 | 进入 Held | 改变持有关系；不再允许 Acquire Flair |

Selected 是明确指定的操作对象，Held 是完成取得后的持有关系；普通取得不要求先选中再点击第二次。Gesture 必须从目标对象上起笔，不能以空白起笔替代对象指定。

### 2.2 Held / Hand

- 稳定 Held 不要求持续按住鼠标，松键不会自动放下。
- 对象使用固定 Hand / Hold Anchor 和各自 Hold Pose，不跟随鼠标沿桌面滑动；是否存在手部模型不影响 Gameplay。
- 通常一个 Primary Held Object。唯一明确支持的持续双持组合为 Primary Bottle + Secondary Jigger。
- Jigger Secondary Held 是真实允许的持有关系，不能仅用第二只手的视觉替代。Support Hand 也可以参与临时动作表现。
- 不扩展为任意双持、任意交换或对象选择菜单。执行其他工具流程前，使用已确认的 Put Down 操作释放占用。

### 2.3 RMB / Put Down

| 当前稳定状态 | 输入 | 完成结果 |
| --- | --- | --- |
| 存在 Primary，可同时存在 Secondary Jigger | RMB | 仅 Primary 放下／归位，Secondary 保持 Held |
| 没有 Primary，仅有 Secondary Jigger | RMB | Jigger 放下并返回 Slot |
| Bottle + Jigger Held，Measurement 刚结束 | RMB | Bottle 回 Bottle Slot，Jigger 继续 Held，液体保持不变 |
| Jigger 自动 Transfer 完成后，只有 Jigger Held | RMB | 空 Jigger 返回 Slot |

Bottle / Jigger 返回自己的固定 Slot / Station。Cup 使用合法固定位置；Shaker 使用当前阶段的 Prep / Rest 规则，不允许自由桌面摆放。

Measurement、Auto Transfer、Flair、Shake Animation、Auto Pour 执行期间 RMB 不中断动作，不触发 Put Down。ESC 不作为 Put Down 输入。

## 3. Object-Specific Behavior

### 3.1 Bottle / Measurement

前置：Bottle 已 Held，目标为 Jigger；必须支持 Primary Bottle + Secondary Jigger 同时 Held 的量取。

| 输入／条件 | 玩家可见响应 | 最终状态与数据 |
| --- | --- | --- |
| 指向 Jigger，Hold LMB | Bottle 自动进入 Pour Pose，显示 Measurement Overlay，锁定 Camera Input | 固定流速向 Jigger 加入该 Bottle 材料 |
| 持续按住，Jigger 未满 | 放大液位持续更新，刻度清晰 | 按实际倒入量累计容量和材料 |
| 达到 Recipe 所需量 | 不自动停止、不限制继续倒液 | 玩家自行判断量取目标 |
| Jigger 达最大容量 | 自动停止继续加液 | 不超过 Jigger 最大容量 |
| Release LMB | 停止 Pour，Overlay 隐藏，相机解锁，Bottle 回 Hold Pose | 已量液体保留；Bottle / Jigger 持有关系不因松键清除 |
| 回到稳定状态后 RMB | Bottle 返回 Slot | Secondary Jigger 保持 Held，量取内容不变 |

Overlay MVP 核心为 Enlarged Jigger、Realtime Liquid Level、Clear Measurement Marks、Camera Lock。不显示实时 ml，不要求 Target Line。玩家依据 Recipe 判断目标，不因订单外材料、加多或加少被阻止；评分评价实际结果。

世界内动画只需合理表达倒液，精确量取反馈由 Overlay 承担。当前不将鼠标位移或倾斜角度映射为可控流速。

### 3.2 Jigger → Shaker / Capacity

前置：Jigger Held，Shaker 位于 Prep Position、Open / Preparing；标准双持量取后先 RMB 放回 Bottle。

Click Shaker → 自动倒入全部 Jigger 当前内容 → Jigger 为空并回到 Held Pose → RMB 返回 Jigger Slot。

- 不要求持续按住，不做第二次精确控量。
- Shaker 当前内容增加 Jigger 实际转入的全部材料与容量。
- Gameplay 不以 Shaker 容量上限拒绝或截断加料，多次 Jigger Transfer 均可继续接收。
- 现有 maxVolume 如何适配属于后续 Architecture，本文不指定处理方式。
- Jigger → Cup 不属于合法路径。

### 3.3 Reusable Shaker

| 阶段 | 玩家输入／触发 | 完成状态 |
| --- | --- | --- |
| Rest | Normal Acquire / Acquire Flair | 同一套 Shaker 到 Prep Position，Open / Preparing，Held 释放 |
| Preparing | Jigger Transfer，可选点击 Ice Well | 累积材料，独立记录 Process Ice |
| Open / Preparing | 点击 CLOSE SHAKER | 自动合盖，Ready To Shake |
| Ready To Shake | 从 Shaker 起笔 Shake Gesture，成功 | 执行固定动画，结束后 Shake Complete、Closed at Prep Position，保留内容，Held 释放 |
| Ready To Shake | Shake Gesture 失败 | 保持 Ready To Shake，允许重试 |
| Ready To Shake，Taste 前 | 点击 OPEN SHAKER | 自动开盖，Open / Preparing，保留液体及 Process Ice 等已有 Preparation Fact，可补料；之后再次 Close |
| Shake Complete，尚未 Taste / Pour | 点击 OPEN SHAKER | 自动开盖，Open / Preparing，清除 ShakePerformed，保留液体及其余 Preparation Fact；之后必须 Close 并重新成功 Shake |
| Shake Complete | 可选 TASTE 或继续准备 Cup | 饮品保留，不要求必须 Taste |
| Shake Complete / Closed at Prep Position | 普通取得 Shaker | Held Shaker，准备向 Cup 出杯 |
| Held Shaker，Cup 已准备 | Click Cup | Auto Strain / Pour，到源空或 Cup 满时结束，自动回 Rest / Reset |

有液体的 Shaker 在补料流程中可以回 Prep Position，再接收 Jigger 内容，不要求持有 Shaker 时进行多工具操作；接收仍遵守 Open / Preparing 条件，Taste 后不得补料修正。

重新打开 Shake Complete Shaker 时立即使本次 Shake 完成状态失效，即使尚未添加材料，也不能继续按旧 Shake Complete 状态 Taste / Pour。补料后必须再次 CLOSE SHAKER → Ready To Shake → 成功 Shake Gesture → Shake Complete；仅合盖不恢复 ShakePerformed。

出杯完成后清理 Shaker 内容与 Process 状态，下一杯复用同一套工具，无 Wash / Rinse Gameplay。

### 3.4 Cup

Shake Complete 后玩家自由选择 Cup Rack 中任意杯型。Normal Acquire 与 Rack 内 Acquire Flair 均将杯子送到固定 Serve Position。

离开 Rack 后仍可普通取得，但不允许再次 Acquire Flair。不因杯型与推荐值不符阻断流程，不自动换成推荐杯。

在 Serve Position 可加入 Serve Ice、接收 Shaker Pour；完成出杯后成为 Finished Cup，供玩家普通取得并提交。

## 4. Ice Rules

| 当前阶段 | 输入 | 动作与记录 |
| --- | --- | --- |
| Shaker Open / Preparing，尚未添加 Process Ice | Click Ice Well | 一次 Standard Ice Action，记录 Process Ice |
| Shake Complete，Cup 已在 Serve Position 的出杯准备阶段，尚未添加 Serve Ice | Click Ice Well | 一次 Standard Serve Ice Action，记录 Serve Ice |

两种冰独立，加入一种不能覆盖另一种的事实。Process Ice 在 Strain 时不随液体进入 Cup，也不自动生成 Serve Ice。

同一杯制作中，每种 Ice 为一次性的 Preparation Fact。对应 Ice 已添加后，再次点击 Ice Well 不重复增加 Gameplay Ice 或 Fact，可以拒绝或给简短反馈。OPEN SHAKER 保留已有 Process Ice，不重置该次制作的加冰资格；Remake 仍按整杯重置规则处理。

当前不控制冰量或冰型，不直接修改六维 Flavor。Interaction 记录制作事实；不加 Process Ice 也允许 Close / Shake，不自动补冰或 Spring。

## 5. Context UI Actions

MVP 使用固定屏幕 Context Button；下列名称表示操作，不冻结正式视觉文案、位置或风格。

| 操作 | 可用条件 | 点击响应 | 数据结果 |
| --- | --- | --- | --- |
| CLOSE SHAKER | Shaker Open 且 Preparing | 自动合盖 | Ready To Shake，保留内容与已有 Process Ice |
| OPEN SHAKER | Ready To Shake，Taste 前 | 自动开盖，回 Open / Preparing，可继续接收 Jigger 内容 | 保留液体和 Process Ice 等已有 Preparation Fact；之后必须重新 Close |
| OPEN SHAKER | Shake Complete，尚未 Taste / Pour | 自动开盖，回 Open / Preparing，允许补料 | 立即清除本次 ShakePerformed，保留液体及其余 Preparation Fact；必须再次 Close / Shake |
| TASTE | Shake Complete | 简短试味表现，可临时用 Support Hand，给一句定性反馈 | 不扣容量，不改变材料或已完成工艺 |
| REMAKE | 当前制作 Taste 后 | 清理当前制作，Shaker 回 Rest | 保留订单，重置当前 Jigger / Shaker / 未完成 Cup 相关状态及 Process Ice / Shake 等制作状态 |

Close 与加冰独立，不附带自动 Add Ice 或 Stir，不要求点击另一个 Tin。Taste 可完全跳过。

Taste 后 OPEN SHAKER 不可用；即使尝试触发也必须拒绝，不能重新进入补料状态。

## 6. Shake Gesture

Acquire Gesture 的失败回退只用于取得；Shake Gesture 失败不得调用该回退来自动完成 Shake。

前置：Shaker Ready To Shake。玩家从 Shaker 上起笔进行识别。

- 成功：播放该 Gesture 对应的固定 Shake / Flair Animation；Shaker 在动作中被持有，动画时长固定，不由玩家画线长度、持续拖动或累计距离决定。
- 动画完成：记录 ShakePerformed，进入 Shake Complete；Shaker 回 Prep Position、Closed、保留饮品内容，并释放 Held。
- 失败：不执行 Shake，不改变饮品容量、材料或工艺完成事实，保持 Ready To Shake，允许重新尝试。失败事实未来可用于剧情／角色反馈，本轮不要求制作该内容。
- 不同成功摇法可有不同动画、剧情或统计用途，当前不得导致不同品质、稀释、温度或 Flavor 结果。

## 7. Taste / Remake

Taste 是 modification cutoff。Taste 后玩家只能继续当前饮品的选杯／出杯流程，或者选择 Remake；不得 OPEN SHAKER 或通过补加材料修改已 Taste 的当前饮品。

Taste 仅提供一句定性反馈，不显示精确 Flavor 数值，不消耗实际容量，也不切换订单。

Remake 重做整杯：清空当前制作内容，清理 Jigger / Shaker / 未完成 Cup 的相关状态，重置 Process Ice / Shake 等制作事实，Shaker 回 Rest Position；订单保持原订单。玩家重新开始同一订单的制作，不操作 Sink 或 Wash Zone。

## 8. Serving / Cup Capacity

标准流程：Shake Complete → 可选 Taste → Cup Selection → 可选 Serve Ice → 普通取得 Shaker → Click Cup → Auto Strain / Pour → Shaker Auto Return / Reset。

### 8.1 自动出杯

- 持续转移到 Shaker 倒空或 Cup 达最大容量，先满足任一条件即停止继续 Transfer。
- Cup 剩余容量不足不拒绝整次 Pour；Cup 只接收其剩余容量允许的液体。
- Cup 满时余液留在 Shaker，不继续向 Cup 加液。
- 本次出杯结束，Shaker 的余液作为 Waste，在 Auto Return / Reset 时清理；不把 Waste 计入 Cup 接收量。
- Shaker 回 Rest Position，内容及 Process Reset，Held 清空。Cup 按实际收到的容量和材料进入 Finished、评分与 Submit 流程。
- Shaker Reset 不应抹掉当前成品用于评分的制作事实。

可验收数量例：Shaker 有 120 单位液体、Cup 剩余容量 80，实际转入 80；余下 40 在 Shaker Return / Reset 时作为 Waste 清理。若 Shaker 有 60、Cup 剩余容量 80，则转入 60，Shaker 倒空。数字仅为测试样例，不设定产品容量或评分数值。

### 8.2 Serve / Submit

Finished Cup → 普通取得 → Held Cup → Click Service Zone → 自动放到服务位并 Submit。

不增加第二次确认。错误杯型、Process Ice / Serve Ice 或材料比例误差不阻止提交；最终评价使用实际成品与制作事实，不自动修正成品。

## 9. Invalid Interaction

非法操作不修改容量、材料、Process State，不进入错误状态，可以提供简短 Context Feedback；不自动替玩家换工具、修正材料或完成制作。

| 操作 | 必须拒绝的结果 |
| --- | --- |
| Bottle 向 Shaker 或 Cup 倒液 | 不能添加液体或材料 |
| Jigger 向 Cup 转移 | 不能绕过 Shaker 出杯流程 |
| 向不满足 Open / Preparing 条件的 Shaker 补料 | 不能改变其内容或自动切回配料状态 |
| Shake Complete 前尝味 | 不能执行 Taste |
| Taste 后补料修正当前饮品 | 不能改变当前饮品 |
| Taste 后尝试 OPEN SHAKER | 必须拒绝开盖补料，不改变内容或 Preparation Fact，不返回 Open / Preparing |
| 重新打开 Shake Complete Shaker 后，尚未重新成功 Shake 就尝试 Taste / Pour | 不能沿用旧 Shake 完成状态执行操作 |
| 同一杯对应 Process Ice / Serve Ice 已存在时重复点击 Ice Well | 不重复增加 Gameplay Ice 或 Preparation Fact |
| Cup 离开 Rack 后尝试 Acquire Flair | 不能播放取杯 Flair；普通取得仍合法 |
| 任意物品自由双持或自由摆放 | 不能进入未支持的持有／摆放状态 |

订单外材料、配方量偏差、错误杯型、选择不加冰以及 Cup 容量不足时的受限出杯均不是上述非法操作，不得一概拦截。

RMB 在指定 Action 期间不承担中断职责；Acquire Gesture 失败回退和 Shake Gesture 失败重试按各自专门规则处理。

## 10. Gameplay Data Requirements

需能表达与验收以下事实，不指定字段或存储结构：

- 当前指定对象、Primary Held Object、Secondary Jigger Held 及各对象的位置／持有关系。
- Jigger、Shaker、Cup 的实际液体容量及材料内容。
- Shaker 当前制作阶段，Open / Closed、ShakePerformed。
- 从 Shake Complete 重新打开时立即清除本次 ShakePerformed；液体及 Process Ice 等其余已存在事实保留。再次成功 Shake 完成后才重新记录 ShakePerformed，仅 Close 不恢复它。
- Process Ice、Serve Ice、Selected Glass，三者独立。
- 同一杯 Process Ice / Serve Ice 各自只记录一次；重复点击不累计，重新开盖不清除已有 Ice Fact。
- 当前饮品是否已 Taste、是否 Finished、是否已 Submitted，以及所属订单。
- Cup 实际接收到的内容与 Shaker 本次出杯清理的余液；不要求新增 Waste UI 或统计系统。
- Shaker 复用 Reset 后，成品评分所需的本杯制作事实仍可用。

Interaction 提供事实，Scoring 决定可调整的 Penalty / Score；不在 Interaction 固定扣分值，不因具体 Shake Gesture 改变品质。Remake 重置当前制作但保留订单。

## 11. Deferred / Out of Scope

- Garnish、Top-up、Bottle → Cup、Soda / Tonic Mixer。
- Stir / Mixing Glass、Wash / Rinse Gameplay、Spring。
- 自由桌面摆放、冰量控制、不同冰型。
- 任意两物品自由双持、双手任意交换、复杂 Hand Switching 或双持选择菜单；唯一持续双持例外为 Primary Bottle + Secondary Jigger。
- 不同 Shake Gesture 对最终品质的差异。
- Measurement Target Line：仅未来可选 UX Aid，不属于当前 MVP 或验收要求；未来也不得限制 Pour 或自动停液。
- 完整 Tutorial、正式 Hand Animation、正式 UI Visual Design。
- Class / Controller 拆分、代码状态机、Event / Interface、现有代码移动或重构、maxVolume 的实现适配。

## 12. Acceptance Criteria

以下为未来实现的验收要求，本次仅核对规格，不代表已运行通过。

| ID | 玩家行为／测试前提 | 应观察到的结果 |
| --- | --- | --- |
| AC-01 | 单击可取得 Bottle，松开鼠标 | Bottle 完成普通取得并持续 Held，固定 Hold Pose，不跟鼠标滑动 |
| AC-02 | 从可取得对象起笔，分别完成成功和失败的 Acquire Gesture | 成功播放 Flair；失败普通取得；均到对象规定的完成位置／状态 |
| AC-03 | 取得 Primary Bottle 与 Secondary Jigger | 可同时 Held；不要求两个通用独立持有系统或任意双持 |
| AC-04 | Bottle + Jigger Held，Measurement 结束后 RMB | 只归还 Bottle，Jigger 保持 Held 且内容不变 |
| AC-05 | 仅 Secondary Jigger Held 时 RMB | Jigger 返回 Slot；不要求手部选择菜单 |
| AC-06 | Measurement / Auto Transfer / Flair / Shake Animation / Auto Pour 期间分别按 RMB | 不打断当前动作或放下对象；ESC 不作为 Put Down |
| AC-07 | Held Bottle 指向 Jigger，按住 LMB 再松开 | 固定流速加液；松开立即停止，保留量取结果，Bottle 回 Hold Pose |
| AC-08 | Measurement 开始并松键结束 | 显示放大 Jigger、实时液位、清晰刻度并锁相机；松键隐藏 Overlay 并解锁；不显示实时 ml；Target Line 不作为 MVP 验收要求 |
| AC-09 | 连续倒入超过 Recipe 目标量直至 Jigger 满 | 配方目标不自动停，最大容量停止继续加液，不超出 Jigger 容量 |
| AC-10 | 使用订单外材料，或相对 Recipe 加多／加少 | 不因订单／目标量被阻止，记录实际结果供评分 |
| AC-11 | Bottle 指向 Shaker / Cup，或 Jigger 指向 Cup 尝试倒液 | 容量、材料、工艺状态不变，无自动纠正 |
| AC-12 | 量取后 RMB 放 Bottle，Held Jigger 单击 Open / Preparing Shaker | 一键转入 Jigger 全部内容，Jigger 为空且仍 Held，之后 RMB 可归位 |
| AC-13 | 多次向 Shaker 转移，使总量超过旧 Shaker 容量限制 | 每次 Jigger 内容均完整接收，不以 Shaker 容量上限阻挡或截断 |
| AC-14 | 普通或 Flair 取得 Rest Shaker | 同一套工具到 Prep Position，Open / Preparing，释放 Held |
| AC-15 | 分别制作无 Process Ice 和有 Process Ice 两条路径，点击 CLOSE SHAKER | 两者均可合盖并 Ready To Shake，不自动补冰或 Spring |
| AC-16 | Ready To Shake 时从 Shaker 起笔，识别失败 | 不完成 Shake、不改变饮品，保持 Ready，能重新尝试 |
| AC-17 | 成功识别 Shake Gesture | 播放对应固定时长动画；完成后记录 ShakePerformed，Closed Shaker 回 Prep，内容保留，Held 清空 |
| AC-18 | 相同原料／制作事实，使用不同成功 Shake Gesture | 允许动画不同，不因摇法产生不同品质、温度、稀释或 Flavor |
| AC-19 | Shake Complete 后跳过 TASTE，直接选杯 | 可继续合法出杯，不强制 Taste |
| AC-20 | 点击 TASTE | 简短定性反馈，不显示精确 Flavor，不扣液体，不改订单；之后补料修正被拒绝 |
| AC-21 | Taste 后点击 REMAKE | 清理当前 Jigger / Shaker / 未完成 Cup 及制作状态，Shaker 回 Rest，仍制作同一订单 |
| AC-22 | 选非推荐杯型；再对离开 Rack 的杯尝试 Flair | 杯型不阻断流程；Rack 外不再允许 Acquire Flair，但可普通取得 |
| AC-23 | Preparing 加 Process Ice，选杯后加 Serve Ice | 两者独立记录；Strain 不把 Process Ice 当成 Serve Ice；不直接改变六维 Flavor |
| AC-24 | Shaker 液量少于 Cup 剩余容量，Click Cup | 全部液体进入 Cup，Shaker 倒空后自动归位 Reset，Cup 使用实际收到的量 |
| AC-25 | Shaker 液量大于 Cup 剩余容量，Click Cup | 不拒绝整次 Pour，Cup 满停，余液留 Shaker 后在 Return / Reset 清为 Waste，Cup 不超容量 |
| AC-26 | 完成出杯，Shaker 已 Reset | Cup 成为 Finished，保留实际成品与评分需要的本杯制作事实；下一杯复用同一 Shaker |
| AC-27 | 取得 Finished Cup 并 Click Service Zone | 自动放杯并 Submit，无第二次确认；制作误差由评分评价 |
| AC-28 | 检查模块边界 | 无新增 Deferred 玩法、正式教学／UI／手部资源要求，无 P5-3 冻结视觉改动，评分数值未在 Interaction 写死 |
| AC-29 | Ready To Shake，记录已有液体及 Process Ice，点击 OPEN SHAKER → Jigger 补料 → CLOSE SHAKER | 自动开盖到 Open / Preparing，原内容和 Preparation Fact 保留，可全量接收 Jigger；再次 Close 才回 Ready To Shake |
| AC-30 | Shake Complete 且尚未 Taste / Pour，点击 OPEN SHAKER，先不补料 | 开盖到 Open / Preparing 时 ShakePerformed 已清除，液体与其余 Preparation Fact 保留；不能用旧完成状态 Taste / Pour |
| AC-31 | 接续 AC-30 补料 → Close → 尝试 Taste / Pour → 失败 Shake → 成功 Shake | 仅 Close 不恢复 ShakePerformed；Taste / Pour 在重新完成 Shake 前不可执行；失败仍 Ready，成功动画完成后才重新 Shake Complete 并可继续流程 |
| AC-32 | Taste 后分别尝试 OPEN SHAKER 和 Jigger 补料 | 均拒绝，容量、材料及 Process State 不变；仍可正常出杯或 REMAKE |
| AC-33 | 同一杯已加 Process Ice，再次点击 Ice Well；Close / Open 后再点 | 不重复增加 Gameplay Ice 或 Fact，已有 Process Ice 保留；可拒绝或短反馈 |
| AC-34 | 同一杯出杯准备阶段已加 Serve Ice，再次点击 Ice Well | 不重复增加 Gameplay Ice 或 Fact，不影响 Process Ice；无冰量或冰型控制 |

本轮闭环核对：AC-29 覆盖 Ready → Open → 补料 → Close；AC-30 / AC-31 覆盖完成状态失效、禁止提前出杯及重新 Shake；AC-32 与 AC-21 / AC-24～AC-27 覆盖 Taste 后拒绝修改、Remake 或继续出杯；AC-33 / AC-34 与 AC-23 覆盖两种 Ice 独立且不重复。验收为文档规则核对，未运行 Gameplay 测试。

## 13. Remaining Open Questions / Review Gate

当前没有阻塞上述核心流程和验收标准的未确认产品问题。

以下保持 TBD，不由本规格代定：Tutorial 具体流程，正式 Context UI 文案／位置／风格，Hand Animation 细节，Gesture Template 具体资源，Scoring Penalty 数值。

已与 PROJECT_STATUS / DECISION_LOG 核对：项目级文件描述已验证 Prototype 与开发顺序；本规格描述已确认的新产品行为，未发现未协调的产品冲突。P5-3 冻结约束不变。

本轮补充已完成产品评审，Status: Product Review PASS / Ready for Architecture。未发现新的主流程歧义；获得下一阶段授权后可进入 Architecture。本次不创建 Architecture，不拆实现 Tickets，不编码。

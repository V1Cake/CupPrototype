# Interaction Core Re-Baseline — GRILL

Status: Product Decisions Complete

Next Stage: Ready for SPEC

Decision Closeout: 2026-09-09

当前仍禁止编码及创建 ARCHITECTURE，不拆 Implementation Tickets。本次授权先补充 GRILL，再在无主流程歧义时创建 SPEC；发现主流程歧义时停止并报告。

本文记录已确认的产品行为，不规定 Controller、Class、数据结构或实现方案。依据为「项目结构校准」中已确认的核心产品讨论及本次 Additional Confirmed Decisions；最终补充决策优先。旧 Prototype 的实际行为不自动成为新产品规则。

## Confirmed Decisions

### 1. Selection / Acquire

- 普通取得：从对象上单击 → 普通 Acquire 表现 → 进入 Held。
- Flair 取得：Gesture 必须从目标对象上起笔；成功后播放对应 Acquire / Flair 动作，进入该对象对应的完成状态。
- Acquire Gesture 失败：降级为普通 Acquire，不阻止取得对象。
- 普通 Acquire 与 Flair Acquire 共用同一套 Gameplay 对象逻辑，主要区别为动作表现；Shaker 开局预备、Cup 从杯槽进入 Serve Position 的完成状态见下文。
- Selected 表示已明确指定、准备操作的对象；Held 表示已完成取得、处于玩家控制中的对象。普通操作不要求先选中再另点一次拿起。
- Gameplay 不依赖 Hand Visual 是否存在。
- P5-3 Selection Outline 为 PASS / Frozen Prototype。保留 Selected / Held 的视觉能力，未经后续明确 Ticket 不修改其视觉实现；Hover 当前不实现。

### 2. Held / Hand / Fixed Placement

- MVP 通常只有一个 Primary Held Object，但撤销“任何时候全局只能存在一个 Held Object”的绝对规则。Jigger 是明确的 Secondary / Support Hand Held 例外，必须支持 Jigger Held + Bottle Held，用于 Bottle 向 Jigger Measurement。稳定 Held 不要求持续按住鼠标，松开取得点击不自动放下。
- Held Object 使用固定 Hand / Hold Anchor，不再跟随鼠标沿桌面平面移动。
- Bottle / Jigger / Shaker / Cup 可以拥有不同 Hold Pose；无手模型时仍使用持握锚点。
- Support Hand 可以参与动作表现，并允许上述 Jigger Held 例外，不再仅限视觉辅助。
- 当前不扩展为任意两个物品自由双持、双手任意交换或两套独立通用 Held 状态机，也不设计复杂手部物理。
- Bottle / Jigger 使用固定 Slot / Station；Shaker 区分 Rest Position 与 Shaker Prep Position。
- Cup 从 Cup Rack / Slot 取得后进入固定 Serve Position，之后仍可普通拿起；离开杯槽后不再允许 Acquire Flair。
- 当前不实现自由桌面摆放。

### 3. Put Down / Return

- 稳定 Held 状态下，RMB → Put Down / Return。
- Bottle / Jigger 等固定工具自动返回自己的固定 Slot / Station。
- Cup 使用当前合法固定位置，不引入自由摆放；Shaker 的阶段性自动归位遵循下述流程。
- Measurement、自动 Transfer、Flair、Shake Animation、Auto Pour 等 Action 执行期间，RMB 暂不承担中断职责。
- ESC 不作为 Put Down 输入。

#### RMB with Primary + Secondary Held

当前 MVP 唯一明确支持的持续双持组合为 Primary Held Object = Bottle、Secondary Held Object = Jigger。

稳定状态下 RMB 的优先级：

1. 存在 Primary：Put Down / Return Primary，Secondary 保持 Held。
2. 不存在 Primary、仅存在 Secondary：Put Down / Return Secondary。

标准流程：Bottle + Jigger Held → Release LMB 停止 Measurement → RMB → Bottle 返回 Bottle Slot，Jigger 保持 Held → Click Shaker → Jigger 自动全部 Transfer → RMB → Jigger 返回 Slot。

Measurement、Auto Transfer、Flair、Shake Animation、Auto Pour 执行过程中，RMB 仍不承担中断职责。不扩展为任意双持对象选择菜单或复杂 Hand Switching。

### 4. Bottle Pour

当前合法配料路径只开放 Bottle → Jigger；Bottle → Shaker、Bottle → Cup 均拒绝。Top-up 不实现。

Held Bottle → 指向 Jigger → Hold LMB → Bottle 自动进入 Pour Pose，以固定流速持续加液；必须支持 Jigger 同时处于 Secondary / Support Hand Held。

Release LMB → 停止倒液 → Bottle 回 Hold Pose；之后 RMB 放下时返回自己的 Bottle Slot。

玩家控制开始与停止，不以鼠标位移或倾斜角度控制流速。

### 5. Jigger Measurement

- 必须支持 Bottle Held 与 Jigger Secondary / Support Hand Held 同时存在的 Measurement，不再以“Jigger 只能是固定 Measurement Target”排除手持量取。
- 开始 Pour 时出现独立 Measurement Overlay，覆盖或弱化正常第一人称画面，并临时锁定 Camera Input。
- MVP Overlay 核心要求为 Enlarged Jigger、Realtime Liquid Level、Clear Measurement Marks、Camera Lock；不显示实时 ml 数字，不要求 Target Line。
- 玩家根据 Recipe 自己判断量取目标；Interaction 不阻止使用订单外材料、加多或加少，这些结果交由后续评分处理。
- 玩家通过 Release LMB 自行停液；达到 Jigger 最大容量时自动停止加液，不因配方目标量自动停止。
- Release LMB 后 Overlay 隐藏、Camera Input 解锁，返回正常画面和 Bottle Hold Pose。
- 世界内 Bottle / Jigger 动画提供合理表现，精确量取反馈由 Overlay 承担。
- Target Line 仅保留为未来可选 UX Aid，不属于当前 MVP 或 Acceptance Criteria；即使未来加入，也不能限制 Pour 或自动停止。

### 6. Jigger → Shaker

放下 Bottle → 普通取得 Jigger → Held Jigger 点击 Open / Preparing Shaker → 自动执行倒入表现，将 Jigger 当前内容全部 Transfer 到 Shaker。

上述为普通取得路径；Measurement 中已 Held 的 Jigger 不需要被视为尚未取得。

当前 Gameplay 不使用 Shaker 容量上限阻止加料，Shaker 可以继续接收多次 Jigger Transfer，每次接收 Jigger 当前全部内容。底层如何处理现有 maxVolume 属于 Architecture 问题，本阶段不修改代码。

不要求持续按住，不进行第二次精确容量控制。倒完后 Jigger 为空并回到 Held Pose；玩家 RMB 放下后归位。Shaker 可以重复接收多个已量好的 Jigger 内容。Jigger → Cup 不属于本轮合法链路。

### 7. Reusable Boston Shaker

使用同一套可复用 Boston Shaker，不为每杯生成一套新工具。

每杯开始：

Shaker Rest Position → 玩家可从 Shaker 上起笔进行 Acquire Flair → 移动到固定 Shaker Prep Position → 桌面 Open / Preparing → Held 清空。

这是取得后落到预备位的明确例外，不保持长期 Held；随后玩家拿 Bottle / Jigger 配料。统一使用 Shaker Prep Position，替代讨论早期的 Mixing Position 称呼。

完成出杯后 Shaker 自动回 Rest Position，内容和 Process 状态 Reset，下一杯继续复用。当前 Demo 不实现 Wash / Rinse Gameplay。

有液体的 Shaker 如果需要继续补料，可以回到 Shaker Prep Position 后继续接收 Jigger 内容，不要求玩家一手持 Shaker 再进行复杂多工具操作。Taste 后禁止补料修正的规则仍保留。

### 8. Process Ice / Serve Ice

- Process Ice 与 Serve Ice 分开记录。
- Shaker Open / Preparing 阶段点击 Ice Well → 自动执行一次 Standard Ice Action → 记录 Shaker Process Ice。
- Shake Complete 后，Cup 已在 Serve Position 的出杯准备阶段点击 Ice Well → 自动加入 Standard Serve Ice → 记录 Cup Serve Ice。
- 加冰目标由上述制作阶段确定。
- Process Ice 和 Serve Ice 均为一次性的 Preparation Fact。同一杯制作中，对应 Ice 尚未添加时点击执行标准加冰动作；已添加后再次点击 Ice Well 不重复增加 Gameplay Ice 或 Fact，可以拒绝或给简短反馈。两者分别判断，当前不控制冰量、多冰 / 少冰或冰型。
- Process Ice 属于 Preparation / Process 事实，当前不直接修改六维 FlavorProfile，不模拟其温度或稀释差异。
- Strain 时 Process Ice 不随液体转入成品杯，不自动等同于 Serve Ice。
- 不加冰也允许 Close 并进入 Shake；不自动加入 Spring。

### 9. Close Shaker

MVP 使用固定屏幕 Context UI。

Shaker Open 且处于 Preparing 状态 → 显示 [CLOSE SHAKER] → Click → 自动执行 Boston Shaker 合盖表现 → Ready To Shake。

加冰与 Close 是独立操作，Close 不自动替玩家加冰或 Stir。未来可考虑点击闲置的另一个 Tin 作为替代入口，当前不实现。

#### Reopen Shaker Before Taste

在 Taste 之前，允许通过固定 Context UI [OPEN SHAKER] 重新打开已合盖的 Shaker 补料。

- Ready To Shake：点击 OPEN SHAKER → 自动开盖 → Open / Preparing → 可继续接收 Jigger 内容；保留液体及 Process Ice 等已有 Preparation Fact。之后必须再次 CLOSE SHAKER 才回 Ready To Shake。
- Shake Complete 且尚未 Taste / Pour：点击 OPEN SHAKER → 自动开盖 → Open / Preparing → 清除本次 ShakePerformed，保留已有液体和 Process Ice 等其余 Preparation Fact，允许补料。清除发生在重新打开时，不等到实际补料。
- 从 Shake Complete 重新打开后，必须再次 CLOSE SHAKER → Shake Gesture → Shake Complete，不能沿用补料前的 Shake 完成状态。
- 执行 Taste 后不允许 OPEN SHAKER 或补料修改当前饮品，只能继续出杯或 REMAKE 整杯；保持 Taste = modification cutoff。

### 10. Shake Gesture

Acquire Flair 与 Shake Gesture 是两个不同 Gameplay 场景。

Ready To Shake → 从 Shaker 上起笔 → Gesture Recognition。

成功：

- Shaker 在 Shake 动作期间被持有，播放该 Gesture 对应的固定 Shake / Flair Animation。
- 动画时长固定，不要求玩家持续来回拖动或累计移动距离。
- 完成动画 → ShakePerformed = true → Shake Complete。
- Shaker 自动回 Shaker Prep Position，保持 Closed，保留饮品内容，Held 清空。

失败：

- 不执行 Shake，不降级为自动成功。
- Shaker 保持 Ready To Shake，玩家可以重新尝试。
- 失败事件未来允许用于剧情或角色反馈。

不同 Shake Gesture 可使用不同动画，可用于剧情、表现或统计；当前不影响成品品质，不产生不同稀释、温度或 Flavor 结果。评分可以关心是否完成 Shake，不因具体摇法产生品质差异。

### 11. Taste

Shake Complete 后显示固定 [TASTE]。Taste 为可选操作，玩家可以跳过并继续选杯出杯。

点击 Taste → 自动播放简短试味表现，可临时使用 Support Hand → 提供一句定性味觉反馈。

不显示具体 Flavor 数值，不实际扣除容量，不允许向当前饮品补料修正。Taste 不改变已完成的制作状态，之后可继续出杯或 Remake。

具体反馈文案属于后续 UI / Content 范围，不在 GRILL 固定示例。

### 12. Remake

Taste 后允许玩家选择固定 [REMAKE]。

点击后：

- 清空当前制作中的饮品。
- 清理 Jigger / Shaker / 未完成 Cup 的相关状态。
- Reset Process Ice、Shake 等制作状态。
- Shaker 回 Rest Position。
- 保留当前订单，玩家重新制作同一杯。

当前不实现手动倒进 Sink / Wash Zone 的重做流程。

### 13. Cup / Shaker → Cup

Shake Complete → 可选 Taste → 从 Cup Rack 自由选择杯型 → 普通 Acquire 或从杯槽起笔的 Acquire Flair → Cup 进入固定 Serve Position → 可选 Serve Ice。

玩家可选择任意杯型，选错不阻止制作，交由后续评分处理。Cup 离开杯槽后只能普通拿起，不再触发取杯 Flair。

出杯时，普通取得 Prep Position 上 Closed 且 Shake Complete 的 Shaker → Held Shaker 点击 Cup → 自动 Strain / Pour，持续至 Shaker 倒空或 Cup 达到最大容量。

Cup 容量不足时不拒绝整次 Pour。Cup 装满后停止 Transfer，多余液体保留在 Shaker；本次出杯完成后，余液作为 Waste，在 Shaker Auto Return / Reset 时清理。

不要求玩家控制倒液角度、速度或容量。完成后自动播放放回表现，Shaker 回 Rest Position，清空内容（包括 Waste）并 Reset Process，Held 清空；Cup 按实际接收到的内容成为 Finished Drink，进入后续评分与 Submit。

### 14. Serve / Submit

Finished Cup → 普通拿起 → Held Cup → 点击 Service Zone → 自动放杯并同时 Serve / Submit。

不增加第二次 Confirm Submit。错误杯型、Process Ice / Serve Ice 选择或比例误差不阻止提交，由评分系统评价；不将这些结果偏差混同于非法 Interaction。

### 15. Error Handling

非法 Interaction 不修改 Gameplay 数据，不切换到错误状态；可以显示简短 Context Feedback。当前不自动替玩家修正操作。

Acquire Gesture 失败的普通取得回退是已确认的专门规则；Shake Gesture 失败则保持 Ready To Shake，不混用两者的处理方式。

### 16. Preparation / Scoring

Interaction Core 只记录制作事实，例如 Process Ice、Serve Ice、Selected Glass、ShakePerformed 及其他 Preparation State。

这些事实可以参与最终评分。具体 Penalty / Score 数值必须可调整，不在 Interaction 中写死；当前不确定具体扣分值，也不在本轮 GRILL 重定六维 Flavor 或评分公式。

### 17. Demo UI Principle

OPEN SHAKER、CLOSE SHAKER、TASTE、REMAKE 当前优先采用固定屏幕 Context Button，以降低 Demo 开发复杂度。

具体视觉样式、位置、动画及反馈文案不属于本次核心产品收口所需的 Architecture 决策，不在本文指定实现。

## Deferred

本轮不实现：

- Garnish。
- Top-up。
- Bottle → Cup。
- Soda / Tonic Mixer。
- Stir / Mixing Glass。
- Wash / Rinse Gameplay。
- 自由桌面摆放。
- 冰量控制。
- 不同冰型。
- Spring。
- 任意物品自由双持、双手任意交换及两套独立通用 Held 状态机（Jigger Secondary / Support Hand Held 为本轮明确例外）。
- Measurement Target Line（仅未来可选 UX Aid）。
- 不同 Shake Gesture 对最终品质的差异。

测试配方采用本轮 Shake 主流程可完成的配方，不因特殊配方新增上述交互分支。

## Remaining Open Questions

容量、Measurement Target Line 及双持例外的 RMB 作用对象均已确认，当前无阻塞核心流程的待决问题。

以下细节保持待定，不自行补全：

- UI 的具体位置、样式、动画，以及 Taste / Context Feedback 文案。
- Preparation / Scoring 的具体权重及 Penalty 数值。
- 最终 Hand / Support Hand 动画细节，以及具体 Gesture 模板和动画资源清单。
- 原 GRILL 的 Tutorial 清单尚无逐项确认记录：常驻操作 HUD、首杯剧情教学、Context Hint 时机及剧情临时锁定 Interaction 的具体策略，保留为后续教学范围问题，不据此新增本轮核心交互规则。

## Conflicts Found

未发现无法直接协调的旧产品规则冲突。原 00_GRILL.md 为待讨论清单，已以确认结果替换。

讨论中的旧 Mixing Position 称呼、单右手限制、Shake 后继续长期 Held、不同摇法影响品质的提议，均由后续明确决策更新；本文采用最终规则。

现有 Prototype 的桌面 Drag、Shift 移液、位移累计 ShakeLevel、Bottle / Jigger 直达 Cup 等行为与新基线的差异属于已确认的 Re-Baseline 范围。本次仅记录产品决策，不修改这些实现或 P5-3 视觉。

## Final GRILL Status

Product Decisions Complete

Ready for SPEC

核心产品决定已收口，可以按独立授权创建 SPEC。以上非阻塞细节不在本轮擅自定案；本状态不是实现许可，也不代表 SPEC / ARCHITECTURE 已通过。

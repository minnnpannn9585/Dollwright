# Dollwright · 工作台与快乐梦境原型

Unity 2022.3.62f3c1，2D URP。第一关文件为 Scenes/Dream01_Graybox.unity。本轮在用户编辑后的第一关上定点修复，并连接 Workbench 场景。没有重新生成关卡。

## 打开和操作

1. 打开 Scenes/Workbench.unity，等待 Unity 编译完成。也可通过 **Dollwright → Graybox → Open Dream 01** 单独测试第一关。
2. 点击顶部 ▶ Play。工作台右下角点击小床“入梦”，镜头靠近展示架后，点击架上的白色人偶进入梦境。按 Esc 或点击“返回工作台”可取消选择。
3. **A/D 或方向键**行走；贴着箱子行走即可推箱。**空格**跳跃，长按跳得更高。
4. 泡泡摊右侧出口按 **E** 醒来，约 0.6 秒后返回工作台；**R** 清空本次梦境记录并重开。
5. 退出 Play 后再调整场景。Play 中的 Inspector 修改不会保存。

## 第一关路线

入口步行 → 必需推箱登台/B1 → 彩旗悬空平台/F1 → 马戏团推箱/F2 → 踩箱登上抬高的平台 → 连续攀登魔术箱 → 箱顶 H1（高潮） → 宽平台下降/B2 → 经过魔术师彩蛋预留入口 → 泡泡摊推箱/B3 → 连平台 → 醒来出口。

- 第一只箱子初始位置距离高台过远；高台 3.2 单位，超出角色直接跳跃高度。把箱子推到高台边，再踩箱登台。
- 第一只和泡泡摊箱子使用安全移动范围；第二只箱子可以推落并复位。三只箱子两侧均能推，踩在箱顶不会推动箱子。柜台、高台都是实体碰撞。
- 彩旗段与攀登段下方撤去托底地面。掉落回本段检查点，停顿 1 秒恢复操作，保留材料。
- 魔术箱顶部高度 8，是主路线最高点；取得魔术师头部后进入宽平台下降段。
- 两处魔术师提示保持实体屏障，第二关和穿墙能力尚未实现。星纹入口属于后续二次探索的预留位置。
- 醒来门在泡泡摊之后，门前落脚区域与摊顶齐平，移除了右侧边界前的小坑。到门口按 E 返回工作台，H1 仍是通关条件。
- B1/B2/B3 为素胚，F1/F2 为面孔，H1 为头部。取材不会重复计数。
- 雾随前进散开，已探索空间不再遮回；接近推箱区时提前显示目标高台/材料，接近马戏团时额外露出魔术箱地标。
- 镜头保持正交侧视，平滑跟随登高，不旋转。灰盒 HUD 和提示不代表最终美术。
- 已移除工作台底部文字条；上次收集数据仍在本次运行的场景切换间保留，退出 Play 后重置。制作/背包与磁盘存档系统尚未接入。

## 调整与测试

Player_WhiteHappinessDoll：速度 4.2、跳跃初速度 11.5、重力倍率 2.8。完整跳跃理想高度 2.41、水平距离 3.52；短跳、边缘宽容与操作手感仍需真人试玩。

推速为 2.4。PushCrate_Required / PushCrate_Bonus 使用 Min X / Max X 限制范围。PushCrate_Circus 开启 Fall And Respawn，Support Platform 引用 CircusRest：中心推出平台边缘后重力掉落，下降 4 个单位或 3 秒后回到 Play 开始时的位置；若玩家站在复位位置，会等待玩家离开再出现。可在边缘前停止推箱并踩箱登高。

MainCamera_StraightSideView：正交尺寸 5.2，横向前视 2.8，随高度跟随。07_Fog_RevealAhead 控制前方显露距离。

PlayMode 测试包含从入口连续推箱、通过所有跳跃、取得六份材料、走到摊后出口的完整物理通关，以及不可绕过的高台、掉落恢复、拾取去重、镜头、迷雾和重开验证。

自动通关时间不能代表首次试玩时间。一分钟仍是设计目标。真人测试先记录：是否自主理解推箱、彩旗掉落次数、攀登段哪一跳最难、是否看见 H1 与二次探索入口、是否愿意拿 B3。

实际通关报告保存到 Logs/DollwrightGraybox/playtest-*.json，记录用时、跳跃、掉落及材料。

## 本地连接与重新生成

编辑器通过 Logs/DollwrightGraybox/Bridge 的本地文件提供状态、打开灰盒场景、Play/Stop、截图等有限命令；不需要第三方插件或网络服务。场景有未保存修改时拒绝切换。

**Rebuild Dream 01** 会覆盖当前第一关灰盒；**Build Both Test Scenes** 会覆盖第一关和移动测试。手动编辑场景后先另存或提交版本，再使用生成菜单。

## 手动复制箱子

请在 01_CollisionGeometry 下复制完整的 PushCrate_Circus 或 PushCrate_Required。PushMark 只是箱子上的箭头文字子对象，不带箱体、碰撞或推箱功能。复制子对象后仍受原箱子的移动和缩放影响。

新箱子要放在平台表面，底部与平台顶部对齐。固定轨道箱重新设置 Min X / Max X；可掉落箱开启 Fall And Respawn 并重新指定 Support Platform。复位位置在进入 Play 时记录，移动编辑态箱体即可修改。

## 人偶与界面配置

Data/Doll01.asset 定义人偶编号、显示名称、能力说明和对应梦境场景路径。当前人偶01对应 Dream01_Graybox。点击 BedButton 后镜头平滑聚焦展示架，点击架上的白色 Doll01 直接进入梦境；点击返回按钮或按 Esc 复原镜头。旧 DollSelection 面板已删除。Workbench_DreamUI 管理镜头与射线选择；Shelf_DollSelection 下的 Doll01_White_Placeholder 是可独立替换的白色示意模型，BoxCollider 是点击区域，ShelfFocus_CameraPose 是聚焦机位。焦距在 WorkbenchDreamUI 的 Focus Field Of View 调整。镜头移动期间不接受人偶点击。

构建列表已加入 Workbench 与 Dream01_Graybox，Workbench 排在第一项。Editor/WorkbenchFlowSetup 是本轮场景迁移脚本；手动调整过新增箱子和门之后，不要重复运行迁移，以免重设这些对象的位置。

Editor/ShelfSelectionSetup 是 2026-10-16 的一次性定向迁移，保留平台位置；不要在手工调整人偶或聚焦镜头后重复执行 Apply，否则会重建这些新增对象。

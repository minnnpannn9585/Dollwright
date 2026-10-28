# 工作台人偶自定义灰盒

场景：Assets/Dollwright/Graybox/Scenes/Workbench.unity

## 操作
1. 在 Unity 打开 Workbench，点击 Play，再点击中央圆形制作台。
2. 左侧选择素胚、脸部、头饰；人偶和名称立即更新。按住人偶所在的中央区域拖动，可以旋转预览。
3. 库存足够时点击“制作”。每次消耗所选素胚、脸部各 1；特殊头饰再消耗 1。预览、更换、关闭都不扣材料。
4. 点击右上角 × 或按 Esc 返回工作台。点击小床，镜头靠近展示架，点击成品进入梦境。超过三只时使用展示架下方的左右箭头翻页。

界面只保留三组选项、库存、成品名称、制作、关闭和必要的翻页控件。没有新增说明弹窗和旧式选人偶卡片。
所有新增的人偶、脸部和头饰均由球、胶囊、圆柱和立方体占位；不包含最终美术。

## 配方
- R1 棉花 / R2 木头 / R3 陶瓷。
- F01 快乐手绘 / F02 快乐纽扣 / F03 悲伤手绘 / F04 悲伤纽扣。
- 无头饰 / H1 魔术师 / H2 泡泡。
- 36 种组合，18 种默认名称。身体和脸部不改变能力或移动参数。
- 无头饰进入梦境 01；魔术师进入梦境 02 独立入口；泡泡能够制作和上架，对应梦境尚未搭建，点击时短暂显示“梦境待制作”。

保留初始快乐布偶。新的存档额外提供 1 份棉花、1 份快乐手绘脸，供第一次制作使用。
其他材料通过梦境收集；数量为 0 的选项仍可预览，制作按钮会按库存禁用。
灰盒快速测试全部素材：Unity 顶部菜单 Dollwright > Graybox > Grant customization test materials。每次添加每种材料各 1，按钮不放在游戏界面内。

## 梦境结算与保存
拾取物上的 materialId 是关卡位置标识；新增 craftingMaterialId 是制作材料标识，不要混用。
梦境 01 的 B1/B2/B3 为 R1；F1 为 F01，F2 为 F03；H1 为魔术师头部。
梦境 02 的 R1/R2 分别为 R1/R2；F1 为 F02；H2 为泡泡头部。关卡结构为方箱入口和一个连续主关卡，主关卡内部材料持续保留。
场景之间保留材料的具体种类，只在醒来时统一入库；同一趟梦境不能重复结算。
Z 回溯、掉落重生和已有材料保留逻辑继续使用原系统。

保存文件：
Application.persistentDataPath/dollwright-customization-v1.json
本机默认目录：
C:/Users/86135/AppData/LocalLow/DefaultCompany/Dollwright/

保存包括库存、独立成品 ID、配方、当前搭配草稿和最近结算的梦境 ID。先写临时文件再原子替换，保留上一份 .bak；写入失败不会扣料或生成半个成品。
自动测试使用隔离库存和临时存档，不写入玩家的正式进度。

## 后续替换美术
DollPlaceholderFactory 负责 3D 占位，DreamDollAppearance 负责梦境中的几何形象。
替换为正式预制体时保留 DollRecipe、DollCraftingInventory、WorkbenchCraftingUI 和旅行逻辑。
素胚、脸部、头饰继续作为独立模块；不需要为 36 个配方制作 36 套独立模型。
Workbench_Customization 中保存制作预览位置、镜头位置、点击区域和展示架槽位。原工作台模型与关卡平台没有重新生成。

## 验证
- 2026-10-20 合并关卡二后，39 项完整 PlayMode 测试通过。
- 补充填充光后再次通过 6 项工作台 / 成品入梦检查。
- 比较修改前后 6 个梦境 / 移动测试场景，原有非脚本组件（Transform、渲染器、碰撞体、刚体等）均未改变。
- 原始完整报告：Logs/DollwrightGraybox/customization-regression-37.json
- 最新界面报告：Logs/DollwrightGraybox/flow-tests.json
- 实际运行截图：Logs/DollwrightGraybox/Customization/crafting.png 和 shelf.png。


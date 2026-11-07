# 工作台 2.0 · 独立美术测试

双击本文件夹中的 `Workbench_2_0.unity`，切换到 Game 窗口查看。
此场景用于材质、灯光与构图验证，不含游戏 UI 或互动，也不在 Build Settings 中。

- `Workbench_2_0_Art_Model`：新版工作台及独立功能模块，171 个网格，11,054 三角面（包含隐藏的木工、陶艺模块）。
- `Background_2D_SoftWorkshop`：单张背景平面，材质的 Blur 控制背景模糊。
- `Lighting_Warm_Window_And_Lamp`：暖窗光、环境填充、工作灯及反射探针。
- `Volume_Workbench_2_0_Only`：本场景调色、Bloom、暗角。
- `Materials`：木材、墨绿旧漆、暗红柜面、黄铜、纸、布料和象牙色圆台。
- `Textures`：本场景独立贴图副本，不引用旧工作台的材质。
- `Prefabs/Workbench_2_0_Art.prefab`：独立工作台美术预制体，不含游戏逻辑。

加工设备中，缝纫机默认停靠左后方，木工和陶艺模块保持关闭；此测试没有切换设备的交互。
右侧展示柜暂留空，后续再接入正式人偶资产。

编辑器菜单 `Dollwright > Art Tests > Workbench 2.0` 提供场景校验和截图。
`Build Separate Art Scene` 会用脚本参数重建本美术场景；如已手动改过场景，请先另存副本。
日常调整可以直接修改场景中的光源、材质和相机，不需要重新生成。

原 Workbench、MainMenu、梦境场景和现有游戏存档均不由此美术测试操作。
背景仅适用于当前正面构图，旋转镜头后不会产生完整 3D 房间的视差。

## 操作台视觉引导调整

- `Lamp_Warm_Pool` 聚光灯对准中央圆台。`Lamp_Beam_To_Crafting_Platform` 提供柔和的可见光束，按场景深度截断，不穿过桌面和前景物体。
- `WB2_LampBeam` 的 Density 控制光束可见程度；真正的台面亮度由聚光灯控制。
- 所有抽屉的把手与连接柄使用 `WB2_DrawerHandle_Matte`，低金属度、低光滑度，并关闭尖锐高光。台灯仍使用自己的黄铜材质。
- 线索板、抽屉和桌面的 8 张纸片使用无图案的 `WB2_Paper`。原 UV 坐标保留，后续可接入自行绘制的图案。

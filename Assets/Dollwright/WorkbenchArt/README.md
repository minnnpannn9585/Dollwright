# 工作台手绘美术首版 · 2026-10-20

已在原 Unity 项目的 `Assets/Dollwright/Graybox/Scenes/Workbench.unity` 应用。

本次修改：

- 绘制并使用低饱和橡木纹理、可调色浅色漆木纹理、独立工作坊背景画。
- 暖木色为主，灰绿色与紫灰色用于柜子、线索板和制作台底座；纸张、陶罐和金属使用简单独立材质。
- 左上方暖色主光、弱冷色填充和环境光；不烘焙光照，不改变其他关卡的渲染管线设置。
- 为 82 个需要贴图的模型部件生成按物理尺度投影的 UV 网格副本；原 FBX、Blender 源文件、物件位置和交互碰撞体均保留。
- 贴图副本的每个三角形位置、绕序、法线与原模型逐项校验一致。家具共 4,788 三角形，未因贴图增加三角面。
- 背景是单独的无碰撞体 Unlit Quad，并在 Ignore Raycast 层；已验证 16:9 全景、两个交互近景及采样的镜头移动过程均不会露边。

Unity 中可继续编辑的位置：

- `Assets/Dollwright/WorkbenchArt/Textures/`：三张实际使用的贴图。
- `Assets/Dollwright/WorkbenchArt/Materials/`：木材、漆木、纸张、陶罐、金属和背景材质。
- `Assets/Dollwright/WorkbenchArt/MappedMeshes/`：UV 网格副本，作为场景中原模型实例的 MeshFilter 覆盖。
- Hierarchy → `Workbench_Art_Lighting_Backdrop`：主光、填充光、背景。
- 菜单 `Dollwright / Workbench Art / Validate Geometry and Camera Coverage`：重新检查几何与背景覆盖。Apply 菜单会重新应用本次预设，手动调色后不必再点 Apply。

原场景备份：`Logs/DollwrightGraybox/BeforeWorkbenchArt-20261020/Workbench.unity`。恢复时须停止 Play，将备份覆盖原场景文件；保留原场景 `.meta`。原模型与 `wood.mat` 未覆盖。

截图 `Workbench_Final.png`、`Crafting_Final.png`、`Shelf_Final.png` 均来自 Unity 实际运行。

素材由内置 imagegen 生成/编辑，完整提示词记录在 `generation-prompts.json`。`Painted_Muted_Oak.png`、`Painted_Neutral_Wood.png`、`Workshop_Backdrop.png` 为最终使用版本；`Painted_Honey_Wood.png` 只保留为过程对照。

这是一版基于现有低模的材质与光照完善。人偶仍为已有的几何占位；纸张保持无文字。背景适合当前三个固定视角，未来改为自由摄像机或超宽屏时需要重新检查覆盖。

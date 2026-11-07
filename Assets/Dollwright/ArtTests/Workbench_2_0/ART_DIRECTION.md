# 工作台 2.0 · Unity 美术测试

目标：依据用户提供的新工作台参考，还原深色木质、暗红脱漆、墨绿布料、黄铜工作灯与左上暖光的组合。当前是独立美术测试，不接入游戏流程。

Unity 场景：Assets/Dollwright/ArtTests/Workbench_2_0/Workbench_2_0.unity

模型来自 2026-10-28 新灰模，保持独立网格与三种加工模块。模型导出及 UV 准备副本是 Workbench_2_0_UV_Source.blend；原灰模保留。工作台与工具是 3D；背景墙、窗、植物和远景陈设只存在于背景贴图中。展示柜暂留空，角色不在本次美术测试范围内。

材质分工：
- 木纹：Painted_Walnut，按木板长轴排列 UV，Unity 中调节深浅。
- 暗红柜面：Aged_Red_Panel，一块完整面板纹理对应一个抽屉正面。
- 墨绿/暗红旧漆：木纹与可调的旧漆覆盖层，阴影和反光由 Unity 实时计算。
- 灯具和把手：黄铜金属材质；刀刃：较冷的钢材；中央圆台：旧象牙色涂层。
- 纸片：按后续反馈改为无图案的暖白纸面；场景不再使用纸片图案贴图，原 UV 保留供后续自行绘制。
- 背景：专门生成的无前景工作台版本，在背景平面着色器中进一步柔化；前景工具不受背景模糊影响。

光影：左上暖色主光、低强度绿色环境填充、工作灯局部暖光、反射探针、克制的 Bloom 和色调映射。Window_Leaf_Light 作为主光 Cookie 投射柔和的窗格和树叶明暗，不增加房间或窗格模型。

操作台引导调整：台灯聚光改为对准中央圆台，并增加深度截断的柔和光束；全部抽屉把手改用独立哑光旧金属材质，降低与操作台争夺注意力的高光。该调整仍不增加交互或游戏逻辑。

此场景不加入 Build Settings，不放置 Canvas、游戏控制器、存档或制作流程脚本。原工作台、梦境场景、游戏入口与旧材质保持原状。

## 素材来源与生成记录

使用内置 image_gen 工具，不使用外部下载材质。用户参考图只用于风格与构图参考，没有作为整幅工作台背景直接贴在前景上。

### Workshop_Background.png 最终提示词

Use case: stylized-concept. Asset type: wide 2:1 environment BACKGROUND PLATE for a Unity 3D workbench art test. Use the supplied image as the precise art-direction reference: dark olive-green, storybook artisan doll workshop, old walnut framing, hand-painted aged surfaces, warm sunlight entering upper left, cool green deep ambient shadows. Generate ONLY the distant workshop environment behind the workbench. REMOVE the entire foreground desk, all tabletop tools, the large brass lamp, and the right foreground doll display cabinet and dolls; these will be rendered as actual 3D models in front. Show softly out-of-focus old green plaster walls, dark timber beams, a partial sunlit window high left, loosely pinned small paper patterns and botanical notes, dim shelving in the centre and right distance, vines by the far edges. No characters, no dolls, no lettering, no interface, no foreground furniture or second workbench. Camera straight-on at desk-eye level, horizontal wide 2:1 composition matching reference, no strong wide-angle perspective. Background should already have tasteful optical depth-of-field blur so notes and foliage are soft shapes, but preserve directional warm light. Lower quarter dark subdued olive/walnut background, visually quiet centre behind main desk. This is a seamless-in-composition game backdrop, NOT a full new scene with a desk. Fill the entire image; no borders or labels.

### Painted_Walnut.png 最终提示词

Use case: stylized-concept. Asset type: seamless square base-color texture for a stylized 3D game workbench, NOT a photograph or rendered object. Fill all pixels with a close flat orthographic surface of aged honey-walnut wood. Continuous single wood surface with long grain running LEFT TO RIGHT, no separate plank seams or boards. Charming painterly storybook hand-painted treatment, broad warm medium brown brushwork, sparse angular pale worn grain accents, understated elongated knots and tiny scratches, warm caramel highlights and muted umber grain. Match an antique artisan doll-making desk lit by sunlight, but texture itself must have EVEN FLAT diffuse illumination, no directional lighting, no shadows, no perspective, no edge vignette, no AO, no border, no text. Midtone relatively light desaturated tan wood so Unity lighting can darken it. Detail intentionally stylized and low-frequency rather than photorealistic sharp grain or pores. Seamlessly repeating left-right and top-bottom. 2048 square texture.

### Aged_Red_Panel.png 最终提示词

Use case: stylized-concept. Asset type: orthographic 2:1 wooden drawer-front albedo texture for a stylized antique doll workshop game. A SINGLE rectangular aged vermilion and oxblood-red painted wood surface filling the entire canvas, richly but tastefully worn, with dark desaturated jade-green underpaint showing in irregular chipped streaks and along the margins, brown wood visible in a few worn patches. Elegant very restrained thin aged ochre-gold pinstripe border inset 8% from edge, gently curved corner flourishes inspired by Victorian sewing-machine stencilling, no large central motif and no hardware. Horizontal subtle wooden grain beneath brushy red paint. Palette brick red, muted deep green and brown, dusky gold. Hand-painted storybook low-poly game texture, large deliberate brushstroke shapes not photorealistic grit. Even neutral albedo lighting, no cast shadows, no baked highlights, no perspective, no bevel render, no drawer knob, no text, no logo, no external frame or margin. Surface runs to all edges; entire image is the texture.

### Window_Leaf_Light.png 最终提示词

Create a grayscale light projection cookie texture, square image, a technical asset for a directional light in a 3D game engine. Entire image is a softly defocused sunlit window pattern: mostly WHITE and LIGHT GRAY open light, a few soft medium-gray silhouettes of small ivy leaves clustered along left and upper edges, and two narrow diagonal blurred window-mullion shadow bars. White means full sunlight, gray means attenuated sunlight. Smooth soft optical penumbra edges, no hard pixel edges. Keep the large central and lower-right parts mostly white with a few gentle scattered gray leaf shadows. Shadow areas about 20 percent of total image, maximum darkness charcoal gray rather than pure black. NOT a picture of a room, NOT a window illustration, NOT actual colored leaves. Only black-and-white out-of-focus abstract projected leaf silhouettes and narrow frame shadows in a flat image, suitable as a lighting gobo. No border, no text, no gradients unrelated to the projection. High quality 1024 square.

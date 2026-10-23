# 《春日来信》美术与界面

美术使用内置 image_gen 生成，无需额外 API 密钥。所有最终 PNG 已保存到作品的 `Game/Art/` 中，并由 `Game/game.json` 映射。生成器返回的实际尺寸低于提示词中的建议尺寸；以此处记录的实际尺寸为准，未人为放大。

## 美术资源

| 文件 | 实际尺寸 | 用途 |
| --- | --- | --- |
| spring-key-visual.png | 1672 × 941 | 标题页整幅插画，小葵形象以立绘为参考 |
| aoi-spring.png | 1024 × 1536 | 小葵立绘，保留真实透明 Alpha |
| campus-spring.png | 1672 × 941 | 樱花校园 |
| library-afternoon.png | 1672 × 941 | 午后图书馆 |
| river-sunset.png | 1672 × 941 | 夕照河畔 |

## 界面规范

- 暖白信纸、灰青文字、低饱和玫瑰色；面板、按钮、提示、进度条全部直角。
- 标题页使用完整主视觉，文字叠加留白渐变；右侧五列中文自上而下排布，附英文功能名、数字及键盘焦点。继续阅读与读取存档为独立入口；当前没有会话时，继续阅读置灰。
- 对话框为半透明信笺、独立姓名签、继续提示和底部文字工具栏；正文 24 设计像素、37 行距、每页最多三行。
- 存档以 3 × 2 场景预览卡展示；设置、历史、暂停和覆盖确认共用细线边框与间距。
- 插画保持宽高比；角色独立以透明度渐入。排版继续按窗口原生分辨率渲染，保留高 DPI、全屏、窗口留边和鼠标换算。

## 完整生成提示词

生成方式：内置 `image_gen`（不是 CLI）。主视觉引用角色立绘作为身份参考，其余场景独立生成。以下提示词为实际提交内容。

### aoi-spring.png

```text
Use case: illustration-story.
Asset type: production anime visual novel character sprite, transparent PNG, tall portrait composition.
Primary request: Create an original, polished Japanese romance visual novel heroine for the Chinese game "春日来信" (Spring Letters). This is Aoi / 小葵, an adult university student, 20 years old. A gentle, slightly shy but hopeful young woman with long flowing chestnut-brown hair reaching the waist, soft side bangs, one small dusty-rose ribbon hair ornament at her left temple, expressive warm teal-green eyes. Warm ivory cardigan over a crisp white collared blouse, a dusty rose neck ribbon, a dark slate-teal pleated skirt. She holds one cream paper letter envelope gently in both hands at the upper waist, anatomy and fingers correct. Relaxed three-quarter standing pose, looking toward the viewer with a small tender smile, subtle blush. Hair and skirt move slightly in a spring breeze.
Style/medium: high-quality commercial bishoujo visual novel sprite, delicate clean colored linework, refined cel shading with soft painted light, detailed luminous eyes, carefully rendered fabric, natural slender adult proportions, elegant and tasteful.
Composition/framing: single complete head-to-mid-shin figure, entire top of hair and all loose hair and both arms contained in frame, large figure filling most of the tall canvas with narrow transparent margins. Feet may fall below frame but do not cut at waist or skirt.
Lighting/mood: soft warm afternoon daylight from upper left, gentle pink reflected light, quiet spring romance.
Constraints: genuinely TRANSPARENT background with clean alpha including hair edges, no white backdrop, no scene, no props beyond envelope, no text, no watermark, no logo, no UI, no border. Single character only. Not chibi, not 3D, not flat geometric shapes.
```

### spring-key-visual.png

```text
Use case: illustration-story.
Asset type: wide 16:9 main menu key visual for original romance visual novel "春日来信" (Spring Letters), production game background, ideally 2560x1440.
Input images: Image 1 is CHARACTER IDENTITY REFERENCE only. Match her face, hairstyle, teal eyes, dusty-rose ribbons and ivory cardigan outfit exactly. It is not an edit target.
Primary request: a beautiful, premium anime visual novel cover illustration. An adult university student Aoi with long chestnut hair, teal eyes, ivory cardigan, white blouse, dusty rose ribbon, dark teal pleated skirt, holding a cream love letter with both hands, standing on a sunlit university avenue under blossoming cherry trees. She turns slightly toward the viewer, soft tender smile, fine strands of hair and pink petals moving in a spring breeze.
Composition: cinematic landscape 16:9 full bleed illustration. Place heroine at x=63% of canvas, head at 18% height, skirt visible toward bottom. Her face and letter must remain completely within the CENTER-RIGHT 45%-78% of canvas. The LEFT 40% is a luminous airy expanse of softly painted campus walkway and light cherry blossoms, visually quiet and low contrast, for a large title overlay. The RIGHTMOST 20% is softly lit blurred pale pink blossoms and sky, quiet enough for four slender vertical text menus. Keep face away from far right. Cherry branches frame the upper edge, perspective stone path recedes, historic cream university building softly visible in distance, distant blue-green foliage and bright blue sky.
Style: refined high-budget Japanese bishoujo visual novel promotional key art. Exquisite anime facial detail and clean delicate outlines matching reference, painterly and richly detailed environment, luminous atmosphere, natural adult proportions. Not a photo, not 3D, not chibi.
Lighting and palette: luminous spring afternoon backlight, ivory highlights, soft sakura pink, powder blue, muted teal and warm chestnut. Nostalgic romantic mood. Rich color and depth but no harsh oversaturation.
Constraints: exactly ONE heroine, no other people, no text or lettering of any kind, no logos, no watermark, no UI elements, no frames. The full canvas is opaque painted scenery.
```

### campus-spring.png

```text
Use case: illustration-story. Asset type: 16:9 landscape background painting for a polished Japanese romance visual novel, original game Spring Letters. Wide cinematic composition, ideally 2560x1440. Refined hand-painted anime background, beautiful atmospheric perspective, intricate natural and architectural detail, delicate brushwork, luminous natural light, ivory, pale sakura pink, powder blue and soft sage-teal palette. Empty scene with NO PEOPLE, no characters, no silhouettes, no text, no letters, no watermark, no logos, no borders, no UI. The center foreground must leave space for a character sprite added separately; keep important scenery above the bottom 25% where dialogue UI will be overlaid. Entire canvas opaque.
Scene: a quiet Japanese university campus cherry blossom avenue in April. View along a stone walkway toward a warm ivory historic university building with tall windows in the middle distance; flowering cherry branches arch across the upper frame, pale pink petals suspended in the breeze, lush low green hedges, narrow flower beds and wooden benches. Soft clear blue sky with wisps of white cloud. Sunlit spring afternoon, tender romantic nostalgic mood, gentle dappled shadows, premium richly painted visual novel scenery. No dramatic perspective distortions.
```

### library-afternoon.png

```text
Use case: illustration-story. Asset type: 16:9 landscape background painting for a polished Japanese romance visual novel, original game Spring Letters. Wide cinematic composition, ideally 2560x1440. Refined hand-painted anime background, beautiful atmospheric perspective, intricate natural and architectural detail, delicate brushwork, luminous natural light, ivory, pale sakura pink, powder blue and soft sage-teal palette. Empty scene with NO PEOPLE, no characters, no silhouettes, no text, no letters, no watermark, no logos, no borders, no UI. The center foreground must leave space for a character sprite added separately; keep important scenery above the bottom 25% where dialogue UI will be overlaid. Entire canvas opaque.
Scene: the interior of a tranquil university library in spring. Rich warm honey-colored wooden bookshelves flank the left and right sides, filled with many individual book spines without readable lettering. Tall white-framed windows across the center-right open a view toward pink cherry trees and a cream campus building. Sheer ivory curtains gently lifted by a breeze, afternoon sunbeams illuminate dust and an empty polished wooden reading table with a closed book in the foreground, slanted golden shadows on warm parquet floor. Inviting hushed atmosphere, detailed realistic spatial perspective with clean anime background painting.
```

### river-sunset.png

```text
Use case: illustration-story. Asset type: 16:9 landscape background painting for a polished Japanese romance visual novel, original game Spring Letters. Wide cinematic composition, ideally 2560x1440. Refined hand-painted anime background, beautiful atmospheric perspective, intricate natural and architectural detail, delicate brushwork, luminous natural light, ivory, pale sakura pink, powder blue and soft sage-teal palette. Empty scene with NO PEOPLE, no characters, no silhouettes, no text, no letters, no watermark, no logos, no borders, no UI. The center foreground must leave space for a character sprite added separately; keep important scenery above the bottom 25% where dialogue UI will be overlaid. Entire canvas opaque.
Scene: a peaceful Japanese riverside path in spring at sunset. Pale pink cherry blossom trees canopy the upper left and upper right, stone pedestrian embankment and simple narrow rail receding on the left, wide calm river reflecting peach-gold sky and soft clouds in center and right, distant arched footbridge and little rooftops beyond the water. A few fallen cherry petals float on the river. Gentle warm setting sun near the horizon, wistful romantic atmosphere, saturated but soft peach, pink, warm cream and blue-teal shadows. Detailed scenic anime game background.
```


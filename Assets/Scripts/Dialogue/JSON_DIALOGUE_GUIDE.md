# JSON Dialogue System Guide

## Referencing GIF Assets in JSON

To reference a GifAsset in your JSON dialogue files, use the `portraitGifPath` field in your dialogue nodes.

### Path Format

The path should be **relative to any Resources folder** in your project. For example:

- If your GifAsset is at: `Assets/Resources/Gifs/RafaIdle.asset`
- Use path: `"Gifs/RafaIdle"` (no `.asset` extension, no `Assets/Resources/` prefix)

- If your GifAsset is at: `Assets/Resources/Characters/Rafa/Portraits/Talking.asset`
- Use path: `"Characters/Rafa/Portraits/Talking"`

### Example JSON Node

```json
{
  "nodeID": "start",
  "speakerID": "Rafa",
  "dialogueTextKey": "Hello!",
  "portraitSprite": null,
  "portraitGif": null,
  "portraitSpritePath": "",
  "portraitGifPath": "Gifs/RafaIdle",
  "animationTrigger": "",
  "choices": [],
  "nextNodeID": "",
  "conditions": [],
  "effects": [],
  "autoAdvanceDelay": 0.0,
  "textSpeed": 100.0
}
```

### Important Notes

1. **GifAssets must be ScriptableObjects** - They need to be created as `.asset` files using the GifAsset Editor (`Window > Unbound > GifAsset Editor`)

2. **Place GifAssets in Resources folders** - Only assets in folders named `Resources` can be loaded at runtime using `Resources.Load()`

3. **Path is case-sensitive** - Make sure the path matches exactly (including capitalization)

4. **No file extension** - Don't include `.asset` in the path

5. **Caching** - GifAssets are automatically cached after first load for better performance

### Loading Behavior

- The system will automatically load the GifAsset when the dialogue node is displayed
- If loading fails, a warning will be logged and the portrait will be skipped
- The loaded GifAsset is cached for subsequent uses

### Sprite Portraits

Similarly, you can reference sprite portraits using `portraitSpritePath`:

```json
"portraitSpritePath": "Sprites/Characters/Rafa/Portrait"
```

The sprite should be in a Resources folder and the path should be relative to Resources (without file extension).


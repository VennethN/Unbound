# Audio System

A comprehensive, modular audio system for Unity that handles music, sound effects, ambient audio, UI sounds, and voice with full volume controls, crossfading, object pooling, and JSON-based configuration.

## Overview

The audio system provides:
- **Music**: Background music with crossfading, playlists, and dynamic transitions
- **SFX**: Sound effects with object pooling, cooldowns, and variant groups
- **Ambient**: Environmental audio with zone-based triggers
- **UI Audio**: Button clicks, hover sounds, sliders, and toggles
- **Voice**: Dialogue and voice-over support
- **Volume Controls**: Master and per-category volume with mute options
- **JSON Configuration**: Define all audio clips in JSON files
- **Save System Integration**: Volume settings persist via PlayerPrefs

## Architecture

### Core Components

| Component | Purpose |
|-----------|---------|
| `AudioManager` | Central singleton managing all audio playback |
| `AudioDatabase` | Loads and manages audio clip data from JSON |
| `AudioSettings` | Volume levels and mute states |
| `AudioClipData` | Data structure for audio clip configuration |

### Specialized Controllers

| Component | Purpose |
|-----------|---------|
| `MusicController` | Playlists, shuffle, crossfading between tracks |
| `SFXController` | Sound groups, cooldowns, variant selection |
| `AmbientController` | Zone-based ambient sounds |

### Helper Components

| Component | Purpose |
|-----------|---------|
| `UIAudio` | Add audio to buttons (hover, click) |
| `UISliderAudio` | Tick sounds for sliders |
| `UIToggleAudio` | Toggle on/off sounds |
| `UIAudioDefaults` | Global UI sound defaults |
| `AudioTrigger` | Trigger sounds via collision/code |
| `FootstepAudio` | Automatic footstep sounds |
| `AmbientZone` | Define ambient sound regions |
| `SurfaceTag` | Mark surfaces for footstep variation |

## Quick Start

### 1. Create Audio Manager GameObject

1. Create an empty GameObject in your scene
2. Name it "AudioManager"
3. Add the `AudioManager` component
4. Add the `AudioDatabase` component

```
AudioManager (GameObject)
├── AudioManager (Component)
└── AudioDatabase (Component)
    └── Audio Data Path: "Data/Audio"
```

### 2. Create Audio JSON Files

Create JSON files in `Assets/Resources/Data/Audio/`:

**music.json:**
```json
{
  "clips": [
    {
      "clipID": "music_main_menu",
      "displayName": "Main Menu Theme",
      "clipPath": "Audio/Music/main_menu",
      "category": 0,
      "defaultVolume": 0.8,
      "loop": true
    }
  ]
}
```

### 3. Add Audio Files

Place your audio files in the Resources folder matching the `clipPath`:
```
Assets/Resources/Audio/Music/main_menu.mp3
```

### 4. Play Audio

```csharp
using Unbound.Audio;

// Play music
AudioManager.Instance.PlayMusic("music_main_menu");

// Play SFX
AudioManager.Instance.PlaySFX("sfx_attack_sword");

// Play UI sound
AudioManager.Instance.PlayUI("ui_click");
```

## JSON Configuration

### Audio Categories

| Value | Category |
|-------|----------|
| 0 | Music |
| 1 | SFX |
| 2 | Ambient |
| 3 | UI |
| 4 | Voice |

### AudioClipData Fields

```json
{
  "clipID": "unique_identifier",
  "displayName": "Human Readable Name",
  "clipPath": "Path/In/Resources/folder",
  "category": 1,
  "defaultVolume": 1.0,
  "defaultPitch": 1.0,
  "pitchVariation": 0.1,
  "loop": false,
  "spatialBlend": 0.0,
  "minDistance": 1.0,
  "maxDistance": 500.0,
  "priority": 128
}
```

| Field | Type | Description |
|-------|------|-------------|
| `clipID` | string | Unique identifier for the clip |
| `displayName` | string | Human-readable name |
| `clipPath` | string | Path to audio file in Resources (without extension) |
| `category` | int | Audio category (0-4) |
| `defaultVolume` | float | Default volume (0-1) |
| `defaultPitch` | float | Default pitch (-3 to 3) |
| `pitchVariation` | float | Random pitch variation range |
| `loop` | bool | Whether to loop the clip |
| `spatialBlend` | float | 2D/3D blend (0 = 2D, 1 = 3D) |
| `minDistance` | float | 3D audio min distance |
| `maxDistance` | float | 3D audio max distance |
| `priority` | int | Audio source priority (0 = highest) |

## Usage Guide

### Music Playback

```csharp
using Unbound.Audio;

// Play music with crossfade
AudioManager.Instance.PlayMusic("music_exploration");

// Play music instantly (no crossfade)
AudioManager.Instance.PlayMusic("music_combat", crossfade: false);

// Play with custom fade duration
AudioManager.Instance.PlayMusic("music_boss", true, 3f);

// Stop music
AudioManager.Instance.StopMusic(fade: true, fadeDuration: 2f);

// Pause/Resume
AudioManager.Instance.PauseMusic();
AudioManager.Instance.ResumeMusic();

// Check if playing
bool isPlaying = AudioManager.Instance.IsMusicPlaying;
string currentTrack = AudioManager.Instance.CurrentMusicID;
```

### Music Playlists

```csharp
using Unbound.Audio;

// Setup playlist
List<string> tracks = new List<string> {
    "music_exploration",
    "music_peaceful",
    "music_adventure"
};

MusicController.Instance.SetPlaylist(tracks);
MusicController.Instance.SetShuffle(true);
MusicController.Instance.SetLoop(true);

// Start playlist
MusicController.Instance.StartPlaylist();

// Navigation
MusicController.Instance.NextTrack();
MusicController.Instance.PreviousTrack();
MusicController.Instance.PlayTrackAtIndex(2);

// Stop
MusicController.Instance.StopPlaylist();
```

### Sound Effects

```csharp
using Unbound.Audio;

// Play SFX (returns instance for tracking)
AudioInstance instance = AudioManager.Instance.PlaySFX("sfx_attack_sword");

// Play one-shot (fire and forget)
AudioManager.Instance.PlaySFXOneShot("sfx_footstep");

// Play at position (3D audio)
AudioManager.Instance.PlaySFX("sfx_explosion", transform.position);

// Play directly from AudioClip
AudioManager.Instance.PlaySFXDirect(myAudioClip, volume: 0.8f);
```

### SFX Groups (Variants)

```csharp
using Unbound.Audio;

// Register a group of related sounds
SFXController.Instance.RegisterGroup("footsteps_stone", 
    new List<string> { 
        "sfx_footstep_stone_1", 
        "sfx_footstep_stone_2",
        "sfx_footstep_stone_3" 
    },
    SFXSelectionMode.RandomNoRepeat
);

// Play random sound from group
SFXController.Instance.PlayFromGroup("footsteps_stone");

// Play in sequence
SFXController.Instance.PlaySequenceFromGroup("footsteps_stone");
```

### Ambient Audio

```csharp
using Unbound.Audio;

// Start ambient sound
AudioManager.Instance.StartAmbient("ambient_forest", fadeInDuration: 2f);

// Stop ambient sound
AudioManager.Instance.StopAmbient("ambient_forest", fadeOutDuration: 2f);

// Stop all ambient
AudioManager.Instance.StopAllAmbient(fadeOutDuration: 1f);
```

### Ambient Zones

Create zone-based ambient audio:

1. Create a GameObject with a Collider2D (trigger)
2. Add the `AmbientZone` component
3. Configure the zone:

```csharp
// AmbientZone component settings
zoneID = "forest_zone";
ambientClipIDs = ["ambient_forest", "ambient_wind"];
fadeDuration = 2f;
useColliderTrigger = true;
playerTag = "Player";
```

**Manual zone control:**
```csharp
using Unbound.Audio;

// Enter/exit zones manually
AmbientController.Instance.EnterZone("forest_zone");
AmbientController.Instance.ExitZone("forest_zone");

// Transition between zones
AmbientController.Instance.TransitionToZone("cave_zone", fadeDuration: 3f);
```

### UI Audio

**Automatic UI sounds:**
1. Add `UIAudio` component to any Button
2. Configure hover/click sounds in Inspector

```csharp
// Or configure via code
UIAudio uiAudio = button.AddComponent<UIAudio>();
uiAudio.SetHoverSound("ui_hover");
uiAudio.SetClickSound("ui_click");
```

**Slider tick sounds:**
```csharp
// Add UISliderAudio to your Slider
UISliderAudio sliderAudio = slider.AddComponent<UISliderAudio>();
```

**Toggle sounds:**
```csharp
// Add UIToggleAudio to your Toggle
UIToggleAudio toggleAudio = toggle.AddComponent<UIToggleAudio>();
```

**Global UI defaults:**
```csharp
using Unbound.Audio;

// Play default sounds from anywhere
UIAudioDefaults.PlayClick();
UIAudioDefaults.PlayHover();
UIAudioDefaults.PlayBack();
UIAudioDefaults.PlayError();
UIAudioDefaults.PlaySuccess();
```

### Volume Controls

```csharp
using Unbound.Audio;

// Master volume
AudioManager.Instance.SetMasterVolume(0.8f);

// Category volumes
AudioManager.Instance.SetCategoryVolume(AudioCategory.Music, 0.7f);
AudioManager.Instance.SetCategoryVolume(AudioCategory.SFX, 1.0f);
AudioManager.Instance.SetCategoryVolume(AudioCategory.Ambient, 0.5f);
AudioManager.Instance.SetCategoryVolume(AudioCategory.UI, 0.6f);

// Mute controls
AudioManager.Instance.SetMuteAll(true);
AudioManager.Instance.SetCategoryMuted(AudioCategory.Music, true);

// Save/Load settings
AudioManager.Instance.SaveSettings();
AudioManager.Instance.LoadSettings();
AudioManager.Instance.ResetSettings();

// Access settings directly
float musicVolume = AudioManager.Instance.Settings.musicVolume;
bool isMuted = AudioManager.Instance.Settings.muteAll;
```

### Audio Triggers

Add `AudioTrigger` component to GameObjects for event-based audio:

```csharp
// AudioTrigger component settings
clipID = "sfx_door_open";
playOnTriggerEnter = true;
use3DPositioning = true;
requiredTag = "Player";
onlyPlayOnce = true;
```

**Manual trigger:**
```csharp
AudioTrigger trigger = GetComponent<AudioTrigger>();
trigger.Play();
trigger.ResetTrigger(); // Allow playing again
```

### Footstep Audio

Add `FootstepAudio` component to player:

```csharp
// Configure in Inspector or code
FootstepAudio footsteps = player.AddComponent<FootstepAudio>();
footsteps.stepInterval = 0.4f;
footsteps.runningMultiplier = 0.6f;

// Set running state
footsteps.SetRunning(isRunning);

// Manual trigger
footsteps.TriggerFootstep();
```

**Surface-specific footsteps:**
1. Add `SurfaceTag` component to ground objects
2. Set `surfaceType` (Stone, Wood, Grass, etc.)
3. Enable `detectSurfaceType` on FootstepAudio
4. Create SFX groups for each surface:
   - `footsteps_stone`, `footsteps_wood`, etc.

### Events

```csharp
using Unbound.Audio;

// AudioManager events
AudioManager.Instance.OnMusicStarted += (clipID) => Debug.Log($"Music started: {clipID}");
AudioManager.Instance.OnMusicStopped += (clipID) => Debug.Log($"Music stopped: {clipID}");
AudioManager.Instance.OnSFXPlayed += (clipID) => Debug.Log($"SFX played: {clipID}");
AudioManager.Instance.OnSettingsChanged += () => Debug.Log("Settings changed");

// MusicController events
MusicController.Instance.OnTrackChanged += (clipID, index) => Debug.Log($"Track: {clipID}");
MusicController.Instance.OnPlaylistEnded += () => Debug.Log("Playlist ended");

// AmbientController events
AmbientController.Instance.OnZoneChanged += (oldZone, newZone) => Debug.Log($"Zone: {newZone}");
```

### Runtime Registration

```csharp
using Unbound.Audio;

// Register audio clip at runtime
AudioDatabase.Instance.RegisterAudioClip("dynamic_sfx", myAudioClip, AudioCategory.SFX, volume: 0.8f);

// Register clip with full data
AudioClipData data = new AudioClipData {
    clipID = "dynamic_music",
    category = AudioCategory.Music,
    defaultVolume = 0.7f,
    loop = true,
    clip = myMusicClip
};
AudioDatabase.Instance.RegisterAudioClip(data);
```

## Setup Examples

### Basic Setup

```
Scene Hierarchy:
├── AudioManager
│   ├── AudioManager (Component)
│   └── AudioDatabase (Component)
├── Player
│   └── FootstepAudio (Component)
└── UI
    └── Buttons with UIAudio (Component)
```

### Full Setup with Controllers

```
Scene Hierarchy:
├── AudioManager
│   ├── AudioManager (Component)
│   ├── AudioDatabase (Component)
│   ├── MusicController (Component)
│   ├── SFXController (Component)
│   ├── AmbientController (Component)
│   └── UIAudioDefaults (Component)
├── Player
│   └── FootstepAudio (Component)
├── Environment
│   ├── ForestZone (AmbientZone)
│   ├── CaveZone (AmbientZone)
│   └── WaterSound (AudioTrigger)
└── UI
    └── Buttons with UIAudio (Component)
```

## File Structure

```
Assets/
├── Resources/
│   └── Data/
│       └── Audio/
│           ├── music.json
│           ├── sfx.json
│           ├── ambient.json
│           └── ui.json
│   └── Audio/
│       ├── Music/
│       │   ├── main_menu.mp3
│       │   └── exploration.mp3
│       ├── SFX/
│       │   ├── attack_sword.wav
│       │   └── footstep_stone_1.wav
│       ├── Ambient/
│       │   ├── forest.mp3
│       │   └── cave.mp3
│       └── UI/
│           ├── click.wav
│           └── hover.wav
└── Scripts/
    └── Audio/
        ├── AudioManager.cs
        ├── AudioDatabase.cs
        ├── AudioSettings.cs
        ├── AudioClipData.cs
        ├── MusicController.cs
        ├── SFXController.cs
        ├── AmbientController.cs
        ├── UIAudio.cs
        ├── AudioTrigger.cs
        └── README.md
```

## API Reference

### AudioManager

| Method | Description |
|--------|-------------|
| `PlayMusic(clipID, crossfade, fadeDuration)` | Play background music |
| `StopMusic(fade, fadeDuration)` | Stop current music |
| `PauseMusic()` / `ResumeMusic()` | Pause/resume music |
| `PlaySFX(clipID, position)` | Play sound effect |
| `PlaySFXOneShot(clipID, position)` | Fire-and-forget SFX |
| `PlaySFXDirect(clip, volume, pitch)` | Play AudioClip directly |
| `PlayUI(clipID)` | Play UI sound |
| `PlayUIDirect(clip, volume)` | Play UI AudioClip directly |
| `StartAmbient(clipID, fadeIn)` | Start ambient loop |
| `StopAmbient(clipID, fadeOut)` | Stop ambient sound |
| `StopAllAmbient(fadeOut)` | Stop all ambient |
| `SetMasterVolume(volume)` | Set master volume |
| `SetCategoryVolume(category, volume)` | Set category volume |
| `SetMuteAll(mute)` | Mute all audio |
| `SetCategoryMuted(category, muted)` | Mute category |
| `SaveSettings()` / `LoadSettings()` | Persist settings |
| `StopAll()` | Stop all audio |
| `PauseAll()` / `ResumeAll()` | Pause/resume all |

### AudioDatabase

| Method | Description |
|--------|-------------|
| `LoadAllAudioData()` | Load from JSON files |
| `GetClipData(clipID)` | Get AudioClipData by ID |
| `GetClip(clipID)` | Get AudioClip by ID |
| `HasClip(clipID)` | Check if clip exists |
| `GetClipsByCategory(category)` | Get all clips in category |
| `RegisterAudioClip(data)` | Register clip at runtime |
| `PreloadAllClips()` | Preload all AudioClips |
| `PreloadCategory(category)` | Preload category |

### MusicController

| Method | Description |
|--------|-------------|
| `SetPlaylist(clipIDs)` | Set playlist tracks |
| `StartPlaylist()` | Begin playlist playback |
| `StopPlaylist()` | Stop playlist |
| `NextTrack()` / `PreviousTrack()` | Navigate playlist |
| `PlayTrack(clipID)` | Play specific track |
| `SetShuffle(enabled)` | Enable/disable shuffle |
| `SetLoop(enabled)` | Enable/disable loop |

### SFXController

| Method | Description |
|--------|-------------|
| `PlaySFX(clipID, position)` | Play with cooldown |
| `PlayFromGroup(groupID, position)` | Play from variant group |
| `RegisterGroup(id, clips, mode)` | Register SFX group |
| `SetCooldown(clipID, duration)` | Set cooldown |
| `ClearAllCooldowns()` | Clear all cooldowns |

### AmbientController

| Method | Description |
|--------|-------------|
| `EnterZone(zoneID)` | Enter ambient zone |
| `ExitZone(zoneID)` | Exit ambient zone |
| `TransitionToZone(zoneID, fade)` | Crossfade zones |
| `StartAmbient(clipID, fade)` | Start ambient |
| `StopAmbient(clipID, fade)` | Stop ambient |
| `StopAllAmbient(fade)` | Stop all ambient |

### PlayerCombat (Audio Methods)

| Method | Description |
|--------|-------------|
| `SetAttackSound(soundID)` | Set attack sound ID |
| `SetHitSound(soundID)` | Set hit sound ID |
| `SetHurtSound(soundID)` | Set hurt sound ID |
| `SetCombatAudioEnabled(enabled)` | Enable/disable combat audio |

## Troubleshooting

### Audio not playing
- Ensure `AudioManager` and `AudioDatabase` are in the scene
- Check that JSON files are in `Resources/Data/Audio/`
- Verify `clipPath` matches actual file location in Resources
- Check volume settings aren't muted

### No crossfade on music change
- Ensure `crossfade` parameter is `true`
- Check `fadeDuration` is greater than 0

### 3D audio not working
- Set `spatialBlend` to 1.0 in JSON
- Provide position when calling `PlaySFX()`
- Check `minDistance` and `maxDistance` values

### UI sounds not playing
- Verify `UIAudio` component is attached
- Check that `Selectable` is interactable
- Ensure UI audio clips are defined in JSON

### Settings not persisting
- Call `AudioManager.Instance.SaveSettings()` after changes
- Ensure `saveSettingsOnDestroy` is enabled

## Combat Audio Integration

The `PlayerCombat` component has built-in audio support with customizable sound IDs.

### Inspector Settings

| Field | Default | Description |
|-------|---------|-------------|
| `Attack Sound ID` | `sfx_attack_sword` | Sound when attacking |
| `Hit Sound ID` | `sfx_hit_enemy` | Sound when hitting an enemy |
| `Hurt Sound ID` | `sfx_player_hurt` | Sound when taking damage |
| `Miss Swing Sound ID` | *(empty)* | Optional sound when attack misses |
| `Enable Combat Audio` | `true` | Toggle all combat sounds |

### Setup

1. Ensure `AudioManager` and `AudioDatabase` are in the scene
2. Define combat sounds in JSON (see example below)
3. Configure sound IDs in `PlayerCombat` Inspector

### Example JSON Configuration

Add to `Assets/Resources/Data/Audio/sfx.json`:

```json
{
  "clips": [
    {
      "clipID": "sfx_attack_sword",
      "displayName": "Sword Attack",
      "clipPath": "Audio/SFX/attack_sword",
      "category": 1,
      "defaultVolume": 0.7,
      "pitchVariation": 0.05
    },
    {
      "clipID": "sfx_hit_enemy",
      "displayName": "Hit Enemy",
      "clipPath": "Audio/SFX/hit_enemy",
      "category": 1,
      "defaultVolume": 0.8,
      "pitchVariation": 0.1,
      "spatialBlend": 0.5
    },
    {
      "clipID": "sfx_player_hurt",
      "displayName": "Player Hurt",
      "clipPath": "Audio/SFX/player_hurt",
      "category": 1,
      "defaultVolume": 0.9
    }
  ]
}
```

### Runtime Configuration

```csharp
using Unbound.Player;

PlayerCombat combat = GetComponent<PlayerCombat>();

// Change sound IDs at runtime
combat.SetAttackSound("sfx_attack_hammer");
combat.SetHitSound("sfx_hit_critical");
combat.SetHurtSound("sfx_player_hurt_heavy");

// Enable/disable combat audio
combat.SetCombatAudioEnabled(false);
```

### Weapon-Specific Sounds

To play different sounds per weapon, you can change the sound IDs when equipping:

```csharp
void OnWeaponEquipped(EquipmentType slot, string itemID)
{
    if (slot != EquipmentType.Weapon) return;
    
    PlayerCombat combat = GetComponent<PlayerCombat>();
    
    switch (itemID)
    {
        case "sword_iron":
            combat.SetAttackSound("sfx_attack_sword");
            break;
        case "hammer_war":
            combat.SetAttackSound("sfx_attack_hammer");
            break;
        case "dagger_steel":
            combat.SetAttackSound("sfx_attack_dagger");
            break;
    }
}
```

## Best Practices

1. **Use clip IDs consistently** - Define all audio in JSON for easy management
2. **Preload important audio** - Use `PreloadCategory()` for frequently used sounds
3. **Use SFX groups for variation** - Prevents repetitive sounds
4. **Set appropriate priorities** - Music: 0, Important SFX: 50-100, Ambient: 200
5. **Use cooldowns** - Prevent sound spam via `SFXController`
6. **Organize JSON files** - Separate by category (music.json, sfx.json, etc.)
7. **Test volume levels** - Balance all categories relative to each other
8. **Use pitch variation** - Add `pitchVariation` to combat sounds for variety


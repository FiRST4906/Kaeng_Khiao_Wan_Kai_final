# Kaeng Khiao Wan Kai

A 2D side-scrolling action-adventure game built with **MonoGame (DesktopGL)** and C#.
Play as William Jones ("Kai"), explore three stages, fight monsters, and defeat a boss at the end of each stage.

> Version: v1.0.0

## Features

- Three stages, each with its own backgrounds, music and boss
  - Stage 1: Beequid
  - Stage 2: Brother
  - Stage 3: Aletheia (two-phase boss: tentacle swipe, then red eye laser)
- Melee combat, combo attacks, jump / double jump, sprint and parry
- Items and inventory: sword, wrench, trident, seaweed, HP potion, jump potion, keys
- Story scenes with dialogue boxes and boss introductions
- Save points (checkpoints) and a stone-code puzzle door
- Settings menu with Master / SFX / Music volume

## Controls

| Action | Input |
|---|---|
| Move left / right | `A` / `D` |
| Sprint | Hold `Shift` |
| Jump (press again in the air for a double jump) | `Space` |
| Attack / use held item (eat seaweed, drink potion) | Left mouse button |
| Aim trident (when holding the trident) | Right mouse button |
| Parry | `R` |
| Interact (doors, save point, pick up items, signs) | `E` |
| Select hotbar slot | `1` - `9` or mouse wheel |
| Open / close inventory | `Tab` |
| Open / close book | `I` |
| Advance dialogue | Left mouse button |
| Pause menu / Settings | `Esc` |
| Toggle fullscreen | `F11` |
| Skip intro video | `Space` |

### Parry
Press `R` just before an enemy attack lands. A successful parry blocks the damage, hurts the enemy instead and resets the parry cooldown.

### Developer console
Press `F3` to open the debug console, type a command and press `Enter`. These commands are for testing only:

| Command | Effect |
|---|---|
| `/skip_map` | Jump to the next stage (stage 1 -> 2 -> 3) |
| `/summon_boss1_<n>`, `/summon_boss2_<n>`, `/summon_boss3_<n>` | Spawn `n` bosses next to the player |
| `/kai_god` | Toggle god mode |
| `/giveme_items` | Open the item-give menu |

## Requirements

- Windows 10 / 11 (64-bit)
- Graphics card with OpenGL 3.0+ support
- To build from source: [.NET SDK](https://dotnet.microsoft.com/download) (the version listed in `TargetFramework` of `Kaeng_Khiao_Wan_Kai.csproj`) and Visual Studio 2022 or later

## How to run (pre-built game)

1. Download `Kaeng_Khiao_Wan_Kai_v1.0.0.zip` from the **Releases** page of this repository.
2. Extract the whole zip into a folder (keep the `Content`, `Sounds` and `IntroVideo` folders next to the `.exe`).
3. Double-click `Kaeng_Khiao_Wan_Kai.exe`.

## How to build from source

### Visual Studio
1. Clone or download this repository.
2. Open `Kaeng_Khiao_Wan_Kai.slnx` in Visual Studio.
3. Set the configuration to **Release**.
4. Press **Start** to run, or right-click the project and choose **Publish** (Folder, target runtime `win-x64`, Self-contained) to create a distributable build.

### Command line
```
cd Kaeng_Khiao_Wan_Kai
dotnet publish -c Release -r win-x64 --self-contained true
```
The output is in `bin/Release/<target-framework>/win-x64/publish/`.

### Content pipeline
All images and sound effects are listed in `Content/Content.mgcb`. If you add a new asset, add it to that file (MonoGame Content Pipeline Tool) so it is built into `.xnb`.
The `Sounds` and `IntroVideo` folders are loaded directly from disk and must be copied next to the `.exe`.

## Project structure

```
Kaeng_Khiao_Wan_Kai/
  Game1.cs            Main game logic (menus, player, enemies, bosses, scenes)
  Program.cs          Entry point
  Content/            Images and other assets (Content.mgcb)
  Sounds/             Sound effect files (.wav)
  IntroVideo/         Intro video frames and audio
```

## Team

- Add your name(s) and student ID(s) here.

## Credits

- Game made with [MonoGame](https://monogame.net/).
- Add credits for any third-party art, music or sound effects here.

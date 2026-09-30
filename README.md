# 🛹 Runner (Subway Surfers-Style 3D Endless Runner)

An endless 3D runner built with **Unity (Unity 6 / 6000.3.2f1)** featuring dynamic 3-track railway gameplay, oncoming trains, obstacles, hoverboard shields with demolition VFX, humanoid animations, and mobile portrait touch controls.

---

## 🎮 Gameplay Features

- **3 Distinct Railway Tracks:** Authentic railway tracks with realistic ballast, timber ties, steel rails, and seamless infinite generation via `TileManager`.
- **Urban City Scenery:** Concrete perimeter retaining walls, overhead signal gantries, two-tier skyscrapers, and atmospheric lighting.
- **Dynamic Obstacles & Moving Trains:**
  - Stationary trains, roadblocks, and hurdles.
  - Oncoming moving commuter trains.
  - Safe-to-run train rooftops with cowcatcher boarding ramps.
- **Subway Surfers Hoverboard Shield Powerup:**
  - Collect hoverboard pickups on the track to add to your inventory stash.
  - Double-tap anywhere on screen, tap the on-screen button, or press `E` to deploy.
  - 15-second speed boost with invulnerability shield.
  - **Crash Demolition Effect:** Crashing with an active hoverboard destroys the obstacle with particle demolition VFX without ending your run or pushing you backward.
  - **Surfer Stance Animation:** Humanoid character switches to a surfing posture while hovering.
- **Mobile Gesture & Swipe Controls (Portrait Mode):**
  - Designed for vertical 9:16 portrait orientation (classic Subway Surfers style).
  - Dynamic camera framing that adapts to device aspect ratios.
  - Touch-friendly responsive HUD and modal menus.

---

## 🕹️ Controls

| Action | Mobile Gesture | PC / Keyboard |
|---|---|---|
| **Change Lane Left** | Swipe Left / Drag Left | `A` or `Left Arrow` |
| **Change Lane Right** | Swipe Right / Drag Right | `D` or `Right Arrow` |
| **Jump** | Swipe Up / Drag Up | `W`, `Up Arrow`, or `Space` |
| **Slide / Dive Down** | Swipe Down / Drag Down | `S` or `Down Arrow` |
| **Deploy Hoverboard** | **Double-Tap screen** or tap HUD widget | `E` or `Return` |

---

## 📁 Project Structure

```
Assets/
├── Editor/          # Automation tools & scene builders
├── Materials/       # Urban city, railway ballast, neon hoverboard & obstacle shaders
├── Models/          # Humanoid 3D character & environmental models
├── Prefabs/         # Tiles, moving trains, obstacles, coins, hoverboard pickups
├── Scenes/          # MainRunnerScene.unity
└── Scripts/
    ├── CameraFollow.cs           # Adaptive portrait/landscape framing & camera shake
    ├── Coin.cs                   # Coin collection logic with audio & trigger guard
    ├── GameManager.cs            # Score, coins, high score, and mobile game over UI
    ├── HoverboardPickup.cs       # Track pickup adding +1 to hoverboard inventory
    ├── MovingTrain.cs            # Oncoming train physics & movement
    ├── Obstacle.cs               # Obstacle collision handling
    ├── ObstacleDemolishEffect.cs # Explosion & demolition particle VFX
    ├── PlayerController.cs       # Unified mobile swipe + keyboard controller & physics
    ├── RunnerCharacterAnimator.cs# Humanoid animation state machine syncing
    └── TileManager.cs            # Procedural infinite track & scenery spawning
```

---

## 🚀 Getting Started

1. Clone this repository:
   ```bash
   git clone https://github.com/GeorgeGlyn/Runner.git
   ```
2. Open the project in **Unity 6 (6000.3.2f1 or compatible)**.
3. Open `Assets/Scenes/MainRunnerScene.unity`.
4. Set the Game View aspect ratio to **9:16 Portrait** (or test in landscape).
5. Press **Play**!

# ShiftRacer: Redline Quarter Mile

> **High-octane first-person superbike drag racing built with Unity 6 (Universal Render Pipeline).**

---

## 🏁 Overview

**ShiftRacer: Redline Quarter Mile** is a precision drag racing game. Astride a high-revving 1000cc inline-four superbike, you line up against a rival drag racer on a regulation 402.336-meter (1/4 mile) drag strip. Victory requires razor-sharp reaction time, pinned throttle management, and 5 consecutive clean upshifts executed precisely within the motorcycle's optimal power band (11,400 – 12,600 RPM).

---

## 🎮 Controls

The game features multi-device input support via Unity's New Input System:

| Action | Primary Keyboard | Alternate Keyboard | Mouse | Gamepad |
| :--- | :--- | :--- | :--- | :--- |
| **Throttle / Rev** | `Space` | `W` / `Up Arrow` | `Left-Click` | Right Trigger / Button South (`A` / `Cross`) |
| **Upshift** | `Left-Shift` | `E` | `Right-Click` | Right Bumper / Button North (`Y` / `Triangle`) |

---

## 🚦 How to Play

1. **Ready Screen**: Hold **Space** (or **W** / **Left-Click**) to stage the superbike at the start line.
2. **Staging Countdown (3-2-1)**: You can pre-rev the engine while the countdown lights sequence to build launch RPM.
3. **Launch (GO!)**: Pin the throttle to accelerate down the drag strip.
4. **Shift Timing**:
   * Watch the tachometer as your RPM climbs toward redline (13,000 RPM).
   * Between **11,400 RPM and 12,600 RPM**, the green shift window is active and the **`>> SHIFT NOW! <<`** badge flashes.
   * Hit **Shift** (or **E** / **Right-Click**) to shift into the next gear (Gears 1 through 6).
5. **Grading**:
   * 🟢 **PERFECT!** — Upshifted within the 11,400–12,600 RPM window. Retains 100% drive power.
   * 🟡 **EARLY SHIFT** — Shifted prematurely below 11,400 RPM (bogged acceleration).
   * 🔴 **LATE SHIFT / LIMITER** — Hit the 13,000 RPM rev limiter (lost acceleration momentum).
6. **Win Condition**: Cross the 1/4 mile finish line (402.3m) ahead of your rival with a clean run (5/5 perfect shifts).

---

## 🏗️ Architecture & Core Systems

The game is modularly decoupled across specialized systems located in `Assets/Scripts/`:

```
Assets/Scripts/
├── Audio/
│   └── BikeAudioController.cs      # DSP thread-safe procedural 4-cylinder engine synth & shift pops
├── Config/
│   └── RaceConfig.cs              # ScriptableObject for track lengths, RPM limits, gearing, & AI
├── Core/
│   ├── BikeEngineModel.cs         # 1000cc engine simulation (RPM rise/fall, limiter stutter, physics)
│   ├── SequentialGearbox.cs       # 6-speed sequential transmission with gear ratios and drop curves
│   ├── ShiftGrade.cs              # Evaluation enum (None, Perfect, Early, Late)
│   ├── ShiftJudge.cs              # Shift grading logic, timing windows, & clean run tracking
│   ├── OpponentController.cs      # Scripted rival racer AI (70 m/s competitive pace)
│   ├── RacerVisualSync.cs         # Z-translation, cockpit camera vibration & rumble shake
│   └── RaceSessionController.cs   # Central session FSM (Ready -> Staging -> Racing -> Result)
├── Editor/
│   └── DragRaceSceneBuilder.cs    # 1-click scene generator (Track, gantries, lighting, HUD, wiring)
└── UI/
    └── RaceHUDController.cs        # Digital tachometer, speed/gear text, sprite badges, shift dots
```

---

## 🛠️ Track Features

The scene features an extended **2,200-meter drag venue** containing:
* **Regulation 1/4 Mile Gantry & Ground Stripes** at `402.336m`.
* **1/2 Mile Extension Gantry** at `804.672m`.
* **1 Mile Finish Line Gantry** at `1609.344m`.
* **Visual Optical Flow**: 200+ dashed center lane lines, alternating red/white safety curbs, distance boards every 100m, and roadside light poles every 40m.

---

## ⚙️ Configuration & Tuning

Track metrics, engine physics, and gearbox settings can be tuned live without modifying code via `Assets/DefaultRaceConfig.asset`:

* **Track Distances**: Quarter mile (402.336m), Half mile (804.672m), One mile (1609.344m).
* **RPM Dynamics**:
  * Idle: `2,800 RPM`
  * Shift Window: `11,400 – 12,600 RPM`
  * Redline / Limiter: `13,000 RPM`
  * Limiter Bounce Frequency: `18 Hz`
* **Gearbox Ratios**:
  * Top Speeds per gear (km/h): `[135, 178, 220, 258, 292, 325]`
  * Post-Shift RPM Drops: `[8600, 9300, 9900, 10400, 10800]`
* **Opponent AI**: Baseline speed (`70 m/s`) and acceleration (`17.5 m/s²`).

---

## 🚀 Getting Started

1. Open the project in **Unity 6 (6000.0.85f2)** or higher.
2. Load the race scene: `Assets/Scenes/DragRace.unity`.
3. Press **Play** in the Unity Editor.
4. *(Optional)* To regenerate the entire drag race track, lighting, UI canvas, and object wiring from scratch, select from the top menu:  
   **`Redline -> Setup Complete Drag Race Scene`**.

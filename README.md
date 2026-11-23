# ShiftRacer: Redline Quarter Mile

> **A high-octane first-person 1000cc superbike drag racer built in Unity 6 (Universal Render Pipeline) for GitHub Game Off 2026.**

[![GitHub Game Off 2026](https://img.shields.io/badge/GitHub%20Game%20Off-2026%20%E2%80%A2%20Theme%3A%20SHIFT!-brightgreen)](https://itch.io/jam/game-off-2026)
[![Unity 6](https://img.shields.io/badge/Engine-Unity%206%20(6000.0.85f2)-blue.svg)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/URP-Universal%20Render%20Pipeline-orange.svg)](https://unity.com/)
[![Target Platform](https://img.shields.io/badge/Platform-WebGL%20%7C%20Windows%20%7C%20Mobile-purple.svg)](https://itch.io/)

---

## 🏆 GitHub Game Off 2026 Theme: "SHIFT!"

ShiftRacer embodies the **Game Off 2026 theme: SHIFT!** across three distinct dimensions:

1. **The Hero Control — The Physical `SHIFT` Key:**  
   The game is built from the ground up as a one-button hero experience. Wide-open auto-throttle is engaged during the run, directing the player's complete focus, reflexes, and timing onto a single keystroke: the **`SHIFT`** key.
2. **The Core Mechanic — Shifting Transmission Gears:**  
   Astride a 1000cc inline-four superbike pulling 0–300+ km/h, every fraction of a second counts. Players must execute 5 consecutive rapid-fire upshifts directly inside the motorcycle's powerband (`11,400 – 12,600 RPM`).
3. **The Multi-Discipline Shift — Shifting Race Lengths:**  
   Players can dynamically shift race distance disciplines between **1/4 Mile** (pure launch reaction & sprint), **1/2 Mile** (rolling power acceleration), and **1 Mile** (terminal aerodynamic redline endurance).

---

## 🏁 Overview

Lined up on a regulation midnight drag strip against a tuned rival, you pilot a 1000cc superbike from a true first-person cockpit perspective. A high-contrast digital tachometer displays your climb toward the 13,000 RPM redline. Shifting inside the glowing green zone awards **PERFECT!** drive efficiency, while short-shifting bogs engine momentum and hitting the limiter triggers rapid-fire fuel-cut stutter.

---

## 🎮 Controls

ShiftRacer supports multi-device inputs via Unity's New Input System, including full keyboard, mouse, gamepad, and mobile touchscreen support:

| Action | Primary Keyboard | Alternate Keyboard | Mouse | Gamepad | Mobile Touchscreen |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Upshift (Hero)** | **`Left-Shift` / `Right-Shift`** | `E` | `Right-Click` | Right Bumper / `Y` / `Triangle` | Tap `UPSHIFT` button or screen |
| **Throttle / Rev** | `Space` *(Auto during race)* | `W` / `Up Arrow` | `Left-Click` | Right Trigger / `A` / `Cross` | Hold `THROTTLE` or right screen |
| **Downshift (Recovery)** | `Q` | — | — | Left Bumper / `X` / `Square` | Tap `DOWNSHIFT` button |
| **Stage & Race** | **`Shift`** / `Space` | `Enter` / `W` | `Left-Click` | `A` / `Cross` | Tap Stage button or screen |
| **Select Distance** | `Tab` / `D` | `Left` / `Right Arrow` | Click Tabs | D-pad Left / Right | Tap Distance Tabs (1/4, 1/2, 1 Mile) |
| **Restart / Replay** | **`Shift`** / `Space` | `R` / `Enter` | `Left-Click` | `A` / `Cross` | Tap screen |

> **Game Off One-Button Mode:** During the race, wide-open throttle is applied automatically (`autoThrottleDuringRace = true`), turning the run into a pure test of shift timing accuracy.

---

## 🚦 Race Progression & Rules

1. **Select Race Distance**: On the Ready screen, choose your strip length:
   * **1/4 Mile (402.3m)** — Explosive sprint reaching ~240–260 km/h in Gears 1–4/5 in ~9.5–10.5 seconds.
   * **1/2 Mile (804.7m)** — Mid-strip power pull pushing 5th and 6th gear to ~280–300 km/h in ~15–17 seconds.
   * **1 Mile (1609.3m)** — Aerodynamic top-speed endurance pinning 6th gear at 310–325 km/h redline in ~25–28 seconds.
2. **Staging Line**: Press **`SHIFT`** or **`Space`** (or tap the red Stage button) to stage. The amber Christmas tree counts down: **3... 2... 1... GO!** (Engine can be pre-revved to build launch RPM).
3. **Green Light Launch**: The bike rockets forward down the asphalt.
4. **Shift Timing Window**:
   * Watch the digital horizontal tachometer bar as RPM climbs.
   * Between **11,400 RPM and 12,600 RPM**, the green sweet spot activates and the **`>> SHIFT NOW! <<`** cue flashes.
   * Hit **`SHIFT`** (or tap screen) to upshift into the next gear.
5. **Shift Evaluation**:
   * 🟢 **PERFECT!** — Shifted within 11,400–12,600 RPM. Boosts acceleration with 100% drive momentum.
   * 🟡 **EARLY SHIFT** — Shifted below 11,400 RPM. Bogs down engine torque.
   * 🔴 **LATE SHIFT / LIMITER** — Hit 13,000 RPM fuel cut. Drops acceleration until shifted.
6. **Victory & Telemetry**:
   * Crossing the finish line ahead of your rival awards a **VICTORY**.
   * Executing 5 clean shifts earns the **★ FLAWLESS RUN ★** rating.
7. **Player-Controlled Replay**:
   * The Result card stays on screen as long as desired to review time, top speed, and shift stats.
   * Press **`SHIFT`**, **`Space`**, or tap anywhere to return to the Ready screen to pick another distance or stage again.

- **Canvas Scaler**: Configured to `ScaleWithScreenSize` (`1920x1080` reference) with balanced `MatchWidthOrHeight = 0.5`, ensuring perfect visual scale on 16:9, modern ultra-wide smartphones (19.5:9 / 20:9), and tablets/iPads (4:3 / 16:10).
- **Hardware Safe Area Adaptation**: Built-in `SafeAreaFitter` dynamically offsets HUD touch controls away from notches, dynamic islands, camera cutouts, and bottom gesture bars.
- **Ergonomic Landscape Touch Controls**:
  - **Right Thumb Zone**: Prominent, touch-reactive **Throttle** and **Upshift** buttons with tactile press-scaling.
  - **Left Thumb Zone**: Dedicated **Downshift** button for recovery.
  - **Interactive Ready Screen**: Tap directly on distance tabs to pick your strip distance, then tap the dedicated Stage button to begin.
  - **Multi-Touch**: Independent tracking allows holding full throttle while tapping the shift paddle with another finger.

---

## 🔊 Audio Experience

All bike audio is mastered in 44.1 kHz 16-bit PCM and rendered with 2D cockpit fidelity (`spatialBlend = 0f`):

* **2,800 RPM Idle Rumble:** The superbike idles audibly on the starting grid at 50% volume so players immediately hear the rumbling exhaust note.
* **Dynamic RPM Pitch Scaling:** Engine sound scales continuously from a deep **0.70x** mechanical growl at idle to a screaming **2.25x** inline-four howl at 13,000 RPM.
* **Quickshifter Ignition Cut:** Upshifting triggers an authentic 70ms ignition cut with an exhaust backfire pop (`Quickshifter_Bark.wav`).
* **Rev Limiter Stutter:** Bouncing off the 13,000 RPM redline triggers rapid 12 Hz fuel-cut exhaust pops (`RevLimiter_Stutter.wav`).
* **Race Cue Audio:** Christmas tree countdown cadence beeps, green light launch horn, and a celebratory victory fanfare.

---

## 📱 UI Ergonomics & Multi-Device Responsiveness

* **Clean Vertical Rhythm:** 30–45px vertical safety margins between all text elements, headers, distance tabs, and buttons on both the Ready and Result screens.
* **Modern Rounded 9-Slice Styling:** Sleek carbon panels with rounded borders (`UI_Rounded_Box.png`, `UI_Rounded_Border.png`, `UI_Pill_Bar.png`).
* **High-Contrast Telemetry Card:** Result screen features distinct metric styling (Cyan elapsed time, Gold top speed, Emerald green shift accuracy) bordered by a dynamic glowing border (Emerald on Victory, Coral on Defeat).
* **Hardware Safe Area Adaptation:** `SafeAreaFitter` dynamically offsets controls away from mobile camera notches, dynamic islands, and gesture bars.
* **Canvas Scaler:** Configured to `1920x1080` reference (`MatchWidthOrHeight = 0.5`), ensuring zero cutoff on 16:9, modern ultrawide screens (20:9), and tablets (4:3).
* **Shift Input Debounce:** A 220ms shift interval cooldown in `SequentialGearbox.cs` prevents accidental double-shifts from rapid taps.

- **Continuous Momentum**: Vehicle forward speed is physical and never drops when upshifting.
- **Physical Gear Ratios**: Each gear operates in its own authentic speed bandwidth:
  - **Gear 1**: 0 – 135 km/h (Ratio: 96.3 RPM per km/h)
  - **Gear 2**: 90 – 178 km/h (Ratio: 73.0 RPM per km/h)
  - **Gear 3**: 130 – 220 km/h (Ratio: 59.1 RPM per km/h)
  - **Gear 4**: 170 – 258 km/h (Ratio: 50.4 RPM per km/h)
  - **Gear 5**: 210 – 292 km/h (Ratio: 44.5 RPM per km/h)
  - **Gear 6**: 250 – 325 km/h (Ratio: 40.0 RPM per km/h)
- **RPM Natural Drop**: Upshifting drops engine RPM directly into the next gear's power band (e.g., 12,200 RPM in Gear 1 drops smoothly to ~9,100 RPM in Gear 2).
- **Dyno Torque Curve**: Peak acceleration occurs in the power band between 9,500 and 12,600 RPM. Shifting too early bogs down torque; bouncing on the rev limiter cuts fuel injection until upshifted.

---

## 🏍️ Superbike Powertrain Physics

* **Continuous Momentum:** Forward speed is integrated physically; vehicle speed never drops artificially on upshift.
* **Physical Gear Ratios (1st through 6th):**
  * **Gear 1:** 0 – 135 km/h
  * **Gear 2:** 90 – 178 km/h
  * **Gear 3:** 130 – 220 km/h
  * **Gear 4:** 170 – 258 km/h
  * **Gear 5:** 210 – 292 km/h
  * **Gear 6:** 250 – 325 km/h
* **RPM Drop Curves:** Upshifting drops engine RPM smoothly into the meat of the next gear's powerband (e.g., 12,200 RPM in Gear 1 drops to ~9,100 RPM in Gear 2).
* **Camera Vibration & Rumble:** Perlin-noise vibration scales dynamically with road speed and rev limiter bounce.

---

## 🏗️ Architecture & Scripts

All code is decoupled across modular systems in `Assets/Scripts/`:

```
Assets/Scripts/
├── Audio/
│   └── BikeAudioController.cs      # 2D engine audio, pitch modulation, quickshifter cuts & limiter pops
├── Config/
│   └── RaceConfig.cs              # ScriptableObject for track lengths, RPM curves, ratios, & AI pace
├── Core/
│   ├── BikeEngineModel.cs         # 1000cc powertrain simulation (RPM integration, torque curves, speed)
│   ├── SequentialGearbox.cs       # 6-speed transmission, ratio drops, 220ms shift debounce cooldown
│   ├── ShiftGrade.cs              # Evaluation enum (None, Perfect, Early, Late)
│   ├── ShiftJudge.cs              # Shift grading logic, green window evaluation, and clean run tracking
│   ├── OpponentController.cs      # Scripted rival racer AI (scales across 1/4, 1/2, and 1 Mile strips)
│   ├── RacerVisualSync.cs         # Z-axis motion synchronization and cockpit camera shake
│   └── RaceSessionController.cs   # Central race state machine (Ready -> Staging -> Racing -> Result)
├── Editor/
│   ├── AutoSceneUpdater.cs        # Editor utility guaranteeing scene UI alignment on load
│   └── DragRaceSceneBuilder.cs    # 1-click generator for track, gantries, lighting, UI canvas, and wiring
└── UI/
    ├── MobileTouchController.cs   # Multi-touch input coordinator & screen quadrant fallback
    ├── OnScreenTouchButton.cs     # Tactile touch button component with pointer-over-UI detection
    ├── RaceHUDController.cs        # Cockpit overlay, digital tachometer, distance pill, and overlays
    └── SafeAreaFitter.cs          # Hardware notch/island safe area dynamic RectTransform offset
```

---

## 🚀 Building & Publishing to itch.io

### 1. Build WebGL in Unity
1. Open the project in **Unity 6 (6000.0.85f2)**.
2. Go to **File > Build Settings** (or **Build Profiles**).
3. Select **WebGL** and ensure `Assets/Scenes/DragRace.unity` is checked.
4. Click **Build**, select/create an output folder (e.g. `Build_WebGL`), and allow Unity to build.

### 2. Package for itch.io
1. Open the `Build_WebGL` folder containing `index.html`, `Build/`, and `TemplateData/`.
2. Select all files inside the folder, right-click, and zip them into `ShiftRacer_WebGL.zip`.

| Asset File                                   | Sub-Sprite / Slice                  | Assigned Inspector Field                   | Location / Role                                             |
| :------------------------------------------- | :---------------------------------- | :----------------------------------------- | :---------------------------------------------------------- |
| `First-Person Superbike Cockpit Overlay.png` | _(Single Sprite)_                   | `cockpitOverlaySprite` on `[CANVAS_HUD]`   | Fullscreen cockpit fairing, windshield & clip-on handlebars |
| `Tachometer_Dial.png`                        | _(Single Sprite)_                   | `tachometerDialSprite` on `[CANVAS_HUD]`   | Analog 14,000 RPM gauge dial face                           |
| `Rotating Tachometer Needle.png`             | _(Single Sprite)_                   | `tachometerNeedleSprite` on `[CANVAS_HUD]` | Dynamic RPM needle (pivots from 130° to -130°)              |
| `UI_sprites.png`                             | `Speedometer_Frame`                 | `speedometerFrameSprite` on `[CANVAS_HUD]` | Backing frame around Digital Speedometer (km/h)             |
| `UI_sprites.png`                             | `Gear_Frame`                        | `gearFrameSprite` on `[CANVAS_HUD]`        | Backing frame around Current Gear Indicator                 |
| `UI_sprites.png`                             | `PerfectShifts_Bar`                 | `perfectShiftsBarSprite` on `[CANVAS_HUD]` | Backing frame behind the 5-shift evaluation dots            |
| `UI_sprites.png`                             | `StartLights_Tree`                  | `startLightsTreeSprite` on `[CANVAS_HUD]`  | Staging Christmas Tree visual element                       |
| `UI_sprites.png`                             | `Badge_Perfect`                     | `badgePerfectSprite` on `[CANVAS_HUD]`     | Pop-up badge on optimal gear shift                          |
| `UI_sprites.png`                             | `Badge_Early`                       | `badgeEarlySprite` on `[CANVAS_HUD]`       | Pop-up badge on short shift                                 |
| `UI_sprites.png`                             | `Badge_Late`                        | `badgeLateSprite` on `[CANVAS_HUD]`        | Pop-up badge on rev limiter / late shift                    |
| `UI_sprites.png`                             | `Badge_ShiftNow`                    | `badgeShiftNowSprite` on `[CANVAS_HUD]`    | Flashing prompt when entering shift window                  |
| `UI_sprites.png`                             | `Hint_Throttle`                     | `hintThrottleSprite` on `[CANVAS_HUD]`     | Mobile touch Throttle button graphic                        |
| `UI_sprites.png`                             | `Hint_Upshift`                      | `hintUpshiftSprite` on `[CANVAS_HUD]`      | Mobile touch Upshift button graphic                         |
| `UI_sprites.png`                             | `Hint_Downshift`                    | `hintDownshiftSprite` on `[CANVAS_HUD]`    | Mobile touch Downshift button graphic                       |
| `signals.png`                                | `Individual Shift Dot States empty` | `dotEmptySprite` on `[CANVAS_HUD]`         | Pending shift dot indicator                                 |
| `signals.png`                                | `Individual Shift Dot States green` | `dotGreenSprite` on `[CANVAS_HUD]`         | Successful clean shift indicator                            |
| `signals.png`                                | `Individual Shift Dot States red`   | `dotRedSprite` on `[CANVAS_HUD]`           | Faulty / missed shift indicator                             |

### 3. itch.io Project Settings
1. Go to your **itch.io Dashboard** > **Create New Project**.
2. **Kind of project**: `HTML` (This file will be played in the browser).
3. **Uploads**: Upload `ShiftRacer_WebGL.zip` and check **"This file will be played in the browser"**.
4. **Viewport dimensions**: Set **Width: `1280`**, **Height: `720`** (or `960 × 540`).
5. **Options**: Check **"Fullscreen button"** and **"Mobile friendly"** (supports orientation: Landscape).
6. **Jam Submission**: Ensure **GitHub Game Off 2026** is checked under jam submissions.
7. Click **Save & View Page** to play!

| Audio Asset (44.1 kHz 16-bit PCM) | Assigned Inspector Field | Target Component                          | Description / Trigger                                                    |
| :-------------------------------- | :----------------------- | :---------------------------------------- | :----------------------------------------------------------------------- |
| `Engine_1000cc_Loop.wav`          | `engineLoopClip`         | `BikeAudioController` on `[GAME_SYSTEMS]` | High-revving 4-cylinder engine loop (dynamically pitch-shifted with RPM) |
| `Quickshifter_Bark.wav`           | `shiftBarkClip`          | `BikeAudioController` on `[GAME_SYSTEMS]` | Ignition-cut quickshifter exhaust pop & mechanical dog engagement        |
| `RevLimiter_Stutter.wav`          | `limiterPulseClip`       | `BikeAudioController` on `[GAME_SYSTEMS]` | Aggressive 18 Hz fuel-cut limiter bounce when bouncing at 13,000 RPM     |
| `Countdown_Beep.wav`              | `countdownBeepClip`      | `BikeAudioController` on `[GAME_SYSTEMS]` | Amber staging countdown cadence tone (3, 2, 1)                           |
| `GreenLight_Go.wav`               | `greenLightGoClip`       | `BikeAudioController` on `[GAME_SYSTEMS]` | High punch tone on green light launch (GO!)                              |
| `VictoryFanfare.wav`              | `victoryFanfareClip`     | `BikeAudioController` on `[GAME_SYSTEMS]` | Celebratory victory fanfare upon crossing the finish line ahead of rival |

---

## 📄 License & Credits

* **Developer:** [AbhiramRavula](https://github.com/AbhiramRavula)
* **Game Jam:** [GitHub Game Off 2026](https://itch.io/jam/game-off-2026)
* **License:** MIT License — Open source magic!

- **Deep Midnight Starry Sky**: Procedural midnight navy skybox coupled with an overhead 2,000-star canopy spanning the entire 2,200m venue.
- **Atmospheric Midnight Fog**: Linear deep navy distance fog (`100m – 1400m`) hiding track boundaries and giving infinite horizon depth.
- **Glowing Neon Milestone Gantries**:
  - **1/4 Mile (402.3m)**: Cyber Neon Electric Yellow beam tubes, glowing border, and downward finish-line spotlight.
  - **1/2 Mile (804.7m)**: Cyber Neon Electric Cyan beam tubes, glowing border, and downward finish-line spotlight.
  - **1 Mile Finish (1609.3m)**: Cyber Neon Hot Magenta / Red arch with glowing checkered ground stripes.
- **Roadside Streetlights**: 40-meter staggered light poles with warm sodium lamp heads casting pools of light onto the asphalt.
- **Racer Lighting**:
  - **Player Superbike LED Headlight**: Forward 80m spotlight cutting through the midnight fog in front of the cockpit windshield.
  - **Opponent Taillight**: Piercing neon red LED taillight illuminating the air ahead as your rival races.
- **URP Post-Processing Volume**:
  - **Bloom**: High-intensity bloom (`1.8x`) making neon signs, taillights, and streetlights bleed and glow.
  - **Color Adjustments**: Punchy high contrast (`+28`) for midnight blacks and vibrant neon.
  - **Vignette**: Realistic visor framing (`0.35x`) simulating a racing full-face helmet.

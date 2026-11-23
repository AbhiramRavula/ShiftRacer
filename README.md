# ShiftRacer: Redline Quarter Mile

> **A high-octane first-person 1000cc superbike drag racer built in Unity 6 (Universal Render Pipeline) for GitHub Game Off 2026.**

[![GitHub Game Off 2026](https://img.shields.io/badge/GitHub%20Game%20Off-2026%20%E2%80%A2%20Theme%3A%20SHIFT!-brightgreen)](https://itch.io/jam/game-off-2026)
[![Unity 6](<https://img.shields.io/badge/Engine-Unity%206%20(6000.0.85f2)-blue.svg>)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/URP-Universal%20Render%20Pipeline-orange.svg)](https://unity.com/)
[![Target Platform](https://img.shields.io/badge/Platform-WebGL%20%7C%20Windows%20%7C%20Mobile-purple.svg)](https://itch.io/)

---

## 🏆 GitHub Game Off 2026 Theme: "SHIFT!"

ShiftRacer embodies the **Game Off 2026 theme: SHIFT!** across three distinct gameplay and design dimensions:

1. **The Hero Control — The Physical `SHIFT` Key:**  
   The game is designed as a **one-button hero experience**. With wide-open auto-throttle engaged during the run, the player's complete focus, reflexes, and timing center entirely upon a single key: the physical **`SHIFT`** key.
2. **The Core Mechanic — Shifting Transmission Gears:**  
   Astride a screaming 1000cc inline-four superbike pulling 0–325 km/h, players must execute five consecutive, millisecond-accurate upshifts directly inside the engine's powerband (`11,400 – 12,600 RPM`).
3. **The Discipline Shift — Shifting Race Distances:**  
   Players can dynamically shift between three distinct drag disciplines:
   * **1/4 Mile (402.3m)**: Explosive sprint emphasizing launch reaction, clutch slip, and rapid early shifts (0–260 km/h in ~9.5–10.5s).
   * **1/2 Mile (804.7m)**: Rolling high-speed pull demanding sustained top-gear efficiency (0–300 km/h in ~15–17s).
   * **1 Mile (1609.3m)**: Terminal aerodynamic endurance testing full-throttle top speed pinned in 6th gear at redline (310–325 km/h in ~25–28s).

---

## 🎮 Controls

ShiftRacer supports multi-device inputs via Unity's New Input System, with automatic fallback for keyboard, mouse, gamepad, and mobile touchscreens:

| Action | Primary Keyboard | Alternate Keyboard | Mouse | Gamepad | Mobile Touchscreen |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Upshift (Hero)** | **`Left-Shift` / `Right-Shift`** | `E` | `Right-Click` | Right Bumper / `Y` / `Triangle` | Tap `UPSHIFT` button or screen |
| **Stage Bike** | **`Shift`** / `Space` | `Enter` | `Left-Click` | `A` / `Cross` | Tap red Stage button |
| **Select Distance** | `Tab` / `D` | `Left` / `Right Arrow` | Click Tabs | D-pad Left / Right | Tap Distance Tabs (1/4, 1/2, 1 Mile) |
| **Throttle / Rev (Staging)** | `Space` *(Auto in Race)* | `W` / `Up Arrow` | `Left-Click` | Right Trigger / `A` | Hold `THROTTLE` button |
| **Downshift (Recovery)** | `Q` | — | — | Left Bumper / `X` | Tap `DOWNSHIFT` button |
| **Return to Menu** | **`Shift`** / `Space` | `R` / `Enter` | `Left-Click` | `A` / `Cross` | Tap screen or result card |

> **Game Off One-Button Mode:** During the race, wide-open throttle is applied automatically (`autoThrottleDuringRace = true`), transforming the run into a pure test of shift timing accuracy.

---

## 🚦 Race Flow & Features

### 1. Ready & Distance Selection Screen
* Select between **1/4 Mile**, **1/2 Mile**, or **1 Mile** using interactive distance tabs or `TAB` / `D`.
* Clean, non-overlapping HUD layout with generous vertical clearance above the superbike cockpit windshield.
* Tap the red **`PRESS SHIFT / SPACE OR TAP TO STAGE`** button or hit **`SHIFT`** to move to the staging line.

### 2. Staging Countdown
* Pre-rev the 1000cc engine to build launch RPM against the clutch.
* Authentic Christmas tree lights count down: **3... 2... 1... GO!**

### 3. Racing & Dynamic Shift Guidance
* **Horizontal Tachometer Bar**: Displays real-time RPM climbing to the 13,000 RPM redline.
* **Green Shift Window (`11,400 – 12,600 RPM`)**:
  * The **`>> SHIFT NOW! <<`** HUD prompt lights up in glowing cyan.
  * The **`Badge_ShiftNow`** visual icon punches into view with an attention-grabbing pop animation.
  * The on-screen **`UPSHIFT` button** bounces (`1.22x` elastic punch) to invite immediate tapping.
* **Top Gear Intelligence**: Once the superbike reaches **6th gear**, all shift prompts and badges are automatically hidden because there are no higher gears. You can pin the throttle through the finish line without distraction.

### 4. Shift Evaluation & Ratings
* 🟢 **PERFECT!** — Shifted within 11,400–12,600 RPM. Boosts acceleration with 100% drive momentum.
* 🟡 **EARLY SHIFT** — Shifted below 11,400 RPM. Bogs engine torque.
* 🔴 **LATE SHIFT / LIMITER** — Hit 13,000 RPM fuel cut. Drops acceleration until shifted.
* **Flawless Run Award**: Landing 5 clean shifts awards the **★ FLAWLESS RUN ★** rating on the telemetry card.

### 5. Result Telemetry & Controlled Return
* The result card displays elapsed time, trap speed, and shift accuracy with glowing victory/defeat borders.
* **Debounced Return**: Tapping the result screen returns the player to the **Ready Menu** with a **0.45s safety lockout**, preventing accidental auto-replays and letting the player freely change distance before staging again.

---

## 🔊 Audio Architecture (2D Mastered Cockpit Audio)

All bike audio is mastered in 44.1 kHz 16-bit PCM and rendered with 2D cockpit fidelity (`spatialBlend = 0f`), ensuring volume never attenuates down the track:

* **Grid Idle Rumble (2,800 RPM):** An audible idle exhaust rumble plays at 50% volume on the starting grid.
* **Continuous Pitch Scaling:** Engine sound dynamically scales from a deep **0.70x** rumble at idle up to a screaming **2.25x** inline-four howl at 13,000 RPM.
* **Quickshifter Ignition Cut:** Upshifting triggers an authentic 70ms ignition cut with an exhaust backfire bark (`Quickshifter_Bark.wav`).
* **Rev Limiter Stutter:** Bouncing off redline triggers rapid 12.5 Hz fuel-cut stutter pops (`RevLimiter_Stutter.wav`), active only while racing.
* **Finish Line Cut-Off:** Engine audio immediately fast-fades to **0% volume** upon crossing the finish line, allowing the celebratory **Victory Fanfare** to ring out clearly.

---

## 🎨 Visual Atmosphere & Application Branding

* **Custom Application Icon & Logo:**  
  [`Assets/Sprites/App_icon.png`](Assets/Sprites/App_icon.png) is configured as the official application icon and startup splash screen logo across all build targets (WebGL, PC Standalone, and Mobile).
* **Automated Pre-Build Enforcer:**  
  `BuildIconEnforcer` implements Unity's official `IPreprocessBuildWithReport` interface in [`AutoSceneUpdater.cs`](Assets/Scripts/Editor/AutoSceneUpdater.cs), guaranteeing that every single build automatically injects the icon and splash logo before compilation begins.
* **Midnight Drag Strip (URP):**
  * Deep midnight skybox with an overhead 2,000-star canopy.
  * Distance fog concealing track boundaries for infinite horizon depth.
  * Cyber neon milestone gantries marking 1/4 Mile (electric yellow), 1/2 Mile (electric cyan), and 1 Mile (neon magenta finish arch).
  * High-intensity URP Bloom (`1.8x`) and full-face helmet visor vignette framing (`0.35x`).

---

## 🏍️ Superbike Powertrain Physics

* **Continuous Momentum:** Forward speed is physically integrated; vehicle speed never drops artificially during an upshift.
* **Authentic 6-Speed Transmission Bandwidths:**
  * **Gear 1:** 0 – 135 km/h (Ratio: 96.3 RPM per km/h)
  * **Gear 2:** 90 – 178 km/h (Ratio: 73.0 RPM per km/h)
  * **Gear 3:** 130 – 220 km/h (Ratio: 59.1 RPM per km/h)
  * **Gear 4:** 170 – 258 km/h (Ratio: 50.4 RPM per km/h)
  * **Gear 5:** 210 – 292 km/h (Ratio: 44.5 RPM per km/h)
  * **Gear 6:** 250 – 325 km/h (Ratio: 40.0 RPM per km/h)
* **RPM Drop Curves:** Upshifting drops engine RPM directly into the meat of the next gear's powerband (e.g., 12,200 RPM in Gear 1 drops smoothly to ~9,100 RPM in Gear 2).
* **Cockpit Camera Rumble:** Perlin-noise shake scales dynamically with road speed and rev limiter bounce.

---

## 📱 Mobile & Responsive UI Ergonomics

* **Canvas Scaler**: Configured to `ScaleWithScreenSize` (`1920x1080` reference) with balanced `MatchWidthOrHeight = 0.5`, ensuring perfect visual scale on 16:9, modern ultra-wide smartphones (19.5:9 / 20:9), and tablets/iPads (4:3 / 16:10).
* **Hardware Safe Area Adaptation**: Built-in `SafeAreaFitter` dynamically offsets HUD touch controls away from notches, dynamic islands, camera cutouts, and bottom gesture bars.
* **Ergonomic Landscape Touch Controls**:
  * **Right Thumb Zone**: Prominent, touch-reactive **Throttle** and **Upshift** buttons with tactile press-scaling.
  * **Left Thumb Zone**: Dedicated **Downshift** button for recovery.
  * **Interactive Ready Screen**: Tap directly on distance tabs to pick your strip distance, then tap the dedicated Stage button to begin.
  * **Multi-Touch**: Independent tracking allows holding full throttle while tapping the shift paddle with another finger.
* **Shift Input Debounce**: A 220ms shift interval cooldown in `SequentialGearbox.cs` prevents accidental double-shifts from rapid taps.

---

## 🏗️ Architecture & Scripts

All gameplay and engine code is decoupled across modular systems in `Assets/Scripts/`:

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
│   ├── AutoSceneUpdater.cs        # Automatic pre-build icon enforcer & scene UI alignment on load
│   └── DragRaceSceneBuilder.cs    # 1-click generator for track, gantries, lighting, UI canvas, and wiring
└── UI/
    ├── MobileTouchController.cs   # Multi-touch input coordinator & screen quadrant fallback
    ├── OnScreenTouchButton.cs     # Tactile touch button component with pointer-over-UI detection & pop routines
    ├── RaceHUDController.cs        # Cockpit overlay, digital tachometer, distance selector, & result card
    └── SafeAreaFitter.cs          # Hardware notch/island safe area dynamic RectTransform offset
```

---

## 🎨 Asset Registry

| Asset File | Sub-Sprite / Slice | Assigned Inspector Field | Location / Role |
| :--- | :--- | :--- | :--- |
| `App_icon.png` | _(Single Sprite)_ | Global Application Icon & Splash | Desktop/browser icon & splash screen logo |
| `First-Person Superbike Cockpit Overlay.png` | _(Single Sprite)_ | `cockpitOverlaySprite` on `[CANVAS_HUD]` | Fullscreen cockpit fairing, windshield & clip-on handlebars |
| `Tachometer_Dial.png` | _(Single Sprite)_ | `tachometerDialSprite` on `[CANVAS_HUD]` | Analog 14,000 RPM gauge dial face |
| `Rotating Tachometer Needle.png` | _(Single Sprite)_ | `tachometerNeedleSprite` on `[CANVAS_HUD]` | Dynamic RPM needle (pivots from 130° to -130°) |
| `UI_sprites.png` | `Speedometer_Frame` | `speedometerFrameSprite` on `[CANVAS_HUD]` | Backing frame around Digital Speedometer (km/h) |
| `UI_sprites.png` | `Gear_Frame` | `gearFrameSprite` on `[CANVAS_HUD]` | Backing frame around Current Gear Indicator |
| `UI_sprites.png` | `PerfectShifts_Bar` | `perfectShiftsBarSprite` on `[CANVAS_HUD]` | Backing frame behind the 5-shift evaluation dots |
| `UI_sprites.png` | `StartLights_Tree` | `startLightsTreeSprite` on `[CANVAS_HUD]` | Staging Christmas Tree visual element |
| `UI_sprites.png` | `Badge_Perfect` | `badgePerfectSprite` on `[CANVAS_HUD]` | Pop-up badge on optimal gear shift |
| `UI_sprites.png` | `Badge_Early` | `badgeEarlySprite` on `[CANVAS_HUD]` | Pop-up badge on short shift |
| `UI_sprites.png` | `Badge_Late` | `badgeLateSprite` on `[CANVAS_HUD]` | Pop-up badge on rev limiter / late shift |
| `UI_sprites.png` | `Badge_ShiftNow` | `badgeShiftNowSprite` on `[CANVAS_HUD]` | Flashing prompt when entering shift window |
| `UI_sprites.png` | `Hint_Throttle` | `hintThrottleSprite` on `[CANVAS_HUD]` | Mobile touch Throttle button graphic |
| `UI_sprites.png` | `Hint_Upshift` | `hintUpshiftSprite` on `[CANVAS_HUD]` | Mobile touch Upshift button graphic |
| `UI_sprites.png` | `Hint_Downshift` | `hintDownshiftSprite` on `[CANVAS_HUD]` | Mobile touch Downshift button graphic |
| `UI_Rounded_Box.png` | _(Sliced 9-slice)_ | `roundedBoxSprite` on `[CANVAS_HUD]` | Translucent dark frosted panel backing |
| `UI_Rounded_Border.png` | _(Sliced 9-slice)_ | `roundedBorderSprite` on `[CANVAS_HUD]` | Glowing result card border outline |
| `UI_Pill_Bar.png` | _(Sliced 9-slice)_ | `pillBarSprite` on `[CANVAS_HUD]` | Rounded stage button & tachometer bar fill |
| `signals.png` | `Individual Shift Dot States empty` | `dotEmptySprite` on `[CANVAS_HUD]` | Pending shift dot indicator |
| `signals.png` | `Individual Shift Dot States green` | `dotGreenSprite` on `[CANVAS_HUD]` | Successful clean shift indicator |
| `signals.png` | `Individual Shift Dot States red` | `dotRedSprite` on `[CANVAS_HUD]` | Faulty / missed shift indicator |

---

## 🔊 Audio Registry

| Audio Asset (44.1 kHz 16-bit PCM) | Assigned Inspector Field | Target Component | Description / Trigger |
| :--- | :--- | :--- | :--- |
| `Engine_1000cc_Loop.wav` | `engineLoopClip` | `BikeAudioController` on `[GAME_SYSTEMS]` | High-revving 4-cylinder engine loop (dynamically pitch-shifted with RPM) |
| `Quickshifter_Bark.wav` | `shiftBarkClip` | `BikeAudioController` on `[GAME_SYSTEMS]` | Ignition-cut quickshifter exhaust pop & mechanical dog engagement |
| `RevLimiter_Stutter.wav` | `limiterPulseClip` | `BikeAudioController` on `[GAME_SYSTEMS]` | Aggressive 12.5 Hz fuel-cut limiter bounce when bouncing at 13,000 RPM |
| `Countdown_Beep.wav` | `countdownBeepClip` | `BikeAudioController` on `[GAME_SYSTEMS]` | Amber staging countdown cadence tone (3, 2, 1) |
| `GreenLight_Go.wav` | `greenLightGoClip` | `BikeAudioController` on `[GAME_SYSTEMS]` | High punch tone on green light launch (GO!) |
| `VictoryFanfare.wav` | `victoryFanfareClip` | `BikeAudioController` on `[GAME_SYSTEMS]` | Celebratory victory fanfare upon crossing the finish line ahead of rival |

---

## 🚀 Building & Publishing to itch.io

### 1. Build WebGL in Unity

1. Open the project in **Unity 6 (6000.0.85f2)**.
2. Go to **File > Build Settings** (or **Build Profiles**).
3. Select **WebGL** and ensure `Assets/Scenes/DragRace.unity` is included.
4. Click **Build**, choose an export folder (e.g. `Build_WebGL`), and allow Unity to compile.
   *(The built-in `BuildIconEnforcer` will automatically verify and lock `App_icon.png` as the application icon and splash logo).*

### 2. Package for itch.io

1. Open the output `Build_WebGL` folder containing `index.html`, `Build/`, and `TemplateData/`.
2. Select all items inside this folder, right-click, and compress them into a zip archive (e.g. `ShiftRacer_WebGL.zip`).

### 3. itch.io Project Settings

1. Navigate to your **itch.io Dashboard** > **Create New Project**.
2. **Title**: `ShiftRacer` (or `ShiftRacer: Redline Quarter Mile`).
3. **Kind of project**: Select **HTML** *(This file will be played in the browser)*.
4. **Uploads**: Upload `ShiftRacer_WebGL.zip` and check **"This file will be played in the browser"**.
5. **Viewport dimensions**: Set **Width: `1280`**, **Height: `720`** (or `960 × 540`).
6. **Options**:
   * Check **"Fullscreen button"**.
   * Check **"Mobile friendly"** (Orientation: **Landscape**).
7. **Cover Image / Icon**: Upload [`Assets/Sprites/App_icon.png`](Assets/Sprites/App_icon.png).
8. **Jam Submission**: Ensure **GitHub Game Off 2026** is selected.
9. Click **Save & View Page** to play and submit!

---

## 📄 License & Credits

- **Developer:** [AbhiramRavula](https://github.com/AbhiramRavula)
- **Game Jam:** [GitHub Game Off 2026](https://itch.io/jam/game-off-2026)
- **License:** MIT License — Open source magic!

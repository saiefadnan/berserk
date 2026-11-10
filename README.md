# Berserk: Dark Fantasy 3D Action RPG Prototype

[![Unity](https://img.shields.io/badge/Unity-6%20LTS-black?style=for-the-badge&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-blue?style=for-the-badge)](https://unity.com/srp/Universal-Render-Pipeline)
[![Language](https://img.shields.io/badge/Language-C%23-239120?style=for-the-badge&logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Genre](https://img.shields.io/badge/Genre-Character%20Action%20%2F%20Souls--like-darkred?style=for-the-badge)]()

A high-intensity, third-person dark fantasy character action combat prototype inspired by the brutal world of Kentaro Miura's **Berserk**, combined with mechanics from **Dark Souls** and **Sekiro: Shadows Die Twice**. 

Engineered with a focus on tactile game feel, precision animation-driven PhysX hitboxes, responsive combat state machines, and a cinematic game loop.

---

## Technical Highlights & Engineering Systems

### 1. Precision Melee Combat State Machine
* **3-Tier Combo System:** Multi-phase heavy greatsword combo (`attack1` ➔ `attack2` ➔ `attack3`) with lunge velocity curves applied directly during impact frames.
* **Normalized-Time Dodge Cancels:** Allows canceling attack wind-ups into defensive rolls or backsteps once the animation reaches its cancel window (`normTime >= 0.55`).
* **Directional Guard & Parry Clashes:** Guts raises the Dragonslayer to absorb 85% of incoming damage, consuming stamina on impact and spawning directional spark VFX, camera shake, and metallic clash audio.
* **Invulnerability Windows (i-Frames):** Precise dodge-roll window granting temporary damage immunity.

### 2. Physical Weapon Hitbox Architecture (`EnemyWeapon.cs`)
* **Elimination of Phantom Distance Math:** Replaced spherical distance checks with true physical blade hitboxes driven by **Animation Events**.
* **Bone Hierarchy Synchronization:** Overcame `SkinnedMeshRenderer` deformation constraints by parenting kinematic trigger colliders to the animated `mixamorig:Sword_joint` bone.
* **Hit-Once-Per-Swing Tracking:** Active swing windows with automatic damage lockout to prevent multiple hits during a single strike.

### 3. Sekiro-Style Target-Lock System (`TargetLockSystem.cs`)
* **Scoring Heuristic:** Evaluates potential targets using a weighted camera-frustum formula (`score = angle * 1.5 + distance`), prioritizing the enemy the player is actively facing.
* **Line-of-Sight Raycasting:** Prevents locking onto enemies behind terrain, pillars, or obstacles.
* **Orbital Combat Strafing:** Dynamically decouples character forward orientation, allowing Guts to strafe around locked targets with smooth camera pitch and yaw tracking.
* **Deathblow Reticle:** Minimalist HUD reticle that pulses crimson when an enemy is staggered and ready for execution.

### 4. Signature Guts Mechanics
* **Prosthetic Left Arm Cannon:** Quick-aim and firing system launching explosive cannonball projectiles with radial physics impulses, debris smoke, and area-of-effect damage.
* **Beast of Darkness (Rage State Machine):** Accumulates rage through dealt and received damage. Activating the mode unleashes:
  * 2.0x damage and +35% attack speed buffs.
  * Vampiric lifesteal (+5 HP per hit, +20 HP burst on kill).
  * Demonic red eye ribbon trails, procedural blood vignette, and low-frequency heartbeat audio.
* **Visceral Feedback:** Camera shake, dynamic ragdoll activation (`RagdollHandler`), blood splatter decals, and dismemberment mesh baking.

### 5. Game Loop & Death Presentation
* **Lethal Hitstop:** Brutal slow-motion dip (`Time.timeScale = 0.15f`) on mortal strikes before restoring normal time.
* **Dark Fantasy "YOU DIED" Screen:** CanvasGroup-driven dark vignette overlay with animated crimson title scaling and pulsing respawn prompt.
* **Synthesized Audio Design:** Custom-engineered sub-bass mortal impact thud and haunting A-minor bell death chime.
* **Checkpoint & Respawn Loop:** Clean PhysX teleportation, full stat revival (`Health.Revive`), stamina restoration, and combat encounter state reset.

---

## Controls & Keybindings

| Key / Action | Function |
| :--- | :--- |
| **`W, A, S, D`** | Character Movement / Combat Strafe |
| **`Mouse Move`** | Camera Look (Pitch / Yaw) |
| **`Left Shift`** | Sprint (Consumes Stamina) |
| **`Left Mouse Button`** | Melee Attack / Combo Chaining |
| **`Right Mouse Button`** | Guard (Hold) / Aim Left Arm Cannon (when unequipped) |
| **`Space`** | Dodge Roll / Backstep Hop / Respawn (Death Screen) |
| **`Middle Mouse (Scroll Click)`** | Toggle Target-Lock On / Off |
| **`Mouse Scroll Wheel`** | Switch Lock-On Target (Left / Right) |
| **`E`** | Equip / Sheathe Dragonslayer Greatsword |
| **`R`** | Unleash Beast of Darkness (when Rage meter is full) |

---

## System Architecture

```
Assets/Scripts/
├── Player/
│   ├── PlayerMovement.cs        # Locomotion, camera tracking & strafing
│   ├── PlayerCombat.cs          # Combo state machine, guard, lunge impulse & dodge
│   ├── PlayerDeathHandler.cs    # Lethal hitstop, input lockout & respawn
│   ├── Stamina.cs               # Resource consumption & regen delay
│   └── BeastOfDarkness.cs       # Rage mode, stat multipliers & eye trail VFX
├── Combat/
│   ├── EnemyCombat.cs           # Enemy attack chaining & animation event triggers
│   ├── EnemyWeapon.cs           # Physical blade trigger hitbox & IDamageable dispatch
│   ├── Sword.cs                 # Player weapon collision & dismemberment trigger
│   └── ShootMecha.cs            # Cannon projectile launch & explosive AoE
├── Enemy/
│   ├── EnemyController.cs       # Spacing, circling AI & combat tokens
│   ├── RagdollHandler.cs        # Skeleton physics & trigger preservation
│   └── Dismemberment.cs         # Dynamic decapitation & limb severing
├── Core/
│   ├── Health.cs                # IDamageable interface, damage reduction & death event
│   ├── CameraShake.cs           # Procedural trauma-based impulse shake
│   └── TargetLockSystem.cs      # Sekiro lock-on scoring, raycasting & reticle
└── UI/
    ├── DeathScreenUI.cs         # "YOU DIED" canvas fade, scale loom & audio
    ├── HealthBar.cs             # Smooth ghost-fill health bar
    └── CanonWidget.cs           # Cannon reload radial wipe cooldown
```

---

## Getting Started

### Prerequisites
* **Unity 6 (6000.0.x LTS)** or higher.
* **Universal Render Pipeline (URP)** package installed.

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/saiefadnan/berserk.git
   ```
2. Open the project inside **Unity Hub**.
3. Load the main scene:
   `Assets/Scenes/SampleScene.unity`
4. Press **Play** in the Unity Editor to enter combat!

---

## Author
* **Saief Adnan** — Gameplay Programming & Systems Engineering
* GitHub: [@saiefadnan](https://github.com/saiefadnan)

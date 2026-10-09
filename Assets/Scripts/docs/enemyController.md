🧟 What's New & Why It's Scary
🔇 Creepy Stare — randomly stops mid-chase, locks eyes on the player and does nothing for a few seconds. Psychologically unsettling — the player never knows what comes next.
💨 Charge Lunge — brief wind-up pause, then an explosive burst of speed (8f) toward the player with a scream. Punishes players who think they're safe at range.
💀 Fake Death — when "killed", there's a 35% chance it falls down and waits 3–7 seconds, then gets back up when you walk close to loot it. Classic horror trope.
👂 Hearing Range — even if the player breaks line of sight, the zombie can hear them within hearingRange. No hiding behind walls for long.
🦵 Limping — below 40% HP it slows down, but it keeps coming. Makes it feel wounded and desperate rather than just weaker.
🎲 Random Damage — each hit deals between minDamage and maxDamage, so the player can never get comfortable calculating patterns.
🔀 Flanking — 25% chance it tries to circle around instead of running straight at you.
🎵 Ambient Breathing — randomly plays creepy breathing sounds even when just patrolling nearby.

🔧 Setup Checklist in Inspector
FieldWhat to assignscreamClips2–3 horror scream .wav filesbreathingClips2–3 creepy breathing .wav filesbloodSplatterA red particle system on the enemyfakeDeathChance0.35 (35% chance to fake death)stareChance0.15 — increase for more dreadchargeChance0.2 — increase for aggression
Also add a PlayerHealth script with a TakeDamage(int) method on your player if you don't have one yet — the enemy now actively damages them!

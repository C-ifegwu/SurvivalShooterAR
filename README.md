# Survival Shooter AR

This is my custom AR Survival Shooter game built for Android using Unity and ARCore. The goal of the game is to survive against waves of enemies that spawn around you in augmented reality.

## How to Play

1. **Scan the Room:** When you start the game, look around with your phone's camera to scan the floor.
2. **Deploy the Arena:** Once a flat surface is detected, you will see my name (Chibueze Victor Ifegwu) on the floor. Tap the screen to lock the combat zone in place.
3. **Survive:** Zombies and soldiers will start spawning on the floor you just scanned. 
   - **Zombies** will run straight at you and attack up close.
   - **Soldiers** will keep their distance and shoot projectiles at you.
4. **Fight Back:** Tap or hold the screen to shoot back. Keep an eye on your health and try to survive as long as the timer allows!

## Features included
- **AR Plane Tracking:** Detects real-world floors to place the game.
- **Custom Visualizer:** Displays my full name on the tracked plane as required by the assignment.
- **Two Enemy Types:** Melee zombies and shooting soldiers.
- **Audio & UI:** Includes background music, shooting sound effects, a fully working menu, and a leaderboard to track your top scores.
- **Optimized Performance:** Built with performance in mind using object recycling (pooling) so the game doesn't stutter on mobile devices when shooting.

## Running the Game

**On an Android Device:**
Make sure your phone supports Google Play Services for AR. You can install the game by transferring the `SurvivalShooterAR.apk` build to your phone, or by connecting your phone via USB and building it directly from Unity.

**In Unity (XR Simulation):**
If you want to test it on a computer, open the main scene, hit Play, and use the right mouse button + WASD to look around and move in the simulated AR room. Click to shoot.

## Credits
- Models: Mixamo Zombie Pack and Low Poly Soldiers
- Audio: Laser Weapons Sound Pack and Zombie Horror Package
- Built by Chibueze Victor Ifegwu

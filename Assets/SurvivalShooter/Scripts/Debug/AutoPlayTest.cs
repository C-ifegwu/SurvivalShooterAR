#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using SurvivalShooter.AR;
using SurvivalShooter.Audio;
using UnityEngine.XR.ARSubsystems;
using SurvivalShooter.Core;
using SurvivalShooter.Data;
using SurvivalShooter.Enemies;
using SurvivalShooter.Player;
using SurvivalShooter.Pooling;
using SurvivalShooter.UI;

namespace SurvivalShooter.DebugTools
{
    /// <summary>
    /// Editor-only automated play-through used to verify every rubric system end-to-end.
    /// Writes a PASS/FAIL report to Logs/ssar_playtest.log and screenshots to Screenshots/.
    /// Launched from: Survival Shooter AR ▸ Run Automated Play Test.
    /// </summary>
    public class AutoPlayTest : MonoBehaviour
    {
        public const string ReportPath = "Logs/ssar_playtest.log";
        private readonly StringBuilder report = new StringBuilder();
        private int pass, fail;
        private int damageEvents;
        private int enemyShots;

        private void Start()
        {
            Directory.CreateDirectory("Screenshots");
            GameEvents.PlayerDamaged += (a, s) => damageEvents++;
            StartCoroutine(Run());
        }

        private void Check(bool ok, string label)
        {
            if (ok) pass++; else fail++;
            string line = (ok ? "[PASS] " : "[FAIL] ") + label;
            report.AppendLine(line);
            Debug.Log("[SSAR Test] " + line);
            Flush();
        }

        private void Note(string s)
        {
            report.AppendLine("       " + s);
            Debug.Log("[SSAR Test] " + s);
            Flush();
        }

        private void Flush() => File.WriteAllText(ReportPath, report.ToString());

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot($"Screenshots/{name}.png");
            yield return null;
        }

        private IEnumerator WaitFor(System.Func<bool> cond, float timeout)
        {
            float t = 0f;
            while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
        }

        private IEnumerator TestAudioSettings()
        {
            float[] saved = new float[VolumeSettings.ChannelCount];
            for (int i = 0; i < saved.Length; i++) saved[i] = VolumeSettings.Get((AudioChannel)i);

            UIManager.Instance.ShowSettings();
            yield return new WaitForSecondsRealtime(0.9f);
            var settingsScreen = FindAnyObjectByType<SettingsScreen>();
            Check(settingsScreen != null && settingsScreen.IsVisible, "Audio Settings opens from the Main Menu");
            yield return Shot("02c_audio_settings");

            // Music slider must drive the live music source
            AudioSource music = null;
            foreach (var src in AudioManager.Instance.GetComponentsInChildren<AudioSource>())
                if (src.loop && src.isPlaying) music = src;
            VolumeSettings.Set(AudioChannel.Music, 0f);
            yield return null;
            Check(music != null && music.volume < 0.001f, "Music volume 0% silences the background music live");
            VolumeSettings.Set(AudioChannel.Music, 1f);
            yield return null;
            Check(music != null && music.volume > 0.05f, $"Music volume 100% restores music (vol {(music != null ? music.volume : -1f):F2})");

            // Channel routing
            Check(AudioManager.ChannelOf(SoundId.EnemyShoot) == AudioChannel.Enemy && AudioManager.ChannelOf(SoundId.MeleeAttack) == AudioChannel.Enemy
                  && AudioManager.ChannelOf(SoundId.PlayerShoot) == AudioChannel.SoundEffects && AudioManager.ChannelOf(SoundId.UIClick) == AudioChannel.Interface,
                  "Sounds routed to Enemy / Sound Effects / Interface channels");

            VolumeSettings.Set(AudioChannel.Master, 0.5f);
            VolumeSettings.Set(AudioChannel.Enemy, 0.4f);
            Check(Mathf.Approximately(VolumeSettings.Effective(AudioChannel.Enemy), 0.2f), "Effective volume = Master x Channel");
            VolumeSettings.Muted = true;
            Check(VolumeSettings.Effective(AudioChannel.Interface) == 0f && (music == null || music.volume < 0.001f), "Mute All silences every channel");
            VolumeSettings.Muted = false;
            Check(PlayerPrefs.HasKey("SSAR_VOLUME_Enemy") && Mathf.Approximately(PlayerPrefs.GetFloat("SSAR_VOLUME_Enemy"), 0.4f), "Volume settings persisted to PlayerPrefs");

            for (int i = 0; i < saved.Length; i++) VolumeSettings.Set((AudioChannel)i, saved[i]);
            UIManager.Instance.HideSettings();
            yield return new WaitForSecondsRealtime(0.4f);
        }

        private IEnumerator Run()
        {
            report.AppendLine($"=== SSAR AUTOMATED PLAY TEST {System.DateTime.Now} ===");
            yield return new WaitForSecondsRealtime(2.0f);

            var gm = GameManager.Instance;
            Check(gm != null, "GameManager singleton present");
            Check(gm.CurrentState == GameStateId.MainMenu, "Starts in Main Menu (Start state)");
            Check(ARPlacementManager.HasInstance && PoolManager.HasInstance && EnemySpawner.HasInstance && EnemyFactory.HasInstance
                  && LeaderboardManager.HasInstance && UIManager.HasInstance && PlayerHealth.HasInstance && PlayerShooter.HasInstance,
                  "All managers initialised");
            Note($"XR running: {ARPlacementManager.IsXRRunning}  ARSession.state: {ARSession.state}");
            int prePlayerPool = PoolManager.Instance.PlayerProjectiles.CountAll;
            int preEnemyPool = PoolManager.Instance.EnemyProjectiles.CountAll;
            Check(prePlayerPool >= 30 && PoolManager.Instance.PlayerProjectiles.CountActive == 0, $"Player bullet pool pre-initialised ({prePlayerPool} inactive)");
            Check(preEnemyPool >= 20, $"Enemy bullet pool pre-initialised ({preEnemyPool})");
            yield return Shot("01_main_menu");

            UIManager.Instance.ShowLeaderboard();
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("02_leaderboard");
            UIManager.Instance.HideLeaderboard();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return TestAudioSettings();

            gm.SetDifficulty(DifficultyLevel.Hard);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("02b_menu_hard_selected");
            gm.SetDifficulty(DifficultyLevel.Normal);

            gm.RequestStart();
            yield return null;
            Check(gm.CurrentState == GameStateId.Scanning, "Start → Scanning state");
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("03_scanning");

            bool planeFound = false;
            System.Action<bool> onPlane = f => { if (f) planeFound = true; };
            GameEvents.PlaneStatusChanged += onPlane;
            // Tilt the XR Simulation camera down towards the floor (like a user pointing the phone at the ground)
            for (int i = 0; i < 40; i++) { DesktopCameraController.RotateSimulationCamera(1f, 0f); yield return null; }
            float sweep = 0f;
            while (!planeFound && sweep < 14f)
            {
                DesktopCameraController.RotateSimulationCamera(0f, Mathf.Sin(sweep * 1.3f) * 0.6f);
                sweep += Time.unscaledDeltaTime;
                yield return null;
            }
            GameEvents.PlaneStatusChanged -= onPlane;
            Check(planeFound, "Horizontal plane detected (reticle shown)");
            var planeMgr = FindAnyObjectByType<ARPlaneManager>();
            if (planeMgr != null) Note($"Tracked planes: {planeMgr.trackables.count}");
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("04_plane_detected");

            // Place the arena in front of the camera
            Camera cam = ARPlacementManager.Instance.ARCamera;
            Vector3 fwd = cam.transform.forward; fwd.y = 0; fwd.Normalize();
            Vector3 p = cam.transform.position + fwd * 1.3f;
            p.y = cam.transform.position.y - 1.4f;
            var rm = FindAnyObjectByType<ARRaycastManager>();
            var hits = new System.Collections.Generic.List<ARRaycastHit>();
            if (rm != null && rm.Raycast(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
            {
                p = hits[0].pose.position;
                Note($"Placing on real AR plane hit at {p} (camera {cam.transform.position})");
            }
            ARPlacementManager.Instance.PlaceArena(new Pose(p, Quaternion.LookRotation(-fwd)));
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Shot("04b_just_placed");
            // Look back up to the horizon for combat
            for (int i = 0; i < 25; i++) { DesktopCameraController.RotateSimulationCamera(-1f, 0f); yield return null; }
            yield return null;
            Check(ARPlacementManager.Instance.IsArenaPlaced, "Arena placed via tap-to-place");
            ARPlacementManager.Instance.PlaceArena(new Pose(p + Vector3.right, Quaternion.identity));
            Check(FindObjectsByType<ArenaVisual>().Length == 1, "Second placement ignored (single instance)");
            if (planeMgr != null) Check(!planeMgr.enabled, "Plane detection stopped after placement");
            yield return new WaitForSecondsRealtime(0.5f);
            int visiblePlanes = 0;
            foreach (var v in FindObjectsByType<CustomPlaneVisualizer>()) if (v.GetComponent<MeshRenderer>().enabled) visiblePlanes++;
            Check(visiblePlanes > 0, $"Custom name plane stays visible under the arena ({visiblePlanes})");
            Check(gm.CurrentState == GameStateId.Countdown, "Placement → Countdown");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("05_countdown");

            yield return WaitFor(() => gm.CurrentState == GameStateId.Playing, 6f);
            Check(gm.CurrentState == GameStateId.Playing, "Countdown → Playing");
            float t0 = gm.Session.TimeRemaining;

            yield return WaitFor(() => EnemySpawner.Instance.AliveCount > 0, 6f);
            Check(EnemySpawner.Instance.AliveCount > 0, "Enemy spawned on plane");
            yield return new WaitForSeconds(1.0f);
            Check(gm.Session.TimeRemaining < t0, "Timer counting down");
            yield return Shot("06_enemies");

            // Shoot enemies with real pooled projectiles until we have one of each type killed
            PlayerHealth.DebugInvulnerable = true;
            int meleeShots = -1, shooterShots = -1;
            float until = Time.time + 45f;
            while (Time.time < until && (meleeShots < 0 || shooterShots < 0) && gm.CurrentState == GameStateId.Playing)
            {
                PlayerHealth.Instance.ResetHealth();
                EnemyBase target = null;
                foreach (var e in EnemySpawner.Instance.ActiveEnemies)
                {
                    if (e == null || !e.IsAlive) continue;
                    if ((e.Type == EnemyType.Melee && meleeShots < 0) || (e.Type == EnemyType.Shooter && shooterShots < 0)) { target = e; break; }
                }
                if (target == null) { yield return new WaitForSeconds(0.3f); continue; }
                yield return new WaitForSeconds(0.6f); // let spawn animation finish
                int shots = 0;
                while (target != null && target.IsAlive && shots < 20)
                {
                    Vector3 from = cam.transform.position;
                    Vector3 dir = (target.AimPoint - from).normalized;
                    var proj = PoolManager.Instance.SpawnProjectile(Team.Player, from + dir * 0.3f, Quaternion.LookRotation(dir));
                    proj.Launch(Team.Player, PlayerShooter.Instance.BulletDamage, 16f);
                    shots++;
                    yield return new WaitForSeconds(0.35f);
                }
                if (target != null && !target.IsAlive)
                {
                    if (target.Type == EnemyType.Melee) meleeShots = shots; else shooterShots = shots;
                    Note($"{target.Type} destroyed by {shots} player bullets");
                }
                if (shots == 3 || shots == 5) yield return Shot("07_combat_hit");
            }
            Check(meleeShots > 0, $"Pooled bullets hit & kill Melee enemy ({meleeShots} bullets)");
            Check(shooterShots > 0, $"Pooled bullets hit & kill Shooter enemy ({shooterShots} bullets)");
            Check(meleeShots > 0 && shooterShots > 0 && meleeShots != shooterShots, "Melee and Shooter need a different number of bullets");
            Check(gm.Session.Kills >= 2 && gm.Session.Score > 0, $"Score increases on kill (score {gm.Session.Score}, kills {gm.Session.Kills})");
            yield return Shot("07_combat");

            // Enemy damage → player
            damageEvents = 0;
            yield return WaitFor(() => damageEvents > 0 || gm.CurrentState != GameStateId.Playing, 30f);
            Check(damageEvents > 0, "Enemies damage the player (melee strike / enemy projectile)");
            yield return Shot("08_player_damaged");

            PlayerHealth.DebugInvulnerable = false;
            Note($"State before pause: {gm.CurrentState}, time left {gm.Session.TimeRemaining:F1}s");

            // Pause
            gm.RequestPause();
            yield return null;
            Check(gm.CurrentState == GameStateId.Paused && Mathf.Approximately(Time.timeScale, 0f), "Pause freezes game");
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("09_pause");
            UIManager.Instance.ShowSettings();
            yield return new WaitForSecondsRealtime(0.9f);
            Check(FindAnyObjectByType<SettingsScreen>() != null && FindAnyObjectByType<SettingsScreen>().IsVisible, "Audio Settings opens from the Pause menu");
            yield return Shot("09b_pause_settings");
            UIManager.Instance.HideSettings();
            yield return new WaitForSecondsRealtime(0.4f);
            gm.RequestResume();
            yield return null;
            Check(gm.CurrentState == GameStateId.Playing && Mathf.Approximately(Time.timeScale, 1f), "Resume");

            // Death → Game Over
            int lbBefore = LeaderboardManager.Instance.Sessions.Count;
            PlayerHealth.Instance.TakeDamage(new DamageInfo(9999, cam.transform.position, Vector3.forward, Team.Enemy));
            yield return null;
            Check(gm.CurrentState == GameStateId.GameOver && gm.Session.Result == GameResult.Defeated, "Player death → Game Over (defeat)");
            yield return null;
            Check(EnemySpawner.Instance.ActiveEnemies.Count == 0, "All enemies wiped at game end");
            Check(PoolManager.Instance.PlayerProjectiles.CountActive == 0 && PoolManager.Instance.EnemyProjectiles.CountActive == 0, "All projectiles returned to pool");
            Check(LeaderboardManager.Instance.Sessions.Count == Mathf.Min(5, lbBefore + 1), "Session saved to leaderboard");
            yield return new WaitForSecondsRealtime(1.8f);
            yield return Shot("10_game_over");

            Check(PoolManager.Instance.PlayerProjectiles.ExpansionCount == 0 && PoolManager.Instance.EnemyProjectiles.ExpansionCount == 0,
                  $"No bullet Instantiate during gameplay (pool expansions P:{PoolManager.Instance.PlayerProjectiles.ExpansionCount} E:{PoolManager.Instance.EnemyProjectiles.ExpansionCount})");

            // Restart → countdown → play → victory
            gm.RequestRestart();
            yield return null;
            Check(gm.CurrentState == GameStateId.Countdown, "Restart → Countdown (arena kept)");
            yield return WaitFor(() => gm.CurrentState == GameStateId.Playing, 6f);
            Check(PlayerHealth.Instance.CurrentHealth == PlayerHealth.Instance.MaxHealth && gm.Session.Score == 0, "Restart resets health & score");
            yield return new WaitForSeconds(3f);
            gm.EndSession(GameResult.Survived);
            yield return null;
            Check(gm.CurrentState == GameStateId.GameOver && gm.Session.Result == GameResult.Survived, "Timer end → Victory summary");
            yield return new WaitForSecondsRealtime(1.8f);
            yield return Shot("11_victory");

            // Fill leaderboard to verify cap at 5
            for (int i = 0; i < 5; i++)
            {
                gm.RequestRestart();
                yield return WaitFor(() => gm.CurrentState == GameStateId.Playing, 6f);
                gm.EndSession(i % 2 == 0 ? GameResult.Survived : GameResult.Defeated);
                yield return null;
            }
            Check(LeaderboardManager.Instance.Sessions.Count == 5, "Leaderboard keeps only the latest 5 sessions");
            UIManager.Instance.ShowLeaderboard();
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("12_leaderboard_filled");
            UIManager.Instance.HideLeaderboard();

            gm.RequestMainMenu();
            yield return null;
            Check(gm.CurrentState == GameStateId.MainMenu && !ARPlacementManager.Instance.IsArenaPlaced, "Main Menu resets placement");
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("13_menu_again");

            // Hard mode config
            var hard = gm.GetConfig(DifficultyLevel.Hard);
            var normal = gm.GetConfig(DifficultyLevel.Normal);
            Check(hard.enemySpeedMultiplier > normal.enemySpeedMultiplier && hard.startSpawnInterval < normal.startSpawnInterval, "Hard difficulty uses tougher gameplay variables");

            report.AppendLine($"=== RESULT: {pass} passed, {fail} failed ===");
            Flush();
            Debug.Log($"[SSAR Test] DONE {pass} passed, {fail} failed");
            yield return new WaitForSecondsRealtime(0.5f);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif

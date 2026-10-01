using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using SurvivalShooter.UI;
using static SurvivalShooter.EditorTools.BuilderUtil;

namespace SurvivalShooter.EditorTools
{
    /// <summary>
    /// Generates the full UI (design system: dark glass cards, pill buttons, cyan accent,
    /// Orbitron titles / Rajdhani HUD numerals / Poppins body) and wires every screen.
    /// Reference resolution 1080×1920 portrait.
    /// </summary>
    public static class SurvivalShooterUIBuilder
    {
        // ---------------------------------------------------------------- Design tokens
        private static readonly Color Accent = new Color(0.25f, 0.91f, 1f);
        private static readonly Color Danger = new Color(1f, 0.25f, 0.38f);
        private static readonly Color Amber = new Color(1f, 0.7f, 0.22f);
        private static readonly Color Success = new Color(0.24f, 1f, 0.63f);
        private static readonly Color Glass = new Color(0.035f, 0.07f, 0.11f, 0.84f);
        private static readonly Color GlassSoft = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color Stroke = new Color(1f, 1f, 1f, 0.13f);
        private static readonly Color TextPrimary = new Color(0.93f, 0.97f, 1f);
        private static readonly Color TextSecondary = new Color(0.56f, 0.65f, 0.73f);
        private static readonly Color TextOnAccent = new Color(0.02f, 0.07f, 0.1f);

        private static TMP_FontAsset fTitle, fHud, fHudSemi, fBody, fBodyMedium;
        private static Material mTitleGlow, mHudShadow, mBodyShadow;

        private static Sprite sRound, sRoundOutline, sPill, sPillOutline, sSmall, sCircle, sRing, sWhite, sGradient, sVignette, sCrosshair, sHitMarker;

        // ================================================================= Fonts

        public static void BuildFonts()
        {
            EnsureFolder(FontsOut);
            fTitle = FontAsset("Orbitron-ExtraBold", "F_Orbitron");
            fHud = FontAsset("Rajdhani-Bold", "F_Rajdhani_Bold");
            fHudSemi = FontAsset("Rajdhani-SemiBold", "F_Rajdhani_SemiBold");
            fBody = FontAsset("Poppins-Regular", "F_Poppins");
            fBodyMedium = FontAsset("Poppins-Medium", "F_Poppins_Medium");

            mTitleGlow = Preset(fTitle, "F_Orbitron_Glow", glow: Accent, underlay: false);
            mHudShadow = Preset(fHud, "F_Rajdhani_Shadow", glow: null, underlay: true);
            mBodyShadow = Preset(fBody, "F_Poppins_Shadow", glow: null, underlay: true);
        }

        private static TMP_FontAsset FontAsset(string ttf, string outName)
        {
            string path = $"{FontsOut}/{outName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var font = Load<Font>($"{Art}/Fonts/{ttf}.ttf");
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = outName;
            AssetDatabase.CreateAsset(fa, path);
            fa.atlasTexture.name = outName + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
            fa.material.name = outName + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~·—•▸");
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        private static Material Preset(TMP_FontAsset font, string name, Color? glow, bool underlay)
        {
            string path = $"{FontsOut}/{name}.mat";
            AssetDatabase.DeleteAsset(path);
            var m = new Material(font.material) { name = name };
            if (glow.HasValue)
            {
                m.EnableKeyword("GLOW_ON");
                m.SetColor("_GlowColor", new Color(glow.Value.r, glow.Value.g, glow.Value.b, 0.55f));
                m.SetFloat("_GlowOuter", 0.35f);
                m.SetFloat("_GlowInner", 0.05f);
                m.SetFloat("_GlowPower", 0.8f);
                m.SetFloat("_GlowOffset", 0f);
            }
            if (underlay)
            {
                m.EnableKeyword("UNDERLAY_ON");
                m.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.55f));
                m.SetFloat("_UnderlayOffsetX", 0.3f);
                m.SetFloat("_UnderlayOffsetY", -0.4f);
                m.SetFloat("_UnderlaySoftness", 0.6f);
                m.SetFloat("_UnderlayDilate", 0.1f);
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static void EnsureFonts()
        {
            if (fTitle == null) BuildFonts();
        }

        // ================================================================= Entry

        public static void BuildUI()
        {
            EnsureFonts();
            sRound = Sprite("UI_RoundRect"); sRoundOutline = Sprite("UI_RoundRectOutline"); sPill = Sprite("UI_Pill"); sPillOutline = Sprite("UI_PillOutline");
            sSmall = Sprite("UI_RoundRectSmall"); sCircle = Sprite("UI_Circle"); sRing = Sprite("UI_Ring");
            sWhite = Sprite("UI_White"); sGradient = Sprite("UI_GradientV"); sVignette = Sprite("UI_Vignette");
            sCrosshair = Sprite("UI_Crosshair"); sHitMarker = Sprite("UI_HitMarker");

            // Event system (Input System UI module)
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();

            // Canvas
            var canvasGo = new GameObject("UI Canvas", typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            canvasGo.AddComponent<AdaptiveCanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform root = canvasGo.transform;

            // Backdrop for menus (lets camera feed show through faintly)
            var backdrop = Stretch(root, "MenuBackdrop");
            var backdropGroup = backdrop.gameObject.AddComponent<CanvasGroup>();
            backdropGroup.blocksRaycasts = false;
            Img(backdrop, "Tint", sWhite, new Color(0.01f, 0.03f, 0.05f, 0.74f), Stretched());
            var grad = Img(backdrop, "Gradient", sGradient, new Color(0.01f, 0.03f, 0.05f, 0.9f), Stretched());
            grad.type = Image.Type.Simple;

            // Full-screen feedback layers (outside safe area)
            var lowHp = Img(root, "LowHealthVignette", sVignette, new Color(1f, 0.1f, 0.15f, 0f), Stretched());
            var dmg = Img(root, "DamageVignette", sVignette, new Color(1f, 0.1f, 0.15f, 0f), Stretched());

            var safe = Stretch(root, "SafeArea");
            safe.gameObject.AddComponent<SafeArea>();

            var mainMenu = BuildMainMenu(safe);
            var scanning = BuildScanning(safe);
            var hud = BuildHUD(safe, dmg, lowHp);
            var pause = BuildPause(safe);
            var gameOver = BuildGameOver(safe);
            var leaderboard = BuildLeaderboard(root);
            var settings = BuildSettings(root);
            // Overlays get their own nested canvas so they always draw above every other screen
            MakeOverlayCanvas(leaderboard.gameObject, 20);
            MakeOverlayCanvas(settings.gameObject, 30);

            foreach (var s in new UIScreen[] { mainMenu, scanning, hud, pause, gameOver, leaderboard, settings }) s.gameObject.SetActive(false);
            var ui = canvasGo.AddComponent<UIManager>();
            Set(ui, "mainMenu", mainMenu);
            Set(ui, "scanning", scanning);
            Set(ui, "hud", hud);
            Set(ui, "pause", pause);
            Set(ui, "gameOver", gameOver);
            Set(ui, "leaderboard", leaderboard);
            Set(ui, "settings", settings);
            Set(ui, "menuBackdrop", backdropGroup);
        }

        // ================================================================= Screens

        private static MainMenuScreen BuildMainMenu(Transform parent)
        {
            var screen = CreateScreen<MainMenuScreen>(parent, "MainMenuScreen", out RectTransform content);

            // Decorative ring + title block
            var ring = Img(content, "DecoRing", sRing, new Color(Accent.r, Accent.g, Accent.b, 0.07f), At(Top, Center, new Vector2(0, -420), new Vector2(980, 980)));
            var crossDeco = Img(content, "DecoCross", sCrosshair, new Color(Accent.r, Accent.g, Accent.b, 0.05f), At(Top, Center, new Vector2(0, -420), new Vector2(620, 620)));

            var title = Rect(content, "TitleBlock", At(Top, new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(1000, 560)));
            var tag = Img(title, "Tag", sPill, new Color(Accent.r, Accent.g, Accent.b, 0.12f), At(Top, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(470, 64)));
            Img(tag.rectTransform, "Stroke", sPillOutline, new Color(Accent.r, Accent.g, Accent.b, 0.45f), Stretched());
            Txt(tag.rectTransform, "Label", "AR COMBAT EXPERIENCE", fHud, 30, Accent, TextAlignmentOptions.Center, Stretched(), spacing: 12);
            var t1 = Txt(title, "Survival", "SURVIVAL", fTitle, 122, TextPrimary, TextAlignmentOptions.Center, At(Top, new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(1000, 150)), spacing: 4);
            var t2 = Txt(title, "Shooter", "SHOOTER", fTitle, 122, Accent, TextAlignmentOptions.Center, At(Top, new Vector2(0.5f, 1f), new Vector2(0, -270), new Vector2(1000, 150)), spacing: 4);
            t2.fontSharedMaterial = mTitleGlow;
            Txt(title, "Subtitle", "Hold your ground. Survive the horde.", fBody, 36, TextSecondary, TextAlignmentOptions.Center, At(Top, new Vector2(0.5f, 1f), new Vector2(0, -440), new Vector2(1000, 60)));

            // Difficulty
            Txt(content, "DifficultyLabel", "SELECT DIFFICULTY", fHud, 30, TextSecondary, TextAlignmentOptions.Center, At(Bottom, Center, new Vector2(0, 930), new Vector2(800, 50)), spacing: 10);
            var normal = DifficultyCard(content, "NormalCard", "NORMAL", "Shield", new Vector2(-240, 770), Accent,
                out Image nBg, out Image nOutline, out TMP_Text nTitle, out TMP_Text nDesc, out Image nIcon);
            var hard = DifficultyCard(content, "HardCard", "HARD", "Bolt", new Vector2(240, 770), Danger,
                out Image hBg, out Image hOutline, out TMP_Text hTitle, out TMP_Text hDesc, out Image hIcon);

            // Primary + secondary actions
            var glow = Img(content, "StartGlow", sPill, new Color(Accent.r, Accent.g, Accent.b, 0.28f), At(Bottom, Center, new Vector2(0, 520), new Vector2(920, 190)));
            var start = MakeButton(content, "StartButton", "START MISSION", "Play", ButtonStyle.Primary, At(Bottom, Center, new Vector2(0, 520), new Vector2(880, 158)), 58);
            var lb = MakeButton(content, "LeaderboardButton", "LEADERBOARD", "Leaderboard", ButtonStyle.Secondary, At(Bottom, Center, new Vector2(0, 345), new Vector2(880, 128)), 44);

            var best = Txt(content, "BestScore", "BEST SCORE 0", fHudSemi, 34, TextSecondary, TextAlignmentOptions.Center, At(Bottom, Center, new Vector2(0, 215), new Vector2(900, 50)), spacing: 6);
            Txt(content, "Credit", "DEVELOPED BY  CHIBUEZE VICTOR IFEGWU", fHudSemi, 26, new Color(1, 1, 1, 0.35f), TextAlignmentOptions.Center, At(Bottom, Center, new Vector2(0, 90), new Vector2(1000, 40)), spacing: 8);

            Set(screen, "startButton", start);
            Set(screen, "leaderboardButton", lb);
            Set(screen, "normalButton", normal);
            Set(screen, "hardButton", hard);
            Set(screen, "normalBg", nBg); Set(screen, "hardBg", hBg);
            Set(screen, "normalOutline", nOutline); Set(screen, "hardOutline", hOutline);
            Set(screen, "normalTitle", nTitle); Set(screen, "hardTitle", hTitle);
            Set(screen, "normalDesc", nDesc); Set(screen, "hardDesc", hDesc);
            Set(screen, "normalIcon", nIcon); Set(screen, "hardIcon", hIcon);
            Set(screen, "bestScoreText", best);
            Set(screen, "titleBlock", title);
            Set(screen, "startGlow", glow.rectTransform);
            Set(screen, "decoRing", ring.rectTransform);
            var gear = MakeIconButton(content, "SettingsButton", "Settings", At(TopRight, Center, new Vector2(-95, -95), new Vector2(116, 116)));
            Set(screen, "settingsButton", gear);
            return screen;
        }

        private static Button DifficultyCard(RectTransform parent, string name, string label, string icon, Vector2 pos, Color color,
            out Image bg, out Image outline, out TMP_Text title, out TMP_Text desc, out Image iconImg)
        {
            bg = Img(parent, name, sRound, GlassSoft, At(Bottom, Center, pos, new Vector2(456, 240)), raycast: true);
            outline = Img(bg.rectTransform, "Outline", sRoundOutline, Stroke, Stretched());
            iconImg = Img(bg.rectTransform, "Icon", Icon(icon), color, At(Top, new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(64, 64)), sliced: false);
            iconImg.preserveAspect = true;
            title = Txt(bg.rectTransform, "Title", label, fHud, 52, TextPrimary, TextAlignmentOptions.Center, At(Center, Center, new Vector2(0, -8), new Vector2(420, 60)), spacing: 8);
            desc = Txt(bg.rectTransform, "Desc", "", fBody, 25, TextSecondary, TextAlignmentOptions.Center, At(Bottom, Center, new Vector2(0, 42), new Vector2(430, 40)));
            var btn = bg.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = bg;
            bg.gameObject.AddComponent<UIButtonFX>();
            return btn;
        }

        private static ScanningScreen BuildScanning(Transform parent)
        {
            var screen = CreateScreen<ScanningScreen>(parent, "ScanningScreen", out RectTransform content);

            var back = MakeIconButton(content, "BackButton", "Back", At(TopLeft, Center, new Vector2(95, -95), new Vector2(116, 116)));
            Txt(content, "Header", "DEPLOY COMBAT ZONE", fHud, 40, TextPrimary, TextAlignmentOptions.Center, At(Top, Center, new Vector2(0, -95), new Vector2(700, 60)), spacing: 10, material: mHudShadow);

            var pulse = Img(content, "TapPulse", sRing, new Color(Success.r, Success.g, Success.b, 0.9f), At(Center, Center, Vector2.zero, new Vector2(300, 300)));

            var card = Img(content, "Card", sRound, Glass, At(Bottom, Center, new Vector2(0, 250), new Vector2(980, 400)));
            var outline = Img(card.rectTransform, "Outline", sRoundOutline, new Color(Amber.r, Amber.g, Amber.b, 0.55f), Stretched());
            var phoneBg = Img(card.rectTransform, "PhoneBg", sCircle, new Color(1, 1, 1, 0.05f), At(Left, Center, new Vector2(150, 30), new Vector2(190, 190)));
            var phone = Img(phoneBg.rectTransform, "Phone", Icon("Phone"), TextPrimary, At(Center, Center, Vector2.zero, new Vector2(120, 120)), sliced: false);
            var dot = Img(card.rectTransform, "StatusDot", sCircle, Amber, At(TopLeft, Center, new Vector2(300, -78), new Vector2(22, 22)));
            var title = Txt(card.rectTransform, "Title", "SCANNING FOR A FLOOR", fHud, 50, TextPrimary, TextAlignmentOptions.Left, At(TopLeft, new Vector2(0, 0.5f), new Vector2(330, -78), new Vector2(620, 60)), spacing: 4);
            var body = Txt(card.rectTransform, "Body", "Slowly sweep your phone across the ground", fBodyMedium, 31, TextPrimary, TextAlignmentOptions.TopLeft, At(TopLeft, new Vector2(0, 1f), new Vector2(290, -125), new Vector2(640, 90)), wrap: true);
            var hint = Txt(card.rectTransform, "Hint", "", fBody, 25, TextSecondary, TextAlignmentOptions.TopLeft, At(TopLeft, new Vector2(0, 1f), new Vector2(290, -225), new Vector2(660, 150)), wrap: true);

            Set(screen, "backButton", back);
            Set(screen, "titleText", title);
            Set(screen, "bodyText", body);
            Set(screen, "hintText", hint);
            Set(screen, "phoneIcon", phone.rectTransform);
            Set(screen, "statusDot", dot);
            Set(screen, "cardOutline", outline);
            Set(screen, "tapPulse", pulse.rectTransform);
            return screen;
        }

        private static HUDScreen BuildHUD(Transform parent, Image damageVignette, Image lowHpVignette)
        {
            var screen = CreateScreen<HUDScreen>(parent, "HUDScreen", out RectTransform content);
            var shake = Stretch(content, "ShakeRoot");

            // Offscreen indicators (behind everything else in HUD)
            var indicatorsRt = Stretch(shake, "OffscreenIndicators");
            var arrow = Img(indicatorsRt, "ArrowTemplate", Icon("Arrow"), Danger, At(Center, Center, Vector2.zero, new Vector2(64, 64)), sliced: false);
            var ind = indicatorsRt.gameObject.AddComponent<OffscreenIndicators>();
            Set(ind, "arrowTemplate", arrow.rectTransform);

            // Pause
            var pauseBtn = MakeIconButton(shake, "PauseButton", "Pause", At(TopLeft, Center, new Vector2(90, -90), new Vector2(108, 108)));

            // Timer pill (center top)
            var pill = Img(shake, "TimerPill", sPill, new Color(0.04f, 0.08f, 0.13f, 0.78f), At(Top, Center, new Vector2(0, -90), new Vector2(330, 112)));
            Img(pill.rectTransform, "Stroke", sPillOutline, Stroke, Stretched());
            Img(pill.rectTransform, "Clock", Icon("Clock"), Accent, At(Left, Center, new Vector2(62, 0), new Vector2(46, 46)), sliced: false);
            var timeText = Txt(pill.rectTransform, "Time", "01:30", fHud, 68, TextPrimary, TextAlignmentOptions.Center, At(Center, Center, new Vector2(28, 2), new Vector2(230, 100)), spacing: 2);
            var progMask = Img(shake, "TimeProgressMask", sPill, new Color(1, 1, 1, 0.12f), At(Top, Center, new Vector2(0, -164), new Vector2(260, 10)));
            progMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var prog = Img(progMask.rectTransform, "Fill", sWhite, Accent, Stretched(), sliced: false);
            prog.type = Image.Type.Filled; prog.fillMethod = Image.FillMethod.Horizontal; prog.fillOrigin = 0; prog.fillAmount = 1f;

            // Score (top right)
            Txt(shake, "ScoreLabel", "SCORE", fHudSemi, 28, TextSecondary, TextAlignmentOptions.Right, At(TopRight, new Vector2(1, 1), new Vector2(-50, -42), new Vector2(360, 36)), spacing: 10, material: mHudShadow);
            var score = Txt(shake, "Score", "0", fHud, 84, TextPrimary, TextAlignmentOptions.Right, At(TopRight, new Vector2(1, 1), new Vector2(-50, -72), new Vector2(420, 96)), material: mHudShadow);

            // Kills (under score)
            var killsGroup = Rect(shake, "Kills", At(TopRight, new Vector2(1, 1), new Vector2(-50, -178), new Vector2(260, 56)));
            Img(killsGroup, "Skull", Icon("Skull"), TextSecondary, At(Right, new Vector2(1, 0.5f), new Vector2(-96, 0), new Vector2(38, 38)), sliced: false);
            var kills = Txt(killsGroup, "Value", "0", fHud, 50, TextPrimary, TextAlignmentOptions.Right, At(Right, new Vector2(1, 0.5f), Vector2.zero, new Vector2(84, 56)), material: mHudShadow);

            // Health (top left under pause)
            var hBlock = Rect(shake, "HealthBlock", At(TopLeft, new Vector2(0, 1), new Vector2(46, -178), new Vector2(560, 64)));
            var heart = Img(hBlock, "Heart", Icon("Heart"), Success, At(Left, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(48, 48)), sliced: false);
            var barMask = Img(hBlock, "BarMask", sPill, new Color(0.02f, 0.04f, 0.06f, 0.7f), At(Left, new Vector2(0, 0.5f), new Vector2(66, 0), new Vector2(380, 30)));
            barMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var lag = Img(barMask.rectTransform, "Lag", sWhite, new Color(1, 1, 1, 0.75f), Stretched(), sliced: false);
            lag.type = Image.Type.Filled; lag.fillMethod = Image.FillMethod.Horizontal; lag.fillAmount = 1f;
            var fill = Img(barMask.rectTransform, "Fill", sWhite, Success, Stretched(), sliced: false);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 1f;
            var hpText = Txt(hBlock, "Value", "100", fHud, 50, TextPrimary, TextAlignmentOptions.Left, At(Left, new Vector2(0, 0.5f), new Vector2(462, 2), new Vector2(110, 60)), material: mHudShadow);

            // Hostiles pill (bottom left)
            var hostile = Img(shake, "HostilesPill", sPill, new Color(0.04f, 0.08f, 0.13f, 0.72f), At(BottomLeft, new Vector2(0, 0), new Vector2(46, 60), new Vector2(300, 96)));
            Img(hostile.rectTransform, "Stroke", sPillOutline, Stroke, Stretched());
            Img(hostile.rectTransform, "Dot", sCircle, Danger, At(Left, Center, new Vector2(50, 0), new Vector2(20, 20)));
            Txt(hostile.rectTransform, "Label", "HOSTILES", fHudSemi, 30, TextSecondary, TextAlignmentOptions.Left, At(Left, new Vector2(0, 0.5f), new Vector2(76, 0), new Vector2(150, 50)), spacing: 6);
            var hostiles = Txt(hostile.rectTransform, "Value", "0", fHud, 52, TextPrimary, TextAlignmentOptions.Right, At(Right, new Vector2(1, 0.5f), new Vector2(-34, 2), new Vector2(80, 70)));

            // Crosshair + hit marker + popup + countdown
            var cross = Img(shake, "Crosshair", sCrosshair, new Color(1, 1, 1, 0.9f), At(Center, Center, Vector2.zero, new Vector2(110, 110)), sliced: false);
            var hit = Img(shake, "HitMarker", sHitMarker, new Color(1, 1, 1, 0), At(Center, Center, Vector2.zero, new Vector2(96, 96)), sliced: false);
            var popup = Txt(shake, "ScorePopup", "+100", fHud, 64, Amber, TextAlignmentOptions.Center, At(Center, Center, new Vector2(0, 120), new Vector2(300, 80)), material: mHudShadow);
            var countdown = Txt(shake, "Countdown", "3", fTitle, 220, TextPrimary, TextAlignmentOptions.Center, At(Center, Center, new Vector2(0, 260), new Vector2(1000, 300)));
            countdown.fontSharedMaterial = mTitleGlow;

            // Fire button (combat controls)
            var controls = Rect(shake, "CombatControls", Stretched());
            var fireRoot = Rect(controls, "FireButton", At(BottomRight, new Vector2(1, 0), new Vector2(-50, 60), new Vector2(250, 250)));
            var fireVisual = Rect(fireRoot, "Visual", Stretched());
            var fireRing = Img(fireVisual, "Ring", sRing, new Color(Accent.r, Accent.g, Accent.b, 0.8f), Stretched(), sliced: false);
            var fireBg = Img(fireVisual, "Bg", sCircle, new Color(Accent.r, Accent.g, Accent.b, 0.22f), At(Center, Center, Vector2.zero, new Vector2(214, 214)), sliced: false, raycast: true);
            Img(fireVisual, "Icon", Icon("Target"), TextPrimary, At(Center, Center, new Vector2(0, 14), new Vector2(96, 96)), sliced: false);
            Txt(fireVisual, "Label", "FIRE", fHud, 34, TextPrimary, TextAlignmentOptions.Center, At(Center, Center, new Vector2(0, -62), new Vector2(200, 40)), spacing: 10);
            var fire = fireRoot.gameObject.AddComponent<FireButton>();
            Set(fire, "visual", fireVisual);
            Set(fire, "ring", fireRing);
            // the whole rect should receive the press
            var fireHit = fireRoot.gameObject.AddComponent<Image>();
            fireHit.color = new Color(0, 0, 0, 0);
            fireHit.sprite = sCircle;

            Txt(controls, "FireHint", "HOLD FIRE  ·  OR TAP ANYWHERE", fHudSemi, 26, new Color(1, 1, 1, 0.55f), TextAlignmentOptions.Center, At(Bottom, Center, new Vector2(0, 360), new Vector2(700, 40)), spacing: 6, material: mHudShadow);

            Set(screen, "healthFill", fill);
            Set(screen, "healthLag", lag);
            Set(screen, "healthText", hpText);
            Set(screen, "healthBlock", hBlock);
            Set(screen, "heartIcon", heart);
            Set(screen, "scoreText", score);
            Set(screen, "timeText", timeText);
            Set(screen, "timePill", pill);
            Set(screen, "timeProgress", prog);
            Set(screen, "killsText", kills);
            Set(screen, "hostilesText", hostiles);
            Set(screen, "crosshair", cross.rectTransform);
            Set(screen, "hitMarker", hit);
            Set(screen, "damageVignette", damageVignette);
            Set(screen, "lowHealthVignette", lowHpVignette);
            Set(screen, "scorePopup", popup);
            Set(screen, "countdownText", countdown);
            Set(screen, "shakeRoot", shake);
            Set(screen, "combatControls", controls.gameObject);
            Set(screen, "pauseButton", pauseBtn);
            return screen;
        }

        private static PauseScreen BuildPause(Transform parent)
        {
            var screen = CreateScreen<PauseScreen>(parent, "PauseScreen", out RectTransform content);
            var card = Img(content, "Card", sRound, Glass, At(Center, Center, new Vector2(0, 0), new Vector2(860, 990)));
            Img(card.rectTransform, "Outline", sRoundOutline, Stroke, Stretched());
            Img(card.rectTransform, "Icon", Icon("Pause"), Accent, At(Top, Center, new Vector2(0, -110), new Vector2(84, 84)), sliced: false);
            Txt(card.rectTransform, "Title", "PAUSED", fTitle, 88, TextPrimary, TextAlignmentOptions.Center, At(Top, Center, new Vector2(0, -225), new Vector2(760, 110)), spacing: 6);
            Txt(card.rectTransform, "Subtitle", "The horde is waiting.", fBody, 32, TextSecondary, TextAlignmentOptions.Center, At(Top, Center, new Vector2(0, -305), new Vector2(760, 50)));
            var resume = MakeButton(card.rectTransform, "ResumeButton", "RESUME", "Play", ButtonStyle.Primary, At(Bottom, Center, new Vector2(0, 505), new Vector2(700, 140)), 52);
            var audio = MakeButton(card.rectTransform, "SettingsButton", "AUDIO SETTINGS", "Settings", ButtonStyle.Secondary, At(Bottom, Center, new Vector2(0, 340), new Vector2(700, 120)), 40);
            var restart = MakeButton(card.rectTransform, "RestartButton", "RESTART", "Restart", ButtonStyle.Secondary, At(Bottom, Center, new Vector2(-178, 175), new Vector2(344, 120)), 40);
            var menu = MakeButton(card.rectTransform, "MenuButton", "MENU", "Home", ButtonStyle.Secondary, At(Bottom, Center, new Vector2(178, 175), new Vector2(344, 120)), 40);
            Set(screen, "settingsButton", audio);
            Set(screen, "resumeButton", resume);
            Set(screen, "restartButton", restart);
            Set(screen, "menuButton", menu);
            return screen;
        }

        private static GameOverScreen BuildGameOver(Transform parent)
        {
            var screen = CreateScreen<GameOverScreen>(parent, "GameOverScreen", out RectTransform content);
            var items = new List<RectTransform>();

            var glow = Img(content, "ResultGlow", Sprite("UI_Circle"), new Color(Success.r, Success.g, Success.b, 0.25f), At(Top, Center, new Vector2(0, -260), new Vector2(300, 300)), sliced: false);
            glow.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/UI/UI_Ring.png");
            var icon = Img(content, "ResultIcon", Icon("Trophy"), Success, At(Top, Center, new Vector2(0, -260), new Vector2(170, 170)), sliced: false);
            var title = Txt(content, "Title", "YOU SURVIVED", fTitle, 92, Success, TextAlignmentOptions.Center, At(Top, Center, new Vector2(0, -470), new Vector2(1040, 120)), spacing: 4);
            var subtitle = Txt(content, "Subtitle", "NORMAL MISSION COMPLETE", fHudSemi, 36, TextSecondary, TextAlignmentOptions.Center, At(Top, Center, new Vector2(0, -555), new Vector2(1000, 50)), spacing: 10);

            var scoreGroup = Rect(content, "ScoreGroup", At(Top, Center, new Vector2(0, -760), new Vector2(1000, 300)));
            Txt(scoreGroup, "Label", "FINAL SCORE", fHudSemi, 34, TextSecondary, TextAlignmentOptions.Center, At(Top, Center, new Vector2(0, -30), new Vector2(800, 50)), spacing: 12);
            var score = Txt(scoreGroup, "Value", "0", fHud, 190, TextPrimary, TextAlignmentOptions.Center, At(Center, Center, new Vector2(0, -10), new Vector2(1000, 190)), material: mHudShadow);
            var bonus = Txt(scoreGroup, "Bonus", "", fBody, 26, Accent, TextAlignmentOptions.Center, At(Bottom, Center, new Vector2(0, 16), new Vector2(900, 40)));
            items.Add(scoreGroup);

            var badge = Img(content, "NewBest", sPill, Amber, At(Top, Center, new Vector2(330, -655), new Vector2(230, 64)));
            Txt(badge.rectTransform, "Label", "NEW BEST!", fHud, 34, TextOnAccent, TextAlignmentOptions.Center, Stretched(), spacing: 6);
            badge.rectTransform.localRotation = Quaternion.Euler(0, 0, -8f);

            var stats = Rect(content, "Stats", At(Top, Center, new Vector2(0, -1060), new Vector2(960, 250)));
            var kills = StatTile(stats, "KillsTile", "Skull", "ENEMIES DEFEATED", new Vector2(-244, 0), Danger, out TMP_Text killsSub);
            var time = StatTile(stats, "TimeTile", "Clock", "TIME SURVIVED", new Vector2(244, 0), Accent, out TMP_Text timeSub);
            timeSub.gameObject.SetActive(false);
            items.Add(stats);

            var restart = MakeButton(content, "RestartButton", "PLAY AGAIN", "Restart", ButtonStyle.Primary, At(Bottom, Center, new Vector2(0, 330), new Vector2(880, 150)), 54);
            var menu = MakeButton(content, "MenuButton", "MAIN MENU", "Home", ButtonStyle.Secondary, At(Bottom, Center, new Vector2(-80, 165), new Vector2(720, 124)), 42);
            var lb = MakeIconButton(content, "LeaderboardButton", "Leaderboard", At(Bottom, Center, new Vector2(376, 165), new Vector2(124, 124)));
            items.Add(restart.GetComponent<RectTransform>());
            items.Add(menu.GetComponent<RectTransform>());
            items.Add(lb.GetComponent<RectTransform>());
            foreach (var rt in items) if (rt.GetComponent<CanvasGroup>() == null) rt.gameObject.AddComponent<CanvasGroup>();

            Set(screen, "titleText", title);
            Set(screen, "subtitleText", subtitle);
            Set(screen, "scoreText", score);
            Set(screen, "killsText", kills);
            Set(screen, "killsBreakdownText", killsSub);
            Set(screen, "timeText", time);
            Set(screen, "bonusText", bonus);
            Set(screen, "resultIcon", icon);
            Set(screen, "resultGlow", glow);
            Set(screen, "survivedSprite", Icon("Trophy"));
            Set(screen, "defeatedSprite", Icon("Skull"));
            Set(screen, "newBestBadge", badge.rectTransform);
            SetArray(screen, "staggerItems", items.ToArray());
            Set(screen, "restartButton", restart);
            Set(screen, "menuButton", menu);
            Set(screen, "leaderboardButton", lb);
            return screen;
        }

        private static TMP_Text StatTile(RectTransform parent, string name, string icon, string label, Vector2 pos, Color color, out TMP_Text sub)
        {
            var tile = Img(parent, name, sRound, Glass, At(Center, Center, pos, new Vector2(466, 250)));
            Img(tile.rectTransform, "Outline", sRoundOutline, Stroke, Stretched());
            Img(tile.rectTransform, "Icon", Icon(icon), color, At(Top, Center, new Vector2(0, -48), new Vector2(50, 50)), sliced: false);
            var value = Txt(tile.rectTransform, "Value", "0", fHud, 88, TextPrimary, TextAlignmentOptions.Center, At(Center, Center, new Vector2(0, -8), new Vector2(440, 100)));
            Txt(tile.rectTransform, "Label", label, fHudSemi, 28, TextSecondary, TextAlignmentOptions.Center, At(Bottom, Center, new Vector2(0, 58), new Vector2(440, 40)), spacing: 8);
            sub = Txt(tile.rectTransform, "Sub", "", fBody, 21, new Color(1, 1, 1, 0.45f), TextAlignmentOptions.Center, At(Bottom, Center, new Vector2(0, 24), new Vector2(440, 32)));
            return value;
        }

        private static LeaderboardScreen BuildLeaderboard(Transform root)
        {
            var screen = CreateScreen<LeaderboardScreen>(root, "LeaderboardScreen", out RectTransform content);
            // Own opaque backdrop so it can overlay any screen
            var bd = Img(screen.transform as RectTransform, "Backdrop", sWhite, new Color(0.012f, 0.03f, 0.05f, 1f), Stretched(), raycast: true);
            bd.transform.SetAsFirstSibling();
            content.gameObject.AddComponent<SafeArea>();

            Img(content, "Icon", Icon("Trophy"), Amber, At(Top, Center, new Vector2(0, -130), new Vector2(92, 92)), sliced: false);
            Txt(content, "Title", "RECENT MISSIONS", fTitle, 70, TextPrimary, TextAlignmentOptions.Center, At(Top, Center, new Vector2(0, -250), new Vector2(1040, 90)), spacing: 4);
            Txt(content, "Subtitle", "Your latest 5 sessions · saved on this device", fBody, 30, TextSecondary, TextAlignmentOptions.Center, At(Top, Center, new Vector2(0, -320), new Vector2(1000, 44)));
            var best = Txt(content, "Best", "ALL-TIME BEST", fHud, 38, TextPrimary, TextAlignmentOptions.Center, At(Top, Center, new Vector2(0, -385), new Vector2(1000, 50)), spacing: 8);

            // Header
            var header = Rect(content, "Header", At(Top, Center, new Vector2(0, -470), new Vector2(980, 40)));
            HeaderCell(header, "#", 20, 70, TextAlignmentOptions.Left);
            HeaderCell(header, "DATE", 100, 260, TextAlignmentOptions.Left);
            HeaderCell(header, "MODE", 370, 150, TextAlignmentOptions.Center);
            HeaderCell(header, "KILLS", 530, 110, TextAlignmentOptions.Center);
            HeaderCell(header, "TIME", 650, 130, TextAlignmentOptions.Center);
            HeaderCell(header, "SCORE", 790, 170, TextAlignmentOptions.Right);

            var rows = new LeaderboardRow[5];
            for (int i = 0; i < 5; i++)
                rows[i] = Row(content, i, new Vector2(0, -575 - i * 172));

            var empty = Rect(content, "EmptyState", At(Top, Center, new Vector2(0, -820), new Vector2(900, 300)));
            Img(empty, "Icon", Icon("Target"), new Color(1, 1, 1, 0.15f), At(Top, Center, new Vector2(0, -40), new Vector2(120, 120)), sliced: false);
            Txt(empty, "Text", "No missions yet.\nComplete a round to record your first score.", fBody, 32, TextSecondary, TextAlignmentOptions.Center, At(Bottom, Center, new Vector2(0, 60), new Vector2(900, 110)), wrap: true);

            var close = MakeButton(content, "CloseButton", "CLOSE", "Close", ButtonStyle.Secondary, At(Bottom, Center, new Vector2(0, 200), new Vector2(760, 130)), 44);
            var clear = MakeButton(content, "ClearButton", "CLEAR HISTORY", null, ButtonStyle.Ghost, At(Bottom, Center, new Vector2(0, 75), new Vector2(420, 80)), 28);

            SetArray(screen, "rows", rows);
            Set(screen, "emptyState", empty.gameObject);
            Set(screen, "bestText", best);
            Set(screen, "closeButton", close);
            Set(screen, "clearButton", clear);
            return screen;
        }

        private static SettingsScreen BuildSettings(Transform root)
        {
            var screen = CreateScreen<SettingsScreen>(root, "SettingsScreen", out RectTransform content);
            var bd = Img(screen.transform as RectTransform, "Backdrop", sWhite, new Color(0.012f, 0.03f, 0.05f, 1f), Stretched(), raycast: true);
            bd.transform.SetAsFirstSibling();
            content.gameObject.AddComponent<SafeArea>();

            Img(content, "Icon", Icon("Volume"), Accent, At(Top, Center, new Vector2(0, -130), new Vector2(92, 92)), sliced: false);
            Txt(content, "Title", "AUDIO SETTINGS", fTitle, 66, TextPrimary, TextAlignmentOptions.Center, At(Top, Center, new Vector2(0, -250), new Vector2(1040, 90)), spacing: 4);
            Txt(content, "Subtitle", "Changes are saved automatically", fBody, 30, TextSecondary, TextAlignmentOptions.Center, At(Top, Center, new Vector2(0, -318), new Vector2(1000, 44)));

            var group = Rect(content, "Sliders", Stretched());
            var groupCg = group.gameObject.AddComponent<CanvasGroup>();

            var defs = new (SurvivalShooter.Audio.AudioChannel ch, string icon, string name, string desc)[]
            {
                (SurvivalShooter.Audio.AudioChannel.Master, "Volume", "MASTER VOLUME", "Overall loudness of the whole game"),
                (SurvivalShooter.Audio.AudioChannel.Music, "Music", "MUSIC", "Menu & combat background soundtrack"),
                (SurvivalShooter.Audio.AudioChannel.SoundEffects, "Effects", "SOUND EFFECTS", "Your weapon, hits, countdown & alerts"),
                (SurvivalShooter.Audio.AudioChannel.Enemy, "Skull", "ENEMY AUDIO", "Zombie growls & bites, soldier gunfire"),
                (SurvivalShooter.Audio.AudioChannel.Interface, "Interface", "INTERFACE", "Button taps & menu sounds"),
            };

            var so = new SerializedObject(screen);
            var rowsProp = so.FindProperty("rows");
            rowsProp.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                var row = Img(group, "Row_" + d.ch, sRound, new Color(1, 1, 1, 0.05f), At(Top, Center, new Vector2(0, -480 - i * 200), new Vector2(980, 186)));
                var rrt = row.rectTransform;
                var icon = Img(rrt, "Icon", Icon(d.icon), Accent, At(TopLeft, Center, new Vector2(62, -52), new Vector2(54, 54)), sliced: false);
                icon.preserveAspect = true;
                Txt(rrt, "Name", d.name, fHud, 44, TextPrimary, TextAlignmentOptions.Left, At(TopLeft, new Vector2(0, 0.5f), new Vector2(112, -40), new Vector2(620, 52)), spacing: 6);
                Txt(rrt, "Description", d.desc, fBody, 24, TextSecondary, TextAlignmentOptions.Left, At(TopLeft, new Vector2(0, 0.5f), new Vector2(112, -80), new Vector2(660, 34)));
                var value = Txt(rrt, "Value", "100%", fHud, 50, Accent, TextAlignmentOptions.Right, At(TopRight, new Vector2(1, 0.5f), new Vector2(-36, -52), new Vector2(200, 60)));
                var slider = MakeSlider(rrt, At(Bottom, Center, new Vector2(0, 44), new Vector2(900, 56)), out Image fill);

                var e = rowsProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("channel").enumValueIndex = (int)d.ch;
                e.FindPropertyRelative("slider").objectReferenceValue = slider;
                e.FindPropertyRelative("valueText").objectReferenceValue = value;
                e.FindPropertyRelative("fill").objectReferenceValue = fill;
                e.FindPropertyRelative("icon").objectReferenceValue = icon;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var mute = MakeButton(content, "MuteButton", "MUTE ALL", "Mute", ButtonStyle.Secondary, At(Bottom, Center, new Vector2(-222, 330), new Vector2(424, 120)), 38);
            var reset = MakeButton(content, "ResetButton", "RESET", "Restart", ButtonStyle.Secondary, At(Bottom, Center, new Vector2(222, 330), new Vector2(424, 120)), 38);
            var done = MakeButton(content, "DoneButton", "DONE", null, ButtonStyle.Primary, At(Bottom, Center, new Vector2(0, 165), new Vector2(760, 140)), 52);

            Set(screen, "doneButton", done);
            Set(screen, "resetButton", reset);
            Set(screen, "muteButton", mute);
            Set(screen, "muteLabel", mute.transform.Find("Content/Label").GetComponent<TMP_Text>());
            Set(screen, "muteIcon", mute.transform.Find("Content/Icon").GetComponent<Image>());
            Set(screen, "soundOnSprite", Icon("Mute"));
            Set(screen, "soundOffSprite", Icon("Volume"));
            Set(screen, "slidersGroup", groupCg);
            return screen;
        }

        private static void MakeOverlayCanvas(GameObject go, int order)
        {
            var c = go.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = order;
            go.AddComponent<GraphicRaycaster>();
        }

        private static Slider MakeSlider(RectTransform parent, Layout layout, out Image fill)
        {
            var root = Rect(parent, "Slider", layout);
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            hit.raycastTarget = true;

            var track = Img(root, "Track", sPill, new Color(1, 1, 1, 0.12f), Stretched());
            track.rectTransform.anchorMin = new Vector2(0, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(0, 14);

            var fillArea = Rect(root, "Fill Area", Stretched());
            fillArea.anchorMin = new Vector2(0, 0.5f);
            fillArea.anchorMax = new Vector2(1, 0.5f);
            fillArea.sizeDelta = new Vector2(0, 14);
            fill = Img(fillArea, "Fill", sPill, Accent, Stretched());

            var handleArea = Rect(root, "Handle Slide Area", Stretched());
            handleArea.offsetMin = new Vector2(22, 0);
            handleArea.offsetMax = new Vector2(-22, 0);
            var handle = Img(handleArea, "Handle", sCircle, Color.white, At(Center, Center, Vector2.zero, new Vector2(52, 52)), sliced: false, raycast: true);
            handle.rectTransform.sizeDelta = new Vector2(52, 0);
            handle.rectTransform.anchorMin = new Vector2(0, 0);
            handle.rectTransform.anchorMax = new Vector2(0, 1);
            Img(handle.rectTransform, "Glow", sRing, new Color(Accent.r, Accent.g, Accent.b, 0.6f), At(Center, Center, Vector2.zero, new Vector2(78, 78)), sliced: false);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.None;
            var nav = slider.navigation; nav.mode = Navigation.Mode.None; slider.navigation = nav;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
            return slider;
        }

        private static void HeaderCell(RectTransform parent, string text, float x, float w, TextAlignmentOptions align)
        {
            Txt(parent, "H_" + text, text, fHudSemi, 26, TextSecondary, align, At(Left, new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(w, 40)), spacing: 6);
        }

        private static LeaderboardRow Row(RectTransform parent, int index, Vector2 pos)
        {
            var bg = Img(parent, $"Row{index + 1}", sRound, new Color(1, 1, 1, 0.05f), At(Top, Center, pos, new Vector2(980, 150)));
            var rt = bg.rectTransform;
            bg.gameObject.AddComponent<CanvasGroup>();
            var idx = Txt(rt, "Index", "01", fHud, 46, TextSecondary, TextAlignmentOptions.Left, At(Left, new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(70, 60)));
            var date = Txt(rt, "Date", "01 JAN · 12:00", fHudSemi, 32, TextPrimary, TextAlignmentOptions.Left, At(Left, new Vector2(0, 0.5f), new Vector2(100, 20), new Vector2(270, 44)));
            var result = Txt(rt, "Result", "SURVIVED", fHud, 26, Success, TextAlignmentOptions.Left, At(Left, new Vector2(0, 0.5f), new Vector2(100, -22), new Vector2(270, 34)), spacing: 6);
            var chip = Img(rt, "Chip", sPill, new Color(Accent.r, Accent.g, Accent.b, 0.22f), At(Left, new Vector2(0, 0.5f), new Vector2(378, 0), new Vector2(134, 50)));
            var diff = Txt(chip.rectTransform, "Mode", "NORMAL", fHud, 26, TextPrimary, TextAlignmentOptions.Center, Stretched(), spacing: 4);
            var kills = Txt(rt, "Kills", "0", fHud, 44, TextPrimary, TextAlignmentOptions.Center, At(Left, new Vector2(0, 0.5f), new Vector2(530, 0), new Vector2(110, 60)));
            var time = Txt(rt, "Time", "00:00", fHud, 40, TextPrimary, TextAlignmentOptions.Center, At(Left, new Vector2(0, 0.5f), new Vector2(650, 0), new Vector2(130, 60)));
            var score = Txt(rt, "Score", "0", fHud, 52, TextPrimary, TextAlignmentOptions.Right, At(Right, new Vector2(1, 0.5f), new Vector2(-26, 0), new Vector2(190, 64)));
            var latest = Img(rt, "LatestTag", sPill, Accent, At(TopRight, new Vector2(1, 1), new Vector2(-20, 18), new Vector2(120, 36)));
            Txt(latest.rectTransform, "Label", "LATEST", fHud, 22, TextOnAccent, TextAlignmentOptions.Center, Stretched(), spacing: 4);

            var row = bg.gameObject.AddComponent<LeaderboardRow>();
            Set(row, "indexText", idx);
            Set(row, "dateText", date);
            Set(row, "difficultyText", diff);
            Set(row, "difficultyChip", chip);
            Set(row, "resultText", result);
            Set(row, "killsText", kills);
            Set(row, "timeText", time);
            Set(row, "scoreText", score);
            Set(row, "background", bg);
            Set(row, "latestTag", latest.gameObject);
            return row;
        }

        // ================================================================= UI kit

        private enum ButtonStyle { Primary, Secondary, Ghost }

        private static Button MakeButton(RectTransform parent, string name, string label, string icon, ButtonStyle style, Layout layout, float fontSize)
        {
            Color fill = style == ButtonStyle.Primary ? Accent : (style == ButtonStyle.Secondary ? new Color(1, 1, 1, 0.08f) : new Color(0, 0, 0, 0));
            Color text = style == ButtonStyle.Primary ? TextOnAccent : (style == ButtonStyle.Ghost ? TextSecondary : TextPrimary);
            var bg = Img(parent, name, sPill, fill, layout, raycast: true);
            if (style == ButtonStyle.Secondary) Img(bg.rectTransform, "Stroke", sPillOutline, new Color(1, 1, 1, 0.22f), Stretched());

            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
            bg.gameObject.AddComponent<UIButtonFX>();

            // Content row: icon + label centred together
            var row = Rect(bg.rectTransform, "Content", Stretched());
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.spacing = fontSize * 0.45f;
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
            if (!string.IsNullOrEmpty(icon))
            {
                var ic = Img(row, "Icon", Icon(icon), text, At(Center, Center, Vector2.zero, Vector2.one * fontSize * 0.8f), sliced: false);
                ic.preserveAspect = true;
                var le = ic.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = fontSize * 0.78f;
            }
            var t = Txt(row, "Label", label, fHud, fontSize, text, TextAlignmentOptions.Center, At(Center, Center, Vector2.zero, new Vector2(600, fontSize * 1.3f)), spacing: 8);
            var tle = t.gameObject.AddComponent<LayoutElement>();
            tle.preferredHeight = fontSize * 1.2f;
            return btn;
        }

        private static Button MakeIconButton(RectTransform parent, string name, string icon, Layout layout)
        {
            var bg = Img(parent, name, sCircle, new Color(0.04f, 0.08f, 0.13f, 0.75f), layout, sliced: false, raycast: true);
            Img(bg.rectTransform, "Stroke", sRing, new Color(1, 1, 1, 0.25f), Stretched(), sliced: false);
            Img(bg.rectTransform, "Icon", Icon(icon), TextPrimary, At(Center, Center, Vector2.zero, layout.size * 0.42f), sliced: false);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
            bg.gameObject.AddComponent<UIButtonFX>();
            return btn;
        }

        private static T CreateScreen<T>(Transform parent, string name, out RectTransform content) where T : UIScreen
        {
            var rt = Stretch(parent, name);
            rt.gameObject.AddComponent<CanvasGroup>();
            var screen = rt.gameObject.AddComponent<T>();
            content = Stretch(rt, "Content");
            Set(screen, "content", content);
            return screen;
        }

        // ---- layout primitives

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);
        private static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        private static readonly Vector2 Left = new Vector2(0f, 0.5f);
        private static readonly Vector2 Right = new Vector2(1f, 0.5f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        private struct Layout
        {
            public bool stretch;
            public Vector2 anchor, pivot, pos, size;
        }

        private static Layout At(Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size) =>
            new Layout { anchor = anchor, pivot = pivot, pos = pos, size = size };

        private static Layout Stretched() => new Layout { stretch = true };

        private static RectTransform Rect(Transform parent, string name, Layout l)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Apply(rt, l);
            return rt;
        }

        private static RectTransform Stretch(Transform parent, string name) => Rect(parent, name, Stretched());

        private static void Apply(RectTransform rt, Layout l)
        {
            if (l.stretch)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.pivot = Center;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                return;
            }
            rt.anchorMin = rt.anchorMax = l.anchor;
            rt.pivot = l.pivot;
            rt.sizeDelta = l.size;
            rt.anchoredPosition = l.pos;
        }

        private static Image Img(Transform parent, string name, Sprite sprite, Color color, Layout l, bool sliced = true, bool raycast = false)
        {
            var rt = Rect(parent, name, l);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sliced && sprite.border.sqrMagnitude > 0f)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
            }
            return img;
        }

        private static TextMeshProUGUI Txt(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align, Layout l, float spacing = 0f, bool wrap = false, Material material = null)
        {
            var rt = Rect(parent, name, l);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            if (material != null) t.fontSharedMaterial = material;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.characterSpacing = spacing;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Kaeng_Khiao_Wan_Kai
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // --- App State (Main Menu / Loading / Intro Video / Playing) ---
        private enum AppState { MainMenu, Loading, IntroVideo, Playing }
        private AppState _appState = AppState.MainMenu;

        // --- Loading Screen (คั่นระหว่างกด Start กับเริ่มเล่นวิดีโอ intro) ---
        private const float LoadingScreenMinDuration = 1.2f;
        private float _loadingTimer;

        // --- Intro Video (เล่นหลัง Loading ก่อนเข้าเกมจริง แปลงจากคลิป คร_ปแรก.mov เป็นชุดภาพเฟรม 1280x720 @ 24fps) ---
        private const string IntroVideoFramesFolder = "IntroVideo/frames";
        private const string IntroVideoAudioPath = "IntroVideo/intro_audio.wav";
        private const string IntroVideoFramePrefix = "frame_";
        private const string IntroVideoFrameExtension = ".jpg";
        private const int IntroVideoFrameDigits = 4;
        private const float IntroVideoFps = 24f;
        private int _introVideoFrameCount;
        private Texture2D _introVideoCurrentTexture;
        private int _introVideoCurrentFrameIndex = -1;
        private float _introVideoElapsed;
        private SoundEffect _introVideoAudioEffect;
        private SoundEffectInstance _introVideoAudioInstance;

        // --- Intro Video Skip (กด Space เพื่อข้าม) ---
        private const Keys IntroVideoSkipKey = Keys.Space;
        private const float IntroVideoSkipDarkenDuration = 0.5f; // เวลาที่จอค่อยๆ มืดลง (เสียงก็เฟดออกพร้อมกัน)
        private const float IntroVideoSkipHoldDuration = 0.4f; // ค้างจอมืดสนิทไว้ก่อนเข้าเกม
        private const float IntroVideoHintFullDuration = 2.5f; // ข้อความ "กด Space เพื่อข้าม" แสดงเต็มกี่วินาที
        private const float IntroVideoHintFadeDuration = 1f; // จากนั้นค่อยๆ จางหายไปกี่วินาที
        private KeyboardState _previousIntroVideoKeyboardState;
        private bool _isSkippingIntroVideo;
        private float _introVideoSkipTimer;
        private float _introVideoSkipOverlayAlpha;
        private float _introVideoAudioFadeStartVolume;

        // --- Game Sounds (ไฟล์ .wav ในโฟลเดอร์ Sounds ข้างไฟล์ .exe เหมือนที่ Intro Video ทำ) ---
        // ถ้าไม่พบไฟล์/โหลดไม่สำเร็จ เกมจะเล่นต่อได้ตามปกติ แค่ไม่มีเสียงนั้น
        private const string SoundsFolder = "Sounds";
        private const string FootstepSoundFile = "s_walk_run.wav";
        private const string PickupSoundFile = "s_pick_up_item.wav";
        private const string JumpSoundFile = "s_jump.wav";
        private const string ItemAuraSoundFile = "s_aura_item.wav";
        private const string BookOpenSoundFile = "s_book_open.wav";    // เสียงเปิดสมุด (ใส่ไว้ในโฟลเดอร์ Sounds)
        private const float BookOpenSoundVolume = 0.8f;

        // เสียงเดิน/วิ่ง: ไฟล์ลูปก้าวเท้า ~0.48 วิ/ก้าว ตอนวิ่งเร่งด้วย pitch (หน่วย octave, ความเร็ว = 2^pitch)
        // 0.26 ≈ เร็วขึ้น 1.2 เท่า ≈ 0.4 วิ/ก้าว ปรับให้ตรงกับจังหวะอนิเมชันวิ่งได้
        private const float FootstepWalkPitch = 0f;
        private const float FootstepRunPitch = 0.26f;
        private const float FootstepVolume = 0.7f;
        private const float FootstepFadeOutDuration = 0.4f; // หยุดเดิน -> เสียงค่อยๆ จางหายในเวลานี้ (วินาที)
        private const float FootstepFadeInDuration = 0.1f;  // กดเดินซ้ำระหว่างกำลังจาง -> ดังกลับเต็มในเวลานี้

        private const float PickupSoundVolume = 0.7f; // ใช้ทั้งตอนเก็บและตอนดรอปไอเทม
        private const float JumpSoundVolume = 0.5f;

        // เสียงออร่าไอเทมบนพื้น: ยิ่งใกล้ยิ่งดัง เริ่มได้ยินในระยะ ItemAuraRange ใช้ไอเทมที่ใกล้ที่สุดชิ้นเดียว
        private const float ItemAuraRange = 500f;
        private const float ItemAuraMaxVolume = 1f;
        private const float ItemAuraVolumeLerpSpeed = 8f; // ยิ่งมากยิ่งเปลี่ยนความดังเร็ว (กันเสียงสะดุ้งตอนเก็บของ)

        private SoundEffect _footstepSoundEffect;
        private SoundEffectInstance _footstepInstance;
        private SoundEffect _pickupSoundEffect;
        private SoundEffect _jumpSoundEffect;
        private SoundEffect _itemAuraSoundEffect;
        private SoundEffectInstance _itemAuraInstance;
        private float _itemAuraCurrentVolume;
        private float _footstepFade; // 0 = เงียบ, 1 = ดังเต็ม (คูณกับ FootstepVolume)

        // --- Main Menu ---
        private Texture2D _mainMenuBackground;
        //private Texture2D _menuLogoTexture; // เอา melanie ออกก่อน
        private Texture2D _btnStartTexture;
        private Texture2D _btnSettingTexture;
        private Texture2D _btnExitTexture;
        private Effect _mainMenuEffect;
        private float _menuEffectTime;

        // --- Setting / เมนู Esc ---
        private Texture2D _btnMenuTexture;                 // ปุ่ม Back (Menu) (bt_menu)
        private Texture2D _hotbarHudTexture;               // ฮอตบาร์มุมล่างซ้าย (hot_bar)
        private bool _isSettingsOpen;                      // หน้า Setting เปิดอยู่ (ใช้ร่วมกันทั้งหน้าหลักและเมนู Esc)
        private bool _isPauseMenuOpen;                     // เมนู Esc ตอนอยู่ในเกม (โลกเกมหยุดนิ่งทั้งหมด)
        private int _settingsDraggingSlider = -1;          // 0 = Master, 1 = SFX, 2 = Music, -1 = ไม่ได้ลากอยู่
        private float _masterVolume = 1f;
        private float _sfxVolume = 1f;
        private float _musicVolume = 1f;
        private KeyboardState _previousMenuKeyboardState;
        private const int MenuLeftMargin = 90;
        private const int MenuTopY = 160;
        private const int MenuButtonGap = 18;
        private const float MenuButtonScale = 0.75f;

        // --- Main Menu Background Shader (มืดลง / คลื่นเบาๆ / ไฟกระพริบเฉพาะจุด) ---
        private const float MenuBackgroundDarkenAmount = 0.18f;
        private const float MenuWaveAmplitude = 0.003f;
        private const float MenuWaveSpeed = 0.6f;
        private static readonly Vector2 MenuFlickerCenter = new Vector2(0.677f, 0.213f); // บริเวณกระจกหมวกนักดำน้ำ
        private const float MenuFlickerRadius = 0.13f;
        private const float MenuFlickerSpeed = 1.6f;
        private const float MenuFlickerIntensity = 0.18f;

        // --- Ground / Floor ---
        private Texture2D _groundTexture;
        private Texture2D _groundTextureNextRoom;   // พื้นห้องถัดไปหลังบอส (ground_t_02) ถ้าโหลดไม่ได้จะใช้ _groundTexture แทน
        private int _groundTileWidthNextRoom;
        private Texture2D _groundTextureLevel3;     // พื้นด่าน 3 (Platform_Lvl3) ถ้าโหลดไม่ได้จะใช้พื้นด่านอื่นแทน
        private int _groundTileWidthLevel3;
        private Rectangle _groundSourceRect;
        private int _floorHeight;
        private int _floorY;
        private int _groundTileWidth;

        // --- Character: Kai ---
        private struct AnimFrame
        {
            public Texture2D Texture;
            public Rectangle Trim;
            public float OriginX;
            public float ClampHalfWidth;
        }

        private const int IdleFrameCount = 8;
        private const int WalkFrameCount = 12;
        private const int JumpFrameCount = 8;
        private const int JumpSheetColumns = 4;
        private const int JumpSheetRows = 2;
        private AnimFrame[] _kaiIdle;
        private AnimFrame[] _kaiRun;
        private AnimFrame[] _kaiJump;
        private float _kaiScale;
        private int _currentFrame;
        private float _frameTimer;
        private const float FrameDuration = 0.1f;
        private const float KaiHeightToFloorRatio = 1.75f;
        private const float KaiSpeed = 300f;
        private float _kaiCenterX;
        private float _kaiJumpOffsetY;
        private bool _isMoving;
        private bool _facingRight = true;

        // --- Jump ---
        private const float JumpVelocity = -800f;
        private const float Gravity = 2000f;
        private const int MaxJumps = 2;
        private const float LandingDuration = 0.15f;
        private float _kaiVelocityY;
        private int _jumpCount;
        private bool _isJumping;
        private bool _isLanding;
        private float _landingTimer;
        private int _jumpFrameIndex;
        private float _jumpAnimTimer;
        private KeyboardState _previousKeyboardState;

        private float _jumpScaleCorrection = 7.5f;

        // --- Camera ---
        private const float CameraZoom = 1.5f;
        private const float WorldWidthMultiplier = 4f;
        private float _worldWidth;
        private float _worldHeight;
        private float _kaiTargetHeight;
        private Matrix _cameraTransform;

        // --- Vignette ---
        private Effect _vignetteEffect;
        private RenderTarget2D _sceneRenderTarget;
        private const float VignetteRadius = 0.75f;
        private const float VignetteSoftness = 0.45f;
        private const float VignetteIntensity = 0.8f;

        // --- Background ---
        private const int BackgroundLayerCount = 7;
        private static readonly string[] BackgroundLayerNames =
        {
            "bg_c_00", "bg_c_01", "bg_c_02",
            "bg_c_03", "bg_c_04", "bg_c_05",
            "parallax_back_cave_06"
        };
        private static readonly float[] BackgroundLayerParallaxFactors = { 0.65f, 0.55f, 0.45f, 0.35f, 0.25f, 0.15f, 0.05f };

        // ความมืดของฉากหลังถ้ำตอนเล่นเกมจริง (0 = สว่างปกติ, 1 = ดำสนิท) — ปรับตัวเลขนี้ได้ตามต้องการ
        private const float GameplayBackgroundDarkenAmount = 0f;
        private static readonly Color GameplayBackgroundTint = new Color(
            (byte)(255 * (1f - GameplayBackgroundDarkenAmount)),
            (byte)(255 * (1f - GameplayBackgroundDarkenAmount)),
            (byte)(255 * (1f - GameplayBackgroundDarkenAmount)),
            (byte)255);

        private Texture2D[] _backgroundLayers;
        private float _backgroundLayerScale;
        private float _backgroundLayerScaledWidth;
        private Matrix[] _backgroundLayerTransforms;

        // --- Background ห้องถัดไปหลังบอส (ห้อง 5): 9 ชั้น bg__00 (หน้าสุด) -> bg__08 (หลังสุด) วาดซ้ำต่อกันแนวนอนเหมือน bg เดิม ---
        // ต้องเพิ่มรูป bg__00.png ถึง bg__08.png เข้า Content Pipeline (Content.mgcb) ถ้าโหลดไม่ครบ เกมจะใช้ bg เดิมในห้องนี้แทน (ไม่ crash)
        private const int NextRoomBackgroundLayerCount = 9;
        private static readonly string[] NextRoomBackgroundLayerNames =
        {
            "LAYER_00", "LAYER_01", "LAYER_02", "LAYER_03", "LAYER_04",
            "LAYER_05", "LAYER_06", "LAYER_07", "bg__08"
        };
        // ความเร็วเลื่อนตามกล้อง: ชั้นหน้า (00) เร็วสุด -> ชั้นหลัง (08) ช้าสุด
        private static readonly float[] NextRoomBackgroundParallaxFactors = { 0.65f, 0.57f, 0.49f, 0.41f, 0.33f, 0.25f, 0.17f, 0.10f, 0.05f };
        // ชั้นที่วาดทับหน้าตัวละคร: ตอนนี้ปิดทั้งหมด (ทุกชั้นอยู่หลังตัวละคร) เพราะชั้นเพดานถ้ำ/เถาวัลย์ (bg__02, 04, 05) บังตัวละครมิด
        // ถ้าอยากให้ชั้นไหนทับหน้าตัวละคร ให้เปลี่ยนค่าของชั้นนั้นเป็น true
        private static readonly bool[] NextRoomBackgroundIsForeground = { false, false, false, false, false, false, false, false, false };
        private Texture2D[] _nextRoomBackgroundLayers;   // null = โหลดไม่ได้ -> ใช้ bg เดิม
        private Matrix[] _nextRoomBackgroundTransforms;
        private float _nextRoomBackgroundScale;
        private float _nextRoomBackgroundScaledWidth;

        // --- Background ด่าน 3: 4 ชั้น LAYER_0_lv3 (หน้าสุด) -> LAYER_3_lv3 (หลังสุด/ท้องฟ้า) วาดซ้ำต่อกันแนวนอน ---
        // ต้องเพิ่มรูปทั้ง 4 เข้า Content Pipeline (Content.mgcb) ถ้าโหลดไม่ครบ เกมจะใช้ bg เดิมในห้องนี้แทน (ไม่ crash)
        private const int Level3BackgroundLayerCount = 4;
        private static readonly string[] Level3BackgroundLayerNames = { "LAYER_0_lv3", "LAYER_1_lv3", "LAYER_2_lv3", "LAYER_3_lv3" };
        private static readonly float[] Level3BackgroundParallaxFactors = { 0.40f, 0.25f, 0.12f, 0.04f };
        // ชั้นที่วาดทับหน้าตัวละคร: ตอนนี้ปิดทั้งหมด ถ้าอยากให้ชั้นหน้าสุด (LAYER_0 กรงเล็บสองข้างจอ) ทับหน้า Kai ให้เปลี่ยนเป็น true
        private static readonly bool[] Level3BackgroundIsForeground = { false, false, false, false };
        private Texture2D[] _level3BackgroundLayers;     // null = โหลดไม่ได้ -> ใช้ bg เดิม
        private Matrix[] _level3BackgroundTransforms;
        private float _level3BackgroundScale;
        private float _level3BackgroundScaledWidth;

        // --- Scene Transition ---
        private const int RoomCount = 3;
        private int _currentRoomIndex;
        private int _transitionDirection;
        private bool _isTransitioning;
        private float _transitionTimer;
        private bool _transitionPlayerResetDone;
        private float _transitionOverlayAlpha;
        private const float TransitionFadeOutDuration = 1.5f;
        private const float TransitionHoldDuration = 0.5f;
        private const float TransitionFadeInDuration = 1.5f;
        private const float TransitionTotalDuration =
            TransitionFadeOutDuration + TransitionHoldDuration + TransitionFadeInDuration;

        // --- Fullscreen toggle ---
        private const int WindowedWidth = 1280;
        private const int WindowedHeight = 720;
        private KeyboardState _previousMiscKeyboardState;

        // --- HUD: HP bar & Stamina bar ---
        private Texture2D _hpBarSheet;
        private Texture2D _staminaBarSheet;
        private static readonly Rectangle HpTextSource = new Rectangle(34, 0, 30, 18);
        private static readonly Rectangle HpBarEmptySource = new Rectangle(29, 57, 95, 15);
        private static readonly Rectangle HpBarFilledSource = new Rectangle(29, 98, 95, 15);
        private static readonly Rectangle HpHeartSource = new Rectangle(0, 131, 36, 33);
        private static readonly Rectangle StaminaBarEmptySource = new Rectangle(0, 0, 76, 12);
        private static readonly Rectangle StaminaBarFilledSource = new Rectangle(0, 16, 76, 12);

        private const int HudMarginX = 16;
        private const int HudMarginY = 16;
        private const int HpHeartOverlapX = 20;
        private const float HeartScale = 1.3f;
        private const int HpTextToBarGap = 4;
        private const int HpToStaminaGap = 0;

        private const float HpBarScale = 1.6f;
        private const float StaminaWidthRatioToHp = 0.9f;
        private const float StaminaBarScaleY = 1.0f;

        private float _currentHP = 100f;
        private float _maxHP = 100f;
        private float _currentStamina = 100f;
        private float _maxStamina = 100f;

        // --- HUD มุมขวาบน: Key (สถานะ), Book (I), Backpack (Tab) ---
        // หมายเหตุ: ต้องเพิ่มรูป key.png / book.png / backpack.png เข้า Content Pipeline
        // ด้วยชื่อ asset ตรงกับที่ Content.Load ด้านล่างเรียก (ui_key / ui_book / ui_backpack)
        private Texture2D _hudKeyTexture;
        private Texture2D _hudBookTexture;
        private Texture2D _hudBackpackTexture;

        private const int TopRightHudMargin = 16;
        private const int TopRightHudIconSize = 64;
        private const int TopRightHudSlotGap = 14;
        private const int TopRightHudLabelGap = 6;
        private const int TopRightHudKeycapHeight = 26;
        private const int TopRightHudKeycapMinWidth = 30;
        private const int TopRightHudKeycapShadowHeight = 3;
        private static readonly Color TopRightHudKeycapBg = new Color(69, 75, 88, 255);
        private static readonly Color TopRightHudKeycapShadow = new Color(38, 42, 51, 255);

        // --- Player Death / Checkpoint Respawn (ไม่มีคืนชีพ/second chance แล้ว ตายแล้วไปเกิดที่เช็คพอยต์ทันที) ---
        private enum PlayerDeathState { Alive, GameOver }
        private PlayerDeathState _deathState = PlayerDeathState.Alive;
        private KeyboardState _previousGameOverKeyboardState;

        // --- God Mode (Debug) ---
        private bool _isGodMode;

        // --- Sprint & Stamina ---
        private const float SprintSpeedMultiplier = 1.7f;
        private const float SprintAnimSpeedMultiplier = 1.5f;
        private const float StaminaSprintDrainPerSecond = 20f;
        private const float StaminaJumpCost = 15f;
        private const float StaminaRegenPerSecond = 15f;
        private const float StaminaRegenDelay = 1f;
        private const float StaminaRecoverThreshold = 0f;
        private bool _isSprinting;
        private bool _isExhausted;
        private float _staminaRegenDelayTimer;

        // --- Command console ---
        private bool _isConsoleOpen;
        private string _consoleText = "";
        private string _consoleMessage = "";
        private Color _consoleMessageColor = Color.White;
        private float _consoleMessageTimer;
        private float _consoleCursorTimer;
        private float _backspaceHoldTimer;
        private bool _escapeHeldFromConsole;
        private KeyboardState _previousConsoleKeyboardState;
        private const int ConsoleMaxLength = 40;
        private const float ConsoleMessageDuration = 3f;
        private const float BackspaceRepeatDelay = 0.4f;
        private const float BackspaceRepeatInterval = 0.05f;
        private const int ConsoleMargin = 16;
        private const int ConsoleBarWidth = 640;
        private const int ConsoleBarHeight = 36;

        // --- Inventory UI & Slot Struct ---
        private enum ItemType { None, Wrench, Seaweed, HpPotion, Key, Orb, JumpPotion, Trident, Sword }

        // --- Stacking rules: อาวุธ/กุญแจสเตกไม่ได้ (max 1), สาหร่าย/ขวดยาสเตกได้สูงสุด 16 ชิ้น/ช่อง ---
        private const int WrenchMaxStack = 1;
        private const int SeaweedMaxStack = 16;
        private const int HpPotionMaxStack = 16;
        private const int JumpPotionMaxStack = 16;
        private const int KeyMaxStack = 1;
        private const int OrbMaxStack = 1;

        // =====================================================================
        // Trident: คลิกขวาค้าง = ง้าง (ขยับเมาส์เพื่อเล็งทิศได้อิสระ) / ปล่อยคลิกขวา = ขว้าง
        //  พุ่งตามทิศที่เล็ง ปักพื้น/ตกพื้นแล้วกลายเป็นไอเทมบนพื้น เก็บกลับด้วย E
        //  Boss 2 กันได้ (กันแล้ว trident เด้งกระเด็น) แล้วต้องรอคูลดาวน์ Boss2TridentBlockCooldown ก่อนจะกันได้อีก
        //  ช่วงคูลดาวน์ trident ปักโดนบอสได้ดาเมจเต็ม
        //  ตอนนี้ trident โดนเฉพาะ Boss 2 (ศัตรูตัวอื่นยังไม่ถูกผูก) ได้มาจาก /giveme_items เท่านั้น
        //  ต้องเพิ่ม trident.png (asset "trident") และ boss_2_parry_attack.png (ท่ากัน ถ้าไม่มีบอสกันได้เหมือนกันแต่ไม่มีท่า) เข้า Content.mgcb
        // =====================================================================
        private const int TridentMaxStack = 1;

        // แกนของภาพ trident.png: ด้ามล่างซ้าย -> ปลายบนขวา ทำมุม -50.1 องศา ยาวเท่าความสูงภาพ (วัดจากไฟล์จริง ย่อภาพได้ไม่ต้องแก้)
        private const float TridentImageAxisAngleDegrees = -50.1f;
        private const float TridentImageAxisLengthToHeight = 1.0f;
        private const float TridentImageCenterXRatio = 0.523f;
        private const float TridentImageCenterYRatio = 0.506f;

        private const float TridentDrawLength = 170f;
        private const float TridentThrowSpeed = 1500f;
        private const float TridentMaxRange = 1100f;               // บินตรงได้ไกลสุดเท่านี้ แล้วตกตามแรงโน้มถ่วง
        private const float TridentGravity = 1800f;
        private const float TridentHitRadius = 16f;
        private const int TridentDamageMin = 60;                   // ดาเมจใส่บอส 2 ตอนบอสกันไม่ได้ (ช่วงคูลดาวน์)
        private const int TridentDamageMax = 80;
        private const float TridentHandDistance = 40f;             // จุดปล่อยห่างจากกลางตัว Kai ตามทิศเล็ง
        private const float TridentAimLineLength = 520f;
        private const float TridentAimDotSpacing = 30f;

        private const float Boss2TridentBlockCooldown = 10f;
        private const float Boss2BlockFrameDuration = 0.07f;

        private sealed class TridentProjectile
        {
            public float X;
            public float Y;
            public float VX;
            public float VY;
            public float Angle;
            public float Spin;
            public float Traveled;
            public bool Falling;      // เกินระยะบินตรง / เด้งออกจากบอส: ตกตามแรงโน้มถ่วง
            public bool Deflected;    // บอสกันแล้ว ไม่โดนบอสซ้ำ
            public int Room;
        }

        private Texture2D _tridentTexture;
        private bool _isAimingTrident;
        private float _tridentAimAngle;
        private readonly List<TridentProjectile> _tridentProjectiles = new List<TridentProjectile>();
        // --- อนิเมชันของ Kai ตอนง้าง/ขว้าง trident (ชีท Char_Attack_Trident_sheet 4x2, 7 เฟรม) ---
        //  กดคลิกขวาค้าง: เล่นเฟรม 1-4 (index 0-3) แล้วค้างที่เฟรม 4 ระหว่างเล็ง
        //  ปล่อยคลิกขวา: เล่นเฟรม 5-7 (index 4-6) แล้วกลับท่าปกติ, trident จริงหลุดจากมือตอนเริ่มเฟรม 5
        //  เฟรม 5-6 ในชีทถูกลบภาพ trident ออกแล้ว (ใช้ตัวที่บินจริงแทน) ต้องใช้ไฟล์ชีทที่ผมส่งให้ ไม่ใช่ต้นฉบับ
        private const int TridentPoseColumns = 4;
        private const int TridentPoseRows = 2;
        private const int TridentPoseFrameCount = 7;
        private const int TridentPoseStandingFrame = 6;       // เฟรมยืนปกติ ใช้เป็นตัวอ้างอิงขนาดตัวและจุดกึ่งกลางลำตัว
        private const int TridentAimLastFrame = 3;
        private const int TridentThrowFirstFrame = 4;
        private const float TridentAimFrameDuration = 0.07f;
        private const float TridentThrowFrameDuration = 0.08f;
        private AnimFrame[] _kaiTridentFrames;
        private float _tridentPoseScale;
        private int _tridentPoseFrame = -1;                   // -1 = ไม่ใช้ท่า trident
        private float _tridentAimAnimTimer;
        private float _tridentThrowAnimTimer;
        private bool _isTridentThrowAnim;
        private AnimFrame[] _boss2Block;   // boss_2_parry_attack: ท่ากัน trident (ไม่มีไฟล์ = บอสกันได้แต่ไม่เล่นท่า)

        /// <summary>
        /// โครงสร้างช่องเก็บของ รองรับการนับจำนวนไอเทม (Stacking)
        /// </summary>
        private struct InventorySlot
        {
            public ItemType Type;
            public int Count;

            public bool IsEmpty => Type == ItemType.None || Count <= 0;

            public void Clear()
            {
                Type = ItemType.None;
                Count = 0;
            }
        }

        private bool _isInventoryOpen;
        private Texture2D _pixelTexture;
        private SpriteFont _hotbarFont;
        private KeyboardState _previousInventoryKeyboardState;

        private const int InventoryGridColumns = 7;
        private const int InventoryGridRows = 3;
        private const int HotbarSlotCount = 8;   // ฮอตบาร์ 8 ช่อง (กดเลข 1-8) ให้ตรงกับรูป hot_bar.png

        private const int InventorySlotSize = 64;
        private const int InventorySlotSpacing = 10;
        private const int InventoryPanelPaddingX = 24;
        private const int InventoryPanelPaddingTopBar = 46;
        private const int InventoryPanelPaddingBottom = 24;
        private const int HotbarGapFromPanel = 16;

        // --- Items / Inventory data ---
        private sealed class WorldItem
        {
            public ItemType Type;
            public int Count = 1; // จำนวนไอเทมที่วางบนพื้น
            public float X;
            public int Room;
            public bool Collected;

            // trident ที่ปักค้างอยู่ (ปักพื้น/ปักตัวบอส): วาดเป็นรูปปักตามมุมแทนไอคอน
            public bool IsStuck;
            public float StuckAngle;
            public float StuckDepth;
            public float StuckTipY;      // Y ของปลาย trident ที่ปักพื้น/แผ่นสูง (X ของปลาย = X ของไอเทม)
            public Boss2 StuckHost;
            public float StuckOffsetX;
            public float StuckOffsetY;
        }

        private Texture2D _wrenchTexture;
        private Texture2D _seaweedTexture;
        private Texture2D _hpPotionTexture;
        private Texture2D _jumpPotionTexture;
        private Texture2D _orbTexture;

        // --- Key (กุญแจ): ไอคอนนิ่งใช้ในกระเป๋า/ฮอตบาร์ ส่วนตอนวางบนพื้นเล่นอนิเมชันตากระพริบ/หมุนวน ---
        private Texture2D _keyItemTexture;
        private AnimFrame[] _keyIdleFrames;
        private const int KeyIdleFrameCount = 8;
        private const int KeyIdleSheetColumns = 4;
        private const int KeyIdleSheetRows = 2;
        private const float KeyIdleFrameDuration = 0.12f;
        private float _keyIdleAnimTimer;

        private readonly InventorySlot[] _inventoryGridSlots = new InventorySlot[InventoryGridColumns * InventoryGridRows];
        private readonly InventorySlot[] _hotbarSlots = new InventorySlot[HotbarSlotCount];
        private readonly List<WorldItem> _worldItems = new List<WorldItem>();
        private int _equippedHotbarIndex = -1;

        // --- เลื่อนลูกกลิ้งเมาส์เพื่อสลับของใน Hotbar ---
        private int _previousScrollWheelValue;
        private int _scrollDeltaThisFrame;   // ผลต่างลูกกลิ้งของเฟรมนี้ (คำนวณต้นทุกเฟรม แม้ตอนเมนู/ฉากล็อกอยู่ จะได้ไม่สะสมค้าง)

        // --- Parry (กด R) ---
        // กด R -> เปิดช่วงแพรี่สั้นๆ ถ้าศัตรูฟาดโดนตัว Kai ระหว่างช่วงนี้ (และ Kai หันหน้าหาศัตรู)
        // จะไม่เสียเลือด และศัตรูตัวนั้นเสียเลือดแทน แพรี่สำเร็จ = ล้าง cooldown ทันที
        private const Keys ParryKey = Keys.R;
        private const float ParryWindowDuration = 0.3f;     // ช่วงที่แพรี่ติดหลังกด R (วินาที)
        private const float ParryCooldownDuration = 0.8f;   // กดพลาด/แพรี่ไม่โดนอะไร ต้องรอเท่านี้ก่อนกดใหม่
        private const int ParryDamageMin = 25;              // ดาเมจที่ศัตรูได้รับเมื่อโดนแพรี่
        private const int ParryDamageMax = 35;
        private const float ParrySuccessFlashDuration = 0.25f;
        private float _parryTimer;
        private float _parryCooldownTimer;
        private float _parrySuccessFlashTimer;

        // --- เตือนเลือดเหลือน้อย: ขอบจอแดงกะพริบเบาๆ เมื่อ HP <= LowHpWarningThreshold (ยิ่งเลือดน้อยยิ่งแดงเข้ม) ---
        private const float LowHpWarningThreshold = 30f;
        private const int LowHpWarningLayers = 36;           // จำนวนชั้นไล่เฉดจากขอบเข้าหาจอกลาง
        private const int LowHpWarningLayerThickness = 6;    // ความหนาชั้นละกี่พิกเซล (ลึกรวม = Layers x Thickness)
        private const float LowHpWarningMaxAlpha = 0.6f;     // ความเข้มสูงสุดที่ขอบจอ (0-1)
        private const float LowHpWarningPulseSpeed = 4f;     // ความเร็วกะพริบ

        // --- หลอดแดงเหนือหัวศัตรูตอนเงื้อโจมตี (ใช้ดูจังหวะแพรี่) ---
        // หลอดค่อยๆ เต็มระหว่างเงื้อ พอเต็มคือจังหวะฟาด ช่วงเงื้อ (ก่อนเฟรมโดน) ถูกยืดให้ช้าลง AttackWindupSlowdown เท่า
        // เพื่อให้เห็นหลอดชัด (1 = ความเร็วเดิม, ยิ่งมากยิ่งช้า/ให้เวลาแพรี่มากขึ้น)
        private const float AttackWindupSlowdown = 1.8f;
        private const int EnemyWindupBarWidth = 70;
        private const int EnemyWindupBarHeight = 8;
        private const float EnemyWindupBarGapAboveHead = 14f;

        // --- โหมดดู Hitbox (กด F4 สลับเปิด/ปิด) ใช้ตรวจว่ากรอบชน/ระยะโจมตีตรงกับตัวสไปรต์ไหม ---
        // ขาว = ขอบสไปรต์จริงของมอน (เฟรมที่กำลังวาด) | แดง = ตัวมอนที่ Kai ตีโดน (hurtbox)
        // เขียว = ตัว Kai (ที่ศัตรูฟาดโดน) | เหลือง = ระยะประแจของ Kai | ส้ม = ระยะฟาดของมอนตอนกำลังโจมตี
        private bool _showHitboxes;

        private const float PickupRange = 90f;
        private const float WorldItemHeight = 44f;
        private const float KeySpawnDropOffsetX = 220f; // ระยะห่างจาก Kai ที่กุญแจจะดรอปมาข้างหน้าตอนเกิด/respawn

        // --- จุดเกิดของผู้เล่น (ห้อง 0) ใช้ทั้งตอนเริ่มเกมและตอนเกิดใหม่หลังตาย ---
        private const float KaiSpawnX = 700f;

        // --- ดรอปไอเทมออกจากกระเป๋า: ลากไอเทมไปปล่อยนอกหน้าต่างกระเป๋า = ดรอปทีละ 1 ชิ้น (ค้าง Ctrl = ดรอปทั้งกอง) ---
        private const float DropItemOffsetX = 70f;        // ระยะที่ของตกห่างจาก Kai ไปข้างหน้า (ตามทิศที่หัน)
        private const float DropItemMergeDistance = 40f;  // ดรอปของชนิดเดียวกัน (สเตกได้) ใกล้กองเดิมในระยะนี้ = รวมเป็นกองเดียว

        // ลาก item ในกระเป๋า
        private bool _isDraggingItem;
        private InventorySlot _dragSlot;
        private bool _dragFromHotbar;
        private int _dragSourceIndex;
        private const int DragIconSize = 56;

        private KeyboardState _previousItemKeyboardState;
        private MouseState _mouseState;
        private MouseState _previousMouseState;

        // --- Seaweed Buff & Eating Effects ---
        private float _seaweedBuffTimer = 0f;
        private const float SeaweedBuffDuration = 30f;
        private const int SeaweedAtkBonus = 10;
        private float _kaiGreenTintTimer = 0f;
        private const float EatEffectDuration = 0.35f;

        // --- HP Potion (มอนสเตอร์ดรอปตอนตาย) ---
        private const float HpPotionHealAmount = 25f;
        private const int HpPotionMinDropCount = 1;
        private const int HpPotionMaxDropCount = 3;

        // --- Jump Potion (น้ำยากระโดด: ดื่มแล้วกระโดดไม่เสีย stamina ตามเวลาบัฟ / ดื่มซ้ำบวกเวลาเพิ่ม / มอนปกติดรอปตามโอกาส บอสดรอปแน่นอน 1 ชิ้น) ---
        // หมายเหตุ: ต้องเพิ่มรูป jump_potion.png เข้า Content Pipeline (Content.mgcb) ด้วยชื่อ asset "jump_potion"
        private const float JumpPotionBuffDuration = 5f;
        private const float JumpPotionDropChance = 0.60f;   // มอนปกติ (Ice Golem / Monster1) ตายแล้วมีโอกาสดรอป
        private const int JumpPotionDropCount = 1;
        private const float JumpPotionDropOffsetX = 50f;    // วางห่างจากยา HP ไม่ให้ไอคอนทับกัน
        private float _jumpBuffTimer = 0f;

        // --- Command /giveme_items Selector Menu ---
        private bool _isGiveItemMenuOpen = false;
        private readonly ItemType[] _giveableItems = new ItemType[] { ItemType.Wrench, ItemType.Seaweed, ItemType.HpPotion, ItemType.Key, ItemType.JumpPotion, ItemType.Trident, ItemType.Sword };

        // ขั้นตอนเลือกจำนวนก่อนเสกของ (Slider)
        private bool _giveItemQuantityStep = false;
        private ItemType _giveItemSelectedType = ItemType.None;
        private int _giveItemQuantity = 1;
        private bool _isDraggingGiveSlider = false;
        private const int GiveItemMaxQuantity = 99;

        // --- Attack ---
        private const int AttackFrameCount = 8;
        private const int AttackSheetColumns = 4;
        private const int AttackSheetRows = 2;
        private const float AttackFrameDuration = 0.07f;
        private AnimFrame[] _kaiAttack;
        private float _attackScale;
        private bool _isAttacking;
        private int _attackFrameIndex;
        private float _attackTimer;

        // --- Training dummy ---
        private Texture2D _dummyTexture;
        private Rectangle _dummyTrim;
        private float _dummyScale;
        private bool _dummyExists;
        private float _dummyX;
        private int _dummyRoom;
        private int _dummyHitCount;
        private int _dummyTotalDamage;
        private float _dummyShakeTimer;
        private bool _attackHitApplied;
        private readonly System.Random _random = new System.Random();

        private sealed class DamagePopup
        {
            public string Text;
            public Vector2 Position;
            public float Age;
            public bool IsCrit;
            public Color Color = Color.White;
        }
        private readonly List<DamagePopup> _damagePopups = new List<DamagePopup>();

        private const float DummyHeightToKaiRatio = 0.9f;
        private const float DummySpawnDistance = 200f;
        private const float DummyShakeDuration = 0.2f;
        private const float DummyShakeAmplitude = 6f;
        private const float DummyStatsTextScale = 0.9f;

        private const float AttackReach = 150f;
        private const int AttackHitFrameStart = 3;

        // --- ท่าฟันดาบ (ชีท Attack_Sword_P1_spritesheet.png 5 คอลัมน์ x 2 แถว = 10 เฟรม, asset "Attack_Sword_P1_spritesheet") ---
        //  คอมโบ 2 ที: คลิกแรกเล่นเฟรม 1-5 (index 0-4) / คลิกที่สองเล่นเฟรม 6-10 (index 5-9) แล้ววนกลับไปทีแรก
        //  คลิกที่สองต้องมาภายใน SwordComboWindow วินาทีหลังทีแรกจบ ไม่งั้นกลับไปฟันทีแรกใหม่ (คลิกระหว่างทีแรกกำลังเล่นก็จำไว้ใช้เป็นทีสอง)
        //  ตีโดนทีแรกที่เฟรม 3-5 (index 2-4) / ทีสองที่เฟรม 8-9 (index 7-8) = ช่วงที่ดาบเหวี่ยงไปด้านหน้าเต็มที่ / ระยะตีใช้ AttackReach เท่าเดิม
        private const int SwordAttackSheetColumns = 5;
        private const int SwordAttackSheetRows = 2;
        private const int SwordAttackTotalFrames = 10;
        private const int SwordAttackUsedFrameCount = 5;
        private const float SwordAttackFrameDuration = 0.12f;    // ช้าและหนัก (เฟรมละ 0.12 วินาที ทีละ ~0.6 วินาที)
        private const float SwordComboWindow = 0.6f;             // ต้องคลิกทีสองภายในเวลานี้หลังทีแรกจบ
        private const int SwordAttackHitFrameStart = 2;
        private const int SwordAttackHitFrameEnd = 4;
        private const int SwordCombo2HitFrameStart = 2;          // ทีสอง: นับในช่วงของทีสอง (0-4) -> เฟรม 8-9 ในชีท
        private const int SwordCombo2HitFrameEnd = 3;
        private const int SwordAttackScaleReferenceFrame = 9;    // เฟรมที่ Kai ยืนตรง (เฟรม 10) ใช้วัดขนาดตัวให้เท่าท่าปกติ
        private const float SwordAttackAnchorXRatio = 0.43f;     // จุดกึ่งกลางลำตัวในเซลล์ (สัดส่วนความกว้างเซลล์) ปรับถ้าตัวไหลซ้าย-ขวาตอนฟัน
        private AnimFrame[] _kaiSwordAttack;
        private float _swordAttackScale;
        private bool _attackUsesSword;                           // การตีรอบนี้เริ่มด้วยดาบ (ล็อกไว้ตลอดท่า)
        private int _swordComboStage;                            // ทีที่กำลังเล่น: 0 = ทีแรก, 1 = ทีสอง
        private int _swordComboNext;                             // คลิกถัดไปจะเป็นทีไหน (ใช้ได้เมื่อหน้าต่างเวลายังไม่หมด)
        private float _swordComboWindowTimer;
        private bool _swordBufferedClick;                        // กดคลิกระหว่างทีแรกกำลังเล่น: ต่อเป็นทีสองทันทีที่ทีแรกจบ
        private const int AttackHitFrameEnd = 5;

        private const int WrenchDamageMin = 8;
        private const int WrenchDamageMax = 12;

        // ดาบ (sword.png ตัวเดียวกับที่บอส 2 ถือ): หยิบได้ที่โครงกระดูกจุดเซฟ ตีแรงกว่าประแจ ~3 เท่า
        // ตอนนี้ยังใช้ท่าตีของประแจ (ภาพ Kai ยังถือประแจตอนตี) รอชีทท่าฟันดาบแล้วค่อยเปลี่ยน
        private const int SwordDamageMin = 24;
        private const int SwordDamageMax = 36;
        private const int SwordMaxStack = 1;
        private const float CritChance = 0.2f;
        private const int CritMultiplier = 2;

        private const float DamagePopupLifetime = 0.9f;
        private const float DamagePopupRiseSpeed = 70f;
        private const float DamagePopupNormalScale = 1f;
        private const float DamagePopupCritScale = 1.5f;

        // --- Ice Golem (summoned monster) ---
        private enum GolemState { Idle, Walking, Attacking, Hurt, Dying }

        private sealed class IceGolem
        {
            public float X;
            public float SpawnX;
            public bool IsAggro;
            public int Room;
            public float HP;
            public GolemState State;
            public float AnimTimer;
            public int FrameIndex;
            public bool FacingRight;
            public float AttackCooldownTimer;
            public bool AttackHitApplied;
            // ตำแหน่งเท้าแนวตั้ง: 0 = บนพื้นเดิม, ติดลบ = ลอยอยู่เหนือพื้น (เช่น ยืนบนพื้นที่ยกสูง/กำลังกระโดด)
            public float YOffset;
            public float VelocityY;
            public bool Airborne;
        }

        private const int GolemIdleFrameCount = 8;
        private const int GolemWalkFrameCount = 10;
        private const int GolemAttackFrameCount = 11;
        private const int GolemHurtFrameCount = 4;
        private const int GolemDieFrameCount = 13;

        private const float GolemFrameDuration = 0.12f;
        private const float GolemAttackFrameDuration = 0.08f;
        private const float GolemHurtFrameDuration = 0.08f;
        private const float GolemDieFrameDuration = 0.07f;

        private const int GolemAttackHitFrameStart = 5;
        private const int GolemAttackHitFrameEnd = 7;

        private const float GolemHeightToKaiRatio = 1.4f; // ใหญ่กว่า Kai
        private const float GolemMaxHP = 250f;
        private const float GolemWalkSpeed = 120f;
        private const float GolemAttackRange = 140f;
        private const float GolemAggroRange = 500f; // ระยะที่ golem จะเริ่มไล่ผู้เล่น
        private const float GolemReturnThreshold = 5f; // ระยะที่ถือว่ากลับถึงจุดเกิดแล้ว
        private const float GolemAttackReach = 160f;
        private const float GolemAttackCooldown = 1f;
        private const int GolemDamageMin = 8;
        private const int GolemDamageMax = 16;

        private const int GolemSummonMaxCount = 32;
        private const float GolemSpawnDistance = 220f;
        private const float GolemSpawnSpacing = 100f;

        private AnimFrame[] _golemIdle;
        private AnimFrame[] _golemWalk;
        private AnimFrame[] _golemAttack;
        private AnimFrame[] _golemHurt;
        private AnimFrame[] _golemDie;
        private float _golemScale;
        private float _golemHalfWidth;
        private float _golemHeight;
        private readonly List<IceGolem> _iceGolems = new List<IceGolem>();

        // --- Monster 1 (มอนสเตอร์ธรรมดา, summon ด้วย /summon_mtlv1_<n>) ---
        // มีแค่ท่าเดินกับท่าโจมตี (ไม่มี idle/hurt/die แยก): ใช้เฟรมแรกของท่าเดินเป็น idle,
        // ตอนโดนตีใช้เอฟเฟกต์กระพริบสี ตอนตายเล่นท่าตาย 8 เฟรม (monster_lv1_die) -> ค้างเฟรมสุดท้ายแล้วค่อยๆ จางหาย -> ลบออก
        private enum Monster1State { Idle, Walking, Attacking }

        private sealed class Monster1
        {
            public float X;
            public float SpawnX;
            public bool IsAggro;
            public int Room;
            public float HP;
            public Monster1State State;
            public float AnimTimer;
            public int FrameIndex;
            public bool FacingRight;
            public float AttackCooldownTimer;
            public bool AttackHitApplied;
            public float HitFlashTimer;
            public bool IsScripted;        // true = ฉาก orb ห้อง 3 คุมการเดินอยู่ (ข้าม AI ปกติ)
            public bool IsDying;
            public float DeathTimer;       // เวลาที่ผ่านไปนับจากตาย (ใช้เลือกเฟรมท่าตาย + คำนวณการจางหาย)
            public bool DeathDropDone;     // ดรอปยาไปแล้วหรือยัง (ดรอปครั้งเดียวตอนท่าตายเล่นจบ)
            public float YOffset;
            public float VelocityY;
            public bool Airborne;
        }

        private const int Monster1WalkFrameCount = 8;
        private const int Monster1AttackFrameCount = 8;

        private const float Monster1FrameDuration = 0.12f;
        private const float Monster1AttackFrameDuration = 0.08f;

        private const int Monster1AttackHitFrameStart = 3;
        private const int Monster1AttackHitFrameEnd = 4;

        // ความแรง/ความเร็วพอๆ กับ Ice Golem ตามที่สั่ง (ใช้ค่าเดียวกัน)
        private const float Monster1HeightToKaiRatio = 1.0f; // ตัวธรรมดา สูงพอๆ กับ Kai (ยังไม่ได้ระบุ ปรับได้ทีหลัง)
        private const float Monster1MaxHP = GolemMaxHP;
        private const float Monster1WalkSpeed = GolemWalkSpeed;
        private const float Monster1AttackRange = GolemAttackRange;
        private const float Monster1AggroRange = GolemAggroRange;
        private const float Monster1ReturnThreshold = GolemReturnThreshold;
        private const float Monster1AttackReach = GolemAttackReach;
        private const float Monster1AttackCooldown = GolemAttackCooldown;
        private const int Monster1DamageMin = GolemDamageMin;
        private const int Monster1DamageMax = GolemDamageMax;

        private const int Monster1SummonMaxCount = 32;
        private const float Monster1SpawnDistance = GolemSpawnDistance;
        private const float Monster1SpawnSpacing = GolemSpawnSpacing;

        // ตัวคูณสเกลเฉพาะท่าโจมตี (1.0 = สเกลเดียวกับท่าเดิน) ปรับได้ถ้าท่าโจมตีดูใหญ่/เล็กกว่าท่าเดิน
        private const float Monster1AttackScaleMultiplier = 1.0f;

        private const float Monster1HitFlashDuration = 0.15f;

        // ท่าตาย: เล่น 8 เฟรมทีละ 0.1 วิ (~0.8 วิ) แล้วค้างเฟรมสุดท้ายพร้อมค่อยๆ จางหายในเวลา Monster1DieFadeOutDuration ก่อนลบ
        private const int Monster1DieFrameCount = 8;
        private const float Monster1DieFrameDuration = 0.1f;
        private const float Monster1DieFadeOutDuration = 0.8f;
        private const float Monster1DieAnimDuration = Monster1DieFrameCount * Monster1DieFrameDuration;
        private const float Monster1DieTotalDuration = Monster1DieAnimDuration + Monster1DieFadeOutDuration;
        // ตัวคูณสเกลเฉพาะท่าตาย (1.0 = สเกลเดียวกับท่าเดิน) ปรับได้ถ้าท่าตายดูใหญ่/เล็กกว่าท่าเดิน
        private const float Monster1DieScaleMultiplier = 1.0f;

        private AnimFrame[] _monster1Walk;
        private AnimFrame[] _monster1Attack;
        private AnimFrame[] _monster1Die;
        private float _monster1Scale;
        private float _monster1AttackScale;
        private float _monster1DieScale;
        private float _monster1HalfWidth;
        private float _monster1Height;
        private readonly List<Monster1> _monster1List = new List<Monster1>();

        // --- Boss 1 (2 phase: เข้า phase 2 เมื่อ HP ต่ำกว่า 40%, ตายแล้วดรอปกุญแจ + ยา, เสกด้วย /summon_boss1_<n> เท่านั้น) ---
        private enum Boss1State { Idle, Walking, Attacking }

        private sealed class Boss1
        {
            public float X;
            public float SpawnX;
            public bool IsAggro;
            public int Room;
            public float HP;
            public Boss1State State;
            public float AnimTimer;
            public int FrameIndex;
            public bool FacingRight;
            public float AttackCooldownTimer;
            public bool AttackHitApplied;
            public bool IsPhase2;
            public bool AttackIsPhase2;   // จดไว้ตอนเริ่มท่าโจมตี ให้ท่าที่กำลังเล่นอยู่ไม่สลับชีทกลางคัน
            public bool AttackIsOrb;      // true = ท่าที่กำลังเล่นคือยิงลูกบอล (ไม่ใช่ฟาดระยะใกล้) เฉพาะ phase 2
            public float HitFlashTimer;
            public bool IsDying;
            public float DeathTimer;
            public bool DeathDropDone;
            public float YOffset;
            public float VelocityY;
            public bool Airborne;
            public bool IsStatue;          // true = ยืนนิ่งเป็นรูปปั้น (ฉากบอสห้องลับ) ข้าม AI และตีไม่โดนจนกว่าฉากจะจบ
        }

        // ทุกชีทมี 8 เฟรม
        private const int Boss1FrameCount = 8;
        private const float Boss1FrameDuration = 0.12f;          // เดิน/ลอยนิ่ง
        private const float Boss1AttackFrameDurationP1 = 0.11f;
        private const float Boss1AttackFrameDurationP2 = 0.08f;  // phase 2 ฟาดเร็วขึ้น
        private const float Boss1DieFrameDuration = 0.15f;
        private const float Boss1DieAnimDuration = Boss1FrameCount * Boss1DieFrameDuration;

        // เฟรมที่หนวดฟาดโดน (นับจาก 0) — ปรับได้ถ้าจังหวะโดนไม่ตรงกับภาพ
        private const int Boss1AttackHitFrameStart = 3;
        private const int Boss1AttackHitFrameEnd = 5;

        // ค่าเริ่มต้น (ตั้งให้ก่อน ปรับทีหลังได้)
        private const float Boss1HeightToKaiRatio = 2.4f;
        private const float Boss1HitboxWidthRatio = 0.6f;   // ฮิตบ็อกซ์แคบกว่าสไปรท์ (สไปรท์รวมวงแหวนที่ลอยออกด้านข้าง)
        private const float Boss1MaxHP = 350f;
        private const float Boss1Phase2HpRatio = 0.4f;      // HP ต่ำกว่า 40% -> phase 2

        private const float Boss1WalkSpeed = 90f;
        private const float Boss1Phase2SpeedMultiplier = 1.6f;
        private const float Boss1AggroRange = 800f;
        private const float Boss1AttackRange = 260f;
        private const float Boss1ReturnThreshold = 5f;

        private const float Boss1AttackReachP1 = 320f;
        private const float Boss1AttackReachP2 = 380f;
        private const float Boss1AttackCooldownP1 = 1.6f;
        private const float Boss1AttackCooldownP2 = 0.9f;
        private const int Boss1DamageMinP1 = 12;
        private const int Boss1DamageMaxP1 = 20;
        private const int Boss1DamageMinP2 = 20;
        private const int Boss1DamageMaxP2 = 32;

        private const float Boss1HitFlashDuration = 0.12f;

        private const int Boss1SummonMaxCount = 4;
        private const float Boss1SpawnDistance = 450f;
        private const float Boss1SpawnSpacing = 500f;

        // ของดรอปตอนตาย: กุญแจ 1 อัน + ยาเพิ่มเลือด
        private const int Boss1KeyDropCount = 1;
        private const int Boss1PotionDropMin = 3;
        private const int Boss1PotionDropMax = 5;
        private const float Boss1LootSpread = 50f;

        private AnimFrame[] _boss1Walk;
        private AnimFrame[] _boss1AttackP1;
        private AnimFrame[] _boss1AttackP2;
        private AnimFrame[] _boss1Die;
        private float _boss1Scale;
        private float _boss1HalfWidth;
        private float _boss1Height;
        private int _boss1FeetLineY; // แถวพิกเซลในชีทที่ถือเป็น "พื้น" (ก้นเฟรมแรกของท่าเดิน) ใช้ร่วมกันทุกเฟรมกันตัวเด้ง
        private readonly List<Boss1> _boss1List = new List<Boss1>();

        // =====================================================================
        // Boss 2 (ด่าน 2): HP 1000, 2 เฟส, เสกด้วย /summon_boss2_<n> เท่านั้น (ยังไม่วางในแมพ)
        //  เฟส 1: เดินไล่ + ฟันดาบระยะใกล้
        //  เฟส 2 (HP < 50%): ฟันเร็วขึ้น + ปาดาบ 4 เล่มใส่ Kai แล้วดึงกลับเหมือนแมกนีโต (ดาบทำดาเมจทั้งขาไปและขากลับ)
        // ชีทภาพทุกชีท 8 เฟรม (4 คอลัมน์ x 2 แถว) และ "หันหน้าไปทางซ้าย" ในรูปต้นฉบับ
        // ต้องเพิ่ม boss_2_walk / boss_2_phase1_attack / boss_2_phase2_attack / boss_2_phase2_Throw / boss_2_phase2_back / boss_2_die
        // เข้า Content Pipeline (Content.mgcb) ถ้ายังไม่เพิ่ม เกมจะเปิดได้ปกติ แต่ /summon_boss2 จะแจ้งว่ายังไม่มีภาพ
        // (ยังไม่ใช้ boss_2_parry_attack และ sword_boss_eff)
        // =====================================================================
        private enum Boss2State { Idle, Walking, Attacking, Throwing, WaitingSwords, Recalling, Blocking }
        private enum Boss2SwordState { Flying, Stuck, Returning, Scattered }

        private sealed class Boss2
        {
            public float X;
            public float SpawnX;
            public bool IsAggro;
            public int Room;
            public float HP;
            public Boss2State State;
            public float AnimTimer;
            public int FrameIndex;
            public bool FacingRight;
            public float AttackCooldownTimer;
            public float ThrowCooldownTimer;
            public float BlockCooldownTimer;   // คูลดาวน์การกัน trident (กันได้อีกครั้งเมื่อเป็น 0)
            public bool IsStatue;              // ยืนนิ่งรอฉากเริ่ม (AI ไม่ทำงาน) ใช้กับบอส 2 ในฉากห้องที่ 3 ของด่าน 2
            public bool AttackHitApplied;
            public bool IsPhase2;
            public bool AttackIsPhase2;      // จดไว้ตอนเริ่มท่า ให้ท่าที่กำลังเล่นอยู่ไม่สลับชีทกลางคัน
            public bool SwordsLaunched;      // ปล่อยดาบรอบนี้แล้วหรือยัง
            public float SwordWaitTimer;     // เวลาที่ดาบทุกเล่มหยุดนิ่งรอถูกดึงกลับ
            public float TotalWaitTimer;     // เวลารวมที่รอดาบ (กันค้าง)
            public bool OutParried;          // รอบนี้แพรี่ดาบขาไปสำเร็จแล้ว (บอสเสียเลือดครั้งเดียวต่อขา)
            public bool BackParried;
            public bool SwordsDeflected;     // รอบปานี้ Kai แพรี่สำเร็จแล้ว: ดาบทั้งหมดไม่ทำดาเมจอีก (ทั้งขาไปและตอนกลับมือ)
            public float HitFlashTimer;
            public bool IsDying;
            public float DeathTimer;
            public float YOffset;
            public float VelocityY;
            public bool Airborne;
        }

        private sealed class Boss2Sword
        {
            public Boss2 Owner;
            public int Index;
            public int Room;
            public Boss2SwordState State;
            public float X;
            public float Y;
            public float VX;
            public float VY;
            public float Angle;              // ทิศปลายดาบ (เรเดียน)
            public float Delay;              // หน่วงปล่อยทีละเล่ม (ระหว่างหน่วงจะไม่ขยับ/ไม่ทำดาเมจ/ไม่วาด)
            public float Traveled;
            public float ReturnTime;
            public bool HitOut;              // ทำดาเมจขาไปแล้ว
            public bool HitBack;             // ทำดาเมจขากลับแล้ว
            public bool Deflected;           // โดนแพรี่แล้วกระจายออก: ไม่ทำดาเมจอีกจนจบรอบปา
            public float Spin;               // ความเร็วหมุนตอนกระจาย (เรเดียน/วินาที)
        }

        private const int Boss2FrameCount = 8;
        private const int Boss2SheetColumns = 4;
        private const int Boss2SheetRows = 2;

        private const float Boss2MaxHP = 1000f;
        private const float Boss2Phase2HpRatio = 0.5f;          // HP ต่ำกว่า 50% -> เฟส 2 (ปรับได้)

        private const float Boss2HeightToKaiRatio = 2.4f;
        private const float Boss2HitboxHalfWidthCellRatio = 0.22f;   // ครึ่งความกว้างฮิตบ็อกซ์ เทียบกับความสูงของเฟรมในชีท (ไม่รวมดาบที่ยื่นออกด้านข้าง)
        private const float Boss2AnchorFromRightCellRatio = 0.3436f; // จุดกึ่งกลางลำตัว = ขอบขวาสุดของตัวบอสในเฟรมแรก ลบค่านี้ (เทียบความสูงเฟรม) ทำให้ลำตัวไม่ไหลเวลาสลับชีท

        private const float Boss2FrameDuration = 0.12f;          // เดิน/ยืน
        private const float Boss2AttackFrameDurationP1 = 0.11f;
        private const float Boss2AttackFrameDurationP2 = 0.09f;
        private const float Boss2ThrowFrameDuration = 0.09f;
        private const float Boss2RecallFrameDuration = 0.09f;
        private const float Boss2DieFrameDuration = 0.15f;
        private const float Boss2DieAnimDuration = Boss2FrameCount * Boss2DieFrameDuration;

        // เฟรมที่ฟาดโดน (นับจาก 0) — ปรับได้ถ้าจังหวะโดนไม่ตรงกับภาพ
        private const int Boss2AttackHitFrameStart = 3;
        private const int Boss2AttackHitFrameEnd = 5;
        private const int Boss2ThrowLaunchFrame = 2;             // เฟรมที่ดาบหลุดจากมือ (เริ่มปล่อยดาบ)

        private const float Boss2WalkSpeed = 100f;
        private const float Boss2Phase2SpeedMultiplier = 1.3f;
        private const float Boss2AggroRange = 900f;
        private const float Boss2AttackRange = 280f;
        private const float Boss2ReturnThreshold = 5f;
        private const float Boss2AttackReachP1 = 300f;
        private const float Boss2AttackReachP2 = 340f;
        private const float Boss2AttackCooldownP1 = 1.6f;
        private const float Boss2AttackCooldownP2 = 1.2f;
        private const int Boss2DamageMinP1 = 50;
        private const int Boss2DamageMaxP1 = 50;
        private const int Boss2DamageMinP2 = 50;
        private const int Boss2DamageMaxP2 = 50;
        private const float Boss2HitFlashDuration = 0.12f;

        // --- เฟส 2: ปาดาบ + ดึงกลับ ---
        private const float Boss2ThrowMinDistance = 450f;        // Kai ต้องอยู่ห่างอย่างน้อยเท่านี้ถึงจะปาดาบ (ใกล้กว่านี้ใช้ฟันแทน)
        private const float Boss2ThrowMaxDistance = 1100f;
        private const float Boss2ThrowCooldown = 2.5f;           // คูลดาวน์หลังดึงดาบกลับมาครบ
        private static readonly float[] Boss2SwordHandHeightRatios = { 0.75f, 0.65f, 0.44f, 0.35f };   // ความสูงมือแต่ละข้าง (สัดส่วนความสูงบอส จากเท้า)
        private static readonly float[] Boss2SwordSpreadRadians = { -0.10f, -0.035f, 0.035f, 0.10f };  // องศาแยกของดาบแต่ละเล่ม (เล็งไป Kai ตอนปา)
        private const float Boss2SwordLaunchStagger = 0.05f;     // ปล่อยดาบห่างกันเล่มละกี่วินาที
        private const float Boss2SwordSpeed = 1000f;
        private const float Boss2SwordRange = 1000f;             // ระยะบินสูงสุดก่อนหยุดค้าง
        private const float Boss2SwordHoverTime = 0.3f;          // ดาบหยุดค้างครบทุกเล่มแล้วรอเท่านี้ก่อนบอสดึงกลับ
        private const float Boss2SwordReturnStartSpeed = 400f;
        private const float Boss2SwordReturnAccel = 2500f;       // ดาบวิ่งกลับเร่งขึ้นเรื่อยๆ
        private const float Boss2SwordArriveDistance = 50f;
        private const float Boss2SwordHideDistance = 130f;       // ใกล้ตัวบอสภายในระยะนี้ไม่วาดดาบ (ในชีทมีภาพดาบบินออก/เข้ามืออยู่แล้ว กันซ้อน)
        private const float Boss2SwordRadius = 16f;              // รัศมีฮิตของแต่ละจุดบนตัวดาบ
        private const float Boss2SwordDrawLength = 160f;
        private const float Boss2SwordDrawThickness = 9f;
        private const int Boss2SwordDamageMin = 9;
        private const int Boss2SwordDamageMax = 14;
        private const float Boss2RecallMaxWait = 3f;

        // --- แพรี่ดาบสำเร็จ: ดาบทั้ง 4 เล่มกระจายออก ตกพื้น แล้วค่อยถูกดึงกลับมือโดยไม่ทำดาเมจ (บอสไม่เสียเลือด) ---
        private const float Boss2SwordScatterMinSpeed = 350f;
        private const float Boss2SwordScatterMaxSpeed = 650f;
        private const float Boss2SwordScatterGravity = 1800f;
        private const float Boss2SwordScatterMaxSpin = 12f;

        private const int Boss2SummonMaxCount = 2;
        private const float Boss2SpawnDistance = 500f;
        private const float Boss2SpawnSpacing = 600f;

        private const string Boss2DisplayName = "Brother";

        private bool _boss2Loaded;
        private AnimFrame[] _boss2Walk;
        private AnimFrame[] _boss2AttackP1;
        private AnimFrame[] _boss2AttackP2;
        private AnimFrame[] _boss2Throw;
        private AnimFrame[] _boss2Back;
        private AnimFrame[] _boss2Die;
        private float _boss2Scale;
        private float _boss2HalfWidth;
        private float _boss2Height;
        // ภาพดาบ sword.png (asset "sword"): ปลายดาบชี้ขึ้นขวาเฉียง ~70.5 องศาจากแนวนอน ค่าด้านล่างวัดจากภาพจริง (ใช้สัดส่วนกับความสูงภาพ ย่อภาพได้โดยไม่ต้องแก้)
        private const float Boss2SwordImageAxisAngleDegrees = -70.455f;
        private const float Boss2SwordImageAxisLengthToHeight = 1.0459f;
        private Texture2D _boss2SwordTexture;   // asset "sword" (sword.png) ถ้าโหลดไม่ได้จะวาดดาบจำลองจากสี่เหลี่ยมแทน
        private readonly List<Boss2> _boss2List = new List<Boss2>();
        private readonly List<Boss2Sword> _boss2Swords = new List<Boss2Sword>();

        // =====================================================================
        // Monster 2 (มอนเล็กด่าน 2): HP 25, ชาร์จแล้วพุ่งชน Kai ดาเมจ 5 + Kai ติดสตั้น 2 วินาที (ทำอะไรไม่ได้เลย)
        //  แพรี่ (R) ตอนมันพุ่งโดน = Kai ไม่เสียเลือด/ไม่สตั้น แต่มอนติดสตั้น 2 วินาที
        //  เสกด้วย /summon_mtlv2_<n> เท่านั้น (ยังไม่วางในแมพ)
        // ชีทภาพ 8 เฟรม (4 คอลัมน์ x 2 แถว): monster_2_walk / monster_2_attack / monster_2_die ต้องเพิ่มเข้า Content.mgcb
        //  attack: เฟรม 0 = ชาร์จ, เฟรม 1-6 = พุ่ง, เฟรม 7 = พักหลังพุ่ง
        // ถ้ายังไม่เพิ่มเข้า Content Pipeline เกมเปิดได้ปกติ แต่ /summon_mtlv2 จะแจ้งว่ายังไม่มีภาพ
        // =====================================================================
        private enum Monster2State { Idle, Walking, Charging, Dashing, Recovering, Stunned }

        private sealed class Monster2
        {
            public float X;
            public float SpawnX;
            public bool IsAggro;
            public bool AlwaysAggro;   // เวฟด่าน 2: ไล่ผู้เล่นเสมอ ไม่กลับจุดเกิดแม้อยู่นอกระยะไล่
            public bool HasRoared;     // ร้องตอนเริ่มไล่ Kai ไปแล้วหรือยัง (ร้องครั้งเดียวต่อตัว)
            public int Room;
            public float HP;
            public Monster2State State;
            public float AnimTimer;
            public float StateTimer;
            public int FrameIndex;
            public bool FacingRight;
            public float AttackCooldownTimer;
            public float DashDirection;
            public float DashTraveled;
            public bool DashHitApplied;
            public float HitFlashTimer;
            public bool DeathDropDone;     // ดรอปยา HP ไปแล้วหรือยัง (ดรอปครั้งเดียวตอนท่าตายเล่นจบ)
            public bool IsDying;
            public float DeathTimer;
            public float YOffset;
            public float VelocityY;
            public bool Airborne;
        }

        private const int Monster2FrameCount = 8;
        private const int Monster2SheetColumns = 4;
        private const int Monster2SheetRows = 2;
        private const bool Monster2SpritesFaceRight = false;      // ชีทต้นฉบับหันหน้าไปทางซ้าย (ถ้าเห็นมอนหันกลับด้านให้สลับค่านี้)
        private const float Monster2AnchorCellWidthRatio = 0.525f; // จุดกึ่งกลางลำตัวในเซลล์ (สัดส่วนความกว้างเซลล์)
        private const float Monster2HitboxHalfWidthCellRatio = 0.36f;

        private const float Monster2HeightToKaiRatio = 0.8f;      // มอนเล็ก ตัวเตี้ยกว่า Kai
        private const float Monster2MaxHP = 25f;
        private const float Monster2WalkSpeed = 150f;
        private const float Monster2AggroRange = 700f;
        private const float Monster2ReturnThreshold = 5f;
        private const float Monster2FrameDuration = 0.1f;         // เดิน
        private const float Monster2ChargeTriggerRange = 420f;    // เข้าใกล้ Kai ถึงระยะนี้ถึงเริ่มชาร์จ
        private const float Monster2ChargeDuration = 0.9f;        // ชาร์จนิ่งก่อนพุ่ง (มีหลอดแดงเหนือหัวให้ดูจังหวะแพรี่)
        private const float Monster2DashSpeed = 950f;
        private const float Monster2DashDistance = 520f;
        private const float Monster2RecoverDuration = 0.7f;       // พักหลังพุ่ง (โดนตีได้สบาย)
        private const float Monster2AttackCooldown = 3f;
        private const int Monster2Damage = 5;
        private const float Monster2KaiStunDuration = 2f;
        private const float Monster2SelfStunDuration = 2f;        // มอนติดสตั้นตอนโดนแพรี่
        private const float Monster2ChargeShakeAmplitude = 3f;
        private const float Monster2HitFlashDuration = 0.15f;
        private const int Monster2DieFrameCount = 8;
        private const float Monster2DieFrameDuration = 0.1f;
        private const float Monster2DieAnimDuration = Monster2DieFrameCount * Monster2DieFrameDuration;

        private const int Monster2SummonMaxCount = 16;
        private const float Monster2SpawnDistance = 500f;
        private const float Monster2SpawnSpacing = 200f;

        private bool _monster2Loaded;
        private AnimFrame[] _monster2Walk;
        private AnimFrame[] _monster2Attack;
        private AnimFrame[] _monster2Die;
        private float _monster2Scale;
        private float _monster2HalfWidth;
        private float _monster2Height;
        private readonly List<Monster2> _monster2List = new List<Monster2>();

        // Kai ติดสตั้น: ทำอะไรไม่ได้เลย (เดิน/กระโดด/โจมตี/แพรี่/ใช้ไอเทม)
        private float _kaiStunTimer;

        // --- Boss 1 phase 2: บินได้ + ยิงลูกบอล (power_boss.png) ---
        // ลอยสูงตอนอยู่ไกล (ยิงลูกบอลพุ่งหา Kai) / ลงมาที่พื้นตอน Kai เข้าใกล้ (ฟาดระยะใกล้)
        private const float Boss1HoverHeightKaiRatio = 1.3f;     // ความสูงที่ลอย (เท่าของความสูง Kai) ลอยสูงจน Kai บนพื้นตีไม่ถึง ต้องกระโดดถึงจะตีได้
        private const float Boss1FlightSpeed = 300f;              // ความเร็วขึ้น/ลง (พิกเซล/วินาที)
        private const float Boss1HoverBobAmplitude = 8f;          // ลอยโยกขึ้นลงเบาๆ
        private const float Boss1HoverBobSpeed = 2.4f;
        private const float Boss1LandingExtraRange = 120f;        // ลงมาแล้วจะอยู่ต่ำจนกว่า Kai จะห่างเกิน AttackRange + ค่านี้ (กันขึ้นๆ ลงๆ รัวๆ)
        private const float BossOrbPreferredDistance = 520f;      // ตอนลอยจะไม่เข้าใกล้ Kai เกินระยะนี้ (ยืนยิงอยู่ห่างๆ)
        private const float BossOrbMaxShootDistance = 1100f;      // ไกลเกินนี้จะเดินเข้าไปก่อนยิง
        private const float BossOrbCooldown = 1.6f;               // คูลดาวน์หลังยิงลูกบอล
        private const float BossOrbSpeed = 520f;
        private const float BossOrbReflectSpeed = 720f;           // ลูกบอลที่ถูกแพรี่สะท้อนกลับเร็วขึ้น
        private const float BossOrbRadius = 34f;                  // รัศมีฮิตบ็อกซ์ (วงกลม)
        private const float BossOrbDrawSize = 140f;               // ความกว้างที่วาดบนจอ (รูปมีหางวนรอบแกนกลาง แกนกลางประมาณครึ่งหนึ่งของความกว้างรูป)
        private const float BossOrbSpinSpeed = 4f;                // รอบ/วินาที (เรเดียน) หมุนวน
        private const float BossOrbLifetime = 6f;
        private const int BossOrbDamageMin = 14;
        private const int BossOrbDamageMax = 22;
        private const int BossOrbReflectDamageMin = 40;           // ดาเมจที่บอสโดนเมื่อโดนลูกบอลของตัวเองสะท้อนกลับ
        private const int BossOrbReflectDamageMax = 55;

        // --- หลอดเลือดบอส (กลางจอด้านบน) แสดงตลอดที่บอสยังมีชีวิตอยู่ในห้องเดียวกับ Kai ---
        private const string Boss1DisplayName = "Beequid";   // ชื่อที่แสดงเหนือหลอด (ถ้าจะใช้ภาษาไทย ฟอนต์ต้องมีตัวอักษรไทย)
        private const int BossHpBarWidth = 460;
        private const int BossHpBarHeight = 18;
        private const int BossHpBarTopMargin = 10;
        private const float BossHpBarNameScale = 0.8f;

        private sealed class BossOrb
        {
            public float X;
            public float Y;
            public float VX;
            public float VY;
            public int Room;
            public float Age;
            public bool Reflected;   // true = โดน Kai แพรี่สะท้อนกลับ (ทำดาเมจบอส ไม่ทำดาเมจ Kai)
        }

        private readonly List<BossOrb> _bossOrbs = new List<BossOrb>();
        private Texture2D _bossOrbTexture;   // ต้องเพิ่ม power_boss.png เข้า Content Pipeline ด้วยชื่อ asset "power_boss" (ถ้าไม่มี จะวาดวงสี่เหลี่ยมแดงแทน ไม่ขึ้น error)

        // --- Wake-up Intro (หลังกด Start ครั้งแรก: Kai นอนคว่ำ -> กะพริบตาช้าๆ -> จอมืดค้าง -> ตื่นมายืนปกติ) ---
        // ลำดับเวลา: เปิดตาค้างไว้ -> กะพริบ (มืดสนิท) x WakeUpBlinkCount ครั้ง โดยครั้งสุดท้ายมืดค้างนาน WakeUpFinalDarkDuration
        // -> ตอนจอมืดสนิทสลับเป็นท่ายืน idle + เล่นเสียงเสื้อผ้า -> จอสว่างกลับแล้วผู้เล่นควบคุมได้
        private const float WakeUpIntroFadeInDuration = 2.5f;   // อินโทรจบ -> จอมืดสนิทแล้วค่อยๆ สว่างขึ้นในเวลานี้ (วินาที)
        private const float WakeUpInitialOpenDuration = 1.2f;   // เปิดตาค้างไว้ก่อนกะพริบครั้งแรก (นับหลังจอสว่างเต็มแล้ว)
        private const int WakeUpBlinkCount = 4;                 // จำนวนครั้งที่กะพริบ (รวมครั้งสุดท้ายที่มืดค้าง)
        private const float WakeUpBlinkCloseDuration = 0.7f;    // จอค่อยๆ มืดลง
        private const float WakeUpBlinkDarkHoldDuration = 0.2f; // ค้างมืดสนิท (ไม่ใช่ครั้งสุดท้าย)
        private const float WakeUpBlinkOpenDuration = 0.7f;     // จอค่อยๆ สว่างกลับ
        private const float WakeUpBlinkGapDuration = 0.8f;      // เปิดตาค้างระหว่างการกะพริบแต่ละครั้ง
        private const float WakeUpFinalDarkDuration = 3.5f;     // กะพริบครั้งสุดท้าย: จอมืดสนิทค้างกี่วินาที
        private const float WakeUpFinalOpenDuration = 1.5f;     // จอสว่างกลับหลังมืดค้าง
        private const float SleepPoseWidthToKaiHeightRatio = 1.1f; // ความยาวตัวตอนนอน เทียบกับความสูงตอนยืน
        private const float WakeUpCameraZoom = 2.5f;           // ซูมเข้าหาตัวละครระหว่างฉากตื่นนอน (ปกติในเกมคือ CameraZoom = 1.5)
        private const float SleepPoseTiltDegrees = 0f;          // เอียงตัวตอนนอนกี่องศา (0 = ไม่เอียง, บวก = หัวเงยขึ้น, ลบ = หัวเอียงลง)
        private const float SleepPoseGroundSink = 35f;            // ลดตัวตอนนอนลงไปจมในแถบพื้น (พิกเซลโลก, กล้องซูม 1.5 เท่า) ให้ท้องแนบพื้น
        private const float WakeUpRustleAfterDarkDelay = 0.8f;      // เสียงเสื้อผ้าครั้งที่ 1: หลังจอมืดสนิทกี่วินาที
        private const float WakeUpRustleBeforeBrightenLead = 0.7f;  // เสียงเสื้อผ้าครั้งที่ 2: ก่อนจอเริ่มสว่างกี่วินาที

        private const float WakeUpFinalDarkStartTime = WakeUpIntroFadeInDuration + WakeUpInitialOpenDuration
            + (WakeUpBlinkCount - 1) * (WakeUpBlinkCloseDuration + WakeUpBlinkDarkHoldDuration + WakeUpBlinkOpenDuration + WakeUpBlinkGapDuration)
            + WakeUpBlinkCloseDuration;
        private const float WakeUpFinalDarkEndTime = WakeUpFinalDarkStartTime + WakeUpFinalDarkDuration;
        private const float WakeUpTotalDuration = WakeUpFinalDarkEndTime + WakeUpFinalOpenDuration;

        private static readonly float[] WakeUpRustleTimes =
        {
            WakeUpFinalDarkStartTime + WakeUpRustleAfterDarkDelay,
            WakeUpFinalDarkEndTime - WakeUpRustleBeforeBrightenLead
        };

        private Texture2D _kaiSleepTexture;
        private Rectangle _kaiSleepTrim;
        private float _kaiSleepScale;
        private float _kaiSleepRotation;
        private float _kaiSleepCenterOffsetY; // ระยะจากแนวพื้นถึงจุดกึ่งกลางภาพ (ทำให้จุดต่ำสุดของตัวที่เอียงแล้วอยู่ที่แนวพื้นพอดี)
        private bool _isWakeUpSequence;
        private float _wakeUpTimer;
        private float _wakeUpOverlayAlpha;
        private int _wakeUpNextRustleIndex;
        // --- Intro Story: ต่อจากกะพริบตา -> บทพูดที่ 1 -> ประแจตกใส่หัว Kai -> จอดำทันที + บทพูดที่ 2
        //     -> จอค่อยๆ สว่าง โดยประแจวางอยู่ข้างหน้า Kai แล้ว -> จบฉาก ผู้เล่นควบคุมได้ ---
        private enum IntroStage { Blinking, Dialogue1, WrenchFall, Dialogue2, FadeIn }
        private IntroStage _introStage = IntroStage.Blinking;

        // ข้อความบทพูด (แก้ตรงนี้ได้เลย): 1 สมาชิก = 1 หน้าข้อความ, ใช้ "\n" ขึ้นบรรทัดใหม่ได้ (ข้อความยาวจะตัดบรรทัดให้อัตโนมัติ)
        // หมายเหตุ: ตัวอักษรที่ไม่มีใน HotbarFont.spritefont จะถูกแสดงเป็น '?' (ถ้าจะใช้ภาษาไทยต้องเพิ่มช่วงตัวอักษรไทยใน spritefont)
        // ชื่อผู้พูด: ถ้าไม่ใส่อะไรจะใช้ชื่อเริ่มต้นของฉากนั้น ถ้าอยากเปลี่ยนเฉพาะหน้าให้เขียนเป็น "ชื่อ|ข้อความ" เช่น "Boss|Hello" (ชื่อยาวไม่เกิน 24 ตัวอักษร)
        private static readonly string[] IntroDialogue1Lines =
        {
            "Where the hell am I ?",
            "A ... cave ?",
            "You've gotta be kidding me !?",
            "How the hell am I supposed to get out of here ?",
            "This is all because of that damn weird storm",
            " ... I’d better find a way out"
        };

        private static readonly string[] IntroDialogue2Lines =
        {
            "Ow! What the hell was that ?",
            "A wrench ? Where the hell did this come from ?",
            "Whatever. I need to find a way out"
        };

        // กรอบข้อความบทพูด (พิกัดหน้าจอเชิงตรรกะ 1280x720)
        // กล่องข้อความใช้รูป text_box.png (1920x1080 มีพื้นโปร่งใสรอบๆ) ย่อด้วย DialogueBoxScale แล้ววางโดยอิงจาก "กล่องใหญ่" ในรูป
        private const float DialogueBoxScale = 0.47f;               // กล่องใหญ่กว้างราว 680px สูงราว 200px
        private const float DialogueArtBodyLeft = 234f;             // กล่องใหญ่ในรูปต้นฉบับ (พิกเซล): ซ้าย/บน/กว้าง/สูง
        private const float DialogueArtBodyTop = 479f;
        private const float DialogueArtBodyWidth = 1452f;
        private const float DialogueArtBodyHeight = 430f;
        private const float DialogueArtNameCenterX = 442f;          // กึ่งกลางส่วนที่โล่งของแท็บชื่อ (ไม่นับลายหนวดทางขวาของแท็บ)
        private const float DialogueArtNameCenterY = 391f;
        private const float DialogueArtNameMaxWidth = 440f;
        private const int DialogueBoxBottomMargin = 90;             // ระยะจากขอบล่างจอถึงขอบล่างของกล่องใหญ่ (เผื่อลายหนวดที่ห้อยลงมา)
        private const int DialogueBoxPaddingX = 36;
        private const int DialogueBoxPaddingY = 26;
        private const float DialogueTextScale = 1.2f;
        private const float DialogueCharsPerSecond = 35f; // ความเร็วพิมพ์ทีละตัว

        // ประแจตกลงมา
        private const float IntroWrenchGravity = 2400f;
        private const float IntroWrenchSpinSpeed = 9f;          // radian/วินาที
        private const float IntroWrenchHitHeightRatio = 0.85f;  // จุดที่ประแจกระทบตัว Kai (สัดส่วนความสูงจากเท้า)
        private const float IntroFadeInDuration = 1.5f;         // จอค่อยๆ สว่างหลังบทพูดบนจอดำ

        private const string ImpactSoundFile = "s_hit_impact.wav";   // เสียงประแจกระทบตัว Kai
        private const string ItemLandSoundFile = "s_item_land.wav";  // เสียงประแจตกถึงพื้น (เล่นตอนจอเริ่มสว่าง)
        private const float ImpactSoundVolume = 1f;

        // เสียงโจมตีของ Kai (ไฟล์อยู่ในโฟลเดอร์ Sounds): ฟาด/ขว้างอาวุธ และประแจตีโดนเป้าหมาย
        private const string SwingSoundFile = "s_swing.wav";            // เสียงฟาดอาวุธ (ประแจ/ดาบ/ขว้างตรีศูล) ตอนไม่โดนเป้าหมาย
        private const string WrenchHitSoundFile = "s_wrench_hit.wav";   // เสียงประแจตีโดนเป้าหมาย (เล่นแทนเสียงฟาด)
        private const float SwingSoundVolume = 0.7f;
        private const float WrenchHitSoundVolume = 0.9f;
        private SoundEffect _swingSoundEffect;
        private SoundEffectInstance _swingSoundInstance;   // เก็บเป็น instance เพื่อหยุดเสียงฟาดได้ถ้าประแจตีโดนต่อมาในช่วงเดียวกัน
        private SoundEffect _wrenchHitSoundEffect;
        private bool _attackWhiffSoundPlayed;              // ประแจ: เล่นเสียงฟาดของสวิงนี้ไปแล้วหรือยัง

        // เสียงกินของ และเสียงร้องมอนสเตอร์ lv1 (Monster1) ไฟล์อยู่ในโฟลเดอร์ Sounds
        private const string EatSoundFile = "s_eat.wav";                    // กินของกินปกติ (สาหร่าย)
        private const string PotionSoundFile = "s_eat_potion.wav";          // กินยา (ยา HP / น้ำยากระโดด)
        private const string Monster1RoarSoundFile = "s_monster1_roar.wav"; // Monster1 เจอ Kai แล้วเริ่มไล่
        private const float EatSoundVolume = 1f;
        private const float PotionSoundVolume = 1f;
        private const float Monster1RoarSoundVolume = 0.8f;
        private const long Monster1RoarMinIntervalMs = 1000;   // มอนเจอพร้อมกันหลายตัว: เล่นเสียงร้องได้ไม่ถี่กว่านี้
        private SoundEffect _eatSoundEffect;
        private SoundEffect _potionSoundEffect;
        private SoundEffect _monster1RoarSoundEffect;
        private long _monster1RoarNextAllowedTick;

        // เสียงมอนสเตอร์ตาย / เสียงร้องมอน lv2 (Monster2) / เสียง Kai โดนตี ไฟล์อยู่ในโฟลเดอร์ Sounds
        private const string MonsterDeathSoundFile = "s_monster_die.wav";     // ศัตรูทุกตัวที่ตายได้ (Golem, Monster1/2, บอส 1/2) เล่นตอนเริ่มท่าตาย
        private const string Monster2RoarSoundFile = "s_monster2_roar.wav";   // Monster2 เริ่มไล่ Kai ครั้งแรก
        private const string KaiHurtSoundFile = "s_kai_hurt.wav";             // Kai เสียเลือดจากศัตรูทุกชนิด (และคำสั่ง /hit_me)
        private const float MonsterDeathSoundVolume = 0.9f;
        private const float Monster2RoarSoundVolume = 0.8f;
        private const float KaiHurtSoundVolume = 0.9f;
        private SoundEffect _monsterDeathSoundEffect;
        private SoundEffect _monster2RoarSoundEffect;
        private SoundEffect _kaiHurtSoundEffect;
        private long _monster2RoarNextAllowedTick;

        // เสียงเหวี่ยงดาบ (Kai ใช้ดาบ / บอส 2 โจมตีระยะประชิด) เสียงเปิดประตู และเสียงลูกบอลไฟของบอส 1 ไฟล์อยู่ในโฟลเดอร์ Sounds
        private const string SwordSwingSoundFile = "s_sword_swing.wav";   // ใช้ทั้งตอนโดนและไม่โดน (ดาบไม่มีเสียงตีโดนแยก)
        private const string DoorOpenSoundFile = "s_door_open.wav";       // เล่นตอนประตู "ถูกเปิด" ครั้งแรก (Orb / กุญแจ / รหัสผ่านถูก) ถ้าไม่มีไฟล์จะใช้เสียงเก็บของเดิม
        private const string BossOrbSoundFile = "s_boss_orb.wav";         // บอส 1 ยิงลูกบอล และตอน Kai แพรี่สะท้อนกลับ
        private const float SwordSwingSoundVolume = 0.9f;
        private const float DoorOpenSoundVolume = 0.8f;
        private const float BossOrbSoundVolume = 0.8f;
        private SoundEffect _swordSwingSoundEffect;
        private SoundEffect _doorOpenSoundEffect;
        private SoundEffect _bossOrbSoundEffect;

        // --- เพลงพื้นหลังด่าน 2 (ผูกกับแถบ Music ในหน้าตั้งค่า) ไฟล์อยู่ในโฟลเดอร์ Sounds ---
        // เพลงแรก: เล่นวนตั้งแต่เข้าห้องแรกของด่าน 2 จนถึงตอนเจอบอส 2 / เพลงบอส: เล่นวนตั้งแต่ฉากเจอบอส 2 เริ่ม จนบอสตาย
        // บอสตายแล้วเงียบ (ยังไม่มีเพลงด่าน 3) / Kai ตายหรือออกจากด่าน 2 เพลงเฟดหาย / เปิดเมนู Esc เพลงหยุดชั่วคราวและเล่นต่อตอนปิดเมนู
        private enum MusicTrack { None, Stage2, Boss2, Stage3 }
        private const string Stage2MusicFile = "m_level2.wav";
        private const string Boss2MusicFile = "m_level2_boss.wav";
        private const string Stage3MusicFile = "m_level3.wav";   // เพลงด่าน 3: เล่นวนตั้งแต่เข้าห้องด่าน 3 (WAV 16 บิต PCM ใส่โฟลเดอร์ Sounds)
        private const float Stage2MusicVolume = 0.8f;
        private const float Boss2MusicVolume = 0.8f;
        private const float Stage3MusicVolume = 0.8f;
        private const float MusicFadeSeconds = 1.5f;   // เวลาเฟดเข้า/ออก/สลับเพลง
        private SoundEffect _stage2MusicEffect;
        private SoundEffect _boss2MusicEffect;
        private SoundEffect _stage3MusicEffect;
        private SoundEffectInstance _stage2MusicInstance;
        private SoundEffectInstance _boss2MusicInstance;
        private SoundEffectInstance _stage3MusicInstance;
        private float _stage2MusicLevel;   // 0-1 ระดับเฟดปัจจุบันของแต่ละเพลง
        private float _boss2MusicLevel;
        private float _stage3MusicLevel;
        private bool _musicPausedByMenu;
        private const float ItemLandSoundVolume = 0.8f;
        private SoundEffect _impactSoundEffect;
        private SoundEffect _itemLandSoundEffect;

        private bool _isDialogueActive;
        private string[] _dialogueLines;
        private string _dialogueDefaultSpeaker = "";   // ผู้พูดเริ่มต้นของบทพูดชุดนี้ (แต่ละหน้าเปลี่ยนได้ด้วย "ชื่อ|ข้อความ")
        private string _dialogueSpeaker = "";           // ชื่อที่แสดงบนแท็บของหน้าข้อความปัจจุบัน
        private Texture2D _dialogueBoxTexture;

        // --- สมุด (กด I เปิด/ปิด) ---
        private Texture2D _bookTexture;
        private SoundEffect _bookOpenSoundEffect;
        private bool _isBookOpen;                         // สมุดเปิดอยู่ (โลกเกมหยุดนิ่งทั้งหมดเหมือนเมนู Esc)
        private KeyboardState _previousBookKeyboardState;
        private const float BookUIScale = 0.8f;           // ย่อรูป book_open.png (1000x1000)
        private const float BookArtCenterX = 502f;        // กึ่งกลางตัวสมุดในรูปต้นฉบับ (พิกเซล) ใช้จัดให้อยู่กลางจอ
        private const float BookArtCenterY = 553f;
        private int _dialogueLineIndex;
        private string _dialogueWrappedText = "";
        private float _dialogueRevealedChars;
        private float _dialogueBlinkTimer;

        private float _introStageTimer;
        private bool _introWrenchVisible;
        private float _introWrenchX;
        private float _introWrenchY;
        private float _introWrenchVelocityY;
        private float _introWrenchRotation;

        private float _cameraZoom = CameraZoom; // ซูมกล้องจริงที่ใช้อยู่ (ปกติ = CameraZoom, ฉากตื่นนอนซูมมากกว่านี้)

        // =====================================================================
        // Room 3: ฉาก Orb (เก็บ orb กลางห้อง -> บทพูด 1 -> Monster1 เดินเข้ามาจากขอบขวา + กล้องซูมไปที่มอน
        //         -> กล้องกลับมาหา Kai -> บทพูด 2 -> ผู้เล่นควบคุมได้ Monster1 ไล่โจมตีตามปกติ)
        // =====================================================================
        private const int OrbSceneRoomIndex = 2;                 // ห้อง 3 (นับจาก 0)
        private const float OrbSceneMonsterWalkSpeed = 260f;     // ความเร็วมอนตอนเดินเข้ามาในฉาก (ปกติ Monster1WalkSpeed = 120 ช้าเกินไปเพราะห้องกว้าง)
        private const float OrbSceneMonsterHoldDistance = 450f;  // มอนหยุดรอห่างจาก Kai เท่านี้จนบทพูด 2 จบ (ยังอยู่ขอบจอ)
        private const float OrbSceneCameraPanDuration = 1.2f;    // เวลากล้องเลื่อนไปหามอน / กลับมาหา Kai
        private const float OrbSceneCameraHoldDuration = 2.5f;   // เวลาที่กล้องค้างอยู่ที่มอน
        private const float OrbSceneMonsterZoom = 2.2f;          // ซูมกล้องตอนโฟกัสมอน (ปกติ CameraZoom = 1.5)

        // ตัว orb บนพื้น (ลอยเหนือพื้นเล็กน้อยและโยกขึ้นลงเบาๆ)
        private const float OrbWorldHeight = 56f;
        private const float OrbHoverHeight = 14f;
        private const float OrbBobAmplitude = 5f;
        private const float OrbBobSpeed = 2.2f;

        // ข้อความบทพูด (แก้ตรงนี้ได้เลย): 1 สมาชิก = 1 หน้าข้อความ, ใช้ "\n" ขึ้นบรรทัดใหม่ได้
        // หมายเหตุ: ตัวอักษรที่ไม่มีใน HotbarFont.spritefont จะแสดงเป็น '?' (ภาษาไทยต้องเพิ่มช่วงตัวอักษรไทยใน spritefont)
        private static readonly string[] OrbDialogue1Lines =
        {
            "[Orb dialogue 1 - line 1: replace this text]",
            "[Orb dialogue 1 - line 2: replace this text]"
        };

        private static readonly string[] OrbDialogue2Lines =
        {
            "[Orb dialogue 2 - line 1: replace this text]",
            "[Orb dialogue 2 - line 2: replace this text]"
        };

        private enum OrbSceneStage { Dialogue1, CameraToMonster, CameraHold, CameraBack, Dialogue2, MonsterApproach }
        private bool _isOrbScene;
        private bool _orbSceneDone;          // เล่นฉากนี้ครั้งเดียว (ดรอปแล้วเก็บ orb ใหม่จะไม่เล่นซ้ำ)
        private OrbSceneStage _orbStage;
        private float _orbStageTimer;
        private float _orbCameraBlend;       // 0 = กล้องอยู่ที่ Kai, 1 = กล้องอยู่ที่มอน
        private Monster1 _orbMonster;
        private float _orbBobTime;

        // --- มอนเกิดเป็นรอบ (wave): เริ่มเมื่อผู้เล่นฆ่า Monster1 ตัวแรกของฉาก orb ---
        // สุ่มเกิดในห้อง 2 กับห้อง 3 (ทุกรอบมีมอนอย่างน้อย 1 ตัวในแต่ละห้อง) มอนที่อยู่คนละห้องจะนิ่งอยู่จนกว่าผู้เล่นจะเข้าห้องนั้น
        // ตัวจำกัดความยาก: (1) จำนวนมอนที่ยังมีชีวิตพร้อมกันไม่เกิน MonsterWaveMaxAlive (2) จำนวนรวมทั้งหมดไม่เกิน MonsterWaveTotalLimit แล้วหยุดเกิด
        private static readonly int[] MonsterWaveSizes = { 2, 3, 3 };     // จำนวนมอนต่อรอบ (ทีละรอบ ต้องฆ่ารอบก่อนหน้าให้หมดก่อน)
        private const int MonsterWaveTotalLimit = 8;                     // ให้เท่ากับผลรวมของ MonsterWaveSizes
        private const int MonsterWaveMaxAlive = 3;                       // มอนที่อยู่พร้อมกันสูงสุด
        private const float MonsterWaveDelay = 4f;                       // เว้นกี่วินาทีหลังฆ่ารอบก่อนหมด ถึงเกิดรอบถัดไป
        private static readonly int[] MonsterWaveRooms = { 1, 2 };       // ห้อง 2 และห้อง 3 (นับจาก 0)
        private const float MonsterWaveSpawnMargin = 1000f;              // ไม่เกิดใกล้ขอบห้องเกินไป (ผู้เล่นเข้าห้องมาจะไม่โดนมอนเข้าประชิดทันที)
        private const float MonsterWaveMinDistanceFromKai = 800f;        // ห้องที่ผู้เล่นอยู่: ไม่เกิดใกล้ Kai กว่านี้
        private const float MonsterWaveMinSpacing = 250f;                // มอนในรอบเดียวกันห้องเดียวกัน ห่างกันอย่างน้อยเท่านี้

        private Monster1 _orbFirstMonster;   // มอนตัวแรกของฉาก orb (ฆ่าแล้วเริ่มรอบแรก)
        private bool _wavesStarted;
        private bool _wavesFinished;
        private int _waveIndex;              // รอบถัดไปที่จะเกิด
        private int _waveTotalSpawned;
        private float _waveDelayTimer;
        private readonly List<Monster1> _waveMonsters = new List<Monster1>();

        // =====================================================================
        // Door (ประตูในห้อง 1 ต้องมี Orb ถึงเปิดได้ เปิดแล้วกด E อีกครั้งเพื่อเข้าห้องลับ)
        // =====================================================================
        private const int DoorRoomIndex = 0;                 // ห้อง 1 (นับจาก 0)
        private const float DoorX = 4550f;                   // กึ่งกลางประตู: อยู่ช่องว่างระหว่าง platform (จบที่ 4400) กับจุดเปลี่ยนห้องฝั่งขวา (~4700)
        private const float DoorWidth = 90f;
        private const float DoorHeightToKaiRatio = 1.4f;
        private const float DoorInteractRange = 120f;
        private const int SecretRoomIndex = 3;               // ห้องหลังประตู (ห้องที่ 4) ใช้ระบบห้องเดียวกับห้อง 1-3 แต่ไม่อยู่ในเส้นทางซ้าย-ขวาปกติ
        private const float SecretRoomSpawnX = 400f;

        // ประตูทางออกห้องบอส: โผล่หลังท่าตายของบอสเล่นจบ ต้องใช้กุญแจที่บอสดรอปเพื่อเปิด (หักกุญแจ 1 ดอก) แล้วไปห้องถัดไป
        private const int NextLevelRoomIndex = 4;                // ห้องที่ 5 = ห้องแรกของด่าน 2 (ห้องถัดไปหลังบอส 1)

        // ด่าน 2: 3 ห้องต่อกันซ้าย-ขวาเหมือนด่าน 1 (ห้อง index 4, 5, 6) เดินชนขอบขวา/ซ้ายเพื่อเปลี่ยนห้อง
        // ห้องแรก (index 4) เดินชนขอบซ้าย = กลับออกทางประตูห้องบอส / ห้องสุดท้าย (index 6) ขอบขวาเป็นกำแพง (จุดสิ้นสุดชั่วคราว)
        // ทุกห้องใช้พื้นหลัง 9 ชั้น + พื้นของด่าน 2 และตอนนี้ยังเป็นห้องเปล่า (ยังไม่มีแผ่นสูง/มอน)
        private const int Level2RoomCount = 3;
        private const int Level2LastRoomIndex = NextLevelRoomIndex + Level2RoomCount - 1;

        private static bool IsLevel2Room(int room)
        {
            return room >= NextLevelRoomIndex && room <= Level2LastRoomIndex;
        }

        // ด่าน 3: ห้องเดียว (index ถัดจากห้องสุดท้ายของด่าน 2) เข้าได้หลังกำจัดบอส 2 แล้วเดินชนขอบขวาของห้องบอส ขอบซ้ายเป็นกำแพง ถอยกลับไม่ได้
        private const int Level3RoomCount = 1;
        private const int Level3RoomIndex = Level2LastRoomIndex + 1;

        private static bool IsLevel3Room(int room)
        {
            return room == Level3RoomIndex;
        }

        private bool _boss2Defeated;   // กำจัดบอส 2 (ตัวในฉากห้องที่ 3 ของด่าน 2) แล้ว: เปิดทางขวาไปด่าน 3

        // ท่าตายบอส 2 เล่นจบ (บอสถูกลบออกจากรายการแล้ว) = ลูกศรขึ้น และเดินชนขอบขวาของห้องบอสเพื่อไปด่าน 3 ได้
        private bool CanGoToLevel3()
        {
            return _boss2Defeated
                && _currentRoomIndex == Level2LastRoomIndex
                && (_boss2SceneBoss == null || !_boss2List.Contains(_boss2SceneBoss));
        }

        // =====================================================================
        // ด่าน 2: หินตัวเลข 0-9 (ห้องแรกและห้องที่สองของด่าน 2) + ป้ายใบ้ + ประตูรหัส 4 หลักก่อนห้องที่ 3
        //  - หินครบ 10 ก้อน (stone_spritesheet 5x2 = เลข 0-9) สุ่มตำแหน่งครั้งเดียวต่อเกมใหม่ แล้วคงที่จนจบเกม (ตายแล้วเกิดใหม่ยังที่เดิม)
        //  - เลขบนหินไม่ซ้ำ ห้องละ 5 ก้อน นับก้อนที่ 1-10 จากซ้ายสุดของห้องแรกไปขวา แล้วต่อห้องที่สอง
        //  - รหัส 4 หลักสุ่มใหม่ทุกเกมใหม่ จากเลขบนหิน 4 ก้อน ป้ายในห้องแรก (กด E) บอกว่าให้อ่านก้อนที่เท่าไรบ้างตามลำดับ
        //  - ประตูรหัสที่ขอบขวาห้องที่สอง: กด E พิมพ์เลข 4 หลัก Enter ยืนยัน ถูก = ประตูเปิด เดินไปห้องที่ 3 ได้ / ผิด = ลองใหม่ ไม่มีโทษ
        //  ต้องเพิ่ม stone_spritesheet.png (asset "stone_spritesheet") เข้า Content.mgcb ถ้ายังไม่เพิ่มจะไม่มีหินให้เห็นและประตูจะล็อกตลอด
        // =====================================================================
        private const int StoneCount = 10;
        private const int StoneSheetColumns = 5;
        private const int StoneSheetRows = 2;
        private const int StoneRoomA = NextLevelRoomIndex;            // ห้องแรกของด่าน 2
        private const int StoneRoomB = NextLevelRoomIndex + 1;        // ห้องที่สองของด่าน 2 (มีประตูรหัสที่ขอบขวา)
        private const int StonesPerRoom = StoneCount / 2;
        private const float StoneTargetHeight = 95f;                  // ความสูงหินเฉลี่ยบนจอ (px)
        private const float StoneMinX = 1100f;
        private const float StoneEdgeMarginRight = 900f;              // เว้นขอบขวาของห้อง (ที่อยู่ของประตูรหัสห้องที่สอง)
        private const float StoneMinSpacing = 420f;
        private const float StoneSignX = 700f;                        // ป้ายใบ้ในห้องแรก
        private const float StoneDoorOffsetFromRight = 420f;
        private const float StoneDoorBlockMargin = 70f;               // ประตูล็อก: ผู้เล่นเดินชิดประตูได้ถึงระยะนี้
        private const float Stage2Room3EntryX = 400f;                 // ตำแหน่งที่ยืนในห้องที่ 3 ของด่าน 2 หลังเข้าทางประตูรหัส
        private const float StoneDoorReturnOffset = 120f;             // ย้อนกลับจากห้องที่ 3: ยืนทางซ้ายของประตูห่างเท่านี้
        private const int PasscodeLength = 4;
        private const float PasscodeMessageDuration = 2.2f;

        private sealed class CodeStone
        {
            public int Digit;
            public int Room;
            public float X;
            public int Order;   // ก้อนที่ 1-10 จากซ้ายสุดห้องแรกไปขวา ต่อห้องที่สอง
        }

        private AnimFrame[] _stoneFrames;
        private float _stoneScale;
        private readonly List<CodeStone> _codeStones = new List<CodeStone>();
        private string _stoneCode = "";
        private string[] _stoneHintLines;
        private bool _passcodeDoorOpen;
        private bool _isPasscodeOpen;
        private bool _isSignReading;
        private string _passcodeInput = "";
        private string _passcodeMessage = "";
        private float _passcodeMessageTimer;
        private bool _passcodeMessageIsError;
        private const float NextLevelRoomSpawnX = 400f;
        private const float BossExitDoorX = 4900f;               // ใกล้ขอบขวาของลานบอส (ห้องกว้าง 5120)

        private static readonly string[] BossExitDoorLockedLines =
        {
            "[Boss exit door locked - line 1: replace this text]",
            "[Boss exit door locked - line 2: replace this text]"
        };

        private static readonly string[] BossExitDoorHasKeyLines =
        {
            "[Boss exit door with Key - line 1: replace this text]",
            "[Boss exit door with Key - line 2: replace this text]"
        };

        private const string BossExitDoorConfirmPrompt = "Use the Key to open the door?";

        // --- จุดเซฟ (โครงกระดูกในห้องบอส): กด E ใกล้โครงกระดูกเพื่อเซฟ ตายแล้วไปเกิดที่นี่แทนจุดเริ่มเกม (บอสคงสถานะเดิม ไม่รีเซ็ต) ---
        // ต้องเพิ่มรูป check.png เข้า Content Pipeline (Content.mgcb) ด้วยชื่อ asset "check"
        private const int SavePointRoomIndex = BossSceneRoomIndex;
        private const float SavePointX = 900f;                 // ฝั่งซ้ายของห้องบอส ใกล้จุดที่เข้าห้อง
        private const float SavePointRespawnOffsetX = 120f;    // ตอนเกิดใหม่ยืนข้างโครงกระดูก (ไม่ทับตัวมัน)
        private const float SavePointDrawHeight = 170f;        // ความสูงของตัวโครงกระดูกที่วาด (px)
        private static readonly Rectangle SavePointSourceRect = new Rectangle(2, 222, 998, 778); // ส่วนที่มีภาพใน check.png (ตัดขอบโปร่งใส)
        private Texture2D _savePointTexture;

        // ดาบที่วางทาบบนโครงกระดูกจุดเซฟ (หยิบได้ครั้งเดียวด้วย E) ปรับตำแหน่ง/มุม/ขนาดได้ที่ค่าด้านล่าง
        private bool _savePointSwordTaken;
        private const float SavePointSwordLengthRatio = 0.85f;        // ความยาวดาบ เทียบความสูงโครงกระดูก
        private const float SavePointSwordOffsetX = 18f;              // ขยับจากกึ่งกลางโครงกระดูก (บวก = ไปทางขวา)
        private const float SavePointSwordOffsetYRatio = 0.55f;       // ความสูงจุดกึ่งกลางดาบจากพื้น เทียบความสูงโครงกระดูก
        private const float SavePointSwordTipAngleDegrees = -70f;     // ทิศปลายดาบ (0 = ขวา, ลบ = ชี้ขึ้น)
        private int _checkpointRoomIndex = -1;                 // -1 = ยังไม่เคยเซฟ -> เกิดที่จุดเริ่มเกม
        private float _checkpointX;

        private bool _bossExitDoorVisible;   // โผล่แล้วหลังบอสตาย
        private bool _bossExitDoorOpened;    // ไขด้วยกุญแจแล้ว
        private bool _doorIsBossExit;        // บทพูด/หน้ายืนยันที่กำลังเล่นอยู่เป็นของประตูห้องบอส (ไม่ใช่ประตู Orb ห้อง 1)

        // ข้อความบทพูด (แก้ตรงนี้ได้เลย) ใช้กรอบเดียวกับฉากตื่นนอน/ฉาก orb: คลิกซ้ายเพื่อไปหน้าถัดไป
        // หมายเหตุ: ตัวอักษรที่ไม่มีใน HotbarFont.spritefont จะแสดงเป็น '?' (ภาษาไทยต้องเพิ่มช่วงตัวอักษรไทยใน spritefont)
        private static readonly string[] DoorLockedLines =
        {
            "Door locked"
        };

        private static readonly string[] DoorHasOrbLines =
        {
            "Door with key"
        };

        private const string DoorConfirmPrompt = "Use the Key to open the door?";

        private enum DoorStage { Dialogue, Confirm }
        private bool _doorOpened;
        private bool _isDoorInteraction;
        private DoorStage _doorStage;
        private bool _doorDialogueLeadsToConfirm;

        // เปลี่ยนห้องผ่านประตู (ไม่ใช่การเดินชนขอบซ้าย/ขวา): ระบุห้องและตำแหน่งปลายทางตรงๆ
        private int _transitionTargetRoom = -1;
        private float _transitionTargetX;
        private bool _transitionTargetFacingRight;

        // =====================================================================
        // ห้องลับ: ฉากบอส (บอสยืนนิ่งเหมือนรูปปั้น -> กด E -> บทพูด 1 -> กล้องซูมไปที่บอส -> บอสตัวสั่น
        //         -> บทพูด 2 ของ Kai -> กล้องซูมออกกว้างตอนต่อสู้ -> บอสตายแล้วกล้องกลับมาซูมปกติ)
        // ใช้ Boss1 ตัวเดิม (สไปรท์/AI/2 phase เดิม) แค่ตั้ง IsStatue = true ให้ AI ไม่ทำงานจนกว่าฉากจะจบ
        // =====================================================================
        private const int BossSceneRoomIndex = SecretRoomIndex;
        private const float BossStatueX = 4550f;                 // ตำแหน่งบอส (ห้องกว้าง 5120) ฝั่งขวาของห้อง ไม่ชิดขอบ
        private const float BossSceneInteractRange = 320f;       // กด E ได้เมื่ออยู่ห่างจากบอสไม่เกินนี้ (บอสตัวใหญ่ จึงกว้างกว่าประตู)
        private const float BossSceneBossZoom = 1.7f;            // ซูมกล้องตอนโฟกัสบอส (ปกติ CameraZoom = 1.5)
        private const float BossSceneCameraPanDuration = 1.2f;   // เวลากล้องเลื่อน/ซูมไปหาบอส
        private const float BossSceneShakeDuration = 1.8f;       // บอสสั่นก่อนบทพูด 2 กี่วินาที (สั่นต่อเนื่องระหว่างบทพูด 2 ด้วย)
        private const float BossSceneShakeAmplitude = 3f;        // ความแรงที่สั่น (พิกเซลโลก) "สั่นเล็กน้อย"
        private const float BossSceneShakeFrequency = 55f;       // ความถี่การสั่น (radian/วินาที)
        private const float BossSceneZoomOutDuration = 1.5f;     // เวลากล้องซูมออก + กลับมาหา Kai หลังบทพูด 2
        private const float BossFightZoom = 1.0f;                // ซูมกล้องตอนสู้บอส (1.0 = เห็นความสูงเต็มห้อง ห้ามต่ำกว่านี้ ไม่งั้นเห็นนอกฉาก)
        private const float BossFightZoomBlendDuration = 1.5f;   // เวลาซูมสลับระหว่างซูมสู้บอส <-> ซูมปกติ (เช่น ตอนบอสตาย/ออกจากห้อง)

        // ข้อความบทพูด (แก้ตรงนี้ได้เลย) ใช้กรอบเดียวกับฉากอื่น: คลิกซ้ายเพื่อไปหน้าถัดไป
        // หมายเหตุ: ตัวอักษรที่ไม่มีใน HotbarFont.spritefont จะแสดงเป็น '?' (ภาษาไทยต้องเพิ่มช่วงตัวอักษรไทยใน spritefont)
        private static readonly string[] BossDialogue1Lines =
        {
            "...",
            "..."
        };

        private static readonly string[] BossDialogue2Lines =
        {
            "...",
            "..."
        };

        private enum BossSceneStage { Dialogue1, CameraToBoss, Shake, Dialogue2, ZoomOut }
        private bool _isBossScene;
        private bool _bossSceneDone;          // เล่นฉากนี้ครั้งเดียว
        private BossSceneStage _bossStage;
        private float _bossStageTimer;
        private float _bossFocusBlend;        // 0 = กล้องอยู่ที่ Kai, 1 = กล้องอยู่ที่บอส
        private float _bossSceneZoom = CameraZoom;
        private bool _bossShakeActive;
        private float _bossShakeTime;
        private Boss1 _bossSceneBoss;
        private bool _bossFightActive;        // ต่อสู้อยู่ (บอสยังไม่ตาย) -> กล้องซูมออกเมื่ออยู่ในห้องบอส
        private float _bossFightZoomBlend;    // 0 = ซูมปกติ, 1 = ซูมสู้บอสเต็มที่

        // =====================================================================
        // ด่าน 2 ห้องที่ 3: ฉากเจอ Boss 2 (เข้าห้อง -> บทพูด 1 -> กล้องซูมไปที่บอส -> บอสตัวสั่น -> บทพูด 2 ของ Kai -> กล้องซูมออกกว้างตอนสู้)
        //  ใช้ขั้นตอนและค่าเวลาเดียวกับฉากบอส 1 (BossScene*) แต่เริ่มเองเมื่อเข้าห้องครั้งแรก (ไม่ต้องกด E)
        //  บอสเกิดฝั่งขวาของห้อง ระหว่างสู้ขอบซ้ายของห้องเป็นกำแพง (หนีไม่ได้) จนกว่าบอสจะตาย แล้วดรอปยา HP
        //  ผู้เล่นตายระหว่างสู้: บอสถูกลบ ฉากเล่นใหม่เมื่อกลับมาเข้าห้อง (บอสเลือดเต็ม) / บอสตายแล้วไม่เกิดซ้ำ
        // =====================================================================
        private const float Boss2SceneBossX = 4400f;
        private const int Boss2PotionDropMin = 1;
        private const int Boss2PotionDropMax = 2;

        // ข้อความบทพูดชั่วคราว (แก้ตรงนี้ได้เลย) ใช้กรอบเดียวกับฉากอื่น: คลิกซ้ายเพื่อไปหน้าถัดไป
        private static readonly string[] Boss2Dialogue1Lines =
        {
            "...",
            "..."
        };

        private static readonly string[] Boss2Dialogue2Lines =
        {
            "B...B...",
            "Brother..."
        };

        private bool _isBoss2Scene;
        private bool _boss2SceneDone;          // เล่นฉากนี้ครั้งเดียว (ตายระหว่างสู้ = รีเซ็ตให้เล่นใหม่)
        private BossSceneStage _boss2Stage;
        private float _boss2StageTimer;
        private float _boss2FocusBlend;        // 0 = กล้องอยู่ที่ Kai, 1 = กล้องอยู่ที่บอส
        private float _boss2SceneZoom = CameraZoom;
        private bool _boss2ShakeActive;
        private float _boss2ShakeTime;
        private Boss2 _boss2SceneBoss;
        private float _boss2FightZoomBlend;    // 0 = ซูมปกติ, 1 = ซูมสู้บอสเต็มที่

        // สถานะบอส 2 ที่จดไว้ตอน Kai ตายระหว่างสู้: เกิดใหม่แล้วกลับมาเข้าห้อง บอสจะเลือดเท่าเดิม/ตำแหน่งเดิม/เฟสเดิม และข้ามฉากเปิดตัว
        private bool _boss2HasSavedState;
        private float _boss2SavedHP;
        private float _boss2SavedX;
        private bool _boss2SavedFacingRight;
        private bool _boss2SavedIsPhase2;

        // Boss 3 (ด่าน 3): HP 550, 2 เฟส (ตอนนี้ทำแค่ยืนนิ่ง idle: ตีไม่โดน ไม่ขยับ ไม่โจมตี) วางไว้ขอบขวาของห้องด่าน 3
        private const string Boss3IdleAsset = "boss_level3_walk";
        private const string Boss3AttackP1Asset = "boss_level3_attack_ph1";
        private const string Boss3AttackP2Asset = "boss_level3_attack_ph2";
        private const string Boss3DieAsset = "boss_level3_die";
        private const int Boss3FrameCount = 8;
        private const int Boss3SheetColumns = 4;
        private const int Boss3SheetRows = 2;
        private const float Boss3FrameDuration = 0.12f;
        private const float Boss3HeightToKaiRatio = 2.4f;
        private const float Boss3MaxHP = 550f;
        private const float Boss3Phase2HpRatio = 0.5f;
        private const int Boss3DamageMin = 20;
        private const int Boss3DamageMax = 30;
        private const float Boss3EdgeOffsetFromRight = 400f;
        private const float Level3SpawnX = 400f;
        private const int Boss3SummonMaxCount = 2;
        private const float Boss3SpawnDistance = 500f;
        private const float Boss3SpawnSpacing = 500f;
        private const bool Boss3SpritesFaceRight = false;   // ชีทต้นฉบับหันซ้าย

        private const string Boss3DisplayName = "Aletheia";
        private const string Boss3GhostAsset = "Ghost";   // รูปเงาผี (PNG โปร่งใส)
        private const float Boss3MoveSpeed = 110f;
        private const float Boss3Phase2SpeedMultiplier = 1.25f;
        private const float Boss3HitboxHalfWidthCellRatio = 0.16f;    // ครึ่งกว้างลำตัว เทียบความกว้างเซลล์
        private const float Boss3AttackFrameDuration = 0.11f;
        private const int Boss3SwipeHitFrameStart = 3;
        private const int Boss3SwipeHitFrameEnd = 5;
        private const int Boss3LaserHitFrameStart = 3;
        private const int Boss3LaserHitFrameEnd = 4;
        private const float Boss3SwipeReachCellRatio = 0.5f;          // ระยะฟาดหนวด (ครึ่งเซลล์ด้านหน้าจากกลางตัว)
        private const float Boss3SwipeCooldown = 1.6f;
        private const float Boss3LaserCooldown = 2.4f;
        private const float Boss3LaserRange = 720f;
        private const float Boss3LaserEyeHeightRatio = 0.13f;         // ตาอยู่ต่ำจากหัวกี่ส่วนของความสูงตัว
        private const float Boss3LaserThickness = 14f;
        private const float Boss3HitFlashDuration = 0.12f;
        private const float Boss3DieFrameDuration = 0.15f;

        // ฉากผีโผล่ (ด่าน 3): เดินครบ 3-5 วินาที -> ผีลอยขึ้นหน้า Kai -> บทพูด -> บอสดูดผีเข้าตัว -> สู้
        private const float Boss3GhostWaitMin = 3f;
        private const float Boss3GhostWaitMax = 5f;
        private const float Boss3GhostSpawnDistance = 280f;
        private const float Boss3GhostHeightToKaiRatio = 1.15f;
        private const float Boss3GhostRiseDuration = 2.2f;
        private const float Boss3GhostFlyDuration = 2.6f;
        private const float Boss3AbsorbDuration = 1.8f;
        private const float Boss3SceneCameraPanDuration = 1.0f;
        private const float Boss3LetterboxHeight = 70f;
        private const float Boss3LetterboxSpeed = 2.5f;
        private const float Boss3CameraShakeAmplitude = 6f;

        private static readonly string[] Boss3GhostDialogueLines =
        {
            "Lost Soul|Wait... you can see me? It has been so long since anyone could.",
            "William Jones|Who are you? How did you end up in a place like this?",
            "Lost Soul|I was swallowed by the thing that sleeps at the end of this hall. It feeds on every soul that wanders in.",
            "William Jones|Then I will put an end to it.",
            "Lost Soul|No... it is awake. It is pulling me back... Be ready!"
        };

        private enum Boss3State { Idle, Moving, Attacking }
        private enum Boss3SceneStage { GhostRise, Dialogue, GhostFly, Absorb, ZoomOut }

        private sealed class Boss3
        {
            public float X;
            public int Room;
            public float HP;
            public bool FacingRight;
            public float AnimTimer;
            public int FrameIndex;
            public float YOffset;
            public Boss3State State;
            public bool IsStatue;          // ยืนนิ่ง ตีไม่โดน ไม่โจมตี (ก่อนฉากผีจบ)
            public bool IsSceneBoss;       // บอสตัวในฉากด่าน 3 (ไม่ใช่ตัวที่เสกด้วยคอนโซล)
            public bool IsPhase2;
            public bool IsDying;
            public float DeathTimer;
            public float HitFlashTimer;
            public float AttackCooldownTimer;
            public bool AttackIsLaser;
            public bool AttackHitApplied;
            public Vector2 LaserEnd;       // จุดปลายเลเซอร์ (พิกัดโลก) ล็อกตอนยิง
            public Vector2 LaserStart;
        }

        private bool _isBoss3Scene;
        private bool _boss3SceneDone;
        private bool _boss3Defeated;
        private Boss3SceneStage _boss3Stage;
        private float _boss3StageTimer;
        private float _boss3EncounterTimer;
        private float _boss3EncounterTarget = 4f;
        private float _boss3FocusBlend;
        private Vector2 _boss3FocusPoint;
        private float _boss3SceneZoom = CameraZoom;
        private bool _boss3ShakeActive;
        private float _boss3ShakeTime;
        private float _boss3FightZoomBlend;
        private float _boss3BarBlend;
        private bool _boss3HasSavedState;
        private float _boss3SavedHP;
        private bool _boss3SavedPhase2;
        private Texture2D _boss3GhostTexture;
        private Rectangle _boss3GhostTrim;
        private float _boss3GhostScale;
        private Vector2 _boss3GhostPos;       // ตำแหน่งเท้าผี (พิกัดโลก)
        private float _boss3GhostAlpha;
        private float _boss3GhostShrink = 1f;
        private float _boss3GhostTime;
        private Boss3 _boss3SceneBoss;

        private bool _boss3Loaded;
        private AnimFrame[] _boss3Idle, _boss3AttackP1, _boss3AttackP2, _boss3Die;
        private float _boss3Scale, _boss3Height, _boss3HalfWidth;
        private readonly List<Boss3> _boss3List = new List<Boss3>();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true; // เริ่มที่เมนูหลัก (Update คำนวณใหม่ทุกเฟรมด้วย ShouldShowMouseCursor)

            Window.TextInput += OnTextInput;

            _graphics.PreferredBackBufferWidth = WindowedWidth;
            _graphics.PreferredBackBufferHeight = WindowedHeight;
        }

        protected override void Initialize()
        {
            _graphics.ApplyChanges();

            _floorHeight = (int)(WindowedHeight * 0.13f);
            _floorY = WindowedHeight - _floorHeight;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            _hotbarFont = Content.Load<SpriteFont>("HotbarFont");

            _mainMenuBackground = Content.Load<Texture2D>("main_menu");
            //_menuLogoTexture = Content.Load<Texture2D>("melanie"); // เอา melanie ออกก่อน
            _btnStartTexture = Content.Load<Texture2D>("bt_start");
            _btnSettingTexture = Content.Load<Texture2D>("bt_setting");
            _btnExitTexture = Content.Load<Texture2D>("bt_exit");
            _btnMenuTexture = Content.Load<Texture2D>("bt_menu");
            _hotbarHudTexture = Content.Load<Texture2D>("hot_bar");
            _dialogueBoxTexture = Content.Load<Texture2D>("text_box");
            _bookTexture = Content.Load<Texture2D>("book_open");
            _mainMenuEffect = Content.Load<Effect>("MainMenuEffect");

            _hpBarSheet = Content.Load<Texture2D>("h_bar_sheet");
            _staminaBarSheet = Content.Load<Texture2D>("stamina_bar_sheet");

            _hudKeyTexture = Content.Load<Texture2D>("ui_key");
            _hudBookTexture = Content.Load<Texture2D>("ui_book");
            _hudBackpackTexture = Content.Load<Texture2D>("ui_backpack_1");

            _groundTexture = Content.Load<Texture2D>("ground_t_01");
            _groundSourceRect = new Rectangle(0, 0, _groundTexture.Width, _groundTexture.Height);
            // สเกลตามอัตราส่วนจริงของรูป แทนการครอปเป็นสี่เหลี่ยมจัตุรัส เพื่อให้ก้อนหินเรียงต่อกันเหมือนต้นฉบับ
            _groundTileWidth = (int)System.Math.Round(_groundTexture.Width * ((float)_floorHeight / _groundTexture.Height));

            // พื้นห้องถัดไปหลังบอส: ต้องเพิ่ม ground_t_02.png เข้า Content Pipeline (Content.mgcb) ถ้ายังไม่เพิ่ม เกมจะใช้พื้นเดิมไปก่อนไม่ crash
            try
            {
                _groundTextureNextRoom = Content.Load<Texture2D>("Platform_Lvl2_3");
                _groundTileWidthNextRoom = (int)System.Math.Round(_groundTextureNextRoom.Width * ((float)_floorHeight / _groundTextureNextRoom.Height));
            }
            catch (Microsoft.Xna.Framework.Content.ContentLoadException)
            {
                _groundTextureNextRoom = null;
            }

            // พื้นด่าน 3: ต้องเพิ่ม Platform_Lvl3.png เข้า Content Pipeline (Content.mgcb) ถ้ายังไม่เพิ่ม เกมจะใช้พื้นเดิมไปก่อนไม่ crash
            try
            {
                _groundTextureLevel3 = Content.Load<Texture2D>("Platform_Lvl3");
                _groundTileWidthLevel3 = (int)System.Math.Round(_groundTextureLevel3.Width * ((float)_floorHeight / _groundTextureLevel3.Height));
            }
            catch (Microsoft.Xna.Framework.Content.ContentLoadException)
            {
                _groundTextureLevel3 = null;
            }

            _backgroundLayers = new Texture2D[BackgroundLayerCount];
            for (int i = 0; i < BackgroundLayerCount; i++)
                _backgroundLayers[i] = Content.Load<Texture2D>(BackgroundLayerNames[i]);

            _backgroundLayerScale = (float)WindowedHeight / _backgroundLayers[0].Height;
            _backgroundLayerScaledWidth = _backgroundLayers[0].Width * _backgroundLayerScale;
            _backgroundLayerTransforms = new Matrix[BackgroundLayerCount];

            try
            {
                var nextRoomLayers = new Texture2D[NextRoomBackgroundLayerCount];
                for (int i = 0; i < NextRoomBackgroundLayerCount; i++)
                    nextRoomLayers[i] = Content.Load<Texture2D>(NextRoomBackgroundLayerNames[i]);

                _nextRoomBackgroundLayers = nextRoomLayers;
                _nextRoomBackgroundScale = (float)WindowedHeight / nextRoomLayers[0].Height;
                _nextRoomBackgroundScaledWidth = nextRoomLayers[0].Width * _nextRoomBackgroundScale;
                _nextRoomBackgroundTransforms = new Matrix[NextRoomBackgroundLayerCount];
            }
            catch (Microsoft.Xna.Framework.Content.ContentLoadException)
            {
                _nextRoomBackgroundLayers = null;
                _nextRoomBackgroundTransforms = null;
            }

            try
            {
                var level3Layers = new Texture2D[Level3BackgroundLayerCount];
                for (int i = 0; i < Level3BackgroundLayerCount; i++)
                    level3Layers[i] = Content.Load<Texture2D>(Level3BackgroundLayerNames[i]);

                _level3BackgroundLayers = level3Layers;
                _level3BackgroundScale = (float)WindowedHeight / level3Layers[0].Height;
                _level3BackgroundScaledWidth = level3Layers[0].Width * _level3BackgroundScale;
                _level3BackgroundTransforms = new Matrix[Level3BackgroundLayerCount];
            }
            catch (Microsoft.Xna.Framework.Content.ContentLoadException)
            {
                _level3BackgroundLayers = null;
                _level3BackgroundTransforms = null;
            }

            _kaiIdle = LoadAnimFrames("kai_idle_", IdleFrameCount);
            _kaiRun = LoadAnimFrames("kai_n_run_", WalkFrameCount);
            _kaiJump = LoadJumpFramesFromSpriteSheet("kai_jump_final_spritesheet", JumpFrameCount, JumpSheetColumns, JumpSheetRows);
            _kaiAttack = LoadAttackFramesFromSpriteSheet("kai_attack_spritesheet_1", AttackFrameCount, AttackSheetColumns, AttackSheetRows);

            _wrenchTexture = Content.Load<Texture2D>("wrench");
            _seaweedTexture = Content.Load<Texture2D>("sea_weed");
            _hpPotionTexture = Content.Load<Texture2D>("h_potion");
            try
            {
                _savePointTexture = Content.Load<Texture2D>("check"); // ถ้ายังไม่เพิ่มเข้า Content Pipeline: ไม่ crash แต่รูปโครงกระดูกจะไม่ขึ้น
            }
            catch (Microsoft.Xna.Framework.Content.ContentLoadException)
            {
                _savePointTexture = null;
            }

            try
            {
                _jumpPotionTexture = Content.Load<Texture2D>("jump_potion"); // ถ้ายังไม่เพิ่มเข้า Content Pipeline: ไม่ crash แต่ไอคอนจะไม่ขึ้น
            }
            catch (Microsoft.Xna.Framework.Content.ContentLoadException)
            {
                _jumpPotionTexture = null;
            }
            _orbTexture = Content.Load<Texture2D>("orb"); // ต้องเพิ่ม orb.png เข้า Content Pipeline ด้วยชื่อ asset "orb"

            // ไอคอนนิ่ง (inventory/hotbar/HUD) + สไปรท์ชีท 4x2 สำหรับอนิเมชันตอนวางอยู่บนพื้น
            _keyItemTexture = Content.Load<Texture2D>("key_icon");
            _keyIdleFrames = LoadJumpFramesFromSpriteSheet("key_idle_spritesheet", KeyIdleFrameCount, KeyIdleSheetColumns, KeyIdleSheetRows);

            _kaiTargetHeight = _floorHeight * KaiHeightToFloorRatio;
            _kaiScale = _kaiTargetHeight / _kaiIdle[0].Trim.Height;
            _attackScale = _kaiTargetHeight / _kaiAttack[0].Trim.Height;

            // ท่านอนคว่ำตอนเริ่มเกม (ต้องเพิ่ม kai_sleep.png เข้า Content Pipeline ด้วยชื่อ asset "kai_sleep")
            _kaiSleepTexture = Content.Load<Texture2D>("kai_sleep");
            _kaiSleepTrim = GetOpaqueBounds(_kaiSleepTexture, new Rectangle(0, 0, _kaiSleepTexture.Width, _kaiSleepTexture.Height));
            _kaiSleepScale = (_kaiTargetHeight * SleepPoseWidthToKaiHeightRatio) / _kaiSleepTrim.Width;
            _kaiSleepRotation = -MathHelper.ToRadians(SleepPoseTiltDegrees); // ภาพพลิกให้หัวอยู่ขวา หมุนทวนเข็ม = หัวเงยขึ้น
            _kaiSleepCenterOffsetY = -ComputeSleepPoseLowestPoint();

            _dummyTexture = Content.Load<Texture2D>("dammy_idle");
            _dummyTrim = GetOpaqueBounds(_dummyTexture, new Rectangle(0, 0, _dummyTexture.Width, _dummyTexture.Height));
            _dummyScale = (_kaiTargetHeight * DummyHeightToKaiRatio) / _dummyTrim.Height;

            _golemIdle = LoadJumpFramesFromSpriteSheet("Golem_1_idle", GolemIdleFrameCount, GolemIdleFrameCount, 1);
            _golemWalk = LoadJumpFramesFromSpriteSheet("Golem_1_walk", GolemWalkFrameCount, GolemWalkFrameCount, 1);
            _golemAttack = LoadJumpFramesFromSpriteSheet("Golem_1_attack", GolemAttackFrameCount, GolemAttackFrameCount, 1);
            _golemHurt = LoadJumpFramesFromSpriteSheet("Golem_1_hurt", GolemHurtFrameCount, GolemHurtFrameCount, 1);
            _golemDie = LoadJumpFramesFromSpriteSheet("Golem_1_die", GolemDieFrameCount, GolemDieFrameCount, 1);

            float golemTargetHeight = _kaiTargetHeight * GolemHeightToKaiRatio;
            _golemScale = golemTargetHeight / _golemIdle[0].Trim.Height;
            _golemHalfWidth = _golemIdle[0].Trim.Width * _golemScale / 2f;
            _golemHeight = _golemIdle[0].Trim.Height * _golemScale;

            // ชีทมอนสเตอร์ตำแหน่งเฟรมไม่ตรงกริดเท่ากัน จึงตัดเฟรมตามช่วงพิกเซลจริงแทนการหารช่องเท่าๆ กัน
            _monster1Walk = LoadFramesByContentRuns("monster_lv1_walk", Monster1WalkFrameCount, false);
            // ท่าโจมตีมีเอฟเฟกต์ปาดเลือดยื่นออกนอกตัว: ล็อกจุดยึดให้นิ่งตามเฟรมแรก ไม่ให้ตัวเด้งตามเอฟเฟกต์
            _monster1Attack = LoadFramesByContentRuns("monster_lv1_attack", Monster1AttackFrameCount, true);
            // ท่าตาย (ต้องเพิ่ม monster_lv1_die.png เข้า Content Pipeline ด้วยชื่อ asset "monster_lv1_die")
            _monster1Die = LoadFramesByContentRuns("monster_lv1_die", Monster1DieFrameCount, false);

            float monster1TargetHeight = _kaiTargetHeight * Monster1HeightToKaiRatio;
            _monster1Scale = monster1TargetHeight / _monster1Walk[0].Trim.Height;
            // สองชีทวาดด้วยสเกลภาพเดียวกัน (แคนวาสขนาดเท่ากัน) ใช้สเกลเดียวกัน ตัวจะได้ขนาดจริงเท่ากันทั้งเดินและโจมตี
            // ท่าโจมตีเตี้ยกว่าท่าเดินเพราะย่อตัวลงโจมตี (ไม่ใช่ตัวเล็กลง) ถ้าอยากปรับแยกให้แก้ Monster1AttackScaleMultiplier
            _monster1AttackScale = _monster1Scale * Monster1AttackScaleMultiplier;
            _monster1DieScale = _monster1Scale * Monster1DieScaleMultiplier;
            _monster1HalfWidth = _monster1Walk[0].Trim.Width * _monster1Scale / 2f;
            _monster1Height = _monster1Walk[0].Trim.Height * _monster1Scale;

            // Boss 1: ใช้จุดยึดแนวนอนคงที่ตามเฟรมแรกทุกชีท (LoadAttackFramesFromSpriteSheet) ให้ลำตัวไม่ไหลตามความกว้างของหนวด
            _boss1Walk = LoadAttackFramesFromSpriteSheet("boss_walk", Boss1FrameCount, Boss1FrameCount, 1);
            _boss1AttackP1 = LoadAttackFramesFromSpriteSheet("boss_phase1_attack", Boss1FrameCount, Boss1FrameCount, 1);
            _boss1AttackP2 = LoadAttackFramesFromSpriteSheet("boss_phase2_attack", Boss1FrameCount, Boss1FrameCount, 1);
            _boss1Die = LoadAttackFramesFromSpriteSheet("boss_die", Boss1FrameCount, Boss1FrameCount, 1);

            _boss1Scale = (_kaiTargetHeight * Boss1HeightToKaiRatio) / _boss1Walk[0].Trim.Height;
            _boss1Height = _boss1Walk[0].Trim.Height * _boss1Scale;
            _boss1HalfWidth = _boss1Walk[0].Trim.Width * _boss1Scale * Boss1HitboxWidthRatio / 2f;
            _boss1FeetLineY = _boss1Walk[0].Trim.Bottom;

            LoadBoss2Content();
            LoadMonster2Content();
            LoadTridentContent();
            LoadSwordAttackContent();
            LoadStoneContent();
            LoadBoss3Content();

            try
            {
                _bossOrbTexture = Content.Load<Texture2D>("power_boss");
            }
            catch (Microsoft.Xna.Framework.Content.ContentLoadException)
            {
                _bossOrbTexture = null;
            }

            _worldWidth = WindowedWidth * WorldWidthMultiplier;
            _worldHeight = WindowedHeight;

            _kaiCenterX = KaiSpawnX;
            _kaiJumpOffsetY = 0f;

            InitPlatforms();
            SpawnBossSceneStatue();
            SpawnBoss3AtEdge();

            // orb กลางห้อง 3 (เก็บแล้วเริ่มฉาก)
            _worldItems.Add(new WorldItem { Type = ItemType.Orb, Count = 1, X = _worldWidth / 2f, Room = OrbSceneRoomIndex });
            SpawnRandomSeaweed();

            _vignetteEffect = Content.Load<Effect>("VignetteEffect");
            _vignetteEffect.Parameters["Resolution"].SetValue(new Vector2(WindowedWidth, WindowedHeight));
            _vignetteEffect.Parameters["VignetteRadius"].SetValue(VignetteRadius);
            _vignetteEffect.Parameters["VignetteSoftness"].SetValue(VignetteSoftness);
            _vignetteEffect.Parameters["VignetteIntensity"].SetValue(VignetteIntensity);

            _sceneRenderTarget = new RenderTarget2D(GraphicsDevice, WindowedWidth, WindowedHeight);

            LoadGameSounds();
        }

        private AnimFrame[] LoadAnimFrames(string namePrefix, int frameCount)
        {
            var frames = new AnimFrame[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                var texture = Content.Load<Texture2D>($"{namePrefix}{i:D2}");
                var trim = GetOpaqueBounds(texture, new Rectangle(0, 0, texture.Width, texture.Height));

                frames[i] = new AnimFrame { Texture = texture, Trim = trim, OriginX = trim.Width / 2f, ClampHalfWidth = trim.Width / 2f };
            }
            return frames;
        }

        private AnimFrame[] LoadJumpFramesFromSpriteSheet(string sheetAssetName, int frameCount, int columns, int rows)
        {
            var sheet = Content.Load<Texture2D>(sheetAssetName);
            int cellWidth = sheet.Width / columns;
            int cellHeight = sheet.Height / rows;

            var frames = new AnimFrame[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                int col = i % columns;
                int row = i / columns;
                var cellRect = new Rectangle(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                var trim = GetOpaqueBounds(sheet, cellRect);

                frames[i] = new AnimFrame { Texture = sheet, Trim = trim, OriginX = trim.Width / 2f, ClampHalfWidth = trim.Width / 2f };
            }
            return frames;
        }

        private AnimFrame[] LoadAttackFramesFromSpriteSheet(string sheetAssetName, int frameCount, int columns, int rows)
        {
            var sheet = Content.Load<Texture2D>(sheetAssetName);
            int cellWidth = sheet.Width / columns;
            int cellHeight = sheet.Height / rows;

            var frames = new AnimFrame[frameCount];
            float anchorRelativeX = 0f;
            float clampHalfWidth = 0f;

            for (int i = 0; i < frameCount; i++)
            {
                int col = i % columns;
                int row = i / columns;
                var cellRect = new Rectangle(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                var trim = GetOpaqueBounds(sheet, cellRect);

                if (i == 0)
                {
                    anchorRelativeX = (trim.X - cellRect.X) + trim.Width / 2f;
                    clampHalfWidth = trim.Width / 2f;
                }

                float anchorAbsoluteX = cellRect.X + anchorRelativeX;
                frames[i] = new AnimFrame
                {
                    Texture = sheet,
                    Trim = trim,
                    OriginX = anchorAbsoluteX - trim.X,
                    ClampHalfWidth = clampHalfWidth
                };
            }
            return frames;
        }

        // ตัดเฟรมตาม "ช่วงคอลัมน์ที่มีพิกเซลทึบจริง" แทนการหารช่องเท่าๆ กัน
        // (ชีทของมอนสเตอร์ตำแหน่งเฟรมไม่ตรงกริดเท่ากัน ตัวละคร/เอฟเฟกต์เลยล้ำเข้าช่องข้างเคียงและโดนตัดขอบ)
        // stableAnchor = true: ล็อกจุดยึดตามความกว้างเฟรมแรกให้นิ่ง (ใช้กับท่าโจมตีที่มีเอฟเฟกต์ยื่นออกนอกตัว)
        // ถ้าจำนวนช่วงที่เจอไม่เท่ากับ frameCount จะถอยกลับไปใช้การหารช่องเท่ากันแบบเดิม
        private AnimFrame[] LoadFramesByContentRuns(string sheetAssetName, int frameCount, bool stableAnchor)
        {
            var sheet = Content.Load<Texture2D>(sheetAssetName);
            var data = new Color[sheet.Width * sheet.Height];
            sheet.GetData(data);

            var runs = new List<Point>();
            int runStart = -1;
            for (int x = 0; x < sheet.Width; x++)
            {
                bool hasPixel = false;
                for (int y = 0; y < sheet.Height; y++)
                {
                    if (data[y * sheet.Width + x].A > 10)
                    {
                        hasPixel = true;
                        break;
                    }
                }

                if (hasPixel && runStart < 0)
                {
                    runStart = x;
                }
                else if (!hasPixel && runStart >= 0)
                {
                    runs.Add(new Point(runStart, x - 1));
                    runStart = -1;
                }
            }
            if (runStart >= 0)
                runs.Add(new Point(runStart, sheet.Width - 1));

            if (runs.Count != frameCount)
            {
                return stableAnchor
                    ? LoadAttackFramesFromSpriteSheet(sheetAssetName, frameCount, frameCount, 1)
                    : LoadJumpFramesFromSpriteSheet(sheetAssetName, frameCount, frameCount, 1);
            }

            var frames = new AnimFrame[frameCount];
            float anchorX = 0f;

            for (int i = 0; i < frameCount; i++)
            {
                var cellRect = new Rectangle(runs[i].X, 0, runs[i].Y - runs[i].X + 1, sheet.Height);
                var trim = GetOpaqueBounds(sheet, cellRect);

                if (stableAnchor && i == 0)
                    anchorX = trim.Width / 2f;

                frames[i] = new AnimFrame
                {
                    Texture = sheet,
                    Trim = trim,
                    OriginX = stableAnchor ? anchorX : trim.Width / 2f,
                    ClampHalfWidth = trim.Width / 2f
                };
            }
            return frames;
        }

        private static Rectangle GetOpaqueBounds(Texture2D texture, Rectangle region)
        {
            var data = new Color[region.Width * region.Height];
            texture.GetData(0, region, data, 0, data.Length);

            int minX = region.Width, minY = region.Height, maxX = -1, maxY = -1;

            for (int y = 0; y < region.Height; y++)
            {
                for (int x = 0; x < region.Width; x++)
                {
                    if (data[y * region.Width + x].A > 10)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (maxX < 0)
                return region;

            return new Rectangle(region.X + minX, region.Y + minY, maxX - minX + 1, maxY - minY + 1);
        }

        private AnimFrame GetCurrentAnimFrame()
        {
            if (_tridentPoseFrame >= 0 && _kaiTridentFrames != null)
                return _kaiTridentFrames[_tridentPoseFrame];

            if (_isAttacking)
                return (_attackUsesSword && _kaiSwordAttack != null) ? _kaiSwordAttack[_attackFrameIndex] : _kaiAttack[_attackFrameIndex];

            if (_isJumping || _isLanding)
                return _kaiJump[_jumpFrameIndex];

            if (_isMoving)
                return _kaiRun[_currentFrame];

            return _kaiIdle[_currentFrame];
        }

        private float GetCurrentScale()
        {
            if (_tridentPoseFrame >= 0 && _kaiTridentFrames != null)
                return _tridentPoseScale;

            if (_isAttacking)
                return (_attackUsesSword && _kaiSwordAttack != null) ? _swordAttackScale : _attackScale;

            bool isJumpFrame = _isJumping || _isLanding;
            return isJumpFrame ? _kaiScale * _jumpScaleCorrection : _kaiScale;
        }

        private float GetCurrentHalfWidth()
        {
            return GetCurrentAnimFrame().ClampHalfWidth * GetCurrentScale();
        }

        // ==================== Wake-up Intro ====================

        // =====================================================================
        // Room 3: ฉาก Orb
        // =====================================================================

        private void StartOrbScene()
        {
            _isOrbScene = true;
            _orbSceneDone = true;
            _orbStage = OrbSceneStage.Dialogue1;
            _orbStageTimer = 0f;
            _orbCameraBlend = 0f;
            _orbMonster = null;

            // ล็อก Kai ให้ยืนนิ่ง (ห้อง 3 ไม่มีพื้นยกสูง จึงหยุดที่พื้นได้เลย)
            _isMoving = false;
            _isSprinting = false;
            _isJumping = false;
            _isLanding = false;
            _isAttacking = false;
            _kaiVelocityY = 0f;
            _kaiJumpOffsetY = 0f;
            _currentFrame = 0;
            _frameTimer = 0f;

            if (!_isDraggingItem)
                _isInventoryOpen = false;

            StartDialogue(OrbDialogue1Lines, "William Jones");
        }

        private void SpawnOrbSceneMonster()
        {
            float x = _worldWidth - _monster1HalfWidth; // ขอบขวาของห้อง 3

            _orbMonster = new Monster1
            {
                X = x,
                SpawnX = x,
                Room = OrbSceneRoomIndex,
                HP = Monster1MaxHP,
                State = Monster1State.Walking,
                FacingRight = false,
                IsAggro = true,
                IsScripted = true
            };
            _monster1List.Add(_orbMonster);
            _orbFirstMonster = _orbMonster;
        }

        private void UpdateOrbScene(GameTime gameTime, bool click)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (_orbStage)
            {
                case OrbSceneStage.Dialogue1:
                    if (UpdateDialogue(dt, click))
                    {
                        SpawnOrbSceneMonster();
                        _orbStage = OrbSceneStage.CameraToMonster;
                        _orbStageTimer = 0f;
                    }
                    break;

                case OrbSceneStage.CameraToMonster:
                    _orbStageTimer += dt;
                    _orbCameraBlend = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_orbStageTimer / OrbSceneCameraPanDuration, 0f, 1f));
                    if (_orbStageTimer >= OrbSceneCameraPanDuration)
                    {
                        _orbCameraBlend = 1f;
                        _orbStage = OrbSceneStage.CameraHold;
                        _orbStageTimer = 0f;
                    }
                    break;

                case OrbSceneStage.CameraHold:
                    _orbStageTimer += dt;
                    _orbCameraBlend = 1f;
                    if (_orbStageTimer >= OrbSceneCameraHoldDuration)
                    {
                        _orbStage = OrbSceneStage.CameraBack;
                        _orbStageTimer = 0f;
                    }
                    break;

                case OrbSceneStage.CameraBack:
                    _orbStageTimer += dt;
                    _orbCameraBlend = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_orbStageTimer / OrbSceneCameraPanDuration, 0f, 1f));
                    if (_orbStageTimer >= OrbSceneCameraPanDuration)
                    {
                        _orbCameraBlend = 0f;
                        _facingRight = true; // หันไปทางที่มอนเดินมา (ขอบขวา)
                        StartDialogue(OrbDialogue2Lines, "William Jones");
                        _orbStage = OrbSceneStage.Dialogue2;
                    }
                    break;

                case OrbSceneStage.Dialogue2:
                    if (UpdateDialogue(dt, click))
                        _orbStage = OrbSceneStage.MonsterApproach; // อ่านจบแล้ว รอมอนเดินมาถึงจุดหยุดก่อนคืนการควบคุม
                    break;

                case OrbSceneStage.MonsterApproach:
                    if (_orbMonster == null || System.Math.Abs(_kaiCenterX - _orbMonster.X) <= OrbSceneMonsterHoldDistance)
                        EndOrbScene();
                    break;
            }

            UpdateOrbSceneMonster(dt);

            _cameraZoom = MathHelper.Lerp(CameraZoom, OrbSceneMonsterZoom, _orbCameraBlend);
        }

        // มอนเดินเข้าหา Kai ด้วยสคริปต์ (เริ่มเดินหลังบทพูด 1 จบ) แล้วหยุดรอที่ระยะ OrbSceneMonsterHoldDistance
        // ใช้ระบบปีนพื้นยกสูง/แรงโน้มถ่วงเดียวกับ AI ปกติ (MoveMonsterHorizontally / UpdateMonsterVertical)
        private void UpdateOrbSceneMonster(float dt)
        {
            if (_orbMonster == null || !_orbMonster.IsScripted || _orbStage == OrbSceneStage.Dialogue1)
                return;

            var monster = _orbMonster;
            monster.AnimTimer += dt;
            monster.FacingRight = _kaiCenterX >= monster.X;

            float distance = System.Math.Abs(_kaiCenterX - monster.X);
            float vy = monster.VelocityY;
            bool air = monster.Airborne;

            if (distance > OrbSceneMonsterHoldDistance)
            {
                float direction = monster.FacingRight ? 1f : -1f;
                monster.X = MoveMonsterHorizontally(monster.Room, monster.X, direction * OrbSceneMonsterWalkSpeed * dt,
                    _monster1HalfWidth, _monster1Height, monster.YOffset, ref vy, ref air);
                monster.X = MathHelper.Clamp(monster.X, _monster1HalfWidth, _worldWidth - _monster1HalfWidth);

                // ความเร็วเดินเร็วกว่าปกติ จึงเร่งอนิเมชันตามเพื่อไม่ให้เท้าลื่น
                float frameDuration = Monster1FrameDuration * Monster1WalkSpeed / OrbSceneMonsterWalkSpeed;
                monster.State = Monster1State.Walking;
                monster.FrameIndex = (int)(monster.AnimTimer / frameDuration) % Monster1WalkFrameCount;
            }
            else
            {
                monster.State = Monster1State.Idle;
                monster.FrameIndex = 0;
            }

            float yOffset = monster.YOffset;
            UpdateMonsterVertical(monster.Room, monster.X, _monster1HalfWidth, ref yOffset, ref vy, ref air, dt);
            monster.YOffset = yOffset;
            monster.VelocityY = vy;
            monster.Airborne = air;
        }

        private void EndOrbScene()
        {
            _isOrbScene = false;
            _isDialogueActive = false;
            _orbCameraBlend = 0f;
            _cameraZoom = CameraZoom;

            // คืนให้ AI ปกติของ Monster1: ไล่โจมตี Kai
            if (_orbMonster != null)
            {
                _orbMonster.IsScripted = false;
                _orbMonster.IsAggro = false;
                AggroMonster1(_orbMonster);   // เริ่มไล่ Kai: เล่นเสียงร้อง
                _orbMonster.State = Monster1State.Walking;
                _orbMonster.AnimTimer = 0f;
                _orbMonster = null;
            }

            UpdateCamera();
        }

        // ---------- มอนเกิดเป็นรอบหลังฆ่ามอนตัวแรกของฉาก orb ----------

        private void UpdateMonsterWaves(float dt)
        {
            if (!_wavesStarted)
            {
                if (_orbFirstMonster == null)
                    return;

                // เริ่มรอบแรกเมื่อมอนตัวแรกตาย (เข้าท่าตาย หรือถูกลบออกจากลิสต์แล้ว)
                if (_orbFirstMonster.IsDying || !_monster1List.Contains(_orbFirstMonster))
                {
                    _wavesStarted = true;
                    _orbFirstMonster = null;
                    _waveDelayTimer = MonsterWaveDelay;
                }
                return;
            }

            if (_wavesFinished)
                return;

            _waveMonsters.RemoveAll(m => m.IsDying || !_monster1List.Contains(m));

            if (_waveIndex >= MonsterWaveSizes.Length || _waveTotalSpawned >= MonsterWaveTotalLimit)
            {
                // เกิดครบทุกรอบแล้ว: รอผู้เล่นฆ่าตัวที่เหลือให้หมด
                if (_waveMonsters.Count == 0)
                {
                    _wavesFinished = true;
                    ShowConsoleMessage("All waves cleared!", false);
                }
                return;
            }

            if (_waveMonsters.Count > 0)
                return; // ยังฆ่ารอบนี้ไม่หมด

            _waveDelayTimer -= dt;
            if (_waveDelayTimer > 0f)
                return;

            SpawnMonsterWave();
            _waveDelayTimer = MonsterWaveDelay;
        }

        private void SpawnMonsterWave()
        {
            int alive = 0;
            foreach (var m in _monster1List)
            {
                if (!m.IsDying)
                    alive++;
            }

            int count = MonsterWaveSizes[_waveIndex];
            count = System.Math.Min(count, MonsterWaveMaxAlive - alive);
            count = System.Math.Min(count, MonsterWaveTotalLimit - _waveTotalSpawned);
            if (count <= 0)
                return; // มอนในห้องเต็มโควตา รอไว้ก่อน (ลองใหม่เฟรมถัดไป)

            // ให้แต่ละห้อง (2 และ 3) มีมอนอย่างน้อย 1 ตัวเสมอ ตัวที่เหลือสุ่มห้อง
            int roomOffset = _random.Next(MonsterWaveRooms.Length);
            for (int i = 0; i < count; i++)
            {
                int room = i < MonsterWaveRooms.Length
                    ? MonsterWaveRooms[(i + roomOffset) % MonsterWaveRooms.Length]
                    : MonsterWaveRooms[_random.Next(MonsterWaveRooms.Length)];

                float x = PickWaveSpawnX(room);

                var monster = new Monster1
                {
                    X = x,
                    SpawnX = x,
                    Room = room,
                    HP = Monster1MaxHP,
                    State = Monster1State.Idle,
                    FacingRight = _random.Next(2) == 0,
                    // ยืนบนพื้นหรือบน platform ที่อยู่ตรงตำแหน่งนั้น
                    YOffset = GetSurfaceYAt(room, x, _monster1HalfWidth) - _floorY
                };

                _monster1List.Add(monster);
                _waveMonsters.Add(monster);
                _waveTotalSpawned++;
            }

            ShowConsoleMessage($"Wave {_waveIndex + 1}/{MonsterWaveSizes.Length}: monsters appeared in Room 2 and Room 3", false);
            _waveIndex++;
        }

        private float PickWaveSpawnX(int room)
        {
            float minX = System.Math.Max(_monster1HalfWidth, MonsterWaveSpawnMargin);
            float maxX = System.Math.Max(minX, _worldWidth - MonsterWaveSpawnMargin);
            float x = minX;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                x = minX + (float)_random.NextDouble() * (maxX - minX);

                if (room == _currentRoomIndex && System.Math.Abs(x - _kaiCenterX) < MonsterWaveMinDistanceFromKai)
                    continue;

                bool tooClose = false;
                foreach (var other in _waveMonsters)
                {
                    if (other.Room == room && System.Math.Abs(other.X - x) < MonsterWaveMinSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                    break;
            }

            return x;
        }

        private void StartWakeUpSequence()
        {
            _isWakeUpSequence = true;
            _wakeUpTimer = 0f;
            _wakeUpOverlayAlpha = 1f; // เริ่มจากจอมืดสนิท กันเฟรมแรกสว่างวาบก่อนเฟดเข้า
            _wakeUpNextRustleIndex = 0;
            _cameraZoom = WakeUpCameraZoom;
            _introStage = IntroStage.Blinking;
            _introWrenchVisible = false;
            _isDialogueActive = false;

            _isMoving = false;
            _isJumping = false;
            _isLanding = false;
            _isAttacking = false;
            _kaiJumpOffsetY = 0f;
            _currentFrame = 0;
            _frameTimer = 0f;

            UpdateCamera();
        }

        private void UpdateWakeUpSequence(GameTime gameTime, bool click)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (_introStage)
            {
                case IntroStage.Blinking:
                    UpdateWakeUpBlinking(dt);
                    break;

                case IntroStage.Dialogue1:
                    if (UpdateDialogue(dt, click))
                        StartIntroWrenchFall();
                    break;

                case IntroStage.WrenchFall:
                    UpdateIntroWrenchFall(dt);
                    break;

                case IntroStage.Dialogue2:
                    if (UpdateDialogue(dt, click))
                    {
                        // บทพูดจบ: วางประแจไว้ข้างหน้า Kai ตอนจอยังดำอยู่ แล้วค่อยๆ เฟดสว่างขึ้น
                        PlaceIntroWrenchInFrontOfKai();
                        _introStage = IntroStage.FadeIn;
                        _introStageTimer = 0f;
                    }
                    break;

                case IntroStage.FadeIn:
                    _introStageTimer += dt;
                    _wakeUpOverlayAlpha = 1f - MathHelper.SmoothStep(0f, 1f, _introStageTimer / IntroFadeInDuration);
                    if (_introStageTimer >= IntroFadeInDuration)
                        EndWakeUpSequence();
                    break;
            }

            // Kai ยืนอยู่ระหว่างบทพูด ให้ท่า idle ขยับตามปกติ
            if (_isWakeUpSequence && !IsSleepPoseVisible())
                UpdateKaiAnimation(gameTime);

            UpdateCamera();
        }

        private void UpdateWakeUpBlinking(float dt)
        {
            _wakeUpTimer += dt;
            _wakeUpOverlayAlpha = GetWakeUpOverlayAlpha(_wakeUpTimer);
            _cameraZoom = GetWakeUpCameraZoom(_wakeUpTimer);

            // เสียงเสื้อผ้าตอนลุก (ใช้เสียงเดียวกับตอนเก็บไอเทม) เล่นในช่วงที่จอมืดสนิท
            while (_wakeUpNextRustleIndex < WakeUpRustleTimes.Length && _wakeUpTimer >= WakeUpRustleTimes[_wakeUpNextRustleIndex])
            {
                PlaySfx(_pickupSoundEffect, PickupSoundVolume);
                _wakeUpNextRustleIndex++;
            }

            if (_wakeUpTimer >= WakeUpTotalDuration)
            {
                // กะพริบตาจบ -> เข้าบทพูดที่ 1 (ซูมกล้องกลับเป็นปกติแล้ว)
                _wakeUpTimer = WakeUpTotalDuration;
                _wakeUpOverlayAlpha = 0f;
                _cameraZoom = CameraZoom;
                StartDialogue(IntroDialogue1Lines, "William Jones");
                _introStage = IntroStage.Dialogue1;
            }
        }

        private void EndWakeUpSequence()
        {
            _isWakeUpSequence = false;
            _introWrenchVisible = false;
            _isDialogueActive = false;
            _wakeUpOverlayAlpha = 0f;
            _cameraZoom = CameraZoom; // กลับเป็นซูมปกติของเกม
        }

        // ---------- ประแจตกใส่หัว Kai / วางไว้ข้างหน้า ----------

        // พิกัด Y ของโลกที่ขอบบนสุดของหน้าจอตอนนี้ (ใช้เริ่มให้ประแจตกจากนอกจอด้านบน)
        private float GetViewTopWorldY()
        {
            return Vector2.Transform(Vector2.Zero, Matrix.Invert(_cameraTransform)).Y;
        }

        private void StartIntroWrenchFall()
        {
            _introStage = IntroStage.WrenchFall;
            _introWrenchX = _kaiCenterX;
            _introWrenchY = GetViewTopWorldY() - WorldItemHeight;
            _introWrenchVelocityY = 0f;
            _introWrenchRotation = 0f;
            _introWrenchVisible = true;
        }

        private void UpdateIntroWrenchFall(float dt)
        {
            _introWrenchVelocityY += IntroWrenchGravity * dt;
            _introWrenchY += _introWrenchVelocityY * dt;
            _introWrenchRotation += IntroWrenchSpinSpeed * dt;

            float hitY = _floorY - _kaiTargetHeight * IntroWrenchHitHeightRatio;
            if (_introWrenchY >= hitY)
            {
                // กระทบหัว Kai: เสียงกระทบ + จอดำทันที แล้วขึ้นบทพูดที่ 2 บนจอดำ
                _introWrenchVisible = false;
                PlaySfx(_impactSoundEffect, ImpactSoundVolume);
                _wakeUpOverlayAlpha = 1f;
                StartDialogue(IntroDialogue2Lines, "William Jones");
                _introStage = IntroStage.Dialogue2;
            }
        }

        // วางประแจเป็นไอเทมบนพื้นข้างหน้า Kai (ตามทิศที่หันอยู่) เลย ไม่มีอนิเมชันตกซ้ำ
        private void PlaceIntroWrenchInFrontOfKai()
        {
            PlaySfx(_itemLandSoundEffect, ItemLandSoundVolume);

            _worldItems.Add(new WorldItem
            {
                Type = ItemType.Wrench,
                Count = 1,
                X = GetDropItemX(),
                Room = _currentRoomIndex
            });
        }

        private void DrawIntroWrench()
        {
            if (!_isWakeUpSequence || !_introWrenchVisible || _wrenchTexture == null)
                return;

            var origin = new Vector2(_wrenchTexture.Width / 2f, _wrenchTexture.Height / 2f);
            float scale = WorldItemHeight / _wrenchTexture.Height;

            _spriteBatch.Draw(
                _wrenchTexture,
                new Vector2(_introWrenchX, _introWrenchY),
                null,
                Color.White,
                _introWrenchRotation,
                origin,
                scale,
                SpriteEffects.None,
                0f
            );
        }

        // ---------- กรอบบทพูด ----------

        private void StartDialogue(string[] lines, string speaker = "")
        {
            _dialogueLines = lines;
            _dialogueDefaultSpeaker = speaker ?? "";
            _dialogueLineIndex = 0;
            _isDialogueActive = lines != null && lines.Length > 0;

            if (_isDialogueActive)
                SetupDialogueLine();
        }

        private void SetupDialogueLine()
        {
            float maxTextWidth = GetDialogueBodyRect().Width - 2 * DialogueBoxPaddingX;

            string rawLine = _dialogueLines[_dialogueLineIndex] ?? "";
            string speaker = _dialogueDefaultSpeaker;
            int separatorIndex = rawLine.IndexOf('|');
            if (separatorIndex > 0 && separatorIndex <= 24)
            {
                speaker = rawLine.Substring(0, separatorIndex).Trim();
                rawLine = rawLine.Substring(separatorIndex + 1).TrimStart();
            }

            _dialogueSpeaker = SanitizeForFont(speaker);
            _dialogueWrappedText = WrapDialogueText(rawLine, maxTextWidth);
            _dialogueRevealedChars = 0f;
            _dialogueBlinkTimer = 0f;
        }

        // คืนค่า true เมื่อผู้เล่นอ่านครบทุกหน้าแล้ว: คลิกครั้งแรก = แสดงข้อความเต็ม, คลิกอีกครั้ง = หน้าถัดไป
        private bool UpdateDialogue(float dt, bool click)
        {
            if (!_isDialogueActive)
                return true;

            _dialogueBlinkTimer += dt;

            int totalChars = _dialogueWrappedText.Length;
            bool wasComplete = _dialogueRevealedChars >= totalChars;

            if (!wasComplete)
                _dialogueRevealedChars = MathHelper.Min(totalChars, _dialogueRevealedChars + DialogueCharsPerSecond * dt);

            if (click)
            {
                if (!wasComplete)
                {
                    _dialogueRevealedChars = totalChars;
                }
                else
                {
                    _dialogueLineIndex++;
                    if (_dialogueLineIndex >= _dialogueLines.Length)
                    {
                        _isDialogueActive = false;
                        return true;
                    }

                    SetupDialogueLine();
                }
            }

            return false;
        }

        // แทนตัวอักษรที่ฟอนต์ไม่มีด้วย '?' กัน DrawString/MeasureString ล้ม
        private string SanitizeForFont(string text)
        {
            var sb = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c == '\r')
                    continue;

                if (c == '\n' || _hotbarFont.Characters.Contains(c))
                    sb.Append(c);
                else
                    sb.Append('?');
            }

            return sb.ToString();
        }

        private float MeasureDialogueWidth(string text)
        {
            return _hotbarFont.MeasureString(text).X * DialogueTextScale;
        }

        // ตัดบรรทัดตามความกว้างกรอบ: ตัดที่ช่องว่างก่อน ถ้าคำเดียวยาวเกินให้ตัดกลางคำ (รองรับข้อความที่ไม่มีช่องว่าง)
        private string WrapDialogueText(string text, float maxWidth)
        {
            text = SanitizeForFont(text);
            var result = new System.Text.StringBuilder();
            string[] paragraphs = text.Split('\n');

            for (int p = 0; p < paragraphs.Length; p++)
            {
                string line = "";

                foreach (string word in paragraphs[p].Split(' '))
                {
                    string trial = line.Length == 0 ? word : line + " " + word;
                    if (MeasureDialogueWidth(trial) <= maxWidth)
                    {
                        line = trial;
                        continue;
                    }

                    if (line.Length > 0)
                    {
                        result.Append(line).Append('\n');
                        line = "";
                    }

                    string current = "";
                    foreach (char c in word)
                    {
                        if (current.Length > 0 && MeasureDialogueWidth(current + c) > maxWidth)
                        {
                            result.Append(current).Append('\n');
                            current = "";
                        }

                        current += c;
                    }

                    line = current;
                }

                result.Append(line);
                if (p < paragraphs.Length - 1)
                    result.Append('\n');
            }

            return result.ToString();
        }

        // กล่องใหญ่ของกล่องข้อความ (พิกัดหน้าจอเชิงตรรกะ) วางกึ่งกลางจอ ชิดล่าง
        private Rectangle GetDialogueBodyRect()
        {
            int width = (int)System.Math.Round(DialogueArtBodyWidth * DialogueBoxScale);
            int height = (int)System.Math.Round(DialogueArtBodyHeight * DialogueBoxScale);
            return new Rectangle((WindowedWidth - width) / 2, WindowedHeight - DialogueBoxBottomMargin - height, width, height);
        }

        private void DrawDialogueBox()
        {
            if ((!_isWakeUpSequence && !_isOrbScene && !_isDoorInteraction && !_isBossScene && !_isBoss2Scene && !_isBoss3Scene && !_isSignReading) || !_isDialogueActive)
                return;

            Rectangle box = GetDialogueBodyRect();

            if (_dialogueBoxTexture != null)
            {
                var artRect = new Rectangle(
                    (int)System.Math.Round(box.X - DialogueArtBodyLeft * DialogueBoxScale),
                    (int)System.Math.Round(box.Y - DialogueArtBodyTop * DialogueBoxScale),
                    (int)System.Math.Round(_dialogueBoxTexture.Width * DialogueBoxScale),
                    (int)System.Math.Round(_dialogueBoxTexture.Height * DialogueBoxScale));
                _spriteBatch.Draw(_dialogueBoxTexture, artRect, Color.White);
            }

            // ชื่อผู้พูดบนแท็บเล็กด้านซ้ายบน (ชื่อยาวเกินแท็บจะย่อตัวอักษรลงให้พอดี)
            if (!string.IsNullOrEmpty(_dialogueSpeaker))
            {
                Vector2 nameSize = _hotbarFont.MeasureString(_dialogueSpeaker);
                float nameScale = 1f;
                float nameMaxWidth = DialogueArtNameMaxWidth * DialogueBoxScale;
                if (nameSize.X > nameMaxWidth)
                    nameScale = nameMaxWidth / nameSize.X;

                var nameCenter = new Vector2(
                    box.X + (DialogueArtNameCenterX - DialogueArtBodyLeft) * DialogueBoxScale,
                    box.Y + (DialogueArtNameCenterY - DialogueArtBodyTop) * DialogueBoxScale);
                _spriteBatch.DrawString(_hotbarFont, _dialogueSpeaker, nameCenter - nameSize * nameScale / 2f,
                    Color.White, 0f, Vector2.Zero, nameScale, SpriteEffects.None, 0f);
            }

            int shown = MathHelper.Clamp((int)_dialogueRevealedChars, 0, _dialogueWrappedText.Length);
            string visibleText = _dialogueWrappedText.Substring(0, shown);
            var textPosition = new Vector2(box.X + DialogueBoxPaddingX, box.Y + DialogueBoxPaddingY);
            _spriteBatch.DrawString(_hotbarFont, visibleText, textPosition, Color.White, 0f, Vector2.Zero, DialogueTextScale, SpriteEffects.None, 0f);

            // ข้อความขึ้นครบแล้ว: ลูกศรชี้ลงกะพริบมุมขวาล่าง บอกให้คลิกเพื่อไปต่อ
            bool complete = _dialogueRevealedChars >= _dialogueWrappedText.Length;
            if (complete && ((int)(_dialogueBlinkTimer * 2f) % 2 == 0))
            {
                int arrowX = box.Right - DialogueBoxPaddingX - 16;
                int arrowY = box.Bottom - DialogueBoxPaddingY - 10;
                for (int i = 0; i < 4; i++)
                    DrawFilledRect(new Rectangle(arrowX + i * 2, arrowY + i * 3, 16 - i * 4, 3), Color.White);
            }
        }

        // ยังอยู่ในช่วงที่ Kai นอนคว่ำ (ก่อนจอมืดสนิทในกะพริบครั้งสุดท้าย)
        private bool IsSleepPoseVisible()
        {
            return _isWakeUpSequence && _wakeUpTimer < WakeUpFinalDarkStartTime;
        }

        // ซูมเข้าค้างไว้ตลอดฉาก แล้วค่อยๆ ซูมออกกลับเป็นปกติตอนจอสว่างหลังมืดค้างครั้งสุดท้าย
        private static float GetWakeUpCameraZoom(float t)
        {
            if (t <= WakeUpFinalDarkEndTime)
                return WakeUpCameraZoom;

            float u = MathHelper.Clamp((t - WakeUpFinalDarkEndTime) / WakeUpFinalOpenDuration, 0f, 1f);
            return MathHelper.Lerp(WakeUpCameraZoom, CameraZoom, MathHelper.SmoothStep(0f, 1f, u));
        }

        // ค่าความมืดของจอ (0 = สว่าง, 1 = มืดสนิท) ตามเวลาของฉากตื่นนอน
        private static float GetWakeUpOverlayAlpha(float t)
        {
            // ช่วงแรก: จากจอมืดสนิทค่อยๆ สว่างขึ้น
            if (t < WakeUpIntroFadeInDuration)
                return 1f - MathHelper.SmoothStep(0f, 1f, t / WakeUpIntroFadeInDuration);

            float cursor = WakeUpIntroFadeInDuration + WakeUpInitialOpenDuration;
            if (t < cursor)
                return 0f;

            for (int i = 0; i < WakeUpBlinkCount; i++)
            {
                bool isLast = i == WakeUpBlinkCount - 1;
                float hold = isLast ? WakeUpFinalDarkDuration : WakeUpBlinkDarkHoldDuration;
                float open = isLast ? WakeUpFinalOpenDuration : WakeUpBlinkOpenDuration;

                if (t < cursor + WakeUpBlinkCloseDuration)
                    return MathHelper.SmoothStep(0f, 1f, (t - cursor) / WakeUpBlinkCloseDuration);
                cursor += WakeUpBlinkCloseDuration;

                if (t < cursor + hold)
                    return 1f;
                cursor += hold;

                if (t < cursor + open)
                    return 1f - MathHelper.SmoothStep(0f, 1f, (t - cursor) / open);
                cursor += open;

                if (!isLast)
                {
                    if (t < cursor + WakeUpBlinkGapDuration)
                        return 0f;
                    cursor += WakeUpBlinkGapDuration;
                }
            }

            return 0f;
        }

        // หาจุดต่ำสุดของตัว (พิกเซลที่ไม่โปร่งใส) หลังพลิกซ้ายขวาและหมุนแล้ว วัดจากกึ่งกลางภาพ หน่วยพิกเซลโลก
        private float ComputeSleepPoseLowestPoint()
        {
            var region = _kaiSleepTrim;
            var data = new Color[region.Width * region.Height];
            _kaiSleepTexture.GetData(0, region, data, 0, data.Length);

            float centerX = region.Width / 2f;
            float centerY = region.Height / 2f;
            float sin = (float)System.Math.Sin(_kaiSleepRotation);
            float cos = (float)System.Math.Cos(_kaiSleepRotation);
            float lowest = float.MinValue;

            for (int y = 0; y < region.Height; y += 2)
            {
                for (int x = 0; x < region.Width; x += 2)
                {
                    if (data[y * region.Width + x].A <= 10)
                        continue;

                    float px = -(x - centerX); // พลิกซ้ายขวา
                    float py = y - centerY;
                    float rotatedY = px * sin + py * cos;
                    if (rotatedY > lowest)
                        lowest = rotatedY;
                }
            }

            return lowest == float.MinValue ? region.Height / 2f * _kaiSleepScale : lowest * _kaiSleepScale;
        }

        private void DrawKaiSleepPose()
        {
            // จุดยึดอยู่กึ่งกลางภาพ (หมุนรอบจุดนี้) วางให้จุดต่ำสุดของตัวอยู่ต่ำกว่าแนวพื้นตาม SleepPoseGroundSink
            var origin = new Vector2(_kaiSleepTrim.Width / 2f, _kaiSleepTrim.Height / 2f);
            var position = new Vector2(_kaiCenterX, _floorY + SleepPoseGroundSink + _kaiSleepCenterOffsetY);

            _spriteBatch.Draw(
                _kaiSleepTexture,
                position,
                _kaiSleepTrim,
                Color.White,
                _kaiSleepRotation,
                origin,
                _kaiSleepScale,
                SpriteEffects.FlipHorizontally, // ภาพต้นฉบับหัวหันซ้าย พลิกให้หัวหันขวา
                0f
            );
        }

        private void DrawWakeUpOverlay()
        {
            if (!_isWakeUpSequence || _wakeUpOverlayAlpha <= 0f)
                return;

            DrawFilledRect(
                new Rectangle(0, 0, WindowedWidth, WindowedHeight),
                new Color(0, 0, 0, (int)(_wakeUpOverlayAlpha * 255f)));
        }

        // =====================================================================
        // Door (ห้อง 1) — ต้องมี Orb ถึงเปิดได้ / เปิดแล้วกด E อีกครั้งเพื่อเข้าห้องลับ
        // =====================================================================

        private float GetDoorHeight()
        {
            return _kaiTargetHeight * DoorHeightToKaiRatio;
        }

        private Rectangle GetDoorRect()
        {
            float height = GetDoorHeight();
            return new Rectangle((int)(DoorX - DoorWidth / 2f), (int)(_floorY - height), (int)DoorWidth, (int)height);
        }

        private bool IsKaiNearDoor()
        {
            if (_currentRoomIndex != DoorRoomIndex)
                return false;

            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            return System.Math.Abs(DoorX - _kaiCenterX) <= DoorInteractRange
                && System.Math.Abs(kaiFeetY - _floorY) <= _kaiTargetHeight * 0.75f;
        }

        // เปลี่ยนห้องด้วย fade เดิม แต่ไปห้อง/ตำแหน่งที่ระบุ (ใช้กับประตู)
        private void StartDoorTransition(int targetRoom, float targetX, bool facingRight)
        {
            _isTransitioning = true;
            _transitionDirection = 0;
            _transitionTargetRoom = targetRoom;
            _transitionTargetX = targetX;
            _transitionTargetFacingRight = facingRight;
            _transitionTimer = 0f;
            _transitionOverlayAlpha = 0f;
            _transitionPlayerResetDone = false;
        }

        // หักไอเทมออกจากกระเป๋า (hotbar ก่อน แล้วกระเป๋าหลัก) คืน false ถ้ามีไม่พอ (ไม่หักอะไรเลย)
        // กุญแจจากห้อง 3 (ชนิด Orb ในโค้ด) กับ Key ของบอสใช้เปิดประตูได้เหมือนกันทั้งสองบาน
        private int GetAnyKeyCount()
        {
            return GetItemTotalCount(ItemType.Key) + GetItemTotalCount(ItemType.Orb);
        }

        // ใช้กุญแจ 1 อัน: preferOrb = true จะใช้อันจากห้อง 3 (Orb) ก่อน เก็บ Key ของบอสไว้ และกลับกัน
        private bool TryRemoveAnyKey(bool preferOrb)
        {
            ItemType first = preferOrb ? ItemType.Orb : ItemType.Key;
            ItemType second = preferOrb ? ItemType.Key : ItemType.Orb;
            return TryRemoveItem(first, 1) || TryRemoveItem(second, 1);
        }

        private bool TryRemoveItem(ItemType type, int count = 1)
        {
            if (type == ItemType.None || count <= 0 || GetItemTotalCount(type) < count)
                return false;

            int remaining = count;

            for (int i = 0; i < _hotbarSlots.Length && remaining > 0; i++)
            {
                if (_hotbarSlots[i].Type != type)
                    continue;

                int take = System.Math.Min(remaining, _hotbarSlots[i].Count);
                _hotbarSlots[i].Count -= take;
                remaining -= take;
                if (_hotbarSlots[i].Count <= 0)
                    _hotbarSlots[i].Clear();
            }

            for (int i = 0; i < _inventoryGridSlots.Length && remaining > 0; i++)
            {
                if (_inventoryGridSlots[i].Type != type)
                    continue;

                int take = System.Math.Min(remaining, _inventoryGridSlots[i].Count);
                _inventoryGridSlots[i].Count -= take;
                remaining -= take;
                if (_inventoryGridSlots[i].Count <= 0)
                    _inventoryGridSlots[i].Clear();
            }

            if (_equippedHotbarIndex >= 0 && _hotbarSlots[_equippedHotbarIndex].IsEmpty)
                _equippedHotbarIndex = -1;

            return remaining == 0;
        }

        // กด E ใกล้ประตู: ประตูเปิดแล้ว = เข้าห้องลับ / ยังล็อกอยู่ = บทพูด (ไม่มี Orb หรือมี Orb ต่างกัน)
        private void UpdateDoorInput()
        {
            if (_currentRoomIndex != DoorRoomIndex)
                return;
            if (_isConsoleOpen || _isTransitioning || _isGiveItemMenuOpen || _isInventoryOpen)
                return;
            if (_isJumping || _isLanding || _isAttacking)
                return;

            var keyboard = Keyboard.GetState();
            if (!IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.E))
                return;
            if (!IsKaiNearDoor())
                return;

            // ถ้ามีไอเทมบนพื้นให้เก็บอยู่ใกล้ตัว ให้การเก็บของมาก่อน
            foreach (var worldItem in _worldItems)
            {
                if (!worldItem.Collected && worldItem.Room == _currentRoomIndex && IsWorldItemInPickupRange(worldItem))
                    return;
            }

            if (_doorOpened)
            {
                StartDoorTransition(SecretRoomIndex, SecretRoomSpawnX, true);
                return;
            }

            StartDoorInteraction(GetAnyKeyCount() > 0);
        }

        // =====================================================================
        // ประตูทางออกห้องบอส (โผล่หลังบอสตาย): ไม่มีกุญแจ = บทพูดล็อก / มีกุญแจ = บทพูด -> ยืนยัน -> หัก Key 1 ดอก -> เปิด
        // เปิดแล้วกด E อีกครั้งเพื่อไปห้องถัดไป (เปิดแล้วเปิดค้างไว้ ไม่ต้องใช้กุญแจซ้ำ)
        // =====================================================================
        private Rectangle GetBossExitDoorRect()
        {
            float height = GetDoorHeight();
            return new Rectangle((int)(BossExitDoorX - DoorWidth / 2f), (int)(_floorY - height), (int)DoorWidth, (int)height);
        }

        private bool IsKaiNearBossExitDoor()
        {
            if (!_bossExitDoorVisible || _currentRoomIndex != BossSceneRoomIndex)
                return false;

            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            return System.Math.Abs(BossExitDoorX - _kaiCenterX) <= DoorInteractRange
                && System.Math.Abs(kaiFeetY - _floorY) <= _kaiTargetHeight * 0.75f;
        }

        private void UpdateBossExitDoorInput()
        {
            if (!_bossExitDoorVisible || _currentRoomIndex != BossSceneRoomIndex)
                return;
            if (_isConsoleOpen || _isTransitioning || _isGiveItemMenuOpen || _isInventoryOpen)
                return;
            if (_isJumping || _isLanding || _isAttacking)
                return;

            var keyboard = Keyboard.GetState();
            if (!IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.E))
                return;
            if (!IsKaiNearBossExitDoor())
                return;

            // ถ้ามีไอเทมบนพื้นให้เก็บอยู่ใกล้ตัว (เช่น กุญแจที่บอสดรอป) ให้การเก็บของมาก่อน
            foreach (var worldItem in _worldItems)
            {
                if (!worldItem.Collected && worldItem.Room == _currentRoomIndex && IsWorldItemInPickupRange(worldItem))
                    return;
            }

            if (_bossExitDoorOpened)
            {
                StartDoorTransition(NextLevelRoomIndex, NextLevelRoomSpawnX, true);
                return;
            }

            StartDoorInteraction(GetAnyKeyCount() > 0, true);
        }

        // =====================================================================
        // จุดเซฟ (โครงกระดูกในห้องบอส): กด E ใกล้ตัวเพื่อเซฟ
        // =====================================================================
        private bool IsKaiNearSavePoint()
        {
            if (_currentRoomIndex != SavePointRoomIndex)
                return false;

            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            return System.Math.Abs(SavePointX - _kaiCenterX) <= DoorInteractRange
                && System.Math.Abs(kaiFeetY - _floorY) <= _kaiTargetHeight * 0.75f;
        }

        private bool IsSavePointActive()
        {
            return _checkpointRoomIndex == SavePointRoomIndex;
        }

        private void UpdateSavePointInput()
        {
            if (_currentRoomIndex != SavePointRoomIndex)
                return;
            if (_isConsoleOpen || _isTransitioning || _isGiveItemMenuOpen || _isInventoryOpen)
                return;
            if (_isJumping || _isLanding || _isAttacking)
                return;

            var keyboard = Keyboard.GetState();
            if (!IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.E))
                return;
            bool canSave = !IsSavePointActive();
            bool canTakeSword = !_savePointSwordTaken;
            if (!IsKaiNearSavePoint() || (!canSave && !canTakeSword))
                return;

            // ถ้ามีไอเทมบนพื้นให้เก็บอยู่ใกล้ตัว ให้การเก็บของมาก่อน
            foreach (var worldItem in _worldItems)
            {
                if (!worldItem.Collected && worldItem.Room == _currentRoomIndex && IsWorldItemInPickupRange(worldItem))
                    return;
            }

            if (canSave)
            {
                _checkpointRoomIndex = SavePointRoomIndex;
                _checkpointX = SavePointX + SavePointRespawnOffsetX;

                PlaySfx(_pickupSoundEffect, PickupSoundVolume);
                _damagePopups.Add(new DamagePopup
                {
                    Text = "Checkpoint saved",
                    Position = new Vector2(_kaiCenterX, _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 10f),
                    Color = new Color(120, 220, 255),
                    IsCrit = true
                });
                ShowConsoleMessage("Checkpoint saved", false);
            }

            // หยิบดาบที่ทาบอยู่บนโครงกระดูก (ครั้งเดียว) เข้ากระเป๋า กระเป๋าเต็มก็ยังหยิบไม่ได้ ดาบยังอยู่ให้กลับมาหยิบใหม่
            if (canTakeSword)
            {
                if (TryAddItem(ItemType.Sword))
                {
                    _savePointSwordTaken = true;
                    PlaySfx(_pickupSoundEffect, PickupSoundVolume);
                    _damagePopups.Add(new DamagePopup
                    {
                        Text = "Got Sword",
                        Position = new Vector2(_kaiCenterX, _floorY + _kaiJumpOffsetY - _kaiTargetHeight - (canSave ? 36f : 10f)),
                        Color = new Color(255, 225, 120),
                        IsCrit = true
                    });
                    ShowConsoleMessage("Got Sword", false);
                }
                else
                {
                    ShowConsoleMessage("Inventory full", true);
                }
            }
        }

        // ดาบทาบบนมือโครงกระดูก + ประกายระยิบ (หายไปหลังหยิบ)
        private void DrawSavePointSword(Rectangle skeletonRect)
        {
            if (_savePointSwordTaken || _boss2SwordTexture == null)
                return;

            float length = SavePointDrawHeight * SavePointSwordLengthRatio;
            float tip = MathHelper.ToRadians(SavePointSwordTipAngleDegrees);
            var center = new Vector2(SavePointX + SavePointSwordOffsetX, _floorY - SavePointDrawHeight * SavePointSwordOffsetYRatio);

            // หมุนให้แกนดาบ (ด้าม -> ปลาย) ชี้ไปทิศ tip (แกนในภาพต้นฉบับเอียง -70.455 องศา ยาว ~1.0459 เท่าของความสูงภาพ)
            float scale = length / (_boss2SwordTexture.Height * Boss2SwordImageAxisLengthToHeight);
            float rotation = tip - MathHelper.ToRadians(Boss2SwordImageAxisAngleDegrees);
            var origin = new Vector2(_boss2SwordTexture.Width / 2f, _boss2SwordTexture.Height / 2f);
            _spriteBatch.Draw(_boss2SwordTexture, center, null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);

            // ประกายสีทองกะพริบเป็นจังหวะตามความยาวดาบ
            float time = System.Environment.TickCount / 1000f;
            var dir = new Vector2((float)System.Math.Cos(tip), (float)System.Math.Sin(tip));
            for (int k = 0; k < 4; k++)
            {
                float phase = time * 2.4f + k * 1.7f;
                float twinkle = (float)System.Math.Sin(phase);
                if (twinkle <= 0f)
                    continue;

                float along = ((k * 0.27f + 0.12f) - 0.5f) * length;
                var pos = center + dir * along + new Vector2((float)System.Math.Cos(phase * 0.7f) * 7f, (float)System.Math.Sin(phase * 0.9f) * 7f);
                float size = 3f + twinkle * 4f;
                Color sparkle = new Color(255, 240, 170) * twinkle;
                _spriteBatch.Draw(_pixelTexture, pos, null, sparkle, 0f, new Vector2(0.5f, 0.5f), new Vector2(size * 2.2f, 1.6f), SpriteEffects.None, 0f);
                _spriteBatch.Draw(_pixelTexture, pos, null, sparkle, 0f, new Vector2(0.5f, 0.5f), new Vector2(1.6f, size * 2.2f), SpriteEffects.None, 0f);
            }
        }

        private void DrawSavePoint()
        {
            if (_currentRoomIndex != SavePointRoomIndex)
                return;

            float scale = SavePointDrawHeight / SavePointSourceRect.Height;
            float drawWidth = SavePointSourceRect.Width * scale;
            var destRect = new Rectangle(
                (int)(SavePointX - drawWidth / 2f),
                (int)(_floorY - SavePointDrawHeight),
                (int)drawWidth,
                (int)SavePointDrawHeight);

            if (_savePointTexture != null)
                _spriteBatch.Draw(_savePointTexture, destRect, SavePointSourceRect, Color.White);

            DrawSavePointSword(destRect);

            bool canPrompt = IsKaiNearSavePoint() && (!IsSavePointActive() || !_savePointSwordTaken) && !_isDoorInteraction && !_isTransitioning
                && !_isInventoryOpen && !_isConsoleOpen && !_isGiveItemMenuOpen;

            if (canPrompt)
            {
                string prompt = !IsSavePointActive() && !_savePointSwordTaken ? "Press E to save / take sword"
                    : !_savePointSwordTaken ? "Press E to take sword"
                    : "Press E to save";
                const float promptScale = 0.7f;
                Vector2 promptSize = _hotbarFont.MeasureString(prompt) * promptScale;
                var promptPosition = new Vector2(SavePointX - promptSize.X / 2f, destRect.Y - promptSize.Y - 12f);
                _spriteBatch.DrawString(_hotbarFont, prompt, promptPosition, Color.White, 0f, Vector2.Zero, promptScale, SpriteEffects.None, 0f);
            }
        }

        private void DrawBossExitDoor()
        {
            if (!_bossExitDoorVisible || _currentRoomIndex != BossSceneRoomIndex)
                return;

            var rect = GetBossExitDoorRect();

            DrawFilledRect(new Rectangle(rect.X - 6, rect.Y - 6, rect.Width + 12, rect.Height + 6), new Color(70, 55, 40));

            if (_bossExitDoorOpened)
            {
                DrawFilledRect(rect, new Color(8, 8, 12));
            }
            else
            {
                DrawFilledRect(rect, new Color(112, 82, 52));
                DrawFilledRect(new Rectangle(rect.Right - 20, rect.Y + rect.Height / 2, 8, 8), new Color(230, 200, 90));
            }

            bool canPrompt = IsKaiNearBossExitDoor() && !_isDoorInteraction && !_isTransitioning
                && !_isInventoryOpen && !_isConsoleOpen && !_isGiveItemMenuOpen;

            if (canPrompt)
            {
                string prompt = _bossExitDoorOpened ? "Press E to enter" : "Press E";
                const float promptScale = 0.7f;
                Vector2 promptSize = _hotbarFont.MeasureString(prompt) * promptScale;
                var promptPosition = new Vector2(rect.X + rect.Width / 2f - promptSize.X / 2f, rect.Y - promptSize.Y - 12f);
                _spriteBatch.DrawString(_hotbarFont, prompt, promptPosition, Color.White, 0f, Vector2.Zero, promptScale, SpriteEffects.None, 0f);
            }
        }

        private void StartDoorInteraction(bool hasOrb, bool isBossExit = false)
        {
            _isDoorInteraction = true;
            _doorIsBossExit = isBossExit;
            _doorStage = DoorStage.Dialogue;
            _doorDialogueLeadsToConfirm = hasOrb; // hasOrb = มีของที่ใช้เปิดประตูนี้ (Orb ของประตูห้อง 1 / Key ของประตูห้องบอส)

            // ล็อก Kai ให้ยืนนิ่งหันหน้าเข้าหาประตู
            _isMoving = false;
            _isSprinting = false;
            _isAttacking = false;
            _attackFrameIndex = 0;
            _currentFrame = 0;
            _frameTimer = 0f;
            _facingRight = (isBossExit ? BossExitDoorX : DoorX) >= _kaiCenterX;

            string[] lines = isBossExit
                ? (hasOrb ? BossExitDoorHasKeyLines : BossExitDoorLockedLines)
                : (hasOrb ? DoorHasOrbLines : DoorLockedLines);
            StartDialogue(lines, "William Jones");
        }

        private void EndDoorInteraction()
        {
            _isDoorInteraction = false;
            _doorIsBossExit = false;
            _isDialogueActive = false;
            _doorStage = DoorStage.Dialogue;
        }

        private void UpdateDoorInteraction(GameTime gameTime, bool click)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_doorStage == DoorStage.Dialogue)
            {
                if (UpdateDialogue(dt, click))
                {
                    if (_doorDialogueLeadsToConfirm)
                        _doorStage = DoorStage.Confirm; // อ่านบทพูดจบแล้ว ถามยืนยันว่าจะใช้ Orb ไหม
                    else
                        EndDoorInteraction();
                }
                return;
            }

            UpdateDoorConfirm(click);
        }

        private void UpdateDoorConfirm(bool click)
        {
            var keyboard = Keyboard.GetState();
            bool escapeJustPressed = IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.Escape);

            if (escapeJustPressed)
            {
                EndDoorInteraction(); // Esc = ไม่ใช้ (เหมือนกด No)
                return;
            }

            if (!click)
                return;

            Vector2 mouse = GetLogicalMousePosition();
            var layout = GetDoorConfirmLayout();

            if (layout.YesRect.Contains(mouse))
            {
                if (_doorIsBossExit)
                {
                    if (TryRemoveAnyKey(false))
                    {
                        _bossExitDoorOpened = true;
                        PlayDoorOpenSound();
                        ShowConsoleMessage("The door opened", false);
                    }
                    else
                    {
                        ShowConsoleMessage("No Key", true);
                    }
                }
                else if (TryRemoveAnyKey(true))
                {
                    _doorOpened = true;
                    PlayDoorOpenSound();
                    ShowConsoleMessage("The door opened", false);
                }
                else
                {
                    ShowConsoleMessage("No Key", true);
                }

                EndDoorInteraction();
            }
            else if (layout.NoRect.Contains(mouse))
            {
                EndDoorInteraction();
            }
        }

        private struct DoorConfirmLayout
        {
            public Rectangle PanelRect;
            public Rectangle YesRect;
            public Rectangle NoRect;
        }

        private DoorConfirmLayout GetDoorConfirmLayout()
        {
            int panelWidth = 480;
            int panelHeight = 170;
            int panelX = (WindowedWidth - panelWidth) / 2;
            int panelY = (WindowedHeight - panelHeight) / 2;

            int buttonWidth = 140;
            int buttonHeight = 40;
            int buttonY = panelY + panelHeight - buttonHeight - 22;

            return new DoorConfirmLayout
            {
                PanelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight),
                NoRect = new Rectangle(panelX + 40, buttonY, buttonWidth, buttonHeight),
                YesRect = new Rectangle(panelX + panelWidth - buttonWidth - 40, buttonY, buttonWidth, buttonHeight)
            };
        }

        // ประตู placeholder (สี่เหลี่ยม): ปิด = สีไม้ + ลูกบิด / เปิด = ช่องมืด
        private void DrawDoor()
        {
            if (_currentRoomIndex != DoorRoomIndex)
                return;

            var rect = GetDoorRect();

            DrawFilledRect(new Rectangle(rect.X - 6, rect.Y - 6, rect.Width + 12, rect.Height + 6), new Color(70, 55, 40));

            if (_doorOpened)
            {
                DrawFilledRect(rect, new Color(8, 8, 12));
            }
            else
            {
                DrawFilledRect(rect, new Color(112, 82, 52));
                DrawFilledRect(new Rectangle(rect.Right - 20, rect.Y + rect.Height / 2, 8, 8), new Color(230, 200, 90));
            }

            bool canPrompt = IsKaiNearDoor() && !_isDoorInteraction && !_isTransitioning
                && !_isWakeUpSequence && !_isOrbScene && !_isInventoryOpen
                && !_isConsoleOpen && !_isGiveItemMenuOpen;

            if (canPrompt)
            {
                string prompt = _doorOpened ? "Press E to enter" : "Press E";
                const float promptScale = 0.7f;
                Vector2 promptSize = _hotbarFont.MeasureString(prompt) * promptScale;
                var promptPosition = new Vector2(rect.X + rect.Width / 2f - promptSize.X / 2f, rect.Y - promptSize.Y - 12f);
                _spriteBatch.DrawString(_hotbarFont, prompt, promptPosition, Color.White, 0f, Vector2.Zero, promptScale, SpriteEffects.None, 0f);
            }
        }

        private void DrawDoorConfirmUI()
        {
            if (!_isDoorInteraction || _doorStage != DoorStage.Confirm)
                return;

            var layout = GetDoorConfirmLayout();
            Vector2 mouse = GetLogicalMousePosition();

            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, WindowedHeight), new Color(0, 0, 0, 120));
            DrawFilledRect(layout.PanelRect, new Color(18, 20, 24, 245));
            DrawRectOutline(layout.PanelRect, 2, new Color(200, 205, 215));

            string confirmPrompt = _doorIsBossExit ? BossExitDoorConfirmPrompt : DoorConfirmPrompt;
            Vector2 promptSize = _hotbarFont.MeasureString(confirmPrompt);
            var promptPos = new Vector2(
                layout.PanelRect.X + (layout.PanelRect.Width - promptSize.X) / 2f,
                layout.PanelRect.Y + 34f);
            _spriteBatch.DrawString(_hotbarFont, confirmPrompt, promptPos, Color.White);

            bool noHover = layout.NoRect.Contains(mouse);
            DrawFilledRect(layout.NoRect, noHover ? new Color(90, 94, 102) : new Color(60, 64, 72));
            DrawRectOutline(layout.NoRect, 2, Color.LightGray);
            DrawCenteredButtonText("No", layout.NoRect);

            bool yesHover = layout.YesRect.Contains(mouse);
            DrawFilledRect(layout.YesRect, yesHover ? new Color(90, 160, 90) : new Color(70, 130, 70));
            DrawRectOutline(layout.YesRect, 2, Color.LightGreen);
            DrawCenteredButtonText("Yes", layout.YesRect);
        }

        // =====================================================================
        // ห้องลับ: ฉากบอส (ดูค่าคงที่ที่ BossScene* ด้านบน)
        // =====================================================================

        private void SpawnBossSceneStatue()
        {
            float x = BossStatueX;

            _bossSceneBoss = new Boss1
            {
                X = x,
                SpawnX = x,
                Room = BossSceneRoomIndex,
                HP = Boss1MaxHP,
                State = Boss1State.Idle,
                FacingRight = false, // หันซ้ายเข้าหาทางเข้าห้อง (Kai เข้ามาจากซ้าย)
                IsStatue = true,
                YOffset = GetSurfaceYAt(BossSceneRoomIndex, x, _boss1HalfWidth) - _floorY
            };
            _boss1List.Add(_bossSceneBoss);
        }

        private bool IsKaiNearBossStatue()
        {
            if (_bossSceneDone || _bossSceneBoss == null || !_bossSceneBoss.IsStatue || _currentRoomIndex != BossSceneRoomIndex)
                return false;

            return System.Math.Abs(_bossSceneBoss.X - _kaiCenterX) <= BossSceneInteractRange
                && System.Math.Abs(_kaiJumpOffsetY) <= _kaiTargetHeight * 0.75f;
        }

        // กด E ใกล้บอสที่ยืนนิ่งอยู่ = เริ่มฉากบอส
        private void UpdateBossSceneInput()
        {
            if (_bossSceneDone || _currentRoomIndex != BossSceneRoomIndex)
                return;
            if (_isConsoleOpen || _isTransitioning || _isGiveItemMenuOpen || _isInventoryOpen)
                return;
            if (_isJumping || _isLanding || _isAttacking)
                return;

            var keyboard = Keyboard.GetState();
            if (!IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.E))
                return;
            if (!IsKaiNearBossStatue())
                return;

            // ถ้ามีไอเทมบนพื้นให้เก็บอยู่ใกล้ตัว ให้การเก็บของมาก่อน
            foreach (var worldItem in _worldItems)
            {
                if (!worldItem.Collected && worldItem.Room == _currentRoomIndex && IsWorldItemInPickupRange(worldItem))
                    return;
            }

            StartBossScene();
        }

        private void StartBossScene()
        {
            _isBossScene = true;
            _bossSceneDone = true;
            _bossStage = BossSceneStage.Dialogue1;
            _bossStageTimer = 0f;
            _bossFocusBlend = 0f;
            _bossSceneZoom = CameraZoom;
            _bossShakeActive = false;
            _bossShakeTime = 0f;

            // ล็อก Kai ให้ยืนนิ่งหันหน้าเข้าหาบอส
            _isMoving = false;
            _isSprinting = false;
            _isAttacking = false;
            _attackFrameIndex = 0;
            _currentFrame = 0;
            _frameTimer = 0f;
            _facingRight = _bossSceneBoss.X >= _kaiCenterX;

            if (!_isDraggingItem)
                _isInventoryOpen = false;

            StartDialogue(BossDialogue1Lines, "Beequid");
        }

        private void UpdateBossScene(GameTime gameTime, bool click)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_bossShakeActive)
                _bossShakeTime += dt;

            switch (_bossStage)
            {
                case BossSceneStage.Dialogue1:
                    _bossFocusBlend = 0f;
                    _bossSceneZoom = CameraZoom;
                    if (UpdateDialogue(dt, click))
                    {
                        _bossStage = BossSceneStage.CameraToBoss;
                        _bossStageTimer = 0f;
                    }
                    break;

                case BossSceneStage.CameraToBoss:
                    _bossStageTimer += dt;
                    {
                        float t = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_bossStageTimer / BossSceneCameraPanDuration, 0f, 1f));
                        _bossFocusBlend = t;
                        _bossSceneZoom = MathHelper.Lerp(CameraZoom, BossSceneBossZoom, t);
                    }
                    if (_bossStageTimer >= BossSceneCameraPanDuration)
                    {
                        _bossFocusBlend = 1f;
                        _bossSceneZoom = BossSceneBossZoom;
                        _bossShakeActive = true;
                        _bossShakeTime = 0f;
                        _bossStage = BossSceneStage.Shake;
                        _bossStageTimer = 0f;
                    }
                    break;

                case BossSceneStage.Shake:
                    _bossStageTimer += dt;
                    _bossFocusBlend = 1f;
                    _bossSceneZoom = BossSceneBossZoom;
                    if (_bossStageTimer >= BossSceneShakeDuration)
                    {
                        StartDialogue(BossDialogue2Lines, "William Jones");
                        _bossStage = BossSceneStage.Dialogue2;
                    }
                    break;

                case BossSceneStage.Dialogue2:
                    _bossFocusBlend = 1f;
                    _bossSceneZoom = BossSceneBossZoom;
                    if (UpdateDialogue(dt, click))
                    {
                        _bossShakeActive = false;
                        _bossStage = BossSceneStage.ZoomOut;
                        _bossStageTimer = 0f;
                    }
                    break;

                case BossSceneStage.ZoomOut:
                    _bossStageTimer += dt;
                    {
                        float t = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_bossStageTimer / BossSceneZoomOutDuration, 0f, 1f));
                        _bossFocusBlend = 1f - t;
                        _bossSceneZoom = MathHelper.Lerp(BossSceneBossZoom, BossFightZoom, t);
                    }
                    if (_bossStageTimer >= BossSceneZoomOutDuration)
                    {
                        EndBossScene();
                        return;
                    }
                    break;
            }

            _cameraZoom = _bossSceneZoom;
        }

        private void EndBossScene()
        {
            _isBossScene = false;
            _isDialogueActive = false;
            _bossShakeActive = false;
            _bossFocusBlend = 0f;

            // เริ่มต่อสู้: กล้องค้างที่ซูมออกจนกว่าบอสจะตาย
            _bossFightActive = true;
            _bossFightZoomBlend = 1f;
            _cameraZoom = BossFightZoom;

            // ปลุกบอสให้กลับไปใช้ AI ปกติ: ไล่โจมตี Kai (หน่วงการโจมตีครั้งแรกเล็กน้อย)
            if (_bossSceneBoss != null)
            {
                _bossSceneBoss.IsStatue = false;
                _bossSceneBoss.IsAggro = true;
                _bossSceneBoss.State = Boss1State.Walking;
                _bossSceneBoss.AnimTimer = 0f;
                _bossSceneBoss.FrameIndex = 0;
                _bossSceneBoss.AttackCooldownTimer = 1f;
            }

            UpdateCamera();
        }

        // ซูมกล้องตอนสู้บอส: ซูมออกเมื่อกำลังสู้และอยู่ในห้องบอส / กลับมาซูมปกติเมื่อบอสตาย (เริ่มตายก็ซูมคืนทันที) หรือออกจากห้อง
        private void UpdateBossFightZoom(float dt)
        {
            if (_bossFightActive && (_bossSceneBoss == null || _bossSceneBoss.IsDying || !_boss1List.Contains(_bossSceneBoss)))
                _bossFightActive = false;

            float previousBlend = _bossFightZoomBlend;
            float target = (_bossFightActive && _currentRoomIndex == BossSceneRoomIndex) ? 1f : 0f;
            float step = dt / BossFightZoomBlendDuration;

            if (_bossFightZoomBlend < target)
                _bossFightZoomBlend = MathHelper.Min(target, _bossFightZoomBlend + step);
            else if (_bossFightZoomBlend > target)
                _bossFightZoomBlend = MathHelper.Max(target, _bossFightZoomBlend - step);

            // ไม่ยุ่งกับ _cameraZoom ถ้าไม่เกี่ยวกับฉากบอส (ฉากอื่นคุมซูมของตัวเอง)
            if (_bossFightZoomBlend > 0f || previousBlend > 0f)
                _cameraZoom = MathHelper.Lerp(CameraZoom, BossFightZoom, MathHelper.SmoothStep(0f, 1f, _bossFightZoomBlend));
        }

        private void DrawBossScenePrompt()
        {
            if (_isBossScene || !IsKaiNearBossStatue())
                return;
            if (_isTransitioning || _isWakeUpSequence || _isOrbScene || _isDoorInteraction
                || _isInventoryOpen || _isConsoleOpen || _isGiveItemMenuOpen)
                return;

            const string prompt = "Press E";
            const float promptScale = 0.7f;
            Vector2 promptSize = _hotbarFont.MeasureString(prompt) * promptScale;
            var promptPosition = new Vector2(
                _bossSceneBoss.X - promptSize.X / 2f,
                _floorY + _bossSceneBoss.YOffset - _boss1Height - promptSize.Y - 12f);
            _spriteBatch.DrawString(_hotbarFont, prompt, promptPosition, Color.White, 0f, Vector2.Zero, promptScale, SpriteEffects.None, 0f);
        }

        // ==================== Game Sounds ====================

        private static SoundEffect TryLoadSoundEffect(string fileName)
        {
            try
            {
                string path = Path.Combine(System.AppContext.BaseDirectory, SoundsFolder, fileName);
                if (!File.Exists(path))
                    return null;

                using (var stream = File.OpenRead(path))
                    return SoundEffect.FromStream(stream);
            }
            catch
            {
                return null;
            }
        }

        private void LoadGameSounds()
        {
            LoadAudioSettings();

            _footstepSoundEffect = TryLoadSoundEffect(FootstepSoundFile);
            if (_footstepSoundEffect != null)
            {
                _footstepInstance = _footstepSoundEffect.CreateInstance();
                _footstepInstance.IsLooped = true;
                _footstepInstance.Volume = FootstepVolume * SfxGain;
            }

            _pickupSoundEffect = TryLoadSoundEffect(PickupSoundFile);
            _jumpSoundEffect = TryLoadSoundEffect(JumpSoundFile);
            _impactSoundEffect = TryLoadSoundEffect(ImpactSoundFile);
            _itemLandSoundEffect = TryLoadSoundEffect(ItemLandSoundFile);
            _bookOpenSoundEffect = TryLoadSoundEffect(BookOpenSoundFile);

            _swingSoundEffect = TryLoadSoundEffect(SwingSoundFile);
            _swingSoundInstance = _swingSoundEffect != null ? _swingSoundEffect.CreateInstance() : null;
            _wrenchHitSoundEffect = TryLoadSoundEffect(WrenchHitSoundFile);
            _eatSoundEffect = TryLoadSoundEffect(EatSoundFile);
            _potionSoundEffect = TryLoadSoundEffect(PotionSoundFile);
            _monster1RoarSoundEffect = TryLoadSoundEffect(Monster1RoarSoundFile);
            _monsterDeathSoundEffect = TryLoadSoundEffect(MonsterDeathSoundFile);
            _monster2RoarSoundEffect = TryLoadSoundEffect(Monster2RoarSoundFile);
            _kaiHurtSoundEffect = TryLoadSoundEffect(KaiHurtSoundFile);
            _swordSwingSoundEffect = TryLoadSoundEffect(SwordSwingSoundFile);
            _doorOpenSoundEffect = TryLoadSoundEffect(DoorOpenSoundFile);
            _bossOrbSoundEffect = TryLoadSoundEffect(BossOrbSoundFile);

            _stage2MusicEffect = TryLoadSoundEffect(Stage2MusicFile);
            if (_stage2MusicEffect != null)
            {
                _stage2MusicInstance = _stage2MusicEffect.CreateInstance();
                _stage2MusicInstance.IsLooped = true;
            }

            _boss2MusicEffect = TryLoadSoundEffect(Boss2MusicFile);
            if (_boss2MusicEffect != null)
            {
                _boss2MusicInstance = _boss2MusicEffect.CreateInstance();
                _boss2MusicInstance.IsLooped = true;
            }

            _stage3MusicEffect = TryLoadSoundEffect(Stage3MusicFile);
            if (_stage3MusicEffect != null)
            {
                _stage3MusicInstance = _stage3MusicEffect.CreateInstance();
                _stage3MusicInstance.IsLooped = true;
            }

            _itemAuraSoundEffect = TryLoadSoundEffect(ItemAuraSoundFile);
            if (_itemAuraSoundEffect != null)
            {
                _itemAuraInstance = _itemAuraSoundEffect.CreateInstance();
                _itemAuraInstance.IsLooped = true;
                _itemAuraInstance.Volume = 0f;
            }
        }

        private void UpdateGameSounds(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            UpdateFootstepSound(deltaSeconds);
            UpdateItemAuraSound(deltaSeconds);
        }

        // เสียงก้าวเท้า: ลูปเฉพาะตอนเดิน/วิ่งอยู่บนพื้น (ไม่เล่นกลางอากาศ/ตอนลงพื้น/ตอนเปลี่ยนห้อง)
        // เมื่อเงื่อนไขหาย (หยุดกด A/D, กระโดด ฯลฯ) เสียงจะค่อยๆ จางหายใน FootstepFadeOutDuration แทนการตัดทันที
        private void UpdateFootstepSound(float deltaSeconds)
        {
            if (_footstepInstance == null)
                return;

            bool shouldPlay = _isMoving && !_isJumping && !_isLanding && !_isTransitioning;
            bool isPlaying = _footstepInstance.State == SoundState.Playing;

            if (shouldPlay)
            {
                _footstepInstance.Pitch = _isSprinting ? FootstepRunPitch : FootstepWalkPitch;

                if (!isPlaying)
                {
                    // เริ่มเล่นใหม่จากเงียบ: ดังเต็มทันที ให้ได้ยินก้าวแรกพร้อมการกดเดิน
                    _footstepFade = 1f;
                    _footstepInstance.Volume = FootstepVolume * SfxGain;
                    _footstepInstance.Play();
                    return;
                }

                // กำลังจางอยู่แล้วกดเดินต่อ: ดังกลับขึ้นเต็มอย่างรวดเร็ว
                _footstepFade = MathHelper.Min(1f, _footstepFade + deltaSeconds / FootstepFadeInDuration);
            }
            else
            {
                if (!isPlaying)
                {
                    _footstepFade = 0f;
                    return;
                }

                _footstepFade = MathHelper.Max(0f, _footstepFade - deltaSeconds / FootstepFadeOutDuration);

                if (_footstepFade <= 0f)
                {
                    _footstepInstance.Stop();
                    return;
                }
            }

            _footstepInstance.Volume = FootstepVolume * _footstepFade * SfxGain;
        }

        // เสียงออร่า: หาไอเทมบนพื้นในห้องนี้ที่ใกล้ที่สุด (ระยะแนวนอน+แนวตั้ง) ยิ่งใกล้ยิ่งดัง นอกระยะ = เงียบและหยุดเล่น
        private void UpdateItemAuraSound(float deltaSeconds)
        {
            if (_itemAuraInstance == null)
                return;

            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            float nearestDistance = float.MaxValue;

            foreach (var item in _worldItems)
            {
                if (item.Collected || item.Room != _currentRoomIndex)
                    continue;

                float dx = item.X - _kaiCenterX;
                float dy = GetWorldItemGroundY(item) - kaiFeetY;
                float distance = (float)System.Math.Sqrt(dx * dx + dy * dy);

                if (distance < nearestDistance)
                    nearestDistance = distance;
            }

            float targetVolume = nearestDistance < ItemAuraRange
                ? ItemAuraMaxVolume * (1f - nearestDistance / ItemAuraRange)
                : 0f;

            _itemAuraCurrentVolume = MathHelper.Lerp(
                _itemAuraCurrentVolume,
                targetVolume,
                MathHelper.Clamp(deltaSeconds * ItemAuraVolumeLerpSpeed, 0f, 1f));

            if (_itemAuraCurrentVolume < 0.01f && targetVolume < 0.01f)
            {
                _itemAuraCurrentVolume = 0f;
                if (_itemAuraInstance.State == SoundState.Playing)
                    _itemAuraInstance.Stop();
                return;
            }

            _itemAuraInstance.Volume = MathHelper.Clamp(_itemAuraCurrentVolume * SfxGain, 0f, 1f);

            if (_itemAuraInstance.State != SoundState.Playing)
                _itemAuraInstance.Play();
        }

        // ใช้ตอน Game Over: หยุดเสียงลูปทั้งหมด
        private void StopGameLoopSounds()
        {
            if (_footstepInstance != null && _footstepInstance.State == SoundState.Playing)
                _footstepInstance.Stop();
            _footstepFade = 0f;

            if (_itemAuraInstance != null && _itemAuraInstance.State == SoundState.Playing)
                _itemAuraInstance.Stop();

            _itemAuraCurrentVolume = 0f;
        }

        // แสดงเคอร์เซอร์เมาส์เฉพาะตอนที่ต้องใช้เมาส์จริงๆ (เมนูหลัก / เมนู Esc / Setting / Game Over / กระเป๋า / เมนู giveme_items / ปุ่ม Yes-No ของประตู)
        // ตอนเล่นปกติ, Loading, วิดีโอ intro, บทพูด, สมุด, หน้ากรอกรหัส, คอนโซล, ง้าง Trident จะซ่อนเคอร์เซอร์
        // ซ่อนแค่ตัวเคอร์เซอร์ การอ่านตำแหน่งเมาส์ (Mouse.GetState / GetLogicalMousePosition) ยังทำงานปกติ
        private bool ShouldShowMouseCursor()
        {
            if (_appState == AppState.MainMenu)
                return true;   // เมนูหลัก (รวมหน้า Setting จากเมนูหลัก)

            if (_appState != AppState.Playing)
                return false;  // Loading / วิดีโอ intro

            return _isPauseMenuOpen                                        // เมนู Esc (รวมหน้า Setting)
                || _deathState == PlayerDeathState.GameOver                // หน้า Game Over
                || _isInventoryOpen                                        // กระเป๋า
                || _isGiveItemMenuOpen                                     // เมนู /giveme_items
                || (_isDoorInteraction && _doorStage == DoorStage.Confirm); // ปุ่ม Yes/No ของประตู
        }

        protected override void Update(GameTime gameTime)
        {
            IsMouseVisible = ShouldShowMouseCursor();

            int scrollNow = Mouse.GetState().ScrollWheelValue;
            _scrollDeltaThisFrame = scrollNow - _previousScrollWheelValue;
            _previousScrollWheelValue = scrollNow;

            UpdateMusic(gameTime);

            if (_appState == AppState.MainMenu)
            {
                _mouseState = Mouse.GetState();
                UpdateFullscreenToggle();
                UpdateMainMenu();
                _menuEffectTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                _previousMouseState = _mouseState;
                base.Update(gameTime);
                return;
            }

            if (_appState == AppState.Loading)
            {
                UpdateFullscreenToggle();
                UpdateLoading(gameTime);
                base.Update(gameTime);
                return;
            }

            if (_appState == AppState.IntroVideo)
            {
                UpdateFullscreenToggle();
                UpdateIntroVideo(gameTime);
                base.Update(gameTime);
                return;
            }

            if (_isWakeUpSequence)
            {
                // คลิกซ้าย (เพิ่งกด) ใช้เดินบทพูด
                _mouseState = Mouse.GetState();
                bool wakeClick = _mouseState.LeftButton == ButtonState.Pressed
                    && _previousMouseState.LeftButton == ButtonState.Released;

                UpdateWakeUpSequence(gameTime, wakeClick);
                UpdateFullscreenToggle();

                // ซิงค์สถานะปุ่ม/เมาส์ทุกเฟรม กันปุ่มที่กดค้างไว้ (เช่น Space ตอนข้ามวิดีโอ) ถูกนับเป็น "เพิ่งกด" ทันทีที่ฉากจบ
                SyncAllInputStates();

                base.Update(gameTime);
                return;
            }

            if (_isOrbScene)
            {
                // ฉาก orb: ล็อกผู้เล่นทั้งหมด คลิกซ้ายไปบทพูดถัดไป
                UpdateLockedScene(gameTime, UpdateOrbScene);
                return;
            }

            if (_isDoorInteraction)
            {
                // ประตู: ล็อกผู้เล่นทั้งหมด คลิกซ้ายเดินบทพูด / เลือก Yes-No
                UpdateLockedScene(gameTime, UpdateDoorInteraction);
                return;
            }

            if (_isPasscodeOpen)
            {
                // หน้าต่างกรอกรหัสประตูห้องที่สองของด่าน 2: ล็อกผู้เล่น พิมพ์เลข 0-9 / Enter ยืนยัน / Esc ยกเลิก
                UpdateLockedScene(gameTime, UpdatePasscode);
                return;
            }

            if (_isSignReading)
            {
                // อ่านป้ายใบ้รหัส: ล็อกผู้เล่น คลิกซ้ายเดินบทพูด
                UpdateLockedScene(gameTime, UpdateSignReading);
                return;
            }

            if (_isBossScene)
            {
                // ฉากบอส: ล็อกผู้เล่นทั้งหมด (ดูรายละเอียดที่ UpdateLockedScene)
                UpdateLockedScene(gameTime, UpdateBossScene);
                return;
            }

            if (_isBoss3Scene)
            {
                UpdateLockedScene(gameTime, UpdateBoss3Scene);
                return;
            }

            if (_isBoss2Scene)
            {
                // ฉากเจอบอส 2 (ห้องที่ 3 ของด่าน 2): ล็อกผู้เล่นทั้งหมดเหมือนฉากบอส 1
                UpdateLockedScene(gameTime, UpdateBoss2Scene);
                return;
            }

            if (_deathState == PlayerDeathState.GameOver)
            {
                StopGameLoopSounds();
                UpdateGameOverState();
                base.Update(gameTime);
                return;
            }

            if (_isPauseMenuOpen)
            {
                // เมนู Esc เปิดอยู่: โลกเกมหยุดนิ่งทั้งหมด (ไม่อัปเดตผู้เล่น/มอน/เวลา/คอนโซล)
                UpdateFullscreenToggle();
                UpdatePauseMenu();
                base.Update(gameTime);
                return;
            }

            if (_isBookOpen)
            {
                // สมุดเปิดอยู่: โลกเกมหยุดนิ่งทั้งหมด (กด I หรือ Esc เพื่อปิด)
                UpdateFullscreenToggle();
                UpdateBookScreen();
                base.Update(gameTime);
                return;
            }

            UpdateConsole(gameTime);

            var keyboardState = Keyboard.GetState();
            if (!keyboardState.IsKeyDown(Keys.Escape))
                _escapeHeldFromConsole = false;

            bool escapeJustPressed = keyboardState.IsKeyDown(Keys.Escape) && !_previousItemKeyboardState.IsKeyDown(Keys.Escape);
            if (escapeJustPressed)
            {
                if (_isGiveItemMenuOpen)
                {
                    if (_giveItemQuantityStep)
                    {
                        // ถอยกลับไปหน้าเลือกไอเทมก่อน แทนที่จะปิดเมนูทั้งหมด
                        _giveItemQuantityStep = false;
                        _isDraggingGiveSlider = false;
                    }
                    else
                    {
                        _isGiveItemMenuOpen = false;
                    }

                    _escapeHeldFromConsole = true;
                }
                else if (_isInventoryOpen)
                {
                    // กด Esc ปิดหน้า Inventory ได้เหมือนกดปุ่ม Tab ซ้ำ
                    _isInventoryOpen = false;
                    _escapeHeldFromConsole = true;
                }
                else if (!_isConsoleOpen && !_escapeHeldFromConsole)
                {
                    // ไม่มี UI อื่นเปิดอยู่ -> เปิดเมนู Esc (Start / Setting / Back (Menu))
                    OpenPauseMenu();
                    base.Update(gameTime);
                    return;
                }
            }

            // กด I = เปิดสมุด (ไม่เปิดตอนมี UI อื่นอยู่: คอนโซล / เมนู give / กระเป๋า)
            bool bookJustPressed = keyboardState.IsKeyDown(Keys.I) && !_previousBookKeyboardState.IsKeyDown(Keys.I);
            _previousBookKeyboardState = keyboardState;
            if (bookJustPressed && !_isConsoleOpen && !_isGiveItemMenuOpen && !_isInventoryOpen)
            {
                OpenBook();
                base.Update(gameTime);
                return;
            }

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            // อ่านค่า Mouse State เป็นอันดับแรกสุด ป้องกันบัฟเฟอร์คลิกค้าง
            _mouseState = Mouse.GetState();

            UpdateGiveItemMenu();
            UpdateInventoryToggle();
            UpdateFullscreenToggle();

            EnsureStonePuzzle();
            UpdateDoorInput();
            UpdateBossExitDoorInput();
            UpdateSavePointInput();
            UpdateStonePuzzleInput();
            UpdateBossSceneInput();
            UpdateBoss2Encounter();
            UpdateBoss3Encounter((float)gameTime.ElapsedGameTime.TotalSeconds);
            UpdateItemPickup();
            UpdateHotbarKeys();
            UpdateHotbarScroll();
            UpdateInventoryDragDrop();

            GameTime worldTime = gameTime;

            UpdateSeaweedBuff(worldTime);
            UpdateJumpBuff(worldTime);
            UpdateKeyIdleAnimation(worldTime);
            _orbBobTime += (float)worldTime.ElapsedGameTime.TotalSeconds;

            if (_isTransitioning)
            {
                UpdateSceneTransition(worldTime);
            }

            UpdateKaiStun(worldTime);
            UpdateKaiMovement(worldTime);
            UpdateKaiJump(worldTime);
            UpdateStamina(worldTime);
            UpdateKaiAnimation(worldTime);
            UpdateParry(worldTime);
            UpdateHitboxDebugToggle();
            UpdateAttack(worldTime);
            UpdateTridentThrow(worldTime);
            UpdateTridentProjectiles(worldTime);
            UpdateStuckTridents();
            UpdateDummy(worldTime);
            UpdateIceGolems(worldTime);
            UpdateMonster1List(worldTime);
            UpdateMonsterWaves((float)worldTime.ElapsedGameTime.TotalSeconds);
            UpdateBoss1List(worldTime);
            UpdateBoss2List(worldTime);
            UpdateBoss3List(worldTime);
            UpdateBoss3FightZoom((float)gameTime.ElapsedGameTime.TotalSeconds);
            UpdateBoss2Swords(worldTime);
            UpdateMonster2List(worldTime);
            UpdateStage2Waves(worldTime);
            UpdateBossOrbs(worldTime);
            UpdateBossFightZoom((float)gameTime.ElapsedGameTime.TotalSeconds);
            UpdateBoss2FightZoom((float)gameTime.ElapsedGameTime.TotalSeconds);
            UpdateCamera();
            UpdateGameSounds(gameTime);

            _previousItemKeyboardState = Keyboard.GetState();
            _previousMouseState = _mouseState;

            if (_isGodMode)
            {
                _currentHP = _maxHP;
                _currentStamina = _maxStamina;
                _isExhausted = false;
            }

            if (_currentHP <= 0f)
                TriggerPlayerDeath();

            base.Update(gameTime);
        }

        // ==================== Setting / เมนู Esc ====================

        private float SfxGain => _masterVolume * _sfxVolume;
        private float MusicGain => _masterVolume * _musicVolume;

        // เล่นเสียงเอฟเฟกต์ผ่านตัวคูณ Master x SFX
        private void PlaySfx(SoundEffect effect, float volume)
        {
            if (effect == null)
                return;

            effect.Play(MathHelper.Clamp(volume * SfxGain, 0f, 1f), 0f, 0f);
        }

        // Monster1 เปลี่ยนจากยังไม่เจอ Kai เป็นเริ่มไล่: เล่นเสียงร้อง (จำกัดความถี่เมื่อเจอพร้อมกันหลายตัว)
        private void AggroMonster1(Monster1 monster)
        {
            if (monster.IsAggro)
                return;

            monster.IsAggro = true;

            long now = System.Environment.TickCount64;
            if (now < _monster1RoarNextAllowedTick)
                return;

            _monster1RoarNextAllowedTick = now + Monster1RoarMinIntervalMs;
            PlaySfx(_monster1RoarSoundEffect, Monster1RoarSoundVolume);
        }

        // Monster2 เริ่มไล่ Kai: ร้องครั้งเดียวต่อตัว (จำกัดความถี่เมื่อเจอพร้อมกันหลายตัว) ตัวที่ตั้งให้ไล่ตลอดก็นับตอนเริ่มไล่ครั้งแรก
        private void AggroMonster2(Monster2 monster)
        {
            monster.IsAggro = true;

            if (monster.HasRoared)
                return;

            monster.HasRoared = true;

            long now = System.Environment.TickCount64;
            if (now < _monster2RoarNextAllowedTick)
                return;

            _monster2RoarNextAllowedTick = now + Monster1RoarMinIntervalMs;
            PlaySfx(_monster2RoarSoundEffect, Monster2RoarSoundVolume);
        }

        // Kai เสียเลือดจากศัตรู (หรือคำสั่ง /hit_me): เล่นเสียงโดนตี ทั้งครั้งที่ตาย ปัดป้องสำเร็จ (ไม่เสียเลือด) ไม่เล่น
        private void PlayKaiHurtSound(float damage)
        {
            if (damage <= 0f)
                return;

            PlaySfx(_kaiHurtSoundEffect, KaiHurtSoundVolume);
        }

        // เพลงที่ควรดังในตอนนี้ ตัดสินจากสถานะเกมทุกเฟรม
        private MusicTrack GetDesiredMusicTrack()
        {
            if (_appState != AppState.Playing || _deathState != PlayerDeathState.Alive)
                return MusicTrack.None;

            // เจอบอส 2 (ฉากเปิดตัวจนถึงบอสเริ่มตาย)
            if (_currentRoomIndex == Level2LastRoomIndex
                && _boss2SceneBoss != null && !_boss2SceneBoss.IsDying && _boss2List.Contains(_boss2SceneBoss))
                return MusicTrack.Boss2;

            // ด่าน 3: เล่นเพลงด่าน 3 ตั้งแต่เข้าห้อง (ตอนบอส 2 ตายแล้วอยู่ในห้องบอสยังเงียบ จนกว่าจะเดินเข้าด่าน 3)
            if (IsLevel3Room(_currentRoomIndex))
                return MusicTrack.Stage3;

            // บอส 2 ตายแล้ว (ยังอยู่ในห้องบอส): เงียบ
            if (_boss2Defeated)
                return MusicTrack.None;

            return IsLevel2Room(_currentRoomIndex) ? MusicTrack.Stage2 : MusicTrack.None;
        }

        private static void PauseMusicInstance(SoundEffectInstance instance)
        {
            if (instance != null && instance.State == SoundState.Playing)
                instance.Pause();
        }

        private static void ResumeMusicInstance(SoundEffectInstance instance)
        {
            if (instance != null && instance.State == SoundState.Paused)
                instance.Resume();
        }

        private void UpdateMusicTrack(SoundEffectInstance instance, ref float level, bool wanted, float baseVolume, float dt)
        {
            if (instance == null)
                return;

            float step = dt / MusicFadeSeconds;
            level = wanted ? System.Math.Min(1f, level + step) : System.Math.Max(0f, level - step);

            if (level <= 0f)
            {
                if (instance.State != SoundState.Stopped)
                    instance.Stop();   // เงียบสนิทแล้ว: หยุด (ครั้งหน้าจะเริ่มเพลงจากต้น)
                return;
            }

            instance.Volume = MathHelper.Clamp(baseVolume * level * MusicGain, 0f, 1f);
            if (instance.State == SoundState.Stopped)
                instance.Play();
        }

        private void UpdateMusic(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // เมนู Esc เปิดอยู่: หยุดเพลงชั่วคราว (ไม่เฟด) แล้วเล่นต่อตอนปิดเมนู
            bool menuPause = _isPauseMenuOpen && _appState == AppState.Playing;
            if (menuPause)
            {
                if (!_musicPausedByMenu)
                {
                    PauseMusicInstance(_stage2MusicInstance);
                    PauseMusicInstance(_boss2MusicInstance);
                    PauseMusicInstance(_stage3MusicInstance);
                    _musicPausedByMenu = true;
                }
                return;
            }

            if (_musicPausedByMenu)
            {
                ResumeMusicInstance(_stage2MusicInstance);
                ResumeMusicInstance(_boss2MusicInstance);
                ResumeMusicInstance(_stage3MusicInstance);
                _musicPausedByMenu = false;
            }

            MusicTrack desired = GetDesiredMusicTrack();
            UpdateMusicTrack(_stage2MusicInstance, ref _stage2MusicLevel, desired == MusicTrack.Stage2, Stage2MusicVolume, dt);
            UpdateMusicTrack(_boss2MusicInstance, ref _boss2MusicLevel, desired == MusicTrack.Boss2, Boss2MusicVolume, dt);
            UpdateMusicTrack(_stage3MusicInstance, ref _stage3MusicLevel, desired == MusicTrack.Stage3, Stage3MusicVolume, dt);
        }

        private void PlaySwordSwingSound()
        {
            PlaySfx(_swordSwingSoundEffect, SwordSwingSoundVolume);
        }

        // ประตูถูกเปิด: ใช้เสียงเปิดประตู ถ้ายังไม่มีไฟล์ใช้เสียงเก็บของเหมือนเดิม
        private void PlayDoorOpenSound()
        {
            if (_doorOpenSoundEffect != null)
                PlaySfx(_doorOpenSoundEffect, DoorOpenSoundVolume);
            else
                PlaySfx(_pickupSoundEffect, PickupSoundVolume);
        }

        // เสียงฟาดอาวุธ (ใช้กับอาวุธทุกชนิด): เริ่มใหม่ทุกครั้งที่เรียก ไม่ซ้อนกับของเดิม
        private void PlaySwingSound()
        {
            if (_swingSoundInstance == null)
                return;

            _swingSoundInstance.Stop();
            _swingSoundInstance.Volume = MathHelper.Clamp(SwingSoundVolume * SfxGain, 0f, 1f);
            _swingSoundInstance.Play();
        }

        private static string GetSettingsFilePath()
        {
            string folder = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "Kaeng_Khiao_Wan_Kai");
            return Path.Combine(folder, "settings.cfg");
        }

        private void LoadAudioSettings()
        {
            try
            {
                string path = GetSettingsFilePath();
                if (!File.Exists(path))
                    return;

                foreach (string line in File.ReadAllLines(path))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length != 2)
                        continue;

                    float value;
                    if (!float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out value))
                        continue;

                    value = MathHelper.Clamp(value, 0f, 1f);

                    switch (parts[0].Trim().ToLowerInvariant())
                    {
                        case "master": _masterVolume = value; break;
                        case "sfx": _sfxVolume = value; break;
                        case "music": _musicVolume = value; break;
                    }
                }
            }
            catch
            {
                // อ่านไฟล์ไม่ได้ -> ใช้ค่าเริ่มต้น (ดังเต็ม)
            }
        }

        private void SaveAudioSettings()
        {
            try
            {
                string path = GetSettingsFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                var inv = System.Globalization.CultureInfo.InvariantCulture;
                File.WriteAllLines(path, new[]
                {
                    "master=" + _masterVolume.ToString("0.00", inv),
                    "sfx=" + _sfxVolume.ToString("0.00", inv),
                    "music=" + _musicVolume.ToString("0.00", inv)
                });
            }
            catch
            {
                // เซฟไม่ได้ก็ข้ามไป (เกมเล่นต่อได้ปกติ)
            }
        }

        // ---------- หน้า Setting (Master / SFX / Music + ปุ่ม Back (Menu)) ----------

        private const int SettingsPanelWidth = 640;
        private const int SettingsPanelHeight = 520;
        private const int SettingsTrackWidth = 300;
        private const int SettingsTrackHeight = 12;
        private const int SettingsKnobSize = 28;
        private static readonly string[] SettingsSliderLabels = { "MASTER", "SFX", "MUSIC" };

        private struct SettingsLayout
        {
            public Rectangle PanelRect;
            public Rectangle[] TrackRects;
            public Rectangle BackRect;
        }

        private SettingsLayout GetSettingsLayout()
        {
            var panel = new Rectangle(
                (WindowedWidth - SettingsPanelWidth) / 2,
                (WindowedHeight - SettingsPanelHeight) / 2,
                SettingsPanelWidth,
                SettingsPanelHeight);

            var tracks = new Rectangle[3];
            int trackX = panel.X + 190;
            for (int i = 0; i < tracks.Length; i++)
            {
                int centerY = panel.Y + 140 + i * 90;
                tracks[i] = new Rectangle(trackX, centerY - SettingsTrackHeight / 2, SettingsTrackWidth, SettingsTrackHeight);
            }

            int backWidth = (int)(_btnMenuTexture.Width * MenuButtonScale);
            int backHeight = (int)(_btnMenuTexture.Height * MenuButtonScale);
            var back = new Rectangle((WindowedWidth - backWidth) / 2, panel.Bottom - backHeight - 30, backWidth, backHeight);

            return new SettingsLayout { PanelRect = panel, TrackRects = tracks, BackRect = back };
        }

        private void SetSliderValue(int index, float value)
        {
            switch (index)
            {
                case 0: _masterVolume = value; break;
                case 1: _sfxVolume = value; break;
                case 2: _musicVolume = value; break;
            }
        }

        // คืน true เมื่อผู้เล่นกดปุ่ม Back (Menu)
        private bool UpdateSettingsScreen(bool leftPressed, bool leftJustPressed, Vector2 mouse)
        {
            var layout = GetSettingsLayout();

            if (leftJustPressed)
            {
                for (int i = 0; i < layout.TrackRects.Length; i++)
                {
                    Rectangle track = layout.TrackRects[i];
                    var hitRect = new Rectangle(
                        track.X - SettingsKnobSize / 2,
                        track.Y - 16,
                        track.Width + SettingsKnobSize,
                        track.Height + 32);

                    if (hitRect.Contains(mouse))
                    {
                        _settingsDraggingSlider = i;
                        break;
                    }
                }

                if (_settingsDraggingSlider < 0 && layout.BackRect.Contains(mouse))
                    return true;
            }

            if (_settingsDraggingSlider >= 0)
            {
                Rectangle track = layout.TrackRects[_settingsDraggingSlider];

                if (leftPressed)
                {
                    float t = MathHelper.Clamp((mouse.X - track.X) / (float)track.Width, 0f, 1f);
                    t = (float)System.Math.Round(t * 100f) / 100f;
                    SetSliderValue(_settingsDraggingSlider, t);
                }
                else
                {
                    int released = _settingsDraggingSlider;
                    _settingsDraggingSlider = -1;
                    SaveAudioSettings();

                    // ปล่อยสไลเดอร์ Master/SFX: เล่นเสียงตัวอย่างให้ได้ยินระดับเสียงที่ตั้ง
                    if (released != 2)
                        PlaySfx(_pickupSoundEffect, PickupSoundVolume);
                }
            }

            return false;
        }

        private void CloseSettingsScreen()
        {
            _isSettingsOpen = false;
            _settingsDraggingSlider = -1;
            SaveAudioSettings();
        }

        // วาดหน้า Setting (ต้องเรียกภายใน SpriteBatch.Begin/End ที่ใช้พิกัดตรรกะ WindowedWidth x WindowedHeight)
        private void DrawSettingsContent()
        {
            var layout = GetSettingsLayout();
            Vector2 mouse = GetLogicalMousePosition();
            Rectangle panel = layout.PanelRect;

            DrawFilledRect(panel, new Color(0, 0, 0, 200));
            DrawRectOutline(panel, 2, new Color(255, 255, 255, 120));

            const string title = "SETTINGS";
            const float titleScale = 1.6f;
            Vector2 titleSize = _hotbarFont.MeasureString(title) * titleScale;
            _spriteBatch.DrawString(_hotbarFont, title,
                new Vector2(panel.X + (panel.Width - titleSize.X) / 2f, panel.Y + 28),
                Color.White, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

            float[] values = { _masterVolume, _sfxVolume, _musicVolume };

            for (int i = 0; i < layout.TrackRects.Length; i++)
            {
                Rectangle track = layout.TrackRects[i];

                Vector2 labelSize = _hotbarFont.MeasureString(SettingsSliderLabels[i]);
                _spriteBatch.DrawString(_hotbarFont, SettingsSliderLabels[i],
                    new Vector2(panel.X + 40, track.Center.Y - labelSize.Y / 2f), Color.White);

                DrawFilledRect(track, new Color(70, 70, 70, 255));

                int fillWidth = (int)(track.Width * values[i]);
                DrawFilledRect(new Rectangle(track.X, track.Y, fillWidth, track.Height), new Color(120, 200, 255, 255));

                var knob = new Rectangle(
                    track.X + fillWidth - SettingsKnobSize / 2,
                    track.Center.Y - SettingsKnobSize / 2,
                    SettingsKnobSize,
                    SettingsKnobSize);
                bool knobActive = _settingsDraggingSlider == i || knob.Contains(mouse);
                DrawFilledRect(knob, knobActive ? Color.White : new Color(200, 200, 200, 255));

                string percentText = ((int)System.Math.Round(values[i] * 100f)) + "%";
                Vector2 percentSize = _hotbarFont.MeasureString(percentText);
                _spriteBatch.DrawString(_hotbarFont, percentText,
                    new Vector2(track.Right + 20, track.Center.Y - percentSize.Y / 2f), Color.White);
            }

            _spriteBatch.Draw(_btnMenuTexture, layout.BackRect, layout.BackRect.Contains(mouse) ? Color.White : Color.Gray);
        }

        // ---------- ฮอตบาร์มุมล่างซ้าย (แสดงผลอย่างเดียว) ----------

        private const float HotbarHudScale = 0.22f;          // ย่อรูป hot_bar.png (1920x580) -> กว้างราว 420px ช่องละราว 40px
        private const int HotbarHudMarginX = 12;
        private const int HotbarHudMarginBottom = 4;
        // ตำแหน่งช่องในรูปต้นฉบับ (พิกเซล): กึ่งกลางช่องแรก, ระยะห่างระหว่างช่อง, กึ่งกลางแนวตั้ง, ขนาดช่อง
        private const float HotbarHudSlotFirstCenterX = 146f;
        private const float HotbarHudSlotPitch = 198.7f;
        private const float HotbarHudSlotCenterY = 327f;
        private const float HotbarHudSlotSize = 186f;

        private Rectangle GetHotbarHudBarRect()
        {
            int width = (int)(_hotbarHudTexture.Width * HotbarHudScale);
            int height = (int)(_hotbarHudTexture.Height * HotbarHudScale);
            return new Rectangle(HotbarHudMarginX, WindowedHeight - height - HotbarHudMarginBottom, width, height);
        }

        private Rectangle GetHotbarHudSlotRect(Rectangle barRect, int index)
        {
            int size = (int)System.Math.Round(HotbarHudSlotSize * HotbarHudScale);
            float centerX = barRect.X + (HotbarHudSlotFirstCenterX + index * HotbarHudSlotPitch) * HotbarHudScale;
            float centerY = barRect.Y + HotbarHudSlotCenterY * HotbarHudScale;
            return new Rectangle(
                (int)System.Math.Round(centerX - size / 2f),
                (int)System.Math.Round(centerY - size / 2f),
                size,
                size);
        }

        private void DrawHotbarHud()
        {
            if (_hotbarHudTexture == null)
                return;

            Rectangle barRect = GetHotbarHudBarRect();
            _spriteBatch.Draw(_hotbarHudTexture, barRect, Color.White);

            for (int i = 0; i < HotbarSlotCount; i++)
            {
                Rectangle slotRect = GetHotbarHudSlotRect(barRect, i);
                DrawSlotItem(slotRect, _hotbarSlots[i], false);

                if (_equippedHotbarIndex == i)
                    DrawRectOutline(slotRect, 2, new Color(255, 210, 80));
            }
        }

        // ---------- สมุด (กด I) ----------

        private void OpenBook()
        {
            _isBookOpen = true;

            // หยุดเสียงลูป (เดิน/ออร่า) ระหว่างหยุดเกม จะกลับมาเล่นเองเมื่อปิดสมุด
            StopGameLoopSounds();
            SyncAllInputStates();
            PlaySfx(_bookOpenSoundEffect, BookOpenSoundVolume);
        }

        private void CloseBook()
        {
            _isBookOpen = false;
            SyncAllInputStates();
        }

        private void UpdateBookScreen()
        {
            var keyboard = Keyboard.GetState();

            bool closePressed =
                (keyboard.IsKeyDown(Keys.I) && !_previousBookKeyboardState.IsKeyDown(Keys.I))
                || (keyboard.IsKeyDown(Keys.Escape) && !_previousBookKeyboardState.IsKeyDown(Keys.Escape));

            if (closePressed)
            {
                CloseBook();
                return;
            }

            _previousBookKeyboardState = keyboard;
        }

        private void DrawBookUI()
        {
            if (!_isBookOpen || _bookTexture == null)
                return;

            // ม่านมืดทับฉากเกมที่หยุดนิ่งอยู่ แล้ววางสมุดกึ่งกลางจอ
            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, WindowedHeight), new Color(0, 0, 0, 150));

            var bookRect = new Rectangle(
                (int)System.Math.Round(WindowedWidth / 2f - BookArtCenterX * BookUIScale),
                (int)System.Math.Round(WindowedHeight / 2f - BookArtCenterY * BookUIScale),
                (int)System.Math.Round(_bookTexture.Width * BookUIScale),
                (int)System.Math.Round(_bookTexture.Height * BookUIScale));
            _spriteBatch.Draw(_bookTexture, bookRect, Color.White);
        }

        // ---------- เมนู Esc ตอนอยู่ในเกม ----------

        private struct PauseMenuLayout
        {
            public Rectangle StartRect;
            public Rectangle SettingRect;
            public Rectangle BackRect;
        }

        private static Rectangle MakeCenteredButtonRect(Texture2D texture, int y)
        {
            int width = (int)(texture.Width * MenuButtonScale);
            int height = (int)(texture.Height * MenuButtonScale);
            return new Rectangle((WindowedWidth - width) / 2, y, width, height);
        }

        private PauseMenuLayout GetPauseMenuLayout()
        {
            int startHeight = (int)(_btnStartTexture.Height * MenuButtonScale);
            int settingHeight = (int)(_btnSettingTexture.Height * MenuButtonScale);
            int backHeight = (int)(_btnMenuTexture.Height * MenuButtonScale);
            int totalHeight = startHeight + settingHeight + backHeight + MenuButtonGap * 2;

            int y = (WindowedHeight - totalHeight) / 2;
            Rectangle startRect = MakeCenteredButtonRect(_btnStartTexture, y);
            Rectangle settingRect = MakeCenteredButtonRect(_btnSettingTexture, startRect.Bottom + MenuButtonGap);
            Rectangle backRect = MakeCenteredButtonRect(_btnMenuTexture, settingRect.Bottom + MenuButtonGap);

            return new PauseMenuLayout { StartRect = startRect, SettingRect = settingRect, BackRect = backRect };
        }


        /// <summary>
        /// ฉากที่ล็อกผู้เล่นทั้งหมด (Orb / ประตู / บอส): ไม่รับปุ่ม/กระเป๋า/คอนโซล คลิกซ้ายใช้เดินบทพูด
        /// </summary>
        private void UpdateLockedScene(GameTime gameTime, System.Action<GameTime, bool> updateScene)
        {
            _mouseState = Mouse.GetState();
            bool click = _mouseState.LeftButton == ButtonState.Pressed
                && _previousMouseState.LeftButton == ButtonState.Released;

            updateScene(gameTime, click);
            UpdateFullscreenToggle();
            UpdateKaiAnimation(gameTime);
            UpdateCamera();
            UpdateGameSounds(gameTime);

            SyncAllInputStates();
            base.Update(gameTime);
        }

        // ซิงค์สถานะปุ่ม/เมาส์ทั้งหมด กันปุ่มที่กดค้างอยู่ถูกนับเป็น "เพิ่งกด" ตอนเปิด/ปิดเมนู (เช่น คลิกปุ่มแล้วไปฟันประแจทันที)
        private void SyncAllInputStates()
        {
            var keyboard = Keyboard.GetState();
            _mouseState = Mouse.GetState();

            _previousKeyboardState = keyboard;
            _previousItemKeyboardState = keyboard;
            _previousInventoryKeyboardState = keyboard;
            _previousConsoleKeyboardState = keyboard;
            _previousBookKeyboardState = keyboard;
            _previousMouseState = _mouseState;
        }

        private void OpenPauseMenu()
        {
            _isPauseMenuOpen = true;
            _isSettingsOpen = false;
            _settingsDraggingSlider = -1;

            // หยุดเสียงลูป (เดิน/ออร่า) ระหว่างหยุดเกม จะกลับมาเล่นเองเมื่อเล่นต่อ
            StopGameLoopSounds();
            SyncAllInputStates();
        }

        private void ClosePauseMenu()
        {
            _isPauseMenuOpen = false;
            _isSettingsOpen = false;
            _settingsDraggingSlider = -1;
            SyncAllInputStates();
        }

        private void UpdatePauseMenu()
        {
            _mouseState = Mouse.GetState();
            var keyboard = Keyboard.GetState();

            bool escapeJustPressed = keyboard.IsKeyDown(Keys.Escape) && !_previousItemKeyboardState.IsKeyDown(Keys.Escape);
            bool leftPressed = _mouseState.LeftButton == ButtonState.Pressed;
            bool leftJustPressed = leftPressed && _previousMouseState.LeftButton == ButtonState.Released;
            Vector2 mouse = GetLogicalMousePosition();

            if (_isSettingsOpen)
            {
                // Back (Menu) ในหน้า Setting / กด Esc = กลับมาที่เมนู Esc
                bool backClicked = UpdateSettingsScreen(leftPressed, leftJustPressed, mouse);
                if (backClicked || escapeJustPressed)
                    CloseSettingsScreen();
            }
            else
            {
                PauseMenuLayout layout = GetPauseMenuLayout();

                if (escapeJustPressed || (leftJustPressed && layout.StartRect.Contains(mouse)))
                {
                    // Start = เล่นต่อ (Resume)
                    ClosePauseMenu();
                    return;
                }

                if (leftJustPressed && layout.SettingRect.Contains(mouse))
                {
                    _isSettingsOpen = true;
                    _settingsDraggingSlider = -1;
                }
                else if (leftJustPressed && layout.BackRect.Contains(mouse))
                {
                    ReturnToMainMenu();
                    return;
                }
            }

            _previousItemKeyboardState = keyboard;
            _previousMouseState = _mouseState;
        }

        private void DrawPauseMenuUI()
        {
            if (!_isPauseMenuOpen)
                return;

            // ม่านมืดทับฉากเกมที่หยุดนิ่งอยู่
            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, WindowedHeight), new Color(0, 0, 0, 150));

            if (_isSettingsOpen)
            {
                DrawSettingsContent();
                return;
            }

            PauseMenuLayout layout = GetPauseMenuLayout();
            Vector2 mouse = GetLogicalMousePosition();

            _spriteBatch.Draw(_btnStartTexture, layout.StartRect, layout.StartRect.Contains(mouse) ? Color.White : Color.Gray);
            _spriteBatch.Draw(_btnSettingTexture, layout.SettingRect, layout.SettingRect.Contains(mouse) ? Color.White : Color.Gray);
            _spriteBatch.Draw(_btnMenuTexture, layout.BackRect, layout.BackRect.Contains(mouse) ? Color.White : Color.Gray);
        }

        // Back (Menu) จากเมนู Esc: รีเซ็ตเกมใหม่หมด แล้วกลับหน้าหลัก (กด Start จากหน้าหลักจะเริ่มตั้งแต่ต้น)
        private void ReturnToMainMenu()
        {
            _isPauseMenuOpen = false;
            _isSettingsOpen = false;
            _settingsDraggingSlider = -1;

            ResetGameState();

            _appState = AppState.MainMenu;
            _mouseState = Mouse.GetState();
            _previousMouseState = _mouseState;
            _previousMenuKeyboardState = Keyboard.GetState();
        }

        private void ResetGameState()
        {
            StopGameLoopSounds();
            _bossOrbs.Clear();
            _boss2Swords.Clear();
            _kaiStunTimer = 0f;
            _isAimingTrident = false;
            _isTridentThrowAnim = false;
            _tridentPoseFrame = -1;
            _tridentProjectiles.Clear();
            _parryTimer = 0f;
            _parryCooldownTimer = 0f;
            _parrySuccessFlashTimer = 0f;

            // --- ผู้เล่น ---
            _currentRoomIndex = 0;
            _kaiCenterX = KaiSpawnX;
            _kaiJumpOffsetY = 0f;
            _kaiVelocityY = 0f;
            _jumpCount = 0;
            _isJumping = false;
            _isLanding = false;
            _landingTimer = 0f;
            _jumpFrameIndex = 0;
            _jumpAnimTimer = 0f;
            _isMoving = false;
            _facingRight = true;
            _currentFrame = 0;
            _frameTimer = 0f;
            _isAttacking = false;
            _attackFrameIndex = 0;
            _attackTimer = 0f;
            _attackHitApplied = false;
            _attackWhiffSoundPlayed = false;
            if (_swingSoundInstance != null)
                _swingSoundInstance.Stop();

            _currentHP = _maxHP;
            _currentStamina = _maxStamina;
            _isExhausted = false;
            _isSprinting = false;
            _staminaRegenDelayTimer = 0f;
            _deathState = PlayerDeathState.Alive;
            _isGodMode = false;
            _checkpointRoomIndex = -1;
            _seaweedBuffTimer = 0f;
            _jumpBuffTimer = 0f;
            _kaiGreenTintTimer = 0f;

            // --- กระเป๋า / ไอเทม ---
            System.Array.Clear(_inventoryGridSlots, 0, _inventoryGridSlots.Length);
            System.Array.Clear(_hotbarSlots, 0, _hotbarSlots.Length);
            _equippedHotbarIndex = -1;
            _isInventoryOpen = false;
            _isDraggingItem = false;
            _dragSlot = default(InventorySlot);
            _dragFromHotbar = false;
            _dragSourceIndex = 0;

            _isGiveItemMenuOpen = false;
            _giveItemQuantityStep = false;
            _giveItemSelectedType = ItemType.None;
            _giveItemQuantity = 1;
            _isDraggingGiveSlider = false;

            _isConsoleOpen = false;
            _isBookOpen = false;
            _consoleText = "";
            _consoleMessage = "";
            _consoleMessageTimer = 0f;

            // --- โลก / มอนสเตอร์ ---
            _worldItems.Clear();
            _worldItems.Add(new WorldItem { Type = ItemType.Orb, Count = 1, X = _worldWidth / 2f, Room = OrbSceneRoomIndex });
            SpawnRandomSeaweed();

            _iceGolems.Clear();
            _monster1List.Clear();
            _boss1List.Clear();
            _boss2List.Clear();
            _boss3List.Clear();
            _monster2List.Clear();
            _savePointSwordTaken = false;   // เริ่มเกมใหม่: ดาบกลับไปอยู่บนโครงกระดูก
            _codeStones.Clear();            // เริ่มเกมใหม่: สุ่มหิน/รหัสใหม่ (สุ่มอีกทีใน EnsureStonePuzzle)
            ResetStage2Waves(true);         // เริ่มเกมใหม่: เวฟด่าน 2 กลับไปเวฟแรก
            ResetBoss2EncounterFull();
            ResetBoss3EncounterFull();
            _stoneCode = "";
            _stoneHintLines = null;
            _passcodeDoorOpen = false;
            _isPasscodeOpen = false;
            _isSignReading = false;
            _passcodeInput = "";
            _boss2Swords.Clear();
            _kaiStunTimer = 0f;
            _isAimingTrident = false;
            _isTridentThrowAnim = false;
            _tridentPoseFrame = -1;
            _tridentProjectiles.Clear();
            _waveMonsters.Clear();
            _damagePopups.Clear();

            _dummyExists = false;
            _dummyHitCount = 0;
            _dummyTotalDamage = 0;
            _dummyShakeTimer = 0f;

            // --- ฉากต่างๆ ---
            _isWakeUpSequence = false;
            _wakeUpTimer = 0f;
            _wakeUpOverlayAlpha = 0f;
            _wakeUpNextRustleIndex = 0;
            _introStage = IntroStage.Blinking;
            _introStageTimer = 0f;
            _introWrenchVisible = false;
            _isDialogueActive = false;

            _isOrbScene = false;
            _orbSceneDone = false;
            _orbStage = default(OrbSceneStage);
            _orbStageTimer = 0f;
            _orbCameraBlend = 0f;
            _orbMonster = null;
            _orbFirstMonster = null;
            _orbBobTime = 0f;

            _wavesStarted = false;
            _wavesFinished = false;
            _waveIndex = 0;
            _waveTotalSpawned = 0;
            _waveDelayTimer = 0f;

            _doorOpened = false;
            _bossExitDoorVisible = false;
            _bossExitDoorOpened = false;
            _doorIsBossExit = false;
            _isDoorInteraction = false;
            _doorStage = default(DoorStage);
            _doorDialogueLeadsToConfirm = false;

            _isBossScene = false;
            _bossSceneDone = false;
            _bossStage = default(BossSceneStage);
            _bossStageTimer = 0f;
            _bossFocusBlend = 0f;
            _bossSceneZoom = CameraZoom;
            _bossShakeActive = false;
            _bossShakeTime = 0f;
            _bossFightActive = false;
            _bossFightZoomBlend = 0f;
            _bossSceneBoss = null;
            SpawnBossSceneStatue();
            SpawnBoss3AtEdge();

            _isTransitioning = false;
            _transitionDirection = 0;
            _transitionTimer = 0f;
            _transitionPlayerResetDone = false;
            _transitionOverlayAlpha = 0f;
            _transitionTargetRoom = -1;

            _cameraZoom = CameraZoom;
            UpdateCamera();
        }

        private struct MainMenuLayout
        {
            public Rectangle StartRect;
            public Rectangle SettingRect;
            public Rectangle ExitRect;
        }

        private MainMenuLayout GetMainMenuLayout()
        {
            int buttonWidth = (int)(_btnStartTexture.Width * MenuButtonScale);
            int buttonHeight = (int)(_btnStartTexture.Height * MenuButtonScale);

            var startRect = new Rectangle(MenuLeftMargin, MenuTopY, buttonWidth, buttonHeight);
            var settingRect = new Rectangle(MenuLeftMargin, startRect.Bottom + MenuButtonGap, buttonWidth, buttonHeight);
            var exitRect = new Rectangle(MenuLeftMargin, settingRect.Bottom + MenuButtonGap, buttonWidth, buttonHeight);

            return new MainMenuLayout
            {
                StartRect = startRect,
                SettingRect = settingRect,
                ExitRect = exitRect
            };
        }

        private void UpdateMainMenu()
        {
            var menuKeyboard = Keyboard.GetState();
            bool escapeJustPressed = menuKeyboard.IsKeyDown(Keys.Escape) && !_previousMenuKeyboardState.IsKeyDown(Keys.Escape);
            _previousMenuKeyboardState = menuKeyboard;

            bool leftPressed = _mouseState.LeftButton == ButtonState.Pressed;
            bool leftJustPressed = leftPressed && _previousMouseState.LeftButton == ButtonState.Released;

            if (_isSettingsOpen)
            {
                bool backClicked = UpdateSettingsScreen(leftPressed, leftJustPressed, GetLogicalMousePosition());
                if (backClicked || escapeJustPressed)
                    CloseSettingsScreen();
                return;
            }

            if (!leftJustPressed)
                return;

            var layout = GetMainMenuLayout();
            Vector2 mouseLogical = GetLogicalMousePosition();

            if (layout.StartRect.Contains(mouseLogical))
            {
                StartLoadingScreen();
            }
            else if (layout.ExitRect.Contains(mouseLogical))
            {
                Exit();
            }
            else if (layout.SettingRect.Contains(mouseLogical))
            {
                _isSettingsOpen = true;
                _settingsDraggingSlider = -1;
            }
        }

        private void DrawMainMenuUI()
        {
            // สเกลจาก resolution ตรรกะ (WindowedWidth x WindowedHeight) ไปเป็นขนาด viewport จริง
            // จำเป็นสำหรับตอนสลับเป็นฟูลสกรีน (F11) ไม่งั้นเนื้อหาจะค้างอยู่แค่มุมบนซ้าย
            float menuScaleX = (float)GraphicsDevice.Viewport.Width / WindowedWidth;
            float menuScaleY = (float)GraphicsDevice.Viewport.Height / WindowedHeight;
            var menuTransform = Matrix.CreateScale(menuScaleX, menuScaleY, 1f);

            // --- พื้นหลัง: ผ่าน shader (มืดลงเล็กน้อย + คลื่นเบาๆ + ไฟกระพริบบริเวณกระจกหมวก) ---
            var backgroundRect = new Rectangle(0, 0, WindowedWidth, WindowedHeight);

            _mainMenuEffect.Parameters["Time"].SetValue(_menuEffectTime);
            _mainMenuEffect.Parameters["DarkenAmount"].SetValue(MenuBackgroundDarkenAmount);
            _mainMenuEffect.Parameters["WaveAmplitude"].SetValue(MenuWaveAmplitude);
            _mainMenuEffect.Parameters["WaveSpeed"].SetValue(MenuWaveSpeed);
            _mainMenuEffect.Parameters["FlickerCenter"].SetValue(MenuFlickerCenter);
            _mainMenuEffect.Parameters["FlickerRadius"].SetValue(MenuFlickerRadius);
            _mainMenuEffect.Parameters["FlickerSpeed"].SetValue(MenuFlickerSpeed);
            _mainMenuEffect.Parameters["FlickerIntensity"].SetValue(MenuFlickerIntensity);

            _spriteBatch.Begin(effect: _mainMenuEffect, transformMatrix: menuTransform);
            _spriteBatch.Draw(_mainMenuBackground, backgroundRect, Color.White);
            _spriteBatch.End();

            // --- ปุ่ม: วาดปกติ (คมชัด ไม่โดนคลื่น/มืดจาก shader) ---
            // Hover: ปุ่มที่เมาส์ชี้อยู่ = White (สว่างเต็ม), ปุ่มอื่น = Gray (หรี่ลง)
            if (_isSettingsOpen)
            {
                _spriteBatch.Begin(transformMatrix: menuTransform);
                DrawSettingsContent();
                _spriteBatch.End();
                return;
            }

            var layout = GetMainMenuLayout();
            Vector2 mouseLogical = GetLogicalMousePosition();

            _spriteBatch.Begin(transformMatrix: menuTransform);
            _spriteBatch.Draw(_btnStartTexture, layout.StartRect, layout.StartRect.Contains(mouseLogical) ? Color.White : Color.Gray);
            _spriteBatch.Draw(_btnSettingTexture, layout.SettingRect, layout.SettingRect.Contains(mouseLogical) ? Color.White : Color.Gray);
            _spriteBatch.Draw(_btnExitTexture, layout.ExitRect, layout.ExitRect.Contains(mouseLogical) ? Color.White : Color.Gray);
            _spriteBatch.End();
        }

        // ==================== Loading Screen ====================

        private void StartLoadingScreen()
        {
            _appState = AppState.Loading;
            _loadingTimer = 0f;
        }

        private void UpdateLoading(GameTime gameTime)
        {
            _mouseState = Mouse.GetState();
            _loadingTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_loadingTimer >= LoadingScreenMinDuration)
                StartIntroVideo();

            _previousMouseState = _mouseState;
        }

        private void DrawLoadingUI()
        {
            string text = "LOADING...";
            Vector2 textSize = _hotbarFont.MeasureString(text) * 1.4f;
            var pos = new Vector2((GraphicsDevice.Viewport.Width - textSize.X) / 2f, (GraphicsDevice.Viewport.Height - textSize.Y) / 2f);

            _spriteBatch.Begin();
            _spriteBatch.DrawString(_hotbarFont, text, pos, Color.White, 0f, Vector2.Zero, 1.4f, SpriteEffects.None, 0f);
            _spriteBatch.End();
        }

        // ==================== Intro Video (แปลงจาก video_melanie.mp4 เป็นชุดภาพเฟรม) ====================
        // หมายเหตุ: DesktopGL ไม่มี VideoPlayer ในตัว จึงเล่นวิดีโอโดยสตรีมโหลดภาพเฟรมทีละภาพจากดิสก์
        // (โฟลเดอร์ IntroVideo/frames และไฟล์เสียง IntroVideo/intro_audio.wav ต้องถูก copy ไปที่โฟลเดอร์ output พร้อม .exe)

        private void StartIntroVideo()
        {
            string framesPath = Path.Combine(System.AppContext.BaseDirectory, IntroVideoFramesFolder);
            _introVideoFrameCount = Directory.Exists(framesPath)
                ? Directory.GetFiles(framesPath, $"{IntroVideoFramePrefix}*{IntroVideoFrameExtension}").Length
                : 0;

            // ไม่พบไฟล์เฟรม (เช่น ยังไม่ได้ copy ไฟล์ไปวาง) -> ข้ามวิดีโอ เข้าเกมเลย กันเกมค้าง
            if (_introVideoFrameCount <= 0)
            {
                _appState = AppState.Playing;
                StartWakeUpSequence();
                return;
            }

            _introVideoElapsed = 0f;
            _introVideoCurrentFrameIndex = -1;
            _isSkippingIntroVideo = false;
            _introVideoSkipTimer = 0f;
            _introVideoSkipOverlayAlpha = 0f;
            _previousIntroVideoKeyboardState = Keyboard.GetState();
            LoadIntroVideoFrame(0);

            string audioPath = Path.Combine(System.AppContext.BaseDirectory, IntroVideoAudioPath);
            if (File.Exists(audioPath))
            {
                using (var stream = File.OpenRead(audioPath))
                    _introVideoAudioEffect = SoundEffect.FromStream(stream);

                _introVideoAudioInstance = _introVideoAudioEffect.CreateInstance();
                _introVideoAudioInstance.Volume = MathHelper.Clamp(MusicGain, 0f, 1f); // เสียงวิดีโออินโทรใช้ช่อง Music
                _introVideoAudioInstance.Play();
            }

            _appState = AppState.IntroVideo;
        }

        private void LoadIntroVideoFrame(int frameIndex)
        {
            string fileName = $"{IntroVideoFramePrefix}{(frameIndex + 1).ToString().PadLeft(IntroVideoFrameDigits, '0')}{IntroVideoFrameExtension}";
            string fullPath = Path.Combine(System.AppContext.BaseDirectory, IntroVideoFramesFolder, fileName);

            if (!File.Exists(fullPath))
                return;

            var oldTexture = _introVideoCurrentTexture;

            using (var stream = File.OpenRead(fullPath))
                _introVideoCurrentTexture = Texture2D.FromStream(GraphicsDevice, stream);

            oldTexture?.Dispose();
            _introVideoCurrentFrameIndex = frameIndex;
        }

        private void UpdateIntroVideo(GameTime gameTime)
        {
            // กด Space เพื่อข้ามวิดีโอได้ (เสียงจะเฟดออกสั้นๆ ก่อนเข้าเกม)
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var keyboard = Keyboard.GetState();

            if (_isSkippingIntroVideo)
            {
                UpdateIntroVideoSkipFade(deltaSeconds);
                _previousIntroVideoKeyboardState = keyboard;
                return;
            }

            bool skipJustPressed = keyboard.IsKeyDown(IntroVideoSkipKey) && !_previousIntroVideoKeyboardState.IsKeyDown(IntroVideoSkipKey);
            if (skipJustPressed)
            {
                StartIntroVideoSkip();
                _previousIntroVideoKeyboardState = keyboard;
                return;
            }

            _introVideoElapsed += deltaSeconds;
            int targetFrameIndex = (int)(_introVideoElapsed * IntroVideoFps);

            if (targetFrameIndex >= _introVideoFrameCount)
                EndIntroVideo();
            else if (targetFrameIndex != _introVideoCurrentFrameIndex)
                LoadIntroVideoFrame(targetFrameIndex);

            _previousIntroVideoKeyboardState = keyboard;
        }

        private void StartIntroVideoSkip()
        {
            _isSkippingIntroVideo = true;
            _introVideoSkipTimer = 0f;
            _introVideoSkipOverlayAlpha = 0f;
            _introVideoAudioFadeStartVolume = _introVideoAudioInstance != null ? _introVideoAudioInstance.Volume : 0f;
        }

        private void UpdateIntroVideoSkipFade(float deltaSeconds)
        {
            _introVideoSkipTimer += deltaSeconds;

            // ช่วงแรก: จอค่อยๆ มืดลง พร้อมกับเสียงเฟดออกไปด้วยกัน
            float darkenT = MathHelper.Clamp(_introVideoSkipTimer / IntroVideoSkipDarkenDuration, 0f, 1f);
            _introVideoSkipOverlayAlpha = darkenT;

            if (_introVideoAudioInstance != null)
                _introVideoAudioInstance.Volume = _introVideoAudioFadeStartVolume * (1f - darkenT);

            // ช่วงที่สอง: ค้างจอมืดสนิทไว้สักพัก ก่อนค่อยตัดเข้าเกมจริง
            float totalDuration = IntroVideoSkipDarkenDuration + IntroVideoSkipHoldDuration;
            if (_introVideoSkipTimer >= totalDuration)
                EndIntroVideo();
        }

        private void EndIntroVideo()
        {
            _introVideoAudioInstance?.Stop();
            _introVideoAudioInstance?.Dispose();
            _introVideoAudioInstance = null;
            _introVideoAudioEffect?.Dispose();
            _introVideoAudioEffect = null;

            _introVideoCurrentTexture?.Dispose();
            _introVideoCurrentTexture = null;
            _introVideoCurrentFrameIndex = -1;

            _isSkippingIntroVideo = false;
            _introVideoSkipTimer = 0f;
            _introVideoSkipOverlayAlpha = 0f;

            _appState = AppState.Playing;
            StartWakeUpSequence();
        }

        private void DrawIntroVideoUI()
        {
            if (_introVideoCurrentTexture == null)
                return;

            float scaleX = (float)GraphicsDevice.Viewport.Width / _introVideoCurrentTexture.Width;
            float scaleY = (float)GraphicsDevice.Viewport.Height / _introVideoCurrentTexture.Height;
            float scale = System.Math.Min(scaleX, scaleY);

            var size = new Vector2(_introVideoCurrentTexture.Width * scale, _introVideoCurrentTexture.Height * scale);
            var pos = new Vector2(
                (GraphicsDevice.Viewport.Width - size.X) / 2f,
                (GraphicsDevice.Viewport.Height - size.Y) / 2f);

            _spriteBatch.Begin();
            _spriteBatch.Draw(_introVideoCurrentTexture, new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y), Color.White);
            DrawIntroVideoSkipHint();

            if (_isSkippingIntroVideo)
            {
                DrawFilledRect(
                    new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
                    new Color(0, 0, 0, (int)(_introVideoSkipOverlayAlpha * 255f)));
            }

            _spriteBatch.End();
        }

        private void DrawIntroVideoSkipHint()
        {
            if (_isSkippingIntroVideo)
                return;

            float hintAlpha = 1f;
            if (_introVideoElapsed > IntroVideoHintFullDuration)
            {
                hintAlpha = 1f - MathHelper.Clamp(
                    (_introVideoElapsed - IntroVideoHintFullDuration) / IntroVideoHintFadeDuration, 0f, 1f);
            }

            if (hintAlpha <= 0f)
                return;

            const string hintText = "Press Space to skip";
            const float hintScale = 0.8f;
            Vector2 hintSize = _hotbarFont.MeasureString(hintText) * hintScale;
            var hintPos = new Vector2(
                GraphicsDevice.Viewport.Width - hintSize.X - 24f,
                GraphicsDevice.Viewport.Height - hintSize.Y - 24f);

            _spriteBatch.DrawString(_hotbarFont, hintText, hintPos, Color.White * hintAlpha,
                0f, Vector2.Zero, hintScale, SpriteEffects.None, 0f);
        }

        private void UpdateInventoryToggle()
        {
            var keyboard = Keyboard.GetState();
            bool wasToggleKeyDown = _previousInventoryKeyboardState.IsKeyDown(Keys.Tab);
            bool isToggleKeyDown = keyboard.IsKeyDown(Keys.Tab);
            bool toggleJustPressed = isToggleKeyDown && !wasToggleKeyDown;

            if (toggleJustPressed && !_isConsoleOpen)
                _isInventoryOpen = !_isInventoryOpen;

            _previousInventoryKeyboardState = keyboard;
        }

        private struct InventoryLayout
        {
            public Rectangle PanelRect;
            public Rectangle TitleBarRect;
            public int GridOriginX;
            public int GridOriginY;
            public int HotbarOriginX;
            public int HotbarOriginY;
        }

        private InventoryLayout GetInventoryLayout()
        {
            int gridWidth = InventoryGridColumns * InventorySlotSize + (InventoryGridColumns - 1) * InventorySlotSpacing;
            int gridHeight = InventoryGridRows * InventorySlotSize + (InventoryGridRows - 1) * InventorySlotSpacing;
            int panelWidth = gridWidth + InventoryPanelPaddingX * 2;
            int panelHeight = gridHeight + InventoryPanelPaddingTopBar + InventoryPanelPaddingBottom;

            // แผงกริดอยู่กึ่งกลางจอ แถวฮอตบาร์ 8 ช่องแนวนอนอยู่ใต้แผง (จัดทั้งกลุ่มให้อยู่กึ่งกลางจอแนวตั้ง)
            int hotbarWidth = HotbarSlotCount * InventorySlotSize + (HotbarSlotCount - 1) * InventorySlotSpacing;
            int groupHeight = panelHeight + HotbarGapFromPanel + InventorySlotSize;
            int panelOriginX = (WindowedWidth - panelWidth) / 2;
            int panelOriginY = (WindowedHeight - groupHeight) / 2;

            int hotbarOriginX = (WindowedWidth - hotbarWidth) / 2;

            var panelRect = new Rectangle(panelOriginX, panelOriginY, panelWidth, panelHeight);
            var titleBarRect = new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, InventoryPanelPaddingTopBar);

            int gridOriginX = panelRect.X + InventoryPanelPaddingX;
            int gridOriginY = panelRect.Y + InventoryPanelPaddingTopBar;

            return new InventoryLayout
            {
                PanelRect = panelRect,
                TitleBarRect = titleBarRect,
                GridOriginX = gridOriginX,
                GridOriginY = gridOriginY,
                HotbarOriginX = hotbarOriginX,
                HotbarOriginY = panelRect.Bottom + HotbarGapFromPanel
            };
        }

        private Rectangle GetGridSlotRect(InventoryLayout layout, int row, int col)
        {
            int slotX = layout.GridOriginX + col * (InventorySlotSize + InventorySlotSpacing);
            int slotY = layout.GridOriginY + row * (InventorySlotSize + InventorySlotSpacing);
            return new Rectangle(slotX, slotY, InventorySlotSize, InventorySlotSize);
        }

        private Rectangle GetHotbarSlotRect(InventoryLayout layout, int index)
        {
            int slotX = layout.HotbarOriginX + index * (InventorySlotSize + InventorySlotSpacing);
            return new Rectangle(slotX, layout.HotbarOriginY, InventorySlotSize, InventorySlotSize);
        }

        private void UpdateKaiMovement(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();

            bool inputBlocked = _isTransitioning || _isConsoleOpen || _isGiveItemMenuOpen || _kaiStunTimer > 0f;
            bool leftPressed = !inputBlocked && keyboard.IsKeyDown(Keys.A);
            bool rightPressed = !inputBlocked && keyboard.IsKeyDown(Keys.D);
            bool shiftHeld = !inputBlocked && (keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift));

            int direction = 0;
            if (leftPressed && !rightPressed) direction = -1;
            else if (rightPressed && !leftPressed) direction = 1;

            bool isMovingNow = direction != 0;

            if (isMovingNow != _isMoving)
            {
                _currentFrame = 0;
                _frameTimer = 0f;
            }
            _isMoving = isMovingNow;

            _isSprinting = isMovingNow && shiftHeld && !_isExhausted;

            if (isMovingNow)
            {
                _facingRight = direction > 0;

                float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
                float speed = _isSprinting ? KaiSpeed * SprintSpeedMultiplier : KaiSpeed;
                _kaiCenterX += direction * speed * deltaSeconds;

                if (_isSprinting)
                    SpendStamina(StaminaSprintDrainPerSecond * deltaSeconds);

                float halfWidth = GetCurrentHalfWidth();
                float minX = halfWidth;
                float maxX = _worldWidth - halfWidth;
                _kaiCenterX = MathHelper.Clamp(_kaiCenterX, minX, maxX);

                // ประตูรหัสห้องที่สองของด่าน 2 ยังล็อกอยู่: เดินชิดประตูได้ถึงระยะหนึ่ง ทะลุไปไม่ได้
                if (_currentRoomIndex == StoneRoomB && !_passcodeDoorOpen)
                    _kaiCenterX = MathHelper.Min(_kaiCenterX, GetStoneDoorX() - StoneDoorBlockMargin);

                // ชนด้านข้างพื้นที่ยกสูง (ยังไม่ได้ขึ้นไปยืนด้านบน) ผ่านไม่ได้
                _kaiCenterX = ResolveKaiPlatformWalls(_kaiCenterX);

                bool canGoRight = _currentRoomIndex < RoomCount - 1
                    || (IsLevel2Room(_currentRoomIndex) && _currentRoomIndex < Level2LastRoomIndex)   // ด่าน 1: ห้อง 1-3 / ด่าน 2: ห้อง index 4-6
                    || CanGoToLevel3();   // ห้องบอส 2: กำจัดบอสแล้ว เดินชนขอบขวา = ไปด่าน 3
                if (_currentRoomIndex == StoneRoomB)
                    canGoRight = false;   // ขอบขวาห้องที่สองของด่าน 2 เป็นกำแพงเสมอ ไปห้องที่ 3 ได้ทางเดียวคือกด E ที่ประตูรหัสที่เปิดแล้ว

                if (direction > 0 && !_isTransitioning && _kaiCenterX >= maxX - 0.01f && canGoRight)
                {
                    StartSceneTransition(1);
                }
                else if (direction < 0 && !_isTransitioning && _kaiCenterX <= minX + 0.01f && _currentRoomIndex > 0 && !IsBoss2ArenaLocked() && !IsLevel3Room(_currentRoomIndex))
                {
                    if (_currentRoomIndex == SecretRoomIndex)
                        StartDoorTransition(DoorRoomIndex, DoorX, false); // ห้องลับ: เดินชนขอบซ้าย = ออกทางประตู
                    else if (_currentRoomIndex == NextLevelRoomIndex)
                        StartDoorTransition(BossSceneRoomIndex, BossExitDoorX, false); // ห้องแรกของด่าน 2: เดินชนขอบซ้าย = กลับออกทางประตูห้องบอส
                    else if (_currentRoomIndex == Level2LastRoomIndex)
                        StartDoorTransition(StoneRoomB, GetStoneDoorX() - StoneDoorReturnOffset, false); // ห้องที่ 3 ของด่าน 2: เดินชนขอบซ้าย = ออกทางประตูรหัสกลับห้องที่สอง
                    else
                        StartSceneTransition(-1);
                }
            }
        }

        private void StartSceneTransition(int direction)
        {
            _isTransitioning = true;
            _transitionDirection = direction;
            _transitionTargetRoom = -1;
            _transitionTimer = 0f;
            _transitionOverlayAlpha = 0f;
            _transitionPlayerResetDone = false;
        }

        private void UpdateSceneTransition(GameTime gameTime)
        {
            _transitionTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_transitionTimer <= TransitionFadeOutDuration)
            {
                _transitionOverlayAlpha = MathHelper.Clamp(_transitionTimer / TransitionFadeOutDuration, 0f, 1f);
            }
            else if (_transitionTimer <= TransitionFadeOutDuration + TransitionHoldDuration)
            {
                _transitionOverlayAlpha = 1f;

                if (!_transitionPlayerResetDone)
                {
                    ResetKaiAfterRoomChange();
                    _transitionPlayerResetDone = true;
                }
            }
            else if (_transitionTimer <= TransitionTotalDuration)
            {
                float fadeInElapsed = _transitionTimer - TransitionFadeOutDuration - TransitionHoldDuration;
                _transitionOverlayAlpha = 1f - MathHelper.Clamp(fadeInElapsed / TransitionFadeInDuration, 0f, 1f);
            }
            else
            {
                _isTransitioning = false;
                _transitionOverlayAlpha = 0f;
            }
        }

        private void ResetKaiAfterRoomChange()
        {
            bool viaDoor = _transitionTargetRoom >= 0;
            if (viaDoor)
                _currentRoomIndex = _transitionTargetRoom;
            else
                _currentRoomIndex += _transitionDirection;

            float halfWidth = GetCurrentHalfWidth();

            if (viaDoor)
            {
                _kaiCenterX = _transitionTargetX;
                _facingRight = _transitionTargetFacingRight;
                _transitionTargetRoom = -1;
            }
            else if (_transitionDirection > 0)
            {
                _kaiCenterX = halfWidth;
                _facingRight = true;
            }
            else
            {
                _kaiCenterX = _worldWidth - halfWidth;
                _facingRight = false;
            }

            _kaiJumpOffsetY = 0f;
            _damagePopups.Clear();

            // เข้าห้องบอส 2 (ห้องที่ 3 ของด่าน 2): เซฟจุดเกิดอัตโนมัติ (เงียบๆ) ที่ตำแหน่งที่ Kai ยืนตอนเข้าห้อง
            if (_currentRoomIndex == Level2LastRoomIndex)
            {
                _checkpointRoomIndex = Level2LastRoomIndex;
                _checkpointX = _kaiCenterX;
            }

            // เข้าด่าน 3: เซฟจุดเกิดอัตโนมัติ (เงียบๆ) ที่ห้องแรกของด่าน 3 ตายแล้วเกิดใหม่ตรงนี้
            if (_currentRoomIndex == Level3RoomIndex)
            {
                _checkpointRoomIndex = Level3RoomIndex;
                _checkpointX = _kaiCenterX;
            }

            UpdateCamera();

        }

        // =====================================================================
        // พื้นที่ยกสูง (Raised platforms) — เป็นก้อนตัน: ชนด้านข้าง/ใต้แผ่นไม่ได้ ยืนได้เฉพาะด้านบน
        // พิกัด Top/Bottom เป็นค่า Y ของโลก (Y ยิ่งน้อยยิ่งสูง) ตัวละครยืนบนแผ่นโดยใช้ YOffset = Top - _floorY
        // =====================================================================

        private struct Platform
        {
            public int Room;
            public float X;
            public float Width;
            public float Top;
            public float Bottom;
        }

        private readonly List<Platform> _platforms = new List<Platform>();

        // มอนสเตอร์กระโดดขึ้นแผ่นที่สูงกว่าเท้าได้ไม่เกินค่านี้ (พิกเซล)
        private const float MonsterMaxClimbHeight = 230f;
        private const float MonsterClimbMargin = 25f;

        private void AddPlatform(int room, float x, float width, float heightAboveFloor)
        {
            _platforms.Add(new Platform
            {
                Room = room,
                X = x,
                Width = width,
                Top = _floorY - heightAboveFloor,
                Bottom = _floorY
            });
        }

        // จัดวางพื้นที่ยกสูงทั่วทั้ง 2 ห้อง (ห้องละ 8 อัน: กลุ่มขั้นบันไดบางจุด + อันเดี่ยวกระจาย)
        // พารามิเตอร์: AddPlatform(ห้อง, x เริ่มต้น, ความกว้าง, ความสูงจากพื้นเดิม)
        // ข้อควรระวังตอนปรับตำแหน่งเอง:
        //  - กระโดดครั้งเดียวสูง ~160px, กระโดดสองชั้น ~300px  -> อย่าตั้งความสูงเกิน ~250 ถ้าไม่มีขั้นรอง
        //  - มอนสเตอร์กระโดดขึ้นได้ไม่เกิน MonsterMaxClimbHeight (230px)
        //  - ห้อง 0: Kai เกิด/respawn ที่ x = KaiSpawnX (700) -> เว้นช่วง ~520-880 ให้โล่ง
        //  - ปลายซ้าย/ขวาของทั้งสองห้องเป็นจุดเดินข้ามห้อง (x < ~420 และ x > ~4700) -> เว้นให้โล่ง
        private void InitPlatforms()
        {
            _platforms.Clear();

            // ---------------- ห้อง 0 ----------------
            // ขั้นบันไดขึ้นไปทางขวา (สูงขึ้นทีละ ~80px)
            AddPlatform(0, 1000f, 220f, 80f);
            AddPlatform(0, 1220f, 220f, 160f);
            AddPlatform(0, 1440f, 260f, 220f);

            AddPlatform(0, 1950f, 260f, 130f);     // อันเดี่ยว
            AddPlatform(0, 2450f, 300f, 100f);     // อันเดี่ยว (ย้ายมาจากต้นห้อง เพราะจุดเกิดย้ายไปฝั่งซ้าย)
            AddPlatform(0, 2900f, 300f, 90f);      // อันเดี่ยว
            AddPlatform(0, 3450f, 240f, 170f);     // อันเดี่ยว
            AddPlatform(0, 4100f, 300f, 140f);     // อันเดี่ยว

            // ---------------- ห้อง 1 ----------------
            AddPlatform(1, 600f, 260f, 110f);      // อันเดี่ยว

            // กลุ่มรูปภูเขา (ขึ้นแล้วลง): 90 -> 180 -> 90
            AddPlatform(1, 1300f, 200f, 90f);
            AddPlatform(1, 1500f, 240f, 180f);
            AddPlatform(1, 1740f, 200f, 90f);

            AddPlatform(1, 2250f, 280f, 140f);     // อันเดี่ยว
            AddPlatform(1, 2850f, 240f, 200f);     // อันเดี่ยว (สูง ต้องกระโดดสองชั้น)
            AddPlatform(1, 3400f, 260f, 120f);     // อันเดี่ยว
            AddPlatform(1, 3900f, 420f, 150f);     // อันเดี่ยว (กว้าง)

            // ---------------- ห้อง 2 (ห้อง 3 ในเกม / index 2) ----------------
            // orb อยู่กลางห้อง (x = 2560) เว้นช่วง ~2400-2800 ให้เก็บ orb บนพื้นราบ / ขอบขวา (x > ~4800) เว้นเป็นจุดเกิดของมอนในฉาก orb
            // มอนในฉาก orb ปีนพื้นยกสูงได้ (ใช้ระบบเดียวกับ AI ปกติ) จึงวางฝั่งขวาได้ แต่ความสูงต้องไม่เกิน MonsterMaxClimbHeight
            AddPlatform(2, 750f, 240f, 100f);      // ขั้นเตี้ยๆ ใกล้จุดเกิด
            AddPlatform(2, 1150f, 220f, 150f);
            AddPlatform(2, 1370f, 240f, 210f);     // ขั้นบันไดขึ้น 150 -> 210
            AddPlatform(2, 1850f, 300f, 120f);     // แท่นกว้าง
            AddPlatform(2, 2180f, 200f, 180f);     // แท่นสุดท้ายก่อนถึง orb

            // ฝั่งขวาของ orb
            AddPlatform(2, 2950f, 260f, 130f);     // แท่นแรกหลังผ่าน orb
            AddPlatform(2, 3350f, 200f, 90f);      // บันไดขึ้น-ลง: 90 -> 180 -> 90
            AddPlatform(2, 3550f, 240f, 180f);
            AddPlatform(2, 3790f, 200f, 90f);
            AddPlatform(2, 4250f, 300f, 150f);     // แท่นกว้าง
            AddPlatform(2, 4620f, 180f, 210f);     // แท่นสูง (ก่อนถึงจุดเกิดของมอน)

            // ---------------- ห้องลับหลังประตู (index 3) ----------------
            // Kai เกิดที่ x = SecretRoomSpawnX (400) และออกทางขอบซ้าย -> เว้นช่วงต้นห้องให้โล่ง (~0-800)
            // ความสูงทุกอันไม่เกิน ~250 (กระโดดสองชั้นถึง) และกลุ่มขั้นบันไดต่างกันไม่เกิน ~90 ต่อขั้น
            AddPlatform(3, 900f, 240f, 100f);      // อันเดี่ยว
            AddPlatform(3, 1250f, 200f, 160f);     // อันเดี่ยว
            AddPlatform(3, 1600f, 260f, 120f);     // อันเดี่ยว (กว้าง)
            AddPlatform(3, 2000f, 200f, 200f);     // อันเดี่ยว (ต้องกระโดดสองชั้น)

            // กลุ่มขั้นบันไดขึ้นไปทางขวา: 90 -> 170 -> 240
            AddPlatform(3, 2400f, 200f, 90f);
            AddPlatform(3, 2600f, 220f, 170f);
            AddPlatform(3, 2820f, 200f, 240f);

            AddPlatform(3, 3250f, 300f, 130f);     // แท่นกว้าง
            AddPlatform(3, 3700f, 200f, 190f);     // อันเดี่ยว
            AddPlatform(3, 4050f, 260f, 110f);     // อันเดี่ยว
            // ปลายห้อง (x > ~4350) เว้นโล่งเป็นลานบอส (BossStatueX) จึงปิดแท่น 4400/4750 เดิมไว้
            // AddPlatform(3, 4400f, 240f, 160f);     // อันเดี่ยว
            // AddPlatform(3, 4750f, 300f, 210f);     // แท่นสูงท้ายห้อง
        }

        private float GetKaiCollisionHalfWidth()
        {
            // ใช้ความกว้างคงที่ (จากท่ายืนนิ่ง) เพื่อไม่ให้ตัวสั่นเวลาสไปรท์เปลี่ยนความกว้างตามอนิเมชัน
            return _kaiIdle[0].ClampHalfWidth * _kaiScale * 0.8f;
        }

        private static bool OverlapsHorizontally(Platform p, float x, float halfWidth)
        {
            return x + halfWidth > p.X && x - halfWidth < p.X + p.Width;
        }

        // ผิวสูงสุดของแผ่นที่คร่อมช่วง x นี้ (ถ้าไม่มีให้ใช้พื้นเดิม) — ใช้กับจุดเกิดมอนสเตอร์/ไอเทมบนพื้น
        private float GetSurfaceYAt(int room, float x, float halfWidth)
        {
            float surface = _floorY;
            foreach (var p in _platforms)
            {
                if (p.Room == room && OverlapsHorizontally(p, x, halfWidth) && p.Top < surface)
                    surface = p.Top;
            }
            return surface;
        }

        // ผิวที่รองรับเท้าอยู่ตอนนี้: แผ่นที่สูงสุดซึ่งอยู่ระดับเท้าหรือต่ำกว่า (ไม่มีให้ใช้พื้นเดิม)
        private float GetSupportY(int room, float x, float halfWidth, float feetY)
        {
            float support = _floorY;
            foreach (var p in _platforms)
            {
                if (p.Room == room && OverlapsHorizontally(p, x, halfWidth) && p.Top >= feetY - 1.5f && p.Top < support)
                    support = p.Top;
            }
            return support;
        }

        // ตอนตกลงมา: เท้าข้ามผิวแผ่น (หรือพื้นเดิม) ในเฟรมนี้หรือไม่ ถ้าใช่ให้ลงจอดที่ผิวแรกที่เจอ
        private bool TryFindLanding(int room, float x, float halfWidth, float prevFeetY, float newFeetY, out float landY)
        {
            bool found = false;
            landY = _floorY;

            if (newFeetY >= _floorY)
                found = true;

            foreach (var p in _platforms)
            {
                if (p.Room != room || !OverlapsHorizontally(p, x, halfWidth))
                    continue;

                if (prevFeetY <= p.Top + 0.5f && newFeetY >= p.Top && p.Top < landY)
                {
                    landY = p.Top;
                    found = true;
                }
            }
            return found;
        }

        // ตอนพุ่งขึ้น: หัวชนใต้แผ่นที่ลอยอยู่หรือไม่
        private bool TryHitCeiling(int room, float x, float halfWidth, float prevHeadY, float newHeadY, out float ceilingY)
        {
            bool found = false;
            ceilingY = 0f;

            foreach (var p in _platforms)
            {
                if (p.Room != room || !OverlapsHorizontally(p, x, halfWidth))
                    continue;

                if (prevHeadY >= p.Bottom - 0.5f && newHeadY < p.Bottom && (!found || p.Bottom > ceilingY))
                {
                    ceilingY = p.Bottom;
                    found = true;
                }
            }
            return found;
        }

        // ดัน Kai ออกจากด้านข้างของแผ่นที่ลำตัวซ้อนอยู่ (ต่ำกว่าผิวบนแผ่น = ยังไม่ได้ขึ้นไปยืน)
        private float ResolveKaiPlatformWalls(float x)
        {
            float halfWidth = GetKaiCollisionHalfWidth();
            float feetY = _floorY + _kaiJumpOffsetY;
            float headY = feetY - _kaiTargetHeight;

            foreach (var p in _platforms)
            {
                if (p.Room != _currentRoomIndex)
                    continue;
                if (!(feetY > p.Top + 1f && headY < p.Bottom))
                    continue;
                if (!OverlapsHorizontally(p, x, halfWidth))
                    continue;

                x = x < p.X + p.Width / 2f ? p.X - halfWidth : p.X + p.Width + halfWidth;
            }
            return x;
        }

        // ฟิสิกส์แนวตั้งของมอนสเตอร์: เดินหลุดขอบแล้วตก / ลงจอดบนแผ่นหรือพื้น
        private void UpdateMonsterVertical(int room, float x, float halfWidth, ref float yOffset, ref float velocityY, ref bool airborne, float deltaSeconds)
        {
            float prevFeetY = _floorY + yOffset;

            if (!airborne)
            {
                float supportY = GetSupportY(room, x, halfWidth, prevFeetY);
                if (supportY <= prevFeetY + 1f)
                    return;

                airborne = true;
                velocityY = 0f;
            }

            velocityY += Gravity * deltaSeconds;
            float newFeetY = prevFeetY + velocityY * deltaSeconds;

            if (velocityY >= 0f && TryFindLanding(room, x, halfWidth, prevFeetY, newFeetY, out float landY))
            {
                newFeetY = landY;
                velocityY = 0f;
                airborne = false;
            }

            yOffset = newFeetY - _floorY;
        }

        // เดินแนวนอนของมอนสเตอร์: ชนด้านข้างแผ่นแล้วหยุด ถ้าแผ่นไม่สูงเกินระยะที่กระโดดถึง (และอยู่บนพื้น) จะกระโดดขึ้น
        private float MoveMonsterHorizontally(int room, float x, float dx, float halfWidth, float bodyHeight, float yOffset, ref float velocityY, ref bool airborne)
        {
            float feetY = _floorY + yOffset;
            float newX = x + dx;

            foreach (var p in _platforms)
            {
                if (p.Room != room)
                    continue;
                if (!(feetY > p.Top + 1f && feetY - bodyHeight < p.Bottom))
                    continue;
                if (!OverlapsHorizontally(p, newX, halfWidth))
                    continue;

                newX = x < p.X + p.Width / 2f ? p.X - halfWidth : p.X + p.Width + halfWidth;

                float climbHeight = feetY - p.Top;
                if (!airborne && climbHeight <= MonsterMaxClimbHeight)
                {
                    velocityY = -(float)System.Math.Sqrt(2f * Gravity * (climbHeight + MonsterClimbMargin));
                    airborne = true;
                }
            }
            return newX;
        }

        // ลำตัวมอนสเตอร์กับ Kai อยู่ช่วงความสูงซ้อนกันหรือไม่ (ใช้ตัดสินใจโจมตี ไม่โจมตีข้ามชั้นที่ตีไม่ถึง)
        private bool IsMonsterVerticallyOverlappingKai(float monsterYOffset, float monsterHeight)
        {
            float monsterFeetY = _floorY + monsterYOffset;
            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            return monsterFeetY > kaiFeetY - _kaiTargetHeight && monsterFeetY - monsterHeight < kaiFeetY;
        }

        // ไอเทมบนพื้นอยู่ที่ผิวสูงสุดตรงตำแหน่ง X นั้น (บนแผ่นพื้นที่ยกสูงถ้าอยู่เหนือแผ่น)
        // =====================================================================
        // สาหร่ายสุ่มวางตามแมพ: สุ่มครั้งเดียวตอนเริ่มเกม/รีเซ็ตเกม ตำแหน่งคงที่ตลอดรอบ (เก็บแล้วไม่เกิดใหม่)
        // วางทุกห้องตั้งแต่ห้อง 1 ถึงห้องบอส (ห้องถัดไปหลังบอสยังไม่มี) ห้องละ SeaweedPerRoomMin-Max ชิ้น
        // วางบนพื้นเท่านั้น (เลี่ยงช่วงที่มี platform) และเลี่ยงจุดสำคัญ: จุดเกิด Kai / ประตู / orb / ลานบอส
        // =====================================================================
        private const int SeaweedPerRoomMin = 2;
        private const int SeaweedPerRoomMax = 3;
        private const float SeaweedEdgeMargin = 300f;      // ห่างขอบห้อง (กันชนจุดเปลี่ยนห้อง)
        private const float SeaweedMinSpacing = 400f;      // ห่างกันเองอย่างน้อยเท่านี้
        private const float SeaweedPlatformMargin = 60f;   // ห่างจากขอบ platform
        private const float SeaweedAvoidRadius = 200f;     // ห่างจากจุดสำคัญ (Kai spawn / ประตู / orb)
        private const float SeaweedBossArenaClearance = 350f; // ห้องบอส: ไม่วางใกล้บอสและประตูทางออกทางขวา
        private const int SeaweedSpawnAttempts = 60;

        private void SpawnRandomSeaweed()
        {
            for (int room = 0; room <= SecretRoomIndex; room++)
            {
                int target = _random.Next(SeaweedPerRoomMin, SeaweedPerRoomMax + 1);
                var placedX = new List<float>();

                for (int n = 0; n < target; n++)
                {
                    for (int attempt = 0; attempt < SeaweedSpawnAttempts; attempt++)
                    {
                        float x = SeaweedEdgeMargin + (float)_random.NextDouble() * (_worldWidth - 2f * SeaweedEdgeMargin);
                        if (!IsSeaweedSpotValid(room, x, placedX))
                            continue;

                        placedX.Add(x);
                        _worldItems.Add(new WorldItem { Type = ItemType.Seaweed, Count = 1, X = x, Room = room });
                        break;
                    }
                }
            }
        }

        private bool IsSeaweedSpotValid(int room, float x, List<float> placedX)
        {
            foreach (float other in placedX)
            {
                if (System.Math.Abs(x - other) < SeaweedMinSpacing)
                    return false;
            }

            foreach (var p in _platforms)
            {
                if (p.Room == room && x > p.X - SeaweedPlatformMargin && x < p.X + p.Width + SeaweedPlatformMargin)
                    return false;
            }

            if (room == 0 && (System.Math.Abs(x - KaiSpawnX) < SeaweedAvoidRadius || System.Math.Abs(x - DoorX) < SeaweedAvoidRadius))
                return false;
            if (room == OrbSceneRoomIndex && System.Math.Abs(x - _worldWidth / 2f) < SeaweedAvoidRadius)
                return false;
            if (room == BossSceneRoomIndex && x > BossStatueX - SeaweedBossArenaClearance)
                return false;

            return true;
        }

        private float GetWorldItemGroundY(WorldItem item)
        {
            return GetSurfaceYAt(item.Room, item.X, 0f);
        }

        // เก็บ/เห็นข้อความ Press E ได้เมื่ออยู่ใกล้ทั้งแนวนอนและแนวตั้ง (ไม่เก็บของบนแผ่นจากพื้นด้านล่างได้)
        private bool IsWorldItemInPickupRange(WorldItem item)
        {
            if (item.IsStuck)
            {
                // trident ปักค้าง (พื้น/แผ่นสูง/ตัวบอส): วัดระยะตามจุดที่ปักจริง ไม่ใช่พื้นใต้ไอเทม
                if (System.Math.Abs(item.X - _kaiCenterX) > PickupRange)
                    return false;

                float stuckTipY = item.StuckHost != null
                    ? _floorY + item.StuckHost.YOffset + item.StuckOffsetY
                    : item.StuckTipY;
                float kaiMidY = _floorY + _kaiJumpOffsetY - _kaiTargetHeight * 0.5f;
                return System.Math.Abs(kaiMidY - stuckTipY) <= _kaiTargetHeight * 0.75f;
            }

            if (System.Math.Abs(item.X - _kaiCenterX) > PickupRange)
                return false;

            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            return System.Math.Abs(kaiFeetY - GetWorldItemGroundY(item)) <= _kaiTargetHeight * 0.75f;
        }

        private void DrawPlatforms()
        {
            foreach (var p in _platforms)
            {
                if (p.Room != _currentRoomIndex)
                    continue;

                for (float x = p.X; x < p.X + p.Width; x += _groundTileWidth)
                {
                    int tileWidth = (int)System.Math.Min(_groundTileWidth, p.X + p.Width - x);
                    int srcWidth = (int)System.Math.Round(tileWidth * ((float)_groundTexture.Width / _groundTileWidth));
                    if (tileWidth <= 0 || srcWidth <= 0)
                        continue;

                    // แผ่นบนสุด: ใช้ภาพพื้นเต็ม (มีขอบสว่างด้านบน) วางให้ขอบบนตรงกับผิวแผ่น
                    // ส่วนที่เหลือใต้ผิวจนถึงพื้นเดิม: ใช้ภาพเดียวกันต่อลงมา
                    for (float y = p.Top; y < p.Bottom; y += _floorHeight)
                    {
                        int tileHeight = (int)System.Math.Min(_floorHeight, p.Bottom - y);
                        int srcHeight = (int)System.Math.Round(tileHeight * ((float)_groundTexture.Height / _floorHeight));
                        if (tileHeight <= 0 || srcHeight <= 0)
                            continue;

                        var dest = new Rectangle((int)x, (int)y, tileWidth, tileHeight);
                        var src = new Rectangle(0, 0, srcWidth, srcHeight);
                        _spriteBatch.Draw(_groundTexture, dest, src, Color.White);
                    }
                }
            }
        }

        private void UpdateKaiJump(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            bool spaceJustPressed = !_isTransitioning && !_isConsoleOpen && !_isGiveItemMenuOpen && _kaiStunTimer <= 0f
                && keyboard.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space);

            bool isAirJump = _jumpCount >= 1;
            bool hasFreeJump = _jumpBuffTimer > 0f; // บัฟน้ำยากระโดด: ไม่เสีย stamina และกระโดดกลางอากาศได้แม้ stamina หมด
            bool canJump = _jumpCount < MaxJumps && !(isAirJump && _isExhausted && !hasFreeJump);

            if (spaceJustPressed && canJump)
            {
                if (!hasFreeJump)
                    SpendStamina(StaminaJumpCost);
                PlaySfx(_jumpSoundEffect, JumpSoundVolume); // เล่นทุกครั้งที่กระโดด รวมกระโดดชั้นที่สองกลางอากาศ
                _kaiVelocityY = JumpVelocity;
                _isJumping = true;
                _isLanding = false;
                _jumpCount++;

                _jumpAnimTimer = 0f;
                _jumpFrameIndex = 0;
            }

            float collisionHalfWidth = GetKaiCollisionHalfWidth();

            // เดินหลุดขอบพื้นที่ยกสูงโดยไม่ได้กระโดด → ตกลงมา (นับเป็นใช้กระโดดไปแล้ว 1 ครั้ง)
            if (!_isJumping)
            {
                float currentFeetY = _floorY + _kaiJumpOffsetY;
                float supportY = GetSupportY(_currentRoomIndex, _kaiCenterX, collisionHalfWidth, currentFeetY);
                if (supportY > currentFeetY + 1f)
                {
                    _isJumping = true;
                    _isLanding = false;
                    _kaiVelocityY = 0f;
                    _jumpCount = System.Math.Max(_jumpCount, 1);
                    _jumpAnimTimer = 0f;
                    _jumpFrameIndex = 0;
                }
            }

            if (_isJumping)
            {
                float prevFeetY = _floorY + _kaiJumpOffsetY;
                _kaiVelocityY += Gravity * deltaSeconds;
                float newFeetY = prevFeetY + _kaiVelocityY * deltaSeconds;

                // พุ่งขึ้นแล้วหัวชนใต้แผ่น → หยุดแล้วเริ่มตก
                if (_kaiVelocityY < 0f
                    && TryHitCeiling(_currentRoomIndex, _kaiCenterX, collisionHalfWidth,
                        prevFeetY - _kaiTargetHeight, newFeetY - _kaiTargetHeight, out float ceilingY))
                {
                    newFeetY = ceilingY + _kaiTargetHeight;
                    _kaiVelocityY = 0f;
                }

                bool landed = false;
                if (_kaiVelocityY >= 0f
                    && TryFindLanding(_currentRoomIndex, _kaiCenterX, collisionHalfWidth, prevFeetY, newFeetY, out float landY))
                {
                    newFeetY = landY;
                    landed = true;
                }

                _kaiJumpOffsetY = newFeetY - _floorY;

                if (landed)
                {
                    _kaiVelocityY = 0f;
                    _isJumping = false;
                    _jumpCount = 0;
                    _isLanding = true;
                    _landingTimer = 0f;
                }
                else
                {
                    // ตกลงมาชนด้านข้างแผ่น (เท้ายังต่ำกว่าผิวแผ่น) → ดันออกด้านข้าง
                    _kaiCenterX = ResolveKaiPlatformWalls(_kaiCenterX);

                    _jumpAnimTimer += deltaSeconds;
                    _jumpFrameIndex = System.Math.Min((int)(_jumpAnimTimer / FrameDuration), JumpFrameCount - 1);
                }
            }

            if (_isLanding)
            {
                _jumpFrameIndex = JumpFrameCount - 1;

                _landingTimer += deltaSeconds;
                if (_landingTimer >= LandingDuration)
                {
                    _isLanding = false;
                }
            }

            _previousKeyboardState = keyboard;
        }

        private void UpdateKaiAnimation(GameTime gameTime)
        {
            int frameCount = _isMoving ? WalkFrameCount : IdleFrameCount;

            _frameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            float frameDuration = _isSprinting ? FrameDuration / SprintAnimSpeedMultiplier : FrameDuration;

            if (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                _currentFrame = (_currentFrame + 1) % frameCount;
            }
        }

        private void UpdateCamera()
        {
            float playerCenterY = _floorY + _kaiJumpOffsetY - (_kaiTargetHeight / 2f);
            if (IsSleepPoseVisible())
                playerCenterY = _floorY + SleepPoseGroundSink + _kaiSleepCenterOffsetY; // โฟกัสกลางลำตัวที่นอนอยู่
            var desiredCameraCenter = new Vector2(_kaiCenterX, playerCenterY);

            // ฉาก orb: เลื่อนกล้องจาก Kai ไปหามอน (blend 0..1)
            if (_isOrbScene && _orbMonster != null && _orbCameraBlend > 0f)
            {
                float monsterCenterY = _floorY + _orbMonster.YOffset - _monster1Height / 2f;
                desiredCameraCenter = Vector2.Lerp(desiredCameraCenter, new Vector2(_orbMonster.X, monsterCenterY), _orbCameraBlend);
            }

            // ฉากบอส: เลื่อนกล้องจาก Kai ไปหาบอส (blend 0..1)
            if (_isBossScene && _bossSceneBoss != null && _bossFocusBlend > 0f)
            {
                float bossCenterY = _floorY + _bossSceneBoss.YOffset - _boss1Height / 2f;
                desiredCameraCenter = Vector2.Lerp(desiredCameraCenter, new Vector2(_bossSceneBoss.X, bossCenterY), _bossFocusBlend);
            }

            // ฉากเจอบอส 2: เลื่อนกล้องจาก Kai ไปหาบอส (blend 0..1)
            if (_isBoss2Scene && _boss2SceneBoss != null && _boss2FocusBlend > 0f)
            {
                float boss2CenterY = _floorY + _boss2SceneBoss.YOffset - _boss2Height / 2f;
                desiredCameraCenter = Vector2.Lerp(desiredCameraCenter, new Vector2(_boss2SceneBoss.X, boss2CenterY), _boss2FocusBlend);
            }

            // ฉากผีด่าน 3: เลื่อนกล้องจาก Kai ไปหาผี/บอส + กล้องสั่นตอนผีถูกดูด
            if (_isBoss3Scene && _boss3FocusBlend > 0f)
                desiredCameraCenter = Vector2.Lerp(desiredCameraCenter, _boss3FocusPoint, _boss3FocusBlend);
            if (_isBoss3Scene && _boss3ShakeActive)
                desiredCameraCenter += new Vector2(
                    (float)System.Math.Sin(_boss3ShakeTime * 55f) * Boss3CameraShakeAmplitude,
                    (float)System.Math.Cos(_boss3ShakeTime * 43f) * Boss3CameraShakeAmplitude * 0.6f);

            float viewHalfWidth = (WindowedWidth / _cameraZoom) / 2f;
            float viewHalfHeight = (WindowedHeight / _cameraZoom) / 2f;

            float minX = viewHalfWidth;
            float maxX = System.Math.Max(minX, _worldWidth - viewHalfWidth);
            float minY = viewHalfHeight;
            float maxY = System.Math.Max(minY, _worldHeight - viewHalfHeight);

            var cameraCenter = new Vector2(
                MathHelper.Clamp(desiredCameraCenter.X, minX, maxX),
                MathHelper.Clamp(desiredCameraCenter.Y, minY, maxY)
            );

            _cameraTransform =
                Matrix.CreateTranslation(-cameraCenter.X, -cameraCenter.Y, 0f) *
                Matrix.CreateScale(_cameraZoom, _cameraZoom, 1f) *
                Matrix.CreateTranslation(
                    WindowedWidth / 2f,
                    WindowedHeight / 2f,
                    0f);

            for (int i = 0; i < BackgroundLayerCount; i++)
            {
                float factor = BackgroundLayerParallaxFactors[i];
                _backgroundLayerTransforms[i] =
                    Matrix.CreateTranslation(-cameraCenter.X * factor, -cameraCenter.Y, 0f) *
                    Matrix.CreateScale(_cameraZoom, _cameraZoom, 1f) *
                    Matrix.CreateTranslation(
                        WindowedWidth / 2f,
                        WindowedHeight / 2f,
                        0f);
            }

            if (_nextRoomBackgroundTransforms != null)
            {
                for (int i = 0; i < NextRoomBackgroundLayerCount; i++)
                {
                    float factor = NextRoomBackgroundParallaxFactors[i];
                    _nextRoomBackgroundTransforms[i] =
                        Matrix.CreateTranslation(-cameraCenter.X * factor, -cameraCenter.Y, 0f) *
                        Matrix.CreateScale(_cameraZoom, _cameraZoom, 1f) *
                        Matrix.CreateTranslation(
                            WindowedWidth / 2f,
                            WindowedHeight / 2f,
                            0f);
                }
            }

            if (_level3BackgroundTransforms != null)
            {
                for (int i = 0; i < Level3BackgroundLayerCount; i++)
                {
                    float factor = Level3BackgroundParallaxFactors[i];
                    _level3BackgroundTransforms[i] =
                        Matrix.CreateTranslation(-cameraCenter.X * factor, -cameraCenter.Y, 0f) *
                        Matrix.CreateScale(_cameraZoom, _cameraZoom, 1f) *
                        Matrix.CreateTranslation(
                            WindowedWidth / 2f,
                            WindowedHeight / 2f,
                            0f);
                }
            }
        }

        private void UpdateFullscreenToggle()
        {
            var keyboard = Keyboard.GetState();
            bool f11JustPressed = keyboard.IsKeyDown(Keys.F11) && !_previousMiscKeyboardState.IsKeyDown(Keys.F11);

            if (f11JustPressed)
            {
                bool goingFullscreen = !_graphics.IsFullScreen;

                if (goingFullscreen)
                {
                    var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                    _graphics.PreferredBackBufferWidth = displayMode.Width;
                    _graphics.PreferredBackBufferHeight = displayMode.Height;
                }
                else
                {
                    _graphics.PreferredBackBufferWidth = WindowedWidth;
                    _graphics.PreferredBackBufferHeight = WindowedHeight;
                }

                _graphics.HardwareModeSwitch = false;
                _graphics.IsFullScreen = goingFullscreen;
                _graphics.ApplyChanges();
            }

            _previousMiscKeyboardState = keyboard;
        }

        // =====================================================================
        // Stamina
        // =====================================================================

        private void SpendStamina(float amount)
        {
            _currentStamina = MathHelper.Max(0f, _currentStamina - amount);
            _staminaRegenDelayTimer = 0f;

            if (_currentStamina <= 0f)
                _isExhausted = true;
        }

        private void UpdateStamina(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (!_isSprinting)
            {
                _staminaRegenDelayTimer += deltaSeconds;

                if (_staminaRegenDelayTimer >= StaminaRegenDelay)
                    _currentStamina = MathHelper.Min(_maxStamina, _currentStamina + StaminaRegenPerSecond * deltaSeconds);
            }

            if (_isExhausted && _currentStamina > StaminaRecoverThreshold)
                _isExhausted = false;
        }

        // =====================================================================
        // Command console
        // =====================================================================

        private void OnTextInput(object sender, TextInputEventArgs e)
        {
            if (!_isConsoleOpen)
                return;

            char c = e.Character;
            if (c < 32 || c > 126)
                return;
            if (_consoleText.Length >= ConsoleMaxLength)
                return;
            if (_hotbarFont != null && !_hotbarFont.Characters.Contains(c))
                return;

            _consoleText += c;
            _consoleCursorTimer = 0f;
        }

        private void UpdateConsole(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _consoleCursorTimer += deltaSeconds;
            if (_consoleMessageTimer > 0f)
                _consoleMessageTimer -= deltaSeconds;

            if (!_isConsoleOpen)
            {
                bool f3JustPressed = keyboard.IsKeyDown(Keys.F3) && !_previousConsoleKeyboardState.IsKeyDown(Keys.F3);
                if (f3JustPressed)
                {
                    _isConsoleOpen = true;
                    _consoleText = "";
                    _consoleCursorTimer = 0f;
                    _backspaceHoldTimer = 0f;
                }
            }
            else
            {
                bool enterJustPressed = keyboard.IsKeyDown(Keys.Enter) && !_previousConsoleKeyboardState.IsKeyDown(Keys.Enter);
                bool escapeJustPressed = keyboard.IsKeyDown(Keys.Escape) && !_previousConsoleKeyboardState.IsKeyDown(Keys.Escape);
                bool f3JustPressedWhileOpen = keyboard.IsKeyDown(Keys.F3) && !_previousConsoleKeyboardState.IsKeyDown(Keys.F3);

                if (escapeJustPressed || f3JustPressedWhileOpen)
                {
                    _isConsoleOpen = false;
                    _escapeHeldFromConsole = true;
                }
                else if (enterJustPressed)
                {
                    string input = _consoleText;
                    _isConsoleOpen = false;
                    ExecuteConsoleCommand(input);
                }
                else if (keyboard.IsKeyDown(Keys.Back))
                {
                    if (!_previousConsoleKeyboardState.IsKeyDown(Keys.Back))
                    {
                        RemoveLastConsoleChar();
                        _backspaceHoldTimer = 0f;
                    }
                    else
                    {
                        _backspaceHoldTimer += deltaSeconds;
                        if (_backspaceHoldTimer >= BackspaceRepeatDelay)
                        {
                            RemoveLastConsoleChar();
                            _backspaceHoldTimer -= BackspaceRepeatInterval;
                        }
                    }
                }
            }

            _previousConsoleKeyboardState = keyboard;
        }

        private void RemoveLastConsoleChar()
        {
            if (_consoleText.Length > 0)
                _consoleText = _consoleText.Substring(0, _consoleText.Length - 1);
        }

        private void ExecuteConsoleCommand(string input)
        {
            string trimmed = input.Trim();
            if (trimmed.Length == 0)
                return;

            if (!trimmed.StartsWith("/", System.StringComparison.Ordinal))
            {
                ShowConsoleMessage("Commands start with /", true);
                return;
            }

            string command = trimmed.Substring(1).ToLowerInvariant();

            const string hitMePrefix = "hit_me_";
            if (command.StartsWith(hitMePrefix, System.StringComparison.Ordinal))
            {
                string amountText = command.Substring(hitMePrefix.Length);

                if (int.TryParse(amountText, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out int damage))
                {
                    _currentHP = MathHelper.Max(0f, _currentHP - damage);
                    PlayKaiHurtSound(damage);
                    ShowConsoleMessage($"HP -{damage}  ({_currentHP:0}/{_maxHP:0})", false);
                }
                else
                {
                    ShowConsoleMessage("Usage: /hit_me_<number>  e.g. /hit_me_20", true);
                }
                return;
            }

            if (command == "spawn_dummy")
            {
                SpawnDummy();
                return;
            }

            if (command == "reset_dummy")
            {
                ResetDummyStats();
                return;
            }

            const string summonGolemPrefix = "summon_ice_golem_";
            if (command.StartsWith(summonGolemPrefix, System.StringComparison.Ordinal))
            {
                string amountText = command.Substring(summonGolemPrefix.Length);

                if (int.TryParse(amountText, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out int count) && count > 0)
                {
                    int spawnCount = System.Math.Min(count, GolemSummonMaxCount);
                    SpawnIceGolems(spawnCount);

                    if (count > GolemSummonMaxCount)
                        ShowConsoleMessage($"Summoned {spawnCount}x Ice Golem (capped at {GolemSummonMaxCount})", false);
                    else
                        ShowConsoleMessage($"Summoned {spawnCount}x Ice Golem", false);
                }
                else
                {
                    ShowConsoleMessage("Usage: /summon_ice_golem_<number>  e.g. /summon_ice_golem_3", true);
                }
                return;
            }

            const string summonMonster1Prefix = "summon_mtlv1_";
            if (command.StartsWith(summonMonster1Prefix, System.StringComparison.Ordinal))
            {
                string amountText = command.Substring(summonMonster1Prefix.Length);

                if (int.TryParse(amountText, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out int count) && count > 0)
                {
                    int spawnCount = System.Math.Min(count, Monster1SummonMaxCount);
                    SpawnMonster1List(spawnCount);

                    if (count > Monster1SummonMaxCount)
                        ShowConsoleMessage($"Summoned {spawnCount}x Monster (capped at {Monster1SummonMaxCount})", false);
                    else
                        ShowConsoleMessage($"Summoned {spawnCount}x Monster", false);
                }
                else
                {
                    ShowConsoleMessage("Usage: /summon_mtlv1_<number>  e.g. /summon_mtlv1_3", true);
                }
                return;
            }

            const string summonBoss1Prefix = "summon_boss1_";
            if (command.StartsWith(summonBoss1Prefix, System.StringComparison.Ordinal))
            {
                string amountText = command.Substring(summonBoss1Prefix.Length);

                if (int.TryParse(amountText, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out int count) && count > 0)
                {
                    int spawnCount = System.Math.Min(count, Boss1SummonMaxCount);
                    SpawnBoss1List(spawnCount);

                    if (count > Boss1SummonMaxCount)
                        ShowConsoleMessage($"Summoned {spawnCount}x Boss 1 (capped at {Boss1SummonMaxCount})", false);
                    else
                        ShowConsoleMessage($"Summoned {spawnCount}x Boss 1", false);
                }
                else
                {
                    ShowConsoleMessage("Usage: /summon_boss1_<number>  e.g. /summon_boss1_1", true);
                }
                return;
            }

            const string summonMonster2Prefix = "summon_mtlv2_";
            if (command.StartsWith(summonMonster2Prefix, System.StringComparison.Ordinal))
            {
                string amountText = command.Substring(summonMonster2Prefix.Length);

                if (!_monster2Loaded)
                {
                    ShowConsoleMessage("Monster 2 sprites not loaded: add monster_2_*.png to Content.mgcb", true);
                }
                else if (int.TryParse(amountText, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out int count) && count > 0)
                {
                    int spawnCount = System.Math.Min(count, Monster2SummonMaxCount);
                    SpawnMonster2List(spawnCount);

                    if (count > Monster2SummonMaxCount)
                        ShowConsoleMessage($"Summoned {spawnCount}x Monster 2 (capped at {Monster2SummonMaxCount})", false);
                    else
                        ShowConsoleMessage($"Summoned {spawnCount}x Monster 2", false);
                }
                else
                {
                    ShowConsoleMessage("Usage: /summon_mtlv2_<number>  e.g. /summon_mtlv2_3", true);
                }
                return;
            }

            const string summonBoss2Prefix = "summon_boss2_";
            if (command.StartsWith(summonBoss2Prefix, System.StringComparison.Ordinal))
            {
                string amountText = command.Substring(summonBoss2Prefix.Length);

                if (!_boss2Loaded)
                {
                    ShowConsoleMessage("Boss 2 sprites not loaded: add boss_2_*.png to Content.mgcb", true);
                }
                else if (int.TryParse(amountText, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out int count) && count > 0)
                {
                    int spawnCount = System.Math.Min(count, Boss2SummonMaxCount);
                    SpawnBoss2List(spawnCount);

                    if (count > Boss2SummonMaxCount)
                        ShowConsoleMessage($"Summoned {spawnCount}x Boss 2 (capped at {Boss2SummonMaxCount})", false);
                    else
                        ShowConsoleMessage($"Summoned {spawnCount}x Boss 2", false);
                }
                else
                {
                    ShowConsoleMessage("Usage: /summon_boss2_<number>  e.g. /summon_boss2_1", true);
                }
                return;
            }

            const string summonBoss3Prefix = "summon_boss3_";
            if (command.StartsWith(summonBoss3Prefix, System.StringComparison.Ordinal))
            {
                string amountText = command.Substring(summonBoss3Prefix.Length);

                if (!_boss3Loaded)
                {
                    ShowConsoleMessage("Boss 3 sprites not loaded: add boss_level3_walk.png to Content.mgcb", true);
                }
                else if (int.TryParse(amountText, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out int count) && count > 0)
                {
                    int spawnCount = System.Math.Min(count, Boss3SummonMaxCount);
                    SpawnBoss3List(spawnCount);

                    if (count > Boss3SummonMaxCount)
                        ShowConsoleMessage($"Summoned {spawnCount}x Boss 3 (capped at {Boss3SummonMaxCount})", false);
                    else
                        ShowConsoleMessage($"Summoned {spawnCount}x Boss 3", false);
                }
                else
                {
                    ShowConsoleMessage("Usage: /summon_boss3_<number>  e.g. /summon_boss3_1", true);
                }
                return;
            }

            if (command == "giveme_items")
            {
                _isGiveItemMenuOpen = true;
                _giveItemQuantityStep = false;
                _giveItemSelectedType = ItemType.None;
                _giveItemQuantity = 1;
                _isDraggingGiveSlider = false;
                ShowConsoleMessage("Select an item to give", false);
                return;
            }

            if (command == "skip_map")
            {
                if (IsLevel3Room(_currentRoomIndex))
                {
                    ShowConsoleMessage("Already in stage 3", true);
                }
                else if (_isTransitioning || _isDoorInteraction || _isBossScene || _isBoss2Scene || _isBoss3Scene || _isOrbScene || _isWakeUpSequence)
                {
                    ShowConsoleMessage("Can't skip map right now", true);
                }
                else if (IsLevel2Room(_currentRoomIndex))
                {
                    StartDoorTransition(Level3RoomIndex, Level3SpawnX, true);
                    ShowConsoleMessage("Skipping to stage 3", false);
                }
                else
                {
                    StartDoorTransition(NextLevelRoomIndex, NextLevelRoomSpawnX, true);
                    ShowConsoleMessage("Skipping to stage 2", false);
                }
                return;
            }

            if (command == "kai_god")
            {
                _isGodMode = !_isGodMode;

                if (_isGodMode)
                {
                    _currentHP = _maxHP;
                    _currentStamina = _maxStamina;
                    _isExhausted = false;
                    ShowConsoleMessage("God mode: ON (infinite HP & Stamina, immortal)", false);
                }
                else
                {
                    ShowConsoleMessage("God mode: OFF", false);
                }
                return;
            }

            ShowConsoleMessage($"Unknown command: {trimmed}", true);
        }

        private void ShowConsoleMessage(string message, bool isError)
        {
            _consoleMessage = message;
            _consoleMessageColor = isError ? new Color(255, 130, 130) : Color.White;
            _consoleMessageTimer = ConsoleMessageDuration;
        }

        private void DrawConsoleUI()
        {
            bool showMessage = _consoleMessageTimer > 0f && _consoleMessage.Length > 0;
            if (!_isConsoleOpen && !showMessage)
                return;

            var barRect = new Rectangle(
                ConsoleMargin,
                WindowedHeight - ConsoleMargin - ConsoleBarHeight,
                ConsoleBarWidth,
                ConsoleBarHeight);

            if (_isConsoleOpen)
            {
                DrawFilledRect(barRect, new Color(10, 12, 15, 220));

                string cursor = ((int)(_consoleCursorTimer * 2f) % 2 == 0) ? "_" : " ";
                string line = "> " + _consoleText + cursor;

                while (line.Length > 3 && _hotbarFont.MeasureString(line).X > barRect.Width - 20)
                    line = line.Substring(1);

                Vector2 lineSize = _hotbarFont.MeasureString(line);
                var linePosition = new Vector2(barRect.X + 10, barRect.Y + (barRect.Height - lineSize.Y) / 2f);
                _spriteBatch.DrawString(_hotbarFont, line, linePosition, Color.White);
            }

            if (showMessage)
            {
                Vector2 messageSize = _hotbarFont.MeasureString(_consoleMessage);
                var messagePosition = new Vector2(barRect.X + 10, barRect.Y - messageSize.Y - 6);
                _spriteBatch.DrawString(_hotbarFont, _consoleMessage, messagePosition, _consoleMessageColor);
            }
        }

        // =====================================================================
        // Player Death / Checkpoint Respawn
        // =====================================================================

        private void TriggerPlayerDeath()
        {
            // ไม่มีคืนชีพ/second chance แล้ว ตายปุ๊บไปหน้า Game Over ทันที
            // (กด Enter ที่หน้า Game Over เพื่อไปเกิดที่เช็คพอยต์ - ดู RespawnAtCheckpoint)
            _deathState = PlayerDeathState.GameOver;
        }

        private void RespawnAtCheckpoint()
        {
            ResetStage2Waves(false);   // เวฟด่าน 2 คงความคืบหน้า เวฟที่เล่นอยู่เริ่มใหม่จากตัวแรก
            ResetBoss2EncounterOnRespawn();
            ResetBoss3EncounterOnRespawn();
            _bossOrbs.Clear();
            _boss2Swords.Clear();
            _kaiStunTimer = 0f;
            _isAimingTrident = false;
            _isTridentThrowAnim = false;
            _tridentPoseFrame = -1;
            _tridentProjectiles.Clear();
            _parryTimer = 0f;
            _parryCooldownTimer = 0f;
            _parrySuccessFlashTimer = 0f;
            // เช็คพอยต์ = จุดเซฟที่โครงกระดูก (ถ้าเคยเซฟ) ไม่งั้นเป็นจุดเริ่มเกม (ห้อง 0 ที่ x = KaiSpawnX ฝั่งซ้ายของโลก)
            if (_checkpointRoomIndex >= 0)
            {
                _currentRoomIndex = _checkpointRoomIndex;
                _kaiCenterX = _checkpointX;
            }
            else
            {
                _currentRoomIndex = 0;
                _kaiCenterX = KaiSpawnX;
            }
            _facingRight = true;
            _kaiJumpOffsetY = 0f;
            _isJumping = false;
            _isLanding = false;
            _isAttacking = false;
            _attackFrameIndex = 0;
            _damagePopups.Clear();

            // ฟื้น HP/Stamina เต็ม 100% (ไม่รีเซ็ตสถิติ dummy/มอนสเตอร์/ของที่เก็บไปแล้ว)
            _currentHP = _maxHP;
            _currentStamina = _maxStamina;
            _isExhausted = false;

            _deathState = PlayerDeathState.Alive;
            UpdateCamera();
        }

        /// <summary>
        /// วางกุญแจ 1 ดอกไว้ข้างหน้า Kai ตามทิศที่หันอยู่ (ตอนนี้ยังไม่ถูกเรียกจากที่ไหน เก็บไว้ใช้วางกุญแจตามจุดที่ต้องการภายหลัง)
        /// ลบกุญแจตัวเก่าที่ยังไม่ถูกเก็บออกก่อน กันของกองซ้อนกันทุกครั้งที่ตาย/เกิดใหม่ซ้ำๆ
        /// </summary>
        private void SpawnKeyInFrontOfKai(int count = 1)
        {
            _worldItems.RemoveAll(item => item.Type == ItemType.Key && !item.Collected);

            float spawnOffsetX = _facingRight ? KeySpawnDropOffsetX : -KeySpawnDropOffsetX;
            _worldItems.Add(new WorldItem
            {
                Type = ItemType.Key,
                Count = count,
                X = _kaiCenterX + spawnOffsetX,
                Room = _currentRoomIndex
            });
        }

        private void UpdateGameOverState()
        {
            var keyboard = Keyboard.GetState();
            bool enterJustPressed = keyboard.IsKeyDown(Keys.Enter) && !_previousGameOverKeyboardState.IsKeyDown(Keys.Enter);

            if (enterJustPressed)
                RespawnAtCheckpoint();
            else if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            _previousGameOverKeyboardState = keyboard;
        }

        private void DrawGameOverUI()
        {
            if (_deathState != PlayerDeathState.GameOver)
                return;

            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, WindowedHeight), new Color(0, 0, 0, 220));

            string title = "YOU DIED";
            float titleScale = 2f;
            Vector2 titleSize = _hotbarFont.MeasureString(title) * titleScale;
            var titlePos = new Vector2((WindowedWidth - titleSize.X) / 2f, WindowedHeight / 2f - titleSize.Y);
            _spriteBatch.DrawString(_hotbarFont, title, titlePos, new Color(200, 30, 30), 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

            string hint = "Press Enter to respawn at checkpoint";
            Vector2 hintSize = _hotbarFont.MeasureString(hint);
            var hintPos = new Vector2((WindowedWidth - hintSize.X) / 2f, WindowedHeight / 2f + 20f);
            _spriteBatch.DrawString(_hotbarFont, hint, hintPos, Color.LightGray);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_appState == AppState.MainMenu)
            {
                GraphicsDevice.SetRenderTarget(null);
                GraphicsDevice.Clear(Color.Black);

                DrawMainMenuUI();

                base.Draw(gameTime);
                return;
            }

            if (_appState == AppState.Loading)
            {
                GraphicsDevice.SetRenderTarget(null);
                GraphicsDevice.Clear(Color.Black);

                DrawLoadingUI();

                base.Draw(gameTime);
                return;
            }

            if (_appState == AppState.IntroVideo)
            {
                GraphicsDevice.SetRenderTarget(null);
                GraphicsDevice.Clear(Color.Black);

                DrawIntroVideoUI();

                base.Draw(gameTime);
                return;
            }

            // --- Pass 1: ฉากเกมทั้งหมด ---
            GraphicsDevice.SetRenderTarget(_sceneRenderTarget);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            bool useNextRoomBackground = IsLevel2Room(_currentRoomIndex) && _nextRoomBackgroundLayers != null;
            bool useLevel3Background = IsLevel3Room(_currentRoomIndex) && _level3BackgroundLayers != null;

            if (useLevel3Background)
            {
                // ด่าน 3: วาดจากชั้นหลังสุด (LAYER_3) มาหน้าสุด (LAYER_0) ข้ามชั้นที่ต้องวาดทับหน้าตัวละคร (วาดทีหลัง)
                for (int i = Level3BackgroundLayerCount - 1; i >= 0; i--)
                {
                    if (Level3BackgroundIsForeground[i])
                        continue;

                    _spriteBatch.Begin(transformMatrix: _level3BackgroundTransforms[i]);
                    DrawBackgroundLayer(_level3BackgroundLayers[i], _level3BackgroundScale, _level3BackgroundScaledWidth);
                    _spriteBatch.End();
                }
            }
            else if (useNextRoomBackground)
            {
                // ห้องถัดไปหลังบอส: วาดจากชั้นหลังสุด (08) มาหน้าสุด (00) ข้ามชั้นที่ต้องวาดทับหน้าตัวละคร (วาดทีหลัง)
                for (int i = NextRoomBackgroundLayerCount - 1; i >= 0; i--)
                {
                    if (NextRoomBackgroundIsForeground[i])
                        continue;

                    _spriteBatch.Begin(transformMatrix: _nextRoomBackgroundTransforms[i]);
                    DrawBackgroundLayer(_nextRoomBackgroundLayers[i], _nextRoomBackgroundScale, _nextRoomBackgroundScaledWidth);
                    _spriteBatch.End();
                }
            }
            else
            {
                for (int i = BackgroundLayerCount - 1; i >= 0; i--)
                {
                    _spriteBatch.Begin(transformMatrix: _backgroundLayerTransforms[i]);
                    DrawBackgroundLayer(i);
                    _spriteBatch.End();
                }
            }

            _spriteBatch.Begin(transformMatrix: _cameraTransform);
            DrawGround();
            DrawPlatforms();
            DrawDoor();
            DrawBossExitDoor();
            DrawSavePoint();
            DrawStoneRoomProps();
            DrawWorldItems();
            DrawDummyStats();
            _spriteBatch.End();

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _cameraTransform);
            DrawDummy();
            DrawIceGolems();
            DrawMonster1List();
            DrawBoss1List();
            DrawBoss2List();
            DrawMonster2List();
            DrawBoss3List();
            _spriteBatch.End();

            _spriteBatch.Begin(transformMatrix: _cameraTransform);
            DrawKai();
            DrawBoss3Ghost();
            DrawParryEffect();
            DrawBossOrbs();
            DrawBoss2Swords();
            DrawTridentProjectiles();
            DrawTridentAim();
            DrawHitboxDebug();
            DrawBoss2HitboxDebug();
            DrawMonster2HitboxDebug();
            DrawIntroWrench();
            DrawBossScenePrompt();
            DrawDamagePopups();
            _spriteBatch.End();

            if (useLevel3Background)
            {
                for (int i = Level3BackgroundLayerCount - 1; i >= 0; i--)
                {
                    if (!Level3BackgroundIsForeground[i])
                        continue;

                    _spriteBatch.Begin(transformMatrix: _level3BackgroundTransforms[i]);
                    DrawBackgroundLayer(_level3BackgroundLayers[i], _level3BackgroundScale, _level3BackgroundScaledWidth);
                    _spriteBatch.End();
                }
            }

            // ห้องถัดไปหลังบอส: ชั้นเถาวัลย์/เพดานถ้ำที่ห้อยจากด้านบน วาดทับหน้าตัวละคร (จากชั้นหลังไปหน้า)
            if (useNextRoomBackground)
            {
                for (int i = NextRoomBackgroundLayerCount - 1; i >= 0; i--)
                {
                    if (!NextRoomBackgroundIsForeground[i])
                        continue;

                    _spriteBatch.Begin(transformMatrix: _nextRoomBackgroundTransforms[i]);
                    DrawBackgroundLayer(_nextRoomBackgroundLayers[i], _nextRoomBackgroundScale, _nextRoomBackgroundScaledWidth);
                    _spriteBatch.End();
                }
            }

            if (_isInventoryOpen)
            {
                _spriteBatch.Begin();
                DrawInventoryUI();
                _spriteBatch.End();
            }

            if (_isTransitioning)
            {
                _spriteBatch.Begin();
                DrawFilledRect(
                    new Rectangle(0, 0, WindowedWidth, WindowedHeight),
                    new Color(0, 0, 0, (int)(_transitionOverlayAlpha * 255f)));
                _spriteBatch.End();
            }

            // --- Pass 2: Vignette Shader ---
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            var destinationRect = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            _spriteBatch.Begin(effect: _vignetteEffect);
            _spriteBatch.Draw(_sceneRenderTarget, destinationRect, Color.White);
            _spriteBatch.End();

            // --- Pass 3: HUD / UI ---
            float hudScaleX = (float)GraphicsDevice.Viewport.Width / WindowedWidth;
            float hudScaleY = (float)GraphicsDevice.Viewport.Height / WindowedHeight;
            var hudTransform = Matrix.CreateScale(hudScaleX, hudScaleY, 1f);

            _spriteBatch.Begin(transformMatrix: hudTransform);
            // ซ่อนแถบ HP/Stamina กับไอคอนมุมขวาบนระหว่างฉากตื่นนอน จนกว่าฉากจะเล่นจบ
            if (!_isWakeUpSequence && !_isOrbScene && !_isDoorInteraction && !_isBossScene && !_isBoss2Scene && !_isBoss3Scene)
            {
                DrawLowHpWarning(gameTime);
                DrawHudBars();
                DrawBossHealthBar();
                DrawTopRightHud();

                // ฮอตบาร์มุมล่างซ้าย ซ่อนตอนเปิดหน้าต่างกระเป๋า (กันซ้อนกับแถวฮอตบาร์ในหน้าต่างนั้น)
                if (!_isInventoryOpen)
                    DrawHotbarHud();
            }
            DrawBuffUI();
            DrawGoRightArrow(gameTime);
            DrawRoomDebugLabel();
            DrawConsoleUI();
            DrawGiveItemMenuUI();
            DrawGameOverUI();
            DrawWakeUpOverlay();
            DrawDoorConfirmUI();
            DrawPasscodeUI();
            DrawBoss3Letterbox();
            DrawDialogueBox();
            DrawBookUI();
            DrawPauseMenuUI();
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        // ลูกศรชี้ขวาลอยกลางหน้าจอด้านขวา (หลังกำจัดบอส 2 แล้ว): ขยับเข้าออกและกะพริบเบาๆ วาดด้วยโค้ดทั้งหมด ไม่ต้องใช้รูป
        private void DrawGoRightArrow(GameTime gameTime)
        {
            if (!CanGoToLevel3() || _isTransitioning || _isBoss2Scene || _isBoss3Scene || _deathState != PlayerDeathState.Alive)
                return;

            float t = (float)gameTime.TotalGameTime.TotalSeconds;
            float bob = (float)System.Math.Sin(t * 4.0) * 12f;                          // ขยับซ้าย-ขวา
            float pulse = 0.65f + 0.35f * (float)System.Math.Sin(t * 4.0 + 1.0);        // กะพริบเบาๆ
            Color fill = new Color(255, 220, 110) * pulse;
            Color edge = new Color(40, 30, 10) * pulse;

            const int shaftLength = 70;
            const int shaftThickness = 22;
            const int headLength = 48;
            const int headHalfHeight = 40;
            const int outline = 3;

            int tipX = (int)(WindowedWidth - 60 + bob);
            int centerY = WindowedHeight / 2;
            int headBaseX = tipX - headLength;
            int shaftStartX = headBaseX - shaftLength;

            // เงา/ขอบ (วาดใหญ่กว่าเล็กน้อยก่อน)
            DrawFilledRect(new Rectangle(shaftStartX - outline, centerY - shaftThickness / 2 - outline, shaftLength + outline, shaftThickness + outline * 2), edge);
            for (int i = 0; i < headLength; i++)
            {
                int half = (int)(headHalfHeight * (1f - i / (float)headLength)) + outline;
                DrawFilledRect(new Rectangle(headBaseX + i, centerY - half, 1, half * 2), edge);
            }
            DrawFilledRect(new Rectangle(tipX, centerY - outline, outline, outline * 2), edge);

            // ตัวลูกศร: ก้านสี่เหลี่ยม + หัวสามเหลี่ยม
            DrawFilledRect(new Rectangle(shaftStartX, centerY - shaftThickness / 2, shaftLength + 1, shaftThickness), fill);
            for (int i = 0; i < headLength; i++)
            {
                int half = (int)(headHalfHeight * (1f - i / (float)headLength));
                if (half <= 0)
                    continue;
                DrawFilledRect(new Rectangle(headBaseX + i, centerY - half, 1, half * 2), fill);
            }
        }

        private void DrawBackgroundLayer(int index)
        {
            DrawBackgroundLayer(_backgroundLayers[index], _backgroundLayerScale, _backgroundLayerScaledWidth);
        }

        private void DrawBackgroundLayer(Texture2D texture, float scale, float scaledWidth)
        {
            float startX = -_worldWidth;
            float endX = _worldWidth * 2f;

            for (float x = startX; x < endX; x += scaledWidth)
            {
                _spriteBatch.Draw(
                    texture,
                    new Vector2(x, 0f),
                    null,
                    GameplayBackgroundTint,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        private void DrawGround()
        {
            int worldWidth = (int)_worldWidth;

            bool useLevel3Ground = IsLevel3Room(_currentRoomIndex) && _groundTextureLevel3 != null;
            bool useNextRoomGround = !useLevel3Ground && IsLevel2Room(_currentRoomIndex) && _groundTextureNextRoom != null;
            Texture2D groundTexture = useLevel3Ground ? _groundTextureLevel3 : (useNextRoomGround ? _groundTextureNextRoom : _groundTexture);
            int tileWidth = useLevel3Ground ? _groundTileWidthLevel3 : (useNextRoomGround ? _groundTileWidthNextRoom : _groundTileWidth);
            Rectangle sourceRect = (useLevel3Ground || useNextRoomGround)
                ? new Rectangle(0, 0, groundTexture.Width, groundTexture.Height)
                : _groundSourceRect;

            for (int x = 0; x < worldWidth; x += tileWidth)
            {
                var destRect = new Rectangle(x, _floorY, tileWidth, _floorHeight);
                _spriteBatch.Draw(groundTexture, destRect, sourceRect, Color.White);
            }
        }

        private void DrawKai()
        {
            // ก่อนจอมืดสนิทในกะพริบครั้งสุดท้าย Kai ยังนอนคว่ำอยู่ พอมืดสนิทแล้วสลับเป็นท่ายืน idle
            if (IsSleepPoseVisible())
            {
                DrawKaiSleepPose();
                return;
            }

            var frame = GetCurrentAnimFrame();

            float originX = _facingRight ? frame.OriginX : frame.Trim.Width - frame.OriginX;
            var origin = new Vector2(originX, frame.Trim.Height);
            var position = new Vector2(_kaiCenterX, _floorY + _kaiJumpOffsetY);
            SpriteEffects effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Color tint = (_kaiGreenTintTimer > 0f) ? new Color(130, 255, 130) : Color.White;
            if (_kaiStunTimer > 0f)
                tint = new Color(255, 255, 120);   // ติดสตั้น = ออกเหลือง

            _spriteBatch.Draw(
                frame.Texture,
                position,
                frame.Trim,
                tint,
                0f,
                origin,
                GetCurrentScale(),
                effects,
                0f
            );
        }

        private struct HudLayout
        {
            public Vector2 TextPosition;
            public Rectangle HeartRect;
            public Vector2 HpBarPosition;
            public Vector2 StaminaBarPosition;
            public float StaminaScaleX;
            public float StaminaScaleY;
            public int BottomY;
        }

        private static HudLayout GetHudLayout()
        {
            int hpBarWidthScaled = (int)System.Math.Round(HpBarEmptySource.Width * HpBarScale);
            int hpBarHeightScaled = (int)System.Math.Round(HpBarEmptySource.Height * HpBarScale);
            int staminaHeightScaled = (int)System.Math.Round(StaminaBarEmptySource.Height * StaminaBarScaleY);
            int heartWidthScaled = (int)System.Math.Round(HpHeartSource.Width * HeartScale);
            int heartHeightScaled = (int)System.Math.Round(HpHeartSource.Height * HeartScale);
            int heartOverlapScaled = (int)System.Math.Round(HpHeartOverlapX * HeartScale);

            int heartX = HudMarginX;
            int heartY = HudMarginY + HpTextSource.Height + HpTextToBarGap;

            int barX = heartX + heartWidthScaled - heartOverlapScaled;
            int barY = heartY + System.Math.Max(0, heartHeightScaled - staminaHeightScaled - HpToStaminaGap - hpBarHeightScaled);

            int staminaX = heartX + heartWidthScaled / 2;
            int staminaY = barY + hpBarHeightScaled + HpToStaminaGap;

            float staminaTargetWidth = hpBarWidthScaled * StaminaWidthRatioToHp;

            return new HudLayout
            {
                TextPosition = new Vector2(HudMarginX, HudMarginY),
                HeartRect = new Rectangle(heartX, heartY, heartWidthScaled, heartHeightScaled),
                HpBarPosition = new Vector2(barX, barY),
                StaminaBarPosition = new Vector2(staminaX, staminaY),
                StaminaScaleX = staminaTargetWidth / StaminaBarEmptySource.Width,
                StaminaScaleY = StaminaBarScaleY,
                BottomY = System.Math.Max(heartY + heartHeightScaled, staminaY + staminaHeightScaled)
            };
        }

        private void DrawHudBars()
        {
            var layout = GetHudLayout();

            _spriteBatch.Draw(_hpBarSheet, layout.TextPosition, HpTextSource, Color.White);

            DrawResourceBar(_hpBarSheet, HpBarEmptySource, HpBarFilledSource,
                layout.HpBarPosition, _currentHP / _maxHP, HpBarScale, HpBarScale);

            DrawResourceBar(_staminaBarSheet, StaminaBarEmptySource, StaminaBarFilledSource,
                layout.StaminaBarPosition, _currentStamina / _maxStamina, layout.StaminaScaleX, layout.StaminaScaleY);

            _spriteBatch.Draw(_hpBarSheet, layout.HeartRect, HpHeartSource, Color.White);
        }

        // =====================================================================
        // HUD มุมขวาบน: Key (สถานะ), Book (I), Backpack (Tab)
        // =====================================================================

        private void DrawTopRightHud()
        {
            // ซ่อนตอนมีเมนู/หน้าจอใดๆ เด้งขึ้นมาทับ แล้วกลับมาโชว์เองตอนกลับสู่การเล่นปกติ
            bool anyMenuOpen = _isInventoryOpen || _isGiveItemMenuOpen || _isConsoleOpen
                || _deathState == PlayerDeathState.GameOver;
            if (anyMenuOpen)
                return;

            const int slotCount = 3;
            int rowWidth = slotCount * TopRightHudIconSize + (slotCount - 1) * TopRightHudSlotGap;

            int rowX = WindowedWidth - TopRightHudMargin - rowWidth;
            int rowY = TopRightHudMargin;

            var keySlotRect = new Rectangle(rowX, rowY, TopRightHudIconSize, TopRightHudIconSize);
            var bookSlotRect = new Rectangle(keySlotRect.Right + TopRightHudSlotGap, rowY, TopRightHudIconSize, TopRightHudIconSize);
            var backpackSlotRect = new Rectangle(bookSlotRect.Right + TopRightHudSlotGap, rowY, TopRightHudIconSize, TopRightHudIconSize);

            // กุญแจ: แสดงสถานะเฉยๆ ไม่มีปุ่มกด จึงไม่มีข้อความใต้ไอคอน (เอียง 45 องศา)
            // แต่แสดงจำนวนกุญแจที่ถืออยู่เป็นตัวเลขมุมขวาบนของไอคอน (x1 / x2 / x3 ...)
            DrawTopRightHudKeyIcon(keySlotRect, _hudKeyTexture);
            DrawTopRightHudKeyCount(keySlotRect);

            DrawTopRightHudIcon(bookSlotRect, _hudBookTexture);
            DrawTopRightHudKeycap("I", bookSlotRect);

            DrawTopRightHudIcon(backpackSlotRect, _hudBackpackTexture);
            DrawTopRightHudKeycap("Tab", backpackSlotRect);
        }

        private void DrawTopRightHudIcon(Rectangle slotRect, Texture2D icon)
        {
            if (icon == null)
                return;

            _spriteBatch.Draw(icon, slotRect, Color.White);
        }

        private void DrawTopRightHudKeyIcon(Rectangle slotRect, Texture2D icon)
        {
            if (icon == null)
                return;

            // หมุนรูปกุญแจ 45 องศา รอบจุดศูนย์กลางของช่อง โดยสเกลให้พอดีกับขนาดไอคอนก่อนหมุน
            float scale = System.Math.Min((float)TopRightHudIconSize / icon.Width, (float)TopRightHudIconSize / icon.Height);
            var origin = new Vector2(icon.Width / 2f, icon.Height / 2f);
            var center = new Vector2(slotRect.X + slotRect.Width / 2f, slotRect.Y + slotRect.Height / 2f);

            _spriteBatch.Draw(icon, center, null, Color.White, MathHelper.ToRadians(45f), origin, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// รวมจำนวนไอเทมชนิดหนึ่งจากทุกช่อง (ฮอตบาร์ + กระเป๋าหลัก) เข้าด้วยกัน
        /// ใช้กับไอเทมที่สเตกไม่ได้อย่างกุญแจ ซึ่งแต่ละดอกแยกช่องกันแต่ต้องรวมยอดโชว์ที่ HUD
        /// </summary>
        private int GetItemTotalCount(ItemType type)
        {
            int total = 0;

            foreach (var slot in _hotbarSlots)
                if (slot.Type == type)
                    total += slot.Count;

            foreach (var slot in _inventoryGridSlots)
                if (slot.Type == type)
                    total += slot.Count;

            return total;
        }

        private void DrawTopRightHudKeyCount(Rectangle keySlotRect)
        {
            int keyCount = GetAnyKeyCount();
            if (keyCount <= 0)
                return;

            string countText = $"x{keyCount}";
            const float countScale = 0.75f;
            Vector2 countSize = _hotbarFont.MeasureString(countText) * countScale;

            // ป้ายตัวเลขมุมขวาบนของไอคอนกุญแจ (เยื้องออกมานอกกรอบเล็กน้อยให้เห็นชัด)
            var countPos = new Vector2(keySlotRect.Right - countSize.X * 0.5f, keySlotRect.Y - countSize.Y * 0.5f);

            _spriteBatch.DrawString(_hotbarFont, countText, countPos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, countScale, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_hotbarFont, countText, countPos, Color.Yellow, 0f, Vector2.Zero, countScale, SpriteEffects.None, 0f);
        }

        private void DrawTopRightHudKeycap(string label, Rectangle aboveSlotRect)
        {
            const float labelScale = 0.85f;
            Vector2 textSize = _hotbarFont.MeasureString(label) * labelScale;
            int keycapWidth = System.Math.Max(TopRightHudKeycapMinWidth, (int)textSize.X + 14);

            var keycapRect = new Rectangle(
                aboveSlotRect.X + (aboveSlotRect.Width - keycapWidth) / 2,
                aboveSlotRect.Bottom + TopRightHudLabelGap,
                keycapWidth,
                TopRightHudKeycapHeight);

            // เงาด้านล่างหนาขึ้น จำลองมิติปุ่มคีย์บอร์ดจริง
            var shadowRect = new Rectangle(
                keycapRect.X, keycapRect.Bottom - TopRightHudKeycapShadowHeight,
                keycapRect.Width, TopRightHudKeycapShadowHeight);

            DrawFilledRect(keycapRect, TopRightHudKeycapBg);
            DrawFilledRect(shadowRect, TopRightHudKeycapShadow);

            var textPos = new Vector2(
                keycapRect.X + (keycapRect.Width - textSize.X) / 2f,
                keycapRect.Y + (keycapRect.Height - TopRightHudKeycapShadowHeight - textSize.Y) / 2f);

            _spriteBatch.DrawString(_hotbarFont, label, textPos, Color.White, 0f, Vector2.Zero, labelScale, SpriteEffects.None, 0f);
        }

        private void DrawBuffUI()
        {
            int row = 0;

            if (_seaweedBuffTimer > 0f)
            {
                DrawBuffBox(row++, _seaweedTexture, (int)System.Math.Ceiling(_seaweedBuffTimer), "+10 ATK",
                    Color.Lime, new Color(80, 220, 120));
            }

            if (_jumpBuffTimer > 0f)
            {
                DrawBuffBox(row++, _jumpPotionTexture, (int)System.Math.Ceiling(_jumpBuffTimer), "Free Jump",
                    new Color(120, 220, 255), new Color(90, 190, 230));
            }
        }

        // กล่องบัฟมุมขวาบน เรียงลงมาทีละแถว (row 0 = บนสุด)
        private void DrawBuffBox(int row, Texture2D icon, int secondsLeft, string labelText, Color labelColor, Color outlineColor)
        {
            string timerText = $"{secondsLeft}s";

            int boxWidth = 130;
            int boxHeight = 42;
            int boxGap = 6;
            int boxX = WindowedWidth - boxWidth - HudMarginX;
            // วางกล่องบัฟไว้ใต้แถวไอคอนมุมขวาบน (Key / Book / Backpack + keycap) ไม่ให้ทับไอคอนกระเป๋า
            int topRightHudBottom = TopRightHudMargin + TopRightHudIconSize + TopRightHudLabelGap + TopRightHudKeycapHeight;
            int boxY = topRightHudBottom + boxGap + row * (boxHeight + boxGap);

            var boxRect = new Rectangle(boxX, boxY, boxWidth, boxHeight);
            DrawFilledRect(boxRect, new Color(15, 20, 25, 210));
            DrawRectOutline(boxRect, 1, outlineColor);

            if (icon != null)
            {
                var iconRect = new Rectangle(boxX + 8, boxY + 5, 32, 32);
                _spriteBatch.Draw(icon, iconRect, Color.White);
            }

            var textPos = new Vector2(boxX + 46, boxY + 4f);
            _spriteBatch.DrawString(_hotbarFont, timerText, textPos, Color.Yellow);

            Vector2 labelSize = _hotbarFont.MeasureString(labelText) * 0.65f;
            var labelPos = new Vector2(boxX + 46, boxY + boxHeight - labelSize.Y - 4f);
            _spriteBatch.DrawString(_hotbarFont, labelText, labelPos, labelColor, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        }

        private void DrawResourceBar(Texture2D texture, Rectangle emptySource, Rectangle filledSource, Vector2 position, float fillPercent, float scaleX, float scaleY)
        {
            var emptyDest = new Rectangle(
                (int)position.X,
                (int)position.Y,
                (int)System.Math.Round(emptySource.Width * scaleX),
                (int)System.Math.Round(emptySource.Height * scaleY));
            _spriteBatch.Draw(texture, emptyDest, emptySource, Color.White);

            fillPercent = MathHelper.Clamp(fillPercent, 0f, 1f);
            if (fillPercent <= 0f)
                return;

            int filledSourceWidth = (int)System.Math.Round(filledSource.Width * fillPercent);
            if (filledSourceWidth <= 0)
                return;

            var clippedSource = new Rectangle(filledSource.X, filledSource.Y, filledSourceWidth, filledSource.Height);
            var filledDest = new Rectangle(
                (int)position.X,
                (int)position.Y,
                (int)System.Math.Round(filledSourceWidth * scaleX),
                (int)System.Math.Round(filledSource.Height * scaleY));
            _spriteBatch.Draw(texture, filledDest, clippedSource, Color.White);
        }

        private void DrawInventoryUI()
        {
            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, WindowedHeight), new Color(0, 0, 0, 160));

            Vector2 mouseLogical = GetLogicalMousePosition();
            var layout = GetInventoryLayout();

            DrawFilledRect(layout.PanelRect, new Color(18, 20, 24, 235));
            DrawFilledRect(layout.TitleBarRect, new Color(10, 12, 15, 235));
            DrawTitleBarText("INVENTORY", layout.TitleBarRect);

            for (int row = 0; row < InventoryGridRows; row++)
            {
                for (int col = 0; col < InventoryGridColumns; col++)
                {
                    int index = row * InventoryGridColumns + col;
                    var slotRect = GetGridSlotRect(layout, row, col);
                    DrawInventorySlot(slotRect, slotRect.Contains(mouseLogical));

                    bool isDragSource = _isDraggingItem && !_dragFromHotbar && _dragSourceIndex == index;
                    DrawSlotItem(slotRect, _inventoryGridSlots[index], isDragSource);
                }
            }

            for (int i = 0; i < HotbarSlotCount; i++)
            {
                var slotRect = GetHotbarSlotRect(layout, i);
                DrawInventorySlot(slotRect, slotRect.Contains(mouseLogical));

                bool isDragSource = _isDraggingItem && _dragFromHotbar && _dragSourceIndex == i;
                DrawSlotItem(slotRect, _hotbarSlots[i], isDragSource);

                var labelPosition = new Vector2(slotRect.X + 5, slotRect.Y + 2);
                _spriteBatch.DrawString(_hotbarFont, (i + 1).ToString(), labelPosition, new Color(200, 205, 215));

                if (_equippedHotbarIndex == i)
                    DrawRectOutline(slotRect, 2, new Color(255, 210, 80));
            }

            // แสดงไอเทมที่กำลังลากอยู่
            if (_isDraggingItem)
            {
                var dragTexture = GetItemTexture(_dragSlot.Type);
                if (dragTexture != null)
                {
                    var dragRect = new Rectangle(
                        (int)mouseLogical.X - DragIconSize / 2,
                        (int)mouseLogical.Y - DragIconSize / 2,
                        DragIconSize,
                        DragIconSize);
                    DrawItemIcon(dragTexture, _dragSlot.Type, dragRect, Color.White * 0.9f);

                    if (_dragSlot.Count > 1)
                    {
                        string countText = _dragSlot.Count.ToString();
                        float fontScale = 0.65f;
                        Vector2 textSize = _hotbarFont.MeasureString(countText) * fontScale;
                        var textPos = new Vector2(
                            dragRect.Right - textSize.X - 2,
                            dragRect.Bottom - textSize.Y - 2);

                        _spriteBatch.DrawString(_hotbarFont, countText, textPos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);
                        _spriteBatch.DrawString(_hotbarFont, countText, textPos, Color.Yellow, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);
                    }
                }
            }
        }

        private void DrawTitleBarText(string text, Rectangle titleBarRect)
        {
            Vector2 textSize = _hotbarFont.MeasureString(text);
            var textPosition = new Vector2(
                titleBarRect.X + InventoryPanelPaddingX,
                titleBarRect.Y + (titleBarRect.Height - textSize.Y) / 2f);
            _spriteBatch.DrawString(_hotbarFont, text, textPosition, Color.White);
        }

        private void DrawRoomDebugLabel()
        {
            string text = _currentRoomIndex == SecretRoomIndex ? "Secret Room"
                : IsLevel2Room(_currentRoomIndex) ? $"Stage 2 - Room {_currentRoomIndex - NextLevelRoomIndex + 1}/{Level2RoomCount}"
                : IsLevel3Room(_currentRoomIndex) ? $"Stage 3 - Room {_currentRoomIndex - Level3RoomIndex + 1}/{Level3RoomCount}"
                : $"Room {_currentRoomIndex + 1}/{RoomCount}";

            var layout = GetHudLayout();
            var position = new Vector2(HudMarginX, layout.BottomY + 10);
            _spriteBatch.DrawString(_hotbarFont, text, position, Color.White);
        }

        private Vector2 GetLogicalMousePosition()
        {
            var mouseState = Mouse.GetState();
            float scaleX = (float)WindowedWidth / _graphics.PreferredBackBufferWidth;
            float scaleY = (float)WindowedHeight / _graphics.PreferredBackBufferHeight;
            return new Vector2(mouseState.X * scaleX, mouseState.Y * scaleY);
        }

        private void DrawInventorySlot(Rectangle slotRect, bool isHovered)
        {
            Color fillColor = isHovered
                ? new Color(90, 100, 115, 200)
                : new Color(20, 24, 32, 140);
            DrawFilledRect(slotRect, fillColor);
        }

        // =====================================================================
        // Items Management (Adding, Stacking, Dragging, Using)
        // =====================================================================

        private static bool IsKeyJustPressed(KeyboardState current, KeyboardState previous, Keys key)
        {
            return current.IsKeyDown(key) && !previous.IsKeyDown(key);
        }

        private Texture2D GetItemTexture(ItemType type)
        {
            switch (type)
            {
                case ItemType.Wrench: return _wrenchTexture;
                case ItemType.Trident: return _tridentTexture;
                case ItemType.Sword: return _boss2SwordTexture;   // ภาพเดียวกับดาบที่บอส 2 ถือ (asset "sword")
                case ItemType.Seaweed: return _seaweedTexture;
                case ItemType.HpPotion: return _hpPotionTexture;
                case ItemType.JumpPotion: return _jumpPotionTexture;
                case ItemType.Key: return _keyItemTexture;
                case ItemType.Orb: return _keyItemTexture;   // Orb แสดงเป็นรูปกุญแจ (ชนิดไอเทมในโค้ดยังเป็น Orb)
                default: return null;
            }
        }

        // มุมเอียงของไอคอนไอเทมตอนอยู่ในกระเป๋า/ฮอตบาร์/ดรอปพื้น (กุญแจเอียง 45 องศา)
        private static float GetItemIconRotation(ItemType type)
        {
            if (type == ItemType.Sword)
                return MathHelper.ToRadians(25.5f);   // ภาพดาบตั้งเกือบตรง (เอียง ~70 องศา) หมุนให้เอียง 45 องศาเหมือนไอเทมยาวอื่น และคงสัดส่วนภาพ

            return (type == ItemType.Key || type == ItemType.Orb) ? MathHelper.ToRadians(45f) : 0f;
        }

        // วาดไอคอนไอเทมโดยเอียงตาม GetItemIconRotation (หมุนรอบจุดกึ่งกลาง)
        private void DrawItemIcon(Texture2D texture, ItemType type, Rectangle iconRect, Color color)
        {
            if (texture == null)
                return;

            float rotation = GetItemIconRotation(type);
            if (rotation == 0f)
            {
                _spriteBatch.Draw(texture, iconRect, color);
                return;
            }

            var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            var position = new Vector2(iconRect.X + iconRect.Width / 2f, iconRect.Y + iconRect.Height / 2f);

            // สเกลอัตราส่วนเดียวกันทั้งแกน X/Y (ไม่ยืด/บีบภาพจนเบี้ยว) แล้วย่อลงอีกนิด (~1/sqrt(2))
            // ชดเชยกรอบล้อมรอบที่กว้างขึ้นตอนหมุนเอียง 45 องศา ไม่ให้ภาพล้นช่องหรือดูเยื้องไม่สมดุล
            float uniformScale = MathHelper.Min(iconRect.Width / (float)texture.Width, iconRect.Height / (float)texture.Height);
            uniformScale *= 0.72f;
            var scale = new Vector2(uniformScale, uniformScale);

            _spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
        }

        private static string GetItemName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Wrench: return "Wrench";
                case ItemType.Trident: return "Trident";
                case ItemType.Sword: return "Sword";
                case ItemType.Seaweed: return "Seaweed";
                case ItemType.HpPotion: return "HP Potion";
                case ItemType.JumpPotion: return "Jump Potion";
                case ItemType.Key: return "Key";
                case ItemType.Orb: return "Key";
                default: return "";
            }
        }

        /// <summary>
        /// จำนวนสูงสุดต่อช่องของไอเทมแต่ละชนิด (อาวุธ = สเตกไม่ได้ / สาหร่าย,ขวดยา = สเตกได้ 16)
        /// </summary>
        private static int GetMaxStack(ItemType type)
        {
            switch (type)
            {
                case ItemType.Wrench: return WrenchMaxStack;
                case ItemType.Trident: return TridentMaxStack;
                case ItemType.Sword: return SwordMaxStack;
                case ItemType.Seaweed: return SeaweedMaxStack;
                case ItemType.HpPotion: return HpPotionMaxStack;
                case ItemType.JumpPotion: return JumpPotionMaxStack;
                case ItemType.Key: return KeyMaxStack;
                case ItemType.Orb: return OrbMaxStack;
                default: return 1;
            }
        }

        private struct GiveOrAddPlanEntry
        {
            public bool IsHotbar;
            public int Index;
            public int Amount;
            public bool IsNewSlot;
        }

        /// <summary>
        /// ฟังก์ชันเพิ่มไอเทมเข้ากระเป๋า รองรับการรวมสแต็ก (Stacking) ตามขีดจำกัดของแต่ละไอเทม
        /// ถ้าเกินขีดจำกัดต่อช่อง จะล้นไปเติม/สร้างช่องใหม่ให้อัตโนมัติ
        /// ไอเทมที่สเตกไม่ได้ (maxStack == 1) จะแยกไปคนละช่องเสมอ
        /// เป็นแบบ all-or-nothing: ถ้าช่องไม่พอสำหรับของทั้งหมด จะไม่เพิ่มอะไรเลยและคืนค่า false
        /// </summary>
        private static bool IsMeleeWeapon(ItemType type)
        {
            return type == ItemType.Wrench || type == ItemType.Sword;
        }

        // ดาเมจพื้นฐานของอาวุธที่ถืออยู่ (ดาบ 24-36 / ประแจ 8-12) ก่อนบวกบัฟสาหร่ายและคริ
        private int RollMeleeBaseDamage()
        {
            return GetHeldItem() == ItemType.Sword
                ? _random.Next(SwordDamageMin, SwordDamageMax + 1)
                : _random.Next(WrenchDamageMin, WrenchDamageMax + 1);
        }

        private bool TryAddItem(ItemType type, int count = 1)
        {
            if (type == ItemType.None || count <= 0) return false;

            int maxStack = GetMaxStack(type);
            int remaining = count;

            var plan = new List<GiveOrAddPlanEntry>();

            // ลำดับช่องที่จะพิจารณา: Hotbar ก่อน แล้วตามด้วยกระเป๋าหลัก
            // 1. ถ้าไอเทมสเตกได้ (maxStack > 1) ให้ลองรวมเข้ากับช่องเดิมที่มีชนิดเดียวกันก่อน จนกว่าจะเต็มขีดจำกัด
            if (maxStack > 1)
            {
                for (int i = 0; i < _hotbarSlots.Length && remaining > 0; i++)
                {
                    if (_hotbarSlots[i].Type == type && _hotbarSlots[i].Count < maxStack)
                    {
                        int amount = System.Math.Min(maxStack - _hotbarSlots[i].Count, remaining);
                        plan.Add(new GiveOrAddPlanEntry { IsHotbar = true, Index = i, Amount = amount, IsNewSlot = false });
                        remaining -= amount;
                    }
                }

                for (int i = 0; i < _inventoryGridSlots.Length && remaining > 0; i++)
                {
                    if (_inventoryGridSlots[i].Type == type && _inventoryGridSlots[i].Count < maxStack)
                    {
                        int amount = System.Math.Min(maxStack - _inventoryGridSlots[i].Count, remaining);
                        plan.Add(new GiveOrAddPlanEntry { IsHotbar = false, Index = i, Amount = amount, IsNewSlot = false });
                        remaining -= amount;
                    }
                }
            }

            // 2. ของที่เหลือ (หรือไอเทมที่สเตกไม่ได้) ให้ลงช่องว่างใหม่ ช่องละไม่เกิน maxStack ชิ้น
            for (int i = 0; i < _hotbarSlots.Length && remaining > 0; i++)
            {
                if (_hotbarSlots[i].IsEmpty)
                {
                    int amount = System.Math.Min(maxStack, remaining);
                    plan.Add(new GiveOrAddPlanEntry { IsHotbar = true, Index = i, Amount = amount, IsNewSlot = true });
                    remaining -= amount;
                }
            }

            for (int i = 0; i < _inventoryGridSlots.Length && remaining > 0; i++)
            {
                if (_inventoryGridSlots[i].IsEmpty)
                {
                    int amount = System.Math.Min(maxStack, remaining);
                    plan.Add(new GiveOrAddPlanEntry { IsHotbar = false, Index = i, Amount = amount, IsNewSlot = true });
                    remaining -= amount;
                }
            }

            if (remaining > 0)
                return false; // ช่องไม่พอสำหรับของทั้งหมด (all-or-nothing ไม่เพิ่มอะไรเลย)

            // Commit แผนที่วางไว้จริง
            foreach (var entry in plan)
            {
                if (entry.IsNewSlot)
                {
                    if (entry.IsHotbar)
                    {
                        _hotbarSlots[entry.Index].Type = type;
                        _hotbarSlots[entry.Index].Count = entry.Amount;
                    }
                    else
                    {
                        _inventoryGridSlots[entry.Index].Type = type;
                        _inventoryGridSlots[entry.Index].Count = entry.Amount;
                    }
                }
                else
                {
                    if (entry.IsHotbar)
                        _hotbarSlots[entry.Index].Count += entry.Amount;
                    else
                        _inventoryGridSlots[entry.Index].Count += entry.Amount;
                }
            }

            return true;
        }

        private ItemType GetHeldItem()
        {
            if (_equippedHotbarIndex < 0)
                return ItemType.None;

            if (_hotbarSlots[_equippedHotbarIndex].IsEmpty)
            {
                _equippedHotbarIndex = -1;
                return ItemType.None;
            }

            return _hotbarSlots[_equippedHotbarIndex].Type;
        }

        private InventorySlot GetSlot(bool isHotbar, int index)
        {
            return isHotbar ? _hotbarSlots[index] : _inventoryGridSlots[index];
        }

        private void SetSlot(bool isHotbar, int index, InventorySlot slot)
        {
            if (isHotbar)
                _hotbarSlots[index] = slot;
            else
                _inventoryGridSlots[index] = slot;
        }

        private void UpdateItemPickup()
        {
            var keyboard = Keyboard.GetState();
            if (_isConsoleOpen || _isTransitioning || _isGiveItemMenuOpen)
                return;
            if (!IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.E))
                return;

            foreach (var worldItem in _worldItems)
            {
                if (worldItem.Collected || worldItem.Room != _currentRoomIndex)
                    continue;
                if (!IsWorldItemInPickupRange(worldItem))
                    continue;

                if (TryAddItem(worldItem.Type, worldItem.Count))
                {
                    worldItem.Collected = true;
                    PlaySfx(_pickupSoundEffect, PickupSoundVolume);
                    ShowConsoleMessage($"Picked up {GetItemName(worldItem.Type)} x{worldItem.Count}", false);

                    if (worldItem.Type == ItemType.Orb && !_orbSceneDone)
                        StartOrbScene();

                    return;
                }
                else
                {
                    ShowConsoleMessage("Inventory full", true);
                    return;
                }
            }
        }

        // ขอบจอแดงไล่เฉด (วาดเป็นกรอบสี่เหลี่ยมซ้อนกันเข้าหากลางจอ ชั้นนอกสุดเข้มสุด) พร้อมกะพริบเบาๆ
        private void DrawLowHpWarning(GameTime gameTime)
        {
            if (_currentHP <= 0f || _currentHP > LowHpWarningThreshold)
                return;

            float severity = 1f - _currentHP / LowHpWarningThreshold;   // 0 = เพิ่งเข้าเกณฑ์, 1 = เกือบตาย
            float pulse = 0.75f + 0.25f * (float)System.Math.Sin(gameTime.TotalGameTime.TotalSeconds * LowHpWarningPulseSpeed);
            float strength = LowHpWarningMaxAlpha * (0.7f + 0.3f * severity) * pulse;

            int th = LowHpWarningLayerThickness;
            for (int i = 0; i < LowHpWarningLayers; i++)
            {
                float t = 1f - i / (float)LowHpWarningLayers;
                var color = new Color(210, 15, 15) * (strength * t * t);   // คูณทั้งสี+อัลฟา (พรีมัลติพลาย) ให้เบลนด์ถูกต้อง

                int inset = i * th;
                int innerHeight = WindowedHeight - 2 * (inset + th);
                if (innerHeight < 0)
                    break;

                DrawFilledRect(new Rectangle(inset, inset, WindowedWidth - 2 * inset, th), color);
                DrawFilledRect(new Rectangle(inset, WindowedHeight - inset - th, WindowedWidth - 2 * inset, th), color);
                DrawFilledRect(new Rectangle(inset, inset + th, th, innerHeight), color);
                DrawFilledRect(new Rectangle(WindowedWidth - inset - th, inset + th, th, innerHeight), color);
            }
        }

        private void UpdateHitboxDebugToggle()
        {
            if (_isConsoleOpen)
                return;

            var keyboard = Keyboard.GetState();
            if (IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.F4))
            {
                _showHitboxes = !_showHitboxes;
                ShowConsoleMessage(_showHitboxes ? "Hitbox debug: ON (F4)" : "Hitbox debug: OFF", false);
            }
        }

        // ขอบสไปรต์จริงที่วาดอยู่ (ใช้พารามิเตอร์ชุดเดียวกับ SpriteBatch.Draw ของมอน)
        private void DrawSpriteBoundsDebug(Vector2 position, Vector2 origin, Rectangle trim, float scale)
        {
            var rect = new Rectangle(
                (int)(position.X - origin.X * scale),
                (int)(position.Y - origin.Y * scale),
                (int)(trim.Width * scale),
                (int)(trim.Height * scale));
            DrawRectOutline(rect, 1, new Color(255, 255, 255, 200));
        }

        private void DrawHitboxDebug()
        {
            if (!_showHitboxes)
                return;

            var hurtColor = new Color(255, 60, 60);
            var enemyAttackColor = new Color(255, 160, 40);

            // Kai: ตัว (เขียว) และระยะประแจ (เหลือง)
            float kaiHalfWidth = GetCurrentHalfWidth();
            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            DrawRectOutline(new Rectangle((int)(_kaiCenterX - kaiHalfWidth), (int)(kaiFeetY - _kaiTargetHeight),
                (int)(kaiHalfWidth * 2f), (int)_kaiTargetHeight), 2, new Color(80, 255, 80));
            DrawRectOutline(new Rectangle((int)(_facingRight ? _kaiCenterX : _kaiCenterX - AttackReach),
                (int)(kaiFeetY - _kaiTargetHeight), (int)AttackReach, (int)_kaiTargetHeight), 2, new Color(255, 230, 60));

            foreach (var golem in _iceGolems)
            {
                if (golem.Room != _currentRoomIndex || golem.State == GolemState.Dying)
                    continue;

                DrawRectOutline(new Rectangle((int)(golem.X - _golemHalfWidth), (int)(_floorY + golem.YOffset - _golemHeight),
                    (int)(_golemHalfWidth * 2f), (int)_golemHeight), 2, hurtColor);

                if (golem.State == GolemState.Attacking)
                    DrawRectOutline(new Rectangle((int)(golem.FacingRight ? golem.X : golem.X - GolemAttackReach),
                        (int)(_floorY + golem.YOffset - _golemHeight), (int)GolemAttackReach, (int)_golemHeight), 2, enemyAttackColor);
            }

            foreach (var monster in _monster1List)
            {
                if (monster.Room != _currentRoomIndex || monster.IsDying)
                    continue;

                DrawRectOutline(new Rectangle((int)(monster.X - _monster1HalfWidth), (int)(_floorY + monster.YOffset - _monster1Height),
                    (int)(_monster1HalfWidth * 2f), (int)_monster1Height), 2, hurtColor);

                if (monster.State == Monster1State.Attacking)
                    DrawRectOutline(new Rectangle((int)(monster.FacingRight ? monster.X : monster.X - Monster1AttackReach),
                        (int)(_floorY + monster.YOffset - _monster1Height), (int)Monster1AttackReach, (int)_monster1Height), 2, enemyAttackColor);
            }

            foreach (var orb in _bossOrbs)
            {
                if (orb.Room == _currentRoomIndex)
                    DrawRectOutline(new Rectangle((int)(orb.X - BossOrbRadius), (int)(orb.Y - BossOrbRadius), (int)(BossOrbRadius * 2f), (int)(BossOrbRadius * 2f)), 2, orb.Reflected ? new Color(80, 220, 255) : enemyAttackColor);
            }

            foreach (var boss in _boss1List)
            {
                if (boss.Room != _currentRoomIndex || boss.IsDying || boss.IsStatue)
                    continue;

                DrawRectOutline(new Rectangle((int)(boss.X - _boss1HalfWidth), (int)(_floorY + boss.YOffset - _boss1Height),
                    (int)(_boss1HalfWidth * 2f), (int)_boss1Height), 2, hurtColor);

                if (boss.State == Boss1State.Attacking && !boss.AttackIsOrb)
                {
                    float reach = boss.AttackIsPhase2 ? Boss1AttackReachP2 : Boss1AttackReachP1;
                    DrawRectOutline(new Rectangle((int)(boss.FacingRight ? boss.X : boss.X - reach),
                        (int)(_floorY + boss.YOffset - _boss1Height), (int)reach, (int)_boss1Height), 2, enemyAttackColor);
                }
            }
        }

        // เฟรมของท่าโจมตีศัตรู: ช่วงเงื้อ (ก่อนเฟรมโดน) เดินช้าลง AttackWindupSlowdown เท่า ส่วนหลังจากนั้นความเร็วเดิม
        private static int GetAttackFrame(float animTimer, float frameDuration, int hitFrameStart)
        {
            float windupFrameDuration = frameDuration * AttackWindupSlowdown;
            float windupTime = hitFrameStart * windupFrameDuration;

            if (animTimer < windupTime)
                return (int)(animTimer / windupFrameDuration);

            return hitFrameStart + (int)((animTimer - windupTime) / frameDuration);
        }

        // 0 = เพิ่งเริ่มเงื้อ, 1 = ถึงเฟรมโดน (จังหวะฟาด)
        private static float GetAttackWindupProgress(float animTimer, float frameDuration, int hitFrameStart)
        {
            float windupTime = hitFrameStart * frameDuration * AttackWindupSlowdown;
            return windupTime <= 0f ? 1f : MathHelper.Clamp(animTimer / windupTime, 0f, 1f);
        }

        // หลอดแดงแนวนอนเหนือหัวศัตรู (headTopY = พิกัดโลกของจุดบนสุดของตัว) แสดงเฉพาะช่วงเงื้อ หายไปตอนถึงจังหวะฟาด
        private void DrawEnemyWindupBar(float centerX, float headTopY, float progress)
        {
            if (progress >= 1f)
                return;

            int x = (int)(centerX - EnemyWindupBarWidth / 2f);
            int y = (int)(headTopY - EnemyWindupBarGapAboveHead - EnemyWindupBarHeight);

            var back = new Rectangle(x, y, EnemyWindupBarWidth, EnemyWindupBarHeight);
            DrawFilledRect(back, new Color(20, 8, 8, 220));

            int fillWidth = (int)((EnemyWindupBarWidth - 2) * progress);
            if (fillWidth > 0)
                DrawFilledRect(new Rectangle(x + 1, y + 1, fillWidth, EnemyWindupBarHeight - 2), new Color(230, 40, 40));

            DrawRectOutline(back, 1, new Color(255, 120, 120));
        }

        // เลื่อนลูกกลิ้ง: เลื่อนขึ้น = ช่องก่อนหน้า, เลื่อนลง = ช่องถัดไป (วนรอบ ข้ามช่องว่าง)
        private void UpdateHotbarScroll()
        {
            if (_scrollDeltaThisFrame == 0)
                return;

            if (_isConsoleOpen || _isGiveItemMenuOpen || _isInventoryOpen || _isTransitioning)
                return;

            int step = _scrollDeltaThisFrame > 0 ? -1 : 1;
            int start = _equippedHotbarIndex >= 0
                ? _equippedHotbarIndex
                : (step > 0 ? -1 : HotbarSlotCount);

            for (int i = 1; i <= HotbarSlotCount; i++)
            {
                int index = ((start + step * i) % HotbarSlotCount + HotbarSlotCount) % HotbarSlotCount;
                if (_hotbarSlots[index].IsEmpty)
                    continue;

                _equippedHotbarIndex = index;
                return;
            }
        }

        private void UpdateParry(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _parryTimer = System.Math.Max(0f, _parryTimer - dt);
            _parryCooldownTimer = System.Math.Max(0f, _parryCooldownTimer - dt);
            _parrySuccessFlashTimer = System.Math.Max(0f, _parrySuccessFlashTimer - dt);

            bool blocked = _isConsoleOpen || _isInventoryOpen || _isGiveItemMenuOpen || _isTransitioning || _isAttacking || _kaiStunTimer > 0f;
            if (blocked || _parryCooldownTimer > 0f)
                return;

            var keyboard = Keyboard.GetState();
            if (IsKeyJustPressed(keyboard, _previousItemKeyboardState, ParryKey))
            {
                _parryTimer = ParryWindowDuration;
                _parryCooldownTimer = ParryCooldownDuration;
            }
        }

        // แพรี่ติดเมื่ออยู่ในช่วงแพรี่ ป้องได้ทุกทิศ (ไม่ต้องหันหน้าไปทางศัตรู)
        // จำตำแหน่งศัตรูที่ป้องได้ไว้ ให้ OnParrySuccess หันตัวละครไปหาตัวที่โจมตี
        private float _parryLastEnemyX;

        private bool IsParryActiveAgainst(float enemyX)
        {
            if (_parryTimer <= 0f)
                return false;

            _parryLastEnemyX = enemyX;
            return true;
        }

        private void OnParrySuccess()
        {
            // แพรี่สำเร็จ: หันหน้าไปหาตัวที่โจมตี (ถ้าอยู่ตรงตัวพอดีคงทิศเดิม)
            if (System.Math.Abs(_parryLastEnemyX - _kaiCenterX) > 0.5f)
                _facingRight = _parryLastEnemyX >= _kaiCenterX;

            _parryTimer = 0f;
            _parryCooldownTimer = 0f;
            _parrySuccessFlashTimer = ParrySuccessFlashDuration;

            _damagePopups.Add(new DamagePopup
            {
                Text = "PARRY!",
                Position = new Vector2(_kaiCenterX, _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 30f),
                IsCrit = true,
                Color = new Color(120, 220, 255)
            });
        }

        private void DrawParryEffect()
        {
            if (_parryTimer <= 0f && _parrySuccessFlashTimer <= 0f)
                return;

            float dir = _facingRight ? 1f : -1f;
            float feetY = _floorY + _kaiJumpOffsetY;

            if (_parryTimer > 0f)
            {
                // เกราะบางๆ ด้านหน้า Kai ระหว่างช่วงแพรี่
                int w = 14;
                int h = (int)(_kaiTargetHeight * 0.9f);
                int x = (int)(_kaiCenterX + dir * (GetCurrentHalfWidth() + 6f) - (dir > 0 ? 0 : w));
                var rect = new Rectangle(x, (int)(feetY - h), w, h);
                DrawFilledRect(rect, new Color(120, 220, 255, 140));
                DrawRectOutline(rect, 2, new Color(200, 245, 255, 220));
            }

            if (_parrySuccessFlashTimer > 0f)
            {
                // แฟลชตอนแพรี่สำเร็จ
                float t = _parrySuccessFlashTimer / ParrySuccessFlashDuration;
                int w = (int)(50f + 40f * (1f - t));
                int h = (int)(_kaiTargetHeight * 1.1f);
                int x = (int)(_kaiCenterX + dir * (GetCurrentHalfWidth() + 10f) - w / 2f);
                var rect = new Rectangle(x, (int)(feetY - h), w, h);
                DrawFilledRect(rect, new Color(255, 255, 255, (int)(160 * t)));
                DrawRectOutline(rect, 3, new Color(120, 220, 255, (int)(255 * t)));
            }
        }

        private void UpdateHotbarKeys()
        {
            var keyboard = Keyboard.GetState();
            if (_isConsoleOpen || _isGiveItemMenuOpen)
                return;

            for (int i = 0; i < HotbarSlotCount; i++)
            {
                var key = (Keys)((int)Keys.D1 + i);
                if (!IsKeyJustPressed(keyboard, _previousItemKeyboardState, key))
                    continue;

                if (_hotbarSlots[i].IsEmpty)
                    continue;

                _equippedHotbarIndex = (_equippedHotbarIndex == i) ? -1 : i;
            }
        }

        private bool TryGetSlotAt(Vector2 point, InventoryLayout layout, out bool isHotbar, out int index)
        {
            for (int i = 0; i < HotbarSlotCount; i++)
            {
                if (GetHotbarSlotRect(layout, i).Contains(point))
                {
                    isHotbar = true;
                    index = i;
                    return true;
                }
            }

            for (int row = 0; row < InventoryGridRows; row++)
            {
                for (int col = 0; col < InventoryGridColumns; col++)
                {
                    if (GetGridSlotRect(layout, row, col).Contains(point))
                    {
                        isHotbar = false;
                        index = row * InventoryGridColumns + col;
                        return true;
                    }
                }
            }

            isHotbar = false;
            index = -1;
            return false;
        }

        private void UpdateInventoryDragDrop()
        {
            if (!_isInventoryOpen || _isConsoleOpen || _isGiveItemMenuOpen)
            {
                _isDraggingItem = false;
                return;
            }

            Vector2 mouse = GetLogicalMousePosition();
            var layout = GetInventoryLayout();

            bool leftJustPressed = _mouseState.LeftButton == ButtonState.Pressed
                && _previousMouseState.LeftButton == ButtonState.Released;
            bool leftJustReleased = _mouseState.LeftButton == ButtonState.Released
                && _previousMouseState.LeftButton == ButtonState.Pressed;

            if (leftJustPressed && !_isDraggingItem)
            {
                if (TryGetSlotAt(mouse, layout, out bool isHotbar, out int index))
                {
                    var slot = GetSlot(isHotbar, index);
                    if (!slot.IsEmpty)
                    {
                        _isDraggingItem = true;
                        _dragSlot = slot;
                        _dragFromHotbar = isHotbar;
                        _dragSourceIndex = index;
                    }
                }
            }
            else if (leftJustReleased && _isDraggingItem)
            {
                if (TryGetSlotAt(mouse, layout, out bool targetIsHotbar, out int targetIndex)
                    && !(targetIsHotbar == _dragFromHotbar && targetIndex == _dragSourceIndex))
                {
                    var targetSlot = GetSlot(targetIsHotbar, targetIndex);
                    var sourceSlot = GetSlot(_dragFromHotbar, _dragSourceIndex);

                    if (!targetSlot.IsEmpty && targetSlot.Type == sourceSlot.Type && GetMaxStack(targetSlot.Type) > 1)
                    {
                        // ไอเทมชนิดเดียวกันและสเตกได้ -> รวมจำนวนเท่าที่ช่องยังรับได้ (ที่เหลือค้างอยู่ช่องเดิม)
                        int maxStack = GetMaxStack(targetSlot.Type);
                        int moveAmount = System.Math.Min(maxStack - targetSlot.Count, sourceSlot.Count);

                        if (moveAmount > 0)
                        {
                            targetSlot.Count += moveAmount;
                            sourceSlot.Count -= moveAmount;
                            if (sourceSlot.Count <= 0)
                                sourceSlot.Clear();
                        }
                    }
                    else
                    {
                        // ชนิดต่างกัน หรือเป็นไอเทมที่สเตกไม่ได้ (เช่นอาวุธ) -> สลับช่องกันแทนการรวม
                        var temp = targetSlot;
                        targetSlot = sourceSlot;
                        sourceSlot = temp;
                    }

                    SetSlot(targetIsHotbar, targetIndex, targetSlot);
                    SetSlot(_dragFromHotbar, _dragSourceIndex, sourceSlot);
                }
                else if (!GetInventoryWindowRect(layout).Contains(mouse))
                {
                    // ปล่อยเมาส์นอกหน้าต่างกระเป๋า = ดรอปลงพื้น (ค้าง Ctrl = ทั้งกอง, ไม่ค้าง = ทีละ 1 ชิ้น)
                    var keyboard = Keyboard.GetState();
                    bool dropWholeStack = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
                    DropItemFromSlot(_dragFromHotbar, _dragSourceIndex, dropWholeStack);
                }

                _isDraggingItem = false;

                if (_equippedHotbarIndex >= 0 && _hotbarSlots[_equippedHotbarIndex].IsEmpty)
                    _equippedHotbarIndex = -1;
            }
        }

        // กรอบรวมของหน้าต่างกระเป๋า (แผงกริด + คอลัมน์ hotbar ด้านซ้าย) ใช้ตัดสินว่าปล่อยเมาส์ "นอกหน้าต่าง" หรือไม่
        private Rectangle GetInventoryWindowRect(InventoryLayout layout)
        {
            int hotbarWidth = HotbarSlotCount * InventorySlotSize + (HotbarSlotCount - 1) * InventorySlotSpacing;
            var hotbarRect = new Rectangle(layout.HotbarOriginX, layout.HotbarOriginY, hotbarWidth, InventorySlotSize);
            return Rectangle.Union(layout.PanelRect, hotbarRect);
        }

        // ตำแหน่ง X ที่ของดรอปจะตก: ข้างหน้า Kai ตามทิศที่หัน
        // ถ้าตำแหน่งนั้นเป็นคนละระดับกับที่ Kai ยืนอยู่ (เช่นติดกำแพง platform หรือหน้าขอบ) ให้วางตรง X ของ Kai แทน
        // กันของไปโผล่บนหลังคา platform ที่เก็บไม่ถึง หรือฝังในก้อน
        private float GetDropItemX()
        {
            float direction = _facingRight ? 1f : -1f;
            float frontX = MathHelper.Clamp(_kaiCenterX + direction * DropItemOffsetX, 0f, _worldWidth);

            float kaiSurfaceY = GetSurfaceYAt(_currentRoomIndex, _kaiCenterX, 0f);
            float frontSurfaceY = GetSurfaceYAt(_currentRoomIndex, frontX, 0f);

            return System.Math.Abs(frontSurfaceY - kaiSurfaceY) < 1f ? frontX : _kaiCenterX;
        }

        // ดรอปไอเทมจากช่องที่ระบุลงพื้น: หักจากช่อง แล้วสร้าง/รวม WorldItem ข้างหน้า Kai
        private void DropItemFromSlot(bool isHotbar, int index, bool dropWholeStack)
        {
            var slot = GetSlot(isHotbar, index);
            if (slot.IsEmpty)
                return;

            ItemType type = slot.Type;
            int amount = dropWholeStack ? slot.Count : 1;

            slot.Count -= amount;
            if (slot.Count <= 0)
                slot.Clear();
            SetSlot(isHotbar, index, slot);

            float dropX = GetDropItemX();

            bool merged = false;
            if (GetMaxStack(type) > 1)
            {
                foreach (var existing in _worldItems)
                {
                    if (existing.Collected || existing.Type != type || existing.Room != _currentRoomIndex)
                        continue;
                    if (System.Math.Abs(existing.X - dropX) > DropItemMergeDistance)
                        continue;

                    existing.Count += amount;
                    merged = true;
                    break;
                }
            }

            if (!merged)
                _worldItems.Add(new WorldItem { Type = type, Count = amount, X = dropX, Room = _currentRoomIndex });

            PlaySfx(_pickupSoundEffect, PickupSoundVolume); // ดรอปใช้เสียงเดียวกับตอนเก็บ
            ShowConsoleMessage($"Dropped {GetItemName(type)} x{amount}", false);
        }

        private void UpdateJumpBuff(GameTime gameTime)
        {
            if (_jumpBuffTimer > 0f)
                _jumpBuffTimer = MathHelper.Max(0f, _jumpBuffTimer - (float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        private void UpdateSeaweedBuff(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_seaweedBuffTimer > 0f)
                _seaweedBuffTimer = MathHelper.Max(0f, _seaweedBuffTimer - deltaSeconds);

            if (_kaiGreenTintTimer > 0f)
                _kaiGreenTintTimer = MathHelper.Max(0f, _kaiGreenTintTimer - deltaSeconds);
        }

        private void UpdateKeyIdleAnimation(GameTime gameTime)
        {
            // วนลูปตลอดเวลา ไม่ต้อง reset เพราะแค่ใช้ mod เอาเฟรมปัจจุบัน
            _keyIdleAnimTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        private int GetKeyIdleFrameIndex()
        {
            if (_keyIdleFrames == null || _keyIdleFrames.Length == 0)
                return 0;

            int frameIndex = (int)(_keyIdleAnimTimer / KeyIdleFrameDuration) % _keyIdleFrames.Length;
            return frameIndex;
        }

        private void UpdateAttack(GameTime gameTime)
        {
            ItemType heldItem = GetHeldItem();

            if (_isAttacking && (!IsMeleeWeapon(heldItem) || _kaiStunTimer > 0f))
            {
                _isAttacking = false;
                _attackFrameIndex = 0;
                _swordComboNext = 0;
                _swordComboWindowTimer = 0f;
                _swordBufferedClick = false;
            }

            bool leftJustPressed = _mouseState.LeftButton == ButtonState.Pressed
                && _previousMouseState.LeftButton == ButtonState.Released;
            bool canAction = !_isInventoryOpen && !_isConsoleOpen && !_isTransitioning && !_isGiveItemMenuOpen && _kaiStunTimer <= 0f;

            // กินสาหร่าย
            if (leftJustPressed && canAction && heldItem == ItemType.Seaweed)
            {
                ConsumeSeaweed();
                return;
            }

            // ดื่มยาฟื้น HP
            if (leftJustPressed && canAction && heldItem == ItemType.HpPotion)
            {
                ConsumeHpPotion();
                return;
            }

            // ดื่มน้ำยากระโดด
            if (leftJustPressed && canAction && heldItem == ItemType.JumpPotion)
            {
                ConsumeJumpPotion();
                return;
            }

            // โจมตีประแจ
            bool canStartAttack = IsMeleeWeapon(heldItem) && !_isAttacking && canAction && _parryTimer <= 0f;

            float attackDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // หน้าต่างเวลาต่อคอมโบดาบ: หมดเวลาแล้วคลิกถัดไปกลับไปเป็นทีแรก
            if (!_isAttacking && _swordComboWindowTimer > 0f)
            {
                _swordComboWindowTimer -= attackDelta;
                if (_swordComboWindowTimer <= 0f)
                {
                    _swordComboWindowTimer = 0f;
                    _swordComboNext = 0;
                }
            }

            if (leftJustPressed && canStartAttack)
            {
                _attackUsesSword = heldItem == ItemType.Sword && _kaiSwordAttack != null;
                _swordComboStage = (_attackUsesSword && _swordComboWindowTimer > 0f) ? _swordComboNext : 0;
                _swordComboWindowTimer = 0f;
                _swordBufferedClick = false;
                _isAttacking = true;
                _attackTimer = 0f;
                _attackFrameIndex = _attackUsesSword ? _swordComboStage * SwordAttackUsedFrameCount : 0;
                _attackHitApplied = false;
                _attackWhiffSoundPlayed = false;

                // ดาบ: เสียงฟาดเล่นทันทีที่เริ่มสวิง (ประแจเล่นตอนถึงช่วงตี เพื่อให้เสียงตีโดนเล่นแทนได้)
                if (_attackUsesSword)
                    PlaySwordSwingSound();
                return;
            }

            if (!_isAttacking)
                return;

            // จำคลิกที่กดระหว่างทีแรกกำลังเล่น ไว้ใช้เป็นทีสอง
            if (_attackUsesSword && _swordComboStage == 0 && leftJustPressed && canAction)
                _swordBufferedClick = true;

            _attackTimer += attackDelta;
            float frameDuration = _attackUsesSword ? SwordAttackFrameDuration : AttackFrameDuration;
            int frameCount = _attackUsesSword ? SwordAttackUsedFrameCount : AttackFrameCount;
            int hitStart = _attackUsesSword ? (_swordComboStage == 0 ? SwordAttackHitFrameStart : SwordCombo2HitFrameStart) : AttackHitFrameStart;
            int hitEnd = _attackUsesSword ? (_swordComboStage == 0 ? SwordAttackHitFrameEnd : SwordCombo2HitFrameEnd) : AttackHitFrameEnd;
            int frameIndex = (int)(_attackTimer / frameDuration);

            if (frameIndex >= frameCount)
            {
                if (_attackUsesSword && _swordComboStage == 0)
                {
                    if (_swordBufferedClick)
                    {
                        // มีคลิกที่จำไว้: ต่อเป็นทีสองทันที ไม่มีช่องว่าง
                        _swordComboStage = 1;
                        _swordBufferedClick = false;
                        _attackTimer = 0f;
                        _attackHitApplied = false;
                        _attackFrameIndex = SwordAttackUsedFrameCount;
                        PlaySwordSwingSound();   // ทีสองของคอมโบดาบ
                        return;
                    }

                    _swordComboNext = 1;
                    _swordComboWindowTimer = SwordComboWindow;
                }
                else if (_attackUsesSword)
                {
                    _swordComboNext = 0;   // จบทีสอง: คลิกถัดไปวนกลับไปทีแรก
                    _swordComboWindowTimer = 0f;
                }

                _isAttacking = false;
                _attackFrameIndex = 0;
            }
            else
            {
                _attackFrameIndex = _attackUsesSword ? _swordComboStage * SwordAttackUsedFrameCount + frameIndex : frameIndex;

                if (!_attackHitApplied && frameIndex >= hitStart && frameIndex <= hitEnd)
                {
                    TryApplyAttackHit();

                    // ประแจ: ถึงช่วงตีแล้วยังไม่โดนอะไร = เล่นเสียงฟาด (ถ้าโดนจะเล่นเสียงตีโดนแทนใน TryApplyAttackHit)
                    if (!_attackUsesSword && !_attackHitApplied && !_attackWhiffSoundPlayed)
                    {
                        _attackWhiffSoundPlayed = true;
                        PlaySwingSound();
                    }
                }
            }
        }

        private void ConsumeSeaweed()
        {
            if (_equippedHotbarIndex < 0) return;

            int index = _equippedHotbarIndex;
            if (_hotbarSlots[index].Type != ItemType.Seaweed || _hotbarSlots[index].IsEmpty) return;

            // 1. หักสาหร่ายทีละ 1 ชิ้น
            _hotbarSlots[index].Count--;
            if (_hotbarSlots[index].Count <= 0)
            {
                _hotbarSlots[index].Clear();
                _equippedHotbarIndex = -1;
            }

            PlaySfx(_eatSoundEffect, EatSoundVolume);

            // 2. สะสมเวลาบัฟ +30 วินาที (เพิ่มพลังโจมตีอย่างเดียว ไม่ลดเลือด)
            _seaweedBuffTimer += SeaweedBuffDuration;

            // 3. กระพริบออร่าสีเขียว
            _kaiGreenTintTimer = EatEffectDuration;

            // 4. ข้อความตัวเลขลอยขึ้นบนหัว
            float kaiHeadY = _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 10f;
            _damagePopups.Add(new DamagePopup
            {
                Text = "+10 ATK!",
                Position = new Vector2(_kaiCenterX, kaiHeadY),
                Color = new Color(100, 255, 100),
                IsCrit = true
            });

            ShowConsoleMessage($"Ate Seaweed! ATK +10 (Buff: {(int)_seaweedBuffTimer}s)", false);
        }

        private void ConsumeJumpPotion()
        {
            if (_equippedHotbarIndex < 0) return;

            int index = _equippedHotbarIndex;
            if (_hotbarSlots[index].Type != ItemType.JumpPotion || _hotbarSlots[index].IsEmpty) return;

            _hotbarSlots[index].Count--;
            if (_hotbarSlots[index].Count <= 0)
            {
                _hotbarSlots[index].Clear();
                _equippedHotbarIndex = -1;
            }

            PlaySfx(_potionSoundEffect, PotionSoundVolume);

            // ดื่มซ้ำตอนบัฟยังเหลือ = บวกเวลาเพิ่ม (สะสมได้เหมือนสาหร่าย)
            _jumpBuffTimer += JumpPotionBuffDuration;

            float kaiHeadY = _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 10f;
            _damagePopups.Add(new DamagePopup
            {
                Text = "Free Jump!",
                Position = new Vector2(_kaiCenterX, kaiHeadY),
                Color = new Color(120, 220, 255),
                IsCrit = true
            });

            ShowConsoleMessage($"Drank Jump Potion! Free jumps (Buff: {(int)System.Math.Ceiling(_jumpBuffTimer)}s)", false);
        }

        private void ConsumeHpPotion()
        {
            if (_equippedHotbarIndex < 0) return;

            int index = _equippedHotbarIndex;
            if (_hotbarSlots[index].Type != ItemType.HpPotion || _hotbarSlots[index].IsEmpty) return;

            // 1. หักขวดยาทีละ 1 ชิ้น
            _hotbarSlots[index].Count--;
            if (_hotbarSlots[index].Count <= 0)
            {
                _hotbarSlots[index].Clear();
                _equippedHotbarIndex = -1;
            }

            PlaySfx(_potionSoundEffect, PotionSoundVolume);

            // 2. ฟื้นพลังชีวิต +50 (ไม่เกินค่าสูงสุด) ไม่มีผลเสียใดๆ
            _currentHP = MathHelper.Min(_maxHP, _currentHP + HpPotionHealAmount);

            // 3. ข้อความตัวเลขลอยขึ้นบนหัว
            float kaiHeadY = _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 10f;
            _damagePopups.Add(new DamagePopup
            {
                Text = $"+{(int)HpPotionHealAmount} HP",
                Position = new Vector2(_kaiCenterX, kaiHeadY),
                Color = new Color(100, 255, 100),
                IsCrit = true
            });

            ShowConsoleMessage($"Drank HP Potion! +{(int)HpPotionHealAmount} HP", false);
        }

        // =====================================================================
        // Training dummy & Damage
        // =====================================================================

        private float GetDummyHalfWidth()
        {
            return _dummyTrim.Width * _dummyScale / 2f;
        }

        private float GetDummyHeight()
        {
            return _dummyTrim.Height * _dummyScale;
        }

        private void TryApplyAttackHit()
        {
            float feetY = _floorY + _kaiJumpOffsetY;
            var hitbox = new Rectangle(
                (int)(_facingRight ? _kaiCenterX : _kaiCenterX - AttackReach),
                (int)(feetY - _kaiTargetHeight),
                (int)AttackReach,
                (int)_kaiTargetHeight);

            bool hitSomething = false;

            if (_dummyExists && _dummyRoom == _currentRoomIndex)
            {
                float halfWidth = GetDummyHalfWidth();
                float dummyHeight = GetDummyHeight();
                var dummyRect = new Rectangle(
                    (int)(_dummyX - halfWidth),
                    (int)(_floorY - dummyHeight),
                    (int)(halfWidth * 2f),
                    (int)dummyHeight);

                if (hitbox.Intersects(dummyRect))
                {
                    ApplyDummyHit();
                    hitSomething = true;
                }
            }

            foreach (var golem in _iceGolems)
            {
                if (golem.Room != _currentRoomIndex || golem.State == GolemState.Dying)
                    continue;

                var golemRect = new Rectangle(
                    (int)(golem.X - _golemHalfWidth),
                    (int)(_floorY + golem.YOffset - _golemHeight),
                    (int)(_golemHalfWidth * 2f),
                    (int)_golemHeight);

                if (hitbox.Intersects(golemRect))
                {
                    ApplyGolemHit(golem);
                    hitSomething = true;
                }
            }

            foreach (var monster in _monster1List)
            {
                if (monster.Room != _currentRoomIndex || monster.IsDying)
                    continue;

                var monsterRect = new Rectangle(
                    (int)(monster.X - _monster1HalfWidth),
                    (int)(_floorY + monster.YOffset - _monster1Height),
                    (int)(_monster1HalfWidth * 2f),
                    (int)_monster1Height);

                if (hitbox.Intersects(monsterRect))
                {
                    ApplyMonster1Hit(monster);
                    hitSomething = true;
                }
            }

            foreach (var boss in _boss1List)
            {
                if (boss.Room != _currentRoomIndex || boss.IsDying || boss.IsStatue)
                    continue;

                var bossRect = new Rectangle(
                    (int)(boss.X - _boss1HalfWidth),
                    (int)(_floorY + boss.YOffset - _boss1Height),
                    (int)(_boss1HalfWidth * 2f),
                    (int)_boss1Height);

                if (hitbox.Intersects(bossRect))
                {
                    ApplyBoss1Hit(boss);
                    hitSomething = true;
                }
            }

            foreach (var boss in _boss2List)
            {
                if (boss.Room != _currentRoomIndex || boss.IsDying)
                    continue;

                var bossRect = new Rectangle(
                    (int)(boss.X - _boss2HalfWidth),
                    (int)(_floorY + boss.YOffset - _boss2Height),
                    (int)(_boss2HalfWidth * 2f),
                    (int)_boss2Height);

                if (hitbox.Intersects(bossRect))
                {
                    ApplyBoss2Hit(boss);
                    hitSomething = true;
                }
            }

            foreach (var boss in _boss3List)
            {
                if (boss.Room != _currentRoomIndex || boss.IsDying || boss.IsStatue)
                    continue;

                if (hitbox.Intersects(GetBoss3Rect(boss)))
                {
                    ApplyBoss3Hit(boss);
                    hitSomething = true;
                }
            }

            foreach (var monster in _monster2List)
            {
                if (monster.Room != _currentRoomIndex || monster.IsDying)
                    continue;

                if (hitbox.Intersects(GetMonster2Rect(monster)))
                {
                    ApplyMonster2Hit(monster);
                    hitSomething = true;
                }
            }

            if (hitSomething)
            {
                _attackHitApplied = true;

                // ประแจตีโดนเป้าหมาย: เล่นเสียงตีโดนแทนเสียงฟาด (ครั้งเดียวต่อหนึ่งสวิง แม้โดนหลายตัว) ดาบไม่มีเสียงตีโดน
                if (GetHeldItem() == ItemType.Wrench)
                {
                    if (_swingSoundInstance != null)
                        _swingSoundInstance.Stop();
                    PlaySfx(_wrenchHitSoundEffect, WrenchHitSoundVolume);
                }
            }
        }

        private void ApplyDummyHit()
        {
            int baseDamage = RollMeleeBaseDamage();
            if (_seaweedBuffTimer > 0f)
            {
                baseDamage += SeaweedAtkBonus;
            }

            int damage = baseDamage;
            bool isCrit = _random.NextDouble() < CritChance;
            if (isCrit)
                damage *= CritMultiplier;

            _dummyHitCount++;
            _dummyTotalDamage += damage;
            _dummyShakeTimer = DummyShakeDuration;

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(_dummyX + _random.Next(-20, 21), _floorY - GetDummyHeight() - 8f),
                IsCrit = isCrit,
                Color = isCrit ? new Color(255, 210, 80) : Color.White
            });
        }

        // =====================================================================
        // Ice Golem
        // =====================================================================

        private void SpawnIceGolems(int count)
        {
            float direction = _facingRight ? 1f : -1f;
            float baseX = _kaiCenterX + direction * GolemSpawnDistance;

            for (int i = 0; i < count; i++)
            {
                float x = MathHelper.Clamp(
                    baseX + direction * i * GolemSpawnSpacing,
                    _golemHalfWidth,
                    _worldWidth - _golemHalfWidth);

                _iceGolems.Add(new IceGolem
                {
                    X = x,
                    SpawnX = x,
                    Room = _currentRoomIndex,
                    HP = GolemMaxHP,
                    State = GolemState.Idle,
                    FacingRight = !_facingRight,
                    // เกิดตรงจุดที่คร่อมแผ่นพื้นที่ยกสูง → วางบนผิวแผ่น (ไม่ให้ฝังอยู่ในก้อน)
                    YOffset = GetSurfaceYAt(_currentRoomIndex, x, _golemHalfWidth) - _floorY
                });
            }
        }

        private void UpdateIceGolems(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = _iceGolems.Count - 1; i >= 0; i--)
            {
                var golem = _iceGolems[i];

                if (golem.Room != _currentRoomIndex)
                    continue;

                if (golem.AttackCooldownTimer > 0f)
                    golem.AttackCooldownTimer = MathHelper.Max(0f, golem.AttackCooldownTimer - deltaSeconds);

                golem.AnimTimer += deltaSeconds;

                // ฟิสิกส์แนวตั้ง: ตกจากขอบพื้นที่ยกสูง / ลงจอดหลังกระโดดขึ้นแผ่น
                float golemVelocityY = golem.VelocityY;
                bool golemAirborne = golem.Airborne;
                float golemYOffset = golem.YOffset;
                UpdateMonsterVertical(golem.Room, golem.X, _golemHalfWidth, ref golemYOffset, ref golemVelocityY, ref golemAirborne, deltaSeconds);
                golem.YOffset = golemYOffset;
                golem.VelocityY = golemVelocityY;
                golem.Airborne = golemAirborne;

                if (golem.State == GolemState.Dying)
                {
                    int frame = (int)(golem.AnimTimer / GolemDieFrameDuration);
                    if (frame >= GolemDieFrameCount)
                    {
                        DropHpPotions(golem.X);
                        _iceGolems.RemoveAt(i);
                        continue;
                    }
                    golem.FrameIndex = frame;
                    continue;
                }

                if (golem.State == GolemState.Hurt)
                {
                    int frame = (int)(golem.AnimTimer / GolemHurtFrameDuration);
                    if (frame >= GolemHurtFrameCount)
                    {
                        golem.State = GolemState.Idle;
                        golem.AnimTimer = 0f;
                        golem.FrameIndex = 0;
                    }
                    else
                    {
                        golem.FrameIndex = frame;
                    }
                    continue;
                }

                if (golem.State == GolemState.Attacking)
                {
                    golem.FacingRight = _kaiCenterX >= golem.X;
                    int frame = GetAttackFrame(golem.AnimTimer, GolemAttackFrameDuration, GolemAttackHitFrameStart);

                    if (frame >= GolemAttackFrameCount)
                    {
                        golem.State = GolemState.Idle;
                        golem.AnimTimer = 0f;
                        golem.FrameIndex = 0;
                        golem.AttackCooldownTimer = GolemAttackCooldown;
                    }
                    else
                    {
                        golem.FrameIndex = frame;

                        if (!golem.AttackHitApplied && frame >= GolemAttackHitFrameStart && frame <= GolemAttackHitFrameEnd)
                            TryApplyGolemAttackHit(golem);
                    }
                    continue;
                }

                // Idle / Walking: ตัดสินใจว่าจะยืนเฉย, ไล่เข้าหา Kai, เข้าโจมตี หรือกลับจุดเกิด
                float distanceToKai = System.Math.Abs(_kaiCenterX - golem.X);
                bool golemCanReachKaiVertically = IsMonsterVerticallyOverlappingKai(golem.YOffset, _golemHeight);

                if (distanceToKai <= GolemAttackRange && golemCanReachKaiVertically)
                {
                    // ระยะโจมตี: หันหน้าเข้าหาผู้เล่นเสมอ และถือว่าอยู่ในสถานะไล่ล่าแล้ว
                    golem.IsAggro = true;
                    golem.FacingRight = _kaiCenterX >= golem.X;

                    if (golem.AttackCooldownTimer <= 0f)
                    {
                        golem.State = GolemState.Attacking;
                        golem.AnimTimer = 0f;
                        golem.FrameIndex = 0;
                        golem.AttackHitApplied = false;
                    }
                    else
                    {
                        golem.State = GolemState.Idle;
                        golem.FrameIndex = (int)(golem.AnimTimer / GolemFrameDuration) % GolemIdleFrameCount;
                    }
                }
                else if (distanceToKai <= GolemAggroRange)
                {
                    // อยู่ในระยะตรวจจับ: เดินไล่เข้าหาผู้เล่น
                    golem.IsAggro = true;
                    golem.FacingRight = _kaiCenterX >= golem.X;

                    if (distanceToKai <= 6f)
                    {
                        // อยู่ตรงตำแหน่ง X เดียวกับผู้เล่น (แต่คนละชั้นความสูง) ไม่ต้องขยับไปมา
                        golem.State = GolemState.Idle;
                        golem.FrameIndex = (int)(golem.AnimTimer / GolemFrameDuration) % GolemIdleFrameCount;
                    }
                    else
                    {
                        golem.State = GolemState.Walking;
                        float direction = golem.FacingRight ? 1f : -1f;
                        float golemVy = golem.VelocityY;
                        bool golemAir = golem.Airborne;
                        golem.X = MoveMonsterHorizontally(golem.Room, golem.X, direction * GolemWalkSpeed * deltaSeconds,
                            _golemHalfWidth, _golemHeight, golem.YOffset, ref golemVy, ref golemAir);
                        golem.VelocityY = golemVy;
                        golem.Airborne = golemAir;
                        golem.X = MathHelper.Clamp(golem.X, _golemHalfWidth, _worldWidth - _golemHalfWidth);
                        golem.FrameIndex = (int)(golem.AnimTimer / GolemFrameDuration) % GolemWalkFrameCount;
                    }
                }
                else if (golem.IsAggro)
                {
                    // ผู้เล่นออกนอกระยะตรวจจับแล้ว: เดินกลับจุดเกิด
                    float distanceToSpawn = System.Math.Abs(golem.SpawnX - golem.X);

                    if (distanceToSpawn <= GolemReturnThreshold)
                    {
                        golem.X = golem.SpawnX;
                        golem.IsAggro = false;
                        golem.State = GolemState.Idle;
                        golem.FrameIndex = (int)(golem.AnimTimer / GolemFrameDuration) % GolemIdleFrameCount;
                    }
                    else
                    {
                        golem.State = GolemState.Walking;
                        float direction = golem.SpawnX >= golem.X ? 1f : -1f;
                        golem.FacingRight = direction > 0f;
                        float golemVy = golem.VelocityY;
                        bool golemAir = golem.Airborne;
                        golem.X = MoveMonsterHorizontally(golem.Room, golem.X, direction * GolemWalkSpeed * deltaSeconds,
                            _golemHalfWidth, _golemHeight, golem.YOffset, ref golemVy, ref golemAir);
                        golem.VelocityY = golemVy;
                        golem.Airborne = golemAir;
                        golem.X = MathHelper.Clamp(golem.X, _golemHalfWidth, _worldWidth - _golemHalfWidth);
                        golem.FrameIndex = (int)(golem.AnimTimer / GolemFrameDuration) % GolemWalkFrameCount;
                    }
                }
                else
                {
                    // ยังไม่เคยตรวจพบผู้เล่น: ยืนเฉยอยู่ที่จุดเกิด
                    golem.State = GolemState.Idle;
                    golem.FrameIndex = (int)(golem.AnimTimer / GolemFrameDuration) % GolemIdleFrameCount;
                }
            }
        }

        private void DropHpPotions(float x)
        {
            int count = _random.Next(HpPotionMinDropCount, HpPotionMaxDropCount + 1);
            _worldItems.Add(new WorldItem { Type = ItemType.HpPotion, Count = count, X = x, Room = _currentRoomIndex });

            // น้ำยากระโดด: ดรอปตามโอกาสจากมอนปกติ (ใช้จุดเดียวกับยา HP จึงครอบคลุมทั้ง Ice Golem และ Monster1)
            if (_random.NextDouble() < JumpPotionDropChance)
            {
                float jumpX = MathHelper.Clamp(x + JumpPotionDropOffsetX, 0f, _worldWidth);
                _worldItems.Add(new WorldItem { Type = ItemType.JumpPotion, Count = JumpPotionDropCount, X = jumpX, Room = _currentRoomIndex });
            }
        }

        private void TryApplyGolemAttackHit(IceGolem golem)
        {
            float halfWidth = GetCurrentHalfWidth();
            var kaiRect = new Rectangle(
                (int)(_kaiCenterX - halfWidth),
                (int)(_floorY + _kaiJumpOffsetY - _kaiTargetHeight),
                (int)(halfWidth * 2f),
                (int)_kaiTargetHeight);

            var golemHitbox = new Rectangle(
                (int)(golem.FacingRight ? golem.X : golem.X - GolemAttackReach),
                (int)(_floorY + golem.YOffset - _golemHeight),
                (int)GolemAttackReach,
                (int)_golemHeight);

            if (!golemHitbox.Intersects(kaiRect))
                return;

            if (IsParryActiveAgainst(golem.X))
            {
                golem.AttackHitApplied = true;
                OnParrySuccess();
                ApplyGolemHit(golem, _random.Next(ParryDamageMin, ParryDamageMax + 1));
                return;
            }

            golem.AttackHitApplied = true;
            int damage = _random.Next(GolemDamageMin, GolemDamageMax + 1);
            _currentHP = MathHelper.Max(0f, _currentHP - damage);
            PlayKaiHurtSound(damage);

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(_kaiCenterX + _random.Next(-15, 16), _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 8f),
                Color = new Color(255, 120, 120)
            });
        }

        private void ApplyGolemHit(IceGolem golem, int damageOverride = 0)
        {
            int baseDamage = RollMeleeBaseDamage();
            if (_seaweedBuffTimer > 0f)
                baseDamage += SeaweedAtkBonus;

            int damage = baseDamage;
            bool isCrit = damageOverride <= 0 && _random.NextDouble() < CritChance;
            if (isCrit)
                damage *= CritMultiplier;
            if (damageOverride > 0)
                damage = damageOverride;   // ดาเมจจากแพรี่ (ไม่คริ ไม่บวกบัฟ)

            golem.HP -= damage;

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(golem.X + _random.Next(-20, 21), _floorY + golem.YOffset - _golemHeight - 8f),
                IsCrit = isCrit,
                Color = damageOverride > 0 ? new Color(120, 220, 255) : (isCrit ? new Color(255, 210, 80) : Color.White)
            });

            if (golem.HP <= 0f)
            {
                golem.HP = 0f;
                if (golem.State != GolemState.Dying)
                    PlaySfx(_monsterDeathSoundEffect, MonsterDeathSoundVolume);
                golem.State = GolemState.Dying;
            }
            else
            {
                golem.State = GolemState.Hurt;
            }

            golem.AnimTimer = 0f;
            golem.FrameIndex = 0;
        }

        private AnimFrame GetGolemFrame(IceGolem golem)
        {
            switch (golem.State)
            {
                case GolemState.Walking:
                    return _golemWalk[System.Math.Min(golem.FrameIndex, GolemWalkFrameCount - 1)];
                case GolemState.Attacking:
                    return _golemAttack[System.Math.Min(golem.FrameIndex, GolemAttackFrameCount - 1)];
                case GolemState.Hurt:
                    return _golemHurt[System.Math.Min(golem.FrameIndex, GolemHurtFrameCount - 1)];
                case GolemState.Dying:
                    return _golemDie[System.Math.Min(golem.FrameIndex, GolemDieFrameCount - 1)];
                default:
                    return _golemIdle[System.Math.Min(golem.FrameIndex, GolemIdleFrameCount - 1)];
            }
        }

        private void DrawIceGolems()
        {
            foreach (var golem in _iceGolems)
            {
                if (golem.Room != _currentRoomIndex)
                    continue;

                var frame = GetGolemFrame(golem);
                var origin = new Vector2(frame.Trim.Width / 2f, frame.Trim.Height);
                var position = new Vector2(golem.X, _floorY + golem.YOffset);
                SpriteEffects effects = golem.FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                _spriteBatch.Draw(frame.Texture, position, frame.Trim, Color.White, 0f, origin, _golemScale, effects, 0f);

                if (golem.State == GolemState.Attacking)
                    DrawEnemyWindupBar(golem.X, _floorY + golem.YOffset - _golemHeight,
                        GetAttackWindupProgress(golem.AnimTimer, GolemAttackFrameDuration, GolemAttackHitFrameStart));

                if (_showHitboxes)
                    DrawSpriteBoundsDebug(position, origin, frame.Trim, _golemScale);
            }
        }

        // =====================================================================
        // Monster 1 (มอนสเตอร์ธรรมดา)
        // =====================================================================

        private void SpawnMonster1List(int count)
        {
            float direction = _facingRight ? 1f : -1f;
            float baseX = _kaiCenterX + direction * Monster1SpawnDistance;

            for (int i = 0; i < count; i++)
            {
                float x = MathHelper.Clamp(
                    baseX + direction * i * Monster1SpawnSpacing,
                    _monster1HalfWidth,
                    _worldWidth - _monster1HalfWidth);

                _monster1List.Add(new Monster1
                {
                    X = x,
                    SpawnX = x,
                    Room = _currentRoomIndex,
                    HP = Monster1MaxHP,
                    State = Monster1State.Idle,
                    FacingRight = !_facingRight,
                    // เกิดตรงจุดที่คร่อมแผ่นพื้นที่ยกสูง → วางบนผิวแผ่น (ไม่ให้ฝังอยู่ในก้อน)
                    YOffset = GetSurfaceYAt(_currentRoomIndex, x, _monster1HalfWidth) - _floorY
                });
            }
        }

        private void UpdateMonster1List(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = _monster1List.Count - 1; i >= 0; i--)
            {
                var monster = _monster1List[i];

                if (monster.Room != _currentRoomIndex)
                    continue;

                if (monster.IsScripted)
                    continue; // ฉาก orb คุมอยู่ (UpdateOrbSceneMonster)

                if (monster.HitFlashTimer > 0f)
                    monster.HitFlashTimer = MathHelper.Max(0f, monster.HitFlashTimer - deltaSeconds);

                if (monster.AttackCooldownTimer > 0f)
                    monster.AttackCooldownTimer = MathHelper.Max(0f, monster.AttackCooldownTimer - deltaSeconds);

                monster.AnimTimer += deltaSeconds;

                // ฟิสิกส์แนวตั้ง: ตกจากขอบพื้นที่ยกสูง / ลงจอดหลังกระโดดขึ้นแผ่น
                float monsterVelocityY = monster.VelocityY;
                bool monsterAirborne = monster.Airborne;
                float monsterYOffset = monster.YOffset;
                UpdateMonsterVertical(monster.Room, monster.X, _monster1HalfWidth, ref monsterYOffset, ref monsterVelocityY, ref monsterAirborne, deltaSeconds);
                monster.YOffset = monsterYOffset;
                monster.VelocityY = monsterVelocityY;
                monster.Airborne = monsterAirborne;

                // ท่าตาย: เล่นเฟรมตามเวลา -> ท่าจบแล้วดรอปยา (ครั้งเดียว) -> ค้างเฟรมสุดท้ายพร้อมจางหาย -> ลบออก
                if (monster.IsDying)
                {
                    monster.DeathTimer += deltaSeconds;

                    if (!monster.DeathDropDone && monster.DeathTimer >= Monster1DieAnimDuration)
                    {
                        DropHpPotions(monster.X);
                        monster.DeathDropDone = true;
                    }

                    if (monster.DeathTimer >= Monster1DieTotalDuration)
                        _monster1List.RemoveAt(i);

                    continue;
                }

                if (monster.State == Monster1State.Attacking)
                {
                    monster.FacingRight = _kaiCenterX >= monster.X;
                    int frame = GetAttackFrame(monster.AnimTimer, Monster1AttackFrameDuration, Monster1AttackHitFrameStart);

                    if (frame >= Monster1AttackFrameCount)
                    {
                        monster.State = Monster1State.Idle;
                        monster.AnimTimer = 0f;
                        monster.FrameIndex = 0;
                        monster.AttackCooldownTimer = Monster1AttackCooldown;
                    }
                    else
                    {
                        monster.FrameIndex = frame;

                        if (!monster.AttackHitApplied && frame >= Monster1AttackHitFrameStart && frame <= Monster1AttackHitFrameEnd)
                            TryApplyMonster1AttackHit(monster);
                    }
                    continue;
                }

                // Idle / Walking: ยืนเฉยจนผู้เล่นเข้าใกล้ค่อยไล่ล่า/โจมตี (เหมือน Ice Golem)
                float distanceToKai = System.Math.Abs(_kaiCenterX - monster.X);
                bool monsterCanReachKaiVertically = IsMonsterVerticallyOverlappingKai(monster.YOffset, _monster1Height);

                if (distanceToKai <= Monster1AttackRange && monsterCanReachKaiVertically)
                {
                    AggroMonster1(monster);
                    monster.FacingRight = _kaiCenterX >= monster.X;

                    if (monster.AttackCooldownTimer <= 0f)
                    {
                        monster.State = Monster1State.Attacking;
                        monster.AnimTimer = 0f;
                        monster.FrameIndex = 0;
                        monster.AttackHitApplied = false;
                    }
                    else
                    {
                        monster.State = Monster1State.Idle;
                        monster.FrameIndex = 0;
                    }
                }
                else if (distanceToKai <= Monster1AggroRange)
                {
                    AggroMonster1(monster);
                    monster.FacingRight = _kaiCenterX >= monster.X;

                    if (distanceToKai <= 6f)
                    {
                        // อยู่ตรงตำแหน่ง X เดียวกับผู้เล่น (แต่คนละชั้นความสูง) ไม่ต้องขยับไปมา
                        monster.State = Monster1State.Idle;
                        monster.FrameIndex = 0;
                    }
                    else
                    {
                        monster.State = Monster1State.Walking;
                        float direction = monster.FacingRight ? 1f : -1f;
                        float monsterVy = monster.VelocityY;
                        bool monsterAir = monster.Airborne;
                        monster.X = MoveMonsterHorizontally(monster.Room, monster.X, direction * Monster1WalkSpeed * deltaSeconds,
                            _monster1HalfWidth, _monster1Height, monster.YOffset, ref monsterVy, ref monsterAir);
                        monster.VelocityY = monsterVy;
                        monster.Airborne = monsterAir;
                        monster.X = MathHelper.Clamp(monster.X, _monster1HalfWidth, _worldWidth - _monster1HalfWidth);
                        monster.FrameIndex = (int)(monster.AnimTimer / Monster1FrameDuration) % Monster1WalkFrameCount;
                    }
                }
                else if (monster.IsAggro)
                {
                    float distanceToSpawn = System.Math.Abs(monster.SpawnX - monster.X);

                    if (distanceToSpawn <= Monster1ReturnThreshold)
                    {
                        monster.X = monster.SpawnX;
                        monster.IsAggro = false;
                        monster.State = Monster1State.Idle;
                        monster.FrameIndex = 0;
                    }
                    else
                    {
                        monster.State = Monster1State.Walking;
                        float direction = monster.SpawnX >= monster.X ? 1f : -1f;
                        monster.FacingRight = direction > 0f;
                        float monsterVy = monster.VelocityY;
                        bool monsterAir = monster.Airborne;
                        monster.X = MoveMonsterHorizontally(monster.Room, monster.X, direction * Monster1WalkSpeed * deltaSeconds,
                            _monster1HalfWidth, _monster1Height, monster.YOffset, ref monsterVy, ref monsterAir);
                        monster.VelocityY = monsterVy;
                        monster.Airborne = monsterAir;
                        monster.X = MathHelper.Clamp(monster.X, _monster1HalfWidth, _worldWidth - _monster1HalfWidth);
                        monster.FrameIndex = (int)(monster.AnimTimer / Monster1FrameDuration) % Monster1WalkFrameCount;
                    }
                }
                else
                {
                    // ยังไม่เคยตรวจพบผู้เล่น: ยืนเฉยที่จุดเกิด (ใช้เฟรมแรกของท่าเดินเป็นภาพ idle)
                    monster.State = Monster1State.Idle;
                    monster.FrameIndex = 0;
                }
            }
        }

        private void TryApplyMonster1AttackHit(Monster1 monster)
        {
            float halfWidth = GetCurrentHalfWidth();
            var kaiRect = new Rectangle(
                (int)(_kaiCenterX - halfWidth),
                (int)(_floorY + _kaiJumpOffsetY - _kaiTargetHeight),
                (int)(halfWidth * 2f),
                (int)_kaiTargetHeight);

            var monsterHitbox = new Rectangle(
                (int)(monster.FacingRight ? monster.X : monster.X - Monster1AttackReach),
                (int)(_floorY + monster.YOffset - _monster1Height),
                (int)Monster1AttackReach,
                (int)_monster1Height);

            if (!monsterHitbox.Intersects(kaiRect))
                return;

            if (IsParryActiveAgainst(monster.X))
            {
                monster.AttackHitApplied = true;
                OnParrySuccess();
                ApplyMonster1Hit(monster, _random.Next(ParryDamageMin, ParryDamageMax + 1));
                return;
            }

            monster.AttackHitApplied = true;
            int damage = _random.Next(Monster1DamageMin, Monster1DamageMax + 1);
            _currentHP = MathHelper.Max(0f, _currentHP - damage);
            PlayKaiHurtSound(damage);

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(_kaiCenterX + _random.Next(-15, 16), _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 8f),
                Color = new Color(255, 120, 120)
            });
        }

        private void ApplyMonster1Hit(Monster1 monster, int damageOverride = 0)
        {
            int baseDamage = RollMeleeBaseDamage();
            if (_seaweedBuffTimer > 0f)
                baseDamage += SeaweedAtkBonus;

            int damage = baseDamage;
            bool isCrit = damageOverride <= 0 && _random.NextDouble() < CritChance;
            if (isCrit)
                damage *= CritMultiplier;
            if (damageOverride > 0)
                damage = damageOverride;   // ดาเมจจากแพรี่ (ไม่คริ ไม่บวกบัฟ)

            monster.HP -= damage;
            monster.HitFlashTimer = Monster1HitFlashDuration;

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(monster.X + _random.Next(-20, 21), _floorY + monster.YOffset - _monster1Height - 8f),
                IsCrit = isCrit,
                Color = damageOverride > 0 ? new Color(120, 220, 255) : (isCrit ? new Color(255, 210, 80) : Color.White)
            });

            if (monster.HP <= 0f)
            {
                monster.HP = 0f;
                if (!monster.IsDying)
                    PlaySfx(_monsterDeathSoundEffect, MonsterDeathSoundVolume);
                monster.IsDying = true;
                monster.DeathTimer = 0f;
                monster.DeathDropDone = false;
            }
        }

        private AnimFrame GetMonster1Frame(Monster1 monster)
        {
            if (monster.IsDying)
            {
                int dieFrame = (int)(monster.DeathTimer / Monster1DieFrameDuration);
                return _monster1Die[System.Math.Min(dieFrame, Monster1DieFrameCount - 1)];
            }

            if (monster.State == Monster1State.Attacking)
                return _monster1Attack[System.Math.Min(monster.FrameIndex, Monster1AttackFrameCount - 1)];

            if (monster.State == Monster1State.Walking)
                return _monster1Walk[System.Math.Min(monster.FrameIndex, Monster1WalkFrameCount - 1)];

            // Idle: ยังไม่มีท่ายืนเฉยของตัวเอง ใช้เฟรมแรกของท่าเดินแทน
            return _monster1Walk[0];
        }

        private void DrawMonster1List()
        {
            foreach (var monster in _monster1List)
            {
                if (monster.Room != _currentRoomIndex)
                    continue;

                var frame = GetMonster1Frame(monster);

                // ใช้ OriginX ของเฟรม (จุดยึดตัวละครคงที่) แทนจุดกึ่งกลาง Trim ทั้งกล่อง
                // กันปัญหาเอฟเฟกต์ปาดเลือดในท่าโจมตีทำให้กล่อง Trim กว้าง/เบี้ยวจนดูเหมือนมีอีกเฟรมยื่นออกมา
                float originX = monster.FacingRight ? frame.OriginX : frame.Trim.Width - frame.OriginX;
                var origin = new Vector2(originX, frame.Trim.Height);
                var position = new Vector2(monster.X, _floorY + monster.YOffset);
                SpriteEffects effects = monster.FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                // ท่าโจมตีกับท่าเดิน/ยืนเฉย มาจากคนละชีท สเกลไม่เท่ากัน ต้องแยกใช้คนละค่า ไม่งั้นขนาดตัวจะไม่เท่ากัน
                float scale = monster.IsDying
                    ? _monster1DieScale
                    : (monster.State == Monster1State.Attacking) ? _monster1AttackScale : _monster1Scale;

                // เอฟเฟกต์กระพริบสีตอนโดนตี (ยังใช้กับการโดนตีครั้งสุดท้ายที่ทำให้ตายด้วย)
                Color tint = Color.White;
                if (monster.HitFlashTimer > 0f)
                {
                    float t = MathHelper.Clamp(monster.HitFlashTimer / Monster1HitFlashDuration, 0f, 1f);
                    tint = Color.Lerp(Color.White, new Color(255, 160, 160), t);
                }

                // ตอนตาย: ท่าตายจบแล้วค้างเฟรมสุดท้ายและค่อยๆ จางหาย
                if (monster.IsDying)
                {
                    float fadeT = (monster.DeathTimer - Monster1DieAnimDuration) / Monster1DieFadeOutDuration;
                    tint *= 1f - MathHelper.Clamp(fadeT, 0f, 1f);
                }

                _spriteBatch.Draw(frame.Texture, position, frame.Trim, tint, 0f, origin, scale, effects, 0f);

                if (monster.State == Monster1State.Attacking && !monster.IsDying)
                    DrawEnemyWindupBar(monster.X, _floorY + monster.YOffset - _monster1Height,
                        GetAttackWindupProgress(monster.AnimTimer, Monster1AttackFrameDuration, Monster1AttackHitFrameStart));

                if (_showHitboxes)
                    DrawSpriteBoundsDebug(position, origin, frame.Trim, scale);
            }
        }

        // =====================================================================
        // Boss 1
        // =====================================================================

        private void SpawnBoss1List(int count)
        {
            float direction = _facingRight ? 1f : -1f;
            float baseX = _kaiCenterX + direction * Boss1SpawnDistance;

            for (int i = 0; i < count; i++)
            {
                float x = MathHelper.Clamp(
                    baseX + direction * i * Boss1SpawnSpacing,
                    _boss1HalfWidth,
                    _worldWidth - _boss1HalfWidth);

                _boss1List.Add(new Boss1
                {
                    X = x,
                    SpawnX = x,
                    Room = _currentRoomIndex,
                    HP = Boss1MaxHP,
                    State = Boss1State.Idle,
                    FacingRight = !_facingRight,
                    YOffset = GetSurfaceYAt(_currentRoomIndex, x, _boss1HalfWidth) - _floorY
                });
            }
        }

        private float GetBoss1WalkSpeed(Boss1 boss)
        {
            return boss.IsPhase2 ? Boss1WalkSpeed * Boss1Phase2SpeedMultiplier : Boss1WalkSpeed;
        }

        private void MoveBoss1(Boss1 boss, float direction, float deltaSeconds)
        {
            float vy = boss.VelocityY;
            bool air = boss.Airborne;
            boss.X = MoveMonsterHorizontally(boss.Room, boss.X, direction * GetBoss1WalkSpeed(boss) * deltaSeconds,
                _boss1HalfWidth, _boss1Height, boss.YOffset, ref vy, ref air);
            boss.VelocityY = vy;
            boss.Airborne = air;
            boss.X = MathHelper.Clamp(boss.X, _boss1HalfWidth, _worldWidth - _boss1HalfWidth);
            boss.FrameIndex = (int)(boss.AnimTimer / Boss1FrameDuration) % Boss1FrameCount;
        }

        private void UpdateBoss1List(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = _boss1List.Count - 1; i >= 0; i--)
            {
                var boss = _boss1List[i];

                if (boss.Room != _currentRoomIndex)
                    continue;

                if (boss.HitFlashTimer > 0f)
                    boss.HitFlashTimer = MathHelper.Max(0f, boss.HitFlashTimer - deltaSeconds);

                if (boss.AttackCooldownTimer > 0f)
                    boss.AttackCooldownTimer = MathHelper.Max(0f, boss.AttackCooldownTimer - deltaSeconds);

                boss.AnimTimer += deltaSeconds;

                // phase 2 บินได้: ไม่ใช้แรงโน้มถ่วง (ความสูงคุมโดย UpdateBoss1Phase2Behavior) ยกเว้นตอนตายให้ร่วงลงพื้น
                if (!(boss.IsPhase2 && !boss.IsDying && !boss.IsStatue))
                {
                    float vy = boss.VelocityY;
                    bool air = boss.Airborne;
                    float yOffset = boss.YOffset;
                    UpdateMonsterVertical(boss.Room, boss.X, _boss1HalfWidth, ref yOffset, ref vy, ref air, deltaSeconds);
                    boss.YOffset = yOffset;
                    boss.VelocityY = vy;
                    boss.Airborne = air;
                }

                // รูปปั้น (ฉากบอสห้องลับ): ยืนนิ่งท่าเฟรมแรก ไม่ไล่/ไม่โจมตี จนกว่าฉากจะจบ
                if (boss.IsStatue)
                {
                    boss.State = Boss1State.Idle;
                    boss.FrameIndex = 0;
                    continue;
                }

                // ท่าตาย: เล่นครบ 8 เฟรม (ชีทค่อยๆ สลายเป็นผงในตัวอยู่แล้ว) -> ดรอปของ -> ลบออก
                if (boss.IsDying)
                {
                    boss.DeathTimer += deltaSeconds;

                    if (boss.DeathTimer >= Boss1DieAnimDuration)
                    {
                        if (!boss.DeathDropDone)
                        {
                            DropBoss1Loot(boss);
                            boss.DeathDropDone = true;

                            // บอสในฉากห้องบอสตาย (ท่าตายเล่นจบแล้ว) -> ประตูทางออกโผล่
                            if (boss == _bossSceneBoss)
                                _bossExitDoorVisible = true;
                        }
                        _boss1List.RemoveAt(i);
                    }
                    continue;
                }

                if (boss.State == Boss1State.Attacking)
                {
                    boss.FacingRight = _kaiCenterX >= boss.X;

                    float attackFrameDuration = boss.AttackIsPhase2 ? Boss1AttackFrameDurationP2 : Boss1AttackFrameDurationP1;
                    int frame = GetAttackFrame(boss.AnimTimer, attackFrameDuration, Boss1AttackHitFrameStart);

                    if (frame >= Boss1FrameCount)
                    {
                        boss.State = Boss1State.Idle;
                        boss.AnimTimer = 0f;
                        boss.FrameIndex = 0;
                        boss.AttackCooldownTimer = boss.AttackIsOrb
                            ? BossOrbCooldown
                            : (boss.AttackIsPhase2 ? Boss1AttackCooldownP2 : Boss1AttackCooldownP1);
                    }
                    else
                    {
                        boss.FrameIndex = frame;

                        if (!boss.AttackHitApplied && frame >= Boss1AttackHitFrameStart && frame <= Boss1AttackHitFrameEnd)
                        {
                            if (boss.AttackIsOrb)
                            {
                                boss.AttackHitApplied = true;
                                SpawnBossOrb(boss);
                            }
                            else
                            {
                                TryApplyBoss1AttackHit(boss);
                            }
                        }
                    }
                    continue;
                }

                // phase 2: พฤติกรรมของตัวเอง (บิน / ยิงลูกบอล / ลงมาฟาด)
                if (boss.IsPhase2)
                {
                    UpdateBoss1Phase2Behavior(boss, deltaSeconds, (float)gameTime.TotalGameTime.TotalSeconds);
                    continue;
                }

                float distanceToKai = System.Math.Abs(_kaiCenterX - boss.X);
                bool canReachKaiVertically = IsMonsterVerticallyOverlappingKai(boss.YOffset, _boss1Height);

                if (distanceToKai <= Boss1AttackRange && canReachKaiVertically)
                {
                    boss.IsAggro = true;
                    boss.FacingRight = _kaiCenterX >= boss.X;

                    if (boss.AttackCooldownTimer <= 0f)
                    {
                        boss.State = Boss1State.Attacking;
                        boss.AnimTimer = 0f;
                        boss.FrameIndex = 0;
                        boss.AttackHitApplied = false;
                        boss.AttackIsPhase2 = boss.IsPhase2;
                    }
                    else
                    {
                        boss.State = Boss1State.Idle;
                        boss.FrameIndex = (int)(boss.AnimTimer / Boss1FrameDuration) % Boss1FrameCount;
                    }
                }
                else if (distanceToKai <= Boss1AggroRange)
                {
                    boss.IsAggro = true;
                    boss.FacingRight = _kaiCenterX >= boss.X;

                    if (distanceToKai <= 6f)
                    {
                        boss.State = Boss1State.Idle;
                        boss.FrameIndex = (int)(boss.AnimTimer / Boss1FrameDuration) % Boss1FrameCount;
                    }
                    else
                    {
                        boss.State = Boss1State.Walking;
                        MoveBoss1(boss, boss.FacingRight ? 1f : -1f, deltaSeconds);
                    }
                }
                else if (boss.IsAggro)
                {
                    float distanceToSpawn = System.Math.Abs(boss.SpawnX - boss.X);

                    if (distanceToSpawn <= Boss1ReturnThreshold)
                    {
                        boss.X = boss.SpawnX;
                        boss.IsAggro = false;
                        boss.State = Boss1State.Idle;
                        boss.FrameIndex = (int)(boss.AnimTimer / Boss1FrameDuration) % Boss1FrameCount;
                    }
                    else
                    {
                        boss.State = Boss1State.Walking;
                        float direction = boss.SpawnX >= boss.X ? 1f : -1f;
                        boss.FacingRight = direction > 0f;
                        MoveBoss1(boss, direction, deltaSeconds);
                    }
                }
                else
                {
                    boss.State = Boss1State.Idle;
                    boss.FrameIndex = (int)(boss.AnimTimer / Boss1FrameDuration) % Boss1FrameCount;
                }
            }
        }

        private static float StepToward(float current, float target, float maxDelta)
        {
            if (System.Math.Abs(target - current) <= maxDelta)
                return target;
            return current + (target > current ? maxDelta : -maxDelta);
        }

        // Boss phase 2 (ไม่ได้อยู่ในท่าโจมตี):
        //  - Kai ห่าง  -> ลอยสูง ยืนห่างๆ ยิงลูกบอลพุ่งหา Kai เป็นระยะ
        //  - Kai เข้าใกล้ -> ลงมาที่พื้น เดินเข้าหาแล้วฟาดระยะใกล้ (ท่าเดิม) พอ Kai ห่างพอก็ลอยขึ้นอีก
        private void UpdateBoss1Phase2Behavior(Boss1 boss, float dt, float time)
        {
            boss.IsAggro = true;
            boss.FacingRight = _kaiCenterX >= boss.X;
            float distance = System.Math.Abs(_kaiCenterX - boss.X);

            float hover = _kaiTargetHeight * Boss1HoverHeightKaiRatio;
            bool isGrounded = boss.YOffset > -hover * 0.5f;
            bool wantsMelee = distance <= Boss1AttackRange + (isGrounded ? Boss1LandingExtraRange : 0f);

            float targetOffset = wantsMelee
                ? 0f
                : -hover + (float)System.Math.Sin(time * Boss1HoverBobSpeed) * Boss1HoverBobAmplitude;
            boss.YOffset = StepToward(boss.YOffset, targetOffset, Boss1FlightSpeed * dt);
            boss.VelocityY = 0f;
            boss.Airborne = false;

            bool isHigh = boss.YOffset <= -hover * 0.8f;
            boss.FrameIndex = (int)(boss.AnimTimer / Boss1FrameDuration) % Boss1FrameCount;

            if (wantsMelee)
            {
                bool isLow = IsMonsterVerticallyOverlappingKai(boss.YOffset, _boss1Height);

                if (distance <= Boss1AttackRange && isLow && boss.AttackCooldownTimer <= 0f)
                {
                    boss.State = Boss1State.Attacking;
                    boss.AnimTimer = 0f;
                    boss.FrameIndex = 0;
                    boss.AttackHitApplied = false;
                    boss.AttackIsPhase2 = true;
                    boss.AttackIsOrb = false;
                }
                else if (distance > Boss1AttackRange * 0.8f && distance > 6f)
                {
                    boss.State = Boss1State.Walking;
                    MoveBoss1Phase2(boss, boss.FacingRight ? 1f : -1f, dt);
                }
                else
                {
                    boss.State = Boss1State.Idle;
                }
                return;
            }

            // Kai อยู่ไกล: เดินเข้าไปถึงระยะยิง (ไม่ชิดเกินไป) แล้วยิงลูกบอลเมื่อลอยสูงพอ
            if (distance > BossOrbPreferredDistance)
            {
                boss.State = Boss1State.Walking;
                MoveBoss1Phase2(boss, boss.FacingRight ? 1f : -1f, dt);
            }
            else
            {
                boss.State = Boss1State.Idle;
            }

            if (isHigh && boss.AttackCooldownTimer <= 0f && distance <= BossOrbMaxShootDistance)
            {
                boss.State = Boss1State.Attacking;
                boss.AnimTimer = 0f;
                boss.FrameIndex = 0;
                boss.AttackHitApplied = false;
                boss.AttackIsPhase2 = true;
                boss.AttackIsOrb = true;
            }
        }

        // เดินแนวนอนของบอส phase 2: อยู่บนพื้นใช้ระบบเดิม (ชนแผ่นพื้น) / ลอยอยู่ขยับ X ตรงๆ
        private void MoveBoss1Phase2(Boss1 boss, float direction, float dt)
        {
            if (boss.YOffset > -1f)
            {
                MoveBoss1(boss, direction, dt);
                return;
            }

            boss.X += direction * GetBoss1WalkSpeed(boss) * dt;
            boss.X = MathHelper.Clamp(boss.X, _boss1HalfWidth, _worldWidth - _boss1HalfWidth);
        }

        private void SpawnBossOrb(Boss1 boss)
        {
            float dirX = boss.FacingRight ? 1f : -1f;
            float startX = boss.X + dirX * _boss1HalfWidth * 0.5f;
            float startY = _floorY + boss.YOffset - _boss1Height * 0.62f;

            // ยิงตรงไปที่ตำแหน่ง Kai ณ ตอนยิง (ไม่ตามตัว ถ้า Kai ขยับออกก็หลบได้)
            float targetX = _kaiCenterX;
            float targetY = _floorY + _kaiJumpOffsetY - _kaiTargetHeight * 0.5f;
            var direction = new Vector2(targetX - startX, targetY - startY);
            if (direction.LengthSquared() < 1f)
                direction = new Vector2(dirX, 0f);
            direction.Normalize();

            _bossOrbs.Add(new BossOrb
            {
                X = startX,
                Y = startY,
                VX = direction.X * BossOrbSpeed,
                VY = direction.Y * BossOrbSpeed,
                Room = boss.Room
            });

            PlaySfx(_bossOrbSoundEffect, BossOrbSoundVolume);   // บอส 1 ยิงลูกบอล
        }

        private static bool CircleIntersectsRect(float cx, float cy, float radius, Rectangle rect)
        {
            float nearestX = MathHelper.Clamp(cx, rect.Left, rect.Right);
            float nearestY = MathHelper.Clamp(cy, rect.Top, rect.Bottom);
            float dx = cx - nearestX;
            float dy = cy - nearestY;
            return dx * dx + dy * dy <= radius * radius;
        }

        private void UpdateBossOrbs(GameTime gameTime)
        {
            if (_bossOrbs.Count == 0)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float halfWidth = GetCurrentHalfWidth();
            var kaiRect = new Rectangle(
                (int)(_kaiCenterX - halfWidth),
                (int)(_floorY + _kaiJumpOffsetY - _kaiTargetHeight),
                (int)(halfWidth * 2f),
                (int)_kaiTargetHeight);

            for (int i = _bossOrbs.Count - 1; i >= 0; i--)
            {
                var orb = _bossOrbs[i];

                if (orb.Room != _currentRoomIndex)
                {
                    _bossOrbs.RemoveAt(i);
                    continue;
                }

                orb.Age += dt;
                orb.X += orb.VX * dt;
                orb.Y += orb.VY * dt;

                if (orb.Age > BossOrbLifetime || orb.X < -300f || orb.X > _worldWidth + 300f || orb.Y < _floorY - 3000f || orb.Y > _floorY + 400f)
                {
                    _bossOrbs.RemoveAt(i);
                    continue;
                }

                if (orb.Reflected)
                {
                    // ลูกบอลที่ถูกสะท้อนกลับ: ทำดาเมจบอสเมื่อโดนตัว
                    Boss1 hitBoss = null;
                    foreach (var boss in _boss1List)
                    {
                        if (boss.Room != _currentRoomIndex || boss.IsDying || boss.IsStatue)
                            continue;

                        var bossRect = new Rectangle(
                            (int)(boss.X - _boss1HalfWidth),
                            (int)(_floorY + boss.YOffset - _boss1Height),
                            (int)(_boss1HalfWidth * 2f),
                            (int)_boss1Height);

                        if (CircleIntersectsRect(orb.X, orb.Y, BossOrbRadius, bossRect))
                        {
                            hitBoss = boss;
                            break;
                        }
                    }

                    if (hitBoss != null)
                    {
                        ApplyBoss1Hit(hitBoss, _random.Next(BossOrbReflectDamageMin, BossOrbReflectDamageMax + 1));
                        _bossOrbs.RemoveAt(i);
                    }
                    continue;
                }

                if (_currentHP <= 0f || !CircleIntersectsRect(orb.X, orb.Y, BossOrbRadius, kaiRect))
                    continue;

                if (IsParryActiveAgainst(orb.X))
                {
                    // แพรี่สำเร็จ: สะท้อนลูกบอลกลับไปหาบอส (ไม่เสียเลือด)
                    OnParrySuccess();
                    orb.Reflected = true;
                    orb.Age = 0f;
                    PlaySfx(_bossOrbSoundEffect, BossOrbSoundVolume);   // สะท้อนกลับ: ใช้เสียงลูกบอลเดียวกัน

                    float aimX = orb.X - orb.VX;   // ไม่มีบอสให้เล็ง = พุ่งย้อนกลับทางเดิม
                    float aimY = orb.Y - orb.VY;
                    foreach (var boss in _boss1List)
                    {
                        if (boss.Room != _currentRoomIndex || boss.IsDying || boss.IsStatue)
                            continue;
                        aimX = boss.X;
                        aimY = _floorY + boss.YOffset - _boss1Height * 0.55f;
                        break;
                    }

                    var back = new Vector2(aimX - orb.X, aimY - orb.Y);
                    if (back.LengthSquared() < 1f)
                        back = new Vector2(-orb.VX, -orb.VY);
                    back.Normalize();
                    orb.VX = back.X * BossOrbReflectSpeed;
                    orb.VY = back.Y * BossOrbReflectSpeed;
                    continue;
                }

                int damage = _random.Next(BossOrbDamageMin, BossOrbDamageMax + 1);
                _currentHP = MathHelper.Max(0f, _currentHP - damage);
                PlayKaiHurtSound(damage);

                _damagePopups.Add(new DamagePopup
                {
                    Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Position = new Vector2(_kaiCenterX + _random.Next(-15, 16), _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 8f),
                    Color = new Color(255, 120, 120)
                });
                _bossOrbs.RemoveAt(i);
            }
        }

        // หลอดเลือดบอสกลางจอด้านบน พร้อมชื่อ และเส้นแบ่งจุดเข้า phase 2 (Boss1Phase2HpRatio)
        private void DrawBossHealthBar()
        {
            if (_deathState == PlayerDeathState.GameOver)
                return;

            Boss1 boss = null;
            foreach (var candidate in _boss1List)
            {
                if (candidate.Room == _currentRoomIndex && !candidate.IsDying && !candidate.IsStatue && candidate.HP > 0f)
                {
                    boss = candidate;
                    break;
                }
            }

            if (boss == null)
            {
                DrawBoss2HealthBar();   // ไม่มี Boss 1 ในห้อง -> แสดงหลอดเลือด Boss 2 แทน (ถ้ามี)
                return;
            }

            float ratio = MathHelper.Clamp(boss.HP / Boss1MaxHP, 0f, 1f);

            Vector2 nameSize = _hotbarFont.MeasureString(Boss1DisplayName) * BossHpBarNameScale;
            var namePosition = new Vector2((WindowedWidth - nameSize.X) / 2f, BossHpBarTopMargin);
            _spriteBatch.DrawString(_hotbarFont, Boss1DisplayName, namePosition + new Vector2(1f, 1f), Color.Black, 0f, Vector2.Zero, BossHpBarNameScale, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_hotbarFont, Boss1DisplayName, namePosition, Color.White, 0f, Vector2.Zero, BossHpBarNameScale, SpriteEffects.None, 0f);

            int x = (WindowedWidth - BossHpBarWidth) / 2;
            int y = BossHpBarTopMargin + (int)nameSize.Y + 4;
            var back = new Rectangle(x, y, BossHpBarWidth, BossHpBarHeight);
            DrawFilledRect(back, new Color(30, 8, 10, 230));

            int innerWidth = BossHpBarWidth - 4;
            int fillWidth = (int)(innerWidth * ratio);
            if (fillWidth > 0)
            {
                // โดนตีแว้บหนึ่งให้หลอดสว่างขึ้น / เฟส 2 เปลี่ยนเป็นสีส้มแดง
                Color fill = boss.HitFlashTimer > 0f
                    ? new Color(255, 140, 130)
                    : (ratio < Boss1Phase2HpRatio ? new Color(235, 85, 40) : new Color(200, 35, 45));
                DrawFilledRect(new Rectangle(x + 2, y + 2, fillWidth, BossHpBarHeight - 4), fill);
            }

            int markerX = x + 2 + (int)(innerWidth * Boss1Phase2HpRatio);
            DrawFilledRect(new Rectangle(markerX - 1, y, 2, BossHpBarHeight), new Color(255, 255, 255, 220));

            DrawRectOutline(back, 2, new Color(210, 185, 185));
        }

        private void DrawBossOrbs()
        {
            foreach (var orb in _bossOrbs)
            {
                if (orb.Room != _currentRoomIndex)
                    continue;

                var position = new Vector2(orb.X, orb.Y);

                if (_bossOrbTexture != null)
                {
                    float scale = BossOrbDrawSize / _bossOrbTexture.Width;
                    var origin = new Vector2(_bossOrbTexture.Width / 2f, _bossOrbTexture.Height / 2f);
                    float spin = orb.Age * BossOrbSpinSpeed * (orb.Reflected ? -1f : 1f);
                    _spriteBatch.Draw(_bossOrbTexture, position, null, Color.White, spin, origin, scale, SpriteEffects.None, 0f);
                }
                else
                {
                    // ไม่มี power_boss.png ใน Content: วาดสี่เหลี่ยมแดงแทน
                    var rect = new Rectangle((int)(orb.X - BossOrbRadius), (int)(orb.Y - BossOrbRadius), (int)(BossOrbRadius * 2f), (int)(BossOrbRadius * 2f));
                    DrawFilledRect(rect, new Color(150, 20, 30));
                    DrawRectOutline(rect, 2, new Color(255, 120, 120));
                }
            }
        }

        private void TryApplyBoss1AttackHit(Boss1 boss)
        {
            float halfWidth = GetCurrentHalfWidth();
            var kaiRect = new Rectangle(
                (int)(_kaiCenterX - halfWidth),
                (int)(_floorY + _kaiJumpOffsetY - _kaiTargetHeight),
                (int)(halfWidth * 2f),
                (int)_kaiTargetHeight);

            float reach = boss.AttackIsPhase2 ? Boss1AttackReachP2 : Boss1AttackReachP1;
            var bossHitbox = new Rectangle(
                (int)(boss.FacingRight ? boss.X : boss.X - reach),
                (int)(_floorY + boss.YOffset - _boss1Height),
                (int)reach,
                (int)_boss1Height);

            if (!bossHitbox.Intersects(kaiRect))
                return;

            if (IsParryActiveAgainst(boss.X))
            {
                boss.AttackHitApplied = true;
                OnParrySuccess();
                ApplyBoss1Hit(boss, _random.Next(ParryDamageMin, ParryDamageMax + 1));
                return;
            }

            boss.AttackHitApplied = true;
            int damage = boss.AttackIsPhase2
                ? _random.Next(Boss1DamageMinP2, Boss1DamageMaxP2 + 1)
                : _random.Next(Boss1DamageMinP1, Boss1DamageMaxP1 + 1);
            _currentHP = MathHelper.Max(0f, _currentHP - damage);
            PlayKaiHurtSound(damage);

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(_kaiCenterX + _random.Next(-15, 16), _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 8f),
                Color = new Color(255, 120, 120)
            });
        }

        private void ApplyBoss1Hit(Boss1 boss, int damageOverride = 0)
        {
            int baseDamage = RollMeleeBaseDamage();
            if (_seaweedBuffTimer > 0f)
                baseDamage += SeaweedAtkBonus;

            int damage = baseDamage;
            bool isCrit = damageOverride <= 0 && _random.NextDouble() < CritChance;
            if (isCrit)
                damage *= CritMultiplier;
            if (damageOverride > 0)
                damage = damageOverride;   // ดาเมจจากแพรี่ (ไม่คริ ไม่บวกบัฟ)

            boss.HP -= damage;
            boss.HitFlashTimer = Boss1HitFlashDuration;

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(boss.X + _random.Next(-30, 31), _floorY + boss.YOffset - _boss1Height - 8f),
                IsCrit = isCrit,
                Color = damageOverride > 0 ? new Color(120, 220, 255) : (isCrit ? new Color(255, 210, 80) : Color.White)
            });

            if (boss.HP <= 0f)
            {
                boss.HP = 0f;
                if (!boss.IsDying)
                    PlaySfx(_monsterDeathSoundEffect, MonsterDeathSoundVolume);
                boss.IsDying = true;
                boss.DeathTimer = 0f;
                boss.DeathDropDone = false;
                return;
            }

            // HP ต่ำกว่า 40% -> phase 2 (ท่าโจมตีที่กำลังเล่นอยู่จะจบด้วยชีทเดิม ท่าถัดไปถึงเปลี่ยน)
            if (!boss.IsPhase2 && boss.HP < Boss1MaxHP * Boss1Phase2HpRatio)
                boss.IsPhase2 = true;
        }

        private void DropBoss1Loot(Boss1 boss)
        {
            float keyX = MathHelper.Clamp(boss.X - Boss1LootSpread, 0f, _worldWidth);
            float potionX = MathHelper.Clamp(boss.X + Boss1LootSpread, 0f, _worldWidth);

            _worldItems.Add(new WorldItem { Type = ItemType.Key, Count = Boss1KeyDropCount, X = keyX, Room = boss.Room });

            int potionCount = _random.Next(Boss1PotionDropMin, Boss1PotionDropMax + 1);
            _worldItems.Add(new WorldItem { Type = ItemType.HpPotion, Count = potionCount, X = potionX, Room = boss.Room });

            // น้ำยากระโดด: บอสดรอปแน่นอน 1 ชิ้น (วางกลางระหว่างกุญแจกับยา HP ที่ตำแหน่งบอส)
            float jumpPotionX = MathHelper.Clamp(boss.X, 0f, _worldWidth);
            _worldItems.Add(new WorldItem { Type = ItemType.JumpPotion, Count = JumpPotionDropCount, X = jumpPotionX, Room = boss.Room });
        }

        private AnimFrame GetBoss1Frame(Boss1 boss)
        {
            if (boss.IsDying)
            {
                int dieFrame = (int)(boss.DeathTimer / Boss1DieFrameDuration);
                return _boss1Die[System.Math.Min(dieFrame, Boss1FrameCount - 1)];
            }

            int index = System.Math.Min(boss.FrameIndex, Boss1FrameCount - 1);

            if (boss.State == Boss1State.Attacking)
                return (boss.AttackIsPhase2 ? _boss1AttackP2 : _boss1AttackP1)[index];

            // Idle ใช้ท่าเดิน/ลอยวนต่อเนื่อง (ตัวลอย ไม่ควรนิ่งสนิท)
            return _boss1Walk[index];
        }

        private void DrawBoss1List()
        {
            foreach (var boss in _boss1List)
            {
                if (boss.Room != _currentRoomIndex)
                    continue;

                var frame = GetBoss1Frame(boss);

                // X: จุดยึดคงที่ของลำตัว (พลิกซ้าย/ขวาตามทิศ)  Y: ใช้แนวพื้นร่วมกันทุกเฟรม กันตัวเด้งตามความสูงของหนวด
                float originX = boss.FacingRight ? frame.OriginX : frame.Trim.Width - frame.OriginX;
                float originY = _boss1FeetLineY - frame.Trim.Y;
                var origin = new Vector2(originX, originY);
                float shakeX = (boss == _bossSceneBoss && _isBossScene && _bossShakeActive)
                    ? (float)System.Math.Sin(_bossShakeTime * BossSceneShakeFrequency) * BossSceneShakeAmplitude
                    : 0f;
                var position = new Vector2(boss.X + shakeX, _floorY + boss.YOffset);
                SpriteEffects effects = boss.FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                Color tint = Color.White;
                if (boss.HitFlashTimer > 0f)
                {
                    float t = MathHelper.Clamp(boss.HitFlashTimer / Boss1HitFlashDuration, 0f, 1f);
                    tint = Color.Lerp(Color.White, new Color(255, 160, 160), t);
                }

                _spriteBatch.Draw(frame.Texture, position, frame.Trim, tint, 0f, origin, _boss1Scale, effects, 0f);

                if (boss.State == Boss1State.Attacking && !boss.IsDying && !boss.IsStatue)
                {
                    float attackFrameDuration = boss.AttackIsPhase2 ? Boss1AttackFrameDurationP2 : Boss1AttackFrameDurationP1;
                    DrawEnemyWindupBar(boss.X, _floorY + boss.YOffset - _boss1Height,
                        GetAttackWindupProgress(boss.AnimTimer, attackFrameDuration, Boss1AttackHitFrameStart));
                }

                if (_showHitboxes)
                    DrawSpriteBoundsDebug(position, origin, frame.Trim, _boss1Scale);
            }
        }

        // =====================================================================
        // Boss 2
        // =====================================================================

        private void LoadBoss3Content()
        {
            try
            {
                _boss3Idle = LoadBoss3Frames(Boss3IdleAsset);
                _boss3Scale = (_kaiTargetHeight * Boss3HeightToKaiRatio) / _boss3Idle[0].Trim.Height;
                _boss3Height = _boss3Idle[0].Trim.Height * _boss3Scale;
                _boss3HalfWidth = Boss3HitboxHalfWidthCellRatio * (_boss3Idle[0].Texture.Width / Boss3SheetColumns) * _boss3Scale;
                _boss3Loaded = true;
            }
            catch (System.Exception)
            {
                _boss3Loaded = false;
                return;
            }

            // ชีทอื่นยังไม่ใช้ในตอนนี้ (เฟสโจมตี/ตายมาทีหลัง) โหลดได้ก็เก็บไว้ ไม่ได้ก็ข้าม
            try { _boss3AttackP1 = LoadBoss3Frames(Boss3AttackP1Asset); } catch (System.Exception) { _boss3AttackP1 = null; }
            try { _boss3AttackP2 = LoadBoss3Frames(Boss3AttackP2Asset); } catch (System.Exception) { _boss3AttackP2 = null; }
            try { _boss3Die = LoadBoss3Frames(Boss3DieAsset); } catch (System.Exception) { _boss3Die = null; }

            try
            {
                _boss3GhostTexture = Content.Load<Texture2D>(Boss3GhostAsset);
                _boss3GhostTrim = GetOpaqueBounds(_boss3GhostTexture, new Rectangle(0, 0, _boss3GhostTexture.Width, _boss3GhostTexture.Height));
                _boss3GhostScale = (_kaiTargetHeight * Boss3GhostHeightToKaiRatio) / _boss3GhostTrim.Height;
            }
            catch (System.Exception)
            {
                _boss3GhostTexture = null;
            }
        }

        // ชีท 4x2: ยึดกึ่งกลางเซลล์เป็นแกน X (ตัวลอย ขยับในเฟรม) และก้นเซลล์เป็นเส้นเท้า ใช้ร่วมกันทุกเฟรมกันตัวเด้ง
        private AnimFrame[] LoadBoss3Frames(string sheetAssetName)
        {
            var sheet = Content.Load<Texture2D>(sheetAssetName);
            int cellWidth = sheet.Width / Boss3SheetColumns;
            int cellHeight = sheet.Height / Boss3SheetRows;

            var frames = new AnimFrame[Boss3FrameCount];
            for (int i = 0; i < Boss3FrameCount; i++)
            {
                int col = i % Boss3SheetColumns;
                int row = i / Boss3SheetColumns;
                var cellRect = new Rectangle(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                var trim = GetOpaqueBounds(sheet, cellRect);
                frames[i] = new AnimFrame
                {
                    Texture = sheet,
                    Trim = trim,
                    OriginX = cellRect.X + cellWidth / 2f - trim.X,
                    ClampHalfWidth = trim.Width / 2f
                };
            }
            return frames;
        }

        private void SpawnBoss3AtEdge()
        {
            _boss3List.Clear();
            _boss3SceneBoss = null;
            if (!_boss3Loaded || _boss3Defeated)
                return;

            float x = _worldWidth - Boss3EdgeOffsetFromRight;
            _boss3SceneBoss = new Boss3
            {
                X = x,
                Room = Level3RoomIndex,
                HP = Boss3MaxHP,
                FacingRight = false,
                YOffset = 0f,
                IsStatue = true,
                IsSceneBoss = true
            };
            _boss3List.Add(_boss3SceneBoss);
        }

        // บอสที่เสกด้วยคอนโซล: ตื่นอยู่แล้ว สู้ได้ทันที (ไม่มีฉากผี)
        private void SpawnBoss3List(int count)
        {
            float direction = _facingRight ? 1f : -1f;
            float baseX = _kaiCenterX + direction * Boss3SpawnDistance;

            for (int i = 0; i < count; i++)
            {
                float x = MathHelper.Clamp(baseX + direction * i * Boss3SpawnSpacing, _boss3HalfWidth, _worldWidth - _boss3HalfWidth);
                _boss3List.Add(new Boss3
                {
                    X = x,
                    Room = _currentRoomIndex,
                    HP = Boss3MaxHP,
                    FacingRight = !_facingRight,
                    YOffset = GetSurfaceYAt(_currentRoomIndex, x, _boss3HalfWidth) - _floorY,
                    State = Boss3State.Moving,
                    AttackCooldownTimer = 1f
                });
            }
        }

        private float GetBoss3WalkSpeed(Boss3 boss)
        {
            return boss.IsPhase2 ? Boss3MoveSpeed * Boss3Phase2SpeedMultiplier : Boss3MoveSpeed;
        }

        private float GetBoss3SwipeReach()
        {
            int cellWidth = _boss3Idle[0].Texture.Width / Boss3SheetColumns;
            return Boss3SwipeReachCellRatio * cellWidth * _boss3Scale;
        }

        private bool IsBoss3Awake(Boss3 boss)
        {
            return boss != null && !boss.IsStatue && !boss.IsDying && _boss3List.Contains(boss);
        }

        // ต่อสู้อยู่ (มีบอส 3 ตื่นและยังไม่ตายในห้องนี้): ใช้คุมซูมกล้อง
        private bool IsBoss3FightActive()
        {
            foreach (var boss in _boss3List)
            {
                if (boss.Room == _currentRoomIndex && !boss.IsStatue && !boss.IsDying)
                    return true;
            }
            return false;
        }

        private void UpdateBoss3List(GameTime gameTime)
        {
            if (!_boss3Loaded || _boss3List.Count == 0)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            int dieFrames = _boss3Die != null ? Boss3FrameCount : 0;

            for (int i = _boss3List.Count - 1; i >= 0; i--)
            {
                var boss = _boss3List[i];
                if (boss.Room != _currentRoomIndex)
                    continue;

                if (boss.HitFlashTimer > 0f)
                    boss.HitFlashTimer = MathHelper.Max(0f, boss.HitFlashTimer - dt);
                if (boss.AttackCooldownTimer > 0f)
                    boss.AttackCooldownTimer = MathHelper.Max(0f, boss.AttackCooldownTimer - dt);

                boss.AnimTimer += dt;

                if (boss.IsDying)
                {
                    boss.DeathTimer += dt;
                    if (boss.DeathTimer >= dieFrames * Boss3DieFrameDuration)
                        _boss3List.RemoveAt(i);
                    continue;
                }

                if (boss.IsStatue)
                {
                    boss.State = Boss3State.Idle;
                    boss.FrameIndex = (int)(boss.AnimTimer / Boss3FrameDuration) % Boss3FrameCount;
                    continue;
                }

                if (boss.State == Boss3State.Attacking)
                {
                    UpdateBoss3Attacking(boss);
                    continue;
                }

                boss.FacingRight = _kaiCenterX >= boss.X;
                float distance = System.Math.Abs(_kaiCenterX - boss.X);
                float swipeReach = GetBoss3SwipeReach();
                bool canReachKaiVertically = IsMonsterVerticallyOverlappingKai(boss.YOffset, _boss3Height);
                bool hasAttackSheet = boss.IsPhase2 ? _boss3AttackP2 != null : _boss3AttackP1 != null;
                float attackRange = boss.IsPhase2 ? Boss3LaserRange : swipeReach * 0.9f;

                if (hasAttackSheet && boss.AttackCooldownTimer <= 0f && distance <= attackRange
                    && (boss.IsPhase2 || canReachKaiVertically))
                {
                    boss.State = Boss3State.Attacking;
                    boss.AnimTimer = 0f;
                    boss.FrameIndex = 0;
                    boss.AttackHitApplied = false;
                    boss.AttackIsLaser = boss.IsPhase2;
                    PlaySwingSound();
                    continue;
                }

                float stopDistance = boss.IsPhase2 ? Boss3LaserRange * 0.6f : swipeReach * 0.75f;
                if (distance > stopDistance)
                {
                    boss.State = Boss3State.Moving;
                    float dir = boss.FacingRight ? 1f : -1f;
                    boss.X = MathHelper.Clamp(boss.X + dir * GetBoss3WalkSpeed(boss) * dt, _boss3HalfWidth, _worldWidth - _boss3HalfWidth);
                }
                else
                {
                    boss.State = Boss3State.Idle;
                }

                boss.FrameIndex = (int)(boss.AnimTimer / Boss3FrameDuration) % Boss3FrameCount;
            }
        }

        private void UpdateBoss3Attacking(Boss3 boss)
        {
            int hitStart = boss.AttackIsLaser ? Boss3LaserHitFrameStart : Boss3SwipeHitFrameStart;
            int hitEnd = boss.AttackIsLaser ? Boss3LaserHitFrameEnd : Boss3SwipeHitFrameEnd;

            if (!boss.AttackHitApplied && GetAttackFrame(boss.AnimTimer, Boss3AttackFrameDuration, hitStart) < hitStart)
                boss.FacingRight = _kaiCenterX >= boss.X;   // ตอนเงื้อยังหันตามผู้เล่น

            int frame = GetAttackFrame(boss.AnimTimer, Boss3AttackFrameDuration, hitStart);

            if (frame >= Boss3FrameCount)
            {
                boss.State = Boss3State.Idle;
                boss.AnimTimer = 0f;
                boss.FrameIndex = 0;
                boss.AttackCooldownTimer = boss.AttackIsLaser ? Boss3LaserCooldown : Boss3SwipeCooldown;
                return;
            }

            boss.FrameIndex = frame;

            if (boss.AttackIsLaser && frame >= hitStart && boss.LaserStart == Vector2.Zero)
            {
                // ล็อกทิศเลเซอร์ไปที่กลางตัว Kai ตอนเริ่มเฟรมยิง
                float dirX = boss.FacingRight ? 1f : -1f;
                int cellWidth = _boss3Idle[0].Texture.Width / Boss3SheetColumns;
                boss.LaserStart = new Vector2(
                    boss.X + dirX * 0.05f * cellWidth * _boss3Scale,
                    _floorY + boss.YOffset - _boss3Height * (1f - Boss3LaserEyeHeightRatio));
                var target = new Vector2(_kaiCenterX, _floorY + _kaiJumpOffsetY - _kaiTargetHeight * 0.5f);
                Vector2 toTarget = target - boss.LaserStart;
                if (toTarget.LengthSquared() < 1f)
                    toTarget = new Vector2(dirX, 0f);
                toTarget.Normalize();
                boss.LaserEnd = boss.LaserStart + toTarget * Boss3LaserRange;
            }

            if (!boss.AttackHitApplied && frame >= hitStart && frame <= hitEnd)
                TryApplyBoss3AttackHit(boss);

            if (frame > hitEnd)
            {
                boss.LaserStart = Vector2.Zero;   // รีเซ็ตให้ยิงครั้งหน้าล็อกใหม่
                boss.LaserEnd = Vector2.Zero;
            }
        }

        private Rectangle GetBoss3Rect(Boss3 boss)
        {
            return new Rectangle(
                (int)(boss.X - _boss3HalfWidth),
                (int)(_floorY + boss.YOffset - _boss3Height),
                (int)(_boss3HalfWidth * 2f),
                (int)_boss3Height);
        }

        private void TryApplyBoss3AttackHit(Boss3 boss)
        {
            float halfWidth = GetCurrentHalfWidth();
            var kaiRect = new Rectangle(
                (int)(_kaiCenterX - halfWidth),
                (int)(_floorY + _kaiJumpOffsetY - _kaiTargetHeight),
                (int)(halfWidth * 2f),
                (int)_kaiTargetHeight);

            float reach = GetBoss3SwipeReach();
            var swipeBox = new Rectangle(
                (int)(boss.FacingRight ? boss.X : boss.X - reach),
                (int)(_floorY + boss.YOffset - _boss3Height),
                (int)reach,
                (int)_boss3Height);

            bool hit = swipeBox.Intersects(kaiRect);

            // เฟส 2 ระยะไกล: เลเซอร์ (เช็คเป็นเส้นจากตาไปจุดล็อก)
            if (!hit && boss.AttackIsLaser && boss.LaserStart != Vector2.Zero)
            {
                Vector2 delta = boss.LaserEnd - boss.LaserStart;
                int steps = System.Math.Max(1, (int)(delta.Length() / 8f));
                var inflated = kaiRect;
                inflated.Inflate((int)(Boss3LaserThickness / 2f), (int)(Boss3LaserThickness / 2f));
                for (int s = 0; s <= steps && !hit; s++)
                {
                    Vector2 p = boss.LaserStart + delta * (s / (float)steps);
                    if (inflated.Contains((int)p.X, (int)p.Y))
                        hit = true;
                }
            }

            if (!hit)
                return;

            boss.AttackHitApplied = true;

            if (IsParryActiveAgainst(boss.X))
            {
                OnParrySuccess();
                ApplyBoss3Hit(boss, _random.Next(ParryDamageMin, ParryDamageMax + 1));
                return;
            }

            int damage = _random.Next(Boss3DamageMin, Boss3DamageMax + 1);
            _currentHP = MathHelper.Max(0f, _currentHP - damage);
            PlayKaiHurtSound(damage);

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(_kaiCenterX + _random.Next(-15, 16), _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 8f),
                Color = new Color(255, 120, 120)
            });
        }

        private void ApplyBoss3Hit(Boss3 boss, int damageOverride = 0)
        {
            if (boss.IsDying || boss.IsStatue)
                return;

            int baseDamage = RollMeleeBaseDamage();
            if (_seaweedBuffTimer > 0f)
                baseDamage += SeaweedAtkBonus;

            int damage = baseDamage;
            bool isCrit = damageOverride <= 0 && _random.NextDouble() < CritChance;
            if (isCrit)
                damage *= CritMultiplier;
            if (damageOverride > 0)
                damage = damageOverride;

            boss.HP -= damage;
            boss.HitFlashTimer = Boss3HitFlashDuration;

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(boss.X + _random.Next(-30, 31), _floorY + boss.YOffset - _boss3Height - 8f),
                IsCrit = isCrit,
                Color = damageOverride > 0 ? new Color(120, 220, 255) : (isCrit ? new Color(255, 210, 80) : Color.White)
            });

            if (boss.HP <= 0f)
            {
                boss.HP = 0f;
                PlaySfx(_monsterDeathSoundEffect, MonsterDeathSoundVolume);
                boss.IsDying = true;
                boss.DeathTimer = 0f;
                boss.State = Boss3State.Idle;
                if (boss.IsSceneBoss)
                {
                    _boss3Defeated = true;
                    _boss3HasSavedState = false;
                }
                return;
            }

            if (!boss.IsPhase2 && boss.HP < Boss3MaxHP * Boss3Phase2HpRatio)
                boss.IsPhase2 = true;
        }

        private AnimFrame GetBoss3Frame(Boss3 boss)
        {
            if (boss.IsDying && _boss3Die != null)
            {
                int dieFrame = (int)(boss.DeathTimer / Boss3DieFrameDuration);
                return _boss3Die[System.Math.Min(dieFrame, Boss3FrameCount - 1)];
            }

            int index = System.Math.Min(boss.FrameIndex, Boss3FrameCount - 1);
            if (boss.State == Boss3State.Attacking && !boss.IsDying)
            {
                var sheet = boss.AttackIsLaser ? _boss3AttackP2 : _boss3AttackP1;
                if (sheet != null)
                    return sheet[index];
            }
            return _boss3Idle[index];
        }

        private void DrawBoss3List()
        {
            if (!_boss3Loaded)
                return;

            foreach (var boss in _boss3List)
            {
                if (boss.Room != _currentRoomIndex)
                    continue;

                var frame = GetBoss3Frame(boss);

                bool flip = boss.FacingRight != Boss3SpritesFaceRight;
                float originX = flip ? frame.Trim.Width - frame.OriginX : frame.OriginX;
                int cellHeight = frame.Texture.Height / Boss3SheetRows;
                int cellBottom = (frame.Trim.Y / cellHeight + 1) * cellHeight;
                var origin = new Vector2(originX, cellBottom - frame.Trim.Y);

                float shakeX = (boss == _boss3SceneBoss && _isBoss3Scene && _boss3ShakeActive)
                    ? (float)System.Math.Sin(_boss3ShakeTime * BossSceneShakeFrequency) * BossSceneShakeAmplitude
                    : 0f;
                var position = new Vector2(boss.X + shakeX, _floorY + boss.YOffset);
                SpriteEffects effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Color tint = Color.White;
                if (boss.HitFlashTimer > 0f)
                {
                    float t = MathHelper.Clamp(boss.HitFlashTimer / Boss3HitFlashDuration, 0f, 1f);
                    tint = Color.Lerp(Color.White, new Color(255, 160, 160), t);
                }
                if (boss == _boss3SceneBoss && _isBoss3Scene && _boss3Stage == Boss3SceneStage.Absorb)
                {
                    float t = MathHelper.Clamp(_boss3StageTimer / Boss3AbsorbDuration, 0f, 1f);
                    tint = Color.Lerp(Color.White, new Color(255, 120, 120), (float)System.Math.Sin(t * System.Math.PI));
                }

                _spriteBatch.Draw(frame.Texture, position, frame.Trim, tint, 0f, origin, _boss3Scale, effects, 0f);

                if (boss.State == Boss3State.Attacking && !boss.IsDying)
                {
                    float headTopY = _floorY + boss.YOffset - _boss3Height;
                    int hitStart = boss.AttackIsLaser ? Boss3LaserHitFrameStart : Boss3SwipeHitFrameStart;
                    DrawEnemyWindupBar(boss.X, headTopY, GetAttackWindupProgress(boss.AnimTimer, Boss3AttackFrameDuration, hitStart));

                    if (boss.AttackIsLaser && boss.LaserStart != Vector2.Zero
                        && boss.FrameIndex >= Boss3LaserHitFrameStart && boss.FrameIndex <= Boss3LaserHitFrameEnd + 1)
                        DrawBoss3Laser(boss);
                }

                if (_showHitboxes)
                {
                    DrawSpriteBoundsDebug(position, origin, frame.Trim, _boss3Scale);
                    DrawRectOutline(GetBoss3Rect(boss), 2, new Color(255, 60, 60));
                }
            }
        }

        private void DrawBoss3Laser(Boss3 boss)
        {
            if (_pixelTexture == null)
                return;

            Vector2 delta = boss.LaserEnd - boss.LaserStart;
            float length = delta.Length();
            float angle = (float)System.Math.Atan2(delta.Y, delta.X);
            var origin = new Vector2(0f, 0.5f);

            _spriteBatch.Draw(_pixelTexture, boss.LaserStart, null, new Color(255, 40, 40, 90), angle, origin,
                new Vector2(length, Boss3LaserThickness * 2.2f), SpriteEffects.None, 0f);
            _spriteBatch.Draw(_pixelTexture, boss.LaserStart, null, new Color(255, 70, 60, 220), angle, origin,
                new Vector2(length, Boss3LaserThickness), SpriteEffects.None, 0f);
            _spriteBatch.Draw(_pixelTexture, boss.LaserStart, null, new Color(255, 235, 230, 255), angle, origin,
                new Vector2(length, Boss3LaserThickness * 0.35f), SpriteEffects.None, 0f);
        }

        private void DrawBoss3HealthBar()
        {
            if (_deathState == PlayerDeathState.GameOver)
                return;

            Boss3 boss = null;
            foreach (var candidate in _boss3List)
            {
                if (candidate.Room == _currentRoomIndex && !candidate.IsStatue && !candidate.IsDying && candidate.HP > 0f)
                {
                    boss = candidate;
                    break;
                }
            }

            if (boss == null)
                return;

            float ratio = MathHelper.Clamp(boss.HP / Boss3MaxHP, 0f, 1f);

            Vector2 nameSize = _hotbarFont.MeasureString(Boss3DisplayName) * BossHpBarNameScale;
            var namePosition = new Vector2((WindowedWidth - nameSize.X) / 2f, BossHpBarTopMargin);
            _spriteBatch.DrawString(_hotbarFont, Boss3DisplayName, namePosition + new Vector2(1f, 1f), Color.Black, 0f, Vector2.Zero, BossHpBarNameScale, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_hotbarFont, Boss3DisplayName, namePosition, Color.White, 0f, Vector2.Zero, BossHpBarNameScale, SpriteEffects.None, 0f);

            int x = (WindowedWidth - BossHpBarWidth) / 2;
            int y = BossHpBarTopMargin + (int)nameSize.Y + 4;
            var back = new Rectangle(x, y, BossHpBarWidth, BossHpBarHeight);
            DrawFilledRect(back, new Color(30, 8, 10, 230));

            int innerWidth = BossHpBarWidth - 4;
            int fillWidth = (int)(innerWidth * ratio);
            if (fillWidth > 0)
            {
                Color fill = boss.HitFlashTimer > 0f
                    ? new Color(255, 140, 130)
                    : (boss.IsPhase2 ? new Color(235, 85, 40) : new Color(170, 30, 60));
                DrawFilledRect(new Rectangle(x + 2, y + 2, fillWidth, BossHpBarHeight - 4), fill);
            }

            int markerX = x + 2 + (int)(innerWidth * Boss3Phase2HpRatio);
            DrawFilledRect(new Rectangle(markerX - 1, y, 2, BossHpBarHeight), new Color(255, 255, 255, 220));
            DrawRectOutline(back, 2, new Color(210, 185, 185));
        }

        // =====================================================================
        // ด่าน 3: ฉากผีโผล่ -> บทพูด -> บอสดูดผีเข้าตัว -> เริ่มสู้
        // =====================================================================

        private void ResetBoss3EncounterTimer()
        {
            _boss3EncounterTimer = 0f;
            _boss3EncounterTarget = Boss3GhostWaitMin + (float)_random.NextDouble() * (Boss3GhostWaitMax - Boss3GhostWaitMin);
        }

        private void UpdateBoss3Encounter(float dt)
        {
            if (!_boss3Loaded || _isBoss3Scene || _currentRoomIndex != Level3RoomIndex)
                return;
            if (_isTransitioning || _isWakeUpSequence || _isOrbScene || _isBossScene || _isBoss2Scene || _isBoss3Scene || _isDoorInteraction
                || _isPasscodeOpen || _isSignReading || _currentHP <= 0f)
                return;

            // กลับมาสู้ต่อหลังตาย: บอสเลือด/เฟสตามที่จดไว้ ข้ามฉากผี
            if (_boss3HasSavedState)
            {
                _boss3HasSavedState = false;
                float sx = _worldWidth - Boss3EdgeOffsetFromRight;
                _boss3SceneBoss = new Boss3
                {
                    X = sx,
                    Room = Level3RoomIndex,
                    HP = MathHelper.Clamp(_boss3SavedHP, 1f, Boss3MaxHP),
                    FacingRight = false,
                    State = Boss3State.Moving,
                    IsSceneBoss = true,
                    IsPhase2 = _boss3SavedPhase2 || _boss3SavedHP < Boss3MaxHP * Boss3Phase2HpRatio,
                    AttackCooldownTimer = 1.5f
                };
                _boss3List.Add(_boss3SceneBoss);
                return;
            }

            if (_boss3SceneDone || _boss3Defeated)
                return;

            if (_boss3SceneBoss == null || !_boss3SceneBoss.IsStatue || !_boss3List.Contains(_boss3SceneBoss))
                return;

            if (_isMoving)
                _boss3EncounterTimer += dt;

            float distanceToBoss = System.Math.Abs(_boss3SceneBoss.X - _kaiCenterX);
            if (_boss3EncounterTimer >= _boss3EncounterTarget || distanceToBoss < 750f)
                StartBoss3Scene();
        }

        private void StartBoss3Scene()
        {
            _boss3SceneDone = true;

            if (_boss3GhostTexture == null)
            {
                WakeBoss3();   // ไม่มีรูปผี: ข้ามฉากไปสู้เลย
                return;
            }

            _isBoss3Scene = true;
            _boss3Stage = Boss3SceneStage.GhostRise;
            _boss3StageTimer = 0f;
            _boss3FocusBlend = 0f;
            _boss3SceneZoom = CameraZoom;
            _boss3ShakeActive = false;
            _boss3ShakeTime = 0f;
            _boss3GhostTime = 0f;
            _boss3GhostAlpha = 0f;
            _boss3GhostShrink = 1f;

            float dirToBoss = _boss3SceneBoss.X >= _kaiCenterX ? 1f : -1f;
            float ghostX = MathHelper.Clamp(_kaiCenterX + dirToBoss * Boss3GhostSpawnDistance, 60f, _worldWidth - 60f);
            _boss3GhostPos = new Vector2(ghostX, _floorY);

            _isMoving = false;
            _isSprinting = false;
            _isAttacking = false;
            _attackFrameIndex = 0;
            _currentFrame = 0;
            _frameTimer = 0f;
            _facingRight = ghostX >= _kaiCenterX;

            if (!_isDraggingItem)
                _isInventoryOpen = false;
        }

        private float GetBoss3GhostHeight()
        {
            return _boss3GhostTrim.Height * _boss3GhostScale;
        }

        // ลอยโยกเบาๆ: แกน X ช้า แกน Y ช้า แอมพลิจูดน้อย กันเวียนหัว
        private void SetBoss3GhostHoverPos(float baseX, float sink)
        {
            float hover = _kaiTargetHeight * 0.12f;
            _boss3GhostPos = new Vector2(
                baseX + (float)System.Math.Sin(_boss3GhostTime * 1.1f) * 14f,
                _floorY - hover + sink + (float)System.Math.Sin(_boss3GhostTime * 1.6f) * 8f);
        }

        private void UpdateBoss3Scene(GameTime gameTime, bool click)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _boss3GhostTime += dt;
            _boss3StageTimer += dt;

            // ฉากเริ่มกลางเฟรมที่ผู้เล่นกำลังเดิน (UpdateKaiMovement ทำงานต่อหลังเริ่มฉาก ทำให้ _isMoving กลับเป็น true) ต้องบังคับให้ยืน idle ตลอดฉาก
            if (_isMoving || _isSprinting)
            {
                _isMoving = false;
                _isSprinting = false;
                _currentFrame = 0;
                _frameTimer = 0f;
            }

            if (_boss3ShakeActive)
                _boss3ShakeTime += dt;

            _boss3BarBlend = MathHelper.Min(1f, _boss3BarBlend + dt * Boss3LetterboxSpeed);

            if (_boss3SceneBoss != null)
            {
                _boss3SceneBoss.AnimTimer += dt;
                _boss3SceneBoss.FrameIndex = (int)(_boss3SceneBoss.AnimTimer / Boss3FrameDuration) % Boss3FrameCount;
            }

            float ghostHeight = GetBoss3GhostHeight();

            switch (_boss3Stage)
            {
                case Boss3SceneStage.GhostRise:
                    {
                        float t = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_boss3StageTimer / Boss3GhostRiseDuration, 0f, 1f));
                        _boss3GhostAlpha = 0.85f * t;
                        _boss3GhostBaseX = _boss3GhostBaseX == 0f ? _boss3GhostPos.X : _boss3GhostBaseX;
                        SetBoss3GhostHoverPos(_boss3GhostBaseX, (1f - t) * ghostHeight * 0.6f);
                        _boss3FocusBlend = 0f;
                        _boss3SceneZoom = CameraZoom;
                        if (_boss3StageTimer >= Boss3GhostRiseDuration)
                        {
                            _boss3Stage = Boss3SceneStage.Dialogue;
                            _boss3StageTimer = 0f;
                            StartDialogue(Boss3GhostDialogueLines, "Lost Soul");
                        }
                        break;
                    }

                case Boss3SceneStage.Dialogue:
                    _boss3GhostAlpha = 0.85f;
                    SetBoss3GhostHoverPos(_boss3GhostBaseX, 0f);
                    _boss3FocusBlend = 0f;
                    _boss3SceneZoom = CameraZoom;
                    if (UpdateDialogue(dt, click))
                    {
                        _boss3Stage = Boss3SceneStage.GhostFly;
                        _boss3StageTimer = 0f;
                        _boss3FlyStart = _boss3GhostPos;
                    }
                    break;

                case Boss3SceneStage.GhostFly:
                    {
                        float t = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_boss3StageTimer / Boss3GhostFlyDuration, 0f, 1f));
                        float bossCenterY = _floorY + _boss3SceneBoss.YOffset - _boss3Height * 0.55f;
                        float sway = (float)System.Math.Sin(_boss3GhostTime * 2.0f) * 16f * (1f - t);
                        float arc = (float)System.Math.Sin(t * System.Math.PI) * 90f;
                        _boss3GhostShrink = 1f - 0.6f * t;
                        _boss3GhostAlpha = 0.85f;
                        _boss3GhostPos = new Vector2(
                            MathHelper.Lerp(_boss3FlyStart.X, _boss3SceneBoss.X, t) + sway,
                            MathHelper.Lerp(_boss3FlyStart.Y, bossCenterY + ghostHeight * _boss3GhostShrink * 0.5f, t) - arc);

                        float pan = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_boss3StageTimer / Boss3SceneCameraPanDuration, 0f, 1f));
                        _boss3FocusPoint = _boss3GhostPos - new Vector2(0f, ghostHeight * _boss3GhostShrink * 0.5f);
                        _boss3FocusBlend = pan;
                        _boss3SceneZoom = MathHelper.Lerp(CameraZoom, BossSceneBossZoom, pan);

                        if (!_boss3ShakeActive && _boss3StageTimer >= Boss3GhostFlyDuration * 0.7f)
                        {
                            _boss3ShakeActive = true;   // บอสเริ่มดูด: กล้อง/บอสสั่น + เสียง
                            _boss3ShakeTime = 0f;
                            PlaySfx(_monsterDeathSoundEffect, MonsterDeathSoundVolume);
                        }

                        if (_boss3StageTimer >= Boss3GhostFlyDuration)
                        {
                            _boss3Stage = Boss3SceneStage.Absorb;
                            _boss3StageTimer = 0f;
                        }
                        break;
                    }

                case Boss3SceneStage.Absorb:
                    {
                        float fade = MathHelper.Clamp(_boss3StageTimer / 0.6f, 0f, 1f);
                        _boss3GhostAlpha = 0.85f * (1f - fade);
                        _boss3GhostShrink = 0.4f * (1f - fade * 0.7f);
                        float bossCenterY = _floorY + _boss3SceneBoss.YOffset - _boss3Height * 0.55f;
                        _boss3GhostPos = new Vector2(_boss3SceneBoss.X, bossCenterY + ghostHeight * _boss3GhostShrink * 0.5f);
                        _boss3FocusPoint = new Vector2(_boss3SceneBoss.X, _floorY + _boss3SceneBoss.YOffset - _boss3Height / 2f);
                        _boss3FocusBlend = 1f;
                        _boss3SceneZoom = BossSceneBossZoom;
                        if (_boss3StageTimer >= Boss3AbsorbDuration)
                        {
                            _boss3ShakeActive = false;
                            _boss3GhostAlpha = 0f;
                            _boss3Stage = Boss3SceneStage.ZoomOut;
                            _boss3StageTimer = 0f;
                        }
                        break;
                    }

                case Boss3SceneStage.ZoomOut:
                    {
                        float t = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_boss3StageTimer / BossSceneZoomOutDuration, 0f, 1f));
                        _boss3FocusPoint = new Vector2(_boss3SceneBoss.X, _floorY + _boss3SceneBoss.YOffset - _boss3Height / 2f);
                        _boss3FocusBlend = 1f - t;
                        _boss3SceneZoom = MathHelper.Lerp(BossSceneBossZoom, BossFightZoom, t);
                        _boss3BarBlend = MathHelper.Max(0f, _boss3BarBlend - dt * Boss3LetterboxSpeed * 2f);
                        if (_boss3StageTimer >= BossSceneZoomOutDuration)
                        {
                            EndBoss3Scene();
                            return;
                        }
                        break;
                    }
            }

            _cameraZoom = _boss3SceneZoom;
        }

        private float _boss3GhostBaseX;
        private Vector2 _boss3FlyStart;

        private void EndBoss3Scene()
        {
            _isBoss3Scene = false;
            _isDialogueActive = false;
            _boss3ShakeActive = false;
            _boss3FocusBlend = 0f;
            _boss3GhostAlpha = 0f;
            _boss3GhostBaseX = 0f;
            WakeBoss3();
            UpdateCamera();
        }

        // ปลุกบอส: เริ่มสู้ (กล้องค้างที่ซูมออกจนกว่าบอสจะตาย)
        private void WakeBoss3()
        {
            _boss3FightZoomBlend = 1f;
            _cameraZoom = BossFightZoom;

            if (_boss3SceneBoss != null)
            {
                _boss3SceneBoss.IsStatue = false;
                _boss3SceneBoss.State = Boss3State.Moving;
                _boss3SceneBoss.AnimTimer = 0f;
                _boss3SceneBoss.FrameIndex = 0;
                _boss3SceneBoss.AttackCooldownTimer = 1.5f;
            }
        }

        private void UpdateBoss3FightZoom(float dt)
        {
            if (_boss3BarBlend > 0f && !_isBoss3Scene)
                _boss3BarBlend = MathHelper.Max(0f, _boss3BarBlend - dt * Boss3LetterboxSpeed * 2f);

            float previousBlend = _boss3FightZoomBlend;
            float target = IsBoss3FightActive() ? 1f : 0f;
            float step = dt / BossFightZoomBlendDuration;

            if (_boss3FightZoomBlend < target)
                _boss3FightZoomBlend = MathHelper.Min(target, _boss3FightZoomBlend + step);
            else if (_boss3FightZoomBlend > target)
                _boss3FightZoomBlend = MathHelper.Max(target, _boss3FightZoomBlend - step);

            if (_boss3FightZoomBlend > 0f || previousBlend > 0f)
                _cameraZoom = MathHelper.Lerp(CameraZoom, BossFightZoom, MathHelper.SmoothStep(0f, 1f, _boss3FightZoomBlend));
        }

        // ผีวาดในแบตช์แบบ Linear (ภาพใหญ่ย่อลงแล้วไม่แตก): ส่องแสงฟ้าจางๆ ด้านหลัง
        private void DrawBoss3Ghost()
        {
            if (!_isBoss3Scene || _boss3GhostTexture == null || _boss3GhostAlpha <= 0.01f)
                return;

            var origin = new Vector2(_boss3GhostTrim.Width / 2f, _boss3GhostTrim.Height);
            float scale = _boss3GhostScale * _boss3GhostShrink;
            float pulse = 0.85f + 0.15f * (float)System.Math.Sin(_boss3GhostTime * 2.2f);
            SpriteEffects fx = _boss3GhostPos.X < _kaiCenterX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            _spriteBatch.Draw(_boss3GhostTexture, _boss3GhostPos, _boss3GhostTrim, new Color(120, 200, 230) * (_boss3GhostAlpha * 0.35f * pulse),
                0f, origin, scale * 1.08f, fx, 0f);
            _spriteBatch.Draw(_boss3GhostTexture, _boss3GhostPos, _boss3GhostTrim, Color.White * (_boss3GhostAlpha * pulse),
                0f, origin, scale, fx, 0f);
        }

        private void DrawBoss3Letterbox()
        {
            if (_boss3BarBlend <= 0f)
                return;

            int h = (int)(Boss3LetterboxHeight * MathHelper.SmoothStep(0f, 1f, _boss3BarBlend));
            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, h), Color.Black);
            DrawFilledRect(new Rectangle(0, WindowedHeight - h, WindowedWidth, h), Color.Black);
        }

        // ผู้เล่นตายแล้วเกิดใหม่: บอสตื่นแล้ว = จดเลือด/เฟสไว้ ลบตัวออก กลับเข้าห้องจะเกิดใหม่ด้วยค่าที่จดไว้ (ข้ามฉากผี)
        private void ResetBoss3EncounterOnRespawn()
        {
            if (_boss3SceneBoss != null && !_boss3SceneBoss.IsDying && _boss3List.Contains(_boss3SceneBoss) && !_boss3SceneBoss.IsStatue)
            {
                _boss3HasSavedState = true;
                _boss3SavedHP = _boss3SceneBoss.HP;
                _boss3SavedPhase2 = _boss3SceneBoss.IsPhase2;
                _boss3List.Remove(_boss3SceneBoss);
                _boss3SceneBoss = null;
            }

            _boss3List.RemoveAll(b => !b.IsSceneBoss);
            _isBoss3Scene = false;
            _boss3ShakeActive = false;
            _boss3FocusBlend = 0f;
            _boss3FightZoomBlend = 0f;
            _boss3BarBlend = 0f;
            _boss3GhostAlpha = 0f;
            ResetBoss3EncounterTimer();
        }

        private void ResetBoss3EncounterFull()
        {
            _isBoss3Scene = false;
            _boss3SceneDone = false;
            _boss3Defeated = false;
            _boss3HasSavedState = false;
            _boss3ShakeActive = false;
            _boss3FocusBlend = 0f;
            _boss3FightZoomBlend = 0f;
            _boss3BarBlend = 0f;
            _boss3GhostAlpha = 0f;
            _boss3GhostBaseX = 0f;
            _boss3SceneBoss = null;
            ResetBoss3EncounterTimer();
        }

        private void LoadBoss2Content()
        {
            try
            {
                _boss2Walk = LoadBoss2Frames("boss_2_walk");
                _boss2AttackP1 = LoadBoss2Frames("boss_2_phase1_attack");
                _boss2AttackP2 = LoadBoss2Frames("boss_2_phase2_attack");
                _boss2Throw = LoadBoss2Frames("boss_2_phase2_Throw");
                _boss2Back = LoadBoss2Frames("boss_2_phase2_back");
                _boss2Die = LoadBoss2Frames("boss_2_die");

                _boss2Scale = (_kaiTargetHeight * Boss2HeightToKaiRatio) / _boss2Walk[0].Trim.Height;
                _boss2Height = _boss2Walk[0].Trim.Height * _boss2Scale;
                int cellHeight = _boss2Walk[0].Texture.Height / Boss2SheetRows;
                _boss2HalfWidth = Boss2HitboxHalfWidthCellRatio * cellHeight * _boss2Scale;
                _boss2Loaded = true;
            }
            catch (System.Exception)
            {
                _boss2Loaded = false;
            }

            try
            {
                _boss2Block = LoadBoss2Frames("boss_2_parry_attack");
            }
            catch (System.Exception)
            {
                _boss2Block = null;
            }

            try
            {
                _boss2SwordTexture = Content.Load<Texture2D>("sword");
            }
            catch (System.Exception)
            {
                _boss2SwordTexture = null;
            }
        }

        // ชีท 4x2 ขนาดเซลล์ไม่เท่ากันทุกชีท (ดาบ/เอฟเฟกต์ยื่นออกด้านซ้าย) จึงยึดจุดกึ่งกลางลำตัวจาก "ขอบขวาสุดของตัวบอสในเฟรมแรก" ซึ่งอยู่ที่เดิมเสมอ
        private AnimFrame[] LoadBoss2Frames(string sheetAssetName)
        {
            var sheet = Content.Load<Texture2D>(sheetAssetName);
            int cellWidth = sheet.Width / Boss2SheetColumns;
            int cellHeight = sheet.Height / Boss2SheetRows;

            var frames = new AnimFrame[Boss2FrameCount];
            float anchorRelativeX = 0f;

            for (int i = 0; i < Boss2FrameCount; i++)
            {
                int col = i % Boss2SheetColumns;
                int row = i / Boss2SheetColumns;
                var cellRect = new Rectangle(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                var trim = GetOpaqueBounds(sheet, cellRect);

                if (i == 0)
                    anchorRelativeX = (trim.Right - cellRect.X) - Boss2AnchorFromRightCellRatio * cellHeight;

                float anchorAbsoluteX = cellRect.X + anchorRelativeX;
                frames[i] = new AnimFrame
                {
                    Texture = sheet,
                    Trim = trim,
                    OriginX = anchorAbsoluteX - trim.X,
                    ClampHalfWidth = trim.Width / 2f
                };
            }
            return frames;
        }

        private void SpawnBoss2List(int count)
        {
            float direction = _facingRight ? 1f : -1f;
            float baseX = _kaiCenterX + direction * Boss2SpawnDistance;

            for (int i = 0; i < count; i++)
            {
                float x = MathHelper.Clamp(
                    baseX + direction * i * Boss2SpawnSpacing,
                    _boss2HalfWidth,
                    _worldWidth - _boss2HalfWidth);

                _boss2List.Add(new Boss2
                {
                    X = x,
                    SpawnX = x,
                    Room = _currentRoomIndex,
                    HP = Boss2MaxHP,
                    State = Boss2State.Idle,
                    FacingRight = !_facingRight,
                    YOffset = GetSurfaceYAt(_currentRoomIndex, x, _boss2HalfWidth) - _floorY
                });
            }
        }

        private float GetBoss2WalkSpeed(Boss2 boss)
        {
            return boss.IsPhase2 ? Boss2WalkSpeed * Boss2Phase2SpeedMultiplier : Boss2WalkSpeed;
        }

        private void MoveBoss2(Boss2 boss, float direction, float deltaSeconds)
        {
            float vy = boss.VelocityY;
            bool air = boss.Airborne;
            boss.X = MoveMonsterHorizontally(boss.Room, boss.X, direction * GetBoss2WalkSpeed(boss) * deltaSeconds,
                _boss2HalfWidth, _boss2Height, boss.YOffset, ref vy, ref air);
            boss.VelocityY = vy;
            boss.Airborne = air;
            boss.X = MathHelper.Clamp(boss.X, _boss2HalfWidth, _worldWidth - _boss2HalfWidth);
            boss.FrameIndex = (int)(boss.AnimTimer / Boss2FrameDuration) % Boss2FrameCount;
        }

        private void UpdateBoss2List(GameTime gameTime)
        {
            if (!_boss2Loaded || _boss2List.Count == 0)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = _boss2List.Count - 1; i >= 0; i--)
            {
                var boss = _boss2List[i];

                if (boss.Room != _currentRoomIndex)
                    continue;

                if (boss.HitFlashTimer > 0f)
                    boss.HitFlashTimer = MathHelper.Max(0f, boss.HitFlashTimer - dt);
                if (boss.AttackCooldownTimer > 0f)
                    boss.AttackCooldownTimer = MathHelper.Max(0f, boss.AttackCooldownTimer - dt);
                if (boss.ThrowCooldownTimer > 0f)
                    boss.ThrowCooldownTimer = MathHelper.Max(0f, boss.ThrowCooldownTimer - dt);
                if (boss.BlockCooldownTimer > 0f)
                    boss.BlockCooldownTimer = MathHelper.Max(0f, boss.BlockCooldownTimer - dt);

                boss.AnimTimer += dt;

                {
                    float vy = boss.VelocityY;
                    bool air = boss.Airborne;
                    float yOffset = boss.YOffset;
                    UpdateMonsterVertical(boss.Room, boss.X, _boss2HalfWidth, ref yOffset, ref vy, ref air, dt);
                    boss.YOffset = yOffset;
                    boss.VelocityY = vy;
                    boss.Airborne = air;
                }

                if (boss.IsDying)
                {
                    boss.DeathTimer += dt;
                    if (boss.DeathTimer >= Boss2DieAnimDuration)
                        _boss2List.RemoveAt(i);
                    continue;
                }

                if (boss.IsStatue)
                {
                    // ฉากเจอบอส: ยืนนิ่งท่ายืน/ลอยวนตามแอนิเมชันเดิม จนกว่าฉากจะจบ
                    boss.State = Boss2State.Idle;
                    boss.FrameIndex = (int)(boss.AnimTimer / Boss2FrameDuration) % Boss2FrameCount;
                    continue;
                }

                switch (boss.State)
                {
                    case Boss2State.Attacking:
                        UpdateBoss2Attacking(boss);
                        continue;

                    case Boss2State.Throwing:
                        UpdateBoss2Throwing(boss);
                        continue;

                    case Boss2State.WaitingSwords:
                        UpdateBoss2WaitingSwords(boss, dt);
                        continue;

                    case Boss2State.Recalling:
                        UpdateBoss2Recalling(boss);
                        continue;

                    case Boss2State.Blocking:
                        UpdateBoss2Blocking(boss);
                        continue;
                }

                float distanceToKai = System.Math.Abs(_kaiCenterX - boss.X);
                bool canReachKaiVertically = IsMonsterVerticallyOverlappingKai(boss.YOffset, _boss2Height);

                // เฟส 2: อยู่ในระยะปาดาบและคูลดาวน์พร้อม -> ปาดาบ
                if (boss.IsPhase2 && boss.ThrowCooldownTimer <= 0f
                    && distanceToKai >= Boss2ThrowMinDistance && distanceToKai <= Boss2ThrowMaxDistance)
                {
                    boss.IsAggro = true;
                    boss.FacingRight = _kaiCenterX >= boss.X;
                    boss.State = Boss2State.Throwing;
                    boss.AnimTimer = 0f;
                    boss.FrameIndex = 0;
                    boss.SwordsLaunched = false;
                    PlaySwingSound();   // ปาดาบ: ใช้เสียงฟาดทั่วไป
                    continue;
                }

                if (distanceToKai <= Boss2AttackRange && canReachKaiVertically)
                {
                    boss.IsAggro = true;
                    boss.FacingRight = _kaiCenterX >= boss.X;

                    if (boss.AttackCooldownTimer <= 0f)
                    {
                        boss.State = Boss2State.Attacking;
                        boss.AnimTimer = 0f;
                        boss.FrameIndex = 0;
                        boss.AttackHitApplied = false;
                        boss.AttackIsPhase2 = boss.IsPhase2;
                        PlaySwordSwingSound();   // โจมตีระยะประชิด: เสียงเหวี่ยงดาบ
                    }
                    else
                    {
                        boss.State = Boss2State.Idle;
                        boss.FrameIndex = (int)(boss.AnimTimer / Boss2FrameDuration) % Boss2FrameCount;
                    }
                }
                else if (distanceToKai <= Boss2AggroRange)
                {
                    boss.IsAggro = true;
                    boss.FacingRight = _kaiCenterX >= boss.X;

                    if (distanceToKai <= 6f)
                    {
                        boss.State = Boss2State.Idle;
                        boss.FrameIndex = (int)(boss.AnimTimer / Boss2FrameDuration) % Boss2FrameCount;
                    }
                    else
                    {
                        boss.State = Boss2State.Walking;
                        MoveBoss2(boss, boss.FacingRight ? 1f : -1f, dt);
                    }
                }
                else if (boss.IsAggro)
                {
                    float distanceToSpawn = System.Math.Abs(boss.SpawnX - boss.X);

                    if (distanceToSpawn <= Boss2ReturnThreshold)
                    {
                        boss.X = boss.SpawnX;
                        boss.IsAggro = false;
                        boss.State = Boss2State.Idle;
                        boss.FrameIndex = (int)(boss.AnimTimer / Boss2FrameDuration) % Boss2FrameCount;
                    }
                    else
                    {
                        boss.State = Boss2State.Walking;
                        float direction = boss.SpawnX >= boss.X ? 1f : -1f;
                        boss.FacingRight = direction > 0f;
                        MoveBoss2(boss, direction, dt);
                    }
                }
                else
                {
                    boss.State = Boss2State.Idle;
                    boss.FrameIndex = (int)(boss.AnimTimer / Boss2FrameDuration) % Boss2FrameCount;
                }
            }
        }

        private void UpdateBoss2Attacking(Boss2 boss)
        {
            boss.FacingRight = _kaiCenterX >= boss.X;

            float frameDuration = boss.AttackIsPhase2 ? Boss2AttackFrameDurationP2 : Boss2AttackFrameDurationP1;
            int frame = GetAttackFrame(boss.AnimTimer, frameDuration, Boss2AttackHitFrameStart);

            if (frame >= Boss2FrameCount)
            {
                boss.State = Boss2State.Idle;
                boss.AnimTimer = 0f;
                boss.FrameIndex = 0;
                boss.AttackCooldownTimer = boss.AttackIsPhase2 ? Boss2AttackCooldownP2 : Boss2AttackCooldownP1;
                return;
            }

            boss.FrameIndex = frame;

            if (!boss.AttackHitApplied && frame >= Boss2AttackHitFrameStart && frame <= Boss2AttackHitFrameEnd)
                TryApplyBoss2AttackHit(boss);
        }

        // ท่าปาดาบ: เล่นชีท Throw พอถึงเฟรมปล่อย ดาบ 4 เล่มพุ่งออกจากมือ -> จบท่าแล้วยืนมือเปล่ารอดาบ
        private void UpdateBoss2Throwing(Boss2 boss)
        {
            if (!boss.SwordsLaunched)
                boss.FacingRight = _kaiCenterX >= boss.X;

            int frame = GetAttackFrame(boss.AnimTimer, Boss2ThrowFrameDuration, Boss2ThrowLaunchFrame);

            if (frame >= Boss2FrameCount)
            {
                boss.State = Boss2State.WaitingSwords;
                boss.FrameIndex = Boss2FrameCount - 1;
                boss.SwordWaitTimer = 0f;
                boss.TotalWaitTimer = 0f;
                return;
            }

            boss.FrameIndex = frame;

            if (!boss.SwordsLaunched && frame >= Boss2ThrowLaunchFrame)
            {
                boss.SwordsLaunched = true;
                SpawnBoss2Swords(boss);
            }
        }

        // ยืนมือเปล่าจนดาบทุกเล่มหยุดค้างแล้ว (รอ Boss2SwordHoverTime) ถึงเริ่มท่าดึงกลับ
        private void UpdateBoss2WaitingSwords(Boss2 boss, float dt)
        {
            boss.FrameIndex = Boss2FrameCount - 1;
            boss.TotalWaitTimer += dt;

            bool anySword = false;
            bool anyFlying = false;
            foreach (var sword in _boss2Swords)
            {
                if (sword.Owner != boss)
                    continue;
                anySword = true;
                if (sword.State == Boss2SwordState.Flying || sword.State == Boss2SwordState.Scattered)
                    anyFlying = true;   // ยังบินอยู่/กำลังตกพื้น ให้รอจนนิ่งก่อนถึงดึงกลับ
            }

            if (!anySword)
            {
                FinishBoss2SwordCycle(boss);
                return;
            }

            boss.SwordWaitTimer = anyFlying ? 0f : boss.SwordWaitTimer + dt;

            if (boss.SwordWaitTimer >= Boss2SwordHoverTime || boss.TotalWaitTimer >= Boss2RecallMaxWait)
            {
                boss.State = Boss2State.Recalling;
                boss.AnimTimer = 0f;
                boss.FrameIndex = 0;
                boss.BackParried = false;

                foreach (var sword in _boss2Swords)
                {
                    if (sword.Owner != boss)
                        continue;
                    sword.State = Boss2SwordState.Returning;
                    sword.ReturnTime = 0f;
                    sword.HitBack = false;
                    sword.Delay = 0f;
                }
            }
        }

        // ท่าดึงดาบกลับ (ชีท Back): ดาบวิ่งเร่งเข้าหามือ ท่าจบเมื่อเล่นครบ 8 เฟรมและดาบกลับมาครบ
        private void UpdateBoss2Recalling(Boss2 boss)
        {
            boss.FrameIndex = System.Math.Min((int)(boss.AnimTimer / Boss2RecallFrameDuration), Boss2FrameCount - 1);

            bool anySword = false;
            foreach (var sword in _boss2Swords)
            {
                if (sword.Owner == boss)
                {
                    anySword = true;
                    break;
                }
            }

            bool animDone = boss.AnimTimer >= Boss2FrameCount * Boss2RecallFrameDuration;

            if (!anySword && animDone)
            {
                FinishBoss2SwordCycle(boss);
            }
            else if (boss.AnimTimer >= Boss2RecallMaxWait)
            {
                _boss2Swords.RemoveAll(s => s.Owner == boss);   // กันค้าง: ดาบยังกลับไม่ถึงนานเกินไป ให้หายเข้ามือเลย
            }
        }

        private void FinishBoss2SwordCycle(Boss2 boss)
        {
            boss.State = Boss2State.Idle;
            boss.AnimTimer = 0f;
            boss.FrameIndex = 0;
            boss.ThrowCooldownTimer = Boss2ThrowCooldown;
            boss.AttackCooldownTimer = MathHelper.Max(boss.AttackCooldownTimer, 0.6f);
        }

        private Vector2 GetBoss2HandPoint(Boss2 boss, int index)
        {
            float dirX = boss.FacingRight ? 1f : -1f;
            float ratio = Boss2SwordHandHeightRatios[index % Boss2SwordHandHeightRatios.Length];
            return new Vector2(
                boss.X + dirX * _boss2HalfWidth * 0.9f,
                _floorY + boss.YOffset - _boss2Height * ratio);
        }

        private void SpawnBoss2Swords(Boss2 boss)
        {
            boss.OutParried = false;
            boss.BackParried = false;
            boss.SwordsDeflected = false;

            float targetX = _kaiCenterX;
            float targetY = _floorY + _kaiJumpOffsetY - _kaiTargetHeight * 0.5f;

            for (int i = 0; i < Boss2SwordHandHeightRatios.Length; i++)
            {
                Vector2 hand = GetBoss2HandPoint(boss, i);
                float angle = (float)System.Math.Atan2(targetY - hand.Y, targetX - hand.X)
                    + Boss2SwordSpreadRadians[i % Boss2SwordSpreadRadians.Length];

                _boss2Swords.Add(new Boss2Sword
                {
                    Owner = boss,
                    Index = i,
                    Room = boss.Room,
                    State = Boss2SwordState.Flying,
                    X = hand.X,
                    Y = hand.Y,
                    Angle = angle,
                    VX = (float)System.Math.Cos(angle) * Boss2SwordSpeed,
                    VY = (float)System.Math.Sin(angle) * Boss2SwordSpeed,
                    Delay = i * Boss2SwordLaunchStagger
                });
            }
        }

        private void UpdateBoss2Swords(GameTime gameTime)
        {
            if (_boss2Swords.Count == 0)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float halfWidth = GetCurrentHalfWidth();
            var kaiRect = new Rectangle(
                (int)(_kaiCenterX - halfWidth),
                (int)(_floorY + _kaiJumpOffsetY - _kaiTargetHeight),
                (int)(halfWidth * 2f),
                (int)_kaiTargetHeight);

            for (int i = _boss2Swords.Count - 1; i >= 0; i--)
            {
                var sword = _boss2Swords[i];

                if (sword.Room != _currentRoomIndex || sword.Owner == null || sword.Owner.IsDying || !_boss2List.Contains(sword.Owner))
                {
                    _boss2Swords.RemoveAt(i);
                    continue;
                }

                if (sword.State == Boss2SwordState.Flying)
                {
                    if (sword.Delay > 0f)
                    {
                        // ยังไม่ถึงคิวปล่อย: ค้างอยู่ที่มือ (ไม่วาด ไม่ทำดาเมจ)
                        sword.Delay -= dt;
                        Vector2 hand = GetBoss2HandPoint(sword.Owner, sword.Index);
                        sword.X = hand.X;
                        sword.Y = hand.Y;
                        continue;
                    }

                    sword.X += sword.VX * dt;
                    sword.Y += sword.VY * dt;
                    sword.Traveled += Boss2SwordSpeed * dt;

                    if (sword.Y >= _floorY - 8f)
                    {
                        sword.Y = _floorY - 8f;
                        sword.State = Boss2SwordState.Stuck;
                    }
                    else if (sword.Traveled >= Boss2SwordRange || sword.X < 0f || sword.X > _worldWidth)
                    {
                        sword.X = MathHelper.Clamp(sword.X, 0f, _worldWidth);
                        sword.State = Boss2SwordState.Stuck;
                    }
                }
                else if (sword.State == Boss2SwordState.Scattered)
                {
                    // กระจายออกตามแรงเหวี่ยง + ตกตามแรงโน้มถ่วง จนถึงพื้นแล้วนอนนิ่ง (Stuck) รอถูกดึงกลับ
                    sword.VY += Boss2SwordScatterGravity * dt;
                    sword.X += sword.VX * dt;
                    sword.Y += sword.VY * dt;
                    sword.Angle += sword.Spin * dt;

                    if (sword.X < 0f || sword.X > _worldWidth)
                    {
                        sword.X = MathHelper.Clamp(sword.X, 0f, _worldWidth);
                        sword.VX = 0f;
                    }

                    if (sword.Y >= _floorY - 8f)
                    {
                        sword.Y = _floorY - 8f;
                        sword.VX = 0f;
                        sword.VY = 0f;
                        sword.Spin = 0f;
                        sword.State = Boss2SwordState.Stuck;
                    }
                }
                else if (sword.State == Boss2SwordState.Returning)
                {
                    sword.ReturnTime += dt;
                    Vector2 hand = GetBoss2HandPoint(sword.Owner, sword.Index);
                    var toHand = new Vector2(hand.X - sword.X, hand.Y - sword.Y);
                    float distance = toHand.Length();
                    float step = (Boss2SwordReturnStartSpeed + Boss2SwordReturnAccel * sword.ReturnTime) * dt;

                    if (distance <= Boss2SwordArriveDistance || step >= distance)
                    {
                        _boss2Swords.RemoveAt(i);
                        continue;
                    }

                    toHand /= distance;
                    sword.X += toHand.X * step;
                    sword.Y += toHand.Y * step;
                    // ถูกดึงกลับ: ด้ามนำ ปลายดาบลากตามหลัง
                    sword.Angle = (float)System.Math.Atan2(toHand.Y, toHand.X) + MathHelper.Pi;
                }

                if (sword.Deflected || sword.Owner.SwordsDeflected)
                    continue;   // แพรี่สำเร็จแล้วในรอบนี้: ไม่ทำดาเมจอีก

                bool canHit = (sword.State == Boss2SwordState.Flying && !sword.HitOut)
                    || (sword.State == Boss2SwordState.Returning && !sword.HitBack);
                if (!canHit || _currentHP <= 0f || !Boss2SwordTouchesRect(sword, kaiRect))
                    continue;

                bool isOut = sword.State == Boss2SwordState.Flying;
                if (isOut)
                    sword.HitOut = true;
                else
                    sword.HitBack = true;

                if (IsParryActiveAgainst(sword.X))
                {
                    // แพรี่สำเร็จ (เล่มไหนก็ได้): ดาบทั้ง 4 เล่มกระจายออก ไม่ทำดาเมจจนจบรอบปา บอสไม่เสียเลือด
                    OnParrySuccess();
                    DeflectBoss2Swords(sword.Owner);
                    continue;
                }

                int damage = _random.Next(Boss2SwordDamageMin, Boss2SwordDamageMax + 1);
                _currentHP = MathHelper.Max(0f, _currentHP - damage);
                PlayKaiHurtSound(damage);

                _damagePopups.Add(new DamagePopup
                {
                    Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Position = new Vector2(_kaiCenterX + _random.Next(-15, 16), _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 8f),
                    Color = new Color(255, 120, 120)
                });
            }
        }

        // ดาบทุกเล่มของบอสตัวนี้กระจายออกเป็นพัดครึ่งวงกลมด้านบน (ความเร็วสุ่ม) แล้วตกพื้น
        // ถ้ากำลังท่าดึงกลับอยู่ ให้บอสกลับไปยืนมือเปล่ารอดาบนิ่งก่อน แล้วค่อยทำท่าดึงกลับใหม่ (ดาบกลับมือโดยไม่ทำดาเมจ)
        private void DeflectBoss2Swords(Boss2 owner)
        {
            owner.SwordsDeflected = true;

            float[] fanDegrees = { -155f, -120f, -60f, -25f };   // องศาในจอ: ลบ = ชี้ขึ้น
            foreach (var sword in _boss2Swords)
            {
                if (sword.Owner != owner)
                    continue;

                float degrees = fanDegrees[sword.Index % fanDegrees.Length] + (float)(_random.NextDouble() * 20.0 - 10.0);
                float angle = MathHelper.ToRadians(degrees);
                float speed = MathHelper.Lerp(Boss2SwordScatterMinSpeed, Boss2SwordScatterMaxSpeed, (float)_random.NextDouble());

                sword.Deflected = true;
                sword.State = Boss2SwordState.Scattered;
                sword.Delay = 0f;
                sword.VX = (float)System.Math.Cos(angle) * speed;
                sword.VY = (float)System.Math.Sin(angle) * speed;
                sword.Spin = (float)(_random.NextDouble() * 2.0 - 1.0) * Boss2SwordScatterMaxSpin;
            }

            if (owner.State == Boss2State.Recalling)
            {
                owner.State = Boss2State.WaitingSwords;
                owner.FrameIndex = Boss2FrameCount - 1;
                owner.SwordWaitTimer = 0f;
                owner.TotalWaitTimer = 0f;
            }
        }

        // เช็คชนหลายจุดตามความยาวตัวดาบ (กลางดาบ + ปลาย + ด้าม)
        private bool Boss2SwordTouchesRect(Boss2Sword sword, Rectangle rect)
        {
            float cos = (float)System.Math.Cos(sword.Angle);
            float sin = (float)System.Math.Sin(sword.Angle);

            for (int k = -2; k <= 2; k++)
            {
                float offset = k * Boss2SwordDrawLength * 0.2f;
                if (CircleIntersectsRect(sword.X + cos * offset, sword.Y + sin * offset, Boss2SwordRadius, rect))
                    return true;
            }
            return false;
        }

        private void TryApplyBoss2AttackHit(Boss2 boss)
        {
            float halfWidth = GetCurrentHalfWidth();
            var kaiRect = new Rectangle(
                (int)(_kaiCenterX - halfWidth),
                (int)(_floorY + _kaiJumpOffsetY - _kaiTargetHeight),
                (int)(halfWidth * 2f),
                (int)_kaiTargetHeight);

            float reach = boss.AttackIsPhase2 ? Boss2AttackReachP2 : Boss2AttackReachP1;
            var bossHitbox = new Rectangle(
                (int)(boss.FacingRight ? boss.X : boss.X - reach),
                (int)(_floorY + boss.YOffset - _boss2Height),
                (int)reach,
                (int)_boss2Height);

            if (!bossHitbox.Intersects(kaiRect))
                return;

            if (IsParryActiveAgainst(boss.X))
            {
                boss.AttackHitApplied = true;
                OnParrySuccess();
                ApplyBoss2Hit(boss, _random.Next(ParryDamageMin, ParryDamageMax + 1));
                return;
            }

            boss.AttackHitApplied = true;
            int damage = boss.AttackIsPhase2
                ? _random.Next(Boss2DamageMinP2, Boss2DamageMaxP2 + 1)
                : _random.Next(Boss2DamageMinP1, Boss2DamageMaxP1 + 1);
            _currentHP = MathHelper.Max(0f, _currentHP - damage);
            PlayKaiHurtSound(damage);

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(_kaiCenterX + _random.Next(-15, 16), _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 8f),
                Color = new Color(255, 120, 120)
            });
        }

        private void ApplyBoss2Hit(Boss2 boss, int damageOverride = 0, bool isTridentHit = false)
        {
            if (boss.IsDying)
                return;

            int baseDamage = RollMeleeBaseDamage();
            if (_seaweedBuffTimer > 0f)
                baseDamage += SeaweedAtkBonus;

            int damage = baseDamage;
            bool isCrit = damageOverride <= 0 && _random.NextDouble() < CritChance;
            if (isCrit)
                damage *= CritMultiplier;
            if (damageOverride > 0)
                damage = damageOverride;   // ดาเมจจากแพรี่ (ไม่คริ ไม่บวกบัฟ)

            boss.HP -= damage;
            boss.HitFlashTimer = Boss2HitFlashDuration;

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(boss.X + _random.Next(-30, 31), _floorY + boss.YOffset - _boss2Height - 8f),
                IsCrit = isCrit,
                Color = isTridentHit ? new Color(255, 215, 90) : (damageOverride > 0 ? new Color(120, 220, 255) : (isCrit ? new Color(255, 210, 80) : Color.White))
            });

            if (boss.HP <= 0f)
            {
                boss.HP = 0f;
                if (!boss.IsDying)
                    PlaySfx(_monsterDeathSoundEffect, MonsterDeathSoundVolume);
                boss.IsDying = true;
                boss.DeathTimer = 0f;
                boss.State = Boss2State.Idle;
                _boss2Swords.RemoveAll(s => s.Owner == boss);
                if (boss == _boss2SceneBoss)
                {
                    _boss2HasSavedState = false;
                    _boss2Defeated = true;   // เปิดทางไปด่าน 3 (ลูกศรขึ้นหลังท่าตายจบ)
                    DropBoss2Loot(boss);   // บอสในฉากห้องที่ 3: ดรอปยา HP
                }
                return;
            }

            // HP ต่ำกว่าเกณฑ์ -> เฟส 2 (ท่าที่กำลังเล่นอยู่จบด้วยชีทเดิม ท่าถัดไปถึงเปลี่ยน)
            if (!boss.IsPhase2 && boss.HP < Boss2MaxHP * Boss2Phase2HpRatio)
                boss.IsPhase2 = true;
        }

        private AnimFrame GetBoss2Frame(Boss2 boss)
        {
            if (boss.IsDying)
            {
                int dieFrame = (int)(boss.DeathTimer / Boss2DieFrameDuration);
                return _boss2Die[System.Math.Min(dieFrame, Boss2FrameCount - 1)];
            }

            int index = System.Math.Min(boss.FrameIndex, Boss2FrameCount - 1);

            switch (boss.State)
            {
                case Boss2State.Attacking:
                    return (boss.AttackIsPhase2 ? _boss2AttackP2 : _boss2AttackP1)[index];
                case Boss2State.Throwing:
                case Boss2State.WaitingSwords:
                    return _boss2Throw[index];
                case Boss2State.Recalling:
                    return _boss2Back[index];
                case Boss2State.Blocking:
                    return _boss2Block != null ? _boss2Block[index] : _boss2Walk[index];
                default:
                    return _boss2Walk[index];
            }
        }

        private void DrawBoss2List()
        {
            if (!_boss2Loaded)
                return;

            foreach (var boss in _boss2List)
            {
                if (boss.Room != _currentRoomIndex)
                    continue;

                var frame = GetBoss2Frame(boss);

                // ชีทต้นฉบับหันหน้าไปทางซ้าย: หันซ้าย = วาดปกติ / หันขวา = พลิกแนวนอน
                float originX = boss.FacingRight ? frame.Trim.Width - frame.OriginX : frame.OriginX;
                int cellHeight = frame.Texture.Height / Boss2SheetRows;
                int cellBottom = (frame.Trim.Y / cellHeight + 1) * cellHeight;   // แถวพื้น = ก้นเซลล์ ใช้ร่วมกันทุกเฟรมกันตัวเด้ง
                var origin = new Vector2(originX, cellBottom - frame.Trim.Y);
                float shakeX = (boss == _boss2SceneBoss && _isBoss2Scene && _boss2ShakeActive)
                    ? (float)System.Math.Sin(_boss2ShakeTime * BossSceneShakeFrequency) * BossSceneShakeAmplitude
                    : 0f;
                var position = new Vector2(boss.X + shakeX, _floorY + boss.YOffset);
                SpriteEffects effects = boss.FacingRight ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Color tint = Color.White;
                if (boss.HitFlashTimer > 0f)
                {
                    float t = MathHelper.Clamp(boss.HitFlashTimer / Boss2HitFlashDuration, 0f, 1f);
                    tint = Color.Lerp(Color.White, new Color(255, 160, 160), t);
                }

                _spriteBatch.Draw(frame.Texture, position, frame.Trim, tint, 0f, origin, _boss2Scale, effects, 0f);

                if (!boss.IsDying)
                {
                    float headTopY = _floorY + boss.YOffset - _boss2Height;

                    if (boss.State == Boss2State.Attacking)
                    {
                        float frameDuration = boss.AttackIsPhase2 ? Boss2AttackFrameDurationP2 : Boss2AttackFrameDurationP1;
                        DrawEnemyWindupBar(boss.X, headTopY,
                            GetAttackWindupProgress(boss.AnimTimer, frameDuration, Boss2AttackHitFrameStart));
                    }
                    else if (boss.State == Boss2State.Throwing && !boss.SwordsLaunched)
                    {
                        DrawEnemyWindupBar(boss.X, headTopY,
                            GetAttackWindupProgress(boss.AnimTimer, Boss2ThrowFrameDuration, Boss2ThrowLaunchFrame));
                    }
                }

                if (_showHitboxes)
                    DrawSpriteBoundsDebug(position, origin, frame.Trim, _boss2Scale);
            }
        }

        private void DrawBoss2Swords()
        {
            foreach (var sword in _boss2Swords)
            {
                if (sword.Room != _currentRoomIndex || sword.Owner == null)
                    continue;
                if (sword.State == Boss2SwordState.Flying && sword.Delay > 0f)
                    continue;

                // ใกล้ตัวบอสไม่วาด: ในชีท Throw/Back มีภาพดาบบินออก/เข้ามืออยู่แล้ว
                float bodyCenterY = _floorY + sword.Owner.YOffset - _boss2Height * 0.5f;
                float dx = sword.X - sword.Owner.X;
                float dy = sword.Y - bodyCenterY;
                if (dx * dx + dy * dy < Boss2SwordHideDistance * Boss2SwordHideDistance)
                    continue;

                var position = new Vector2(sword.X, sword.Y);

                if (_boss2SwordTexture != null)
                {
                    // หมุนให้แกนดาบ (ด้าม -> ปลาย) ตรงกับทิศ sword.Angle แล้วย่อให้ยาว Boss2SwordDrawLength
                    float scale = Boss2SwordDrawLength / (_boss2SwordTexture.Height * Boss2SwordImageAxisLengthToHeight);
                    float rotation = sword.Angle - MathHelper.ToRadians(Boss2SwordImageAxisAngleDegrees);
                    var textureOrigin = new Vector2(_boss2SwordTexture.Width / 2f, _boss2SwordTexture.Height / 2f);
                    _spriteBatch.Draw(_boss2SwordTexture, position, null, Color.White, rotation, textureOrigin, scale, SpriteEffects.None, 0f);
                    continue;
                }

                // ไม่มีภาพดาบเดี่ยว: วาดดาบจำลองจากสี่เหลี่ยม (ปลายดาบอยู่ทางทิศ Angle)
                float cos = (float)System.Math.Cos(sword.Angle);
                float sin = (float)System.Math.Sin(sword.Angle);
                var half = new Vector2(0.5f, 0.5f);
                float len = Boss2SwordDrawLength;

                _spriteBatch.Draw(_pixelTexture, position, null, new Color(205, 210, 220), sword.Angle, half,
                    new Vector2(len, Boss2SwordDrawThickness), SpriteEffects.None, 0f);
                _spriteBatch.Draw(_pixelTexture, position, null, new Color(245, 248, 255), sword.Angle, half,
                    new Vector2(len, Boss2SwordDrawThickness * 0.35f), SpriteEffects.None, 0f);

                var guard = position - new Vector2(cos, sin) * (len * 0.5f);
                _spriteBatch.Draw(_pixelTexture, guard, null, new Color(215, 175, 70), sword.Angle, half,
                    new Vector2(6f, 30f), SpriteEffects.None, 0f);

                var grip = position - new Vector2(cos, sin) * (len * 0.5f + 12f);
                _spriteBatch.Draw(_pixelTexture, grip, null, new Color(95, 60, 35), sword.Angle, half,
                    new Vector2(22f, 7f), SpriteEffects.None, 0f);
            }
        }

        private void DrawBoss2HealthBar()
        {
            if (_deathState == PlayerDeathState.GameOver)
                return;

            Boss2 boss = null;
            foreach (var candidate in _boss2List)
            {
                if (candidate.Room == _currentRoomIndex && !candidate.IsDying && candidate.HP > 0f)
                {
                    boss = candidate;
                    break;
                }
            }

            if (boss == null)
            {
                DrawBoss3HealthBar();
                return;
            }

            float ratio = MathHelper.Clamp(boss.HP / Boss2MaxHP, 0f, 1f);

            Vector2 nameSize = _hotbarFont.MeasureString(Boss2DisplayName) * BossHpBarNameScale;
            var namePosition = new Vector2((WindowedWidth - nameSize.X) / 2f, BossHpBarTopMargin);
            _spriteBatch.DrawString(_hotbarFont, Boss2DisplayName, namePosition + new Vector2(1f, 1f), Color.Black, 0f, Vector2.Zero, BossHpBarNameScale, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_hotbarFont, Boss2DisplayName, namePosition, Color.White, 0f, Vector2.Zero, BossHpBarNameScale, SpriteEffects.None, 0f);

            int x = (WindowedWidth - BossHpBarWidth) / 2;
            int y = BossHpBarTopMargin + (int)nameSize.Y + 4;
            var back = new Rectangle(x, y, BossHpBarWidth, BossHpBarHeight);
            DrawFilledRect(back, new Color(30, 8, 10, 230));

            int innerWidth = BossHpBarWidth - 4;
            int fillWidth = (int)(innerWidth * ratio);
            if (fillWidth > 0)
            {
                Color fill = boss.HitFlashTimer > 0f
                    ? new Color(255, 140, 130)
                    : (boss.IsPhase2 ? new Color(235, 85, 40) : new Color(200, 35, 45));
                DrawFilledRect(new Rectangle(x + 2, y + 2, fillWidth, BossHpBarHeight - 4), fill);
            }

            int markerX = x + 2 + (int)(innerWidth * Boss2Phase2HpRatio);
            DrawFilledRect(new Rectangle(markerX - 1, y, 2, BossHpBarHeight), new Color(255, 255, 255, 220));

            DrawRectOutline(back, 2, new Color(210, 185, 185));
        }

        private void DrawBoss2HitboxDebug()
        {
            if (!_showHitboxes)
                return;

            var hurtColor = new Color(255, 60, 60);
            var enemyAttackColor = new Color(255, 160, 40);

            foreach (var boss in _boss2List)
            {
                if (boss.Room != _currentRoomIndex || boss.IsDying)
                    continue;

                DrawRectOutline(new Rectangle((int)(boss.X - _boss2HalfWidth), (int)(_floorY + boss.YOffset - _boss2Height),
                    (int)(_boss2HalfWidth * 2f), (int)_boss2Height), 2, hurtColor);

                if (boss.State == Boss2State.Attacking)
                {
                    float reach = boss.AttackIsPhase2 ? Boss2AttackReachP2 : Boss2AttackReachP1;
                    DrawRectOutline(new Rectangle((int)(boss.FacingRight ? boss.X : boss.X - reach),
                        (int)(_floorY + boss.YOffset - _boss2Height), (int)reach, (int)_boss2Height), 2, enemyAttackColor);
                }
            }

            foreach (var sword in _boss2Swords)
            {
                if (sword.Room != _currentRoomIndex || (sword.State == Boss2SwordState.Flying && sword.Delay > 0f))
                    continue;

                float cos = (float)System.Math.Cos(sword.Angle);
                float sin = (float)System.Math.Sin(sword.Angle);
                for (int k = -2; k <= 2; k++)
                {
                    float offset = k * Boss2SwordDrawLength * 0.2f;
                    int r = (int)Boss2SwordRadius;
                    DrawRectOutline(new Rectangle((int)(sword.X + cos * offset) - r, (int)(sword.Y + sin * offset) - r, r * 2, r * 2), 1, enemyAttackColor);
                }
            }
        }

        // =====================================================================
        // Monster 2
        // =====================================================================

        private void LoadMonster2Content()
        {
            try
            {
                _monster2Walk = LoadMonster2Frames("monster_2_walk");
                _monster2Attack = LoadMonster2Frames("monster_2_attack");
                _monster2Die = LoadMonster2Frames("monster_2_die");

                _monster2Scale = (_kaiTargetHeight * Monster2HeightToKaiRatio) / _monster2Walk[0].Trim.Height;
                _monster2Height = _monster2Walk[0].Trim.Height * _monster2Scale;
                int cellHeight = _monster2Walk[0].Texture.Height / Monster2SheetRows;
                _monster2HalfWidth = Monster2HitboxHalfWidthCellRatio * cellHeight * _monster2Scale;
                _monster2Loaded = true;
            }
            catch (System.Exception)
            {
                _monster2Loaded = false;
            }
        }

        // ทุกชีทเซลล์ขนาดเท่ากันและเท้าอยู่ก้นเซลล์ จึงยึดจุดกึ่งกลางลำตัวที่ตำแหน่งคงที่ในเซลล์
        private AnimFrame[] LoadMonster2Frames(string sheetAssetName)
        {
            var sheet = Content.Load<Texture2D>(sheetAssetName);
            int cellWidth = sheet.Width / Monster2SheetColumns;
            int cellHeight = sheet.Height / Monster2SheetRows;

            var frames = new AnimFrame[Monster2FrameCount];
            for (int i = 0; i < Monster2FrameCount; i++)
            {
                int col = i % Monster2SheetColumns;
                int row = i / Monster2SheetColumns;
                var cellRect = new Rectangle(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                var trim = GetOpaqueBounds(sheet, cellRect);

                float anchorAbsoluteX = cellRect.X + Monster2AnchorCellWidthRatio * cellWidth;
                frames[i] = new AnimFrame
                {
                    Texture = sheet,
                    Trim = trim,
                    OriginX = anchorAbsoluteX - trim.X,
                    ClampHalfWidth = trim.Width / 2f
                };
            }
            return frames;
        }

        private void SpawnMonster2List(int count)
        {
            float direction = _facingRight ? 1f : -1f;
            float baseX = _kaiCenterX + direction * Monster2SpawnDistance;

            for (int i = 0; i < count; i++)
            {
                float x = MathHelper.Clamp(
                    baseX + direction * i * Monster2SpawnSpacing,
                    _monster2HalfWidth,
                    _worldWidth - _monster2HalfWidth);

                _monster2List.Add(new Monster2
                {
                    X = x,
                    SpawnX = x,
                    Room = _currentRoomIndex,
                    HP = Monster2MaxHP,
                    State = Monster2State.Idle,
                    FacingRight = !_facingRight,
                    YOffset = GetSurfaceYAt(_currentRoomIndex, x, _monster2HalfWidth) - _floorY
                });
            }
        }

        private void UpdateKaiStun(GameTime gameTime)
        {
            if (_kaiStunTimer > 0f)
                _kaiStunTimer = MathHelper.Max(0f, _kaiStunTimer - (float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        private Rectangle GetMonster2Rect(Monster2 monster)
        {
            return new Rectangle(
                (int)(monster.X - _monster2HalfWidth),
                (int)(_floorY + monster.YOffset - _monster2Height),
                (int)(_monster2HalfWidth * 2f),
                (int)_monster2Height);
        }

        private void UpdateMonster2List(GameTime gameTime)
        {
            if (!_monster2Loaded || _monster2List.Count == 0)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = _monster2List.Count - 1; i >= 0; i--)
            {
                var monster = _monster2List[i];

                if (monster.Room != _currentRoomIndex)
                    continue;

                if (monster.HitFlashTimer > 0f)
                    monster.HitFlashTimer = MathHelper.Max(0f, monster.HitFlashTimer - dt);
                if (monster.AttackCooldownTimer > 0f)
                    monster.AttackCooldownTimer = MathHelper.Max(0f, monster.AttackCooldownTimer - dt);

                monster.AnimTimer += dt;

                {
                    float vy = monster.VelocityY;
                    bool air = monster.Airborne;
                    float yOffset = monster.YOffset;
                    UpdateMonsterVertical(monster.Room, monster.X, _monster2HalfWidth, ref yOffset, ref vy, ref air, dt);
                    monster.YOffset = yOffset;
                    monster.VelocityY = vy;
                    monster.Airborne = air;
                }

                if (monster.IsDying)
                {
                    monster.DeathTimer += dt;
                    if (monster.DeathTimer >= Monster2DieAnimDuration)
                    {
                        // ท่าตายจบ: ดรอปยา HP (ครั้งเดียว) ที่จุดที่ตาย แล้วลบมอนออก
                        if (!monster.DeathDropDone)
                        {
                            monster.DeathDropDone = true;
                            int potionCount = _random.Next(HpPotionMinDropCount, HpPotionMaxDropCount + 1);
                            _worldItems.Add(new WorldItem
                            {
                                Type = ItemType.HpPotion,
                                Count = potionCount,
                                X = MathHelper.Clamp(monster.X, 0f, _worldWidth),
                                Room = monster.Room
                            });
                        }
                        _monster2List.RemoveAt(i);
                    }
                    continue;
                }

                switch (monster.State)
                {
                    case Monster2State.Charging:
                        monster.FacingRight = _kaiCenterX >= monster.X;
                        monster.StateTimer += dt;
                        monster.FrameIndex = 0;
                        if (monster.StateTimer >= Monster2ChargeDuration)
                        {
                            // เริ่มพุ่ง: ล็อกทิศไปทาง Kai ตอนนี้ (ไม่ตามตัว ถ้า Kai ขยับ/กระโดดหนีก็หลบได้)
                            monster.State = Monster2State.Dashing;
                            monster.StateTimer = 0f;
                            monster.DashDirection = _kaiCenterX >= monster.X ? 1f : -1f;
                            monster.FacingRight = monster.DashDirection > 0f;
                            monster.DashTraveled = 0f;
                            monster.DashHitApplied = false;
                        }
                        continue;

                    case Monster2State.Dashing:
                        UpdateMonster2Dashing(monster, dt);
                        continue;

                    case Monster2State.Recovering:
                        monster.StateTimer += dt;
                        monster.FrameIndex = Monster2FrameCount - 1;
                        if (monster.StateTimer >= Monster2RecoverDuration)
                        {
                            monster.State = Monster2State.Idle;
                            monster.AnimTimer = 0f;
                            monster.AttackCooldownTimer = Monster2AttackCooldown;
                        }
                        continue;

                    case Monster2State.Stunned:
                        monster.StateTimer += dt;
                        monster.FrameIndex = Monster2FrameCount - 1;
                        if (monster.StateTimer >= Monster2SelfStunDuration)
                        {
                            monster.State = Monster2State.Idle;
                            monster.AnimTimer = 0f;
                            monster.AttackCooldownTimer = Monster2AttackCooldown;
                        }
                        continue;
                }

                float distanceToKai = System.Math.Abs(_kaiCenterX - monster.X);
                bool canReachKaiVertically = IsMonsterVerticallyOverlappingKai(monster.YOffset, _monster2Height);

                if (distanceToKai <= Monster2ChargeTriggerRange && canReachKaiVertically && monster.AttackCooldownTimer <= 0f)
                {
                    AggroMonster2(monster);
                    monster.FacingRight = _kaiCenterX >= monster.X;
                    monster.State = Monster2State.Charging;
                    monster.StateTimer = 0f;
                    monster.FrameIndex = 0;
                }
                else if (distanceToKai <= Monster2AggroRange || monster.AlwaysAggro)
                {
                    AggroMonster2(monster);
                    monster.FacingRight = _kaiCenterX >= monster.X;

                    // คูลดาวน์ยังไม่หมด และอยู่ใกล้แล้ว: ยืนรอ ไม่เดินทับตัว Kai
                    if (distanceToKai <= 6f || (monster.AttackCooldownTimer > 0f && distanceToKai <= Monster2ChargeTriggerRange * 0.5f))
                    {
                        monster.State = Monster2State.Idle;
                        monster.FrameIndex = 0;
                    }
                    else
                    {
                        monster.State = Monster2State.Walking;
                        MoveMonster2(monster, monster.FacingRight ? 1f : -1f, dt);
                    }
                }
                else if (monster.IsAggro)
                {
                    float distanceToSpawn = System.Math.Abs(monster.SpawnX - monster.X);

                    if (distanceToSpawn <= Monster2ReturnThreshold)
                    {
                        monster.X = monster.SpawnX;
                        monster.IsAggro = false;
                        monster.State = Monster2State.Idle;
                        monster.FrameIndex = 0;
                    }
                    else
                    {
                        monster.State = Monster2State.Walking;
                        float direction = monster.SpawnX >= monster.X ? 1f : -1f;
                        monster.FacingRight = direction > 0f;
                        MoveMonster2(monster, direction, dt);
                    }
                }
                else
                {
                    monster.State = Monster2State.Idle;
                    monster.FrameIndex = 0;
                }
            }
        }

        private void MoveMonster2(Monster2 monster, float direction, float dt)
        {
            float vy = monster.VelocityY;
            bool air = monster.Airborne;
            monster.X = MoveMonsterHorizontally(monster.Room, monster.X, direction * Monster2WalkSpeed * dt,
                _monster2HalfWidth, _monster2Height, monster.YOffset, ref vy, ref air);
            monster.VelocityY = vy;
            monster.Airborne = air;
            monster.X = MathHelper.Clamp(monster.X, _monster2HalfWidth, _worldWidth - _monster2HalfWidth);
            monster.FrameIndex = (int)(monster.AnimTimer / Monster2FrameDuration) % Monster2FrameCount;
        }

        // พุ่งตรงไปตามทิศที่ล็อกไว้ จนครบระยะ / ชน Kai / ชนกำแพง แล้วพัก
        private void UpdateMonster2Dashing(Monster2 monster, float dt)
        {
            monster.StateTimer += dt;

            float dashDuration = Monster2DashDistance / Monster2DashSpeed;
            monster.FrameIndex = MathHelper.Clamp(1 + (int)(monster.StateTimer / dashDuration * 6f), 1, 6);

            float before = monster.X;
            float vy = monster.VelocityY;
            bool air = monster.Airborne;
            float step = Monster2DashSpeed * dt;
            monster.X = MoveMonsterHorizontally(monster.Room, monster.X, monster.DashDirection * step,
                _monster2HalfWidth, _monster2Height, monster.YOffset, ref vy, ref air);
            monster.VelocityY = vy;
            monster.Airborne = air;
            monster.X = MathHelper.Clamp(monster.X, _monster2HalfWidth, _worldWidth - _monster2HalfWidth);

            float moved = System.Math.Abs(monster.X - before);
            monster.DashTraveled += moved;

            if (!monster.DashHitApplied)
            {
                float halfWidth = GetCurrentHalfWidth();
                var kaiRect = new Rectangle(
                    (int)(_kaiCenterX - halfWidth),
                    (int)(_floorY + _kaiJumpOffsetY - _kaiTargetHeight),
                    (int)(halfWidth * 2f),
                    (int)_kaiTargetHeight);

                if (_currentHP > 0f && GetMonster2Rect(monster).Intersects(kaiRect))
                {
                    monster.DashHitApplied = true;

                    if (IsParryActiveAgainst(monster.X))
                    {
                        // แพรี่สำเร็จ: Kai ไม่เสียเลือด/ไม่สตั้น มอนติดสตั้นกลับ
                        OnParrySuccess();
                        monster.State = Monster2State.Stunned;
                        monster.StateTimer = 0f;
                        return;
                    }

                    _currentHP = MathHelper.Max(0f, _currentHP - Monster2Damage);
                    PlayKaiHurtSound(Monster2Damage);
                    _kaiStunTimer = Monster2KaiStunDuration;
                    _damagePopups.Add(new DamagePopup
                    {
                        Text = Monster2Damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        Position = new Vector2(_kaiCenterX + _random.Next(-15, 16), _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 8f),
                        Color = new Color(255, 120, 120)
                    });

                    monster.State = Monster2State.Recovering;
                    monster.StateTimer = 0f;
                    return;
                }
            }

            // ครบระยะ หรือพุ่งไม่ไปต่อ (ชนแผ่นพื้นสูงเกินกระโดดถึง/ขอบโลก) -> พัก
            if (monster.DashTraveled >= Monster2DashDistance || moved < step * 0.2f)
            {
                monster.State = Monster2State.Recovering;
                monster.StateTimer = 0f;
            }
        }

        private void ApplyMonster2Hit(Monster2 monster)
        {
            if (monster.IsDying)
                return;

            int baseDamage = RollMeleeBaseDamage();
            if (_seaweedBuffTimer > 0f)
                baseDamage += SeaweedAtkBonus;

            int damage = baseDamage;
            bool isCrit = _random.NextDouble() < CritChance;
            if (isCrit)
                damage *= CritMultiplier;

            monster.HP -= damage;
            monster.HitFlashTimer = Monster2HitFlashDuration;

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(monster.X + _random.Next(-20, 21), _floorY + monster.YOffset - _monster2Height - 8f),
                IsCrit = isCrit,
                Color = isCrit ? new Color(255, 210, 80) : Color.White
            });

            if (monster.HP <= 0f)
            {
                monster.HP = 0f;
                if (!monster.IsDying)
                    PlaySfx(_monsterDeathSoundEffect, MonsterDeathSoundVolume);
                monster.IsDying = true;
                monster.DeathTimer = 0f;
                monster.State = Monster2State.Idle;
            }
        }

        private AnimFrame GetMonster2Frame(Monster2 monster)
        {
            if (monster.IsDying)
            {
                int dieFrame = (int)(monster.DeathTimer / Monster2DieFrameDuration);
                return _monster2Die[System.Math.Min(dieFrame, Monster2DieFrameCount - 1)];
            }

            int index = System.Math.Min(monster.FrameIndex, Monster2FrameCount - 1);

            switch (monster.State)
            {
                case Monster2State.Charging:
                case Monster2State.Dashing:
                case Monster2State.Recovering:
                case Monster2State.Stunned:
                    return _monster2Attack[index];
                default:
                    return _monster2Walk[index];
            }
        }

        private void DrawMonster2List()
        {
            if (!_monster2Loaded)
                return;

            foreach (var monster in _monster2List)
            {
                if (monster.Room != _currentRoomIndex)
                    continue;

                var frame = GetMonster2Frame(monster);

                bool flip = monster.FacingRight != Monster2SpritesFaceRight;
                float originX = flip ? frame.Trim.Width - frame.OriginX : frame.OriginX;
                int cellHeight = frame.Texture.Height / Monster2SheetRows;
                int cellBottom = (frame.Trim.Y / cellHeight + 1) * cellHeight;   // เท้าอยู่ก้นเซลล์ ใช้ร่วมกันทุกเฟรมกันตัวเด้ง
                var origin = new Vector2(originX, cellBottom - frame.Trim.Y);

                // ชาร์จ: ตัวสั่นเบาๆ ให้รู้ว่ากำลังจะพุ่ง
                float shakeX = monster.State == Monster2State.Charging && !monster.IsDying
                    ? (float)System.Math.Sin(monster.StateTimer * 70f) * Monster2ChargeShakeAmplitude
                    : 0f;
                var position = new Vector2(monster.X + shakeX, _floorY + monster.YOffset);
                SpriteEffects effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Color tint = Color.White;
                if (monster.State == Monster2State.Stunned && !monster.IsDying)
                    tint = new Color(150, 190, 255);   // ติดสตั้น = ออกฟ้า
                if (monster.HitFlashTimer > 0f)
                {
                    float t = MathHelper.Clamp(monster.HitFlashTimer / Monster2HitFlashDuration, 0f, 1f);
                    tint = Color.Lerp(tint, new Color(255, 160, 160), t);
                }

                _spriteBatch.Draw(frame.Texture, position, frame.Trim, tint, 0f, origin, _monster2Scale, effects, 0f);

                if (monster.State == Monster2State.Charging && !monster.IsDying)
                {
                    DrawEnemyWindupBar(monster.X, _floorY + monster.YOffset - _monster2Height,
                        MathHelper.Clamp(monster.StateTimer / Monster2ChargeDuration, 0f, 1f));
                }

                if (_showHitboxes)
                    DrawSpriteBoundsDebug(position, origin, frame.Trim, _monster2Scale);
            }
        }

        private void DrawMonster2HitboxDebug()
        {
            if (!_showHitboxes)
                return;

            foreach (var monster in _monster2List)
            {
                if (monster.Room != _currentRoomIndex || monster.IsDying)
                    continue;

                DrawRectOutline(GetMonster2Rect(monster), 2, monster.State == Monster2State.Dashing ? new Color(255, 160, 40) : new Color(255, 60, 60));
            }
        }

        // =====================================================================
        // Trident
        // =====================================================================

        private void LoadTridentContent()
        {
            try
            {
                _kaiTridentFrames = LoadKaiTridentFrames("Char_Attack_Trident_sheet");
                _tridentPoseScale = _kaiTargetHeight / _kaiTridentFrames[TridentPoseStandingFrame].Trim.Height;
            }
            catch (System.Exception)
            {
                _kaiTridentFrames = null;   // ไม่มีชีท: ใช้แบบเดิม (รูป trident ลอยที่ตัว Kai ตอนเล็ง)
            }

            try
            {
                _tridentTexture = Content.Load<Texture2D>("trident");
            }
            catch (System.Exception)
            {
                _tridentTexture = null;   // ไม่มีภาพก็ใช้ได้ แต่ไอคอน/ภาพตอนบินจะเป็นเส้นสีทองแทน
            }
        }

        // ทุกเฟรมในชีทขนาดเซลล์เท่ากัน เท้าอยู่ก้นเซลล์ ยึดจุดกึ่งกลางลำตัวจากเฟรมยืน (ไม่มี trident) ให้ตัวไม่ไหลตอนสลับเฟรม
        private AnimFrame[] LoadKaiTridentFrames(string sheetAssetName)
        {
            var sheet = Content.Load<Texture2D>(sheetAssetName);
            int cellWidth = sheet.Width / TridentPoseColumns;
            int cellHeight = sheet.Height / TridentPoseRows;

            var cells = new Rectangle[TridentPoseFrameCount];
            var trims = new Rectangle[TridentPoseFrameCount];
            for (int i = 0; i < TridentPoseFrameCount; i++)
            {
                cells[i] = new Rectangle((i % TridentPoseColumns) * cellWidth, (i / TridentPoseColumns) * cellHeight, cellWidth, cellHeight);
                trims[i] = GetOpaqueBounds(sheet, cells[i]);
            }

            var stand = trims[TridentPoseStandingFrame];
            float anchorRelativeX = (stand.X - cells[TridentPoseStandingFrame].X) + stand.Width / 2f;
            float halfWidth = stand.Width / 2f;

            var frames = new AnimFrame[TridentPoseFrameCount];
            for (int i = 0; i < TridentPoseFrameCount; i++)
            {
                frames[i] = new AnimFrame
                {
                    Texture = sheet,
                    Trim = trims[i],
                    OriginX = cells[i].X + anchorRelativeX - trims[i].X,
                    ClampHalfWidth = halfWidth
                };
            }
            return frames;
        }

        private void LoadSwordAttackContent()
        {
            try
            {
                var sheet = Content.Load<Texture2D>("Attack_Sword_P1_spritesheet");
                int cellWidth = sheet.Width / SwordAttackSheetColumns;
                int cellHeight = sheet.Height / SwordAttackSheetRows;

                var frames = new AnimFrame[SwordAttackTotalFrames];
                for (int i = 0; i < SwordAttackTotalFrames; i++)
                {
                    var cell = new Rectangle((i % SwordAttackSheetColumns) * cellWidth, (i / SwordAttackSheetColumns) * cellHeight, cellWidth, cellHeight);
                    var trim = GetOpaqueBounds(sheet, cell);
                    frames[i] = new AnimFrame
                    {
                        Texture = sheet,
                        Trim = trim,
                        OriginX = cell.X + SwordAttackAnchorXRatio * cellWidth - trim.X   // จุดกึ่งกลางลำตัวคงที่ในเซลล์ เท้าอยู่ก้นเซลล์ทุกเฟรม
                    };
                }

                _swordAttackScale = _kaiTargetHeight / frames[SwordAttackScaleReferenceFrame].Trim.Height;

                // ความกว้างใช้ชนกำแพง/ศัตรูตอนฟัน: ใช้เท่าตัว Kai ท่าปกติ (ไม่ผันตามความกว้างดาบที่เหวี่ยง)
                float idleHalfWidth = _kaiIdle[0].ClampHalfWidth * _kaiScale;
                for (int i = 0; i < SwordAttackTotalFrames; i++)
                    frames[i].ClampHalfWidth = idleHalfWidth / _swordAttackScale;

                _kaiSwordAttack = frames;
            }
            catch (System.Exception)
            {
                _kaiSwordAttack = null;   // ไม่มีชีท: ตีด้วยดาบใช้ท่าประแจเหมือนเดิม
            }
        }

        private Vector2 GetKaiTridentCenter()
        {
            return new Vector2(_kaiCenterX, _floorY + _kaiJumpOffsetY - _kaiTargetHeight * 0.62f);
        }

        // คลิกขวาค้างตอนถือ trident = ง้างและเล็งตามเมาส์ (เล่นท่าเฟรม 1-4 แล้วค้าง) / ปล่อย = ขว้าง (เล่นท่าเฟรม 5-7)
        private void UpdateTridentThrow(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // ท่าขว้างหลังปล่อยคลิกขวา: เฟรม 5 -> 7 แล้วกลับท่าปกติ
            if (_isTridentThrowAnim)
            {
                _tridentThrowAnimTimer += dt;
                int throwFrame = TridentThrowFirstFrame + (int)(_tridentThrowAnimTimer / TridentThrowFrameDuration);

                if (throwFrame >= TridentPoseFrameCount || _kaiStunTimer > 0f)
                {
                    _isTridentThrowAnim = false;
                    _tridentPoseFrame = -1;
                }
                else
                {
                    _tridentPoseFrame = throwFrame;
                }
                return;
            }

            ItemType heldItem = GetHeldItem();
            bool canAction = !_isInventoryOpen && !_isConsoleOpen && !_isTransitioning && !_isGiveItemMenuOpen && _kaiStunTimer <= 0f;
            bool rightDown = _mouseState.RightButton == ButtonState.Pressed;

            if (_isAimingTrident && (heldItem != ItemType.Trident || !canAction || _isAttacking))
            {
                _isAimingTrident = false;   // เปลี่ยนไอเทม/เปิดเมนู/ติดสตั้น = ยกเลิกการง้าง
                _tridentPoseFrame = -1;
                return;
            }

            if (!_isAimingTrident)
            {
                bool rightJustPressed = rightDown && _previousMouseState.RightButton == ButtonState.Released;
                if (rightJustPressed && heldItem == ItemType.Trident && canAction && !_isAttacking && _parryTimer <= 0f)
                {
                    _isAimingTrident = true;
                    _tridentAimAnimTimer = 0f;
                }
                else
                {
                    _tridentPoseFrame = -1;
                    return;
                }
            }

            // ท่าง้าง: เฟรม 1 -> 4 แล้วค้างที่เฟรม 4
            _tridentAimAnimTimer += dt;
            _tridentPoseFrame = _kaiTridentFrames != null
                ? System.Math.Min(TridentAimLastFrame, (int)(_tridentAimAnimTimer / TridentAimFrameDuration))
                : -1;

            // เล็งไปที่เมาส์ (แปลงพิกัดเมาส์เป็นพิกัดโลกด้วยกล้องที่ใช้อยู่)
            Vector2 center = GetKaiTridentCenter();
            Vector2 mouseWorld = Vector2.Transform(GetLogicalMousePosition(), Matrix.Invert(_cameraTransform));
            Vector2 toMouse = mouseWorld - center;
            if (toMouse.LengthSquared() > 4f)
                _tridentAimAngle = (float)System.Math.Atan2(toMouse.Y, toMouse.X);

            _facingRight = System.Math.Cos(_tridentAimAngle) >= 0.0;   // หันหน้าไปทางที่เล็ง

            if (!rightDown)
            {
                ThrowTrident();   // trident จริงหลุดจากมือพร้อมเริ่มเฟรม 5
                _isAimingTrident = false;

                if (_kaiTridentFrames != null)
                {
                    _isTridentThrowAnim = true;
                    _tridentThrowAnimTimer = 0f;
                    _tridentPoseFrame = TridentThrowFirstFrame;
                }
                else
                {
                    _tridentPoseFrame = -1;
                }
            }
        }

        private void ThrowTrident()
        {
            if (_equippedHotbarIndex < 0 || _hotbarSlots[_equippedHotbarIndex].Type != ItemType.Trident
                || _hotbarSlots[_equippedHotbarIndex].IsEmpty)
                return;

            int index = _equippedHotbarIndex;
            _hotbarSlots[index].Count--;
            if (_hotbarSlots[index].Count <= 0)
            {
                _hotbarSlots[index].Clear();
                _equippedHotbarIndex = -1;
            }

            var dir = new Vector2((float)System.Math.Cos(_tridentAimAngle), (float)System.Math.Sin(_tridentAimAngle));
            Vector2 start = GetKaiTridentCenter() + dir * TridentHandDistance;

            _tridentProjectiles.Add(new TridentProjectile
            {
                X = start.X,
                Y = start.Y,
                VX = dir.X * TridentThrowSpeed,
                VY = dir.Y * TridentThrowSpeed,
                Angle = _tridentAimAngle,
                Room = _currentRoomIndex
            });

            PlaySwingSound();   // เสียงฟาดอาวุธ ตอนตรีศูลหลุดจากมือ
        }

        private void DropTridentAt(float x, int room)
        {
            _worldItems.Add(new WorldItem
            {
                Type = ItemType.Trident,
                Count = 1,
                X = MathHelper.Clamp(x, 30f, _worldWidth - 30f),
                Room = room
            });
        }

        private Rectangle GetBoss2Rect(Boss2 boss)
        {
            return new Rectangle(
                (int)(boss.X - _boss2HalfWidth),
                (int)(_floorY + boss.YOffset - _boss2Height),
                (int)(_boss2HalfWidth * 2f),
                (int)_boss2Height);
        }

        private bool TridentTouchesRect(TridentProjectile p, Rectangle rect)
        {
            float cos = (float)System.Math.Cos(p.Angle);
            float sin = (float)System.Math.Sin(p.Angle);

            for (int k = -2; k <= 2; k++)
            {
                float offset = k * TridentDrawLength * 0.2f;
                if (CircleIntersectsRect(p.X + cos * offset, p.Y + sin * offset, TridentHitRadius, rect))
                    return true;
            }
            return false;
        }

        private void UpdateTridentProjectiles(GameTime gameTime)
        {
            if (_tridentProjectiles.Count == 0)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = _tridentProjectiles.Count - 1; i >= 0; i--)
            {
                var p = _tridentProjectiles[i];

                if (p.Room != _currentRoomIndex)
                {
                    DropTridentAt(p.X, p.Room);   // เปลี่ยนห้องระหว่างบิน: ตกเป็นไอเทมในห้องเดิม
                    _tridentProjectiles.RemoveAt(i);
                    continue;
                }

                Vector2 oldTip = GetTridentTip(p);

                if (!p.Falling)
                {
                    p.X += p.VX * dt;
                    p.Y += p.VY * dt;
                    p.Traveled += TridentThrowSpeed * dt;
                    p.Angle = (float)System.Math.Atan2(p.VY, p.VX);
                    if (p.Traveled >= TridentMaxRange)
                        p.Falling = true;
                }
                else
                {
                    p.VY += TridentGravity * dt;
                    p.X += p.VX * dt;
                    p.Y += p.VY * dt;
                    if (p.Deflected)
                        p.Angle += p.Spin * dt;
                    else
                        p.Angle = (float)System.Math.Atan2(p.VY, p.VX);
                }

                // ชนบอส 2
                bool consumed = false;
                if (!p.Deflected)
                {
                    foreach (var boss in _boss2List)
                    {
                        if (boss.Room != _currentRoomIndex || boss.IsDying)
                            continue;
                        if (!TridentTouchesRect(p, GetBoss2Rect(boss)))
                            continue;

                        consumed = HandleTridentHitBoss2(p, boss);
                        break;
                    }
                }

                if (consumed)
                {
                    _tridentProjectiles.RemoveAt(i);
                    continue;
                }

                if (TryStickTridentOnSolid(p, oldTip))
                {
                    _tridentProjectiles.RemoveAt(i);   // ปักค้างที่พื้น/แผ่นสูง เก็บกลับด้วย E
                }
                else if (p.X < 0f || p.X > _worldWidth)
                {
                    DropTridentAt(p.X, p.Room);        // หลุดขอบโลก: ตกเป็นไอเทมธรรมดา
                    _tridentProjectiles.RemoveAt(i);
                }
            }
        }

        // true = trident ถูกใช้ไปแล้ว (ปักบอส -> ตกเป็นไอเทมข้างตัวบอส) / false = บอสกัน trident เด้งกระเด็นออกไป ยังบินต่อ
        private bool HandleTridentHitBoss2(TridentProjectile p, Boss2 boss)
        {
            Vector2 bossHead = new Vector2(boss.X, _floorY + boss.YOffset - _boss2Height - 8f);

            if (boss.BlockCooldownTimer <= 0f)
            {
                // กันได้: เข้าคูลดาวน์ + เล่นท่ากัน (ถ้าบอสว่างอยู่) + trident เด้งกลับขึ้นฟ้า
                boss.BlockCooldownTimer = Boss2TridentBlockCooldown;
                boss.FacingRight = _kaiCenterX >= boss.X;

                if (_boss2Block != null && (boss.State == Boss2State.Idle || boss.State == Boss2State.Walking))
                {
                    boss.State = Boss2State.Blocking;
                    boss.AnimTimer = 0f;
                    boss.FrameIndex = 0;
                    PlaySwingSound();   // ป้องกัน: ใช้เสียงฟาดทั่วไป
                }

                _damagePopups.Add(new DamagePopup
                {
                    Text = "BLOCK",
                    Position = bossHead,
                    IsCrit = true,
                    Color = new Color(200, 200, 210)
                });

                float fromSide = p.VX >= 0f ? -1f : 1f;   // ด้านที่ trident บินมาถึง
                p.X = boss.X + fromSide * (_boss2HalfWidth + 12f);
                p.VX = fromSide * (float)(250.0 + _random.NextDouble() * 200.0);
                p.VY = -(float)(450.0 + _random.NextDouble() * 250.0);
                p.Spin = (float)(_random.NextDouble() * 2.0 - 1.0) * 14f;
                p.Falling = true;
                p.Deflected = true;
                return false;
            }

            // ช่วงคูลดาวน์: ปักโดนบอสเต็มๆ
            ApplyBoss2Hit(boss, _random.Next(TridentDamageMin, TridentDamageMax + 1), true);
            if (!boss.IsDying)
                StickTridentInBoss(p, boss);   // ปักค้างในตัวบอส เดินไปใกล้ๆ แล้วกด E ดึงออก
            else
                DropTridentAt(boss.X, boss.Room);
            return true;
        }

        private void UpdateBoss2Blocking(Boss2 boss)
        {
            boss.FacingRight = _kaiCenterX >= boss.X;

            int frame = System.Math.Min((int)(boss.AnimTimer / Boss2BlockFrameDuration), Boss2FrameCount - 1);
            boss.FrameIndex = frame;

            if (boss.AnimTimer >= Boss2FrameCount * Boss2BlockFrameDuration)
            {
                boss.State = Boss2State.Idle;
                boss.AnimTimer = 0f;
                boss.FrameIndex = 0;
                boss.AttackCooldownTimer = MathHelper.Max(boss.AttackCooldownTimer, 0.5f);
            }
        }

        private void DrawTridentSprite(Vector2 position, float angle, Color color)
        {
            if (_tridentTexture == null)
            {
                // ไม่มีภาพ: วาดเป็นเส้นสีทอง (ปลายอยู่ทางทิศ angle)
                _spriteBatch.Draw(_pixelTexture, position, null, new Color(220, 180, 60) * (color.A / 255f), angle,
                    new Vector2(0.5f, 0.5f), new Vector2(TridentDrawLength, 6f), SpriteEffects.None, 0f);
                return;
            }

            float scale = TridentDrawLength / (_tridentTexture.Height * TridentImageAxisLengthToHeight);
            float rotation = angle - MathHelper.ToRadians(TridentImageAxisAngleDegrees);
            var origin = new Vector2(_tridentTexture.Width * TridentImageCenterXRatio, _tridentTexture.Height * TridentImageCenterYRatio);
            _spriteBatch.Draw(_tridentTexture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
        }

        private void DrawTridentProjectiles()
        {
            foreach (var p in _tridentProjectiles)
            {
                if (p.Room != _currentRoomIndex)
                    continue;
                DrawTridentSprite(new Vector2(p.X, p.Y), p.Angle, Color.White);
            }
        }

        // ตอนง้าง: trident ถือเล็งไปทางเมาส์ + เส้นจุดบอกทิศ
        private void DrawTridentAim()
        {
            if (!_isAimingTrident)
                return;

            Vector2 center = GetKaiTridentCenter();
            var dir = new Vector2((float)System.Math.Cos(_tridentAimAngle), (float)System.Math.Sin(_tridentAimAngle));

            if (_kaiTridentFrames == null)   // มีชีทท่าง้างแล้ว trident อยู่ในมือในภาพอยู่แล้ว ไม่ต้องวาดซ้ำ
                DrawTridentSprite(center + dir * (TridentHandDistance * 0.5f), _tridentAimAngle, Color.White);

            int dots = (int)(TridentAimLineLength / TridentAimDotSpacing);
            for (int i = 1; i <= dots; i++)
            {
                float t = i / (float)dots;
                Vector2 pos = center + dir * (TridentHandDistance + i * TridentAimDotSpacing);
                var rect = new Rectangle((int)pos.X - 3, (int)pos.Y - 3, 6, 6);
                _spriteBatch.Draw(_pixelTexture, rect, new Color(255, 230, 120) * (0.85f * (1f - t * 0.7f)));
            }
        }

        // ===== trident ปักค้าง (พื้น / ตัวบอส) =====
        // ปักค้างเป็น WorldItem ชนิด Trident ที่มี IsStuck = true: เก็บคืนด้วย E ผ่านระบบเก็บของเดิม แต่วาดเป็นรูปปักค้างตามมุม
        // ปักตัวบอส: เกาะตามตัวบอสไปทุกที่ ถ้าบอสตายหรือหายไป จะหลุดกลายเป็นไอเทมบนพื้นธรรมดา
        private const float TridentGroundStuckDepth = 30f;    // ส่วนปลายที่จมลงพื้น (px)
        private const float TridentBossStuckDepth = 50f;      // ส่วนปลายที่จมในตัวบอส (px)
        private const float TridentMinGroundStuckAngleDegrees = 30f;   // ปักพื้นต้องเอียงลงอย่างน้อยกี่องศา (ไม่ให้นอนราบ)

        private static float ClampTridentGroundAngle(float angle)
        {
            float minSin = (float)System.Math.Sin(MathHelper.ToRadians(TridentMinGroundStuckAngleDegrees));
            float cos = (float)System.Math.Cos(angle);
            float sin = (float)System.Math.Sin(angle);
            if (sin >= minSin)
                return angle;

            float minAngle = MathHelper.ToRadians(TridentMinGroundStuckAngleDegrees);
            return cos >= 0f ? minAngle : MathHelper.Pi - minAngle;
        }

        private Vector2 GetTridentTip(TridentProjectile p)
        {
            float half = TridentDrawLength * 0.5f;
            return new Vector2(p.X + (float)System.Math.Cos(p.Angle) * half, p.Y + (float)System.Math.Sin(p.Angle) * half);
        }

        // ปักที่จุดแน่นอนในโลก (พื้น / ผิวแผ่นสูง / ด้านข้างแผ่นสูง): ปลายอยู่ที่ (tipX, tipY)
        private void StickTridentAtPoint(TridentProjectile p, float tipX, float tipY, float angle)
        {
            _worldItems.Add(new WorldItem
            {
                Type = ItemType.Trident,
                Count = 1,
                X = MathHelper.Clamp(tipX, 10f, _worldWidth - 10f),
                Room = p.Room,
                IsStuck = true,
                StuckAngle = angle,
                StuckDepth = TridentGroundStuckDepth,
                StuckTipY = tipY
            });
        }

        // ชนของแข็งหรือยัง: แผ่นสูง (ก้อนตันตั้งแต่ผิวบนถึงพื้น) หรือพื้นเดิม ใช้ "ปลาย" trident เป็นตัวชน
        // oldTip = ตำแหน่งปลายก่อนขยับเฟรมนี้ ใช้ดูว่าเข้ามาทางผิวบนหรือด้านข้าง
        private bool TryStickTridentOnSolid(TridentProjectile p, Vector2 oldTip)
        {
            Vector2 tip = GetTridentTip(p);

            foreach (var plat in _platforms)
            {
                if (plat.Room != p.Room)
                    continue;

                float left = plat.X;
                float right = plat.X + plat.Width;
                if (tip.X < left || tip.X > right || tip.Y < plat.Top || tip.Y > plat.Bottom)
                    continue;

                // เข้ามาทางไหน: เทียบความลึกที่ทะลุผิวบน กับด้านซ้าย/ขวา เอาผิวที่ทะลุน้อยกว่า (เพิ่งข้ามมา)
                float penTop = oldTip.Y < plat.Top ? tip.Y - plat.Top : float.MaxValue;
                float penSide = float.MaxValue;
                bool fromLeft = false;
                if (oldTip.X < left)
                {
                    penSide = tip.X - left;
                    fromLeft = true;
                }
                else if (oldTip.X > right)
                {
                    penSide = right - tip.X;
                }

                if (penSide < penTop)
                {
                    // ปักด้านข้างแผ่น: คงมุมจริง
                    StickTridentAtPoint(p, fromLeft ? left : right, tip.Y, p.Angle);
                }
                else
                {
                    StickTridentAtPoint(p, tip.X, plat.Top, ClampTridentGroundAngle(p.Angle));
                }
                return true;
            }

            if (tip.Y >= _floorY)
            {
                StickTridentAtPoint(p, tip.X, _floorY, ClampTridentGroundAngle(p.Angle));
                return true;
            }

            return false;
        }

        private void StickTridentInBoss(TridentProjectile p, Boss2 boss)
        {
            float half = TridentDrawLength * 0.5f;
            float tipX = p.X + (float)System.Math.Cos(p.Angle) * half;
            float tipY = p.Y + (float)System.Math.Sin(p.Angle) * half;

            Rectangle rect = GetBoss2Rect(boss);
            tipX = MathHelper.Clamp(tipX, rect.Left + 10f, rect.Right - 10f);
            tipY = MathHelper.Clamp(tipY, rect.Top + 10f, rect.Bottom - 10f);

            _worldItems.Add(new WorldItem
            {
                Type = ItemType.Trident,
                Count = 1,
                X = tipX,
                Room = boss.Room,
                IsStuck = true,
                StuckAngle = p.Angle,
                StuckDepth = TridentBossStuckDepth,
                StuckHost = boss,
                StuckOffsetX = tipX - boss.X,
                StuckOffsetY = tipY - (_floorY + boss.YOffset)
            });
        }

        private void UpdateStuckTridents()
        {
            foreach (var item in _worldItems)
            {
                if (item.Collected || !item.IsStuck || item.StuckHost == null)
                    continue;

                var host = item.StuckHost;
                if (host.IsDying || !_boss2List.Contains(host))
                {
                    // บอสตาย/หายไป: trident หลุดตกเป็นไอเทมบนพื้นธรรมดา
                    item.X = MathHelper.Clamp(host.X + item.StuckOffsetX, 30f, _worldWidth - 30f);
                    item.IsStuck = false;
                    item.StuckHost = null;
                    continue;
                }

                item.X = host.X + item.StuckOffsetX;
                item.Room = host.Room;
            }
        }

        private void DrawStuckTrident(WorldItem item)
        {
            Vector2 tip;
            if (item.StuckHost != null)
                tip = new Vector2(item.StuckHost.X + item.StuckOffsetX, _floorY + item.StuckHost.YOffset + item.StuckOffsetY);
            else
                tip = new Vector2(item.X, item.StuckTipY);

            var dir = new Vector2((float)System.Math.Cos(item.StuckAngle), (float)System.Math.Sin(item.StuckAngle));
            Vector2 center = tip - dir * (TridentDrawLength * 0.5f - item.StuckDepth);   // ปลายจมอยู่ที่จุดปัก
            DrawTridentSprite(center, item.StuckAngle, Color.White);

            if (IsWorldItemInPickupRange(item) && !_isConsoleOpen && !_isGiveItemMenuOpen && !_isWakeUpSequence && !_isOrbScene && !_isBossScene && !_isBoss2Scene && !_isBoss3Scene)
            {
                string prompt = "Press E";
                const float promptScale = 0.7f;
                Vector2 promptSize = _hotbarFont.MeasureString(prompt) * promptScale;
                var promptPosition = new Vector2(
                    item.X - promptSize.X / 2f,
                    MathHelper.Min(tip.Y, center.Y) - promptSize.Y - 6f);
                _spriteBatch.DrawString(_hotbarFont, prompt, promptPosition, Color.White, 0f, Vector2.Zero, promptScale, SpriteEffects.None, 0f);
            }
        }

        // =====================================================================
        // ด่าน 2: หินตัวเลข + ป้ายใบ้ + ประตูรหัส
        // =====================================================================

        private void LoadStoneContent()
        {
            try
            {
                var sheet = Content.Load<Texture2D>("stone_spritesheet");
                int cellWidth = sheet.Width / StoneSheetColumns;
                int cellHeight = sheet.Height / StoneSheetRows;

                var frames = new AnimFrame[StoneCount];
                float heightSum = 0f;
                for (int i = 0; i < StoneCount; i++)
                {
                    var cell = new Rectangle((i % StoneSheetColumns) * cellWidth, (i / StoneSheetColumns) * cellHeight, cellWidth, cellHeight);
                    var trim = GetOpaqueBounds(sheet, cell);
                    frames[i] = new AnimFrame { Texture = sheet, Trim = trim, OriginX = trim.Width / 2f, ClampHalfWidth = trim.Width / 2f };
                    heightSum += trim.Height;
                }

                _stoneScale = StoneTargetHeight / (heightSum / StoneCount);   // ขนาดเทียบกันตามภาพจริง (หินแต่ละก้อนสูงไม่เท่ากันนิดหน่อย)
                _stoneFrames = frames;
            }
            catch (System.Exception)
            {
                _stoneFrames = null;
            }
        }

        private static string GetOrdinal(int n)
        {
            if (n % 100 >= 11 && n % 100 <= 13)
                return n + "th";
            switch (n % 10)
            {
                case 1: return n + "st";
                case 2: return n + "nd";
                case 3: return n + "rd";
                default: return n + "th";
            }
        }

        // สุ่มหิน/รหัสครั้งเดียวต่อเกมใหม่ (เรียกเมื่อยังไม่มีข้อมูล ResetGameState เป็นตัวล้างให้สุ่มใหม่)
        private void EnsureStonePuzzle()
        {
            if (_codeStones.Count > 0)
                return;

            // สลับเลข 0-9 แล้วแบ่งห้องละ 5 ก้อน
            var digits = new List<int>();
            for (int d = 0; d < StoneCount; d++)
                digits.Add(d);
            for (int i = digits.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                int tmp = digits[i]; digits[i] = digits[j]; digits[j] = tmp;
            }

            float maxX = _worldWidth - StoneEdgeMarginRight;
            for (int room = 0; room < 2; room++)
            {
                var xs = new List<float>();
                for (int attempt = 0; attempt < 400 && xs.Count < StonesPerRoom; attempt++)
                {
                    float x = StoneMinX + (float)_random.NextDouble() * (maxX - StoneMinX);
                    bool tooClose = false;
                    foreach (float other in xs)
                    {
                        if (System.Math.Abs(other - x) < StoneMinSpacing)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (!tooClose)
                        xs.Add(x);
                }

                // กันกรณีสุ่มไม่ลงตัว (ห้องแคบเกินไป): เติมให้เว้นระยะเท่าๆ กัน
                while (xs.Count < StonesPerRoom)
                    xs.Add(StoneMinX + (maxX - StoneMinX) * (xs.Count + 0.5f) / StonesPerRoom);

                xs.Sort();
                for (int k = 0; k < StonesPerRoom; k++)
                {
                    _codeStones.Add(new CodeStone
                    {
                        Digit = digits[room * StonesPerRoom + k],
                        Room = room == 0 ? StoneRoomA : StoneRoomB,
                        X = xs[k],
                        Order = room * StonesPerRoom + k + 1
                    });
                }
            }

            // รหัส 4 หลัก = เลขบนหิน 4 ก้อนที่สุ่มเลือก (เรียงตามลำดับที่สุ่ม)
            var orders = new List<int>();
            for (int o = 1; o <= StoneCount; o++)
                orders.Add(o);
            for (int i = orders.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                int tmp = orders[i]; orders[i] = orders[j]; orders[j] = tmp;
            }

            var code = new System.Text.StringBuilder();
            var ordinals = new List<string>();
            for (int k = 0; k < PasscodeLength; k++)
            {
                int order = orders[k];
                foreach (var stone in _codeStones)
                {
                    if (stone.Order == order)
                        code.Append(stone.Digit);
                }
                ordinals.Add(GetOrdinal(order));
            }

            _stoneCode = code.ToString();
            string list = string.Join(", ", ordinals.GetRange(0, ordinals.Count - 1)) + " and " + ordinals[ordinals.Count - 1];
            _stoneHintLines = new[]
            {
                "Note|The door ahead needs a four digit code. It is carved into the mossy stones in these two rooms.",
                "Note|The code is the number on the " + list + " stone, in that order. Count the stones from the far left of the first room, going right, then keep counting into the second room."
            };
        }

        private float GetStoneDoorX()
        {
            return _worldWidth - StoneDoorOffsetFromRight;
        }

        private bool IsKaiNearStoneDoor()
        {
            if (_currentRoomIndex != StoneRoomB)
                return false;

            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            return System.Math.Abs(GetStoneDoorX() - _kaiCenterX) <= DoorInteractRange
                && System.Math.Abs(kaiFeetY - _floorY) <= _kaiTargetHeight * 0.75f;
        }

        // =====================================================================
        // ด่าน 2: Monster 2 เป็นเวฟ (ห้องที่ 1 และ 2 ของด่าน 2 แต่ละห้องมีชุดของตัวเอง)
        //  4 เวฟ เวฟละ 2 ตัว เกิดทีละตัว: ตัวแรกเกิดก่อน ตายแล้วตัวที่สองถึงเกิด ตัวที่สองตายคือจบเวฟ แล้วเวฟถัดไป (รวม 8 ตัวต่อห้อง)
        //  เริ่มเมื่อผู้เล่นเข้าห้อง ตำแหน่งเกิดสุ่มในห้องห่างผู้เล่น 500-900 px ตัวที่เกิดจะไล่ผู้เล่นเสมอ
        //  ผู้เล่นตายแล้วเกิดใหม่: เวฟคงความคืบหน้าไว้ แต่เวฟที่กำลังเล่นอยู่เริ่มใหม่จากตัวแรกของเวฟนั้น
        // =====================================================================
        private const int Stage2WaveCount = 4;
        private const int Stage2MonstersPerWave = 2;
        private const float Stage2WaveSpawnDelay = 1.2f;        // หน่วงก่อนตัวถัดไปเกิดหลังตัวก่อนหน้าตาย
        private const float Stage2WaveFirstSpawnDelay = 1.0f;   // หน่วงก่อนตัวแรกเกิดหลังเข้าห้อง
        private const float Stage2WaveSpawnMinDistance = 500f;
        private const float Stage2WaveSpawnMaxDistance = 900f;

        private sealed class Stage2WaveState
        {
            public int Room;
            public int Wave;              // เวฟที่กำลังเล่น (0-3)
            public int KillsInWave;       // ตัวที่ตายแล้วในเวฟนี้ (0-1)
            public bool Done;             // ครบ 4 เวฟแล้ว
            public float SpawnDelay;
            public Monster2 Current;
        }

        private readonly Stage2WaveState[] _stage2Waves =
        {
            new Stage2WaveState { Room = StoneRoomA, SpawnDelay = Stage2WaveFirstSpawnDelay },
            new Stage2WaveState { Room = StoneRoomB, SpawnDelay = Stage2WaveFirstSpawnDelay }
        };

        private bool IsKaiNearStoneSign()
        {
            if (_currentRoomIndex != StoneRoomA)
                return false;

            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            return System.Math.Abs(StoneSignX - _kaiCenterX) <= DoorInteractRange
                && System.Math.Abs(kaiFeetY - _floorY) <= _kaiTargetHeight * 0.75f;
        }

        // กด E ใกล้ป้าย = อ่านใบ้ / ใกล้ประตูรหัสที่ยังล็อก = เปิดหน้าต่างกรอกรหัส
        private void UpdateStonePuzzleInput()
        {
            if (_currentRoomIndex != StoneRoomA && _currentRoomIndex != StoneRoomB)
                return;
            if (_isConsoleOpen || _isTransitioning || _isGiveItemMenuOpen || _isInventoryOpen)
                return;
            if (_isJumping || _isLanding || _isAttacking)
                return;

            var keyboard = Keyboard.GetState();
            if (!IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.E))
                return;

            // ถ้ามีไอเทมบนพื้นให้เก็บอยู่ใกล้ตัว ให้การเก็บของมาก่อน
            foreach (var worldItem in _worldItems)
            {
                if (!worldItem.Collected && worldItem.Room == _currentRoomIndex && IsWorldItemInPickupRange(worldItem))
                    return;
            }

            if (IsKaiNearStoneSign() && _stoneHintLines != null)
            {
                LockKaiForPuzzleUi(StoneSignX);
                _isSignReading = true;
                StartDialogue(_stoneHintLines, "Note");
            }
            else if (IsKaiNearStoneDoor())
            {
                if (_passcodeDoorOpen)
                {
                    // ประตูเปิดแล้ว: กด E เข้าห้องที่ 3 (เปลี่ยนห้องแบบประตูเหมือนด่าน 1)
                    StartDoorTransition(Level2LastRoomIndex, Stage2Room3EntryX, true);
                }
                else
                {
                    LockKaiForPuzzleUi(GetStoneDoorX());
                    _isPasscodeOpen = true;
                    _passcodeInput = "";
                    _passcodeMessage = "";
                    _passcodeMessageTimer = 0f;
                }
            }
        }

        private void LockKaiForPuzzleUi(float faceX)
        {
            _isMoving = false;
            _isSprinting = false;
            _isAttacking = false;
            _attackFrameIndex = 0;
            _currentFrame = 0;
            _frameTimer = 0f;
            _facingRight = faceX >= _kaiCenterX;
        }

        private void UpdateSignReading(GameTime gameTime, bool click)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var keyboard = Keyboard.GetState();

            if (IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.Escape) || UpdateDialogue(dt, click))
            {
                _isSignReading = false;
                _isDialogueActive = false;
            }
        }

        private int GetPressedDigit(KeyboardState keyboard, KeyboardState previous)
        {
            for (int d = 0; d <= 9; d++)
            {
                Keys top = Keys.D0 + d;
                Keys pad = Keys.NumPad0 + d;
                if (IsKeyJustPressed(keyboard, previous, top) || IsKeyJustPressed(keyboard, previous, pad))
                    return d;
            }
            return -1;
        }

        private void UpdatePasscode(GameTime gameTime, bool click)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_passcodeMessageTimer > 0f)
                _passcodeMessageTimer = MathHelper.Max(0f, _passcodeMessageTimer - dt);

            var keyboard = Keyboard.GetState();

            if (IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.Escape))
            {
                _isPasscodeOpen = false;
                return;
            }

            int digit = GetPressedDigit(keyboard, _previousItemKeyboardState);
            if (digit >= 0 && _passcodeInput.Length < PasscodeLength)
            {
                _passcodeInput += digit.ToString(System.Globalization.CultureInfo.InvariantCulture);
                _passcodeMessageTimer = 0f;
            }

            if (IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.Back) && _passcodeInput.Length > 0)
            {
                _passcodeInput = _passcodeInput.Substring(0, _passcodeInput.Length - 1);
                _passcodeMessageTimer = 0f;
            }

            if (IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.Enter))
            {
                if (_passcodeInput.Length < PasscodeLength)
                {
                    _passcodeMessage = "Enter all " + PasscodeLength + " digits";
                    _passcodeMessageIsError = true;
                    _passcodeMessageTimer = PasscodeMessageDuration;
                }
                else if (_passcodeInput == _stoneCode)
                {
                    _passcodeDoorOpen = true;
                    _isPasscodeOpen = false;
                    PlayDoorOpenSound();
                    _damagePopups.Add(new DamagePopup
                    {
                        Text = "Door unlocked",
                        Position = new Vector2(_kaiCenterX, _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 10f),
                        Color = new Color(120, 255, 160),
                        IsCrit = true
                    });
                    ShowConsoleMessage("Door unlocked", false);
                }
                else
                {
                    _passcodeInput = "";
                    _passcodeMessage = "Wrong code. Try again.";
                    _passcodeMessageIsError = true;
                    _passcodeMessageTimer = PasscodeMessageDuration;
                }
            }
        }

        // หินตัวเลข + ป้ายใบ้ + ประตูรหัส (วาดในโลก)
        private void DrawStoneRoomProps()
        {
            if (_currentRoomIndex != StoneRoomA && _currentRoomIndex != StoneRoomB)
                return;

            if (_stoneFrames != null)
            {
                foreach (var stone in _codeStones)
                {
                    if (stone.Room != _currentRoomIndex)
                        continue;

                    var frame = _stoneFrames[stone.Digit];
                    var origin = new Vector2(frame.Trim.Width / 2f, frame.Trim.Height);
                    _spriteBatch.Draw(frame.Texture, new Vector2(stone.X, _floorY + 4f), frame.Trim, Color.White, 0f, origin, _stoneScale, SpriteEffects.None, 0f);
                }
            }

            if (_currentRoomIndex == StoneRoomA)
            {
                // ป้ายไม้ (วาดจากสี่เหลี่ยม): เสา + แผ่นป้าย + เส้นข้อความ
                float baseY = _floorY;
                DrawFilledRect(new Rectangle((int)StoneSignX - 4, (int)(baseY - 70f), 8, 70), new Color(88, 62, 40));
                var board = new Rectangle((int)StoneSignX - 34, (int)(baseY - 118f), 68, 50);
                DrawFilledRect(board, new Color(150, 112, 70));
                DrawRectOutline(board, 2, new Color(84, 58, 34));
                for (int k = 0; k < 3; k++)
                    DrawFilledRect(new Rectangle(board.X + 10, board.Y + 12 + k * 10, 48 - (k == 2 ? 14 : 0), 3), new Color(70, 48, 28));

                if (IsKaiNearStoneSign() && !_isSignReading)
                    DrawWorldPrompt("Press E to read", StoneSignX, baseY - 128f);
            }
            else
            {
                float doorX = GetStoneDoorX();
                float height = GetDoorHeight();
                var rect = new Rectangle((int)(doorX - DoorWidth / 2f), (int)(_floorY - height), (int)DoorWidth, (int)height);

                DrawFilledRect(new Rectangle(rect.X - 6, rect.Y - 6, rect.Width + 12, rect.Height + 6), new Color(70, 55, 40));
                if (_passcodeDoorOpen)
                {
                    DrawFilledRect(rect, new Color(8, 8, 12));
                }
                else
                {
                    DrawFilledRect(rect, new Color(96, 96, 104));
                    DrawRectOutline(rect, 3, new Color(60, 60, 68));
                    // แผงตัวเลขเล็กๆ ข้างประตู
                    var pad = new Rectangle(rect.Right - 30, rect.Y + rect.Height / 2 - 14, 18, 28);
                    DrawFilledRect(pad, new Color(30, 34, 40));
                    for (int r = 0; r < 3; r++)
                        for (int cc = 0; cc < 2; cc++)
                            DrawFilledRect(new Rectangle(pad.X + 3 + cc * 7, pad.Y + 3 + r * 8, 5, 5), new Color(220, 200, 90));
                }

                if (IsKaiNearStoneDoor() && !_isPasscodeOpen && !_isTransitioning)
                    DrawWorldPrompt(_passcodeDoorOpen ? "Press E to enter" : "Press E to enter code", doorX, rect.Y - 12f);
            }
        }

        private void DrawWorldPrompt(string prompt, float centerX, float bottomY)
        {
            const float promptScale = 0.7f;
            Vector2 size = _hotbarFont.MeasureString(prompt) * promptScale;
            var position = new Vector2(centerX - size.X / 2f, bottomY - size.Y);
            _spriteBatch.DrawString(_hotbarFont, prompt, position, Color.White, 0f, Vector2.Zero, promptScale, SpriteEffects.None, 0f);
        }

        // หน้าต่างกรอกรหัส (วาดใน HUD)
        private void DrawPasscodeUI()
        {
            if (!_isPasscodeOpen)
                return;

            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, WindowedHeight), new Color(0, 0, 0, 150));

            var panel = new Rectangle(WindowedWidth / 2 - 270, WindowedHeight / 2 - 160, 540, 320);
            DrawFilledRect(panel, new Color(22, 26, 32, 245));
            DrawRectOutline(panel, 3, new Color(190, 170, 110));

            string title = "Enter the 4 digit code";
            float titleScale = 1.0f;
            Vector2 titleSize = _hotbarFont.MeasureString(title) * titleScale;
            _spriteBatch.DrawString(_hotbarFont, title, new Vector2(panel.X + (panel.Width - titleSize.X) / 2f, panel.Y + 22), new Color(255, 232, 150), 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

            const int boxW = 74, boxH = 92, gap = 18;
            int totalW = PasscodeLength * boxW + (PasscodeLength - 1) * gap;
            int startX = panel.X + (panel.Width - totalW) / 2;
            int boxY = panel.Y + 86;
            for (int i = 0; i < PasscodeLength; i++)
            {
                var box = new Rectangle(startX + i * (boxW + gap), boxY, boxW, boxH);
                DrawFilledRect(box, new Color(10, 12, 16));
                DrawRectOutline(box, 2, i == _passcodeInput.Length ? new Color(255, 232, 150) : new Color(110, 110, 120));

                if (i < _passcodeInput.Length)
                {
                    string ch = _passcodeInput[i].ToString();
                    float digitScale = 2.0f;
                    Vector2 chSize = _hotbarFont.MeasureString(ch) * digitScale;
                    _spriteBatch.DrawString(_hotbarFont, ch, new Vector2(box.X + (box.Width - chSize.X) / 2f, box.Y + (box.Height - chSize.Y) / 2f), Color.White, 0f, Vector2.Zero, digitScale, SpriteEffects.None, 0f);
                }
            }

            if (_passcodeMessageTimer > 0f && !string.IsNullOrEmpty(_passcodeMessage))
            {
                float msgScale = 0.85f;
                Vector2 msgSize = _hotbarFont.MeasureString(_passcodeMessage) * msgScale;
                Color msgColor = _passcodeMessageIsError ? new Color(255, 120, 120) : new Color(130, 255, 170);
                _spriteBatch.DrawString(_hotbarFont, _passcodeMessage, new Vector2(panel.X + (panel.Width - msgSize.X) / 2f, boxY + boxH + 18), msgColor, 0f, Vector2.Zero, msgScale, SpriteEffects.None, 0f);
            }

            string help = "0-9 type   Backspace delete   Enter confirm   Esc cancel";
            float helpScale = 0.6f;
            Vector2 helpSize = _hotbarFont.MeasureString(help) * helpScale;
            _spriteBatch.DrawString(_hotbarFont, help, new Vector2(panel.X + (panel.Width - helpSize.X) / 2f, panel.Bottom - helpSize.Y - 18), new Color(180, 180, 190), 0f, Vector2.Zero, helpScale, SpriteEffects.None, 0f);
        }

        // full = เริ่มเกมใหม่ (ล้างทุกอย่าง) / ไม่ full = ผู้เล่นตายแล้วเกิดใหม่ (เวฟคงความคืบหน้า เวฟที่เล่นอยู่เริ่มใหม่จากตัวแรก)
        private void ResetStage2Waves(bool full)
        {
            foreach (var state in _stage2Waves)
            {
                if (state.Current != null)
                {
                    _monster2List.Remove(state.Current);
                    state.Current = null;
                }

                state.KillsInWave = 0;
                state.SpawnDelay = Stage2WaveFirstSpawnDelay;

                if (full)
                {
                    state.Wave = 0;
                    state.Done = false;
                }
            }
        }

        private void UpdateStage2Waves(GameTime gameTime)
        {
            if (!_monster2Loaded)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (var state in _stage2Waves)
            {
                if (state.Room != _currentRoomIndex || state.Done)
                    continue;

                if (state.Current != null)
                {
                    if (!state.Current.IsDying && _monster2List.Contains(state.Current))
                        continue;   // ตัวปัจจุบันยังไม่ตาย

                    state.Current = null;
                    state.KillsInWave++;
                    state.SpawnDelay = Stage2WaveSpawnDelay;

                    if (state.KillsInWave >= Stage2MonstersPerWave)
                    {
                        state.KillsInWave = 0;
                        state.Wave++;
                        if (state.Wave >= Stage2WaveCount)
                        {
                            state.Done = true;
                            continue;
                        }
                    }
                }

                if (_currentHP <= 0f)
                    continue;

                if (state.SpawnDelay > 0f)
                {
                    state.SpawnDelay -= dt;
                    continue;
                }

                state.Current = SpawnStage2WaveMonster(state.Room);
            }
        }

        // เกิดที่ตำแหน่งสุ่มในห้อง ห่างผู้เล่น 500-900 px (สุ่มซ้าย/ขวา ถ้าด้านนั้นเกินขอบห้องใช้อีกด้าน) ไล่ผู้เล่นเสมอ
        private Monster2 SpawnStage2WaveMonster(int room)
        {
            float minX = _monster2HalfWidth + 40f;
            float maxX = _worldWidth - _monster2HalfWidth - 40f;
            float distance = Stage2WaveSpawnMinDistance + (float)_random.NextDouble() * (Stage2WaveSpawnMaxDistance - Stage2WaveSpawnMinDistance);
            float side = _random.Next(2) == 0 ? -1f : 1f;

            float x = _kaiCenterX + side * distance;
            if (x < minX || x > maxX)
                x = _kaiCenterX - side * distance;   // ด้านที่สุ่มได้เกินขอบห้อง ใช้อีกด้านแทน
            x = MathHelper.Clamp(x, minX, maxX);

            var monster = new Monster2
            {
                X = x,
                SpawnX = x,
                Room = room,
                HP = Monster2MaxHP,
                State = Monster2State.Idle,
                AlwaysAggro = true,
                IsAggro = true,
                FacingRight = _kaiCenterX >= x,
                YOffset = GetSurfaceYAt(room, x, _monster2HalfWidth) - _floorY
            };
            _monster2List.Add(monster);
            return monster;
        }

        // =====================================================================
        // ด่าน 2 ห้องที่ 3: ฉากเจอ Boss 2 (ดูค่าคงที่ Boss2Scene* / BossScene* ด้านบน)
        // =====================================================================

        // บอสยังมีชีวิตและอยู่ในห้องที่ 3: ขอบซ้ายของห้องเป็นกำแพง (ออกไปห้องที่ 2 ไม่ได้) จนกว่าบอสจะตาย
        private bool IsBoss2ArenaLocked()
        {
            return _currentRoomIndex == Level2LastRoomIndex
                && _boss2SceneBoss != null
                && !_boss2SceneBoss.IsDying
                && _boss2List.Contains(_boss2SceneBoss);
        }

        // เข้าห้องที่ 3 ครั้งแรก (หลังเปลี่ยนห้องเสร็จ): บอสเกิดฝั่งขวาแบบยืนนิ่ง แล้วเริ่มฉากทันที
        private void UpdateBoss2Encounter()
        {
            if (!_boss2Loaded || _boss2SceneDone || _isBoss2Scene || _isBoss3Scene || _currentRoomIndex != Level2LastRoomIndex)
                return;
            if (_isTransitioning || _isWakeUpSequence || _isOrbScene || _isBossScene || _isDoorInteraction || _isPasscodeOpen || _isSignReading)
                return;
            if (_currentHP <= 0f)
                return;

            // กลับมาสู้ต่อหลังตาย: บอสเลือด/เฟส/ตำแหน่งตามที่จดไว้ ข้ามฉากเปิดตัวไปสู้เลย
            if (_boss2HasSavedState)
            {
                float sx = MathHelper.Clamp(_boss2SavedX, 0f, _worldWidth);
                _boss2SceneBoss = new Boss2
                {
                    X = sx,
                    SpawnX = Boss2SceneBossX,
                    Room = Level2LastRoomIndex,
                    HP = MathHelper.Clamp(_boss2SavedHP, 1f, Boss2MaxHP),
                    State = Boss2State.Walking,
                    FacingRight = _boss2SavedFacingRight,
                    IsStatue = false,
                    IsAggro = true,
                    IsPhase2 = _boss2SavedIsPhase2 || _boss2SavedHP < Boss2MaxHP * Boss2Phase2HpRatio,
                    AttackCooldownTimer = 1f,
                    ThrowCooldownTimer = 2f,
                    YOffset = GetSurfaceYAt(Level2LastRoomIndex, sx, _boss2HalfWidth) - _floorY
                };
                _boss2List.Add(_boss2SceneBoss);
                _boss2SceneDone = true;
                _boss2HasSavedState = false;
                return;
            }

            float x = Boss2SceneBossX;
            _boss2SceneBoss = new Boss2
            {
                X = x,
                SpawnX = x,
                Room = Level2LastRoomIndex,
                HP = Boss2MaxHP,
                State = Boss2State.Idle,
                FacingRight = false,   // หันซ้ายเข้าหาทางเข้าห้อง (Kai เข้ามาจากซ้าย)
                IsStatue = true,
                YOffset = GetSurfaceYAt(Level2LastRoomIndex, x, _boss2HalfWidth) - _floorY
            };
            _boss2List.Add(_boss2SceneBoss);

            StartBoss2Scene();
        }

        private void StartBoss2Scene()
        {
            _isBoss2Scene = true;
            _boss2SceneDone = true;
            _boss2Stage = BossSceneStage.Dialogue1;
            _boss2StageTimer = 0f;
            _boss2FocusBlend = 0f;
            _boss2SceneZoom = CameraZoom;
            _boss2ShakeActive = false;
            _boss2ShakeTime = 0f;

            // ล็อก Kai ให้ยืนนิ่งหันหน้าเข้าหาบอส
            _isMoving = false;
            _isSprinting = false;
            _isAttacking = false;
            _attackFrameIndex = 0;
            _currentFrame = 0;
            _frameTimer = 0f;
            _facingRight = _boss2SceneBoss.X >= _kaiCenterX;

            if (!_isDraggingItem)
                _isInventoryOpen = false;

            StartDialogue(Boss2Dialogue1Lines, "Brother");
        }

        private void UpdateBoss2Scene(GameTime gameTime, bool click)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_boss2ShakeActive)
                _boss2ShakeTime += dt;

            switch (_boss2Stage)
            {
                case BossSceneStage.Dialogue1:
                    _boss2FocusBlend = 0f;
                    _boss2SceneZoom = CameraZoom;
                    if (UpdateDialogue(dt, click))
                    {
                        _boss2Stage = BossSceneStage.CameraToBoss;
                        _boss2StageTimer = 0f;
                    }
                    break;

                case BossSceneStage.CameraToBoss:
                    _boss2StageTimer += dt;
                    {
                        float t = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_boss2StageTimer / BossSceneCameraPanDuration, 0f, 1f));
                        _boss2FocusBlend = t;
                        _boss2SceneZoom = MathHelper.Lerp(CameraZoom, BossSceneBossZoom, t);
                    }
                    if (_boss2StageTimer >= BossSceneCameraPanDuration)
                    {
                        _boss2FocusBlend = 1f;
                        _boss2SceneZoom = BossSceneBossZoom;
                        _boss2ShakeActive = true;
                        _boss2ShakeTime = 0f;
                        _boss2Stage = BossSceneStage.Shake;
                        _boss2StageTimer = 0f;
                    }
                    break;

                case BossSceneStage.Shake:
                    _boss2StageTimer += dt;
                    _boss2FocusBlend = 1f;
                    _boss2SceneZoom = BossSceneBossZoom;
                    if (_boss2StageTimer >= BossSceneShakeDuration)
                    {
                        StartDialogue(Boss2Dialogue2Lines, "William Jones");
                        _boss2Stage = BossSceneStage.Dialogue2;
                    }
                    break;

                case BossSceneStage.Dialogue2:
                    _boss2FocusBlend = 1f;
                    _boss2SceneZoom = BossSceneBossZoom;
                    if (UpdateDialogue(dt, click))
                    {
                        _boss2ShakeActive = false;
                        _boss2Stage = BossSceneStage.ZoomOut;
                        _boss2StageTimer = 0f;
                    }
                    break;

                case BossSceneStage.ZoomOut:
                    _boss2StageTimer += dt;
                    {
                        float t = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_boss2StageTimer / BossSceneZoomOutDuration, 0f, 1f));
                        _boss2FocusBlend = 1f - t;
                        _boss2SceneZoom = MathHelper.Lerp(BossSceneBossZoom, BossFightZoom, t);
                    }
                    if (_boss2StageTimer >= BossSceneZoomOutDuration)
                    {
                        EndBoss2Scene();
                        return;
                    }
                    break;
            }

            _cameraZoom = _boss2SceneZoom;
        }

        private void EndBoss2Scene()
        {
            _isBoss2Scene = false;
            _isDialogueActive = false;
            _boss2ShakeActive = false;
            _boss2FocusBlend = 0f;

            // เริ่มต่อสู้: กล้องค้างที่ซูมออกจนกว่าบอสจะตาย
            _boss2FightZoomBlend = 1f;
            _cameraZoom = BossFightZoom;

            // ปลุกบอสให้ใช้ AI ปกติ: ไล่โจมตี Kai (หน่วงการโจมตีครั้งแรกเล็กน้อย)
            if (_boss2SceneBoss != null)
            {
                _boss2SceneBoss.IsStatue = false;
                _boss2SceneBoss.IsAggro = true;
                _boss2SceneBoss.State = Boss2State.Walking;
                _boss2SceneBoss.AnimTimer = 0f;
                _boss2SceneBoss.FrameIndex = 0;
                _boss2SceneBoss.AttackCooldownTimer = 1f;
                _boss2SceneBoss.ThrowCooldownTimer = 2f;
            }

            UpdateCamera();
        }

        // ซูมกล้องตอนสู้บอส 2: ซูมออกเมื่อกำลังสู้ในห้องที่ 3 / กลับมาซูมปกติเมื่อบอสตายหรือออกจากห้อง
        private void UpdateBoss2FightZoom(float dt)
        {
            float previousBlend = _boss2FightZoomBlend;
            float target = IsBoss2ArenaLocked() ? 1f : 0f;
            float step = dt / BossFightZoomBlendDuration;

            if (_boss2FightZoomBlend < target)
                _boss2FightZoomBlend = MathHelper.Min(target, _boss2FightZoomBlend + step);
            else if (_boss2FightZoomBlend > target)
                _boss2FightZoomBlend = MathHelper.Max(target, _boss2FightZoomBlend - step);

            if (_boss2FightZoomBlend > 0f || previousBlend > 0f)
                _cameraZoom = MathHelper.Lerp(CameraZoom, BossFightZoom, MathHelper.SmoothStep(0f, 1f, _boss2FightZoomBlend));
        }

        // ผู้เล่นตายแล้วเกิดใหม่: ถ้าบอสยังไม่ตาย ลบบอสออกแล้วจดเลือด/ตำแหน่ง/เฟสไว้ ตอนกลับมาเข้าห้องจะสร้างบอสใหม่ด้วยค่าที่จดไว้ (ข้ามฉากเปิดตัว)
        private void ResetBoss2EncounterOnRespawn()
        {
            if (_boss2SceneBoss != null && !_boss2SceneBoss.IsDying && _boss2List.Contains(_boss2SceneBoss))
            {
                // ตายหลังฉากเปิดตัวจบ (บอสเริ่มสู้แล้ว) = จดเลือด/ตำแหน่ง/เฟสไว้ ให้กลับมาสู้ต่อจากเดิม
                if (!_boss2SceneBoss.IsStatue)
                {
                    _boss2HasSavedState = true;
                    _boss2SavedHP = _boss2SceneBoss.HP;
                    _boss2SavedX = _boss2SceneBoss.X;
                    _boss2SavedFacingRight = _boss2SceneBoss.FacingRight;
                    _boss2SavedIsPhase2 = _boss2SceneBoss.IsPhase2;
                }

                _boss2List.Remove(_boss2SceneBoss);
                _boss2SceneBoss = null;
                _boss2SceneDone = false;
            }

            _isBoss2Scene = false;
            _boss2ShakeActive = false;
            _boss2FocusBlend = 0f;
            _boss2FightZoomBlend = 0f;
        }

        // เริ่มเกมใหม่: ล้างสถานะฉากบอส 2 ทั้งหมด (รายการบอสถูกล้างแล้วที่ _boss2List.Clear())
        private void ResetBoss2EncounterFull()
        {
            _isBoss2Scene = false;
            _boss2SceneDone = false;
            _boss2Stage = default(BossSceneStage);
            _boss2StageTimer = 0f;
            _boss2FocusBlend = 0f;
            _boss2SceneZoom = CameraZoom;
            _boss2ShakeActive = false;
            _boss2ShakeTime = 0f;
            _boss2FightZoomBlend = 0f;
            _boss2SceneBoss = null;
            _boss2HasSavedState = false;
            _boss2Defeated = false;
        }

        // บอส 2 ตาย: ดรอปยา HP เหมือนบอส 1 (เฉพาะบอสในฉากห้องที่ 3 ตัวที่เสกด้วยคอนโซลไม่ดรอป)
        private void DropBoss2Loot(Boss2 boss)
        {
            int potionCount = _random.Next(Boss2PotionDropMin, Boss2PotionDropMax + 1);
            _worldItems.Add(new WorldItem
            {
                Type = ItemType.HpPotion,
                Count = potionCount,
                X = MathHelper.Clamp(boss.X, 0f, _worldWidth),
                Room = boss.Room
            });
        }

        private void SpawnDummy()
        {
            bool alreadyExists = _dummyExists;

            float halfWidth = GetDummyHalfWidth();
            float x = _kaiCenterX + (_facingRight ? 1f : -1f) * DummySpawnDistance;
            _dummyX = MathHelper.Clamp(x, halfWidth, _worldWidth - halfWidth);
            _dummyRoom = _currentRoomIndex;
            _dummyExists = true;
            _dummyShakeTimer = 0f;
            _damagePopups.Clear();

            ShowConsoleMessage(alreadyExists ? "Dummy moved here" : "Dummy spawned", false);
        }

        private void ResetDummyStats()
        {
            if (!_dummyExists)
            {
                ShowConsoleMessage("No dummy yet. Use /spawn_dummy", true);
                return;
            }

            _dummyHitCount = 0;
            _dummyTotalDamage = 0;
            _damagePopups.Clear();
            ShowConsoleMessage("Dummy counters reset", false);
        }

        private void UpdateDummy(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_dummyShakeTimer > 0f)
                _dummyShakeTimer = MathHelper.Max(0f, _dummyShakeTimer - deltaSeconds);

            for (int i = _damagePopups.Count - 1; i >= 0; i--)
            {
                var popup = _damagePopups[i];
                popup.Age += deltaSeconds;
                popup.Position.Y -= DamagePopupRiseSpeed * deltaSeconds;

                if (popup.Age >= DamagePopupLifetime)
                    _damagePopups.RemoveAt(i);
            }
        }

        private void DrawDummy()
        {
            if (!_dummyExists || _dummyRoom != _currentRoomIndex)
                return;

            float shakeRatio = _dummyShakeTimer / DummyShakeDuration;
            float offsetX = shakeRatio > 0f
                ? (float)System.Math.Sin(_dummyShakeTimer * 80f) * DummyShakeAmplitude * shakeRatio
                : 0f;
            Color tint = Color.Lerp(Color.White, new Color(255, 110, 110), shakeRatio);

            var origin = new Vector2(_dummyTrim.Width / 2f, _dummyTrim.Height);
            _spriteBatch.Draw(_dummyTexture, new Vector2(_dummyX + offsetX, _floorY), _dummyTrim, tint, 0f, origin, _dummyScale, SpriteEffects.None, 0f);
        }

        private void DrawDummyStats()
        {
            if (!_dummyExists || _dummyRoom != _currentRoomIndex)
                return;

            float average = _dummyHitCount > 0 ? (float)_dummyTotalDamage / _dummyHitCount : 0f;
            string[] lines =
            {
                $"Hits: {_dummyHitCount}",
                $"Total: {_dummyTotalDamage}",
                $"Avg: {average.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}"
            };

            float y = _floorY + 8f;
            foreach (var line in lines)
            {
                Vector2 size = _hotbarFont.MeasureString(line) * DummyStatsTextScale;
                var position = new Vector2(_dummyX - size.X / 2f, y);
                _spriteBatch.DrawString(_hotbarFont, line, position + new Vector2(1f, 1f), Color.Black, 0f, Vector2.Zero, DummyStatsTextScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_hotbarFont, line, position, Color.White, 0f, Vector2.Zero, DummyStatsTextScale, SpriteEffects.None, 0f);
                y += size.Y;
            }
        }

        private void DrawDamagePopups()
        {
            foreach (var popup in _damagePopups)
            {
                float lifeRatio = popup.Age / DamagePopupLifetime;
                float alpha = 1f - MathHelper.Clamp((lifeRatio - 0.5f) / 0.5f, 0f, 1f);
                float scale = popup.IsCrit ? DamagePopupCritScale : DamagePopupNormalScale;
                Color color = popup.Color * alpha;

                Vector2 size = _hotbarFont.MeasureString(popup.Text) * scale;
                var position = popup.Position - new Vector2(size.X / 2f, size.Y);

                _spriteBatch.DrawString(_hotbarFont, popup.Text, position + new Vector2(2f, 2f), Color.Black * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_hotbarFont, popup.Text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        private void DrawWorldItems()
        {
            foreach (var worldItem in _worldItems)
            {
                if (worldItem.Collected || worldItem.Room != _currentRoomIndex)
                    continue;

                if (worldItem.IsStuck)
                {
                    DrawStuckTrident(worldItem);
                    continue;
                }

                Texture2D texture;
                Rectangle? sourceRect = null;
                float iconHeight;

                if ((worldItem.Type == ItemType.Key || worldItem.Type == ItemType.Orb) && _keyIdleFrames != null && _keyIdleFrames.Length > 0)
                {
                    // กุญแจบนพื้น: เล่นอนิเมชันตากระพริบ/หมุนวนจากสไปรท์ชีท แทนไอคอนนิ่ง
                    var frame = _keyIdleFrames[GetKeyIdleFrameIndex()];
                    texture = frame.Texture;
                    sourceRect = frame.Trim;
                    iconHeight = frame.Trim.Height;
                }
                else
                {
                    texture = GetItemTexture(worldItem.Type);
                    if (texture == null)
                        continue;
                    iconHeight = texture.Height;
                }

                // orb ใหญ่กว่าไอเทมทั่วไปและลอยเหนือพื้นโยกขึ้นลงเบาๆ
                float displayHeight = WorldItemHeight;
                float lift = 0f;
                if (worldItem.Type == ItemType.Orb)
                {
                    displayHeight = OrbWorldHeight;
                    lift = OrbHoverHeight + (float)System.Math.Sin(_orbBobTime * OrbBobSpeed) * OrbBobAmplitude;
                }

                float scale = displayHeight / iconHeight;
                var origin = sourceRect.HasValue
                    ? new Vector2(sourceRect.Value.Width / 2f, sourceRect.Value.Height)
                    : new Vector2(texture.Width / 2f, texture.Height);
                // กุญแจมีอนิเมชันสไปรท์ชีทของตัวเองอยู่แล้ว ไม่ต้องเอียงซ้ำ ส่วนไอเทมอื่น (เช่นประแจ) เอียงตาม GetItemIconRotation
                float rotation = sourceRect.HasValue ? 0f : GetItemIconRotation(worldItem.Type);
                float itemGroundY = GetWorldItemGroundY(worldItem);
                _spriteBatch.Draw(texture, new Vector2(worldItem.X, itemGroundY - lift), sourceRect, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);

                bool inRange = IsWorldItemInPickupRange(worldItem);
                if (inRange && !_isConsoleOpen && !_isGiveItemMenuOpen && !_isWakeUpSequence && !_isOrbScene && !_isBossScene && !_isBoss2Scene && !_isBoss3Scene)
                {
                    string prompt = $"Press E (x{worldItem.Count})";
                    const float promptScale = 0.7f;
                    Vector2 promptSize = _hotbarFont.MeasureString(prompt) * promptScale;
                    var promptPosition = new Vector2(
                        worldItem.X - promptSize.X / 2f,
                        itemGroundY - lift - displayHeight - promptSize.Y - 6f);
                    _spriteBatch.DrawString(_hotbarFont, prompt, promptPosition, Color.White, 0f, Vector2.Zero, promptScale, SpriteEffects.None, 0f);
                }
            }
        }

        private void DrawSlotItem(Rectangle slotRect, InventorySlot slot, bool isDragSource)
        {
            if (slot.IsEmpty || isDragSource)
                return;

            var texture = GetItemTexture(slot.Type);
            if (texture == null)
                return;

            var iconRect = new Rectangle(slotRect.X + 6, slotRect.Y + 6, slotRect.Width - 12, slotRect.Height - 12);
            DrawItemIcon(texture, slot.Type, iconRect, Color.White);

            // วาดตัวเลขจำนวนไอเทม (เมื่อมีมากกว่า 1 ชิ้น)
            if (slot.Count > 1)
            {
                string countText = slot.Count.ToString();
                float fontScale = 0.65f;
                Vector2 textSize = _hotbarFont.MeasureString(countText) * fontScale;
                var textPos = new Vector2(
                    slotRect.Right - textSize.X - 4,
                    slotRect.Bottom - textSize.Y - 2);

                _spriteBatch.DrawString(_hotbarFont, countText, textPos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_hotbarFont, countText, textPos, Color.Yellow, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);
            }
        }

        // =====================================================================
        // /giveme_items Modal UI
        // =====================================================================

        // --- Layout ของหน้าเลือกไอเทม ---
        private struct GiveItemSelectLayout
        {
            public Rectangle PanelRect;
            public Rectangle TitleBarRect;
            public int StartX;
            public int StartY;
            public int SlotSize;
            public int Spacing;

            public Rectangle GetSlotRect(int index)
            {
                int slotX = StartX + index * (SlotSize + Spacing);
                return new Rectangle(slotX, StartY, SlotSize, SlotSize);
            }
        }

        private GiveItemSelectLayout GetGiveItemSelectLayout()
        {
            int panelWidth = 780;   // กว้างขึ้นให้ใส่ไอเทมช่องที่ 6 (Trident) ได้
            int panelHeight = 240;
            int panelX = (WindowedWidth - panelWidth) / 2;
            int panelY = (WindowedHeight - panelHeight) / 2;

            return new GiveItemSelectLayout
            {
                PanelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight),
                TitleBarRect = new Rectangle(panelX, panelY, panelWidth, 42),
                StartX = panelX + 40,
                StartY = panelY + 80,
                SlotSize = 72,
                Spacing = 20
            };
        }

        // --- Layout ของหน้าเลือกจำนวน (Slider) ---
        private struct GiveItemQuantityLayout
        {
            public Rectangle PanelRect;
            public Rectangle TitleBarRect;
            public Rectangle IconRect;
            public Rectangle TrackRect;
            public int HandleX;
            public int HandleY;
            public int HandleRadius;
            public Rectangle GiveButtonRect;
            public Rectangle BackButtonRect;

            public Rectangle GetHandleGrabRect()
            {
                return new Rectangle(HandleX - HandleRadius - 4, HandleY - HandleRadius - 4, (HandleRadius + 4) * 2, (HandleRadius + 4) * 2);
            }
        }

        private GiveItemQuantityLayout GetGiveItemQuantityLayout()
        {
            int panelWidth = 480;
            int panelHeight = 240;
            int panelX = (WindowedWidth - panelWidth) / 2;
            int panelY = (WindowedHeight - panelHeight) / 2;

            var panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);
            var titleBarRect = new Rectangle(panelX, panelY, panelWidth, 42);
            var iconRect = new Rectangle(panelX + (panelWidth - 64) / 2, panelY + 54, 64, 64);

            int trackX = panelX + 60;
            int trackY = panelY + 168;
            int trackWidth = panelWidth - 120;
            int trackHeight = 8;
            var trackRect = new Rectangle(trackX, trackY, trackWidth, trackHeight);

            float t = (GiveItemMaxQuantity > 1)
                ? (float)(_giveItemQuantity - 1) / (GiveItemMaxQuantity - 1)
                : 0f;
            int handleX = trackX + (int)System.Math.Round(t * trackWidth);
            int handleY = trackY + trackHeight / 2;
            const int handleRadius = 10;

            int buttonWidth = 140;
            int buttonHeight = 40;
            int buttonY = panelY + panelHeight - buttonHeight - 20;
            var backButtonRect = new Rectangle(panelX + 40, buttonY, buttonWidth, buttonHeight);
            var giveButtonRect = new Rectangle(panelX + panelWidth - buttonWidth - 40, buttonY, buttonWidth, buttonHeight);

            return new GiveItemQuantityLayout
            {
                PanelRect = panelRect,
                TitleBarRect = titleBarRect,
                IconRect = iconRect,
                TrackRect = trackRect,
                HandleX = handleX,
                HandleY = handleY,
                HandleRadius = handleRadius,
                GiveButtonRect = giveButtonRect,
                BackButtonRect = backButtonRect
            };
        }

        private void UpdateGiveItemMenu()
        {
            if (!_isGiveItemMenuOpen)
                return;

            if (_giveItemQuantityStep)
            {
                UpdateGiveItemQuantityStep();
                return;
            }

            bool leftJustPressed = _mouseState.LeftButton == ButtonState.Pressed
                && _previousMouseState.LeftButton == ButtonState.Released;

            if (!leftJustPressed)
                return;

            Vector2 mouseLogical = GetLogicalMousePosition();
            var layout = GetGiveItemSelectLayout();

            for (int i = 0; i < _giveableItems.Length; i++)
            {
                var slotRect = layout.GetSlotRect(i);

                if (slotRect.Contains(mouseLogical))
                {
                    // ไปหน้าเลือกจำนวนก่อน ยังไม่เสกของทันที
                    _giveItemSelectedType = _giveableItems[i];
                    _giveItemQuantity = 1;
                    _giveItemQuantityStep = true;
                    break;
                }
            }
        }

        private void UpdateGiveItemQuantityStep()
        {
            Vector2 mouseLogical = GetLogicalMousePosition();
            var layout = GetGiveItemQuantityLayout();

            bool leftPressed = _mouseState.LeftButton == ButtonState.Pressed;
            bool leftJustPressed = leftPressed && _previousMouseState.LeftButton == ButtonState.Released;

            if (leftJustPressed && !_isDraggingGiveSlider)
            {
                if (layout.GetHandleGrabRect().Contains(mouseLogical) || layout.TrackRect.Contains(mouseLogical))
                    _isDraggingGiveSlider = true;
            }

            if (_isDraggingGiveSlider)
            {
                if (leftPressed)
                {
                    float t = layout.TrackRect.Width > 0
                        ? MathHelper.Clamp((mouseLogical.X - layout.TrackRect.X) / layout.TrackRect.Width, 0f, 1f)
                        : 0f;
                    _giveItemQuantity = 1 + (int)System.Math.Round(t * (GiveItemMaxQuantity - 1));
                }
                else
                {
                    _isDraggingGiveSlider = false;
                }
            }

            if (leftJustPressed)
            {
                if (layout.GiveButtonRect.Contains(mouseLogical))
                {
                    if (TryAddItem(_giveItemSelectedType, _giveItemQuantity))
                        ShowConsoleMessage($"Gave {_giveItemQuantity}x {GetItemName(_giveItemSelectedType)}", false);
                    else
                        ShowConsoleMessage("Inventory full!", true);

                    _isGiveItemMenuOpen = false;
                    _giveItemQuantityStep = false;
                    _isDraggingGiveSlider = false;
                }
                else if (layout.BackButtonRect.Contains(mouseLogical))
                {
                    _giveItemQuantityStep = false;
                    _isDraggingGiveSlider = false;
                }
            }
        }

        private void DrawGiveItemMenuUI()
        {
            if (!_isGiveItemMenuOpen)
                return;

            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, WindowedHeight), new Color(0, 0, 0, 180));

            if (_giveItemQuantityStep)
                DrawGiveItemQuantityUI();
            else
                DrawGiveItemSelectUI();
        }

        private void DrawGiveItemSelectUI()
        {
            var layout = GetGiveItemSelectLayout();

            DrawFilledRect(layout.PanelRect, new Color(18, 20, 24, 245));
            DrawFilledRect(layout.TitleBarRect, new Color(10, 12, 15, 245));

            string title = "ITEM SELECTOR";
            Vector2 titleSize = _hotbarFont.MeasureString(title);
            var titlePos = new Vector2(layout.PanelRect.X + (layout.PanelRect.Width - titleSize.X) / 2f, layout.PanelRect.Y + (42 - titleSize.Y) / 2f);
            _spriteBatch.DrawString(_hotbarFont, title, titlePos, Color.Gold);

            string subText = "Click an item, then choose quantity (Esc to close)";
            Vector2 subSize = _hotbarFont.MeasureString(subText) * 0.7f;
            var subPos = new Vector2(layout.PanelRect.X + (layout.PanelRect.Width - subSize.X) / 2f, layout.PanelRect.Y + 48f);
            _spriteBatch.DrawString(_hotbarFont, subText, subPos, Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            Vector2 mouseLogical = GetLogicalMousePosition();

            for (int i = 0; i < _giveableItems.Length; i++)
            {
                ItemType itemType = _giveableItems[i];
                var slotRect = layout.GetSlotRect(i);

                bool isHovered = slotRect.Contains(mouseLogical);
                DrawInventorySlot(slotRect, isHovered);

                if (isHovered)
                    DrawRectOutline(slotRect, 2, Color.Gold);

                var texture = GetItemTexture(itemType);
                if (texture != null)
                {
                    var iconRect = new Rectangle(slotRect.X + 8, slotRect.Y + 8, slotRect.Width - 16, slotRect.Height - 16);
                    DrawItemIcon(texture, itemType, iconRect, Color.White);
                }

                string itemName = GetItemName(itemType);
                Vector2 nameSize = _hotbarFont.MeasureString(itemName) * 0.7f;
                var namePos = new Vector2(slotRect.X + (slotRect.Width - nameSize.X) / 2f, slotRect.Y + slotRect.Height + 6f);
                _spriteBatch.DrawString(_hotbarFont, itemName, namePos, Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }

        private void DrawGiveItemQuantityUI()
        {
            var layout = GetGiveItemQuantityLayout();

            DrawFilledRect(layout.PanelRect, new Color(18, 20, 24, 245));
            DrawFilledRect(layout.TitleBarRect, new Color(10, 12, 15, 245));

            string title = "SELECT QUANTITY";
            Vector2 titleSize = _hotbarFont.MeasureString(title);
            var titlePos = new Vector2(layout.PanelRect.X + (layout.PanelRect.Width - titleSize.X) / 2f, layout.PanelRect.Y + (42 - titleSize.Y) / 2f);
            _spriteBatch.DrawString(_hotbarFont, title, titlePos, Color.Gold);

            // ไอคอน + ชื่อไอเทมที่เลือกไว้
            var texture = GetItemTexture(_giveItemSelectedType);
            if (texture != null)
                DrawItemIcon(texture, _giveItemSelectedType, layout.IconRect, Color.White);

            string itemName = GetItemName(_giveItemSelectedType);
            Vector2 nameSize = _hotbarFont.MeasureString(itemName) * 0.8f;
            var namePos = new Vector2(layout.PanelRect.X + (layout.PanelRect.Width - nameSize.X) / 2f, layout.IconRect.Bottom + 4f);
            _spriteBatch.DrawString(_hotbarFont, itemName, namePos, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            // แถบเลื่อนจำนวน (Slider)
            DrawFilledRect(layout.TrackRect, new Color(40, 44, 52, 255));

            if (layout.HandleX > layout.TrackRect.X)
            {
                var filledTrackRect = new Rectangle(layout.TrackRect.X, layout.TrackRect.Y, layout.HandleX - layout.TrackRect.X, layout.TrackRect.Height);
                DrawFilledRect(filledTrackRect, new Color(255, 210, 80, 255));
            }

            var handleRect = new Rectangle(layout.HandleX - layout.HandleRadius, layout.HandleY - layout.HandleRadius, layout.HandleRadius * 2, layout.HandleRadius * 2);
            DrawFilledRect(handleRect, Color.Gold);
            DrawRectOutline(handleRect, 2, Color.White);

            // ข้อความจำนวนที่เลือกอยู่
            string quantityText = $"x{_giveItemQuantity}";
            Vector2 qtySize = _hotbarFont.MeasureString(quantityText);
            var qtyPos = new Vector2(layout.TrackRect.Center.X - qtySize.X / 2f, layout.TrackRect.Y - qtySize.Y - 10f);
            _spriteBatch.DrawString(_hotbarFont, quantityText, qtyPos, Color.White);

            string hintText = "Drag the slider, then click Give (Esc to go back)";
            Vector2 hintSize = _hotbarFont.MeasureString(hintText) * 0.65f;
            var hintPos = new Vector2(layout.PanelRect.X + (layout.PanelRect.Width - hintSize.X) / 2f, layout.TrackRect.Bottom + 10f);
            _spriteBatch.DrawString(_hotbarFont, hintText, hintPos, Color.LightGray, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);

            // ปุ่ม Back / Give
            DrawFilledRect(layout.BackButtonRect, new Color(60, 64, 72, 255));
            DrawRectOutline(layout.BackButtonRect, 2, Color.LightGray);
            DrawCenteredButtonText("Back", layout.BackButtonRect);

            DrawFilledRect(layout.GiveButtonRect, new Color(70, 130, 70, 255));
            DrawRectOutline(layout.GiveButtonRect, 2, Color.LightGreen);
            DrawCenteredButtonText("Give", layout.GiveButtonRect);
        }

        private void DrawCenteredButtonText(string text, Rectangle buttonRect)
        {
            Vector2 textSize = _hotbarFont.MeasureString(text);
            var textPos = new Vector2(
                buttonRect.X + (buttonRect.Width - textSize.X) / 2f,
                buttonRect.Y + (buttonRect.Height - textSize.Y) / 2f);
            _spriteBatch.DrawString(_hotbarFont, text, textPos, Color.White);
        }

        private void DrawRectOutline(Rectangle rect, int thickness, Color color)
        {
            DrawFilledRect(new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            DrawFilledRect(new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            DrawFilledRect(new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            DrawFilledRect(new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawFilledRect(Rectangle rect, Color color)
        {
            _spriteBatch.Draw(_pixelTexture, rect, color);
        }
    }
}
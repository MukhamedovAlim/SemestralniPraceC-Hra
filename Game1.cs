/*
 * This program is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
 * See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SemestralniPraceC_
{
    /// <summary>
    /// Určuje jednotlivé stavy, ve kterých se herní smyčka může nacházet.
    /// </summary>
    enum GameState
    {
        /// <summary>
        /// Zobrazení hlavního menu; hráč může vybrat spuštění hry, nastavení nebo ukončení.
        /// </summary>
        Menu,

        /// <summary>
        /// Hra je aktivní a běží; zpracovávají se vstupy, aktualizace herního světa a vykreslování.</summary>
        Playing,

        /// <summary>
        /// Hra je pozastavena (např. stisknuto Esc); zobrazí se pauzovací menu.</summary>
        Paused,

        /// <summary>
        /// Zobrazení nabídky nastavení; hráč může měnit hlasitost, ovládání a další volby.</summary>
        Options,

        /// <summary>
        /// Hráč úspěšně dokončil všechny úrovně; zobrazí se obrazovka vítězství.</summary>
        Victory,

        /// <summary>
        /// Hráč prohrál nebo ztratil život; zobrazí se obrazovka Game Over s možností restartu či návratu do menu.</summary>
        GameOver
    }

    /// <summary>
    /// Hlavní třída hry, která řídí životní cyklus MonoGame aplikace (inicializace, načítání obsahu,
    /// aktualizace herní logiky a vykreslování).
    /// </summary>
    public class Game1 : Game
    {
        /// <summary>Uložená nejlepší skóre z předchozích spuštění (načítá se ze souboru).</summary>
        private int highScore = 0;

        /// <summary>Cesta k souboru, ve kterém se ukládá nejlepší skóre.</summary>
        private readonly string highScoreFilePath;

        /// <summary>Životy hráče; při dosažení ≤ 0 následuje GameOver.</summary>
        private int playerHP = 100;

        /// <summary>Oblast, ve které se mohou nacházet herní objekty (lodi, nepřátelé).</summary>
        private Rectangle playableArea;

        /// <summary>Časovač pro generování dalšího nepřítele (ve vteřinách).</summary>
        private float spawnTimer = 0f;

        /// <summary>Výchozí interval mezi spawnem nepřátel na 1. úrovni (v sekundách).</summary>
        private float baseSpawnInterval = 2f;

        /// <summary>Základní počet bodů, které hráč získá za zničení nepřítele na 1. úrovni.</summary>
        private int basePoints = 100;

        /// <summary>Počáteční prahová hodnota skóre pro přechod na další úroveň.</summary>
        private int baseThreshold = 1000;

        /// <summary>Aktuální prahová hodnota pro dosažení další úrovně.</summary>
        private int scoreForNextLevel = 1000;

        /// <summary>Položky pauzovacího menu.</summary>
        private string[] pauseMenuItems = { "Continue", "Leave to Menu" };

        /// <summary>Index aktuálně vybrané položky v pauzovacím menu.</summary>
        private int pauseSelectionIndex = 0;

        /// <summary>Úroveň vybraná v hlavním menu (výchozí hodnota).</summary>
        private int selectedLevel = 1;

        /// <summary>Maximální počet úrovní ve hře.</summary>
        private const int maxLevel = 3;

        /// <summary>Aktuální úroveň, na které hráč momentálně hraje.</summary>
        private int currentLevel = 1;

        /// <summary>Základní rychlost pohybu nepřátel; zvyšuje se s každou úrovní.</summary>
        private float enemySpeed = 100f;

        /// <summary>Aktuální stav hry (Menu, Playing, Paused, Options, Victory, GameOver).</summary>
        private GameState currentState = GameState.Menu;

        /// <summary>Index aktuálně zvýrazněné položky v hlavním menu.</summary>
        private int menuSelectionIndex = 0;

        /// <summary>Položky hlavního menu (Start Game, Options, Exit).</summary>
        private string[] menuItems = { "Start Game", "Options", "Exit" };

        // --- Grafika a vykreslování ---

        /// <summary>Správce grafického zařízení (nastavení rozlišení, full‑screen atd.).</summary>
        private GraphicsDeviceManager graphics;

        /// <summary>Objekt pro dávkové vykreslování 2D prvků.</summary>
        private SpriteBatch spriteBatch;

        /// <summary>Textura nepřátelské lodě.</summary>
        private Texture2D enemyTexture;

        /// <summary>Textura hráčovy vesmírné lodi.</summary>
        private Texture2D spaceshipTexture;

        /// <summary>Textura projektilu vystřeleného hráčem.</summary>
        private Texture2D bulletTexture;

        /// <summary>Textura jednoho pixelu sloužícího pro vykreslování částic.</summary>
        private Texture2D particleTexture;

        // --- Herní systémy a objekty ---

        /// <summary>Systém částic pro exploze a efekty výstřelů.</summary>
        private ParticleSystem particleSystem;

        /// <summary>Sběr všech aktivních projektilů na scéně.</summary>
        private List<Bullet> bullets = new List<Bullet>();

        /// <summary>Generátor náhodných čísel pro herní logiku (pozice, rozptyl, atd.).</summary>
        private Random random = new Random();

        // --- Vlastnosti hráčovy lodi ---

        /// <summary>Aktuální pozice hráčovy lodi v herním prostoru.</summary>
        private Vector2 spaceshipPosition = new Vector2(100, 100);

        /// <summary>Rychlost pohybu lodi (body za sekundu).</summary>
        private float spaceshipMoveSpeed = 1000f;

        // --- Recoil efekt při výstřelu ---

        /// <summary>Posunutí lodi pro vizuální efekt zpětného rázu.</summary>
        private Vector2 recoilOffset = new Vector2(0, 5);

        /// <summary>Flag, zda je recoil efekt momentálně aktivní.</summary>
        private bool isRecoilActive = false;

        /// <summary>Doba trvání recoil efektu v sekundách.</summary>
        private double recoilDuration = 0.1;

        /// <summary>Uplynulý čas od začátku recoil efektu.</summary>
        private double elapsedRecoilTime = 0;

        // --- Vlastnosti střelby ---

        /// <summary>Základní rychlost vystřelených projektilů.</summary>
        private float baseBulletSpeed = 400f;

        /// <summary>Základní interval mezi výstřely (v sekundách).</summary>
        private double baseShootCooldown = 0.5;

        /// <summary>Časovač pro sledování cooldownu mezi dvěma výstřely.</summary>
        private double shootCooldownTimer = 0;

        /// <summary>Úroveň zbraně (ovlivňuje rychlost projektilů a cooldown).</summary>
        private int gunLevel = 1;

        /// <summary>Ukládané zvukové efekty pro různé typy výstřelů.</summary>
        private SoundEffect _gunSound1, _gunSound2, _gunSound3;

        /// <summary>Instanční přehrávače zvukových efektů.</summary>
        private SoundEffectInstance soundInstance1, soundInstance2, soundInstance3;

        /// <summary>Jednobodová textura pro vizuální kreslení kolizních boxů.</summary>
        private Texture2D boundingBoxTexture;

        /// <summary>Sběr všech aktivních nepřátel na scéně.</summary>
        List<Enemy> enemies = new List<Enemy>();

        /// <summary>Aktuální skóre hráče v běžné hře.</summary>
        private int score = 0;

        /// <summary>Font použitý pro vykreslení skóre, úrovně a dalších textů.</summary>
        private SpriteFont scoreFont;

        // --- Zpracování vstupů ---

        /// <summary>Uložený stav klávesnice z předchozího snímku (pro detekci jediného stisku).</summary>
        private KeyboardState previousKeyboardState;

        /// <summary>
        /// Konstruktor třídy Game1. Inicializuje grafické prostředí, nastavuje cestu k assetům
        /// a řídí viditelnost kurzoru myši.
        /// </summary>
        public Game1()
        {
            // Inicializuje správce grafiky pro MonoGame,
            // který se postará o nastavení okna, rozlišení a režimu zobrazení.
            graphics = new GraphicsDeviceManager(this);
            // Určuje složku, odkud se budou načítat všechny assety (textury, fonty, zvuky).
            Content.RootDirectory = "Content";
            // Zajišťuje, že bude ve hře viditelný standardní systémový kurzor myši.
            IsMouseVisible = true;

            // --- Nastavíme AppData složku pro highscore ---
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string gameFolder = Path.Combine(appData, "MyAwesomeGame");
            Directory.CreateDirectory(gameFolder);  // vytvoří složku, pokud neexistuje
            highScoreFilePath = Path.Combine(gameFolder, "highscore.txt");
        }
        /// <summary>
        /// Provádí počáteční nastavení hry před načtením obsahu a spuštěním smyčky.
        /// </summary>
        protected override void Initialize()
        {
            // Nastaví rozlišení obrazovky na 2560×1440 (2K) a fullscreen režim.
            graphics.PreferredBackBufferWidth = 2560;
            graphics.PreferredBackBufferHeight = 1440;
            graphics.IsFullScreen = true;
            graphics.ApplyChanges();

            // Definuje hratelnou oblast s 200px okraji ze všech stran,
            // aby herní objekty zůstaly uvnitř viditelné části obrazovky.
            int margin = 200;
            int playableWidth = graphics.PreferredBackBufferWidth - 2 * margin;
            int playableHeight = graphics.PreferredBackBufferHeight - 2 * margin;
            playableArea = new Rectangle(margin, margin, playableWidth, playableHeight);

            // Načte uložené nejlepší skóre ze souboru, pokud existuje,
            // jinak nastaví highScore na 0.
            if (File.Exists(highScoreFilePath))
            {
                if (int.TryParse(File.ReadAllText(highScoreFilePath), out int parsedScore))
                {
                    highScore = parsedScore;
                }
            }
            else
            {
                highScore = 0;
            }

            // Volá základní inicializační logiku třídy Game (nutné pro komponenty a služby).
            base.Initialize();
        }

        /// <summary>
        /// Načte veškeré herní assety (textury, fonty, zvuky) a připraví objekty pro vykreslování.
        /// </summary>
        protected override void LoadContent()
        {
            // Inicializace SpriteBatch pro vykreslování 2D prvků
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Načtení textury vesmírné lodi (soubor „1.png“ v Content)
            spaceshipTexture = Content.Load<Texture2D>("1");

            // Vytvoření jednobodové bílé textury pro kreslení bounding boxů
            boundingBoxTexture = new Texture2D(GraphicsDevice, 1, 1);
            boundingBoxTexture.SetData(new[] { Color.White });

            // Vytvoření 2×2 bílé textury pro částice a inicializace ParticleSystem
            particleTexture = new Texture2D(GraphicsDevice, 2, 2);
            particleTexture.SetData(new[]
            {
        Color.White, Color.White,
        Color.White, Color.White
    });
            particleSystem = new ParticleSystem(particleTexture);

            // Načtení textury projektilů a fontu pro skóre
            bulletTexture = Content.Load<Texture2D>("bulletSpace");
            scoreFont = Content.Load<SpriteFont>("ScoreFont");

            // Načtení textury nepřátel
            enemyTexture = Content.Load<Texture2D>("2B");

            // Načtení zvukových efektů a vytvoření instancí pro opakované přehrávání
            _gunSound1 = Content.Load<SoundEffect>("flaunch");
            soundInstance1 = _gunSound1.CreateInstance();
            _gunSound2 = Content.Load<SoundEffect>("iceball");
            soundInstance2 = _gunSound2.CreateInstance();
            _gunSound3 = Content.Load<SoundEffect>("slimeball");
            soundInstance3 = _gunSound3.CreateInstance();

            // Zarovná pozici lodi na střed spodní části obrazovky
            int screenWidth = graphics.PreferredBackBufferWidth;
            int screenHeight = graphics.PreferredBackBufferHeight;
            spaceshipPosition = new Vector2(
                screenWidth / 2f - spaceshipTexture.Width / 2f,
                screenHeight - spaceshipTexture.Height - 50f
            );
        }


        /// <summary>
        /// Hlavní aktualizační metoda, která se volá každý snímek. Zpracovává herní logiku
        /// pro různé stavy hry (Menu, Playing, Paused, Options, Victory, GameOver).
        /// </summary>
        /// <param name="gameTime">Obsahuje časové informace od předchozího snímku.</param>
        protected override void Update(GameTime gameTime)
        {
            // Získání aktuálního stavu klávesnice
            KeyboardState currentKeyboardState = Keyboard.GetState();
            // Delta time (uplynulý čas od posledního snímku), používaný pro pohyby a časovače
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // --- Zpracování koncových stavů (Victory / GameOver) ---
            if (currentState == GameState.Victory || currentState == GameState.GameOver)
            {
                // Uložení nejlepšího skóre, pokud bylo překročeno
                SaveHighScore();

                // Stisk Enter: restart hry (přejde do Playing)
                if (currentKeyboardState.IsKeyDown(Keys.Enter) && previousKeyboardState.IsKeyUp(Keys.Enter))
                {
                    ResetGame();                       // Obnova výchozích hodnot proměnných
                    currentState = GameState.Playing;  // Návrat do herního módu
                    return;                            // Ukončení dalšího zpracování v tomto snímku
                }

                // Stisk Escape: návrat do hlavního menu
                if (currentKeyboardState.IsKeyDown(Keys.Escape) && previousKeyboardState.IsKeyUp(Keys.Escape))
                {
                    ResetGame();                     // Obnova stavu pro novou hru
                    currentState = GameState.Menu;   // Přepnutí do menu
                    return;                          // Ukončení dalšího zpracování v tomto snímku
                }
            }

            // --- Globální ukončení aplikace (Back na gamepadu) ---
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }

            // --- Pauza během hraní ---
            if (currentState == GameState.Playing &&
                currentKeyboardState.IsKeyDown(Keys.Escape) &&
                previousKeyboardState.IsKeyUp(Keys.Escape))
            {
                // Přepnutí do pauzovacího menu
                currentState = GameState.Paused;
                pauseSelectionIndex = 0; // Reset výchozího výběru
                return;                  // Ukončení zbytku Update() v tomto snímku
            }

            // --- Zpracování pauzovacího menu ---
            if (currentState == GameState.Paused)
            {
                // Navigace šipkami nahoru/dolů
                if (currentKeyboardState.IsKeyDown(Keys.Up) && previousKeyboardState.IsKeyUp(Keys.Up))
                    pauseSelectionIndex = (pauseSelectionIndex + pauseMenuItems.Length - 1) % pauseMenuItems.Length;
                if (currentKeyboardState.IsKeyDown(Keys.Down) && previousKeyboardState.IsKeyUp(Keys.Down))
                    pauseSelectionIndex = (pauseSelectionIndex + 1) % pauseMenuItems.Length;

                // Potvrzení volby Enterem
                if (currentKeyboardState.IsKeyDown(Keys.Enter) && previousKeyboardState.IsKeyUp(Keys.Enter))
                {
                    switch (pauseSelectionIndex)
                    {
                        case 0: // Continue – návrat k hraní
                            currentState = GameState.Playing;
                            previousKeyboardState = currentKeyboardState; // spotřebovat vstup
                            return;

                        case 1: // Leave to Menu – návrat do hlavního menu
                            ResetGame();
                            currentState = GameState.Menu;
                            previousKeyboardState = currentKeyboardState; // spotřebovat vstup
                            return;
                    }
                }
            }

            // --- Hlavní menu ---
            if (currentState == GameState.Menu)
            {
                // Navigace šipkami nahoru/dolů mezi položkami menu
                if (currentKeyboardState.IsKeyDown(Keys.Up) && previousKeyboardState.IsKeyUp(Keys.Up))
                    menuSelectionIndex = (menuSelectionIndex + menuItems.Length - 1) % menuItems.Length;
                if (currentKeyboardState.IsKeyDown(Keys.Down) && previousKeyboardState.IsKeyUp(Keys.Down))
                    menuSelectionIndex = (menuSelectionIndex + 1) % menuItems.Length;

                // Výběr úrovně pomocí šipek vlevo/vpravo
                if (currentKeyboardState.IsKeyDown(Keys.Left) && previousKeyboardState.IsKeyUp(Keys.Left) && selectedLevel > 1)
                    selectedLevel--;
                if (currentKeyboardState.IsKeyDown(Keys.Right) && previousKeyboardState.IsKeyUp(Keys.Right) && selectedLevel < maxLevel)
                    selectedLevel++;

                // Potvrzení volby Enterem
                if (currentKeyboardState.IsKeyDown(Keys.Enter) && previousKeyboardState.IsKeyUp(Keys.Enter))
                {
                    switch (menuSelectionIndex)
                    {
                        case 0: // Start Game
                            currentLevel = selectedLevel;
                            currentState = GameState.Playing;
                            break;
                        case 1: // Options
                            currentState = GameState.Options;
                            break;
                        case 2: // Exit
                            Exit();
                            break;
                    }
                    return; // Prevent falling through to other states in this frame
                }
            }

            // --- Herní logika během hraní ---
            if (currentState == GameState.Playing)
            {
                UpdatePlaying(gameTime);
            }
            // --- Menu nastavení ---
            else if (currentState == GameState.Options)
            {
                // Návrat do hlavního menu stiskem Escape
                if (currentKeyboardState.IsKeyDown(Keys.Escape) && previousKeyboardState.IsKeyUp(Keys.Escape))
                    currentState = GameState.Menu;
            }

            // Uložení stavu klávesnice pro příští snímek
            previousKeyboardState = currentKeyboardState;

            // Volání základní logiky Update z Game
            base.Update(gameTime);
        }

        /// <summary>
        /// Zpracovává uživatelský vstup během hraní, konkrétně pohyb lodi a výstřely.
        /// </summary>
        /// <param name="keyboardState">Aktuální stav klávesnice.</param>
        /// <param name="gameTime">Časové informace pro výpočty založené na čase.</param>
        private void ProcessInput(KeyboardState keyboardState, GameTime gameTime)
        {
            // Delta time: kolik sekund uplynulo od posledního snímku
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // --- Pohyb lodi ---
            // Stisk klávesy A: pohyb doleva
            if (keyboardState.IsKeyDown(Keys.A))
            {
                spaceshipPosition.X -= spaceshipMoveSpeed * deltaTime;
            }
            // Stisk klávesy D: pohyb doprava
            if (keyboardState.IsKeyDown(Keys.D))
            {
                spaceshipPosition.X += spaceshipMoveSpeed * deltaTime;
            }

            // Omezí pozici lodi na hranice herní oblasti
            spaceshipPosition.X = MathHelper.Clamp(
                spaceshipPosition.X,
                playableArea.X,
                playableArea.X + playableArea.Width - spaceshipTexture.Width);
            spaceshipPosition.Y = MathHelper.Clamp(
                spaceshipPosition.Y,
                playableArea.Y,
                playableArea.Y + playableArea.Height - spaceshipTexture.Height);

            // --- Ovládání střelby ---
            // Sniž cooldown mezi výstřely
            shootCooldownTimer -= deltaTime;

            // Pokud je mezera (Space) stisknutá a cooldown vypršel, vystřel
            if (keyboardState.IsKeyDown(Keys.Space) && shootCooldownTimer <= 0)
            {
                FireBullet();
            }
        }


        /// <summary>
        /// Vystřelí projektil z aktuální pozice lodi, aplikujíc vizuální recoil efekt,
        /// nastaví cooldown pro další střelbu a spustí příslušné zvukové efekty.
        /// </summary>
        private void FireBullet()
        {
            // --- Recoil efekt ---
            // Aktivuje vizuální posun lodi při výstřelu
            isRecoilActive = true;
            elapsedRecoilTime = 0;

            // --- Výpočet parametrů projektilu ---
            // Rychlost projektilu roste o 10 % za každý stupeň upgradu zbraně
            float currentBulletSpeed = baseBulletSpeed * (1f + 0.1f * (gunLevel - 1));

            // Cooldown mezi výstřely se zkracuje o 0,05 s za každý stupeň upgradu,
            // minimálně však 0,1 s
            double currentShootCooldown = baseShootCooldown - 0.05 * (gunLevel - 1);
            if (currentShootCooldown < 0.1)
            {
                currentShootCooldown = 0.1;
            }
            shootCooldownTimer = currentShootCooldown;

            // --- Vytvoření projektilu ---
            // Určíme, odkud má projektil odstartovat (střed lodi + jemný posun)
            Vector2 gunPosition = new Vector2(
                spaceshipPosition.X + spaceshipTexture.Width / 2 - bulletTexture.Width / 2 + 14,
                spaceshipPosition.Y);

            // Přidáme nový projektil s nastavenou rychlostí směrem vzhůru
            bullets.Add(new Bullet(gunPosition, new Vector2(0, -currentBulletSpeed)));

            // --- Částicový efekt při výstřelu ---
            // Pět náhodně rozptýlených částic
            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = new Vector2(
                    (float)(random.NextDouble() * 2 - 1) * 50,
                    -random.Next(20, 50));
                float lifetime = (float)(random.NextDouble() * 0.5 + 0.3);
                float size = (float)(random.NextDouble() * 0.5 + 0.5);

                particleSystem.AddParticle(
                    gunPosition,
                    velocity,
                    lifetime,
                    Color.OrangeRed,
                    size);
            }

            // --- Přehrání zvukových efektů ---
            // Hrají se postupně podle úrovně zbraně

            // Základní zvuk
            if (soundInstance1.State == SoundState.Playing)
                soundInstance1.Stop();
            soundInstance1.Volume = 0.3f;
            soundInstance1.Play();

            // Druhý zvuk pro úroveň zbraně ≥ 2
            if (gunLevel >= 2)
            {
                if (soundInstance2.State == SoundState.Playing)
                    soundInstance2.Stop();
                soundInstance2.Volume = 0.4f;
                soundInstance2.Play();
            }

            // Třetí zvuk pro úroveň zbraně ≥ 3
            if (gunLevel >= 3)
            {
                if (soundInstance3.State == SoundState.Playing)
                    soundInstance3.Stop();
                soundInstance3.Volume = 0.5f;
                soundInstance3.Play();
            }
        }


        /// <summary>
        /// Aktualizuje stav všech projektilů a odstraňuje ty, které již nejsou aktivní (vyjely mimo obrazovku).
        /// </summary>
        /// <param name="gameTime">Obsahuje časové informace od předchozího snímku.</param>
        private void UpdateBullets(GameTime gameTime)
        {
            // Získání delta času pro hladké pohyby nezávislé na framerate
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Prochází seznam projektilů odzadu, aby bylo možné bezpečně mazat neaktivní položky
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                // Posuň projektil podle jeho rychlosti a delta času
                bullets[i].Update(deltaTime);

                // Pokud projektil uspěje podmínku IsActive == false (vyjel mimo scénu),
                // odstraň ho ze sbírky
                if (!bullets[i].IsActive)
                {
                    bullets.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Aktualizuje stav systému částic (např. exploze či efekty výstřelu).
        /// </summary>
        /// <param name="gameTime">Informace o uplynulém čase od posledního snímku.</param>
        private void UpdateParticles(GameTime gameTime)
        {
            // Předává do ParticleSystemu delta čas pro vypočítání pohybu a životnosti částic
            particleSystem.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        /// <summary>
        /// Spravuje vizuální recoil efekt lodi a deaktivuje jej po uplynutí definované doby.
        /// </summary>
        /// <param name="gameTime">Informace o uplynulém čase od posledního snímku.</param>
        private void UpdateRecoil(GameTime gameTime)
        {
            // Pokud je recoil právě aktivní, navyšuj akumulovaný čas
            if (isRecoilActive)
            {
                elapsedRecoilTime += gameTime.ElapsedGameTime.TotalSeconds;

                // Po dosažení délky recoilDuration efekt deaktivuj
                if (elapsedRecoilTime >= recoilDuration)
                {
                    isRecoilActive = false;
                }
            }
        }

        /// <summary>
        /// Vykreslí celou scénu podle aktuálního stavu hry a úrovně.
        /// </summary>
        /// <param name="gameTime">Informace o čase od posledního snímku.</param>
        protected override void Draw(GameTime gameTime)
        {
            // --- Výběr barvy pozadí podle úrovně ---
            Color bg;
            switch (currentLevel)
            {
                case 1:
                    bg = Color.MidnightBlue;    // Tmavě modrá pro 1. úroveň
                    break;
                case 2:
                    bg = Color.DarkSlateGray;   // Tmavě šedá pro 2. úroveň
                    break;
                case 3:
                    bg = Color.DarkGreen;       // Tmavě zelená pro 3. úroveň
                    break;
                default:
                    bg = Color.Black;           // Záložní barva (černá)
                    break;
            }

            // Vyčistí obrazovku na zvolenou barvu pozadí
            GraphicsDevice.Clear(bg);

            // Začátek dávkového vykreslování 2D prvků
            spriteBatch.Begin();

            // --- Vykreslení podle aktuálního stavu hry ---
            switch (currentState)
            {
                case GameState.Menu:
                    DrawMainMenu();    // Hlavní menu
                    break;

                case GameState.Paused:
                    DrawPauseMenu();   // Pauzovací menu
                    break;

                case GameState.Options:
                    DrawOptions();     // Nastavení hry
                    break;

                case GameState.Victory:
                    DrawVictory();     // Obrazovka vítězství
                    break;

                case GameState.GameOver:
                    DrawGameOver();    // Obrazovka prohry
                    break;

                case GameState.Playing:
                    DrawPlaying();     // Vlastní herní scéna
                    break;
            }

            // Ukončení dávkového vykreslování
            spriteBatch.End();

            // Volání výchozího vykreslení rodičovské třídy
            base.Draw(gameTime);
        }

        /// <summary>
        /// Vykreslí hlavní nabídku hry na střed obrazovky, včetně názvu, nejlepšího skóre,
        /// položek menu a výběru úrovně.
        /// </summary>
        private void DrawMainMenu()
        {
            // Získání rozměrů obrazovky a výpočet středu
            Viewport vp = GraphicsDevice.Viewport;
            int centerX = vp.Width / 2;
            int centerY = vp.Height / 2;

            // --- Příprava textů ---
            // Název hry s dvojnásobným zvětšením
            string title = "My Awesome Game";
            float titleScale = 2f;
            Vector2 titleSize = scoreFont.MeasureString(title) * titleScale;

            // Text s nejlepším skóre
            string hsText = "High Score: " + highScore;
            Vector2 hsSize = scoreFont.MeasureString(hsText);

            // Položky menu
            float lineHeight = scoreFont.LineSpacing;
            int itemCount = menuItems.Length;

            // Celková výška všech prvků pro vertikální centrování
            float totalHeight =
                titleSize.Y       // výška názvu
              + 20                // mezera pod názvem
              + hsSize.Y          // výška řádku nejlepšího skóre
              + 10                // mezera pod skóre
              + (itemCount * lineHeight)  // výška všech položek menu
              + 10                // mezera před výběrem úrovně
              + lineHeight        // výška textu výběru úrovně
              + 10;               // dolní mezera
            float startY = centerY - totalHeight / 2;

            // --- Vykreslení názvu ---
            Vector2 titlePos = new Vector2(centerX - titleSize.X / 2, startY);
            spriteBatch.DrawString(
                scoreFont,
                title,
                titlePos,
                Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: titleScale,
                effects: SpriteEffects.None,
                layerDepth: 0f);

            // --- Vykreslení nejlepšího skóre pod názvem ---
            Vector2 hsPos = new Vector2(centerX - hsSize.X / 2, startY + titleSize.Y + 20);
            spriteBatch.DrawString(scoreFont, hsText, hsPos, Color.White);

            // --- Vykreslení položek menu ---
            for (int i = 0; i < itemCount; i++)
            {
                Vector2 itemSize = scoreFont.MeasureString(menuItems[i]);
                float y = hsPos.Y + lineHeight + 10 + (i * lineHeight);
                Vector2 pos = new Vector2(centerX - itemSize.X / 2, y);

                // Zvýraznění aktuálně vybrané položky žlutou barvou
                Color color = (i == menuSelectionIndex) ? Color.Yellow : Color.White;
                spriteBatch.DrawString(scoreFont, menuItems[i], pos, color);
            }

            // --- Vykreslení volby úrovně pod položkami menu ---
            string lvlText = "Start at Level: " + selectedLevel;
            Vector2 lvlSize = scoreFont.MeasureString(lvlText);
            float lvlY = hsPos.Y + lineHeight + 10 + (itemCount * lineHeight) + 10;
            Vector2 lvlPos = new Vector2(centerX - lvlSize.X / 2, lvlY);
            spriteBatch.DrawString(scoreFont, lvlText, lvlPos, Color.White);
        }


        /// <summary>
        /// Vykreslí pauzovací menu uprostřed obrazovky s možnostmi návratu k hře nebo opuštění do hlavního menu.
        /// </summary>
        private void DrawPauseMenu()
        {
            // Zjištění rozměrů obrazovky a výpočet středu
            Viewport vp = GraphicsDevice.Viewport;
            int centerX = vp.Width / 2;
            int centerY = vp.Height / 2;

            // Výška jednoho řádku textu a mezera mezi řádky
            float lineHeight = scoreFont.LineSpacing;
            float padding = 10f;

            // Počet položek v pauzovacím menu
            int itemCount = pauseMenuItems.Length;

            // Celková výška bloku menu pro vertikální centrování
            float totalHeight = itemCount * lineHeight + (itemCount - 1) * padding;
            float startY = centerY - totalHeight / 2;

            // Pro každou položku menu: změř šířku textu, vypočti pozici a vykresli
            for (int i = 0; i < itemCount; i++)
            {
                Vector2 size = scoreFont.MeasureString(pauseMenuItems[i]);
                Vector2 pos = new Vector2(
                    centerX - size.X / 2,              // horizontální centrování
                    startY + i * (lineHeight + padding) // vertikální odsazení podle indexu
                );

                // Zvýraznění aktuálně vybrané položky žlutou barvou
                Color color = (i == pauseSelectionIndex) ? Color.Yellow : Color.White;
                spriteBatch.DrawString(scoreFont, pauseMenuItems[i], pos, color);
            }
        }

        /// <summary>
        /// Vykreslí obrazovku Game Over uprostřed displeje s informacemi o skóre
        /// a instrukcemi pro restart nebo návrat do menu.
        /// </summary>
        private void DrawGameOver()
        {
            Viewport vp = GraphicsDevice.Viewport;
            int centerX = vp.Width / 2;
            int centerY = vp.Height / 2;

            // Texty k zobrazení
            string[] lines = {
        "Game Over!",
        "Final Score: " + score,
        "Press Enter to Restart",
        "Press Escape for Menu"
    };
            // Barvy pro jednotlivé řádky (první dva červené, zbývající bílé)
            Color[] cols = { Color.Red, Color.Red, Color.White, Color.White };

            float lineHeight = scoreFont.LineSpacing;
            float padding = 10;
            // Celková výška textového bloku pro vertikální centrování
            float totalHeight = lines.Length * lineHeight + (lines.Length - 1) * padding;
            float startY = centerY - totalHeight / 2;

            // Pro každý řádek: změř šířku a vykresli ho centrovaně
            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 size = scoreFont.MeasureString(lines[i]);
                Vector2 pos = new Vector2(
                    centerX - size.X / 2,
                    startY + i * (lineHeight + padding)
                );
                spriteBatch.DrawString(scoreFont, lines[i], pos, cols[i]);
            }
        }

        /// <summary>
        /// Vykreslí obrazovku Victory uprostřed displeje s gratulací, skóre
        /// a instrukcemi pro restart nebo návrat do menu.
        /// </summary>
        private void DrawVictory()
        {
            Viewport vp = GraphicsDevice.Viewport;
            int centerX = vp.Width / 2;
            int centerY = vp.Height / 2;

            // Texty k zobrazení
            string[] lines = {
        "Congratulations!",
        "You have completed all levels!",
        "Final Score: " + score,
        "Press Enter to Restart",
        "Press Escape for Menu"
    };
            // Barvy pro jednotlivé řádky (prvních 3 zelené, zbývající bílé)
            Color[] cols = { Color.Green, Color.Green, Color.Green, Color.White, Color.White };

            float lineHeight = scoreFont.LineSpacing;
            float padding = 10;
            // Celková výška textového bloku pro vertikální centrování
            float totalHeight = lines.Length * lineHeight + (lines.Length - 1) * padding;
            float startY = centerY - totalHeight / 2;

            // Pro každý řádek: změř šířku a vykresli ho centrovaně
            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 size = scoreFont.MeasureString(lines[i]);
                Vector2 pos = new Vector2(
                    centerX - size.X / 2,
                    startY + i * (lineHeight + padding)
                );
                spriteBatch.DrawString(scoreFont, lines[i], pos, cols[i]);
            }
        }

        /// <summary>
        /// Vykreslí obrazovku Options uprostřed displeje s jednoduchými možnostmi
        /// a instrukcí pro návrat do hlavního menu.
        /// </summary>
        private void DrawOptions()
        {
            Viewport vp = GraphicsDevice.Viewport;
            int centerX = vp.Width / 2;
            int centerY = vp.Height / 2;

            // Texty k zobrazení
            string[] lines = { "Options", "Press Escape to return"};
            float lineHeight = scoreFont.LineSpacing;
            float padding = 10;
            // Celková výška textového bloku pro vertikální centrování
            float totalHeight = lines.Length * lineHeight + (lines.Length - 1) * padding;
            float startY = centerY - totalHeight / 2;

            // Pro každý řádek: změř šířku a vykresli ho centrovaně
            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 size = scoreFont.MeasureString(lines[i]);
                Vector2 pos = new Vector2(
                    centerX - size.X / 2,
                    startY + i * (lineHeight + padding)
                );
                spriteBatch.DrawString(scoreFont, lines[i], pos, Color.White);
            }
        }
        private void SpawnEnemy()
        {
            // Enemies spawn from just above the playable area, and only within its width.
            int spawnX = random.Next(playableArea.X, playableArea.X + playableArea.Width - enemyTexture.Width);
            int spawnY = playableArea.Y - enemyTexture.Height; // spawn slightly above
            Vector2 spawnPosition = new Vector2(spawnX, spawnY);
            Vector2 enemyVelocity = new Vector2(0, enemySpeed);
            Enemy newEnemy = new Enemy(enemyTexture, spawnPosition, enemyVelocity);
            enemies.Add(newEnemy);
        }
        private void SaveHighScore()
        {
            if (score > highScore)
            {
                highScore = score;
                File.WriteAllText(highScoreFilePath, highScore.ToString());
            }
        }
        private void ResetGame()
        {
            score = 0;
            playerHP = 100;
            currentLevel = selectedLevel;
            enemySpeed = 100f;
            scoreForNextLevel = baseThreshold;
            enemies.Clear();
            bullets.Clear();
        }
        private void UpdatePlaying(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            // Ship vs enemy collision
            Rectangle shipBounds = new Rectangle((int)spaceshipPosition.X, (int)spaceshipPosition.Y, spaceshipTexture.Width, spaceshipTexture.Height);
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i].GetBounds().Intersects(shipBounds))
                {
                    currentState = GameState.GameOver;
                    return;
                }
            }
            // Enemy reach bottom
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i].Position.Y + enemyTexture.Height >= playableArea.Y + (int)(playableArea.Height * 0.99))
                {
                    playerHP -= 10;
                    enemies.RemoveAt(i);
                    if (playerHP <= 0)
                    {
                        currentState = GameState.GameOver;
                        return;
                    }
                }
            }
            // Spawn
            spawnTimer -= deltaTime;
            if (spawnTimer <= 0f) { SpawnEnemy(); spawnTimer = baseSpawnInterval / currentLevel; }
            // Update bullets & enemies
            bullets.RemoveAll(b => { b.Update(deltaTime); return !b.IsActive; });
            enemies.RemoveAll(e => { e.Update(deltaTime); return !e.IsActive; });
            // Bullet-enemy collisions
            foreach (var enemy in enemies.ToArray())
            {
                Rectangle eb = enemy.GetBounds();
                foreach (var bullet in bullets.ToArray())
                {
                    Rectangle bb = new Rectangle((int)bullet.Position.X, (int)bullet.Position.Y, bulletTexture.Width, bulletTexture.Height);
                    if (eb.Intersects(bb))
                    {
                        enemy.IsActive = false;
                        bullet.IsActive = false;
                        score += basePoints * currentLevel;
                        soundInstance2.Volume = 0.3f;
                        soundInstance2.Play();
                        break;
                    }
                }
            }
            // Level up
            if (score >= scoreForNextLevel)
            {
                score -= scoreForNextLevel;
                if (currentLevel >= maxLevel) { currentState = GameState.Victory; return; }
                currentLevel++;
                scoreForNextLevel = baseThreshold * currentLevel * currentLevel;
                enemySpeed *= 1.3f;
            }
            // Input & effects
            ProcessInput(Keyboard.GetState(), gameTime);
            if (Keyboard.GetState().IsKeyDown(Keys.L) && shootCooldownTimer <= 0)
            {
                gunLevel++;
            }
            UpdateBullets(gameTime);
            UpdateParticles(gameTime);
            UpdateRecoil(gameTime);
        }
        private void DrawPlaying()
        {
            // Draw ship
            Vector2 drawPos = isRecoilActive ? spaceshipPosition + recoilOffset : spaceshipPosition;
            spriteBatch.Draw(spaceshipTexture, drawPos, Color.White);
            // Particles
            particleSystem.Draw(spriteBatch);
            // Bullets
            float bs = 0.5f;
            foreach (var bullet in bullets)
            {
                spriteBatch.Draw(bulletTexture, bullet.Position, null, Color.White, 0f, new Vector2(bulletTexture.Width / 2, bulletTexture.Height / 2), bs, SpriteEffects.None, 0f);
            }
            // Enemies
            foreach (var enemy in enemies) enemy.Draw(spriteBatch);
            // HUD
            spriteBatch.DrawString(scoreFont, "Score: " + score, new Vector2(10, 10), Color.White);
            spriteBatch.DrawString(scoreFont, "Level: " + currentLevel, new Vector2(10, 40), Color.White);
            spriteBatch.DrawString(scoreFont, "HP: " + playerHP, new Vector2(10, 70), Color.White);
            // Borders
            Texture2D border = new Texture2D(GraphicsDevice, 1, 1);
            border.SetData(new[] { Color.Yellow });
            spriteBatch.Draw(border, new Rectangle(playableArea.X, playableArea.Y, 2, playableArea.Height), Color.Yellow);
            spriteBatch.Draw(border, new Rectangle(playableArea.X + playableArea.Width - 2, playableArea.Y, 2, playableArea.Height), Color.Yellow);
        }
    }

}

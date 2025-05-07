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
    enum GameState
    {
        Menu,
        Playing,
        Paused,
        Options,
        Victory,  // New state when the player wins
        GameOver  // (Optional) In case you want a losing condition
    }

    public class Game1 : Game
    {
        private int highScore = 0;
        private string highScoreFilePath = "highscore.txt";
        private int playerHP = 100; // You can adjust the starting HP as needed
        private Rectangle playableArea;
        private float spawnTimer = 0f;
        // Base spawn interval at level 1 (in seconds)
        private float baseSpawnInterval = 2f;
        private int basePoints = 100;      // Base points for an enemy at level 1.
        private int baseThreshold = 1000;  // Base threshold for leveling up.
        private int scoreForNextLevel = 1000;  // Initial threshold.
        private string[] pauseMenuItems = { "Continue", "Leave to Menu" };
        private int pauseSelectionIndex = 0;
        private int selectedLevel = 1; // Default level selected in the menu
        private const int maxLevel = 10;  // Maximum level you allow the player to select
        private int currentLevel = 1;
        private float enemySpeed = 100f;      // Base enemy speed; will increase with each level
        private GameState currentState = GameState.Menu;
        private int menuSelectionIndex = 0;
        private string[] menuItems = { "Start Game", "Options", "Exit" };
        // Graphics and rendering
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D enemyTexture;
        // Textures and assets
        private Texture2D spaceshipTexture;
        private Texture2D bulletTexture;
        private Texture2D particleTexture;

        // Game objects and systems
        private ParticleSystem particleSystem;
        private List<Bullet> bullets = new List<Bullet>();
        private Random random = new Random();

        // Spaceship properties
        private Vector2 spaceshipPosition = new Vector2(100, 100); // Default starting position
        private float spaceshipMoveSpeed = 600f;

        // Recoil effect properties
        private Vector2 recoilOffset = new Vector2(0, 5); // Recoil visual offset
        private bool isRecoilActive = false;
        private double recoilDuration = 0.1; // Duration of the recoil effect in seconds
        private double elapsedRecoilTime = 0;

        // Shooting and bullet properties
        private float baseBulletSpeed = 400f; // Base speed of bullets
        private double baseShootCooldown = 0.5; // Base cooldown in seconds for level 1
        private double shootCooldownTimer = 0; // Timer for shooting cooldown
        private int gunLevel = 1; // Gun level, can be increased to adjust bullet speed and cooldown
        private SoundEffect _gunSound1;
        private SoundEffect _gunSound2;
        private SoundEffect _gunSound3;
        private SoundEffectInstance soundInstance3;
        private SoundEffectInstance soundInstance2;
        private SoundEffectInstance soundInstance1;
        private Texture2D boundingBoxTexture;
        List<Enemy> enemies = new List<Enemy>();
        private int score = 0;
        private SpriteFont scoreFont;

        // Input handling
        private KeyboardState previousKeyboardState;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // Set 2K resolution and fullscreen mode
            graphics.PreferredBackBufferWidth = 2560;
            graphics.PreferredBackBufferHeight = 1440;
            graphics.IsFullScreen = true;
            graphics.ApplyChanges();

            // Define a smaller playable area with 200-pixel margins all around
            int margin = 200;
            int playableWidth = graphics.PreferredBackBufferWidth - 2 * margin;
            int playableHeight = graphics.PreferredBackBufferHeight - 2 * margin;
            playableArea = new Rectangle(margin, margin, playableWidth, playableHeight);

            if (File.Exists(highScoreFilePath))
            {
                int parsedScore;
                if (int.TryParse(File.ReadAllText(highScoreFilePath), out parsedScore))
                {
                    highScore = parsedScore;
                }
            }
            else
            {
                highScore = 0;
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            spaceshipTexture = Content.Load<Texture2D>("1"); // Filename without extension
            boundingBoxTexture = new Texture2D(GraphicsDevice, 1, 1);
            boundingBoxTexture.SetData(new Color[] { Color.White });

            // Create a simple white texture for particles
            particleTexture = new Texture2D(GraphicsDevice, 2, 2);
            particleTexture.SetData(new Color[] { Color.White, Color.White, Color.White, Color.White });
            particleSystem = new ParticleSystem(particleTexture);

            bulletTexture = Content.Load<Texture2D>("bulletSpace");
            scoreFont = Content.Load<SpriteFont>("ScoreFont"); // Make sure the name matches your font asset

            enemyTexture = Content.Load<Texture2D>("2B");

            _gunSound1 = Content.Load<SoundEffect>("flaunch");
            soundInstance1 = _gunSound1.CreateInstance();
            _gunSound2 = Content.Load<SoundEffect>("iceball");
            soundInstance2 = _gunSound2.CreateInstance();
            _gunSound3 = Content.Load<SoundEffect>("slimeball");
            soundInstance3 = _gunSound3.CreateInstance();

            // Center the spaceship at the bottom of the screen
            int screenWidth = graphics.PreferredBackBufferWidth;
            int screenHeight = graphics.PreferredBackBufferHeight;
            spaceshipPosition = new Vector2(
                screenWidth / 2 - spaceshipTexture.Width / 2,
                screenHeight - spaceshipTexture.Height - 50);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle Victory/GameOver
            if (currentState == GameState.Victory || currentState == GameState.GameOver)
            {
                SaveHighScore();
                if (currentKeyboardState.IsKeyDown(Keys.Enter) && previousKeyboardState.IsKeyUp(Keys.Enter))
                {
                    // Restart game
                    ResetGame();
                    currentState = GameState.Playing;
                    return;
                }
                if (currentKeyboardState.IsKeyDown(Keys.Escape) && previousKeyboardState.IsKeyUp(Keys.Escape))
                {
                    ResetGame();
                    currentState = GameState.Menu;
                    return;
                }
            }

            // Global Exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            // Pause toggle from Playing
            if (currentState == GameState.Playing && currentKeyboardState.IsKeyDown(Keys.Escape) && previousKeyboardState.IsKeyUp(Keys.Escape))
            {
                currentState = GameState.Paused;
                pauseSelectionIndex = 0;
                return;
            }

            // Pause menu
            if (currentState == GameState.Paused)
            {
                // Navigate Pause menu
                if (currentKeyboardState.IsKeyDown(Keys.Up) && previousKeyboardState.IsKeyUp(Keys.Up))
                    pauseSelectionIndex = (pauseSelectionIndex + pauseMenuItems.Length - 1) % pauseMenuItems.Length;
                if (currentKeyboardState.IsKeyDown(Keys.Down) && previousKeyboardState.IsKeyUp(Keys.Down))
                    pauseSelectionIndex = (pauseSelectionIndex + 1) % pauseMenuItems.Length;

                if (currentKeyboardState.IsKeyDown(Keys.Enter) && previousKeyboardState.IsKeyUp(Keys.Enter))
                {
                    switch (pauseSelectionIndex)
                    {
                        case 0: // Continue
                            currentState = GameState.Playing;
                            previousKeyboardState = currentKeyboardState; // consume input
                            return;

                        case 1: // Leave to menu
                            ResetGame();
                            currentState = GameState.Menu;
                            previousKeyboardState = currentKeyboardState; // consume input
                            return;
                    }
                }
            }

            // Main Menu
            if (currentState == GameState.Menu)
            {
                // Navigate main menu
                if (currentKeyboardState.IsKeyDown(Keys.Up) && previousKeyboardState.IsKeyUp(Keys.Up))
                    menuSelectionIndex = (menuSelectionIndex + menuItems.Length - 1) % menuItems.Length;
                if (currentKeyboardState.IsKeyDown(Keys.Down) && previousKeyboardState.IsKeyUp(Keys.Down))
                    menuSelectionIndex = (menuSelectionIndex + 1) % menuItems.Length;
                // Level selection
                if (currentKeyboardState.IsKeyDown(Keys.Left) && previousKeyboardState.IsKeyUp(Keys.Left) && selectedLevel > 1)
                    selectedLevel--;
                if (currentKeyboardState.IsKeyDown(Keys.Right) && previousKeyboardState.IsKeyUp(Keys.Right) && selectedLevel < maxLevel)
                    selectedLevel++;
                // Confirm
                if (currentKeyboardState.IsKeyDown(Keys.Enter) && previousKeyboardState.IsKeyUp(Keys.Enter))
                {
                    switch (menuSelectionIndex)
                    {
                        case 0:
                            currentLevel = selectedLevel;
                            currentState = GameState.Playing;
                            break;
                        case 1:
                            currentState = GameState.Options;
                            break;
                        case 2:
                            Exit();
                            break;
                    }
                    return;
                }
            }

            // Playing state logic omitted for brevity
            if (currentState == GameState.Playing)
            {
                UpdatePlaying(gameTime);
            }
            // Options
            else if (currentState == GameState.Options)
            {
                if (currentKeyboardState.IsKeyDown(Keys.Escape) && previousKeyboardState.IsKeyUp(Keys.Escape))
                    currentState = GameState.Menu;
            }

            previousKeyboardState = currentKeyboardState;
            base.Update(gameTime);
        }


        private void ProcessInput(KeyboardState keyboardState, GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            // Move spaceship left and right
            if (keyboardState.IsKeyDown(Keys.A))
            {
                spaceshipPosition.X -= spaceshipMoveSpeed * deltaTime;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                spaceshipPosition.X += spaceshipMoveSpeed * deltaTime;
            }
            // Clamp spaceshipPosition to playableArea
            spaceshipPosition.X = MathHelper.Clamp(spaceshipPosition.X, playableArea.X, playableArea.X + playableArea.Width - spaceshipTexture.Width);
            spaceshipPosition.Y = MathHelper.Clamp(spaceshipPosition.Y, playableArea.Y, playableArea.Y + playableArea.Height - spaceshipTexture.Height);

            // Update shooting cooldown timer
            shootCooldownTimer -= gameTime.ElapsedGameTime.TotalSeconds;
            // If the Space key is held down and the cooldown timer has expired, fire a bullet.
            if (keyboardState.IsKeyDown(Keys.Space) && shootCooldownTimer <= 0)
            {
                FireBullet();
            }
        }

        private void FireBullet()
        {
            // Trigger the recoil effect
            isRecoilActive = true;
            elapsedRecoilTime = 0;
            // Calculate bullet speed based on gun level (each level increases speed by 10%)
            float currentBulletSpeed = baseBulletSpeed * (1f + 0.1f * (gunLevel - 1));
            // Calculate shooting cooldown based on gun level (each level decreases cooldown by 0.05 sec, with a minimum threshold)
            double currentShootCooldown = baseShootCooldown - 0.05 * (gunLevel - 1);
            if (currentShootCooldown < 0.1)
                currentShootCooldown = 0.1;
            shootCooldownTimer = currentShootCooldown;
            // Determine the bullet's spawn position relative to the spaceship
            Vector2 gunPosition = new Vector2(
                spaceshipPosition.X + spaceshipTexture.Width / 2 - bulletTexture.Width / 2 + 14,
                spaceshipPosition.Y);
            // Create a new bullet moving upward
            bullets.Add(new Bullet(gunPosition, new Vector2(0, -currentBulletSpeed)));
            // Spawn particles for the shot
            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = new Vector2(
                    (float)(random.NextDouble() * 2 - 1) * 50,
                    -random.Next(20, 50));
                float lifetime = (float)(random.NextDouble() * 0.5 + 0.3);
                float size = (float)(random.NextDouble() * 0.5 + 0.5);
                particleSystem.AddParticle(gunPosition, velocity, lifetime, Color.OrangeRed, size);
            }
            // Always play the first gun sound.
            if (soundInstance1.State == SoundState.Playing)
                soundInstance1.Stop();
            soundInstance1.Volume = 0.3f;
            soundInstance1.Play();
            // If gun level is 2 or higher, also play the second sound.
            if (gunLevel >= 2)
            {
                if (soundInstance2.State == SoundState.Playing)
                    soundInstance2.Stop();
                soundInstance2.Volume = 0.4f;
                soundInstance2.Play();
            }
            // If gun level is 3 or higher, also play the third sound.
            if (gunLevel >= 3)
            {
                if (soundInstance3.State == SoundState.Playing)
                    soundInstance3.Stop();
                soundInstance3.Volume = 0.5f;
                soundInstance3.Play();
            }
        }

        private void UpdateBullets(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                bullets[i].Update(deltaTime);
                if (!bullets[i].IsActive)
                {
                    bullets.RemoveAt(i);
                }
            }
        }

        private void UpdateParticles(GameTime gameTime)
        {
            particleSystem.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        private void UpdateRecoil(GameTime gameTime)
        {
            if (isRecoilActive)
            {
                elapsedRecoilTime += gameTime.ElapsedGameTime.TotalSeconds;
                if (elapsedRecoilTime >= recoilDuration)
                {
                    isRecoilActive = false;
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();

            switch (currentState)
            {
                case GameState.Menu:
                    DrawMainMenu();
                    break;
                case GameState.Paused:
                    DrawPauseMenu();
                    break;
                case GameState.Options:
                    DrawOptions();
                    break;
                case GameState.Victory:
                    DrawVictory();
                    break;
                case GameState.GameOver:
                    DrawGameOver();
                    break;
                case GameState.Playing:
                    DrawPlaying();
                    break;
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }
        private void DrawMainMenu()
        {
            Viewport vp = GraphicsDevice.Viewport;
            int centerX = vp.Width / 2;
            int centerY = vp.Height / 2;

            // Title
            string title = "My Awesome Game";
            float titleScale = 2f;
            Vector2 titleSize = scoreFont.MeasureString(title) * titleScale;

            // High score text
            string hsText = "High Score: " + highScore;
            Vector2 hsSize = scoreFont.MeasureString(hsText);

            // Menu items
            float lineHeight = scoreFont.LineSpacing;
            int itemCount = menuItems.Length;

            // Calculate vertical start so everything is centered
            float totalHeight = titleSize.Y + 20 + hsSize.Y + 10 + (itemCount * lineHeight) + 10 + lineHeight + 10;
            float startY = centerY - totalHeight / 2;

            // Draw title
            Vector2 titlePos = new Vector2(centerX - titleSize.X / 2, startY);
            spriteBatch.DrawString(scoreFont, title, titlePos, Color.White, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

            // Draw high score
            Vector2 hsPos = new Vector2(centerX - hsSize.X / 2, startY + titleSize.Y + 20);
            spriteBatch.DrawString(scoreFont, hsText, hsPos, Color.White);

            // Draw menu items
            for (int i = 0; i < itemCount; i++)
            {
                Vector2 itemSize = scoreFont.MeasureString(menuItems[i]);
                float y = hsPos.Y + lineHeight + 10 + (i * lineHeight);
                Vector2 pos = new Vector2(centerX - itemSize.X / 2, y);
                Color col = (i == menuSelectionIndex) ? Color.Yellow : Color.White;
                spriteBatch.DrawString(scoreFont, menuItems[i], pos, col);
            }

            // Draw level selector
            string lvlText = "Start at Level: " + selectedLevel;
            Vector2 lvlSize = scoreFont.MeasureString(lvlText);
            float lvlY = hsPos.Y + lineHeight + 10 + (itemCount * lineHeight) + 10;
            Vector2 lvlPos = new Vector2(centerX - lvlSize.X / 2, lvlY);
            spriteBatch.DrawString(scoreFont, lvlText, lvlPos, Color.White);
        }

        private void DrawPauseMenu()
        {
            Viewport vp = GraphicsDevice.Viewport;
            int centerX = vp.Width / 2;
            int centerY = vp.Height / 2;

            float lineHeight = scoreFont.LineSpacing;
            float padding = 10f;
            int itemCount = pauseMenuItems.Length;
            float totalHeight = itemCount * lineHeight + (itemCount - 1) * padding;
            float startY = centerY - totalHeight / 2;

            for (int i = 0; i < itemCount; i++)
            {
                Vector2 size = scoreFont.MeasureString(pauseMenuItems[i]);
                Vector2 pos = new Vector2(centerX - size.X / 2, startY + i * (lineHeight + padding));
                Color col = (i == pauseSelectionIndex) ? Color.Yellow : Color.White;
                spriteBatch.DrawString(scoreFont, pauseMenuItems[i], pos, col);
            }
        }

        private void DrawGameOver()
        {
            Viewport vp = GraphicsDevice.Viewport;
            int centerX = vp.Width / 2;
            int centerY = vp.Height / 2;
            string[] lines = {
        "Game Over!",
        "Final Score: " + score,
        "Press Enter to Restart",
        "Press Escape for Menu"
    };
            Color[] cols = { Color.Red, Color.Red, Color.White, Color.White };
            float lh = scoreFont.LineSpacing;
            float totalH = lines.Length * lh + (lines.Length - 1) * 10;
            float startY = centerY - totalH / 2;

            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 size = scoreFont.MeasureString(lines[i]);
                Vector2 pos = new Vector2(centerX - size.X / 2, startY + i * (lh + 10));
                spriteBatch.DrawString(scoreFont, lines[i], pos, cols[i]);
            }
        }
        private void DrawVictory()
        {
            Viewport vp = GraphicsDevice.Viewport;
            int centerX = vp.Width / 2;
            int centerY = vp.Height / 2;
            string[] lines = {
        "Congratulations!",
        "You have completed all levels!",
        "Final Score: " + score,
        "Press Enter to Restart",
        "Press Escape for Menu"
    };
            Color[] cols = { Color.Green, Color.Green, Color.Green, Color.White, Color.White };
            float lh = scoreFont.LineSpacing;
            float totalH = lines.Length * lh + (lines.Length - 1) * 10;
            float startY = centerY - totalH / 2;

            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 size = scoreFont.MeasureString(lines[i]);
                Vector2 pos = new Vector2(centerX - size.X / 2, startY + i * (lh + 10));
                spriteBatch.DrawString(scoreFont, lines[i], pos, cols[i]);
            }
        }
        private void DrawOptions()
        {
            // Centered Options screen
            Viewport vp = GraphicsDevice.Viewport;
            int centerX = vp.Width / 2;
            int centerY = vp.Height / 2;

            string[] lines = { "Options", "Press Escape to return" };
            float lineHeight = scoreFont.LineSpacing;
            float padding = 10;
            float totalHeight = lines.Length * lineHeight + (lines.Length - 1) * padding;
            float startY = centerY - totalHeight / 2;

            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 textSize = scoreFont.MeasureString(lines[i]);
                Vector2 position = new Vector2(
                    centerX - textSize.X / 2,
                    startY + i * (lineHeight + padding)
                );
                spriteBatch.DrawString(scoreFont, lines[i], position, Color.White);
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
                enemySpeed *= 1.1f;
            }
            // Input & effects
            ProcessInput(Keyboard.GetState(), gameTime);
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

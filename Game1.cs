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

            if (currentState == GameState.Victory || currentState == GameState.GameOver)
            {
                if (currentKeyboardState.IsKeyDown(Keys.Enter) && previousKeyboardState.IsKeyUp(Keys.Enter))
                {
                    // Reset game variables
                    score = 0;
                    playerHP = 100; // Reset HP
                    currentLevel = selectedLevel;  // or reset to a default level like 1
                    enemySpeed = 100f;
                    scoreForNextLevel = baseThreshold;
                    enemies.Clear();
                    bullets.Clear();
                    currentState = GameState.Menu;
                }
            }

            // Only exit when in Playing state (or adjust as needed)
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                (currentState == GameState.Playing && currentKeyboardState.IsKeyDown(Keys.Escape)))
            {
                Exit();
            }

            if (currentState == GameState.Menu)
            {
                // Navigate the menu
                if (currentKeyboardState.IsKeyDown(Keys.Up) && previousKeyboardState.IsKeyUp(Keys.Up))
                {
                    menuSelectionIndex--;
                    if (menuSelectionIndex < 0)
                        menuSelectionIndex = menuItems.Length - 1; // Wrap around to bottom
                }
                if (currentKeyboardState.IsKeyDown(Keys.Down) && previousKeyboardState.IsKeyUp(Keys.Down))
                {
                    menuSelectionIndex++;
                    if (menuSelectionIndex >= menuItems.Length)
                        menuSelectionIndex = 0; // Wrap around to top
                }
                // If "Start Game" is highlighted, allow level selection with left/right arrows
                if (menuSelectionIndex == 0)
                {
                    if (currentKeyboardState.IsKeyDown(Keys.Left) && previousKeyboardState.IsKeyUp(Keys.Left))
                    {
                        if (selectedLevel > 1)
                            selectedLevel--;
                    }
                    if (currentKeyboardState.IsKeyDown(Keys.Right) && previousKeyboardState.IsKeyUp(Keys.Right))
                    {
                        if (selectedLevel < maxLevel)
                            selectedLevel++;
                    }
                }
                // Select menu option with Enter
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
                }
            }
            else if (currentState == GameState.Playing)
            {
                Rectangle GetSpaceshipBounds()
                {
                    return new Rectangle((int)spaceshipPosition.X, (int)spaceshipPosition.Y, spaceshipTexture.Width, spaceshipTexture.Height);
                }
                // Check collisions between enemies and the spaceship
                Rectangle spaceshipBounds = GetSpaceshipBounds();
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Enemy enemy = enemies[i];
                    if (enemy.GetBounds().Intersects(spaceshipBounds))
                    {
                        // Collision detected: you lose the game
                        currentState = GameState.GameOver;
                        break;
                    }
                }
                // Check if any enemy is too close to the bottom of the playable area
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Enemy enemy = enemies[i];
                    // If enemy reaches 90% of the playable area's height, damage the player
                    if (enemy.Position.Y + enemyTexture.Height >= playableArea.Y + (int)(playableArea.Height * 0.99))
                    {
                        playerHP -= 10; // Reduce HP by 10, adjust as needed
                        enemy.IsActive = false; // Optionally remove the enemy
                        enemies.RemoveAt(i);

                        // If HP drops to zero or below, trigger game over
                        if (playerHP <= 0)
                        {
                            currentState = GameState.GameOver;
                            break;
                        }
                    }
                }


                // Timer-based enemy spawning
                float spawnInterval = baseSpawnInterval / currentLevel; // Faster spawn at higher levels
                spawnTimer -= deltaTime;
                if (spawnTimer <= 0f)
                {
                    SpawnEnemy();
                    spawnTimer = spawnInterval;
                }

                // Update bullets
                for (int i = bullets.Count - 1; i >= 0; i--)
                {
                    bullets[i].Update(deltaTime);
                    if (!bullets[i].IsActive)
                        bullets.RemoveAt(i);
                }
                // Update enemies
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    enemies[i].Update(deltaTime);
                    if (!enemies[i].IsActive)
                    {
                        enemies.RemoveAt(i);
                    }
                }
                // Check collisions between bullets and enemies
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Enemy enemy = enemies[i];
                    Rectangle enemyBounds = enemy.GetBounds();
                    for (int j = bullets.Count - 1; j >= 0; j--)
                    {
                        Bullet bullet = bullets[j];
                        Rectangle bulletBounds = new Rectangle((int)bullet.Position.X, (int)bullet.Position.Y, bulletTexture.Width, bulletTexture.Height);
                        if (enemyBounds.Intersects(bulletBounds))
                        {
                            enemy.IsActive = false;
                            bullet.IsActive = false;
                            // Increase score: points scale with the current level
                            score += basePoints * currentLevel;
                            // Create explosion particles
                            for (int y = 0; y < 50; y++)
                            {
                                float angle = (float)(random.NextDouble() * MathHelper.TwoPi);
                                float speed = (float)(random.NextDouble() * 150 + 50);
                                Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;
                                float lifetime = (float)(random.NextDouble() * 0.5 + 0.5);
                                float size = (float)(random.NextDouble() * 0.5 + 0.5);
                                particleSystem.AddParticle(bullet.Position, velocity, lifetime, Color.Red, size);
                            }
                            soundInstance2.Volume = 0.3f;
                            soundInstance2.Play();
                            break;
                        }
                    }
                }
                // Check if it's time to level up
                if (score >= scoreForNextLevel)
                {
                    score -= scoreForNextLevel; // Optionally carry over extra points

                    // If the next level exceeds maxLevel, the player wins
                    if (currentLevel >= maxLevel)
                    {
                        currentState = GameState.Victory;
                    }
                    else
                    {
                        currentLevel++;
                        // Quadratic scaling: new threshold increases significantly each level
                        scoreForNextLevel = baseThreshold * currentLevel * currentLevel;
                        enemySpeed *= 1.1f;  // Increase enemy speed by 10%
                    }
                }

                // Process game-specific input and update other systems only in Playing state
                ProcessInput(currentKeyboardState, gameTime);
                UpdateBullets(gameTime);
                UpdateParticles(gameTime);
                UpdateRecoil(gameTime);
            }
            else if (currentState == GameState.Options)
            {
                // Options menu logic: press Escape to return to the main menu
                if (currentKeyboardState.IsKeyDown(Keys.Escape) && previousKeyboardState.IsKeyUp(Keys.Escape))
                {
                    currentState = GameState.Menu;
                }
            }
            else if (currentState == GameState.Victory)
            {
                SaveHighScore();
                if (currentKeyboardState.IsKeyDown(Keys.Enter) && previousKeyboardState.IsKeyUp(Keys.Enter))
                {
                    // Reset game variables if necessary
                    score = 0;
                    currentLevel = selectedLevel;  // or reset to 1
                    enemySpeed = 100f;
                    scoreForNextLevel = baseThreshold;
                    enemies.Clear();
                    bullets.Clear();
                    currentState = GameState.Menu;
                }
                if (currentState == GameState.Victory || currentState == GameState.GameOver)
                {
                    if (currentKeyboardState.IsKeyDown(Keys.Enter) && previousKeyboardState.IsKeyUp(Keys.Enter))
                    {
                        // Reset game variables
                        score = 0;
                        playerHP = 100; // Reset HP
                        currentLevel = selectedLevel;  // or reset to a default level like 1
                        enemySpeed = 100f;
                        scoreForNextLevel = baseThreshold;
                        enemies.Clear();
                        bullets.Clear();
                        currentState = GameState.Menu;
                    }
                }

            }
            else if (currentState == GameState.GameOver)
            {
                SaveHighScore();
                if (currentKeyboardState.IsKeyDown(Keys.Enter) && previousKeyboardState.IsKeyUp(Keys.Enter))
                {
                    // Optionally reset game variables
                    score = 0;
                    currentLevel = selectedLevel;  // or reset to a default level like 1
                    enemySpeed = 100f;
                    scoreForNextLevel = baseThreshold;
                    enemies.Clear();
                    bullets.Clear();
                    currentState = GameState.Menu;
                }
            }

            // Store current keyboard state for next frame
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
            if (currentState == GameState.Menu)
            {
                // Draw a title (optional)
                spriteBatch.DrawString(scoreFont, "My Awesome Game", new Vector2(100, 50), Color.White);
                // Draw the high score at a fixed position
                spriteBatch.DrawString(scoreFont, "High Score: " + highScore, new Vector2(100, 100), Color.White);
                // Draw menu items
                for (int i = 0; i < menuItems.Length; i++)
                {
                    Color itemColor = (i == menuSelectionIndex) ? Color.Yellow : Color.White;
                    Vector2 position = new Vector2(100, 150 + i * 40);
                    spriteBatch.DrawString(scoreFont, menuItems[i], position, itemColor);
                }
                // If the "Start Game" option is highlighted, show the current level selection
                if (menuSelectionIndex == 0)
                {
                    spriteBatch.DrawString(scoreFont, "Start at Level: " + selectedLevel, new Vector2(100, 150 + menuItems.Length * 40), Color.White);
                }
            }
            else if (currentState == GameState.Playing)
            {
                // Apply recoil offset when active
                Vector2 drawPosition = isRecoilActive ? spaceshipPosition + recoilOffset : spaceshipPosition;
                spriteBatch.Draw(spaceshipTexture, drawPosition, Color.White);
                particleSystem.Draw(spriteBatch);
                // Draw bullets
                float bulletScale = 0.5f;
                foreach (var bullet in bullets)
                {
                    spriteBatch.Draw(bulletTexture,
                                     bullet.Position,
                                     null,
                                     Color.White,
                                     0f,
                                     new Vector2(bulletTexture.Width / 2, bulletTexture.Height / 2),
                                     bulletScale,
                                     SpriteEffects.None,
                                     0f);
                    int scaledWidth = (int)(bulletTexture.Width * bulletScale);
                    int scaledHeight = (int)(bulletTexture.Height * bulletScale);
                    Rectangle bulletBounds = new Rectangle(
                        (int)(bullet.Position.X - scaledWidth / 2f),
                        (int)(bullet.Position.Y - scaledHeight / 2f),
                        scaledWidth,
                        scaledHeight
                    );
                    spriteBatch.Draw(boundingBoxTexture, bulletBounds, Color.Red * 0.5f);
                }
                foreach (Enemy enemy in enemies)
                {
                    enemy.Draw(spriteBatch);
                }
                // Draw score and level on top
                spriteBatch.DrawString(scoreFont, "Score: " + score, new Vector2(10, 10), Color.White);
                spriteBatch.DrawString(scoreFont, "Level: " + currentLevel, new Vector2(10, 40), Color.White);
                // Draw left and right borders for the playable area
                Texture2D sideBorderTexture = new Texture2D(GraphicsDevice, 1, 1);
                sideBorderTexture.SetData(new[] { Color.Yellow });
                // Left border
                spriteBatch.Draw(sideBorderTexture, new Rectangle(playableArea.X, playableArea.Y, 2, playableArea.Height), Color.Yellow);
                // Right border
                spriteBatch.Draw(sideBorderTexture, new Rectangle(playableArea.X + playableArea.Width - 2, playableArea.Y, 2, playableArea.Height), Color.Yellow);
            }
            else if (currentState == GameState.Options)
            {
                // Draw Options screen
                spriteBatch.DrawString(scoreFont, "Options - Press Escape to return", new Vector2(100, 150), Color.White);
            }
            else if (currentState == GameState.Victory)
            {
                spriteBatch.DrawString(scoreFont, "Congratulations!", new Vector2(100, 100), Color.Green);
                spriteBatch.DrawString(scoreFont, "You have completed all levels!", new Vector2(100, 150), Color.Green);
                spriteBatch.DrawString(scoreFont, "Final Score: " + score, new Vector2(100, 200), Color.Green);
                spriteBatch.DrawString(scoreFont, "Press Enter to return to the Menu", new Vector2(100, 250), Color.Yellow);
            }
            else if (currentState == GameState.GameOver)
            {
                spriteBatch.DrawString(scoreFont, "Game Over!", new Vector2(100, 100), Color.Red);
                spriteBatch.DrawString(scoreFont, "Final Score: " + score, new Vector2(100, 150), Color.Red);
                spriteBatch.DrawString(scoreFont, "Press Enter to return to the Menu", new Vector2(100, 200), Color.Yellow);
            }
            // Draw score, level, and HP on top
            spriteBatch.DrawString(scoreFont, "Score: " + score, new Vector2(10, 10), Color.White);
            spriteBatch.DrawString(scoreFont, "Level: " + currentLevel, new Vector2(10, 40), Color.White);
            spriteBatch.DrawString(scoreFont, "HP: " + playerHP, new Vector2(10, 70), Color.White);

            spriteBatch.End();
            base.Draw(gameTime);
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
    }
}

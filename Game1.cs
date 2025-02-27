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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace SemestralniPraceC_;

public class Game1 : Game
{

    private ParticleSystem particleSystem;
    private Texture2D particleTexture;
    private Random random = new Random();
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _spaceshipTexture;
    Vector2 position = new Vector2(100, 100); // Default position
    Vector2 shootOffset = new Vector2(0, 5); // Recoil effect (move left)
    bool isShooting = false;
    double shootDuration = 0.1; // Time for the recoil effect
    double elapsedShootTime = 0;
    private List<Bullet> _bullets = new List<Bullet>();
    private Texture2D _bulletTexture;
    private float _bulletSpeed = 600f; // Speed of bullets




    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Set 2K resolution
        _graphics.PreferredBackBufferWidth = 2560;
        _graphics.PreferredBackBufferHeight = 1440;
        _graphics.IsFullScreen = true; // Set to true for fullscreen
        _graphics.ApplyChanges(); // Apply resolution settings

        base.Initialize();
    }



    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _spaceshipTexture = Content.Load<Texture2D>("1"); // Filename without extension
        particleTexture = new Texture2D(GraphicsDevice, 2, 2);
        particleTexture.SetData(new Color[] { Color.White, Color.White, Color.White, Color.White });

        particleSystem = new ParticleSystem(particleTexture);
        _bulletTexture = Content.Load<Texture2D>("bulletSpace");

        // Get screen size dynamically
        int screenWidth = _graphics.PreferredBackBufferWidth;
        int screenHeight = _graphics.PreferredBackBufferHeight;

        // Set spaceship position after texture is loaded
        position = new Vector2(screenWidth / 2 - _spaceshipTexture.Width / 2,
                               screenHeight - _spaceshipTexture.Height - 50);
    }






    protected override void Update(GameTime gameTime)
    {
        // Define movement speed
        float moveSpeed = 600f; // Adjust speed as needed

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        KeyboardState keyboard = Keyboard.GetState();

        // Move left
        if (keyboard.IsKeyDown(Keys.A))
        {
            position.X -= moveSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        // Move right
        if (keyboard.IsKeyDown(Keys.D))
        {
            position.X += moveSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        // Detect shooting (Space key)
        if (keyboard.IsKeyDown(Keys.Space))
        {
            isShooting = true;
            elapsedShootTime = 0; // Reset timer

            Vector2 gunPosition = new Vector2(position.X + _spaceshipTexture.Width / 2 - _bulletTexture.Width / 2 + 14, position.Y);

            _bullets.Add(new Bullet(gunPosition, new Vector2(0, -_bulletSpeed))); // Moves up

            // Update bullets
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                _bullets[i].Update((float)gameTime.ElapsedGameTime.TotalSeconds);

                // Remove inactive bullets
                if (!_bullets[i].IsActive)
                    _bullets.RemoveAt(i);
            }

            // Spawn 5 particles per shot
            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = new Vector2((float)(random.NextDouble() * 2 - 1) * 50, -random.Next(20, 50)); // Random spread
                float lifetime = (float)(random.NextDouble() * 0.5 + 0.3); // Random lifetime (0.3 - 0.8 sec)
                float size = (float)(random.NextDouble() * 0.5 + 0.5); // Random size (0.5 - 1.0)

                particleSystem.AddParticle(gunPosition, velocity, lifetime, Color.OrangeRed, size);
            }
        }

        // Update particles
        particleSystem.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        // Manage recoil effect duration
        if (isShooting)
        {
            elapsedShootTime += gameTime.ElapsedGameTime.TotalSeconds;

            // Reset position after recoil effect time
            if (elapsedShootTime >= shootDuration)
            {
                isShooting = false;
            }
        }

        base.Update(gameTime);

    }






    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        // Apply recoil effect only when shooting
        Vector2 drawPosition = isShooting ? position + shootOffset : position;
        _spriteBatch.Draw(_spaceshipTexture, drawPosition, Color.White);

        // Draw particles
        particleSystem.Draw(_spriteBatch);

        float bulletScale = 0.5f; // Adjust as needed
        foreach (var bullet in _bullets)
        {
            _spriteBatch.Draw(_bulletTexture, bullet.Position, null, Color.White, 0f,
                              new Vector2(_bulletTexture.Width / 2, _bulletTexture.Height / 2),
                              bulletScale, SpriteEffects.None, 0f);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}

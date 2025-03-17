using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Enemy
{
    public Texture2D Texture { get; private set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public bool IsActive { get; set; }
    public float Scale { get; set; } = 0.5f; // Default scale

    public Enemy(Texture2D texture, Vector2 startPosition, Vector2 velocity, float scale = 0.5f)
    {
        Texture = texture;
        Position = startPosition;
        Velocity = velocity;
        IsActive = true;
        Scale = scale;
    }

    public void Update(float deltaTime)
    {
        Position += Velocity * deltaTime;
        // Deactivate enemy if it goes off-screen, for example.
        if (Position.Y > 1440 || Position.Y < -Texture.Height ||
            Position.X > 2560 || Position.X < -Texture.Width)
        {
            IsActive = false;
        }
    }
    public Rectangle GetBounds()
    {
        return new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            (int)(Texture.Width * Scale),
            (int)(Texture.Height * Scale));
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (IsActive)
        {
            spriteBatch.Draw(Texture, Position, null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
    }
}

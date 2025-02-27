using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Bullet
{
    public Vector2 Position;
    public Vector2 Velocity;
    public bool IsActive;

    public Bullet(Vector2 position, Vector2 velocity)
    {
        Position = position;
        Velocity = velocity;
        IsActive = true;
    }

    public void Update(float deltaTime)
    {
        Position += Velocity * deltaTime;
        
        // Deactivate bullet if it moves off-screen
        if (Position.Y < -10)
            IsActive = false;
    }
}

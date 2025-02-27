using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
public class Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float LifeTime;
    public Color Color;
    public float Size;

    public Particle(Vector2 position, Vector2 velocity, float lifetime, Color color, float size)
    {
        Position = position;
        Velocity = velocity;
        LifeTime = lifetime;
        Color = color;
        Size = size;
    }

    public void Update(float deltaTime)
    {
        Position += Velocity * deltaTime;
        LifeTime -= deltaTime;
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
public class ParticleSystem
{
    private List<Particle> particles = new List<Particle>();
    private Texture2D texture;

    public ParticleSystem(Texture2D texture)
    {
        this.texture = texture;
    }

    public void AddParticle(Vector2 position, Vector2 velocity, float lifetime, Color color, float size)
    {
        particles.Add(new Particle(position, velocity, lifetime, color, size));
    }

    public void Update(float deltaTime)
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            particles[i].Update(deltaTime);
            if (particles[i].LifeTime <= 0)
                particles.RemoveAt(i);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var particle in particles)
        {
            spriteBatch.Draw(texture, particle.Position, null, particle.Color, 0f,
                             new Vector2(texture.Width / 2, texture.Height / 2), 
                             particle.Size, SpriteEffects.None, 0f);
        }
    }
}

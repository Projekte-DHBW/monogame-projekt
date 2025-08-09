using GameLibrary;
using GameLibrary.Graphics;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using Microsoft.Xna.Framework;

namespace DHBW_Game.GameObjects;

public class CircleColliderTest : PhysicsComponent
{
    // The AnimatedSprite used as a test texture.
    private readonly AnimatedSprite _sprite;

    /// <summary>
    /// Creates a new <see cref="CircleColliderTest"/> object.
    /// </summary>
    /// <param name="mass">The mass of the physics component.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    public CircleColliderTest(float mass, bool isElastic)
    {
        Collider = new CircleCollider((int)Position.X, (int)Position.Y, 30, this, isElastic);
        
        Mass = mass;
    }
    
    /// <summary>
    /// Creates a new <see cref="CircleColliderTest"/> object.
    /// </summary>
    /// <param name="mass">The mass of the physics component.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    /// <param name="sprite">The animated sprite to use.</param>
    public CircleColliderTest(float mass, bool isElastic, AnimatedSprite sprite) : this(mass, isElastic)
    {
        _sprite = sprite;
    }
    
    /// <summary>
    /// Initializes the circle collider test object at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the circle collider test object should spawn.</param>
    public void Initialize(Vector2 startingPosition)
    {
        Position = startingPosition;
    }

    /// <summary>
    /// Updates the animated sprite which is set.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public void Update(GameTime gameTime)
    {
        // Update the animated sprite.
        _sprite?.Update(gameTime);
    }

    /// <summary>
    /// Draws the animated sprite which is set.
    /// </summary>
    public void Draw()
    {
        _sprite?.Draw(Core.SpriteBatch, Position);
    }
}
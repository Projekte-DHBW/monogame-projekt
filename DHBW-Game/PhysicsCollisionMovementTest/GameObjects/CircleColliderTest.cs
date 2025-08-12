using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using Microsoft.Xna.Framework;

namespace DHBW_Game.GameObjects;

public class CircleColliderTest : GameObject
{
    /// <summary>
    /// Creates a new <see cref="CircleColliderTest"/> object.
    /// </summary>
    /// <param name="mass">The mass of the physics component.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    public CircleColliderTest(float mass, bool isElastic)
    {
        Collider = new CircleCollider(this, new Vector2(0, 0), 30, isElastic);
        
        PhysicsComponent = new PhysicsComponent(this, mass);
        
        ServiceLocator.Get<PhysicsEngine>().Add(PhysicsComponent);
    }
    
    /// <summary>
    /// Initializes the circle collider test object at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the circle collider test object should spawn.</param>
    public override void Initialize(Vector2 startingPosition)
    { 
        base.Initialize(startingPosition);
    }

    /// <summary>
    /// Updates the sprite which is set.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    /// <summary>
    /// Draws the sprite which is set.
    /// </summary>
    public override void Draw()
    {
        base.Draw();
    }
}
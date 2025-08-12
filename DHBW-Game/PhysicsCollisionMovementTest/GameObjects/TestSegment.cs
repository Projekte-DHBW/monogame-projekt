using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Physics.Colliders;
using Microsoft.Xna.Framework;

namespace DHBW_Game.GameObjects;

public class TestSegment : GameObject
{
    /// <summary>
    /// Creates a new <see cref="TestSegment"/> object.
    /// </summary>
    /// <param name="width">The width of the test segment.</param>
    /// <param name="height">The height of the test segment.</param>
    /// <param name="rotation">The rotation of the test segment.</param>
    public TestSegment(int width, int height, int rotation)
    {
        Collider = new RectangleCollider(this, Vector2.Zero, width, height, rotation);
        
        ServiceLocator.Get<CollisionEngine>().Add(Collider);
    }
    
    /// <summary>
    /// Initializes the test segment at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the test segment should spawn.</param>
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
using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Physics.Colliders;
using Microsoft.Xna.Framework;

namespace DHBW_Game.GameObjects;

public class Segment : GameObject
{
    /// <summary>
    /// Creates a new <see cref="Segment"/> object.
    /// </summary>
    /// <param name="width">The width of the segment.</param>
    /// <param name="height">The height of the segment.</param>
    /// <param name="rotation">The rotation of the segment.</param>
    /// <param name="isElastic">Whether the segment is elastic. If either the segment or the game object colliding with it is elastic, the collision is elastic.</param>
    /// <param name="frictionCoefficient">The friction coefficient of this segment. A higher value leads to higher friction and decelerates game objects on the surface if there is no force accelerating them.</param>
    public Segment(int width, int height, float rotation, bool isElastic, float frictionCoefficient)
    {
        Collider = new RectangleCollider(this, Vector2.Zero, width, height, rotation, isElastic, frictionCoefficient);
        Collider.CanBeGround = true;
        
        ServiceLocator.Get<CollisionEngine>().Add(Collider);
    }
    
    /// <summary>
    /// Initializes the <see cref="Segment"/> object at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the <see cref="Segment"/> object should spawn.</param>
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
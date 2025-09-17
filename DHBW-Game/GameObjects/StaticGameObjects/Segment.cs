using System;
using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Physics.Colliders;
using GameLibrary.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DHBW_Game.GameObjects;

public class Segment : GameObject
{
    public Texture2D Texture { get; set; }

    /// <summary>
    /// Creates a new <see cref="Segment"/> object.
    /// </summary>
    /// <param name="width">The width of the segment.</param>
    /// <param name="height">The height of the segment.</param>
    /// <param name="rotation">The rotation of the segment in degrees.</param>
    /// <param name="isElastic">Whether the segment is elastic. If either the segment or the game object colliding with it is elastic, the collision is elastic.</param>
    /// <param name="frictionCoefficient">The friction coefficient of this segment. A higher value leads to higher friction and decelerates game objects on the surface if there is no force accelerating them.</param>
    public Segment(int width, int height, float rotation, bool isElastic, float frictionCoefficient)
    {
        Collider = new RectangleCollider(this, Vector2.Zero, width, height, rotation, isElastic, frictionCoefficient);
        Collider.CanBeGround = true;

        ServiceLocator.Get<CollisionEngine>().Add(Collider);
    }

    /// <summary>
    /// Creates a new <see cref="Segment"/> object.
    /// </summary>
    /// <param name="width">The width of the segment.</param>
    /// <param name="height">The height of the segment.</param>
    /// <param name="rotation">The rotation of the segment in degrees.</param>
    /// <param name="isElastic">Whether the segment is elastic. If either the segment or the game object colliding with it is elastic, the collision is elastic.</param>
    /// <param name="frictionCoefficient">The friction coefficient of this segment. A higher value leads to higher friction and decelerates game objects on the surface if there is no force accelerating them.</param>
    /// <param name="texture">The texture being used to draw the segment.</param>
    public Segment(int width, int height, float rotation, bool isElastic, float frictionCoefficient, Texture2D texture) : this(width, height, rotation, isElastic, frictionCoefficient)
    {
        Texture = texture;
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
    /// Draws the texture which is set.
    /// </summary>
    public override void Draw()
    {
        base.Draw();

        if (Texture != null && Collider is RectangleCollider rect)
        {
            Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);
            Vector2 scale = new Vector2(rect.Width / (float)Texture.Width, rect.Height / (float)Texture.Height);
            float rotation = (float)(rect.Rotation * Math.PI/180f); // Convert degrees to radians
            ServiceLocator.Get<Camera>().Draw(
                Core.SpriteBatch,
                Texture,
                Position,
                null,
                Color.White,
                rotation,
                origin,
                scale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
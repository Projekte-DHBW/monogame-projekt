using GameLibrary.Entities;
using GameLibrary.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.Physics.Colliders;

public class CircleCollider : Collider
{
    /// <summary>
    /// Gets the radius of the circle collider.
    /// </summary>
    public int Radius { get; init; }
    
    /// <summary>
    /// The texture used to visualize the circle collider.
    /// </summary>
    private static Texture2D _texture;
    
    /// <summary>
    /// Creates a new <see cref="CircleCollider"/>.
    /// </summary>
    /// <param name="gameObject">The game object which the collider is attached to.</param>
    /// <param name="localPosition">The local position of the collider center relative to the parent game object.</param>
    /// <param name="radius">The length from the center of the circle collider to the edge.</param>
    public CircleCollider(GameObject gameObject, Vector2 localPosition, int radius) : base(gameObject, localPosition)
    {
        Radius = radius;
        SetTexture();
    }
    
    /// <summary>
    /// Creates a new <see cref="CircleCollider"/>.
    /// </summary>
    /// <param name="gameObject">The game object which the collider is attached to.</param>
    /// <param name="localPosition">The local position of the collider center relative to the parent game object.</param>
    /// <param name="radius">The length from the center of the circle collider to the edge.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    public CircleCollider(GameObject gameObject, Vector2 localPosition, int radius, bool isElastic) : base(gameObject, localPosition, isElastic)
    {
        Radius = radius;
        SetTexture();
    }
    
    /// <summary>
    /// Creates a new <see cref="CircleCollider"/>.
    /// </summary>
    /// <param name="gameObject">The game object which the collider is attached to.</param>
    /// <param name="localPosition">The local position of the collider center relative to the parent game object.</param>
    /// <param name="radius">The length from the center of the circle collider to the edge.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    /// <param name="frictionCoefficient">The friction coefficient of the collider.</param>
    public CircleCollider(GameObject gameObject, Vector2 localPosition, int radius, bool isElastic, float frictionCoefficient) : base(gameObject, localPosition, isElastic, frictionCoefficient)
    {
        Radius = radius;
        SetTexture();
    }

    /// <summary>
    /// Set the texture which is used to visualize the circle collider.
    /// </summary>
    private void SetTexture()
    {
        // Create new texture with the correct dimensions
        int textureWidth = Radius * 2;
        _texture = new Texture2D(Core.GraphicsDevice, textureWidth, textureWidth);
        Color[] data = new Color[textureWidth * textureWidth];

        // Calculate which pixels are inside the circle
        for (int x = 0; x < textureWidth; x++)
        {
            for (int y = 0; y < textureWidth; y++)
            {
                // Check if the pixel is within the circle's radius
                float dx = x - Radius;
                float dy = y - Radius;
                if (dx * dx + dy * dy <= Radius * Radius)
                {
                    data[x + y * textureWidth] = Color.White; // Inside circle: white
                }
                else
                {
                    data[x + y * textureWidth] = Color.Transparent; // Outside circle: transparent
                }
            }
        }

        // Set the texture data
        _texture.SetData(data);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // Draw the circle collider visualization
        ServiceLocator.Get<Camera>().Draw(
            spriteBatch,
            _texture, // The circle texture
            GlobalPosition, // Position (center of the circle)
            null, // Source rectangle (null to use full texture)
            Color.Red, // Tint color
            0f, // Rotation
            new Vector2(Radius, Radius), // Origin (center of the texture)
            new Vector2(1, 1), // Scale
            SpriteEffects.None,
            0f // Layer depth
        );
    }
}
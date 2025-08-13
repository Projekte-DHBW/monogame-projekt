using GameLibrary.Entities;
using GameLibrary.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.Physics.Colliders;

public class RectangleCollider : Collider
{
    /// <summary>
    /// Gets the width of the rectangle collider.
    /// </summary>
    public int Width { get; init; }
    
    /// <summary>
    /// Gets the height of the rectangle collider.
    /// </summary>
    public int Height { get; init; }
    
    /// <summary>
    /// Gets the rotation of the rectangle collider.
    /// </summary>
    public float Rotation { get; init; }
    
    /// <summary>
    /// The texture used to visualize the rectangle collider.
    /// </summary>
    private static Texture2D _texture;
    
    /// <summary>
    /// Creates a new <see cref="RectangleCollider"/>.
    /// </summary>
    /// <param name="gameObject">The game object which the collider is attached to.</param>
    /// <param name="localPosition">The local position of the collider center relative to the parent game object.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <param name="rotation">The rotation of the rectangle.</param>
    public RectangleCollider(GameObject gameObject, Vector2 localPosition, int width, int height, float rotation) : base(gameObject, localPosition)
    {
        Width = width;
        Height = height;
        Rotation = rotation;
        SetTexture();
    }
    
    /// <summary>
    /// Creates a new <see cref="RectangleCollider"/>.
    /// </summary>
    /// <param name="gameObject">The game object which the collider is attached to.</param>
    /// <param name="localPosition">The local position of the collider center relative to the parent game object.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <param name="rotation">The rotation of the rectangle.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    public RectangleCollider(GameObject gameObject, Vector2 localPosition, int width, int height, float rotation, bool isElastic) : base(gameObject, localPosition, isElastic)
    {
        Width = width;
        Height = height;
        Rotation = rotation;
        SetTexture();
    }
    
    /// <summary>
    /// Creates a new <see cref="RectangleCollider"/>.
    /// </summary>
    /// <param name="gameObject">The game object which the collider is attached to.</param>
    /// <param name="localPosition">The local position of the collider center relative to the parent game object.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <param name="rotation">The rotation of the rectangle.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    /// <param name="frictionCoefficient">The friction coefficient of the collider.</param>
    public RectangleCollider(GameObject gameObject, Vector2 localPosition, int width, int height, float rotation, bool isElastic, float frictionCoefficient) : base(gameObject, localPosition, isElastic, frictionCoefficient)
    {
        Width = width;
        Height = height;
        Rotation = rotation;
        SetTexture();
    }
    
    /// <summary>
    /// Set the texture which is used to visualize the rectangle collider.
    /// </summary>
    private void SetTexture()
    {
        // Create a 1x1 pixel texture
        _texture = new Texture2D(Core.GraphicsDevice, 1, 1);
        _texture.SetData(new[] { Color.White }); // Set the pixel color to white
    }
    
    public override void Draw(SpriteBatch spriteBatch)
    {
        // Convert the rotation of the rectangle collider from degrees to radians
        float rotation = MathHelper.ToRadians(Rotation);
        
        // Draw the rectangle collider visualization
        ServiceLocator.Get<Camera>().Draw(
            spriteBatch,
            _texture, // The 1x1 texture
            GlobalPosition, // Position (center of the rectangle)
            null, // Source rectangle (null to use full texture)
            Color.Red, // Tint color
            rotation, // Rotation in radians
            new Vector2(0.5f, 0.5f), // Origin (center of the 1x1 texture)
            new Vector2(Width, Height), // Scale (width and height of the rectangle)
            SpriteEffects.None,
            0f // Layer depth
        );
    }
}
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
    /// Gets the y-coordinate of the highest point on this circle collider.
    /// </summary>
    public int Top => Y - Radius;
    
    /// <summary>
    /// Gets the y-coordinate of the lowest point on this circle collider.
    /// </summary>
    public int Bottom => Y + Radius;
    
    /// <summary>
    /// Gets the x-coordinate of the leftmost point on this circle collider.
    /// </summary>
    public int Left => X - Radius;
    
    /// <summary>
    /// Gets the x-coordinate of the rightmost point on this circle collider.
    /// </summary>
    public int Right => X + Radius;
    
    /// <summary>
    /// The texture used to visualize the circle collider.
    /// </summary>
    private static Texture2D _texture;
    
    /// <summary>
    /// Creates a new circle collider located at the specified position.
    /// </summary>
    /// <param name="x">The x-coordinate of the collider center.</param>
    /// <param name="y">The x-coordinate of the collider center.</param>
    /// <param name="radius">The length from the center of the circle collider to the edge.</param>
    public CircleCollider(int x, int y, int radius) : base(x, y)
    {
        Radius = radius;
        SetTexture();
    }
    
    /// <summary>
    /// Creates a new circle collider located at the specified position.
    /// </summary>
    /// <param name="x">The x-coordinate of the collider center.</param>
    /// <param name="y">The x-coordinate of the collider center.</param>
    /// <param name="radius">The length from the center of the circle collider to the edge.</param>
    /// <param name="physicsComponent">The physics component associated with this collider.</param>
    public CircleCollider(int x, int y, int radius, PhysicsComponent physicsComponent) : base(x, y, physicsComponent)
    {
        Radius = radius;
        SetTexture();
    }
    
    /// <summary>
    /// Creates a new circle collider located at the specified position.
    /// </summary>
    /// <param name="x">The x-coordinate of the collider center.</param>
    /// <param name="y">The x-coordinate of the collider center.</param>
    /// <param name="radius">The length from the center of the circle collider to the edge.</param>
    /// <param name="physicsComponent">The physics component associated with this collider.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    public CircleCollider(int x, int y, int radius, PhysicsComponent physicsComponent, bool isElastic) : base(x, y, physicsComponent, isElastic)
    {
        Radius = radius;
        SetTexture();
    }
    
    /// <summary>
    /// Creates a new circle collider located at the specified position.
    /// </summary>
    /// <param name="x">The x-coordinate of the collider center.</param>
    /// <param name="y">The x-coordinate of the collider center.</param>
    /// <param name="radius">The length from the center of the circle collider to the edge.</param>
    /// <param name="physicsComponent">The physics component associated with this collider.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    /// <param name="frictionCoefficient">The friction coefficient of the collider.</param>
    public CircleCollider(int x, int y, int radius, PhysicsComponent physicsComponent, bool isElastic, float frictionCoefficient) : base(x, y, physicsComponent, isElastic, frictionCoefficient)
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
        spriteBatch.Draw(
            _texture, // The circle texture
            new Vector2(X, Y), // Position (center of the circle)
            null, // Source rectangle (null to use full texture)
            Color.Red, // Tint color
            0f, // Rotation
            new Vector2(Radius, Radius), // Origin (center of the texture)
            1, // Scale
            SpriteEffects.None,
            0f // Layer depth
        );
    }
}
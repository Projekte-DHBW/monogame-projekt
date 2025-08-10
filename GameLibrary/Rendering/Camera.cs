using GameLibrary.Entities;
using GameLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.Rendering;

/// <summary>
/// Represents a camera that can transform world-space coordinates to screen-space for rendering,
/// with support for following game objects and applying offsets for centered views.
/// </summary>
public class Camera
{
    /// <summary>
    /// Gets or sets the position of the camera in world space. This represents the center point of the view.
    /// </summary>
    public Vector2 Position { get; set; }
    
    /// <summary>
    /// The percentage of the viewport size to use as the dead zone (0.0 to 1.0). 
    /// e.g., 0.2 means 20% of width/height as the central area where no camera movement occurs.
    /// </summary>
    public float DeadZonePercentage { get; set; } = 0.2f;
    
    /// <summary>
    /// Gets the offset to transform world positions to screen positions, centering the camera's Position.
    /// </summary>
    public Vector2 Offset
    {
        get
        {
            Vector2 viewportCenter = new Vector2(Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height) * 0.5f;
            return -Position + viewportCenter;
        }
    }
    
    /// <summary>
    /// The game object being followed by the camera, if any.
    /// </summary>
    private GameObject _target;
    
    /// <summary>
    /// Sets the camera to follow the specified game object with a drag effect.
    /// </summary>
    /// <param name="gameObject">The game object to follow (or null to stop following).</param>
    public void Follow(GameObject gameObject)
    {
        _target = gameObject;
    }
    
    /// <summary>
    /// Updates the camera's position based on the followed target, if any.
    /// This has to be called each frame in the game's Update loop.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public void Update(GameTime gameTime)
    {
        if (_target == null) return;

        Vector2 viewportSize = new Vector2(Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        Vector2 viewportCenter = viewportSize * 0.5f;
        Vector2 deadZoneSize = viewportSize * DeadZonePercentage;

        // Compute the target's position in screen space (relative to current camera view)
        Vector2 targetScreenPos = _target.Position - Position + viewportCenter;

        // Dead zone boundaries (centered on screen)
        float deadZoneLeft = viewportCenter.X - deadZoneSize.X * 0.5f;
        float deadZoneRight = viewportCenter.X + deadZoneSize.X * 0.5f;
        float deadZoneTop = viewportCenter.Y - deadZoneSize.Y * 0.5f;
        float deadZoneBottom = viewportCenter.Y + deadZoneSize.Y * 0.5f;

        // Calculate how much to move the camera (only if target is outside dead zone)
        Vector2 delta = Vector2.Zero;
        if (targetScreenPos.X < deadZoneLeft)
        {
            delta.X = targetScreenPos.X - deadZoneLeft;
        }
        else if (targetScreenPos.X > deadZoneRight)
        {
            delta.X = targetScreenPos.X - deadZoneRight;
        }

        if (targetScreenPos.Y < deadZoneTop)
        {
            delta.Y = targetScreenPos.Y - deadZoneTop;
        }
        else if (targetScreenPos.Y > deadZoneBottom)
        {
            delta.Y = targetScreenPos.Y - deadZoneBottom;
        }

        // Apply the delta to move the camera (brings target back to dead zone edge)
        Position += delta;
    }

    /// <summary>
    /// Draws a sprite at the specified world position, adjusted by the camera's offset.
    /// </summary>
    /// <param name="sprite">The sprite to draw.</param>
    /// <param name="position">The world-space position to draw the sprite at.</param>
    public void Draw(Sprite sprite, Vector2 position)
    {
        if (sprite != null)
        {
            sprite.Draw(Core.SpriteBatch, position + Offset);
        }
    }
    
    /// <summary>
    /// Draws a texture region at the specified world position, adjusted by the camera's offset.
    /// </summary>
    /// <param name="textureRegion">The TextureRegion to draw, containing the source texture and rectangle.</param>
    /// <param name="spriteBatch">The SpriteBatch instance to use for drawing.</param>
    /// <param name="position">The world-space position to draw the texture region at.</param>
    /// <param name="color">The color tint to apply to the texture region.</param>
    /// <param name="rotation">The rotation angle in radians to apply to the texture region.</param>
    /// <param name="origin">The origin point for rotation and scaling, relative to the texture region's top-left corner.</param>
    /// <param name="scale">The scale factor to apply to the x- and y-axes of the texture region.</param>
    /// <param name="effects">Sprite effects such as flipping horizontally or vertically.</param>
    /// <param name="layerDepth">The layer depth for sorting the texture region during rendering.</param>
    public void Draw(
        TextureRegion textureRegion,
        SpriteBatch spriteBatch,
        Vector2 position, 
        Color color, 
        float rotation, 
        Vector2 origin, 
        Vector2 scale, 
        SpriteEffects effects, 
        float layerDepth)
    {
        if (textureRegion != null)
        {
            textureRegion.Draw(spriteBatch, position + Offset, color, rotation, origin, scale, effects, layerDepth);
        }
    }

    /// <summary>
    /// Draws a texture at the specified world position using the provided SpriteBatch, 
    /// adjusted by the camera's offset.
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch instance to use for drawing.</param>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="position">The world-space position to draw the texture at.</param>
    /// <param name="sourceRectangle">The optional source rectangle within the texture.</param>
    /// <param name="color">The color tint to apply.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="origin">The origin point for rotation and scaling.</param>
    /// <param name="scale">The scale factor to apply.</param>
    /// <param name="effects">Sprite effects such as flipping.</param>
    /// <param name="layerDepth">The layer depth for sorting.</param>
    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth)
    {
        spriteBatch.Draw(texture, position + Offset, sourceRectangle, color, rotation, origin, scale, effects, layerDepth);
    }
}
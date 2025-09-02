using GameLibrary.Graphics;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Rendering;
using Microsoft.Xna.Framework;

namespace GameLibrary.Entities;

public abstract class GameObject
{
    /// <summary>
    /// Gets or sets the physics component of the game object.
    /// </summary>
    public PhysicsComponent PhysicsComponent {get; set; }
    
    /// <summary>
    /// Gets or sets the collider of the game object.
    /// </summary>
    public Collider Collider {get; set; }
    
    /// <summary>
    /// Gets or sets the sprite of the game object.
    /// </summary>
    public Sprite Sprite {get; set; }
    
    /// <summary>
    /// Gets or sets the position of the game object. This position is also used as the position for the collider and the physics component to avoid redundancy and synchronization issues.
    /// </summary>
    public Vector2 Position {get; set; }

    /// <summary>
    /// Creates a new <see cref="GameObject"/> object.
    /// </summary>
    /// <param name="physicsComponent">The physics component to attach to this game object.</param>
    /// <param name="collider">The collider to attach to this game object.</param>
    /// <param name="sprite">The sprite for this game object.</param>
    protected GameObject(PhysicsComponent physicsComponent, Collider collider, Sprite sprite)
    {
        PhysicsComponent = physicsComponent;
        Collider = collider;
        Sprite = sprite;
    }
    
    /// <summary>
    /// Creates a new <see cref="GameObject"/> object.
    /// </summary>
    protected GameObject()
    {
        
    }
    
    /// <summary>
    /// Initializes the game object at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the game object should spawn.</param>
    public virtual void Initialize(Vector2 startingPosition)
    {
        Position = startingPosition;
        LoadContent();
    }
    
    public virtual void LoadContent()
    {
    }

    /// <summary>
    /// Updates the sprite which is set.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public virtual void Update(GameTime gameTime)
    {
        // Update the sprite if it's an animated sprite.
        if (Sprite is AnimatedSprite sprite)
        {
            sprite.Update(gameTime);   
        }
    }

    /// <summary>
    /// Draws the sprite which is set.
    /// </summary>
    public virtual void Draw()
    {
        ServiceLocator.Get<Camera>().Draw(Sprite, Position);
    }

    public virtual void TriggerCollision(Collider collider)
    {

    }
}
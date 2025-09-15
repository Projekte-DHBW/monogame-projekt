using GameLibrary.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.Physics.Colliders;

public abstract class Collider
{
    /// <summary>
    /// Gets or sets the local position of the collider center relative to the parent game object.
    /// </summary>
    public Vector2 LocalPosition { get; set; }
    
    /// <summary>
    /// Gets or sets the global position of the collider center. Takes local position into account and adjusts the position of the game object when setting a new value.
    /// </summary>
    public Vector2 GlobalPosition
    {
        get => GameObject.Position + LocalPosition;
        set => GameObject.Position = value - LocalPosition;
    }
    
    /// <summary>
    /// Gets or sets whether the collider is elastic.
    /// This influences whether the collider bounces when colliding with other colliders. If at least one of the colliders is elastic, the collision is elastic.
    /// </summary>
    public bool IsElastic { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether the collider is on the ground, meaning the collider is located on top of another collider (<see cref="GroundCollider"/>) without a collision between these two colliders.
    /// </summary>
    public bool IsOnGround { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the ground collider. This is the collider on which the current collider rests when <see cref="IsOnGround"/> is True.
    /// </summary>
    public Collider GroundCollider { get; set; } = null; // Collider on which the collider is on when IsOnGround is true
    
    /// <summary>
    /// Gets or sets the slope angle of the ground when <see cref="IsOnGround"/> is True. This is important for calculating the sliding motion due to gravity.
    /// </summary>
    public float SlopeAngle { get; set; } = 0f;
    
    /// <summary>
    /// Gets or sets the friction coefficient of the collider.
    /// This is not strictly physically correct as it's not really possible to specify the friction coefficient for one material without knowing the other material.
    /// For the time being, the friction coefficient is only used for movement when <see cref="IsOnGround"/> is True and in that case, the friction coefficient of the <see cref="GroundCollider"/> is used.
    /// </summary>
    public float FrictionCoefficient { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the collision group for this collider (e.g., "player", "enemy", "movable", "default").
    /// Used to determine if a collision pair should be treated as a trigger (event only) or physical.
    /// </summary>
    public string CollisionGroup { get; set; } = "default";

    /// <summary>
    /// Gets or sets whether this collider can act as a ground surface for other colliders (e.g., for jumping or friction).
    /// </summary>
    public bool CanBeGround { get; set; } = false;  // Default to false
    
    /// <summary>
    /// Gets or sets the game object which the collider is attached to.
    /// </summary>
    public GameObject GameObject { get; set; }
    
    /// <summary>
    /// Gets the physics component associated with this collider. Can be null as some static game objects like walls don't have a physics component.
    /// </summary>
    public PhysicsComponent PhysicsComponent => GameObject.PhysicsComponent;

    public abstract void Draw(SpriteBatch spriteBatch);
    
    /// <summary>
    /// Creates a new <see cref="Collider"/>.
    /// </summary>
    /// <param name="gameObject">The game object which the collider is attached to.</param>
    /// <param name="localPosition">The local position of the collider center relative to the parent game object.</param>
    protected Collider(GameObject gameObject, Vector2 localPosition)
    {
        GameObject = gameObject;
        LocalPosition = localPosition;
    }
    
    /// <summary>
    /// Creates a new <see cref="Collider"/>.
    /// </summary>
    /// <param name="gameObject">The game object which the collider is attached to.</param>
    /// <param name="localPosition">The local position of the collider center relative to the parent game object.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    protected Collider(GameObject gameObject, Vector2 localPosition, bool isElastic) : this(gameObject, localPosition)
    {
        IsElastic = isElastic;
    }
    
    /// <summary>
    /// Creates a new <see cref="Collider"/>.
    /// </summary>
    /// <param name="gameObject">The game object which the collider is attached to.</param>
    /// <param name="localPosition">The local position of the collider center relative to the parent game object.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    /// <param name="frictionCoefficient">The friction coefficient of the collider.</param>
    protected Collider(GameObject gameObject, Vector2 localPosition, bool isElastic, float frictionCoefficient) : this(gameObject, localPosition, isElastic)
    {
        FrictionCoefficient = frictionCoefficient;
    }
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.Physics.Colliders;

public abstract class Collider
{
    /// <summary>
    /// Gets or sets the x-coordinate of the collider center.
    /// </summary>
    public int X { get; set; }
    
    /// <summary>
    /// Gets or sets the y-coordinate of the collider center.
    /// </summary>
    public int Y { get; set; }
    
    /// <summary>
    /// Gets the location of the collider center as Point.
    /// </summary>
    public Point Location => new Point(X, Y);
    
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
    public float FrictionCoefficient { get; set; } = 0f;
    
    /// <summary>
    /// Gets or sets the physics component associated with this collider. Can be null as some game objects like walls don't have a physics component.
    /// </summary>
    public PhysicsComponent PhysicsComponent { get; init; }

    public abstract void Draw(SpriteBatch spriteBatch);
    
    /// <summary>
    /// Creates a new collider located at the specified position.
    /// </summary>
    /// <param name="x">The x-coordinate of the collider center.</param>
    /// <param name="y">The x-coordinate of the collider center.</param>
    protected Collider(int x, int y)
    {
        X = x;
        Y = y;
        PhysicsComponent = null;
    }
    
    /// <summary>
    /// Creates a new collider located at the specified position.
    /// </summary>
    /// <param name="x">The x-coordinate of the collider center.</param>
    /// <param name="y">The x-coordinate of the collider center.</param>
    /// <param name="physicsComponent">The physics component associated with this collider.</param>
    protected Collider(int x, int y, PhysicsComponent physicsComponent) : this(x, y)
    {
        PhysicsComponent = physicsComponent;
    }
    
    /// <summary>
    /// Creates a new collider located at the specified position.
    /// </summary>
    /// <param name="x">The x-coordinate of the collider center.</param>
    /// <param name="y">The x-coordinate of the collider center.</param>
    /// <param name="physicsComponent">The physics component associated with this collider.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    protected Collider(int x, int y, PhysicsComponent physicsComponent, bool isElastic) : this(x, y, physicsComponent)
    {
        IsElastic = isElastic;
    }
    
    /// <summary>
    /// Creates a new collider located at the specified position.
    /// </summary>
    /// <param name="x">The x-coordinate of the collider center.</param>
    /// <param name="y">The x-coordinate of the collider center.</param>
    /// <param name="physicsComponent">The physics component associated with this collider.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    /// <param name="frictionCoefficient">The friction coefficient of the collider.</param>
    protected Collider(int x, int y, PhysicsComponent physicsComponent, bool isElastic, float frictionCoefficient) : this(x, y, physicsComponent, isElastic)
    {
        FrictionCoefficient = frictionCoefficient;
    }
}
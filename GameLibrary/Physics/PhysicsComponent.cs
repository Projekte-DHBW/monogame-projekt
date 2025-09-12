using System.Collections.Generic;
using GameLibrary.Entities;
using GameLibrary.Physics.Colliders;
using Microsoft.Xna.Framework;

namespace GameLibrary.Physics;

public class PhysicsComponent
{
    /// <summary>
    /// Gets or sets the position of the physics object. The get and set requests are forwarded to the position of the game object to avoid redundancy and synchronization issues.
    /// </summary>
    public Vector2 Position
    {
        get => GameObject.Position;
        set => GameObject.Position = value;
        
    }
    
    /// <summary>
    /// Gets or sets the velocity of the physics object.
    /// </summary>
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    
    /// <summary>
    /// Gets or sets the new velocity of the physics object. This new velocity is calculated by the physics engine and applied at the end of the physics loop.
    /// </summary>
    public Vector2 NewVelocity { get; set; } = Vector2.Zero;
    
    /// <summary>
    /// Gets or sets the mass of the physics object.
    /// </summary>
    public float Mass { get; set; } = 1f;

    /// <summary>
    /// Flag to skip friction application this frame (for custom mechanics like bunnyhopping).
    /// Reset or set per frame by the owning GameObject.
    /// </summary>
    public bool SkipFrictionThisFrame { get; set; } = false;
    
    /// <summary>
    /// Gets the list of forces acting on the physics object.
    /// These forces are used by the physics engine to calculate the new velocity and position of the physics object. The list is cleared after every physics update, which means that a force has to be added again in each update cycle to still act.
    /// </summary>
    public List<Vector2> Forces { get; } = new List<Vector2>();

    /// <summary>
    /// Gets or sets the game object which the physics component is attached to.
    /// </summary>
    public GameObject GameObject { get; set; }
    
    /// <summary>
    /// Gets the collider associated with this physics object.
    /// </summary>
    public Collider Collider => GameObject.Collider;

    /// <summary>
    /// Creates a new <see cref="PhysicsComponent"/>.
    /// </summary>
    /// <param name="gameObject">The game object which the physics component is attached to.</param>
    /// <param name="mass">The mass of the physics component</param>
    public PhysicsComponent(GameObject gameObject, float mass)
    {
        GameObject = gameObject;
        Mass = mass;
    }
}
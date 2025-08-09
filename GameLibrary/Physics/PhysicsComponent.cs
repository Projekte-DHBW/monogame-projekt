using System.Collections.Generic;
using GameLibrary.Physics.Colliders;
using Microsoft.Xna.Framework;

namespace GameLibrary.Physics;

public abstract class PhysicsComponent
{
    /// <summary>
    /// Gets or sets the position of the physics object.
    /// </summary>
    public Vector2 Position { get; set; } = Vector2.Zero;
    
    /// <summary>
    /// Gets or sets the new position of the physics object. This new position is calculated by the physics engine and applied at the end of the physics loop.
    /// </summary>
    public Vector2 NewPosition { get; set; } = Vector2.Zero;
    
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
    /// Gets the list of forces acting on the physics object.
    /// These forces are used by the physics engine to calculate the new velocity and position of the physics object. The list is cleared after every physics update, which means that a force has to be added again in each update cycle to still act.
    /// </summary>
    public List<Vector2> Forces { get; } = new List<Vector2>();

    /// <summary>
    /// Gets or sets the collider associated with this physics object. A collider is required for every physics object (set in "Initialize" method).
    /// </summary>
    public Collider Collider { get; set; }
}
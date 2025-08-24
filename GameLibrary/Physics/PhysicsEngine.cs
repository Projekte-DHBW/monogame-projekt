using System;
using System.Collections.Generic;
using GameLibrary.Physics.Colliders;
using Microsoft.Xna.Framework;

namespace GameLibrary.Physics
{
    public class PhysicsEngine : GameComponent
    {
        /// <summary>
        /// A list of all physics objects which have been added to this physics engine and are taken into account in the physics update.
        /// </summary>
        private readonly List<PhysicsComponent> _physicsObjects = new List<PhysicsComponent>();
        
        /// <summary>
        /// Gets the collision engine associated with this physics engine. A collision engine is needed because a physics object implicitly requires collision handling to behave physically correct.
        /// </summary>
        public CollisionEngine CollisionEngine { get; init; }

        /// <summary>
        /// Creates a new physics engine.
        /// </summary>
        /// <param name="game">The game with which the physics engine is associated.</param>
        public PhysicsEngine(Game game) : base(game)
        {
            CollisionEngine = new CollisionEngine();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Adds a new physics object which is managed by the physics engine.
        /// </summary>
        /// <param name="physicsObject">The new physics object to add.</param>
        public void Add(PhysicsComponent physicsObject)
        {
            _physicsObjects.Add(physicsObject);
            CollisionEngine.Add(physicsObject.Collider);
        }
        
        /// <summary>
        /// The physics engine update loop which runs every update cycle of the game.
        /// </summary>
        /// <param name="gameTime">The current time state of the game.</param>
        public override void Update(GameTime gameTime)
        {
            // Calculate the elapsed time since the last update
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Iterate over all physics objects and calculate their new position and new velocity
            foreach (var physicsObject in _physicsObjects)
            {
                // Add all forces acting on the physics object
                Vector2 totalForce = Vector2.Zero;
                foreach (var force in physicsObject.Forces)
                {
                    totalForce += force;
                }
                physicsObject.Forces.Clear(); // Clear all forces

                // Calculate acceleration from total force: a = F / m
                Vector2 acceleration = totalForce / physicsObject.Mass;
                
                // gravity acceleration
                float g = 500f;
                
                // Depending on whether the physics object is on the ground or not, the acceleration resulting from gravity has to be calculated differently
                if (physicsObject.Collider.IsOnGround)
                {
                    // If the physics object is on the ground, the slope of the ground determines the acceleration due to gravity
                    float slopeAngle = physicsObject.Collider.SlopeAngle;
                    Vector2 tangent = new Vector2((float)Math.Cos(slopeAngle), (float)Math.Sin(slopeAngle));
                    // Derive normal from tangent (rotate clockwise)
                    Vector2 unitNormal = new Vector2(tangent.Y, -tangent.X);
                    
                    // Project gravity for correct slope acceleration
                    Vector2 gravity = Vector2.UnitY * g;
                    Vector2 effectiveGravity = gravity - Vector2.Dot(gravity, unitNormal) * unitNormal; // Dot product between gravity vector and the unit normal vector corresponds to |gravity| * cos(α) with α being the angle between the normal and the gravity vector. This value represents how much of the gravity vector is acting in the direction of the normal.
                    acceleration += effectiveGravity;
                    
                    // Apply friction (right now, only some kind of friction coefficient of the ground is used which is not physically correct but allows for an easy implementation of friction)
                    //
                    // Calculate normal force magnitude: N = m * |gravity| * cos(α)
                    float normalForceMagnitude = physicsObject.Mass * g * (float)Math.Cos(slopeAngle);
                    // Calculate friction force: F_friction = -μ * N * normalized_velocity
                    Vector2 frictionForce = Vector2.Zero;
                    if (physicsObject.Velocity.LengthSquared() > 0f) // Only apply friction if there's movement
                    {
                        Vector2 velocityDirection = Vector2.Normalize(physicsObject.Velocity);
                        frictionForce = -physicsObject.Collider.GroundCollider.FrictionCoefficient * normalForceMagnitude * velocityDirection;
                    }
                    acceleration -= frictionForce / physicsObject.Mass;
                }
                else
                {
                    // Clear ground collider when not on ground
                    physicsObject.Collider.GroundCollider = null;
                    physicsObject.Collider.SlopeAngle = 0f;
                    
                    // Apply vertical gravity when not on ground
                    acceleration += Vector2.UnitY * g; // TODO: has to be made more realistic (pixel distance is a bad way to measure distance)
                }
                
                // Calculate new velocity
                physicsObject.NewVelocity = physicsObject.Velocity + acceleration * dt;
                
                // Update the position
                physicsObject.Position += physicsObject.Velocity * dt;
            }

            // Collision detection and handling
            CollisionEngine.CheckCollisions();

            // Now that the collisions are handled and the new velocities are correct, they can be applied
            foreach (var physicsObject in _physicsObjects)
            {
                // Speed cap
                float maxSpeed = 800f;
                float minSpeed = 8f;
                if (physicsObject.NewVelocity.Length() > maxSpeed)
                {
                    physicsObject.NewVelocity = Vector2.Normalize(physicsObject.NewVelocity) * maxSpeed;
                }
                
                float angle = physicsObject.Collider.SlopeAngle;

                // Normalize to [0, 2π)
                float twoPi = 2f * (float)Math.PI;
                angle = angle % twoPi;
                if (angle < 0f) angle += twoPi;
                
                // Compute modulo π
                float modPi = angle % (float)Math.PI;

                // Effective absolute deviation from flat
                float effective = Math.Min(modPi, (float)Math.PI - modPi);

                float toleranceInRadians = 1f * (float)Math.PI / 180f; // ~0.01745f
                
                // Only use min speed when not on a slope because the slope friction logic needs the velocity vector to point along the slope. This is not the case when the x component is zeroed when it is below the min speed (such as when changing directions from sliding uphill, due to momentum, to sliding downhill).
                if (Math.Abs(physicsObject.NewVelocity.X) < minSpeed && effective < toleranceInRadians)
                {
                    physicsObject.NewVelocity = new Vector2(0, physicsObject.NewVelocity.Y);
                }
                physicsObject.Velocity = physicsObject.NewVelocity;
            }
        }
        
        /// <summary>
        /// Removes a physics component from the physics engine.
        /// </summary>
        /// <param name="physicsComponent">The physics component to remove.</param>
        public void RemoveComponent(PhysicsComponent physicsComponent)
        {
            if (physicsComponent == null) return;
            _physicsObjects.Remove(physicsComponent);
        }

        /// <summary>
        /// Removes all physics components
        /// </summary>
        public void ClearComponents()
        {
            _physicsObjects.Clear();
        }
    }
}
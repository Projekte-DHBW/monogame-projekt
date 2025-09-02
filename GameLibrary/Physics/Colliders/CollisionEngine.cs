using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GameLibrary.Physics.Colliders
{
    public class CollisionEngine
    {
        private struct CollisionData
        {
            public bool Intersects; // True if colliders intersect
            public Vector2 Normal; // Unit normal vector for collision resolution (points from collider1 to collider2)
            public float Depth; // Penetration depth for separation
        }
        
        /// <summary>
        /// A list of all colliders which have been added to this collision engine and are taken into account in the collision check.
        /// </summary>
        private readonly List<Collider> _colliders = new List<Collider>();
        
        public CollisionEngine()
        {
        }
        
        /// <summary>
        /// Adds a new collider which is managed by the collision engine.
        /// </summary>
        /// <param name="collider">The new collider to add.</param>
        public void Add(Collider collider)
        {
            _colliders.Add(collider);
        }
        
        /// <summary>
        /// Visualizes all colliders by calling their respective Draw methods.
        /// Requires the colliders to implement everything necessary to be able to draw them by calling their Draw method.
        /// </summary>
        public void VisualizeColliders()
        {
            foreach (Collider collider in _colliders)
            {
                collider.Draw(Core.SpriteBatch);
            }
        }

        /// <summary>
        /// Checks all colliders managed by this collision engine for collisions and handles them if there are some.
        /// </summary>
        /// <param name="maxIterations">
        /// Determines how many iterations of collision checks should be executed at a maximum.
        /// The iterations aim to resolve all collisions, even secondary, tertiary etc. ones, which can't be resolved with a single pass.
        /// A higher number is better for more complex scenes but impacts performance. The default number is 10.
        /// </param>
        public void CheckCollisions(int maxIterations = 10)
        {
            // Reset IsOnGround
            foreach (var collider in _colliders)
            {
                if (collider.PhysicsComponent != null) // Only for dynamics
                {
                    collider.IsOnGround = false;
                    collider.GroundCollider = null;
                }
            }
            
            int iteration = 0;
            bool hadIntersections;
            
            do
            {
                hadIntersections = false;
                
                // Iterate over all unique pairs of colliders
                for (int i = 0; i < _colliders.Count; i++)
                {
                    Collider collider1 = _colliders[i];
                    
                    for (int j = i + 1; j < _colliders.Count; j++)
                    {
                        Collider collider2 = _colliders[j];
                        
                        // Skip if both are static because static colliders can't move – even if a collision between them occurs
                        if (collider1.PhysicsComponent == null && collider2.PhysicsComponent == null)
                            continue;
                        
                        // Get collision data for current positions
                        CollisionData data = GetCollisionData(collider1, collider2);
                        
                        if (data.Intersects)
                        {
                            hadIntersections = true;
                            Console.WriteLine($"Collision detected between {collider1} and {collider2}");
                            HandleCollision(collider1, collider2, data);
                        }
                        else
                        {
                            // Integrated on-ground check (only for dynamic-static pairs where no current intersection)
                            Collider dynamicCollider = null;
                            Collider staticCollider = null;
                            
                            if (collider1.PhysicsComponent != null && collider2.PhysicsComponent == null)
                            {
                                dynamicCollider = collider1;
                                staticCollider = collider2;
                            }
                            else if (collider1.PhysicsComponent == null && collider2.PhysicsComponent != null)
                            {
                                dynamicCollider = collider2;
                                staticCollider = collider1;
                            }
                            
                            if (dynamicCollider != null)
                            {
                                Vector2 originalPosition = dynamicCollider.GlobalPosition;
                                float probeDistance = 2f; // Shift slightly down to probe for a collision (if that shift results in a collision, the dynamic collider is on the ground) (TODO: consider using surface normal instead of y axis for probing as slopes could have problems with the current implementation; problem: surface normal isn't known at the time of probing)
                                dynamicCollider.GlobalPosition = originalPosition + Vector2.UnitY * probeDistance; // Gravity direction
                                
                                CollisionData groundData = GetCollisionData(dynamicCollider, staticCollider);
                                
                                if (groundData.Intersects)
                                {
                                    dynamicCollider.IsOnGround = true;
                                    dynamicCollider.GroundCollider = staticCollider;
                                    if (groundData.Normal.LengthSquared() > 0f)
                                    {
                                        Vector2 unitNormal = groundData.Normal / groundData.Normal.Length();
                                        SetSlopeFromNormal(dynamicCollider, unitNormal);
                                    }
                                    else
                                    {
                                        dynamicCollider.SlopeAngle = 0f;
                                    }
                                }
                                
                                dynamicCollider.GlobalPosition = originalPosition;
                            }
                        }
                    }
                }
            } while (hadIntersections && ++iteration < maxIterations);

            if (hadIntersections)
            {
                Console.WriteLine("Last iteration still had intersections! The allowed number of iterations was too small.");
            }
        }

        /// <summary>
        /// Handles the collision between two colliders.
        /// Assumes that these colliders have already been checked and are in a collision.
        /// </summary>
        /// <param name="collider1">The first collider.</param>
        /// <param name="collider2">The second collider.</param>
        /// <param name="collisionData">The collision data.</param>
        private void HandleCollision(Collider collider1, Collider collider2, CollisionData collisionData)
        {
            bool isCollider1Static = collider1.PhysicsComponent == null;
            bool isCollider2Static = collider2.PhysicsComponent == null;

            // If at least one collider is static, the collision is handled by the Bounce method, and if both are dynamic, it's handled by the Collision method
            if (isCollider1Static && !isCollider2Static)
            {
                Bounce(collider1, collider2, collisionData);
            }
            else if (!isCollider1Static && isCollider2Static)
            {
                // Invert normal when swapping order (now points from static to dynamic)
                CollisionData invertedData = collisionData;
                invertedData.Normal = -collisionData.Normal;
                Bounce(collider2, collider1, invertedData);
            }
            else if (!isCollider1Static && !isCollider2Static)
            {
                collider1.GameObject.TriggerCollision(collider2);
                collider2.GameObject.TriggerCollision(collider1);
                //Collision(collider1, collider2, collisionData);

            }
        }

        /// <summary>
        /// Handles the collision between two dynamic colliders as an elastic collision.
        /// Assumes that these colliders have already been checked and are in a collision.
        /// </summary>
        /// <param name="dynamicCollider1">The first collider.</param>
        /// <param name="dynamicCollider2">The second collider.</param>
        /// <param name="collisionData">The collision data.</param>
        private void Collision(Collider dynamicCollider1, Collider dynamicCollider2, CollisionData collisionData)
        {
            Vector2 normal = collisionData.Normal;
            
            // Normalize the normal vector to get a unit normal
            float normalLength = normal.Length();
            if (normalLength == 0)
            {
                // No valid normal, skip collision handling
                return;
            }
            Vector2 unitNormal = normal / normalLength;
    
            // Get velocities and masses
            Vector2 v1 = dynamicCollider1.PhysicsComponent.Velocity;
            Vector2 v2 = dynamicCollider2.PhysicsComponent.Velocity;
            float m1 = dynamicCollider1.PhysicsComponent.Mass;
            float m2 = dynamicCollider2.PhysicsComponent.Mass;
    
            // Project velocities onto the unit normal (scalar components)
            float u1 = Vector2.Dot(v1, unitNormal);
            float u2 = Vector2.Dot(v2, unitNormal);
    
            // Compute new scalar components along the normal for elastic collision
            // Special case if total mass is zero (though unlikely, avoid division by zero)
            float totalMass = m1 + m2;
            if (totalMass == 0)
            {
                return;
            }
            float u1_new = (u1 * (m1 - m2) + 2 * m2 * u2) / totalMass;
            float u2_new = (u2 * (m2 - m1) + 2 * m1 * u1) / totalMass;
    
            // Compute tangential components
            Vector2 v1_normal = u1 * unitNormal;
            Vector2 v1_tangent = v1 - v1_normal;
            Vector2 v2_normal = u2 * unitNormal;
            Vector2 v2_tangent = v2 - v2_normal;
    
            // Reconstruct new velocities
            Vector2 new_v1 = v1_tangent + u1_new * unitNormal;
            Vector2 new_v2 = v2_tangent + u2_new * unitNormal;
    
            // Set new velocities
            dynamicCollider1.PhysicsComponent.NewVelocity = new_v1;
            dynamicCollider2.PhysicsComponent.NewVelocity = new_v2;
    
            // Nudge positions to prevent sticking
            Separate(dynamicCollider1, dynamicCollider2, unitNormal, collisionData.Depth);
        }

        /// <summary>
        /// Handles the collision between a static and a dynamic collider.
        /// Assumes that these colliders have already been checked and are in a collision.
        /// </summary>
        /// <param name="staticCollider">The static collider.</param>
        /// <param name="dynamicCollider">The dynamic collider.</param>
        /// <param name="collisionData">The collision data.</param>
        private void Bounce(Collider staticCollider, Collider dynamicCollider, CollisionData collisionData)
        {
            Vector2 normal = collisionData.Normal;
            
            // Normalize the normal vector
            float normalLength = normal.Length();
            if (normalLength == 0)
            {
                // No valid normal, skip collision handling
                return;
            }
            Vector2 unitNormal = normal / normalLength;
            
            if (staticCollider.IsElastic || dynamicCollider.IsElastic)
            {
                // Get the dynamic object's velocity
                Vector2 velocity = dynamicCollider.PhysicsComponent.Velocity;
    
                // Project velocity onto the unit normal (scalar component)
                float v_normal = Vector2.Dot(velocity, unitNormal);
    
                // Reflect the normal component: v_new = v - 2 * (v · n) * n
                Vector2 newVelocity = velocity - 2 * v_normal * unitNormal;
    
                // Apply energy loss (0.8 factor)
                newVelocity *= 0.8f;
    
                // Set new velocity
                dynamicCollider.PhysicsComponent.NewVelocity = newVelocity;
            }
            else
            {
                SetSlopeFromNormal(dynamicCollider, unitNormal);

                Vector2 slopeDirection = new Vector2((float)Math.Cos(dynamicCollider.SlopeAngle), (float)Math.Sin(dynamicCollider.SlopeAngle));
                float v_along_slope = Vector2.Dot(dynamicCollider.PhysicsComponent.Velocity, slopeDirection);
                dynamicCollider.PhysicsComponent.NewVelocity = slopeDirection * v_along_slope;
            }
            
            // Nudge position to prevent sticking, using unit normal for consistent distance
            Separate(staticCollider, dynamicCollider, unitNormal, collisionData.Depth);
            
            // If the deflection speed of the dynamic collider is low enough, we can set IsOnGround to True to mitigate an infinite bounce
            CheckDeflectionSpeed(dynamicCollider, unitNormal);
            if (dynamicCollider.IsOnGround)
            {
                dynamicCollider.GroundCollider = staticCollider;
            }
        }
        
        /// <summary>
        /// Sets the slope angle of the ground for a dynamic collider which is on the ground.
        /// </summary>
        /// <param name="dynamicCollider">The dynamic collider.</param>
        /// <param name="unitNormal">The unit normal of the slope.</param>
        private void SetSlopeFromNormal(Collider dynamicCollider, Vector2 unitNormal)
        {
            // Compute tangent by rotating the normal 90 degrees counterclockwise
            Vector2 tangent = new Vector2(-unitNormal.Y, unitNormal.X);
            float slopeAngle = (float)Math.Atan2(tangent.Y, tangent.X);
            dynamicCollider.SlopeAngle = slopeAngle;
        }
        
        
        /// <summary>
        /// Gets the collision data for a potential collision between two colliders.
        /// </summary>
        /// <param name="collider1">The first collider.</param>
        /// <param name="collider2">The second collider.</param>
        /// <returns>A CollisionData struct with intersection status, normal, and penetration depth.</returns>
        private CollisionData GetCollisionData(Collider collider1, Collider collider2)
        {
            // Initialize default result (no collision)
            CollisionData data = new CollisionData { Intersects = false, Normal = Vector2.Zero, Depth = 0f };
        
            // Handle circle-circle collision
            if (CircleAndCircle(collider1, collider2))
            {
                CircleCollider circle1 = (CircleCollider)collider1;
                CircleCollider circle2 = (CircleCollider)collider2;
                
                // Calculate distance between circle centers
                Vector2 delta = circle2.GlobalPosition - circle1.GlobalPosition;
                float dist = delta.Length();
                float sumRadii = circle1.Radius + circle2.Radius;
                
                // Check for intersection with small epsilon for numerical stability
                if (dist >= sumRadii + 0.1f) return data; // No intersection
        
                // Set intersection data
                data.Intersects = true;
                data.Depth = sumRadii - dist; // Penetration depth is how much circles overlap
                // Normal points from circle1 to circle2; use UnitY as fallback for zero distance
                data.Normal = dist > 0.0001f ? delta / dist : Vector2.UnitY;
                return data;
            }
            // Handle rectangle-rectangle collision using Separating Axis Theorem (SAT)
            else if (RectAndRect(collider1, collider2))
            {
                RectangleCollider rect1 = (RectangleCollider)collider1;
                RectangleCollider rect2 = (RectangleCollider)collider2;
        
                // Get rectangle centers and rotations (in radians for math)
                Vector2 center1 = rect1.GlobalPosition;
                Vector2 center2 = rect2.GlobalPosition;
                float angle1 = MathHelper.ToRadians(rect1.Rotation);
                float angle2 = MathHelper.ToRadians(rect2.Rotation);
        
                // Compute vertices for both rectangles (for SAT projection)
                Vector2[] vertices1 = GetRectangleVertices(center1, rect1.Width, rect1.Height, angle1);
                Vector2[] vertices2 = GetRectangleVertices(center2, rect2.Width, rect2.Height, angle2);
        
                // Define axes to test (normals to edges of both rectangles)
                Vector2 edge10 = vertices1[1] - vertices1[0];
                Vector2 axis10 = new Vector2(-edge10.Y, edge10.X); axis10.Normalize();
                Vector2 edge11 = vertices1[2] - vertices1[1];
                Vector2 axis11 = new Vector2(-edge11.Y, edge11.X); axis11.Normalize();
                Vector2 edge20 = vertices2[1] - vertices2[0];
                Vector2 axis20 = new Vector2(-edge20.Y, edge20.X); axis20.Normalize();
                Vector2 edge21 = vertices2[2] - vertices2[1];
                Vector2 axis21 = new Vector2(-edge21.Y, edge21.X); axis21.Normalize();
        
                Vector2[] axes = { axis10, axis11, axis20, axis21 };
        
                // Find minimum overlap and corresponding axis (for normal and depth)
                float minOverlap = float.MaxValue;
                Vector2 minAxis = Vector2.Zero;
        
                foreach (Vector2 axis in axes)
                {
                    // Project vertices onto axis; no overlap means no collision
                    if (!OverlapOnAxisWithPenetration(vertices1, vertices2, axis, out float overlap))
                    {
                        return data; // No intersection, return default
                    }
                    // Track smallest overlap for penetration depth and normal
                    if (overlap < minOverlap)
                    {
                        minOverlap = overlap;
                        minAxis = axis;
                    }
                }
        
                // Intersection confirmed; set data
                data.Intersects = true;
                data.Depth = minOverlap;
                
                // Adjust normal direction to point from rect1 to rect2
                Vector2 delta = center2 - center1;
                if (Vector2.Dot(delta, minAxis) < 0)
                {
                    minAxis = -minAxis;
                }
                data.Normal = minAxis;
        
                return data;
            }
            // Handle rectangle-circle collision
            else if (RectAndCircle(collider1, collider2))
            {
                // Assign rectangle and circle based on collider types
                RectangleCollider rect = collider1 is RectangleCollider ? (RectangleCollider)collider1 : (RectangleCollider)collider2;
                CircleCollider circle = collider1 is CircleCollider ? (CircleCollider)collider1 : (CircleCollider)collider2;
                bool rectIsCollider1 = collider1 is RectangleCollider;
        
                // Get centers and rectangle rotation
                Vector2 rectCenter = rect.GlobalPosition;
                Vector2 circleCenter = circle.GlobalPosition;
                float angle = MathHelper.ToRadians(rect.Rotation);
                float cos = (float)Math.Cos(angle);
                float sin = (float)Math.Sin(angle);
        
                // Transform circle center to rectangle's local space
                Vector2 delta = circleCenter - rectCenter;
                Vector2 localPos = new Vector2(delta.X * cos + delta.Y * sin, -delta.X * sin + delta.Y * cos);
        
                // Find closest point on rectangle to circle center
                float halfW = rect.Width / 2f;
                float halfH = rect.Height / 2f;
                float closestX = MathHelper.Clamp(localPos.X, -halfW, halfW);
                float closestY = MathHelper.Clamp(localPos.Y, -halfH, halfH);
        
                // Calculate distance from circle center to closest point
                float dx = localPos.X - closestX;
                float dy = localPos.Y - closestY;
                float distSquared = dx * dx + dy * dy;
        
                // Check for intersection with epsilon for numerical stability
                if (distSquared > circle.Radius * circle.Radius + 0.1f) return data; // No intersection
        
                // Intersection confirmed; compute depth
                data.Intersects = true;
                if (distSquared > 0)
                {
                    // Circle outside rectangle; depth is radius minus distance to closest point
                    data.Depth = circle.Radius - (float)Math.Sqrt(distSquared);
                }
                else
                {
                    // Circle center inside rectangle; depth is distance to nearest edge plus radius
                    float distLeft = halfW + localPos.X;
                    float distRight = halfW - localPos.X;
                    float distBottom = halfH + localPos.Y;
                    float distTop = halfH - localPos.Y;
                    data.Depth = Math.Min(Math.Min(distLeft, distRight), Math.Min(distBottom, distTop)) + circle.Radius;
                }
        
                // Compute normal in local space (from closest point to circle center)
                Vector2 localNormal = new Vector2(dx, dy);
                if (localNormal.LengthSquared() < 0.0001f)
                {
                    localNormal = Vector2.UnitY; // Fallback for center overlap
                }
                else if (closestX == -halfW || closestX == halfW || closestY == -halfH || closestY == halfH)
                {
                    localNormal.Normalize(); // On edge, use direction to circle center
                }
        
                // Transform normal back to world space
                Vector2 worldNormal = new Vector2(localNormal.X * cos - localNormal.Y * sin, localNormal.X * sin + localNormal.Y * cos);
                worldNormal.Normalize();
        
                // Ensure the normal points from rect to circle
                if (Vector2.Dot(worldNormal, circleCenter - rectCenter) < 0)
                {
                    worldNormal = -worldNormal;
                }
                // Flip normal if circle is collider1 (to keep normal from collider1 to collider2)
                if (!rectIsCollider1)
                {
                    worldNormal = -worldNormal;
                }
                data.Normal = worldNormal;
        
                return data;
            }
        
            // Return default (no collision) for unsupported collider types
            return data;
        }

        /// <summary>
        /// Separates two colliding colliders by moving them along the collision normal to resolve penetration.
        /// Assumes a valid unit normal and penetration depth from GetCollisionData.
        /// </summary>
        /// <param name="collider1">First collider in the pair.</param>
        /// <param name="collider2">Second collider in the pair.</param>
        /// <param name="unitNormal">Unit normal vector pointing from collider1 to collider2.</param>
        /// <param name="penetrationDepth">Depth of penetration to resolve.</param>
        private void Separate(Collider collider1, Collider collider2, Vector2 unitNormal, float penetrationDepth)
        {
            // Add small extra distance to prevent colliders from sticking due to numerical errors
            float extra = 1f;
            float totalSeparation = penetrationDepth + extra;
            
            // Calculate correction vector to push colliders apart along the normal
            Vector2 correction = unitNormal * totalSeparation;

            // Move dynamic collider1 backward along the normal (if dynamic)
            if (collider1.PhysicsComponent != null)
            {
                collider1.GlobalPosition -= correction;
            }

            // Move dynamic collider2 forward along the normal (if dynamic)
            if (collider2.PhysicsComponent != null)
            {
                collider2.GlobalPosition += correction;
            }
        }

        /// <summary>
        /// Checks if a dynamic collider's velocity along the surface normal is low enough to consider it grounded.
        /// Sets IsOnGround and zeros out vertical velocity to prevent small bounces.
        /// </summary>
        /// <param name="collider">The dynamic collider to check.</param>
        /// <param name="unitNormal">The unit surface normal of the ground.</param>
        private void CheckDeflectionSpeed(Collider collider, Vector2 unitNormal)
        {
            // Calculate the component of velocity normal to the surface
            float normalSpeed = Math.Abs(Vector2.Dot(collider.PhysicsComponent.NewVelocity, unitNormal));
            float deflectionThreshold = 60f;
            
            if (normalSpeed < deflectionThreshold)
            {
                collider.IsOnGround = true;
            
                // Zero the normal component
                collider.PhysicsComponent.NewVelocity -= Vector2.Dot(collider.PhysicsComponent.NewVelocity, unitNormal) * unitNormal;
            }
        }
        
        /// <summary>
        /// Computes the four vertices of a rectangle given its center, dimensions, and rotation.
        /// Used for Separating Axis Theorem (SAT) in rectangle-rectangle collision detection.
        /// </summary>
        /// <param name="center">Center point of the rectangle.</param>
        /// <param name="width">Width of the rectangle.</param>
        /// <param name="height">Height of the rectangle.</param>
        /// <param name="angle">Rotation angle in radians.</param>
        /// <returns>Array of four vertices in order: bottom-left, bottom-right, top-right, top-left.</returns>
        private Vector2[] GetRectangleVertices(Vector2 center, float width, float height, float angle)
        {
            // Precompute sine and cosine for rotation
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            
            // Calculate half-dimensions for vertex offsets
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            // Initialize vertex array
            Vector2[] vertices = new Vector2[4];
            
            // Bottom-left vertex: rotate (-halfWidth, -halfHeight) around center
            vertices[0] = center + new Vector2(
                (-halfWidth * cos - -halfHeight * sin),
                (-halfWidth * sin + -halfHeight * cos)
            );
            
            // Bottom-right vertex: rotate (halfWidth, -halfHeight) around center
            vertices[1] = center + new Vector2(
                (halfWidth * cos - -halfHeight * sin),
                (halfWidth * sin + -halfHeight * cos)
            );
            
            // Top-right vertex: rotate (halfWidth, halfHeight) around center
            vertices[2] = center + new Vector2(
                (halfWidth * cos - halfHeight * sin),
                (halfWidth * sin + halfHeight * cos)
            );
            
            // Top-left vertex: rotate (-halfWidth, halfHeight) around center
            vertices[3] = center + new Vector2(
                (-halfWidth * cos - halfHeight * sin),
                (-halfWidth * sin + halfHeight * cos)
            );

            return vertices;
        }

        /// <summary>
        /// Checks for overlap on a projection axis and computes penetration depth (for SAT).
        /// Used in rectangle-rectangle collision to find minimum overlap for normal and depth.
        /// </summary>
        /// <param name="vertices1">Vertices of the first shape.</param>
        /// <param name="vertices2">Vertices of the second shape.</param>
        /// <param name="axis">Projection axis (unit vector).</param>
        /// <param name="overlap">Output: Penetration depth along the axis (0 if no overlap).</param>
        /// <returns>True if projections overlap, false if separated.</returns>
        private bool OverlapOnAxisWithPenetration(Vector2[] vertices1, Vector2[] vertices2, Vector2 axis, out float overlap)
        {
            // Project both shapes' vertices onto the axis
            float min1, max1, min2, max2;
            ProjectVertices(vertices1, axis, out min1, out max1);
            ProjectVertices(vertices2, axis, out min2, out max2);
            
            // Check for overlap and compute penetration depth
            if (min1 <= max2 && min2 <= max1)
            {
                // Depth is the smaller of the two possible overlaps
                overlap = Math.Min(max1, max2) - Math.Max(min1, min2);
                return true;
            }
            
            // No overlap; set depth to 0
            overlap = 0f;
            return false;
        }

        /// <summary>
        /// Projects a set of vertices onto an axis and finds the minimum and maximum projection values.
        /// Used in SAT for rectangle-rectangle collision detection.
        /// </summary>
        /// <param name="vertices">Vertices to project.</param>
        /// <param name="axis">Projection axis (unit vector).</param>
        /// <param name="min">Output: Minimum projection value.</param>
        /// <param name="max">Output: Maximum projection value.</param>
        private void ProjectVertices(Vector2[] vertices, Vector2 axis, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            
            // Project each vertex onto the axis using dot product
            foreach (Vector2 vertex in vertices)
            {
                float projection = Vector2.Dot(vertex, axis);
                min = Math.Min(min, projection);
                max = Math.Max(max, projection);
            }
        }

        /// <summary>
        /// Helper method which returns true if both of the passed colliders are circle colliders.
        /// Otherwise, returns false.
        /// </summary>
        /// <param name="collider1">The first collider.</param>
        /// <param name="collider2">The second collider.</param>
        private bool CircleAndCircle(Collider collider1, Collider collider2)
        {
            return collider1 is CircleCollider && collider2 is CircleCollider;
        }

        /// <summary>
        /// Helper method which returns true if both of the passed colliders are rectangle colliders.
        /// Otherwise, returns false.
        /// </summary>
        /// <param name="collider1">The first collider.</param>
        /// <param name="collider2">The second collider.</param>
        private bool RectAndRect(Collider collider1, Collider collider2)
        {
            return collider1 is RectangleCollider && collider2 is RectangleCollider;
        }
        
        /// <summary>
        /// Helper method which returns true if one of the passed colliders is a circle collider and the other one a rectangle collider.
        /// Otherwise, returns false.
        /// The argument order doesn't matter.
        /// </summary>
        /// <param name="collider1">The first collider.</param>
        /// <param name="collider2">The second collider.</param>
        private bool RectAndCircle(Collider collider1, Collider collider2)
        {
            return (collider1 is RectangleCollider && collider2 is CircleCollider) || (collider1 is CircleCollider && collider2 is RectangleCollider);
        }

        /// <summary>
        /// Removes all colliders from the collision engine.
        /// </summary>
        /// <param name="collider">The collider to remove.</param>
        public void ClearColliders()
        {
            _colliders.Clear();
        }
    }
}
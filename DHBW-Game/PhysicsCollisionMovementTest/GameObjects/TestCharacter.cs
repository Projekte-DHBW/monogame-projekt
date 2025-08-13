using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Rendering;
using Microsoft.Xna.Framework;
using MonoGameTutorial;

namespace DHBW_Game.GameObjects;

public class TestCharacter : GameObject
{
    // Because movement is done with forces and a jump is typically not continuous but a discrete event, a duration over which the jump force acts is needed.
    private double _jumpDuration;
    
    /// <summary>
    /// Creates a new <see cref="TestCharacter"/> object.
    /// </summary>
    /// <param name="mass">The mass of the physics component.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    public TestCharacter(float mass, bool isElastic)
    {
        // Use circle collider
        Collider = new CircleCollider(this, new Vector2(0, 0), 30, isElastic);
        
        // Use rectangle collider
        //Collider = new RectangleCollider(this, new Vector2(0, 0), 50, 50, 0, isElastic);
        
        PhysicsComponent = new PhysicsComponent(this, mass);
        
        ServiceLocator.Get<PhysicsEngine>().Add(PhysicsComponent);
    }
    
    /// <summary>
    /// Initializes the test character at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the test character should spawn.</param>
    public override void Initialize(Vector2 startingPosition)
    {
        base.Initialize(startingPosition);
        
        // Make the camera follow the test character
        ServiceLocator.Get<Camera>().Follow(this);
    }

    /// <summary>
    /// Handles the input of the <see cref="GameController"/> class to create forces which move the test character object.
    /// </summary>
    /// <param name="gameTime">The current time state of the game.</param>
    private void HandleInput(GameTime gameTime)
    {
        Vector2 nextDirection = Vector2.Zero;
        
        // Upwards movement (jumping) results in a force over the set jump duration so that the jump "event" which is a button press still leads to an acceleration
        if (GameController.MoveUp())
        {
            _jumpDuration = 0.1;
        }
        if (GameController.MoveDown())
        {
            nextDirection += Vector2.UnitY * 4000;
        }
        if (GameController.MoveLeft())
        {
            nextDirection += -Vector2.UnitX * 4000;
        }
        if (GameController.MoveRight())
        {
            nextDirection += Vector2.UnitX * 4000;
        }
        
        if (_jumpDuration > 0)
        {
            nextDirection += -Vector2.UnitY * 15000;
        }
        
        _jumpDuration -= gameTime.ElapsedGameTime.TotalSeconds;
        
        PhysicsComponent.Forces.Add(nextDirection);
    }

    /// <summary>
    /// Updates the sprite which is set and also handles the input.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Handle any player input
        HandleInput(gameTime);
    }

    /// <summary>
    /// Draws the sprite which is set.
    /// </summary>
    public override void Draw()
    {
        base.Draw();
    }
}
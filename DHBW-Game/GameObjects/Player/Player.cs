using System;
using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameTutorial;

namespace GameObjects.Player;

public class Player : GameObject
{
    // Because movement is done with forces and a jump is typically not continuous but a discrete event, a duration over which the jump force acts is needed.
    private double _jumpDuration;

    private AnimatedSprite _playerRunning;
    private Sprite _playerStanding;
    private PlayerState _currentPlayerState = PlayerState.Standing;

    private enum PlayerState
    {
        Standing,
        Running
    }

    /// <summary>
    /// Creates a new <see cref="Player"/> object.
    /// </summary>
    /// <param name="mass">The mass of the physics component.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    public Player(float mass, bool isElastic)
    {
        // Use circle collider
        //Collider = new CircleCollider(this, new Vector2(0, 0), 30, isElastic);

        // Use rectangle collider
        Collider = new RectangleCollider(this, new Vector2(0, 0), 50, 130, 0, isElastic);

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

    public override void LoadContent()
    {
        // Load the player texture atlases
        TextureAtlas playerRunningAtlas = TextureAtlas.FromFile(Core.Content, "Animated_Sprites/Player/Run-definition.xml");
        TextureAtlas playerStandingAtlas = TextureAtlas.FromFile(Core.Content, "Animated_Sprites/Player/Idle-definition.xml");

        // Create the player sprite for running
        _playerRunning = playerRunningAtlas.CreateAnimatedSprite("running-animation");
        _playerRunning.Scale = new Vector2(4.0f, 4.0f);

        // Create the player sprite for standing
        var standingRegion = playerStandingAtlas.GetRegion("standing");
        _playerStanding = new Sprite(standingRegion);
        _playerStanding.Scale = new Vector2(4.0f, 4.0f);
    }

    /// <summary>
    /// Handles the input of the <see cref="GameController"/> class to create forces which move the test character object.
    /// </summary>
    /// <param name="gameTime">The current time state of the game.</param>
    private void HandleInput(GameTime gameTime)
    {
        // The current player state is initially set to standing so that when no input is registered, the Update Loop can assign the correct state.
        // This avoids unexpected animations on the ground, for instance, when the player runs down a slope without any input.
        _currentPlayerState = PlayerState.Standing;
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
            Sprite = _playerRunning;
            Sprite.Effects = SpriteEffects.FlipHorizontally;
            _currentPlayerState = PlayerState.Running;

            nextDirection += -Vector2.UnitX * 4000;
        }
        if (GameController.MoveRight())
        {
            // If the player is already running, that would mean that the user input is both MoveLeft and MoveRight, which results in no movement, so the Update Loop can assign the correct state.
            if (_currentPlayerState == PlayerState.Running)
            {
                Sprite = _playerStanding;
                _currentPlayerState = PlayerState.Standing;
            }
            else
            {
                Sprite = _playerRunning;
                Sprite.Effects = SpriteEffects.None;
                _currentPlayerState = PlayerState.Running;
            }

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

        // If the absolute velocity in x direction is high enough and there is no Input, the direction of the running animation is chosen based on the direction of the x velocity
        if (Math.Abs(PhysicsComponent.Velocity.X) > 10 && _currentPlayerState == PlayerState.Standing)
        {
            Sprite = _playerRunning;
            _currentPlayerState = PlayerState.Running;

            if (PhysicsComponent.Velocity.X < 0)
            {
                Sprite.Effects = SpriteEffects.FlipHorizontally;
            }
            else
            {
                Sprite.Effects = SpriteEffects.None;
            }
        }
        else
        {
            _currentPlayerState = PlayerState.Standing;
            Sprite = _playerStanding;
        }
    }

    /// <summary>
    /// Draws the sprite which is set.
    /// </summary>
    public override void Draw()
    {
        base.Draw();
    }
}
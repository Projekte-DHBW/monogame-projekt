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
    private AnimatedSpriteOnce _playerJumping;
    private Sprite _playerStanding;
    private AnimatePlayer _animatePlayer;
    private PlayerAnimationReturn _playerAnimationReturn;
    private bool _moveUp;
    private bool _moveDown;
    private bool _moveLeft;
    private bool _moveRight;

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
        TextureAtlas playerJumpingAtlas = TextureAtlas.FromFile(Core.Content, "Animated_Sprites/Player/Jump-definition.xml");

        // Create the player sprite for running
        _playerRunning = playerRunningAtlas.CreateAnimatedSprite("running-animation");
        _playerRunning.Scale = new Vector2(4.0f, 4.0f);

        // Create the player sprite for standing
        var standingRegion = playerStandingAtlas.GetRegion("standing");
        _playerStanding = new Sprite(standingRegion);
        _playerStanding.Scale = new Vector2(4.0f, 4.0f);

        // Create the player sprite for jumping
        _playerJumping = playerJumpingAtlas.CreateAnimatedSpriteOnce("jumping-animation");
        _playerJumping.Scale = new Vector2(4.0f, 4.0f);

        _animatePlayer = new AnimatePlayer();

        _moveUp = false;
        _moveDown = false;
        _moveLeft = false;
        _moveRight = false;
    }

    /// <summary>
    /// Handles the input of the <see cref="GameController"/> class to create forces which move the test character object.
    /// </summary>
    /// <param name="gameTime">The current time state of the game.</param>
    private void HandleInput(GameTime gameTime)
    {
        // The current player state is initially set to standing so that when no input is registered, the Update Loop can assign the correct state.
        // This avoids unexpected animations on the ground, for instance, when the player runs down a slope without any input.
        Vector2 nextDirection = Vector2.Zero;

        _moveUp = GameController.MoveUp();
        _moveDown = GameController.MoveDown();
        _moveLeft = GameController.MoveLeft();
        _moveRight = GameController.MoveRight();

        // Upwards movement (jumping) results in a force over the set jump duration so that the jump "event" which is a button press still leads to an acceleration
        if (_moveUp)
        {
            _jumpDuration = 0.1;
        }
        if (_moveDown)
        {
            nextDirection += Vector2.UnitY * 4000;
        }
        if (_moveLeft)
        {
            nextDirection += -Vector2.UnitX * 4000;
        }
        if (_moveRight)
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

        _playerAnimationReturn = _animatePlayer.GetAnimation(_moveUp, _moveDown, _moveLeft, _moveRight, PhysicsComponent);

        switch (_playerAnimationReturn.State)
        {
            case PlayerState.Idle:
                Sprite = _playerStanding;
                break;
            case PlayerState.Run:
                Sprite = _playerRunning;
                break;
            case PlayerState.Jump:
                if (Sprite != _playerJumping)
                    _playerJumping.ResetAnimation();
                Sprite = _playerJumping;
                break;
        }

        switch (_playerAnimationReturn.Facing)
        {
            case PlayerFacing.Left:
                Sprite.Effects = SpriteEffects.FlipHorizontally;
                break;
            case PlayerFacing.Right:
                Sprite.Effects = SpriteEffects.None;
                break;
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
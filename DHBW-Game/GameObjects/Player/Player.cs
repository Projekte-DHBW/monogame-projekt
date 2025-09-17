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
using GameObjects.Animate;

namespace GameObjects.Player;

public class Player : GameObject
{
    // Because movement is done with forces and a jump is typically not continuous but a discrete event, a duration over which the jump force acts is needed.
    private double _jumpDuration;

    private AnimatedSprite _playerRunning;
    private AnimatedSpriteOnce _playerJumping;
    private AnimatedSpriteOnce _playerTransitionJ2F;
    private AnimatedSprite _playerFalling;
    private Sprite _playerStanding;
    private AnimateGameObject _animatePlayer;
    private AnimationReturn _playerAnimationReturn;
    private bool _moveUp;
    private bool _moveDown;
    private bool _moveLeft;
    private bool _moveRight;

    // Fields for bunnyhop mechanic
    private float _timeSinceLanded = float.MaxValue; // Reset to 0 on landing
    private bool _wasOnGround;
    private float _jumpBufferTimer;
    private const float JumpBufferTime = 0.2f; // Window before landing to buffer jump input
    private const float LandingGraceTime = 0.2f; // Window after landing to press jump without slowdown
    private const int Height = 130;
    private const int Width = 50;

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
        Collider = new RectangleCollider(this, new Vector2(0, 0), Width, Height, 0, isElastic);
        Collider.CollisionGroup = "player";

        PhysicsComponent = new PhysicsComponent(this, mass);

        ServiceLocator.Get<PhysicsEngine>().Add(PhysicsComponent);
        _timeSinceLanded = float.MaxValue;
        _wasOnGround = false;
        _jumpBufferTimer = 0f;
        ServiceLocator.Register(this);
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
        TextureAtlas playerTransitionJ2FAtlas = TextureAtlas.FromFile(Core.Content, "Animated_Sprites/Player/Transition_Jump2Fall-definition.xml");
        TextureAtlas playerFallingAtlas = TextureAtlas.FromFile(Core.Content, "Animated_Sprites/Player/Fall-definition.xml");

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

        // Create the player sprite for the transition between jumping and falling
        _playerTransitionJ2F = playerTransitionJ2FAtlas.CreateAnimatedSpriteOnce("transitionJ2F-animation");
        _playerTransitionJ2F.Scale = new Vector2(4.0f, 4.0f);

        // Create the player sprite for falling
        _playerFalling = playerFallingAtlas.CreateAnimatedSprite("falling-animation");
        _playerFalling.Scale = new Vector2(4.0f, 4.0f);

        _animatePlayer = new AnimateGameObject();

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
        // Update jump buffer (buffer input if jump pressed, even in air)
        if (GameController.MoveUp())
        {
            _jumpBufferTimer = JumpBufferTime;
        }

        // The current player state is initially set to standing so that when no input is registered, the Update Loop can assign the correct state.
        // This avoids unexpected animations on the ground, for instance, when the player runs down a slope without any input.
        Vector2 nextDirection = Vector2.Zero;

        _moveUp = GameController.MoveUp();
        _moveDown = GameController.MoveDown() && !Collider.IsOnGround;
        _moveLeft = GameController.MoveLeft();
        _moveRight = GameController.MoveRight();

        float forceMagnitude;
        if (Collider.IsOnGround)
        {
            forceMagnitude = 4000;
        }
        else
        {
            forceMagnitude = 2000;
        }

        // Upwards movement (jumping) results in a force over the set jump duration so that the jump "event" which is a button press still leads to an acceleration
        if (_moveUp && Collider.IsOnGround)
        {
            _jumpDuration = 0.1;
        }
        if (_moveDown)
        {
            nextDirection += Vector2.UnitY * forceMagnitude;
        }
        if (_moveLeft)
        {
            nextDirection += -Vector2.UnitX * forceMagnitude;
        }
        if (_moveRight)
        {
            nextDirection += Vector2.UnitX * forceMagnitude;
        }

        PhysicsComponent.Forces.Add(nextDirection);

        // Update landing timer (detect just landed)
        bool justLanded = !_wasOnGround && Collider.IsOnGround;
        if (justLanded)
        {
            _timeSinceLanded = 0f;
        }
        else if (Collider.IsOnGround)
        {
            _timeSinceLanded += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
        _wasOnGround = Collider.IsOnGround;

        // Decrement buffer timer
        _jumpBufferTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Determine if player wants to jump (buffered or direct press within grace)
        bool wantsToJump = (_jumpBufferTimer > 0f) || (GameController.MoveUp() && _timeSinceLanded < LandingGraceTime);

        // Trigger jump if on ground and wants to
        if (wantsToJump && Collider.IsOnGround)
        {
            _jumpDuration = 0.1; // Start jump force duration
            _jumpBufferTimer = 0f; // Consume buffer

            // Detect if this is a wall jump and push away for skill-based mechanic
            float slopeAngle = Collider.SlopeAngle; // Radians
            float effectiveAngle = Helper.GetEffectiveSlopeAngleRadians(slopeAngle);
            float wallThresholdInRadians = (float)Math.PI / 4f; // 45 degrees equivalent
            if (effectiveAngle > wallThresholdInRadians)
            {
                // Reconstruct unit normal (matches PhysicsEngine logic)
                Vector2 tangent = new Vector2((float)Math.Cos(slopeAngle), (float)Math.Sin(slopeAngle));
                Vector2 unitNormal = new Vector2(tangent.Y, -tangent.X);

                // Apply push force away from wall
                float pushMagnitude = 40000f; // Strong enough to require quick recovery for multi-jumps
                PhysicsComponent.Forces.Add(unitNormal * pushMagnitude);
            }
        }

        // Add jump force if duration active
        if (_jumpDuration > 0)
        {
            PhysicsComponent.Forces.Add(-Vector2.UnitY * 15000);
        }
        _jumpDuration -= gameTime.ElapsedGameTime.TotalSeconds;

        // Set friction skip flag for this frame (only if on ground and jumping soon)
        PhysicsComponent.SkipFrictionThisFrame = Collider.IsOnGround && wantsToJump && _timeSinceLanded < LandingGraceTime;
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
            case State.Idle:
                Sprite = _playerStanding;
                break;
            case State.Run:
                Sprite = _playerRunning;
                break;
            case State.Jump:
                if (Sprite != _playerJumping)
                    _playerJumping.ResetAnimation();
                Sprite = _playerJumping;
                break;
            case State.Fall:
                if ((Sprite == _playerTransitionJ2F) && (_playerTransitionJ2F.IsFinished))
                {
                    Sprite = _playerFalling;
                    _playerTransitionJ2F.ResetAnimation();
                }
                else
                    if (Sprite != _playerFalling)
                    {
                    Sprite = _playerTransitionJ2F;
                }
                break;
            case State.Slide: // Use fall animation for sliding
                if ((Sprite == _playerTransitionJ2F) && (_playerTransitionJ2F.IsFinished))
                {
                    Sprite = _playerFalling;
                    _playerTransitionJ2F.ResetAnimation();
                }
                else if (Sprite != _playerFalling)
                {
                    Sprite = _playerTransitionJ2F;
                }
                break;
        }

        switch (_playerAnimationReturn.Facing)
        {
            case Facing.Left:
                Sprite.Effects = SpriteEffects.FlipHorizontally;
                break;
            case Facing.Right:
                Sprite.Effects = SpriteEffects.None;
                break;
        }
    }

    /// <summary>
    /// Draws the sprite which is set.
    /// </summary>
    public override void Draw()
    {
        if (!Collider.IsOnGround || Collider.GroundCollider == null)
        {
            base.Draw();
            return;
        }

        float effectiveAngle = Helper.GetEffectiveSlopeAngleRadians(Collider.SlopeAngle);
        float toleranceInRadians = (float)Math.PI / 180f; // ~1 degree

        // When not on a slope: return
        // Otherwise: offset the sprite to look like it's on the slope (without this,
        // there is a gap between the sprite and the slope due to the rectangle collider colliding at one of its corners)
        // To see this in action: activate collider visualization
        if (effectiveAngle < toleranceInRadians)
        {
            base.Draw();
            return;
        }

        // Bottom center start position
        int halfHeight = Height / 2;
        Vector2 bottomCenter = Position + new Vector2(0, halfHeight);

        // Get offset from CollisionEngine
        CollisionEngine ce = ServiceLocator.Get<PhysicsEngine>().CollisionEngine;
        float offset = ce.GetVerticalDistanceToSurface(bottomCenter, Collider.GroundCollider);

        // Apply the offset to the draw position
        Vector2 drawPosition = Position + Vector2.UnitY * offset;
        ServiceLocator.Get<Camera>().Draw(Sprite, drawPosition);
    }

    public override void TriggerCollision(Collider collider)
    {
        base.TriggerCollision(collider);
    }
}
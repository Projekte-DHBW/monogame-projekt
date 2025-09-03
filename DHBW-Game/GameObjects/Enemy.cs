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
using DHBW_Game;
using GameLibrary.Scenes;
using DHBW_Game.Scenes;

namespace GameObjects.Enemy;

public class Enemy : GameObject
{
    // Because movement is done with forces and a jump is typically not continuous but a discrete event, a duration over which the jump force acts is needed.
    protected double _jumpDuration;

    protected AnimatedSprite _enemyRunning;
    protected AnimatedSpriteOnce _enemyJumping;
    protected AnimatedSpriteOnce _enemyTransitionJ2F;
    protected AnimatedSprite _enemyFalling;
    protected Sprite _enemyStanding;
    protected AnimateGameObject _animateEnemy;
    protected AnimationReturn _animationReturn;
    protected bool _moveUp;
    protected bool _moveDown;
    protected bool _moveLeft;
    protected bool _moveRight;
    protected bool _hasCollided;
    protected TimeSpan _elapsed;
    protected TimeSpan _cooldown;

    /// <summary>
    /// Creates a new <see cref="Enemy"/> object.
    /// </summary>
    /// <param name="mass">The mass of the physics component.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    public Enemy(float mass, bool isElastic)
    {
        // Use circle collider
        //Collider = new CircleCollider(this, new Vector2(0, 0), 30, isElastic);

        // Use rectangle collider
        _animateEnemy = new AnimateGameObject();
        _cooldown = TimeSpan.FromMilliseconds(5000);
    }

    /// <summary>
    /// Initializes the test character at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the test character should spawn.</param>
    public override void Initialize(Vector2 startingPosition)
    {
        base.Initialize(startingPosition);
    }

    public override void LoadContent()
    {
        _moveUp = false;
        _moveDown = false;
        _moveLeft = false;
        _moveRight = true;
    }

    /// <summary>
    /// Updates the sprite which is set and also handles the input.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        _elapsed += gameTime.ElapsedGameTime;

        if (_elapsed >= _cooldown)
        {
            _elapsed -= _cooldown;
            if (_moveRight)
            {
                _moveRight = false;
                _moveLeft = true;
            }
            else
            {
                _moveLeft = false;
                _moveRight = true;
            }
        }
        Vector2 nextDirection = Vector2.Zero;

        if (_moveRight)
        {
            nextDirection += Vector2.UnitX * 2000;
        }

        if (_moveLeft)
        {
            nextDirection += -Vector2.UnitX * 2000;
        }

        if ((Math.Abs(PhysicsComponent.Velocity.X)) < 400)
            PhysicsComponent.Forces.Add(nextDirection);

        _animationReturn = _animateEnemy.GetAnimation(PhysicsComponent);
        switch (_animationReturn.State)
        {
            case State.Idle:
                Sprite = _enemyStanding;
                break;
            case State.Run:
                Sprite = _enemyRunning;
                break;
                /*
            case State.Jump:
                if (Sprite != _enemyJumping)
                    _enemyJumping.ResetAnimation();
                Sprite = _enemyJumping;
                break;
            case State.Fall:
                if ((Sprite == _enemyTransitionJ2F) && (_enemyTransitionJ2F.IsFinished))
                {
                    Sprite = _enemyFalling;
                    _enemyTransitionJ2F.ResetAnimation();
                }
                else
                    if (Sprite != _enemyFalling)
                {
                    Sprite = _enemyTransitionJ2F;
                }
                break;
        */}


        /*switch (_animationReturn.Facing)
        {
            case Facing.Left:
                Sprite.Effects = SpriteEffects.FlipHorizontally;
                break;
            case Facing.Right:
                Sprite.Effects = SpriteEffects.None;
                break;
        }*/
    }

    /// <summary>
    /// Draws the sprite which is set.
    /// </summary>
    public override void Draw()
    {
        base.Draw();
    }

    public override void TriggerCollision(Collider collider)
    {

    }
}
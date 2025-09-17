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

public class Professor : Enemy
{
    public string LecturerID { get; private set; }

    /// <summary>
    /// Creates a new <see cref="Professor"/> object.
    /// </summary>
    /// <param name="lecturerID">The ID of the lecturer (e.g., surname) for sprite and question selection.</param>
    /// <param name="mass">The mass of the physics component.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    public Professor(string lecturerID, float mass, bool isElastic) : base (mass, isElastic)
    {
        LecturerID = lecturerID ?? "berninger"; // Fallback if null

        // Use circle collider
        //Collider = new CircleCollider(this, new Vector2(0, 0), 30, isElastic);

        // Use rectangle collider
        Collider = new RectangleCollider(this, new Vector2(0, 0), 55, 140, 0, isElastic);
        Collider.CollisionGroup = "enemy";

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
    }

    public override void LoadContent()
    {
        // Load the lecturer-specific texture atlases
        string runPath = $"Animated_Sprites/Lecturers/{LecturerID}-Run-definition.xml";
        //string idlePath = $"Animated_Sprites/Lecturers/{LecturerID}-Idle-definition.xml";

        TextureAtlas enemyRunningAtlas = TextureAtlas.FromFile(Core.Content, runPath);
        //TextureAtlas enemyStandingAtlas = TextureAtlas.FromFile(Core.Content, idlePath);
        //TextureAtlas enemyJumpingAtlas = TextureAtlas.FromFile(Core.Content, "Animated_Sprites/Walk_Dozent-definition.xml");
        //TextureAtlas enemyTransitionJ2FAtlas = TextureAtlas.FromFile(Core.Content, "Animated_Sprites/Walk_Dozent-definition.xml");
        //TextureAtlas enemyFallingAtlas = TextureAtlas.FromFile(Core.Content, "Animated_Sprites/Walk_Dozent-definition.xml");

        // Create the enemy sprite for running
        _enemyRunning = enemyRunningAtlas.CreateAnimatedSprite("running-animation");
        _enemyRunning.Scale = new Vector2(4.0f, 4.0f);

        // Create the enemy sprite for standing
        //var standingRegion = enemyStandingAtlas.GetRegion("standing");
        //_enemyStanding = new Sprite(standingRegion);
        //_enemyStanding.Scale = new Vector2(4.0f, 4.0f);
        //
        //// Create the enemy sprite for jumping
        //_enemyJumping = enemyJumpingAtlas.CreateAnimatedSpriteOnce("jumping-animation");
        //_enemyJumping.Scale = new Vector2(4.0f, 4.0f);
        //
        //// Create the enemy sprite for the transition between jumping and falling
        //_enemyTransitionJ2F = enemyTransitionJ2FAtlas.CreateAnimatedSpriteOnce("transitionJ2F-animation");
        //_enemyTransitionJ2F.Scale = new Vector2(4.0f, 4.0f);
        //
        //// Create the enemy sprite for falling
        //_enemyFalling = enemyFallingAtlas.CreateAnimatedSprite("falling-animation");
        //_enemyFalling.Scale = new Vector2(4.0f, 4.0f);
        base.LoadContent();
    }

    /// <summary>
    /// Updates the sprite which is set and also handles the input.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Handle any enemy input
        //HandleInput(gameTime);

        _animationReturn = _animateEnemy.GetAnimation(PhysicsComponent);
        switch (_animationReturn.State)
        {
            case State.Idle:
                //Sprite = _enemyStanding;
                //break;
            case State.Run:
                Sprite = _enemyRunning;
                break;
            default:
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
        */
        }


        switch (_animationReturn.Facing)
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
        base.Draw();
    }

    public override void TriggerCollision(Collider collider)
    {
        base.TriggerCollision(collider);

        if ((collider.GameObject is Player.Player) && (!_hasCollided))
        {
            _hasCollided = true;

            GameScene scene = (GameScene)ServiceLocator.Get<Scene>();
            scene.ShowQuestion(LecturerID);
        }
    }
}
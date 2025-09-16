using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using GameLibrary.Physics.Colliders;
using Microsoft.Xna.Framework;
using DHBW_Game.Scenes;
using GameLibrary.Scenes;

namespace GameObjects.Static_Sprites.Elevator;

public class Elevator : GameObject
{
    private bool _isActivated;
    private readonly bool _isElevatorAvailable;
    private AnimatedSpriteOnce _openingElevator;
    private AnimatedSpriteOnce _closingElevator;
    private AnimatedSpriteOnce _closingEntranceElevator;
    private Sprite _openElevator;
    private Sprite _closedElevator;

  
    public Elevator(bool isElevatorAvailable = true)
    {
        // Use rectangle collider
        Collider = new RectangleCollider(this, new Vector2(0, 0), 250, 256, 0, false);
        Collider.CollisionGroup = "enemy";

        ServiceLocator.Get<CollisionEngine>().Add(Collider);

        _isActivated = false;
        _isElevatorAvailable = isElevatorAvailable;
    }

    /// <summary>
    /// Initializes the <see cref="Elevator"/> object at the given starting position in the world.
    /// </summary>
    /// <param name="position">The position at which the <see cref="Elevator"/> object should spawn.</param>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        Position = position;
    }

    public override void LoadContent()
    {
        TextureAtlas elevatorAtlas = TextureAtlas.FromFile(Core.Content, "Animated_Sprites/Elevator/Elevator-definition.xml");

        _openingElevator = elevatorAtlas.CreateAnimatedSpriteOnce("elevatorOpening-animation");
        _openingElevator.Scale = new Vector2(4.0f, 4.0f);

        _closingElevator = elevatorAtlas.CreateAnimatedSpriteOnce("elevatorClosing-animation");
        _closingElevator.Scale = new Vector2(4.0f, 4.0f);

        _closingEntranceElevator = elevatorAtlas.CreateAnimatedSpriteOnce("entranceElevatorClosing-animation");
        _closingEntranceElevator.Scale = new Vector2(4.0f, 4.0f);

        var elevatorRegionOpen = elevatorAtlas.GetRegion("elevatorOpen");
        _openElevator = new Sprite(elevatorRegionOpen);
        _openElevator.Scale = new Vector2(4.0f, 4.0f);

        var elevatorRegionClosed = elevatorAtlas.GetRegion("elevatorClosed");
        _closedElevator = new Sprite(elevatorRegionClosed);
        _closedElevator.Scale = new Vector2(4.0f, 4.0f);

        if (_isElevatorAvailable)
        {
            Sprite = _closedElevator;
        }
        else
        {
            Sprite = _closingEntranceElevator;
        }

        base.LoadContent();
    }

   

    /// <summary>
    /// Updates the sprite which is set.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (!_isElevatorAvailable)
        {
            return;
        }

        if (!_isActivated)
        {
            Deactivate();
            if (_closingElevator.IsFinished)
            {
                Sprite = _closedElevator;
            }
            else if (Sprite != _closingElevator)
            {
                Sprite = _closingElevator;
                _openingElevator.ResetAnimation();
            }
        }
        _isActivated = false;
    }

    /// <summary>
    /// Draws the sprite which is set.
    /// </summary>
    public override void Draw()
    {

        base.Draw();
    }

    private void Activate()
    {
        _isActivated = true;
    }

    private void Deactivate()
    {
        _isActivated = false;
    }

    public override void TriggerCollision(Collider collider)
    {
        base.TriggerCollision(collider);

        if (_isElevatorAvailable)
        {
            if (collider.GameObject is Player.Player)
            {
                Activate();
                if (_openingElevator.IsFinished)
                {
                    Sprite = _openElevator;
                }
                else if (Sprite != _openingElevator)
                {
                    Sprite = _openingElevator;
                    _closingElevator.ResetAnimation();
                }

                if ((collider.GlobalPosition - Position).LengthSquared() < 5000)
                {
                    GameScene scene = (GameScene)ServiceLocator.Get<Scene>();
                    scene.LevelCompleted();
                }
            }
        }
    }
}
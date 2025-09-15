using FlatRedBall.Glue.StateInterpolation;
using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameTutorial;
using System;

namespace GameObjects.Static_Sprites.Elevator;

public class Elevator : GameObject
{
    //private AnimatedSprite _elevator;
    private TextureAtlas elevatorAtlas;
    private bool _isActivated;
    private bool _isEntranceElevator;
    private AnimatedSpriteOnce _openingElevator;
    private AnimatedSpriteOnce _closingElevator;
    private AnimatedSpriteOnce _closingEntranceElevator;
    private Sprite _openElevator;
    private Sprite _closedElevator;

  
    public Elevator(bool isEntranceElevator = true)
    {
        _isActivated = false;
        _isEntranceElevator = isEntranceElevator;
    }

    /// <summary>
    /// Initializes the test character at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the test character should spawn.</param>
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

        Sprite = _closingEntranceElevator;

        base.LoadContent();
    }

   

    /// <summary>
    /// Updates the sprite which is set and also handles the input.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (!_isEntranceElevator)
        {
            return;
        }
        if (Math.Abs(Position.X - ServiceLocator.Get<Player.Player>().Position.X) < 200)
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

        } 
        else 
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
}
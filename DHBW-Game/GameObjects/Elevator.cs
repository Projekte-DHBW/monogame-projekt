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
    private bool _isMirrored;

    /*
    public Elevator(float mass, bool isElastic) 
    {
        // Use rectangle collider
        Collider = new RectangleCollider(this, new Vector2(0, 0), 50, 130, 0, isElastic);

        PhysicsComponent = new PhysicsComponent(this, mass);

        ServiceLocator.Get<PhysicsEngine>().Add(PhysicsComponent);
    }*/

    public Elevator(bool isActivated = true, bool isMirrored = false)
    {
        _isActivated = isActivated;
        _isMirrored = isMirrored;
    }

    /// <summary>
    /// Initializes the test character at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the test character should spawn.</param>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        UpdateSprite();
        Position = position;
    }

    public override void LoadContent()
    {
        elevatorAtlas = TextureAtlas.FromFile(Core.Content, "Animated_Sprites/Elevator/Elevator-definition.xml");
        UpdateSprite();

        base.LoadContent();
    }

    private void UpdateSprite()
    {
        var elevatorRegion = _isActivated ? elevatorAtlas.GetRegion("elevatorOpen") : elevatorAtlas.GetRegion("elevatorClosed");
        Sprite = new Sprite(elevatorRegion);
        Sprite.Scale = new Vector2(4.0f, 4.0f);
        Sprite.Effects = _isMirrored ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
    }

    /// <summary>
    /// Updates the sprite which is set and also handles the input.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    /// <summary>
    /// Draws the sprite which is set.
    /// </summary>
    public override void Draw()
    {

        base.Draw();
    }

    public void Activate()
    {
        _isActivated = true;
        UpdateSprite();
    }

    public void Deactivate()
    {
        _isActivated = false;
        UpdateSprite();
    }

    public bool IsActivated => _isActivated;
}
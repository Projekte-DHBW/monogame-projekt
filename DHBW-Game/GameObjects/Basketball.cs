using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DHBW_Game.GameObjects;

public class Basketball : GameObject
{
    private float _rotation = 0f;
    private Vector2 _origin;

    public Basketball(float mass)
    {
        Collider = new CircleCollider(this, new Vector2(0, 0), 16, true);
        Collider.CollisionGroup = "movable";
        Collider.CanBeGround = false;

        PhysicsComponent = new PhysicsComponent(this, mass);

        ServiceLocator.Get<PhysicsEngine>().Add(PhysicsComponent);
    }


    /// <summary>
    /// Initializes the <see cref="Basketball"/> object at the given starting position in the world.
    /// </summary>
    /// <param name="position">The position at which the <see cref="Basketball"/> object should spawn.</param>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);

    }

    public override void LoadContent()
    {
        TextureAtlas ballAtlas = TextureAtlas.FromFile(Core.Content, "Static_Sprites/Ball-definition.xml");

        // Create the basketball sprite
        var basketballRegion = ballAtlas.GetRegion("ball");
        Sprite = new Sprite(basketballRegion);
        Sprite.Scale = new Vector2(1.0f, 1.0f);

        // Set origin to center for rotation
        _origin = new Vector2(basketballRegion.Width / 2f, basketballRegion.Height / 2f);

        base.LoadContent();
    }


    /// <summary>
    /// Updates the sprite which is set.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float radius = ((CircleCollider)Collider).Radius;
        float angularSpeed = PhysicsComponent.Velocity.X / radius; // Radians per second; positive for rightward roll (clockwise)

        _rotation += angularSpeed * dt; // Accumulate rotation
    }

    /// <summary>
    /// Draws the sprite which is set.
    /// </summary>
    public override void Draw()
    {
        var camera = ServiceLocator.Get<Camera>();
        camera.Draw(
            Sprite.Region,
            Core.SpriteBatch,
            Position,
            Color.White,
            _rotation,
            _origin,
            Sprite.Scale,
            SpriteEffects.None,
            0f
        );
    }
}
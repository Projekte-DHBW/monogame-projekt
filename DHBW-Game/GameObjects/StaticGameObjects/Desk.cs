using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using Microsoft.Xna.Framework;

namespace GameObjects.Static_Sprites.Desk;

public class Desk : GameObject
{

    public Desk(float mass, bool isElastic)
    {
        Collider = new RectangleCollider(this, new Vector2(0, 0), 152, 84, 0, isElastic);
        Collider.CollisionGroup = "movable";
        Collider.CanBeGround = true;

        PhysicsComponent = new PhysicsComponent(this, mass);

        ServiceLocator.Get<PhysicsEngine>().Add(PhysicsComponent);
    }


    /// <summary>
    /// Initializes the <see cref="Desk"/> object at the given starting position in the world.
    /// </summary>
    /// <param name="position">The position at which the <see cref="Desk"/> object should spawn.</param>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        
    }

    public override void LoadContent()
    {
        TextureAtlas deskAtlas = TextureAtlas.FromFile(Core.Content, "Static_Sprites/Desk-definition.xml");

        // Create the desk sprite
        var deskRegion = deskAtlas.GetRegion("desk");
        Sprite = new Sprite(deskRegion);
        Sprite.Scale = new Vector2(4.0f, 4.0f);

        base.LoadContent();
    }


    /// <summary>
    /// Updates the sprite which is set.
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
}
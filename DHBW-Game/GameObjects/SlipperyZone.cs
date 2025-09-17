using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using GameLibrary.Physics.Colliders;
using Microsoft.Xna.Framework;
using DHBW_Game;
using GameLibrary.Scenes;
using DHBW_Game.Scenes;

namespace GameObjects;

public class SlipperyZone : GameObject
{
    /// <summary>
    /// Creates a new <see cref="SlipperyZone"/> object.
    /// </summary>
    public SlipperyZone()
    {
        // Use rectangle collider
        Collider = new RectangleCollider(this, new Vector2(0, 30), 400, 10, 0, false);
        Collider.CollisionGroup = "interactive";

        ServiceLocator.Get<CollisionEngine>().Add(Collider);
    }

    /// <summary>
    /// Initializes the <see cref="SlipperyZone"/> object at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the <see cref="SlipperyZone"/> object should spawn.</param>
    public override void Initialize(Vector2 startingPosition)
    {
        base.Initialize(startingPosition);
    }

    public override void LoadContent()
    {
        TextureAtlas slipperyZoneAtlas = TextureAtlas.FromFile(Core.Content, "Static_Sprites/Caution_Board-definition.xml");

        // Create the slipperyZone sprite
        var slipperyZoneRegion = slipperyZoneAtlas.GetRegion("cautionBoard");
        Sprite = new Sprite(slipperyZoneRegion);
        Sprite.Scale = new Vector2(2.0f, 2.0f);

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

    public override void TriggerCollision(Collider collider)
    {
        base.TriggerCollision(collider);

        if (collider.GameObject is Player.Player)
        {
            var player = collider.GameObject as Player.Player;
            player.Slide();
        }
    }
}
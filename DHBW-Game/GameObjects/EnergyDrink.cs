using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using GameLibrary.Physics.Colliders;
using Microsoft.Xna.Framework;
using DHBW_Game;
using GameLibrary.Scenes;
using DHBW_Game.Scenes;

namespace GameObjects;

public class EnergyDrink : GameObject
{
    private bool _isCollected;

    /// <summary>
    /// Creates a new <see cref="EnergyDrink"/> object.
    /// </summary>
    public EnergyDrink()
    {
        // Use rectangle collider
        Collider = new RectangleCollider(this, new Vector2(0, 0), 40, 256, 0, false);
        Collider.CollisionGroup = "interactive";

        ServiceLocator.Get<CollisionEngine>().Add(Collider);
    }

    /// <summary>
    /// Initializes the <see cref="EnergyDrink"/> object at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the <see cref="EnergyDrink"/> object should spawn.</param>
    public override void Initialize(Vector2 startingPosition)
    {
        base.Initialize(startingPosition);
        _isCollected = false;
    }

    public override void LoadContent()
    {
        TextureAtlas energyDrinkAtlas = TextureAtlas.FromFile(Core.Content, "Static_Sprites/EnergyDrink-definition.xml");

        // Create the energyDrink sprite
        var energyDrinkRegion = energyDrinkAtlas.GetRegion("energyDrink");
        Sprite = new Sprite(energyDrinkRegion);
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
        if (_isCollected)
        {
            return;
        }
        base.Draw();
    }

    public override void TriggerCollision(Collider collider)
    {
        base.TriggerCollision(collider);

        if ((collider.GameObject is Player.Player) && !_isCollected)
        {
            _isCollected = true;
            var player = collider.GameObject as Player.Player;
            player.Boost(3,7);
        }
    }
}
using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using GameLibrary.Physics.Colliders;
using Microsoft.Xna.Framework;
using DHBW_Game;
using GameLibrary.Scenes;
using DHBW_Game.Scenes;

namespace GameObjects;

public class Goal : GameObject
{
    /// <summary>
    /// Creates a new <see cref="Goal"/> object.
    /// </summary>
    public Goal()
    {
        // Use rectangle collider
        Collider = new RectangleCollider(this, new Vector2(0, 0), 40, 256, 0, false);
        Collider.CollisionGroup = "enemy";

        ServiceLocator.Get<CollisionEngine>().Add(Collider);
    }

    /// <summary>
    /// Initializes the <see cref="Goal"/> object at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the <see cref="Goal"/> object should spawn.</param>
    public override void Initialize(Vector2 startingPosition)
    {
        base.Initialize(startingPosition);
    }

    public override void LoadContent()
    {
        TextureAtlas goalAtlas = TextureAtlas.FromFile(Core.Content, "Static_Sprites/Goal-definition.xml");

        // Create the goal sprite
        var goalRegion = goalAtlas.GetRegion("goal");
        Sprite = new Sprite(goalRegion);
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

    public override void TriggerCollision(Collider collider)
    {
        base.TriggerCollision(collider);

        if (collider.GameObject is Player.Player)
        {
            ServiceLocator.Get<Game1>().GameOver();

            GameScene scene = (GameScene)ServiceLocator.Get<Scene>();
            scene.ShowFinalWinPanel();
        }
    }
}
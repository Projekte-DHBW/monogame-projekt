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

public class Goal : Enemy
{
    /// <summary>
    /// Creates a new <see cref="Professor"/> object.
    /// </summary>
    /// <param name="mass">The mass of the physics component.</param>
    /// <param name="isElastic">Whether the collider is elastic.</param>
    public Goal(float mass, bool isElastic) : base (mass, isElastic)
    {
        // Use circle collider
        //Collider = new CircleCollider(this, new Vector2(0, 0), 30, isElastic);

        // Use rectangle collider
        Collider = new RectangleCollider(this, new Vector2(0, 0), 4, 256, 0, isElastic);
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
        TextureAtlas goalAtlas = TextureAtlas.FromFile(Core.Content, "Static_Sprites/Goal-definition.xml");

        // Create the closed door sprite
        var goalRegion = goalAtlas.GetRegion("goal");
        Sprite = new Sprite(goalRegion);
        Sprite.Scale = new Vector2(4.0f, 4.0f);

        base.LoadContent();
    }

    /// <summary>
    /// Updates the sprite which is set and also handles the input.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public override void Update(GameTime gameTime)
    {
        //base.Update(gameTime);

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

            ServiceLocator.Get<Game1>().GameOver();

            GameScene scene = (GameScene)ServiceLocator.Get<Scene>();
            scene.ShowFinalWinPanel();
        }
    }
}
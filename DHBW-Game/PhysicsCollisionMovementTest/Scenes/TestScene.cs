using DHBW_Game.GameObjects;
using DHBW_Game.Levels;
using GameLibrary;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Scenes;
using GameObjects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameTutorial;

namespace DHBW_Game.Scenes;

public class TestScene : Scene
{

    // The map instance
    private Level _level;
    
    private readonly CollisionEngine _collisionEngine;
    private readonly PhysicsEngine _physicsEngine;
    
    public TestScene()
    {
        _physicsEngine = ServiceLocator.Get<PhysicsEngine>();
        _collisionEngine = _physicsEngine.CollisionEngine;
    }
    
    public override void Initialize()
    {
        // LoadContent is called during base.Initialize().
        base.Initialize();
        
        Core.ExitOnEscape = false;
        
        // Initialize a new game to be played.
        InitializeNewGame();
    }
    
    private void InitializeNewGame()
    {
        _level = new Level(Core.Content, "00.txt");
    }
    
    public override void LoadContent()
    {
    }
    
    public override void Update(GameTime gameTime)
    {
        // Check whether to pause the game. Currently works like a toggle.
        if (GameController.Pause())
        {
            if (IsPaused)
            {
                ServiceLocator.Get<Game1>().Resume();
            }
            else
            {
                ServiceLocator.Get<Game1>().Pause();
            }
        }

        if (IsPaused)
        {
            return;
        }
        
        // Update the map (which updates all placed game objects).
        _level.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        // Clear the back buffer.
        Core.GraphicsDevice.Clear(Color.CornflowerBlue);
        
        // Begin the sprite batch.
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        // Visualize the colliders. This enables debugging the colliders without relying on sprites which don't exactly depict the colliders.
        _collisionEngine.VisualizeColliders();
        
        // Draw the map (which draws the background tilemap and all placed game objects).
        _level.Draw(Core.SpriteBatch);

        // End the sprite batch when finished.
        Core.SpriteBatch.End();
    }
}
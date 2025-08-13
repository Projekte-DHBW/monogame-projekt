using DHBW_Game.GameObjects;
using DHBW_Game.Maps;
using GameLibrary;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DHBW_Game.Scenes;

public class TestScene : Scene
{
    // Player character
    private TestCharacter _character;

    // The map instance
    private Map _map;
    
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
        
        Core.ExitOnEscape = true;
        
        // Initialize a new game to be played.
        InitializeNewGame();
    }
    
    private void InitializeNewGame()
    {
        // Load the map from an XML configuration file using the content manager.
        _map = Map.FromFile(Core.Content, "Maps/test_map.xml");
        
        // Create the player character separately, using the start position from the map.
        _character = new TestCharacter(mass: 2f, isElastic: false);
        _character.Initialize(_map.StartPosition);
    }
    
    public override void LoadContent()
    {
    }
    
    public override void Update(GameTime gameTime)
    {
        // Update the map (which updates all placed game objects).
        _map.Update(gameTime);
        
        // Update the player character.
        _character.Update(gameTime);
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
        _map.Draw(Core.SpriteBatch);
        
        // Draw the player character.
        _character.Draw();

        // End the sprite batch when finished.
        Core.SpriteBatch.End();
    }
}
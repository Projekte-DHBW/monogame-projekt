using DHBW_Game.GameObjects;
using DHBW_Game.Levels;
using DHBW_Game.Question_System;
using GameLibrary;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Scenes;
using GameObjects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameTutorial;
using MonoGameTutorial.UI;

namespace DHBW_Game.Scenes;

public class TestScene : Scene
{

    // The map instance
    private Level _level;
    
    private readonly CollisionEngine _collisionEngine;
    private readonly PhysicsEngine _physicsEngine;
    
    private GameSceneUI _ui;
    
    private QuestionPool _questionPool;
    
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
        
        // Initialize the user interface for the game scene.
        InitializeUI();
        
        // Initialize a new game to be played.
        InitializeNewGame();
        
        _questionPool = ServiceLocator.Get<QuestionPool>();
    }

    private void InitializeUI()
    {
        // Clear out any previous UI element in case we came here
        // from a different scene.
        GumService.Default.Root.Children.Clear();

        // Create the game scene ui instance.
        _ui = new GameSceneUI();
        
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
        // Ensure the UI is always updated.
        _ui.Update(gameTime);
        
        // Temporary demonstration code for the question display system
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Q))
        {
            var (q, idx) = _questionPool.GetNextQuestion();
            if (q != null)
            {
                ServiceLocator.Get<Game1>().Pause();
                _ui.ShowQuestion(q, () => _questionPool.MarkAsAnswered(idx), () => ServiceLocator.Get<Game1>().Resume() );
            }
        }
        
        // Check whether to pause the game. Currently works like a toggle.
        if (GameController.Pause())
        {
            if (IsPaused)
            {
                ServiceLocator.Get<Game1>().Resume();
                _ui.HidePausePanel();
            }
            else
            {
                ServiceLocator.Get<Game1>().Pause();
                _ui.ShowPausePanel();
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
        
        // Draw the UI last (overlays everything else).
        _ui.Draw();
    }
}
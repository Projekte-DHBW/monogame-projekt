using DHBW_Game.Levels;
using GameLibrary;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace DHBW_Game.Scenes
{
    /// <summary>
    /// Scene that handles the game's level progression, loading levels sequentially
    /// and managing transitions between them.
    /// </summary>
    public class GameScene : Scene
    {
        private Level _currentLevel;
        private int _currentLevelNumber = 0;
        private readonly CollisionEngine _collisionEngine;
        private readonly PhysicsEngine _physicsEngine;

        public GameScene()
        {
            _physicsEngine = ServiceLocator.Get<PhysicsEngine>();
            _collisionEngine = _physicsEngine.CollisionEngine;
        }

        public override void Initialize()
        {
            base.Initialize();
            LoadCurrentLevel();
        }

        /// <summary>
        /// Loads the current level based on the level number.
        /// Level files are expected to be named "00.txt", "01.txt", etc.
        /// </summary>
        private void LoadCurrentLevel()
        {
            string levelFile = $"{_currentLevelNumber:00}.txt";

            try
            {
                // Load the level - Note: Don't use File.Exists here since MonoGame handles content paths differently
                _currentLevel = new Level(Core.Content, levelFile);
            }
            catch (FileNotFoundException)
            {
                // If   no more levels exist, return to the title screen
                Core.ChangeScene(new TitleScene());
                return;
            }

        }

        /// <summary>
        /// Advances to the next level by incrementing the level counter
        /// and loading the corresponding level file
        /// </summary>
        private void NextLevel()
        {
            _currentLevelNumber++;
            LoadCurrentLevel();
        }

        public override void Update(GameTime gameTime)
        {
            // Update the current level
            _currentLevel?.Update(gameTime);

            // Check if the current level is completed
            if (_currentLevel?.IsCompleted == true)
            {
                NextLevel();
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Core.GraphicsDevice.Clear(Color.CornflowerBlue);

            Core.SpriteBatch.Begin(SpriteSortMode.Deferred,
                                    BlendState.AlphaBlend,
                                    SamplerState.PointClamp,    // Use point sampling for crisp pixel art
                                    null,
                                    null,
                                    null,
                                    null);
            _currentLevel?.Draw(Core.SpriteBatch);
            Core.SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
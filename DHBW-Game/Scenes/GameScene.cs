using DHBW_Game.Levels;
using GameLibrary;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using DHBW_Game.Question_System;
using DHBW_Game.Save_System;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameTutorial;
using MonoGameTutorial.UI;
using System.Collections.Generic;
using DHBW_Game.UI;

namespace DHBW_Game.Scenes
{

    public class NewLevelEventArgs
    {

        public double Duration;

        public NewLevelEventArgs(double duration)
        {
            Duration = duration;
        }
    }
    /// <summary>
    /// Scene that handles the game's level progression, loading levels sequentially
    /// and managing transitions between them.
    /// </summary>
    public class GameScene : Scene
    {
        public delegate void NewLevelHandler(object sender, NewLevelEventArgs e);
        public event NewLevelHandler NewLevel;
        private DurationCollection _durationCollection;

        private Level _currentLevel;
        public int _currentLevelNumber = 0;
        private readonly CollisionEngine _collisionEngine;
        private readonly PhysicsEngine _physicsEngine;
        private GameSceneUI _ui;
        private QuestionPool _questionPool;
        private List<double> _grades = new List<double>();
        public List<double> Grades => _grades;

        public GameScene()
        {
            _physicsEngine = ServiceLocator.Get<PhysicsEngine>();
            _collisionEngine = _physicsEngine.CollisionEngine;
            _durationCollection = new DurationCollection();
            ServiceLocator.Register(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Core.ExitOnEscape = false;

            _questionPool = ServiceLocator.Get<QuestionPool>();

            // Load saved progress if available
            _currentLevelNumber = SaveManager.LoadProgress();
            _grades = SaveManager.LoadGrades();

            // Initialize the user interface for the game scene.
            InitializeUI();

            LoadCurrentLevel();
        }

        private void InitializeUI()
        {
            // Clear out any previous UI element in case we came here
            // from a different scene.
            GumService.Default.Root.Children.Clear();

            // Create the game scene ui instance.
            _ui = new GameSceneUI();
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
                NewLevel?.Invoke(this, new NewLevelEventArgs(_durationCollection.Durations[_currentLevelNumber]));
            }
            catch (FileNotFoundException)
            {
                // If no more levels exist, return to the title screen
                Core.ChangeScene(new TitleScene());
                SaveManager.ResetProgress();
                return;
            }

        }

        /// <summary>
        /// Advances to the next level by incrementing the level counter
        /// and loading the corresponding level file
        /// </summary>
        public void NextLevel()
        {
            _currentLevelNumber++;
            LoadCurrentLevel();
        }

        public void ShowQuestion()
        {
            var (q, idx) = _questionPool.GetNextQuestion();
            if (q != null)
            {
                ServiceLocator.Get<Game1>().Pause();
                _ui.ShowQuestion(q, idx, () => _questionPool.MarkAsAnswered(idx), () => ServiceLocator.Get<Game1>().QuestionResume());
            }
        }

        public void ShowGameOver()
        {
            if (_ui != null)
                _ui.ShowGameOver();
        }
        public void ShowWinFloorPanel()
        {
            if (_ui != null)
                _ui.ShowWinFloorPanel();
        }
        public void ShowFinalWinPanel()
        {
            if (_ui != null)
                _ui.ShowFinalWinPanel();
        }

        public override void Update(GameTime gameTime)
        {
            // Ensure the UI is always updated.
            _ui.Update(gameTime);

            base.Update(gameTime);

            // Temporary demonstration code for the question display system
            if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Q))
            {
                ShowQuestion();
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

            // Update the current level
            _currentLevel?.Update(gameTime);

            // Check if the current level is completed
            if (_currentLevel?.IsCompleted == true)
            {
                // Save progress for the next level
                SaveManager.SaveProgress(_currentLevelNumber + 1);

                ServiceLocator.Get<Game1>().GameOver();
                ShowWinFloorPanel();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Core.GraphicsDevice.Clear(Color.CornflowerBlue);

            Core.SpriteBatch.Begin(SpriteSortMode.Deferred,
                                    BlendState.AlphaBlend,
                                    SamplerState.PointClamp,
                                    null,
                                    null,
                                    null,
                                    null);
            //_collisionEngine.VisualizeColliders();
            _currentLevel?.Draw(Core.SpriteBatch);
            Core.SpriteBatch.End();

            base.Draw(gameTime);

            // Draw the UI last (overlays everything else).
            _ui.Draw();
        }
    }
}
using System;
using System.Xml.Linq;
using DHBW_Game.Question_System;
using DHBW_Game.Scenes;
using DHBW_Game.UI;
using GameLibrary;
using GameLibrary.Graphics;
using GameLibrary.Scenes;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using static DHBW_Game.UI.QuestionPanel;

namespace MonoGameTutorial.UI;

/// <summary>
/// Represents the user interface for the game scene, managing the pause and question panels.
/// </summary>
public class GameSceneUI : ContainerRuntime
{
    // The sound effect to play for auditory feedback of the user interface.
    private SoundEffect _uiSoundEffect;

    // The pause panel
    private PausePanel _pausePanel;

    // The question panel
    private QuestionPanel _questionPanel;

    // The options panel
    private OptionsPanel _optionsPanel;

    // The GameOver panel
    private GameOverPanel _gameOverPanel;

    private WinFloorPanel _winFloorPanel;

    private FinalWinPanel _finalWinPanel;

    public OptionsSlider MusicSlider { get; private set; }
    private ContainerRuntime _audioContainer;
    public GPAIndicatorUI _GPAIndicatorUI { get; private set; }
    private ContainerRuntime Container;
    private Panel hud;
    private TextRuntime floorText;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameSceneUI"/> class.
    /// </summary>
    public GameSceneUI()
    {
        // Set the container to fill the entire screen
        Dock(Gum.Wireframe.Dock.Fill);

        // Add the container to the root element
        this.AddToRoot();

        // Get a reference to the content manager registered with GumService
        ContentManager content = GumService.Default.ContentLoader?.XnaContentManager;

        // Load the sound effect and texture atlas for UI elements
        _uiSoundEffect = content?.Load<SoundEffect>("audio/ui");
        TextureAtlas atlas = TextureAtlas.FromFile(content, "images/atlas-definition.xml");

        // Initialize question panel
        _questionPanel = new QuestionPanel(atlas, _uiSoundEffect);
        _questionPanel.Answer += HandleAnswer;
        _questionPanel.AddToRoot();
        
        // Initialize pause panel
        _pausePanel = new PausePanel(atlas, _uiSoundEffect,
            () =>
            {
                _optionsPanel.Show();
                _pausePanel.Hide();
            });
        _pausePanel.AddToRoot();

        // Create options panel
        _optionsPanel = new OptionsPanel(atlas, _uiSoundEffect,
            () =>
            {
                _optionsPanel.Hide();
                _pausePanel.Show();
            },
            () =>
            {
                _optionsPanel.Hide();
            },
            false);
        _optionsPanel.AddToRoot();

        // Initialize gameOver panel
        _gameOverPanel = new GameOverPanel(atlas, _uiSoundEffect);
        _gameOverPanel.AddToRoot();

        // Initialize winFloor panel
        _winFloorPanel = new WinFloorPanel(atlas, _uiSoundEffect);
        _winFloorPanel.AddToRoot();

        // Initialize finalWin panel
        _finalWinPanel = new FinalWinPanel(atlas, _uiSoundEffect);
        _finalWinPanel.AddToRoot();

        hud = new Panel();
        hud.Dock(Gum.Wireframe.Dock.Fill);
        hud.AddToRoot();

        _GPAIndicatorUI = new GPAIndicatorUI(atlas, new Stages(new Stage(100, Color.Lime, 0), new Stage(45, Color.Lime, 1), new Stage(30, Color.Yellow, 2), new Stage(15, Color.Orange, 3), new Stage(0, Color.Red, 4)));
        _GPAIndicatorUI.Name = "bar";
        _GPAIndicatorUI.Anchor(Gum.Wireframe.Anchor.TopRight);
        _GPAIndicatorUI.X = -2;
        _GPAIndicatorUI.Y = .5f;
        _GPAIndicatorUI.Value = 1f;

        hud.AddChild(_GPAIndicatorUI);

        GameScene gameScene = (GameScene)ServiceLocator.Get<Scene>();

        floorText = new TextRuntime();
        floorText.Red = 70;
        floorText.Green = 86;
        floorText.Blue = 130;
        floorText.CustomFontFile = @"fonts/04b_30.fnt";
        floorText.FontScale = 0.2f;
        floorText.UseCustomFont = true;
        floorText.Text = "Floor: "+ (gameScene._currentLevelNumber+1).ToString();
        floorText.Anchor(Gum.Wireframe.Anchor.TopLeft);
        floorText.X = 2;
        floorText.Y = 1;
        floorText.OutlineThickness = 1;

        hud.AddChild(floorText);
    }

    /// <summary>
    /// Handles the event when a UI element receives focus, playing a sound effect.
    /// </summary>
    /// <param name="sender">The UI element that received focus.</param>
    /// <param name="args">Event arguments.</param>
    private void OnElementGotFocus(object sender, EventArgs args)
    {
        // Play the UI sound effect for auditory feedback when a UI element gains focus
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
    }

    public void HandleAnswer(object sender, AnswerEventArgs e)
    {
        _GPAIndicatorUI.HandleAnswer(e.CorrectAnswer);
    }

    /// <summary>
    /// Displays a multiple-choice question in the question panel.
    /// </summary>
    /// <param name="question">The multiple-choice question to display.</param>
    /// <param name="onAnswered">The action to invoke when the question is answered.</param>
    /// <param name="onClose">The action to invoke when the question panel is closed.</param>
    public void ShowQuestion(MultipleChoiceQuestion question, Action onAnswered, Action onClose)
    {
        // Configure the question panel with the provided question and callbacks
        _questionPanel.SetQuestion(question, onAnswered, onClose);
        
        // Show the question panel
        _questionPanel.IsVisible = true;
    }

    /// <summary>
    /// Shows the pause panel.
    /// </summary>
    public void ShowPausePanel()
    {
        _pausePanel.Show();
    }

    /// <summary>
    /// Hides the pause panel.
    /// </summary>
    public void HidePausePanel()
    {
        _pausePanel.Hide();
    }

    /// <summary>
    /// Shows the gameOver panel.
    /// </summary>
    public void ShowGameOver()
    {
        _questionPanel.IsVisible = false;
        _gameOverPanel.Show();
    }

    /// <summary>
    /// Shows the winFloor panel.
    /// </summary>
    public void ShowWinFloorPanel()
    {
        _winFloorPanel.UpdateGrade(_GPAIndicatorUI.Stage.GetCurrentGrade(_GPAIndicatorUI.Value), _GPAIndicatorUI.Stage.GetCurrentColor(_GPAIndicatorUI.Value*100));
        _winFloorPanel.Show();
    }

    /// <summary>
    /// Shows the finalWin panel.
    /// </summary>
    public void ShowFinalWinPanel()
    {
        _finalWinPanel.UpdateGrade(_GPAIndicatorUI.Stage.GetCurrentGrade(_GPAIndicatorUI.Value), _GPAIndicatorUI.Stage.GetCurrentColor(_GPAIndicatorUI.Value * 100), _GPAIndicatorUI.Stage.GetCurrentGrade(_GPAIndicatorUI.Value));
        _finalWinPanel.Show();
    }

    /// <summary>
    /// Updates the game scene UI.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public void Update(GameTime gameTime)
    {
        // Update the GumService to handle UI updates
        GumService.Default.Update(gameTime);
        _GPAIndicatorUI.Update(gameTime);
    }

    /// <summary>
    /// Draws the game scene UI.
    /// </summary>
    public void Draw()
    {
        // Render the UI elements using GumService
        GumService.Default.Draw();
    }
}
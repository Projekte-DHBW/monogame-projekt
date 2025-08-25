using System;
using DHBW_Game.Question_System;
using DHBW_Game.UI;
using GameLibrary;
using GameLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using MonoGameGum;
using MonoGameGum.GueDeriving;

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
            },
            () =>
            {
                _optionsPanel.Hide();
            });
        _optionsPanel.AddToRoot();
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
    /// Updates the game scene UI.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public void Update(GameTime gameTime)
    {
        // Update the GumService to handle UI updates
        GumService.Default.Update(gameTime);
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
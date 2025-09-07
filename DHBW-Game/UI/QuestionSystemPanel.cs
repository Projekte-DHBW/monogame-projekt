using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DHBW_Game.Question_System;
using GameLibrary;
using GameLibrary.Graphics;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace DHBW_Game.UI;

/// <summary>
/// Represents a UI panel for managing the question system, including API key input, question generation, and audio generation.
/// </summary>
public class QuestionSystemPanel : Panel
{
    // Back button for navigation
    private AnimatedButton _backButton;

    // Text element for displaying status messages
    private TextRuntime _statusText;

    // Button to trigger question generation
    private AnimatedButton _generateQuestionsButton;

    // Button to trigger audio generation
    private AnimatedButton _generateAudioButton;

    // Text box for entering the Gemini API key
    private TextBox _apiKeyTextBox;

    // Font used for text in the panel
    private const string OptionsFont = @"fonts/04b_30.fnt";

    // Sound effect for UI interactions
    private readonly SoundEffect _uiSoundEffect;

    // Callback for back button action
    private readonly Action _onBack;

    // Tasks for tracking asynchronous operations
    private Task _questionTask;
    private Task _audioTask;

    // Thread-safe queue for UI updates from async operations
    private readonly ConcurrentQueue<Action> _uiActions = new ConcurrentQueue<Action>();

    // Reference to the question pool for managing questions
    private readonly QuestionPool _questionPool;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionSystemPanel"/> class.
    /// </summary>
    /// <param name="atlas">The texture atlas for UI elements.</param>
    /// <param name="uiSoundEffect">The sound effect played on UI interactions.</param>
    /// <param name="onBack">The action to invoke when the back button is clicked.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="atlas"/> or <paramref name="uiSoundEffect"/> is null.</exception>
    public QuestionSystemPanel(TextureAtlas atlas, SoundEffect uiSoundEffect, Action onBack)
    {
        // Validate input parameters
        if (atlas == null) throw new ArgumentNullException(nameof(atlas));
        if (uiSoundEffect == null) throw new ArgumentNullException(nameof(uiSoundEffect));

        _uiSoundEffect = uiSoundEffect;

        // Retrieve the question pool from the service locator
        _questionPool = ServiceLocator.Get<QuestionPool>();

        // Store the back action callback
        _onBack = onBack;

        // Set panel to fill the parent container
        Dock(Gum.Wireframe.Dock.Fill);

        // Initially hide the panel
        IsVisible = false;

        // Initialize child UI elements
        InitializeChildren(atlas);
    }

    /// <summary>
    /// Initializes the child UI elements, including the background, title text, API key input, generate buttons, status text, and back button.
    /// </summary>
    /// <param name="atlas">The texture atlas for UI elements.</param>
    private void InitializeChildren(TextureAtlas atlas)
    {
        // Plain background (added first to be behind other elements)
        ColoredRectangleRuntime background = new ColoredRectangleRuntime();
        background.Color = Color.Gray * 0.9f; // Semi-transparent gray
        background.Width = 0; // Relative to parent (full width)
        background.Height = 0; // Relative to parent (full height)
        background.Dock(Gum.Wireframe.Dock.Fill);
        AddChild(background);

        // Gemini API Key label
        TextRuntime apiLabel = new TextRuntime();
        apiLabel.Anchor(Gum.Wireframe.Anchor.Top);
        apiLabel.Text = "Gemini API Key"; // Label text
        apiLabel.UseCustomFont = true;
        apiLabel.FontScale = 0.25f; // Smaller font size
        apiLabel.X = 0; // Position X coordinate
        apiLabel.Y = 20f;
        apiLabel.CustomFontFile = OptionsFont;
        AddChild(apiLabel);

        // Gemini API Key input field
        _apiKeyTextBox = new TextBox();
        _apiKeyTextBox.Anchor(Gum.Wireframe.Anchor.Top);
        _apiKeyTextBox.Visual.Y = 35f; // Below label
        _apiKeyTextBox.Width = ActualWidth - 24f; // Full width with padding
        _apiKeyTextBox.Height = 25f; // Fixed height for input field
        _apiKeyTextBox.Text = SecureApiKeyStorage.LoadApiKey() ?? ""; // Load saved API key
        _apiKeyTextBox.Placeholder = ""; // No placeholder
        _apiKeyTextBox.TextChanged += HandleApiKeyTextChanged; // Subscribe to text change event
        AddChild(_apiKeyTextBox);

        // Generate Questions button
        _generateQuestionsButton = new AnimatedButton(atlas);
        _generateQuestionsButton.Text = "Generate Questions"; // Button text
        _generateQuestionsButton.Anchor(Gum.Wireframe.Anchor.Top); // Anchor to the top
        _generateQuestionsButton.Visual.Y = 70f; // Below input field
        _generateQuestionsButton.Height = 20f;
        _generateQuestionsButton.Visual.WidthUnits = DimensionUnitType.Absolute;
        _generateQuestionsButton.Width = 200f;
        _generateQuestionsButton.Click += HandleGenerateQuestionsClick; // Subscribe to click event
        AddChild(_generateQuestionsButton);

        // Generate Audio button
        _generateAudioButton = new AnimatedButton(atlas);
        _generateAudioButton.Text = "Generate Audio"; // Button text
        _generateAudioButton.Anchor(Gum.Wireframe.Anchor.Top); // Anchor to the top
        _generateAudioButton.Visual.Y = 90f; // Below questions button
        _generateAudioButton.Height = 20f;
        _generateAudioButton.Visual.WidthUnits = DimensionUnitType.Absolute;
        _generateAudioButton.Width = 200f;
        _generateAudioButton.Click += HandleGenerateAudioClick; // Subscribe to click event
        AddChild(_generateAudioButton);

        // Status text below generate buttons
        _statusText = new TextRuntime();
        _statusText.Text = ""; // Initial empty text
        _statusText.UseCustomFont = true;
        _statusText.FontScale = 0.25f; // Smaller font size
        _statusText.Anchor(Gum.Wireframe.Anchor.Top); // Anchor to the top
        _statusText.HorizontalAlignment = HorizontalAlignment.Center; // Center text horizontally
        _statusText.X = 0; // No offset in x direction
        _statusText.Y = 125f; // Below audio button
        _statusText.CustomFontFile = OptionsFont;
        _statusText.Color = Color.Yellow; // Default color for status messages
        _statusText.WidthUnits = DimensionUnitType.Absolute;
        _statusText.Width = background.GetAbsoluteWidth() - 20;
        _statusText.Wrap = true;
        AddChild(_statusText);

        // Back button
        _backButton = new AnimatedButton(atlas);
        _backButton.Text = "BACK"; // Button text
        _backButton.Anchor(Gum.Wireframe.Anchor.BottomRight); // Anchor to bottom-right
        _backButton.X = -28f; // Horizontal position offset
        _backButton.Y = -10f; // Vertical position offset
        _backButton.Click += HandleBackButtonClick; // Subscribe to click event
        AddChild(_backButton);
    }

    /// <summary>
    /// Handles changes to the API key text box, saving the new value to secure storage.
    /// </summary>
    /// <param name="sender">The text box that triggered the event.</param>
    /// <param name="args">Event arguments.</param>
    private void HandleApiKeyTextChanged(object sender, EventArgs args)
    {
        // Save the API key to secure storage immediately
        SecureApiKeyStorage.SaveApiKey(_apiKeyTextBox.Text);
        _questionPool.RefreshQuestionGenerator(); // Refresh the generator after saving
    }

    /// <summary>
    /// Handles the "generate questions" button click, initiating question generation and updating the UI.
    /// </summary>
    /// <param name="sender">The button that triggered the event.</param>
    /// <param name="args">Event arguments.</param>
    private void HandleGenerateQuestionsClick(object sender, EventArgs args)
    {
        // Play UI sound effect for interaction
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Prevent multiple generation tasks from running concurrently
        if (_questionTask != null && !_questionTask.IsCompleted)
        {
            return;
        }

        // Update UI to indicate generation in progress
        _statusText.Text = "Generating questions...";
        _generateQuestionsButton.IsEnabled = false;
        _generateAudioButton.IsEnabled = false;

        // Start asynchronous question generation
        _questionTask = GenerateQuestionsAsync();
    }

    /// <summary>
    /// Asynchronously generates new questions, updating the UI.
    /// </summary>
    /// <returns>A task that completes when generation is done.</returns>
    private async Task GenerateQuestionsAsync()
    {
        try
        {
            // Define callback for status updates
            Action<string> updateStatus = (message) => _uiActions.Enqueue(() => _statusText.Text = message);

            // Generate 10 new questions, replacing existing ones
            await _questionPool.GenerateNewQuestions(10, false, updateStatus);

            // Queue UI update for success
            _uiActions.Enqueue(() =>
            {
                _statusText.Text = "Questions generated!";
            });
        }
        catch (Exception ex)
        {
            // Queue UI update for error
            _uiActions.Enqueue(() =>
            {
                Console.WriteLine(ex.Message);
                _statusText.Text = $"Error: {ex.Message}";
            });
        }

        _generateQuestionsButton.IsEnabled = true;
        _generateAudioButton.IsEnabled = true;
    }

    /// <summary>
    /// Handles the "generate audio" button click, initiating audio generation and updating the UI.
    /// </summary>
    /// <param name="sender">The button that triggered the event.</param>
    /// <param name="args">Event arguments.</param>
    private void HandleGenerateAudioClick(object sender, EventArgs args)
    {
        // Play UI sound effect for interaction
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Prevent multiple audio tasks from running concurrently
        if (_audioTask != null && !_audioTask.IsCompleted)
        {
            return;
        }

        // Update UI to indicate generation in progress
        _statusText.Text = "Generating audio...";
        _generateAudioButton.IsEnabled = false;
        _generateQuestionsButton.IsEnabled = false;

        // Start asynchronous audio generation
        _audioTask = GenerateAudioAsync();
    }

    /// <summary>
    /// Asynchronously generates audio, updating the UI.
    /// </summary>
    /// <returns>A task that completes when generation is done.</returns>
    private async Task GenerateAudioAsync()
    {
        try
        {
            // Define callback for status updates
            Action<string> updateStatus = (message) => _uiActions.Enqueue(() => _statusText.Text = message);

            // Generate audio
            await _questionPool.GenerateAudioAsync(updateStatus);

            // Queue UI update for success
            _uiActions.Enqueue(() =>
            {
                _statusText.Text = "Audio generated!";
            });
        }
        catch (Exception ex)
        {
            // Queue UI update for error
            _uiActions.Enqueue(() =>
            {
                Console.WriteLine(ex.Message);
                _statusText.Text = $"Error: {ex.Message}";
            });
        }

        _generateAudioButton.IsEnabled = true;
        _generateQuestionsButton.IsEnabled = true;
    }

    /// <summary>
    /// Updates the panel, processing queued UI actions on the main thread.
    /// </summary>
    public override void Activity()
    {
        // Call base activity method
        base.Activity();

        // Process any queued UI actions
        while (_uiActions.TryDequeue(out var action))
        {
            action();
        }
    }

    /// <summary>
    /// Handles the back button click, triggering the back action and playing a sound.
    /// </summary>
    /// <param name="sender">The button that triggered the event.</param>
    /// <param name="args">Event arguments.</param>
    private void HandleBackButtonClick(object sender, EventArgs args)
    {
        // Play UI sound effect for interaction
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
        
        // Invoke the back action (e.g., return to options panel)
        _onBack?.Invoke();
    }

    /// <summary>
    /// Shows the question system panel and sets focus to the back button.
    /// </summary>
    public void Show()
    {
        // Make the panel visible
        IsVisible = true;
        
        // Set focus to the back button
        _backButton.IsFocused = true;
    }

    /// <summary>
    /// Hides the question system panel.
    /// </summary>
    public void Hide()
    {
        // Hide the panel
        IsVisible = false;
    }
}
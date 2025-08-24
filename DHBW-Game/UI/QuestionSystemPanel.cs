using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DHBW_Game.Question_System;
using GameLibrary;
using GameLibrary.Graphics;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace DHBW_Game.UI;

/// <summary>
/// Represents a UI panel for managing the question system, including API key input and question regeneration.
/// </summary>
public class QuestionSystemPanel : Panel
{
    // Back button for navigation
    private AnimatedButton _backButton;

    // Text element for displaying status messages
    private TextRuntime _statusText;
    
    // Button to trigger question regeneration
    private AnimatedButton _regenerateButton;
    
    // Text box for entering the Gemini API key
    private TextBox _apiKeyTextBox;

    // Font used for text in the panel
    private const string OptionsFont = @"fonts/04b_30.fnt";

    // Sound effect for UI interactions
    private readonly SoundEffect _uiSoundEffect;
    
    // Callback for back button action
    private readonly Action _onBack;
    
    // Task for tracking asynchronous question generation
    private Task _generationTask;
    
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
    /// Initializes the child UI elements, including the background, title text, API key input, regenerate button, status text, and back button.
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

        // Question System title text
        TextRuntime questionSystemText = new TextRuntime();
        questionSystemText.X = 10; // Position X coordinate
        questionSystemText.Y = 10; // Position Y coordinate (top of panel)
        questionSystemText.Text = "QUESTION SYSTEM"; // Title text
        questionSystemText.UseCustomFont = true; // Enable custom font
        questionSystemText.FontScale = 0.5f; // Font scaling
        questionSystemText.CustomFontFile = OptionsFont; // Set custom font file
        AddChild(questionSystemText);

        // Gemini API Key label
        TextRuntime apiLabel = new TextRuntime();
        apiLabel.Text = "Gemini API Key"; // Label text
        apiLabel.UseCustomFont = true;
        apiLabel.FontScale = 0.25f; // Smaller font size
        apiLabel.X = 12; // Position X coordinate
        apiLabel.Y = 56f; // Below title
        apiLabel.CustomFontFile = OptionsFont;
        AddChild(apiLabel);

        // Gemini API Key input field
        _apiKeyTextBox = new TextBox();
        _apiKeyTextBox.Anchor(Gum.Wireframe.Anchor.Top);
        _apiKeyTextBox.Visual.Y = 74f; // Below label
        _apiKeyTextBox.Width = ActualWidth - 24f; // Full width with padding
        _apiKeyTextBox.Height = 25f; // Fixed height for input field
        _apiKeyTextBox.Text = ""; // Initial empty text
        _apiKeyTextBox.Placeholder = ""; // No placeholder
        _apiKeyTextBox.TextChanged += HandleApiKeyTextChanged; // Subscribe to text change event
        AddChild(_apiKeyTextBox);

        // Regenerate Questions button
        _regenerateButton = new AnimatedButton(atlas);
        _regenerateButton.Text = "Regenerate Questions"; // Button text
        _regenerateButton.Anchor(Gum.Wireframe.Anchor.Top); // Anchor to the top
        _regenerateButton.Visual.Y = 110f; // Below input field
        _regenerateButton.Click += HandleRegenerateButtonClick; // Subscribe to click event
        AddChild(_regenerateButton);

        // Status text below regenerate button
        _statusText = new TextRuntime();
        _statusText.Text = ""; // Initial empty text
        _statusText.UseCustomFont = true;
        _statusText.FontScale = 0.25f; // Smaller font size
        _statusText.Anchor(Gum.Wireframe.Anchor.Top); // Anchor to the top
        _statusText.HorizontalAlignment = HorizontalAlignment.Center; // Center text horizontally
        _statusText.X = 0; // No offset in x direction
        _statusText.Y = 138f; // Below regenerate button
        _statusText.CustomFontFile = OptionsFont;
        _statusText.Color = Color.Yellow; // Default color for status messages
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
    }

    /// <summary>
    /// Handles the "regenerate questions" button click, initiating question generation and updating the UI.
    /// </summary>
    /// <param name="sender">The button that triggered the event.</param>
    /// <param name="args">Event arguments.</param>
    private void HandleRegenerateButtonClick(object sender, EventArgs args)
    {
        // Play UI sound effect for interaction
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Prevent multiple generation tasks from running concurrently
        if (_generationTask != null && !_generationTask.IsCompleted)
        {
            return;
        }

        // Update UI to indicate generation in progress
        _statusText.Text = "Generating...";
        _regenerateButton.IsEnabled = false;

        // Start asynchronous question generation
        _generationTask = GenerateAsync();
    }

    /// <summary>
    /// Asynchronously generates new questions and queues UI updates.
    /// </summary>
    /// <returns>A task that completes when generation is done.</returns>
    private async Task GenerateAsync()
    {
        try
        {
            // Generate 10 new questions, replacing existing ones
            await _questionPool.GenerateNewQuestions(10, false);
            
            // Queue UI update for success
            _uiActions.Enqueue(() =>
            {
                _statusText.Text = "Generated successfully!";
                _regenerateButton.IsEnabled = true;
            });
        }
        catch (Exception ex)
        {
            // Queue UI update for error
            _uiActions.Enqueue(() =>
            {
                Console.WriteLine(ex.Message);
                _statusText.Text = $"Error: {ex.Message}";
                _regenerateButton.IsEnabled = true;
            });
        }
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
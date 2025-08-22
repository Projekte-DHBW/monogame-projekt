using System;
using System.Collections.Generic;
using DHBW_Game.Question_System;
using GameLibrary;
using GameLibrary.Graphics;
using Gum.DataTypes;
using Microsoft.Xna.Framework.Audio;
using MonoGameGum;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace DHBW_Game.UI;

/// <summary>
/// Represents a UI panel for displaying multiple-choice questions with options, feedback, and a continue button.
/// </summary>
public class QuestionPanel : Panel
{
    // Texture atlas for UI elements
    private readonly TextureAtlas _atlas;
    
    // Sound effect for UI interactions
    private readonly SoundEffect _uiSoundEffect;

    // UI element for displaying the question text
    private TextRuntime _questionText;
    
    // List of buttons for answer options
    private List<AnimatedButton> _optionButtons = new List<AnimatedButton>();
    
    // Container for feedback UI elements
    private ContainerRuntime _feedbackContainer;
    
    // UI element for displaying feedback text
    private TextRuntime _feedbackText;
    
    // Button to continue after answering
    private AnimatedButton _continueButton;
    
    // Background for the feedback container
    private ColoredRectangleRuntime _feedbackBackground;
    
    // Background for the entire panel
    private ColoredRectangleRuntime _background; 

    // Current question being displayed
    private MultipleChoiceQuestion _currentQuestion;
    
    // Callback for when the question is answered
    private Action _onAnswered;
    
    // Callback for when the panel is closed
    private Action _onClose;

    // Font used for text in the panel
    private const string PanelFont = @"fonts/04b_30.fnt";

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionPanel"/> class.
    /// </summary>
    /// <param name="atlas">The texture atlas for UI elements.</param>
    /// <param name="uiSoundEffect">The sound effect played on UI interactions.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="atlas"/> or <paramref name="uiSoundEffect"/> is null.</exception>
    public QuestionPanel(TextureAtlas atlas, SoundEffect uiSoundEffect)
    {
        // Validate input parameters
        if (atlas == null) throw new ArgumentNullException(nameof(atlas));
        if (uiSoundEffect == null) throw new ArgumentNullException(nameof(uiSoundEffect));

        _atlas = atlas;
        _uiSoundEffect = uiSoundEffect;

        // Set the panel to fill the parent container
        Dock(Gum.Wireframe.Dock.Fill);
        // Initially hide the panel
        IsVisible = false;

        // Initialize child UI elements
        InitializeChildren();
    }

    /// <summary>
    /// Initializes the child UI elements, including background, question text, feedback container, and buttons.
    /// </summary>
    private void InitializeChildren()
    {
        // Create a semi-transparent background rectangle for the panel
        _background = new ColoredRectangleRuntime();
        _background.Dock(Gum.Wireframe.Dock.Fill);
        _background.Color = new Microsoft.Xna.Framework.Color(50, 50, 50, 200); // Semi-transparent dark background
        AddChild(_background);

        // Question text (positioned near top)
        _questionText = new TextRuntime();
        _questionText.HorizontalAlignment = HorizontalAlignment.Center; // Center text horizontally
        _questionText.VerticalAlignment = VerticalAlignment.Top; // Vertically align text to the top
        _questionText.Anchor(Gum.Wireframe.Anchor.Top);
        _questionText.X = 0; // No offset in x direction
        _questionText.Y = 10; // Position down from top
        _questionText.WidthUnits = DimensionUnitType.Absolute;
        _questionText.Width = _background.GetAbsoluteWidth() - 20; // Fit within background with padding
        _questionText.Wrap = true; // Enable text wrapping
        _questionText.UseCustomFont = true;
        _questionText.CustomFontFile = PanelFont; // Set custom font
        _questionText.FontScale = 0.25f; // Font scaling
        _questionText.Red = 255; // White text color
        _questionText.Green = 255;
        _questionText.Blue = 255;
        _questionText.Alpha = 255;
        AddChild(_questionText);

        // Feedback container (centered, initially hidden)
        _feedbackContainer = new ContainerRuntime();
        _feedbackContainer.Visible = false; // Initially hidden
        _feedbackContainer.Dock(Gum.Wireframe.Dock.Fill);
        AddChild(_feedbackContainer);

        // Add a feedback background
        _feedbackBackground = new ColoredRectangleRuntime();
        _feedbackBackground.Dock(Gum.Wireframe.Dock.Fill);
        _feedbackBackground.Alpha = 50; // Semi-transparent
        _feedbackBackground.Blue = 0; // Initial color (the blue color channel is always zero - red and green will be updated based on answer)
        _feedbackContainer.AddChild(_feedbackBackground);

        // Feedback text
        _feedbackText = new TextRuntime();
        _feedbackText.Anchor(Gum.Wireframe.Anchor.Top);
        _feedbackText.WidthUnits = DimensionUnitType.Absolute;
        _feedbackText.HorizontalAlignment = HorizontalAlignment.Center; // Center text horizontally
        _feedbackText.VerticalAlignment = VerticalAlignment.Center; // Center text vertically
        _feedbackText.X = 0; // No offset in x direction
        _feedbackText.Y = 20; // Position below top
        _feedbackText.Width = _background.GetAbsoluteWidth() - 20; // Fit within background with padding
        _feedbackText.Height = _background.GetAbsoluteHeight() - _feedbackText.Y - 50; // Fit within available space
        _feedbackText.Wrap = true; // Enable text wrapping
        _feedbackText.UseCustomFont = true;
        _feedbackText.CustomFontFile = PanelFont; // Set custom font
        _feedbackText.FontScale = 0.25f; // Font scaling
        _feedbackContainer.AddChild(_feedbackText);

        // Continue button
        _continueButton = new AnimatedButton(_atlas);
        _continueButton.Text = "Continue"; // Button text
        _continueButton.Anchor(Gum.Wireframe.Anchor.Bottom);
        _continueButton.Visual.Y = -10; // Position below feedback text
        _continueButton.Click += HandleContinueClicked; // Subscribe to click event
        _feedbackContainer.AddChild(_continueButton);
    }

    /// <summary>
    /// Configures the panel to display a multiple-choice question with its options and callbacks.
    /// </summary>
    /// <param name="question">The multiple-choice question to display.</param>
    /// <param name="onAnswered">The action to invoke when the question is answered.</param>
    /// <param name="onClose">The action to invoke when the panel is closed.</param>
    public void SetQuestion(MultipleChoiceQuestion question, Action onAnswered, Action onClose)
    {
        // Store question and callback data
        _currentQuestion = question;
        _onAnswered = onAnswered;
        _onClose = onClose;

        // Set question text, defaulting to a placeholder if null
        _questionText.Text = question?.QuestionText ?? "No question available";

        // Clear previous option buttons
        foreach (var button in _optionButtons)
        {
            button.RemoveFromRoot(); // Remove from UI hierarchy
        }
        _optionButtons.Clear();

        // Create buttons for each option
        if (question != null && question.Options != null)
        {
            // Calculate vertical space for option buttons
            float availableVerticalSpace = _background.GetAbsoluteHeight() - _questionText.GetAbsoluteHeight();
            float totalHeight = question.Options.Count * 20;
            float verticalOffset = (availableVerticalSpace - totalHeight) / 2;

            // Create a button for each option
            for (int i = 0; i < question.Options.Count; i++)
            {
                var optionIndex = i; // Capture for closure
                var button = new AnimatedButton(_atlas);
                button.Text = question.Options[i]; // Set option text
                button.Anchor(Gum.Wireframe.Anchor.Top);
                button.Visual.X = 0; // Center horizontally
                button.Visual.Y = verticalOffset + (i * 20); // Adjusted starting Y and spacing
                button.IsVisible = true; // Ensure visibility
                button.Click += (sender, args) => HandleOptionClicked(optionIndex); // Subscribe to click event
                AddChild(button);
                _optionButtons.Add(button);
            }
        }

        // Hide feedback initially
        _feedbackContainer.Visible = false;
    }

    /// <summary>
    /// Handles the click event for an option button, processing the answer and showing feedback.
    /// </summary>
    /// <param name="selectedIndex">The index of the selected option.</param>
    private void HandleOptionClicked(int selectedIndex)
    {
        // Play UI sound effect for interaction
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Mark the question as answered
        _onAnswered?.Invoke();

        // Determine if the selected answer is correct
        bool isCorrect = _currentQuestion != null && selectedIndex == _currentQuestion.CorrectOptionIndex;

        // Set feedback text and color based on correctness
        _feedbackText.Text = isCorrect ? "Correct!" : $"Incorrect! {_currentQuestion?.Explanation ?? "No explanation available"}";
        _feedbackText.Red = isCorrect ? 0 : 255; // Green for correct, red for incorrect
        _feedbackText.Green = isCorrect ? 255 : 0;
        _feedbackText.Blue = 0;
        _feedbackText.Alpha = 255;

        // Set feedback background color (green for correct, red for incorrect)
        _feedbackBackground.Red = isCorrect ? 0 : 255;
        _feedbackBackground.Green = isCorrect ? 255 : 0;

        // Hide option buttons
        foreach (var button in _optionButtons)
        {
            button.IsVisible = false;
        }

        // Show feedback
        _feedbackContainer.Visible = true;

        // Set focus to the continue button
        _continueButton.IsFocused = true;
    }

    /// <summary>
    /// Handles the click event for the continue button, closing the panel.
    /// </summary>
    /// <param name="sender">The button that triggered the event.</param>
    /// <param name="args">Event arguments.</param>
    private void HandleContinueClicked(object sender, EventArgs args)
    {
        // Play UI sound effect for interaction
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Hide the panel and invoke the close callback
        IsVisible = false;
        _onClose?.Invoke();
    }
}
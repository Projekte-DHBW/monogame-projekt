using System;
using System.Collections.Generic;
using DHBW_Game.Question_System;
using DHBW_Game.Scenes;
using GameLibrary;
using GameLibrary.Graphics;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGameGum;
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

    // Demo of tts
    private readonly SoundEffect _ttsDemo;
    
    // Container for question texture and nineslice background
    private ContainerRuntime _questionContainer;
    
    // Background for question container
    private NineSliceRuntime _questionBackground;
    
    // UI element for displaying the question as texture
    private SpriteRuntime _questionSprite;
    
    // List of buttons for answer options
    private readonly List<OptionButton> _optionButtons = new List<OptionButton>();
    
    // Container for feedback UI elements
    private ContainerRuntime _feedbackContainer;
    
    // UI element for displaying feedback as texture
    private SpriteRuntime _feedbackSprite;
    
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
    public class AnswerEventArgs
    {

        public bool CorrectAnswer;

        public AnswerEventArgs(bool correctAnswer)
        {
            CorrectAnswer = correctAnswer;
        }
    }
    public delegate void AnswerlHandler(object sender, AnswerEventArgs e);
    public event AnswerlHandler Answer;

    // Paths to fonts (if left null, the default fonts of the LaTeXRenderer will be used)
    private const string TextFontPath = null;
    private const string MathFontPath = null;
    private const float FontSizeText = 44f;
    private const float FontSizeMath = 36f;

    // Zoom level of the Gum camera; required to correctly scale rendered textures to match the UI resolution
    private readonly float _zoom = GumService.Default.Renderer.Camera.Zoom;

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

        _ttsDemo = Core.Content.Load<SoundEffect>("audio/ttsTestMariaIncreasedVolume");
    }

    /// <summary>
    /// Initializes the child UI elements, including the panel background, question container and sprite, feedback container, and continue button.
    /// </summary>
    private void InitializeChildren()
    {
        // Create a semi-transparent background rectangle for the entire panel
        _background = new ColoredRectangleRuntime();
        _background.Dock(Gum.Wireframe.Dock.Fill);
        _background.Color = new Microsoft.Xna.Framework.Color(50, 50, 50, 200); // Semi-transparent dark background
        AddChild(_background);
        
        // Question container (positioned near the top and centered horizontally; height will be set dynamically based on content)
        _questionContainer = new ContainerRuntime();
        _questionContainer.Anchor(Gum.Wireframe.Anchor.Top);
        _questionContainer.XOrigin = HorizontalAlignment.Center;
        _questionContainer.XUnits = GeneralUnitType.Percentage;
        _questionContainer.X = 50; // 50% to center horizontally
        _questionContainer.Y = 10;
        _questionContainer.WidthUnits = DimensionUnitType.Absolute;
        _questionContainer.HeightUnits = DimensionUnitType.Absolute;
        _questionContainer.Width = _background.GetAbsoluteWidth() - 20;
        AddChild(_questionContainer);
        
        // Static nine-slice background for the question container (matches the unfocused state of OptionButton for visual consistency)
        _questionBackground = new NineSliceRuntime();
        _questionBackground.Texture = _atlas.Texture;
        _questionBackground.TextureAddress = TextureAddress.Custom;
        _questionBackground.Color = Color.White * 0.1f;
        _questionBackground.Alpha = 200;
        TextureRegion unfocusedTextureRegion = _atlas.GetRegion("unfocused-button");
        _questionBackground.TextureTop = (int)(unfocusedTextureRegion.TopTextureCoordinate * _atlas.Texture.Height);
        _questionBackground.TextureLeft = (int)(unfocusedTextureRegion.LeftTextureCoordinate * _atlas.Texture.Width);
        _questionBackground.TextureHeight = (int)((unfocusedTextureRegion.BottomTextureCoordinate - unfocusedTextureRegion.TopTextureCoordinate) * _atlas.Texture.Height);
        _questionBackground.TextureWidth = (int)((unfocusedTextureRegion.RightTextureCoordinate - unfocusedTextureRegion.LeftTextureCoordinate) * _atlas.Texture.Width);
        _questionBackground.WidthUnits = DimensionUnitType.RelativeToParent;
        _questionBackground.HeightUnits = DimensionUnitType.RelativeToParent;
        _questionBackground.Width = 0;
        _questionBackground.Height = 0;  
        _questionContainer.AddChild(_questionBackground);
        
        // Question sprite (centered within the question container)
        _questionSprite = new SpriteRuntime();
        _questionSprite.XOrigin = HorizontalAlignment.Center;
        _questionSprite.XUnits = GeneralUnitType.Percentage;
        _questionSprite.YOrigin = VerticalAlignment.Center;
        _questionSprite.YUnits = GeneralUnitType.Percentage;
        _questionSprite.X = 50;
        _questionSprite.Y = 50;
        _questionSprite.WidthUnits = DimensionUnitType.Absolute;
        _questionSprite.HeightUnits = DimensionUnitType.Absolute;
        _questionContainer.AddChild(_questionSprite);

        // Feedback container (fills the panel and initially hidden)
        _feedbackContainer = new ContainerRuntime();
        _feedbackContainer.Visible = false; // Initially hidden
        _feedbackContainer.Dock(Gum.Wireframe.Dock.Fill);
        AddChild(_feedbackContainer);

        // Add a semi-transparent background for the feedback container
        _feedbackBackground = new ColoredRectangleRuntime();
        _feedbackBackground.Dock(Gum.Wireframe.Dock.Fill);
        _feedbackBackground.Alpha = 50; // Semi-transparent
        _feedbackBackground.Blue = 0; // Initial color (blue channel is always zero; red and green channels will be updated based on whether the answer is correct)
        _feedbackContainer.AddChild(_feedbackBackground);

        // Feedback sprite (centered within the feedback container)
        _feedbackSprite = new SpriteRuntime();
        _feedbackSprite.Anchor(Gum.Wireframe.Anchor.Top);
        _feedbackSprite.XOrigin = HorizontalAlignment.Center;
        _feedbackSprite.XUnits = GeneralUnitType.Percentage;
        _feedbackSprite.YOrigin = VerticalAlignment.Center;
        _feedbackSprite.YUnits = GeneralUnitType.Percentage;
        _feedbackSprite.X = 50;
        _feedbackSprite.Y = 50;
        _feedbackSprite.WidthUnits = DimensionUnitType.Absolute;
        _feedbackSprite.HeightUnits = DimensionUnitType.Absolute;
        _feedbackContainer.AddChild(_feedbackSprite);

        // Continue button (anchored to the bottom of the feedback container)
        _continueButton = new AnimatedButton(_atlas);
        _continueButton.Text = "Continue"; // Button text
        _continueButton.Anchor(Gum.Wireframe.Anchor.Bottom);
        _continueButton.Visual.Y = -10; // Position slightly above the bottom edge, below the feedback text
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
        
        // Render question text to texture, handling null or empty questions
        string questionStr = question?.QuestionText ?? "No question available";
        var questionTexture = LaTeXRenderer.Render(Core.GraphicsDevice, questionStr, TextFontPath, MathFontPath, FontSizeText, FontSizeMath, Color.White, (_questionContainer.GetAbsoluteWidth() - 20) * _zoom);
        _questionSprite.Texture = questionTexture;
        _questionSprite.Width = questionTexture.Width / _zoom;
        _questionSprite.Height = questionTexture.Height / _zoom;

        
        // Adjust question container size to fit the sprite with padding (matches OptionButton logic)
        _questionContainer.Height = _questionSprite.Height + 10;


        // Clear previous option buttons
        foreach (var button in _optionButtons)
        {
            button.RemoveFromRoot(); // Remove from UI hierarchy
        }
        _optionButtons.Clear();

        // Create buttons for each option
        if (question != null && question.Options != null)
        {
            // Calculate vertical space for option buttons, starting just below the question container
            float beginOfFreeSpace = _questionContainer.Y + _questionContainer.Height;
            float verticalOffset = beginOfFreeSpace + 5;
            

            // Total height of all rendered OptionButtons
            float totalOptionsHeight = 0f;
            
            // Create a button with sprite for each option
            for (int i = 0; i < question.Options.Count; i++)
            {
                var optionIndex = i; // Capture for closure
                var button = new OptionButton(_atlas);
                
                // Render option text to texture and set it on the button
                var optionTexture = LaTeXRenderer.Render(Core.GraphicsDevice, question.Options[i], TextFontPath, MathFontPath, FontSizeText, FontSizeMath, Color.White);
                button.SetOptionTexture(optionTexture);

                button.Anchor(Gum.Wireframe.Anchor.Top);
                button.Visual.Y = verticalOffset + totalOptionsHeight + 10f; // Adjusted starting Y and spacing
                button.Visual.Width = _background.GetAbsoluteWidth() - 20;
                button.IsVisible = true; // Ensure visibility
                button.Click += (sender, args) => HandleOptionClicked(optionIndex); // Subscribe to click event
                AddChild(button);
                _optionButtons.Add(button);

                totalOptionsHeight += button.Visual.GetAbsoluteHeight();
            }
        }

        // Hide feedback initially
        _feedbackContainer.Visible = false;

        Core.Audio.PlaySoundEffect(_ttsDemo, 1.0f, 0.0f, 0.0f, false);
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

        Answer?.Invoke(this, new AnswerEventArgs(isCorrect));

        // Render feedback text to texture, using appropriate color and content based on correctness
        string feedbackStr = isCorrect ? "Correct!" : $"Incorrect! {_currentQuestion?.Explanation ?? "No explanation available"}";
        Color feedbackColor = isCorrect ? Color.GreenYellow : Color.OrangeRed;
        var feedbackTexture = LaTeXRenderer.Render(Core.GraphicsDevice, feedbackStr, TextFontPath, MathFontPath, FontSizeText, FontSizeMath, feedbackColor, (_background.GetAbsoluteWidth() - 20) * _zoom);
        _feedbackSprite.Texture = feedbackTexture;
        _feedbackSprite.Width = feedbackTexture.Width / _zoom;
        _feedbackSprite.Height = feedbackTexture.Height / _zoom;

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
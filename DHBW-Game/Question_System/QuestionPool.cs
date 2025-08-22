using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DHBW_Game.Question_System;

/// <summary>
/// Manages a pool of multiple-choice questions, including loading, generating, and tracking answered questions.
/// </summary>
public class QuestionPool
{
    // List of questions in the pool
    private List<MultipleChoiceQuestion> _questions = new List<MultipleChoiceQuestion>();
    
    // Set of indices for answered questions
    private HashSet<int> _answeredIndices = new HashSet<int>();
    
    // File path for the XML file storing questions
    private readonly string _filePath;
    
    // Serializer for handling XML operations
    private readonly QuestionXmlSerializer _xmlSerializer;
    
    // Generator for creating new questions via API
    private readonly QuestionGenerator _questionGenerator;
    
    // Random number generator for getting a random question from the pool
    private readonly Random _randomNumberGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionPool"/> class, loading questions from the specified or default XML file.
    /// </summary>
    /// <param name="filePath">Optional path to the XML file. Defaults to user AppData directory.</param>
    /// <exception cref="InvalidOperationException">Thrown if the API key is not found.</exception>
    public QuestionPool(string filePath = null)
    {
        // Set the file path, defaulting to AppData directory if not provided
        _filePath = filePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DHBW-Game", "questions.xml");
        
        // Initialize the XML serializer
        _xmlSerializer = new QuestionXmlSerializer(_filePath);

        // Load the API key from secure storage
        var apiKey = SecureApiKeyStorage.LoadApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("API key not found. Save it using SecureApiKeyStorage.SaveApiKey().");
        }
        // Initialize the question generator with the API key
        _questionGenerator = new QuestionGenerator(apiKey);
        
        // Initialize the random number generator
        _randomNumberGenerator = new Random();
        
        // Load questions from the XML file
        LoadQuestions();
    }

    /// <summary>
    /// Loads questions from the XML file into the pool.
    /// </summary>
    private void LoadQuestions()
    {
        // Load questions using the XML serializer
        _questions = _xmlSerializer.LoadFromFile();
        
        // Clear answered indices to reset tracking
        _answeredIndices.Clear();
    }

    /// <summary>
    /// Generates new questions using the API and adds them to the pool.
    /// </summary>
    /// <param name="numberOfQuestions">The number of questions to generate.</param>
    /// <param name="keepExisting">Whether to keep existing questions or replace them.</param>
    /// <returns>A task that completes when the generation and saving are done.</returns>
    /// <exception cref="Exception">Thrown if the API fails to generate questions.</exception>
    public async Task GenerateNewQuestions(int numberOfQuestions, bool keepExisting)
    {
        // Generate questions via the API
        var xmlContent = await _questionGenerator.GenerateQuestions(numberOfQuestions);
        if (xmlContent.StartsWith("Error:"))
        {
            throw new Exception($"Failed to generate questions: {xmlContent}");
        }

        // Load the generated questions from the XML string
        var newQuestions = _xmlSerializer.LoadFromString(xmlContent);

        // Clear existing questions if not keeping them
        if (!keepExisting)
        {
            _questions.Clear();
        }
        // Add new questions to the pool
        _questions.AddRange(newQuestions);
        
        // Save the updated question list to the XML file
        _xmlSerializer.SaveToFile(_questions);
    }

    /// <summary>
    /// Retrieves a random unanswered question from the pool along with its index.
    /// </summary>
    /// <returns>A tuple containing the question and its index, or (null, -1) if no unanswered questions remain.</returns>
    public (MultipleChoiceQuestion Question, int Index) GetNextQuestion()
    {
        // Get indices of unanswered questions
        var availableIndices = Enumerable.Range(0, _questions.Count)
            .Where(i => !_answeredIndices.Contains(i))
            .ToList();

        // Return null if no unanswered questions are available
        if (availableIndices.Count == 0)
        {
            return (null, -1);
        }

        // Select a random index from available indices
        var selectedIndex = availableIndices[_randomNumberGenerator.Next(availableIndices.Count)];
        return (_questions[selectedIndex], selectedIndex);
    }

    /// <summary>
    /// Marks a question as answered by its index.
    /// </summary>
    /// <param name="index">The index of the question in the pool.</param>
    public void MarkAsAnswered(int index)
    {
        // Add the index to the answered set if valid
        if (index >= 0 && index < _questions.Count)
        {
            _answeredIndices.Add(index);
        }
    }

    /// <summary>
    /// Resets the answered status of all questions, making them all available again.
    /// </summary>
    public void ResetAnswered()
    {
        // Clear the answered indices set
        _answeredIndices.Clear();
    }

    /// <summary>
    /// Deletes a question from the pool by its index and adjusts answered indices.
    /// </summary>
    /// <param name="index">The index of the question to delete.</param>
    public void DeleteQuestion(int index)
    {
        // Validate the index and remove the question
        if (index >= 0 && index < _questions.Count)
        {
            _questions.RemoveAt(index);

            // Adjust answered indices to account for the removed question
            var newAnswered = new HashSet<int>();
            foreach (var i in _answeredIndices)
            {
                if (i < index)
                {
                    newAnswered.Add(i);
                }
                else if (i > index)
                {
                    newAnswered.Add(i - 1);
                }
            }
            _answeredIndices = newAnswered;

            // Save the updated question list to the XML file
            _xmlSerializer.SaveToFile(_questions);
        }
    }

    /// <summary>
    /// Edits a question in the pool by replacing it with a new one at the specified index.
    /// </summary>
    /// <param name="index">The index of the question to edit.</param>
    /// <param name="updatedQuestion">The updated question object.</param>
    public void EditQuestion(int index, MultipleChoiceQuestion updatedQuestion)
    {
        // Validate the index and question, then update the question
        if (index >= 0 && index < _questions.Count && updatedQuestion != null)
        {
            _questions[index] = updatedQuestion;
            
            // Save the updated question list to the XML file
            _xmlSerializer.SaveToFile(_questions);
        }
    }

    /// <summary>
    /// Gets all questions in the pool as a read-only list.
    /// </summary>
    public IReadOnlyList<MultipleChoiceQuestion> AllQuestions => _questions.AsReadOnly();

    /// <summary>
    /// Gets the number of unanswered questions in the pool.
    /// </summary>
    public int UnansweredCount => _questions.Count - _answeredIndices.Count;
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml.Linq;

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
    private QuestionGenerator _questionGenerator;

    // Random number generator for selecting a random question from the pool
    private readonly Random _randomNumberGenerator;

    // List to track running Python processes
    private readonly List<Process> _runningProcesses = new List<Process>();

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

        if (!string.IsNullOrEmpty(apiKey))
        {
            // Initialize the question generator with the API key
            _questionGenerator = new QuestionGenerator(apiKey);
        }

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
    /// Generates new questions using the API and adds them to the pool, saving to file. Deletes existing audio files.
    /// </summary>
    /// <param name="numberOfQuestions">The number of questions to generate.</param>
    /// <param name="keepExisting">Whether to keep existing questions or replace them.</param>
    /// <param name="updateStatus">Callback to update UI status.</param>
    /// <returns>A task that completes when the generation and saving are done.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the API key is missing.</exception>
    /// <exception cref="Exception">Thrown if the API fails to generate questions or returns no questions.</exception>
    public async Task GenerateNewQuestions(int numberOfQuestions, bool keepExisting, Action<string> updateStatus = null)
    {
        if (_questionGenerator == null)
        {
            throw new InvalidOperationException("API key not set. Please provide a valid API key to generate questions.");
        }

        updateStatus?.Invoke("Generating questions...");

        // Define audio directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var audioDir = Path.Combine(appData, "DHBW-Game", "audio", "questions");

        // Clear existing audio files to avoid mismatches (if no new audio files are generated, the game would think that the old audio files belong to the new questions)
        try
        {
            ClearExistingAudioFiles(audioDir, null); // Currently the UI update status is not needed
        }
        catch (Exception ex)
        {
            updateStatus?.Invoke($"Warning: Failed to clear old audio files: {ex.Message}");
            Console.WriteLine($"Failed to clear old audio files: {ex.Message}");
        }

        // Generate and parse questions via the API
        var newQuestions = await _questionGenerator.GenerateQuestions(numberOfQuestions);

        // Check if any questions were returned
        if (newQuestions.Count == 0)
        {
            throw new Exception("Failed to generate questions: Invalid or empty response from API.");
        }

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
    /// Ensures the TTS venv is set up and generates audio for questions using lecturer voice samples.
    /// </summary>
    /// <param name="voicePromptPath">Path to a single lecturer voice sample WAV file. If null, uses per-lecturer voices from voices_dir.</param>
    /// <param name="updateStatus">Callback to update UI status.</param>
    /// <returns>A task that completes when audio files are detected or timeout occurs.</returns>
    private async Task EnsureTTSSetupAndGenerateAudio(string voicePromptPath = null, Action<string> updateStatus = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var venvPath = Path.Combine(appData, "DHBW-Game", "tts_venv");
        var audioDir = Path.Combine(appData, "DHBW-Game", "audio", "questions");

        // Find project root by searching for TTS\tts_controller.py
        var projectRoot = AppDomain.CurrentDomain.BaseDirectory;
        while (!File.Exists(Path.Combine(projectRoot, "TTS", "tts_controller.py")) && !string.IsNullOrEmpty(projectRoot))
        {
            projectRoot = Path.GetDirectoryName(projectRoot);
        }
        if (string.IsNullOrEmpty(projectRoot))
        {
            updateStatus?.Invoke("Error: Could not find TTS\\tts_controller.py");
            Console.WriteLine("Error: Could not find project root containing TTS\\tts_controller.py");
            return;
        }

        var scriptPath = Path.Combine(projectRoot, "TTS", "setup_tts.ps1");
        var voicesDir = Path.Combine(projectRoot, "TTS", "voices");

        // Check if questions.xml exists
        if (!File.Exists(_filePath))
        {
            updateStatus?.Invoke($"Error: Questions XML not found at {_filePath}");
            Console.WriteLine($"Questions XML not found at {_filePath}. Skipping TTS generation.");
            return;
        }

        // Count expected number of questions from XML
        int expectedAudioFiles = 0;
        try
        {
            var xmlDoc = XDocument.Load(_filePath);
            expectedAudioFiles = xmlDoc.Element("Questions")?.Elements("Question").Count() ?? 0;
        }
        catch (Exception ex)
        {
            updateStatus?.Invoke($"Error: Failed to parse questions.xml: {ex.Message}");
            Console.WriteLine($"Failed to parse questions.xml: {ex.Message}");
            return;
        }

        if (expectedAudioFiles == 0)
        {
            updateStatus?.Invoke("No questions to generate audio for.");
            return;
        }

        // Set up venv if needed
        if (!await SetupTTSVenvIfNeeded(venvPath, scriptPath, updateStatus))
        {
            return; // Early return on setup failure
        }

        // Clear existing audio files
        try
        {
            ClearExistingAudioFiles(audioDir, updateStatus);
        }
        catch (Exception ex)
        {
            updateStatus?.Invoke($"Warning: Failed to clear old audio files: {ex.Message}");
            Console.WriteLine($"Failed to clear old audio files: {ex.Message}");
        }

        // Generate audio
        if (!await GenerateAudioWithTTS(projectRoot, _filePath, audioDir, voicePromptPath, voicesDir, expectedAudioFiles, updateStatus))
        {
            updateStatus?.Invoke("Warning: Audio generation incomplete. Check Python window.");
        }
        else
        {
            updateStatus?.Invoke("Audio generated!");
        }
    }

    /// <summary>
    /// Sets up the TTS virtual environment if it doesn't exist by running the setup PowerShell script.
    /// </summary>
    /// <param name="venvPath">Path to the venv directory.</param>
    /// <param name="setupScriptPath">Path to the setup_tts.ps1 script.</param>
    /// <param name="updateStatus">Callback to update UI status.</param>
    /// <returns>True if setup succeeds or venv already exists; false on timeout or error.</returns>
    private async Task<bool> SetupTTSVenvIfNeeded(string venvPath, string setupScriptPath, Action<string> updateStatus = null)
    {
        var venvPython = Path.Combine(venvPath, "Scripts", "python.exe");

        // Check and run setup script if venv is missing
        if (!File.Exists(venvPython))
        {
            var setupArgs = $"-ExecutionPolicy Bypass -File \"{setupScriptPath}\" -VenvPath \"{venvPath}\"";
            Console.WriteLine($"Running setup detached: cmd /c start powershell.exe {setupArgs}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" powershell.exe {setupArgs}",
                    UseShellExecute = true,
                    CreateNoWindow = false
                }
            };
            process.Start();
            _runningProcesses.Add(process); // Track the process

            // Poll for venv creation
            updateStatus?.Invoke("Setting up TTS environment...");
            bool setupComplete = false;
            const int setupTimeoutMs = 1800 * 1000; // 30 minutes
            const int setupPollIntervalMs = 5000; // Check every 5 seconds
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < setupTimeoutMs)
            {
                if (File.Exists(venvPython))
                {
                    setupComplete = true;
                    break;
                }
                await Task.Delay(setupPollIntervalMs);
            }

            if (!setupComplete)
            {
                updateStatus?.Invoke("Error: TTS setup timed out. Check the PowerShell window for issues.");
                Console.WriteLine("TTS setup timed out.");
                return false;
            }
        }

        return true; // Venv exists or setup succeeded
    }

    /// <summary>
    /// Clears existing question audio files from the specified directory.
    /// </summary>
    /// <param name="audioDir">Path to the audio directory.</param>
    /// <param name="updateStatus">Callback to update UI status.</param>
    private void ClearExistingAudioFiles(string audioDir, Action<string> updateStatus = null)
    {
        if (Directory.Exists(audioDir))
        {
            foreach (var file in Directory.GetFiles(audioDir, "question_*.wav"))
            {
                File.Delete(file);
            }
            updateStatus?.Invoke("Cleared existing audio files.");
        }
    }

    /// <summary>
    /// Launches the TTS Python script to generate audio files and polls for completion asynchronously.
    /// </summary>
    /// <param name="projectRoot">Path to the project root.</param>
    /// <param name="xmlPath">Path to questions.xml.</param>
    /// <param name="outputDir">Output directory for audio files.</param>
    /// <param name="voicePromptPath">Path to single voice sample (null for per-lecturer).</param>
    /// <param name="voicesDir">Directory for per-lecturer voice samples.</param>
    /// <param name="expectedAudioFiles">Number of expected audio files.</param>
    /// <param name="updateStatus">Callback to update UI status.</param>
    /// <returns>A task that completes with true if all expected files are generated; false on timeout.</returns>
    private async Task<bool> GenerateAudioWithTTS(string projectRoot, string xmlPath, string outputDir, string voicePromptPath, string voicesDir, int expectedAudioFiles, Action<string> updateStatus = null)
    {
        var ttsScript = Path.Combine(projectRoot, "TTS", "tts_controller.py");
        Directory.CreateDirectory(outputDir);

        // Launch TTS audio generation
        string cmdArgs;
        if (!string.IsNullOrEmpty(voicePromptPath))
        {
            // Single voice mode
            if (!File.Exists(voicePromptPath))
            {
                updateStatus?.Invoke($"Error: Voice sample not found at {voicePromptPath}");
                Console.WriteLine($"Voice sample not found at {voicePromptPath}. Skipping TTS generation.");
                return false;
            }
            cmdArgs = $"\"{ttsScript}\" --xml \"{xmlPath}\" --output \"{outputDir}\" --voice \"{voicePromptPath}\"";
        }
        else
        {
            // Per-lecturer mode, let Python handle lecturer iteration
            cmdArgs = $"\"{ttsScript}\" --xml \"{xmlPath}\" --output \"{outputDir}\" --voices_dir \"{voicesDir}\"";
        }

        var venvPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DHBW-Game", "tts_venv");
        var venvPython = Path.Combine(venvPath, "Scripts", "python.exe");

        Console.WriteLine($"Running TTS detached: cmd /c start \"\" \"{venvPython}\" {cmdArgs}");
        var processTTS = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{venvPython}\" {cmdArgs}",
                UseShellExecute = true,
                CreateNoWindow = true
            }
        };
        processTTS.Start();
        _runningProcesses.Add(processTTS); // Track the process

        // Poll for audio files asynchronously
        updateStatus?.Invoke("Generating audio...");
        bool audioGenerated = false;
        const int audioTimeoutMs = 1800 * 1000; // 30 minutes
        const int audioPollIntervalMs = 2000; // Check every 2 seconds
        var audioStartTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - audioStartTime).TotalMilliseconds < audioTimeoutMs)
        {
            var audioFiles = Directory.GetFiles(outputDir, "question_*.wav");
            var generatedCount = audioFiles.Length;
            if (generatedCount >= expectedAudioFiles)
            {
                audioGenerated = true;
                break;
            }
            // Update progress
            updateStatus?.Invoke($"Generating audio ({generatedCount}/{expectedAudioFiles} files complete)...");

            await Task.Delay(audioPollIntervalMs); // Non-blocking await to yield control to the game loop
        }

        return audioGenerated;
    }

    /// <summary>
    /// Generates audio for questions.
    /// </summary>
    /// <param name="updateStatus">Callback to update UI status.</param>
    /// <returns>A task that completes when audio files are detected or timeout occurs.</returns>
    public async Task GenerateAudioAsync(Action<string> updateStatus = null)
    {
        // Set to null to use per-lecturer voices
        await EnsureTTSSetupAndGenerateAudio(null, updateStatus);
    }

    /// <summary>
    /// Cancels all running Python processes (e.g., TTS setup and audio generation).
    /// </summary>
    public void CancelRunningProcesses()
    {
        foreach (var process in _runningProcesses.ToList()) // Create a copy to avoid modification issues
        {
            try
            {
                if (!process.HasExited)
                {
                    // Kill the process and its descendants (cmd.exe and python.exe)
                    process.Kill(true); // true ensures the entire process tree is terminated
                    Console.WriteLine($"Terminated process with ID {process.Id}");
                }
                _runningProcesses.Remove(process); // Remove from tracking
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to terminate process with ID {process.Id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Refreshes the question generator by reloading the API key from storage.
    /// </summary>
    public void RefreshQuestionGenerator()
    {
        var apiKey = SecureApiKeyStorage.LoadApiKey();
        _questionGenerator = string.IsNullOrEmpty(apiKey) ? null : new QuestionGenerator(apiKey);
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
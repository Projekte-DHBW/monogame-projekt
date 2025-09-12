using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DHBW_Game.Question_System;

/// <summary>
/// Generates multiple-choice questions for the game using an external API.
/// </summary>
public class QuestionGenerator
{
    // API key for accessing the generative language model
    private readonly string _apiKey;

    // Shared HTTP client for making API requests
    private static readonly HttpClient _client = new HttpClient();

    // XML format template for generating questions
    private static string _format = "<Questions>\n<Question Topic=\"Programming\">\n<Text>Which of the following is a valid way to declare an integer variable in C++?</Text>\n<TTSFriendlyText>Which is a valid C plus plus integer declaration?</TTSFriendlyText>\n<LecturerID>berninger</LecturerID>\n<Options>\n<Option>A: int x;</Option>\n<Option>B: x integer;</Option>\n<Option>C: Integer x;</Option>\n<Option>D: var x: int;</Option>\n</Options>\n<CorrectOptionIndex>0</CorrectOptionIndex>\n<Explanation>In C++, `int x;` is the correct syntax to declare an integer variable named `x`.</Explanation>\n</Question>\n</Questions>";

    // String for the API to know which lecturer ID to assign to the question
    private static string _lecturers = "berninger: programming, data structures, algorithms etc.; schwenker: math";

    // System prompt template for the API to generate questions
    private static string _systemPromptTemplate = @"You are a multiple choice question generator for a student game. The questions are for second semester computer science students and could involve topics like analysis, linear algebra, programming, data structures and algorithms. Generate exactly {0} questions in the following XML format. Keep option text short (<= 40 characters). For mathematical expressions, formulas, superscripts, or subscripts in the question text, options, or explanation, always use inline LaTeX delimited by '\', e.g. 'The formula for kinetic energy is \( E_k = \frac{{1}}{{2}}mv^2 \), where m is mass and v is velocity.'. Use LaTeX as well for big O notation and similar stuff. Do not use plain text superscripts, subscripts, or other non-LaTeX formatting for math. For each question, include a <TTSFriendlyText> element with a plain-text version of the question optimized for speech (no LaTeX, e.g., 'The complexity is Big O of n squared' for \( O(n^2) \)). Assign a <LecturerID> based on the topic: {1}. Do not add any additional text, explanations, introductions, markdown, code blocks, or anything else. In all XML element content, properly escape XML special characters: replace '&' with '&amp', '<' with '&lt', '>' with '&gt', '\' with '&quot;', ' with '&apos;'. Output only the pure XML string, starting directly with the <Questions> tag and ending with </Questions>. Don't start with ```xml.\n\n" + _format;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionGenerator"/> class with the specified API key.
    /// </summary>
    /// <param name="apiKey">The API key for accessing the generative language model.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="apiKey"/> is null or empty.</exception>
    public QuestionGenerator(string apiKey)
    {
        // Validate the API key
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty.");
        }
        _apiKey = apiKey;
    }

    /// <summary>
    /// Generates the specified number of multiple-choice questions using the external API and parses them into objects.
    /// </summary>
    /// <param name="numberOfQuestions">The number of questions to generate.</param>
    /// <returns>A task that resolves to a list of parsed <see cref="MultipleChoiceQuestion"/> objects.</returns>
    /// <exception cref="Exception">Thrown if the API fails to generate valid XML or parsing fails.</exception>
    public async Task<List<MultipleChoiceQuestion>> GenerateQuestions(int numberOfQuestions)
    {
        // Format the prompt with the desired number of questions and lecturer mapping
        string prompt = string.Format(_systemPromptTemplate, numberOfQuestions, _lecturers);
        string xmlContent = await GenerateWithGeminiAsync(prompt);

        // Check for API errors
        if (xmlContent.StartsWith("Error:"))
        {
            throw new Exception(xmlContent);
        }

        // Fix unescaped '&' characters in content (preserving valid entities like &lt;)
        xmlContent = Regex.Replace(xmlContent, @"&(?!#?[a-zA-Z0-9]+;)", "&amp;");

        // Parse the XML into question objects
        var serializer = new QuestionXmlSerializer();
        var questions = serializer.LoadFromString(xmlContent);

        // Warn if the number of questions doesn't match expectations
        if (questions.Count != numberOfQuestions)
        {
            Console.WriteLine($"Warning: Expected {numberOfQuestions} questions, got {questions.Count}.");
        }

        return questions;
    }

    /// <summary>
    /// Sends a request to the Gemini API to generate questions based on the provided prompt.
    /// </summary>
    /// <param name="prompt">The prompt to send to the API.</param>
    /// <returns>A task that resolves to the cleaned XML response or an error message.</returns>
    private async Task<string> GenerateWithGeminiAsync(string prompt)
    {
        // Construct the API URL with the encoded API key
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={Uri.EscapeDataString(_apiKey)}";
        
        // Create the request body with the prompt
        var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        // Send the POST request to the API
        var response = await _client.PostAsync(url, content);
        if (response.IsSuccessStatusCode)
        {
            // Read the response content
            var result = await response.Content.ReadAsStringAsync();

            // Parse the JSON response to extract the generated text
            var jsonDoc = JsonDocument.Parse(result);
            var rawText = jsonDoc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0]
                .GetProperty("text").GetString();

            // Return an error message if the API response is not valid
            if (rawText == null)
            {
                return "Error: Invalid content from API";
            }
            
            // Clean potential markdown code blocks and whitespace
            var cleanedText = rawText.Trim();

            // Use regex to strip common code fences (e.g., ```xml)
            cleanedText = Regex.Replace(cleanedText, @"^```(?:xml)?\s*\n?|\n?\s*```$", string.Empty, RegexOptions.Multiline | RegexOptions.Singleline);

            // Additional trim to remove leftover whitespace
            cleanedText = cleanedText.Trim();

            // Validate that the response starts with the expected <Questions> tag
            if (string.IsNullOrWhiteSpace(cleanedText) || !cleanedText.StartsWith("<Questions>"))
            {
                return "Error: Invalid content from API - " + cleanedText;
            }
            
            return cleanedText;
        }

        // Return an error message if the API request fails
        return "Error: " + response.ReasonPhrase;
    }
}
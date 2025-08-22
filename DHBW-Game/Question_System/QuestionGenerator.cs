using System;
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
    private static string _format = "<Questions>\n<Question Topic=\"Programming\">\n<Text>Which of the following is a valid way to declare an integer variable in C++?</Text>\n<Options>\n<Option>A: int x;</Option>\n<Option>B: x integer;</Option>\n<Option>C: Integer x;</Option>\n<Option>D: var x: int;</Option>\n</Options>\n<CorrectOptionIndex>0</CorrectOptionIndex>\n<Explanation>In C++, `int x;` is the correct syntax to declare an integer variable named `x`.</Explanation>\n</Question>\n</Questions>";

    // System prompt template for the API to generate questions
    private static string _systemPromptTemplate = "You are a multiple choice question generator for a mini student game. The questions are for computer science students in the second semester and could involve topics like analysis, linear algebra, programming, data structures and algorithms. Generate exactly {0} questions in the following XML format. Keep the option text short with <= 40 characters. Don't use superscript numbers directly in the question text, the answer options or the explanation. Do not add any additional text, explanations, introductions, markdown, code blocks, or anything else. Output only the pure XML string, starting directly with the <Questions> tag and ending with </Questions>. Don't start with ```xml.\n\n" + _format;

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
    /// Generates the specified number of multiple-choice questions using the external API.
    /// </summary>
    /// <param name="numberOfQuestions">The number of questions to generate.</param>
    /// <returns>A task that resolves to the XML string containing the generated questions or an error message.</returns>
    public async Task<string> GenerateQuestions(int numberOfQuestions)
    {
        // Format the system prompt with the desired number of questions
        string prompt = string.Format(_systemPromptTemplate, numberOfQuestions);
        return await GenerateWithGeminiAsync(prompt);
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
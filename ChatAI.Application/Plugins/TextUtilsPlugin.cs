using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;

namespace ChatAI.Application.Plugins;

/// <summary>
/// Text utilities plugin for Semantic Kernel
/// </summary>
public class TextUtilsPlugin
{
    [KernelFunction, Description("Convert text to uppercase")]
    public string ToUpperCase([Description("The text to convert")] string text)
    {
        return text.ToUpperInvariant();
    }

    [KernelFunction, Description("Convert text to lowercase")]
    public string ToLowerCase([Description("The text to convert")] string text)
    {
        return text.ToLowerInvariant();
    }

    [KernelFunction, Description("Reverse a string")]
    public string Reverse([Description("The text to reverse")] string text)
    {
        var chars = text.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    [KernelFunction, Description("Count the number of words in text")]
    public int CountWords([Description("The text to analyze")] string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
        
        return text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    [KernelFunction, Description("Count the number of characters in text")]
    public int CountCharacters([Description("The text to analyze")] string text)
    {
        return text?.Length ?? 0;
    }

    [KernelFunction, Description("Extract a substring from text")]
    public string Substring(
        [Description("The source text")] string text,
        [Description("Start index (0-based)")] int start,
        [Description("Length of substring")] int length)
    {
        if (start < 0 || start >= text.Length)
            throw new ArgumentException("Start index out of range");
        
        if (start + length > text.Length)
            length = text.Length - start;
        
        return text.Substring(start, length);
    }

    [KernelFunction, Description("Replace text within a string")]
    public string Replace(
        [Description("The source text")] string text,
        [Description("Text to find")] string oldValue,
        [Description("Replacement text")] string newValue)
    {
        return text.Replace(oldValue, newValue);
    }

    [KernelFunction, Description("Trim whitespace from both ends of text")]
    public string Trim([Description("The text to trim")] string text)
    {
        return text.Trim();
    }
}

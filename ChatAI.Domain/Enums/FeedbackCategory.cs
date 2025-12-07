namespace ChatAI.Domain.Enums;

/// <summary>
/// Categories for negative feedback
/// </summary>
public enum FeedbackCategory
{
    /// <summary>
    /// Response was factually incorrect
    /// </summary>
    Incorrect = 1,
    
    /// <summary>
    /// Response was not helpful or relevant
    /// </summary>
    NotHelpful = 2,
    
    /// <summary>
    /// Response was too long or verbose
    /// </summary>
    TooVerbose = 3,
    
    /// <summary>
    /// Response didn't use knowledge base when it should have
    /// </summary>
    MissingContext = 4,
    
    /// <summary>
    /// Response was inappropriate or offensive
    /// </summary>
    Inappropriate = 5,
    
    /// <summary>
    /// Other reason
    /// </summary>
    Other = 99
}

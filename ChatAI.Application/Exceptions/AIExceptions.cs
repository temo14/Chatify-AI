namespace ChatAI.Application.Exceptions;

/// <summary>
/// Base exception for AI-related errors
/// </summary>
public class AIException : Exception
{
    public AIException(string message) : base(message) { }
    public AIException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when AI service is unavailable or returns an error
/// </summary>
public class AIServiceException : AIException
{
    public AIServiceException(string message) : base(message) { }
    public AIServiceException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when tool execution fails
/// </summary>
public class ToolExecutionException : AIException
{
    public string ToolName { get; }
    
    public ToolExecutionException(string toolName, string message) : base(message)
    {
        ToolName = toolName;
    }
    
    public ToolExecutionException(string toolName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ToolName = toolName;
    }
}

/// <summary>
/// Thrown when a requested resource is not found
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// Thrown when authentication or authorization fails
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

/// <summary>
/// Thrown when validation fails (e.g., duplicate slug, invalid data)
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}

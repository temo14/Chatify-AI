using ChatAI.Application.Commands;
using ChatAI.Application.Validators;
using FluentValidation.TestHelper;

namespace ChatAI.Tests.Unit.Validators;

public class AddKnowledgeDocumentCommandValidatorTests
{
    private readonly AddKnowledgeDocumentCommandValidator _validator;

    public AddKnowledgeDocumentCommandValidatorTests()
    {
        _validator = new AddKnowledgeDocumentCommandValidator();
    }

    [Fact]
    public void Should_Pass_When_Valid_Command()
    {
        // Arrange
        var command = new AddKnowledgeDocumentCommand
        {
            Title = "How to use the API",
            Content = "This is a detailed guide on how to use our API...",
            Source = "documentation.md",
            Category = "documentation",
            MetadataJson = "{\"author\": \"admin\"}",
            IsActive = true
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Title_Empty()
    {
        // Arrange
        var command = new AddKnowledgeDocumentCommand
        {
            Title = "",
            Content = "Some content",
            IsActive = true
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_Fail_When_Content_Too_Short()
    {
        // Arrange
        var command = new AddKnowledgeDocumentCommand
        {
            Title = "Test",
            Content = "Too short",
            IsActive = true
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content must be at least 10 characters long");
    }

    [Fact]
    public void Should_Fail_When_Category_Has_Invalid_Characters()
    {
        // Arrange
        var command = new AddKnowledgeDocumentCommand
        {
            Title = "Test",
            Content = "This is valid content with enough characters",
            Category = "cat@gory!", // Invalid characters
            IsActive = true
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void Should_Fail_When_MetadataJson_Invalid()
    {
        // Arrange
        var command = new AddKnowledgeDocumentCommand
        {
            Title = "Test",
            Content = "This is valid content with enough characters",
            MetadataJson = "{ invalid json",
            IsActive = true
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetadataJson)
            .WithErrorMessage("MetadataJson must be valid JSON");
    }

    [Fact]
    public void Should_Pass_When_Optional_Fields_Null()
    {
        // Arrange
        var command = new AddKnowledgeDocumentCommand
        {
            Title = "Test Document",
            Content = "This is valid content with enough characters",
            Source = null,
            Category = null,
            MetadataJson = null,
            IsActive = true
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

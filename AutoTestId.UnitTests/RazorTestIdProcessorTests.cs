using Shouldly;

namespace AutoTestId.UnitTests;

public class RazorTestIdProcessorTests
{
    private readonly RazorTestIdProcessor _processor = new();

    [Fact]
    public void ProcessContent_ShouldAddTestId_WhenBasicHtmlTagHasNoTestId()
    {
        // Arrange
        const string content = "<div class=\"container\">\n    <h1>Hello World</h1>\n</div>";
        const string componentName = "MyComponent";
        
        // Act
        var result = _processor.ProcessContent(content, componentName);
        
        // Assert
        result.ShouldContain("data-testid=\"MyComponent\"");
        result.ShouldStartWith("<div data-testid=\"MyComponent\" class=\"container\">");
    }

    [Fact]
    public void ProcessContent_ShouldReplaceExistingTestId_WhenTagAlreadyHasTestId()
    {
        // Arrange
        const string content = "<div data-testid=\"old-test-id\" class=\"container\">\n    <h1>Hello World</h1>\n</div>";
        const string componentName = "MyComponent";
        
        // Act
        var result = _processor.ProcessContent(content, componentName);
        
        // Assert
        result.ShouldContain("data-testid=\"MyComponent\"");
        result.ShouldNotContain("data-testid=\"old-test-id\"");
        result.ShouldStartWith("<div data-testid=\"MyComponent\" class=\"container\">");
    }

    [Fact]
    public void ProcessContent_ShouldReturnUnchanged_WhenNoHtmlTagFound()
    {
        // Arrange
        const string content = "@code {\n    // Some C# code\n}";
        const string componentName = "MyComponent";
        
        // Act
        var result = _processor.ProcessContent(content, componentName);
        
        // Assert
        result.ShouldBe(content);
    }

    [Fact]
    public void ProcessContent_ShouldHandleTagWithNoAttributes()
    {
        // Arrange
        const string content = "<div>\n    <h1>Hello World</h1>\n</div>";
        const string componentName = "MyComponent";
        
        // Act
        var result = _processor.ProcessContent(content, componentName);
        
        // Assert
        result.ShouldStartWith("<div data-testid=\"MyComponent\">");
    }

    [Fact]
    public void ProcessContent_ShouldPreserveWhitespace()
    {
        // Arrange
        const string content = "  <div class=\"container\">\n    <h1>Hello World</h1>\n  </div>";
        const string componentName = "MyComponent";
        
        // Act
        var result = _processor.ProcessContent(content, componentName);
        
        // Assert
        result.ShouldStartWith("  <div data-testid=\"MyComponent\" class=\"container\">");
    }

    [Fact]
    public void ProcessContent_ShouldAddTestId_ToAllRootComponents()
    {
        // Arrange
        const string content = @"<div class=""header"">
    <h1>Header</h1>
</div>

<div class=""main"">
    <p>Main content</p>
</div>

<footer>
    <p>Footer</p>
</footer>";
        const string componentName = "MyComponent";
        
        // Act
        var result = _processor.ProcessContent(content, componentName);
        
        // Assert
        result.ShouldContain(@"<div data-testid=""MyComponent"" class=""header"">");
        result.ShouldContain(@"<div data-testid=""MyComponent"" class=""main"">");
        result.ShouldContain(@"<footer data-testid=""MyComponent"">");
        
        // Count occurrences of data-testid
        var testIdCount = System.Text.RegularExpressions.Regex.Matches(result, @"data-testid=""MyComponent""").Count;
        testIdCount.ShouldBe(3);
    }

    [Fact]
    public void ProcessContent_ShouldReplaceExistingTestIds_InMultipleRootComponents()
    {
        // Arrange
        const string content = @"<div data-testid=""old-id-1"" class=""header"">
    <h1>Header</h1>
</div>

<div data-testid=""old-id-2"" class=""main"">
    <p>Main content</p>
</div>";
        const string componentName = "MyComponent";
        
        // Act
        var result = _processor.ProcessContent(content, componentName);
        
        // Assert
        result.ShouldNotContain("old-id-1");
        result.ShouldNotContain("old-id-2");
        var testIdCount = System.Text.RegularExpressions.Regex.Matches(result, @"data-testid=""MyComponent""").Count;
        testIdCount.ShouldBe(2);
    }
}
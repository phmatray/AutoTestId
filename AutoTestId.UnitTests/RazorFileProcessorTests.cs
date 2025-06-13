using Shouldly;

namespace AutoTestId.UnitTests;

public class RazorFileProcessorTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly RazorFileProcessor _processor = new();

    public RazorFileProcessorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"AutoTestId_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void ProcessFile_ShouldAddTestId_ToRazorFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "TestComponent.razor");
        const string originalContent = "<div class=\"container\">\n    <h1>Hello World</h1>\n</div>";
        File.WriteAllText(filePath, originalContent);

        // Act
        _processor.ProcessFile(filePath);

        // Assert
        var newContent = File.ReadAllText(filePath);
        newContent.ShouldContain("data-testid=\"TestComponent\"");
        newContent.ShouldStartWith("<div data-testid=\"TestComponent\" class=\"container\">");
    }

    [Fact]
    public void ProcessFile_ShouldNotModifyFile_WhenNoHtmlTagFound()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "CodeOnly.razor");
        const string originalContent = "@code {\n    private int count = 0;\n}";
        File.WriteAllText(filePath, originalContent);
        var originalTimestamp = File.GetLastWriteTime(filePath);

        // Act
        Thread.Sleep(10); // Ensure different timestamp if file is modified
        _processor.ProcessFile(filePath);

        // Assert
        var newContent = File.ReadAllText(filePath);
        newContent.ShouldBe(originalContent);
        File.GetLastWriteTime(filePath).ShouldBe(originalTimestamp);
    }

    [Fact]
    public void ProcessDirectory_ShouldProcessAllRazorFiles()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "Components");
        Directory.CreateDirectory(subDir);

        var file1 = Path.Combine(_testDirectory, "Component1.razor");
        var file2 = Path.Combine(subDir, "Component2.razor");
        var file3 = Path.Combine(_testDirectory, "NotARazorFile.txt");

        File.WriteAllText(file1, "<div>Component 1</div>");
        File.WriteAllText(file2, "<span>Component 2</span>");
        File.WriteAllText(file3, "<div>Not a razor file</div>");

        // Act
        _processor.ProcessDirectory(_testDirectory);

        // Assert
        File.ReadAllText(file1).ShouldContain("data-testid=\"Component1\"");
        File.ReadAllText(file2).ShouldContain("data-testid=\"Component2\"");
        File.ReadAllText(file3).ShouldNotContain("data-testid");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
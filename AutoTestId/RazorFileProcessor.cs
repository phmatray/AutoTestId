namespace AutoTestId;

public class RazorFileProcessor
{
    private readonly RazorTestIdProcessor _processor = new();

    public void ProcessFile(string filePath)
    {
        string content = File.ReadAllText(filePath);
        string componentName = Path.GetFileNameWithoutExtension(filePath);
        
        string newContent = _processor.ProcessContent(content, componentName);
        
        if (content != newContent)
        {
            Console.WriteLine($"Injected data-testid into {filePath}");
            File.WriteAllText(filePath, newContent);
        }
    }

    public void ProcessDirectory(string directory)
    {
        var razorFiles = Directory.EnumerateFiles(directory, "*.razor", SearchOption.AllDirectories);
        
        foreach (var file in razorFiles)
        {
            ProcessFile(file);
        }
    }
}
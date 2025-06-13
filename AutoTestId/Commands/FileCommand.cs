using AutoTestId.Commands.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace AutoTestId.Commands;

/// <summary>
/// Command to process a single Razor file and add test-id attributes
/// </summary>
[Description("Process a single Razor file")]
public class FileCommand : Command<FileCommandSettings>
{
    private readonly RazorTestIdProcessor _processor = new();

    /// <summary>
    /// Executes the file command
    /// </summary>
    /// <param name="context">The command context</param>
    /// <param name="settings">The command settings</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public override int Execute([NotNull] CommandContext context, [NotNull] FileCommandSettings settings)
    {
        try
        {
            if (!File.Exists(settings.FilePath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {settings.FilePath}");
                return 1;
            }

            if (!settings.FilePath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] File {settings.FilePath} is not a Razor file");
                return 1;
            }

            ProcessFile(settings.FilePath, settings.TestId, settings.DryRun);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    /// <summary>
    /// Processes a single Razor file
    /// </summary>
    /// <param name="filePath">Path to the file to process</param>
    /// <param name="customTestId">Optional custom test ID to use</param>
    /// <param name="dryRun">Whether to perform a dry run</param>
    private void ProcessFile(string filePath, string? customTestId, bool dryRun)
    {
        var content = File.ReadAllText(filePath);
        var testId = customTestId ?? Path.GetFileNameWithoutExtension(filePath);
        var newContent = _processor.ProcessContent(content, testId);

        if (content == newContent)
        {
            AnsiConsole.MarkupLine($"[dim]No changes needed for[/] {filePath}");
            return;
        }

        if (dryRun)
        {
            AnsiConsole.MarkupLine($"[yellow]Would update:[/] {filePath}");
            
            // Show a preview of the changes
            var panel = new Panel(newContent.Length > 500 ? newContent.Substring(0, 500) + "..." : newContent)
            {
                Header = new PanelHeader("Preview"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);
        }
        else
        {
            File.WriteAllText(filePath, newContent);
            AnsiConsole.MarkupLine($"[green]✓[/] Updated {filePath}");
        }
    }
}
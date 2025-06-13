using AutoTestId.Commands.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace AutoTestId.Commands;

/// <summary>
/// Command to process all Razor files in a folder
/// </summary>
[Description("Process all Razor files in a folder")]
public class FolderCommand : Command<FolderCommandSettings>
{
    private readonly RazorTestIdProcessor _processor = new();

    /// <summary>
    /// Executes the folder command
    /// </summary>
    /// <param name="context">The command context</param>
    /// <param name="settings">The command settings</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public override int Execute([NotNull] CommandContext context, [NotNull] FolderCommandSettings settings)
    {
        try
        {
            if (!Directory.Exists(settings.FolderPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Directory not found: {settings.FolderPath}");
                return 1;
            }

            var searchOption = settings.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(settings.FolderPath, settings.Pattern, searchOption)
                .Where(f => !IsExcluded(f, settings.ExcludeDirectories))
                .ToList();

            if (files.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No {settings.Pattern} files found in {settings.FolderPath}[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"Found [cyan]{files.Count}[/] files to process");

            if (settings.DryRun)
            {
                AnsiConsole.MarkupLine("[yellow]Running in dry-run mode - no files will be modified[/]");
            }

            ProcessFiles(files, settings.DryRun);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    /// <summary>
    /// Checks if a file path should be excluded based on exclude patterns
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <param name="excludePatterns">Array of directory names to exclude</param>
    /// <returns>True if the file should be excluded</returns>
    private bool IsExcluded(string filePath, string[] excludePatterns)
    {
        if (excludePatterns.Length == 0) return false;

        var normalizedPath = Path.GetFullPath(filePath).Replace('\\', '/');
        return excludePatterns.Any(pattern => 
            normalizedPath.Contains($"/{pattern}/", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Processes multiple files with progress reporting
    /// </summary>
    /// <param name="files">List of files to process</param>
    /// <param name="dryRun">Whether to perform a dry run</param>
    private void ProcessFiles(List<string> files, bool dryRun)
    {
        var updatedCount = 0;
        var skippedCount = 0;

        AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn(),
            })
            .Start(ctx =>
            {
                var task = ctx.AddTask("[green]Processing files[/]", maxValue: files.Count);

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    task.Description = $"Processing {fileName}";

                    if (ProcessFile(file, dryRun))
                    {
                        updatedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }

                    task.Increment(1);
                }
            });

        // Summary
        AnsiConsole.WriteLine();
        var table = new Table();
        table.AddColumn("Status");
        table.AddColumn("Count");
        table.AddRow("[green]Updated[/]", updatedCount.ToString());
        table.AddRow("[dim]Skipped[/]", skippedCount.ToString());
        table.AddRow("[bold]Total[/]", files.Count.ToString());
        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Processes a single file
    /// </summary>
    /// <param name="filePath">Path to the file to process</param>
    /// <param name="dryRun">Whether to perform a dry run</param>
    /// <returns>True if the file was updated, false if skipped</returns>
    private bool ProcessFile(string filePath, bool dryRun)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var testId = Path.GetFileNameWithoutExtension(filePath);
            var newContent = _processor.ProcessContent(content, testId);

            if (content == newContent)
            {
                return false; // No changes needed
            }

            if (!dryRun)
            {
                File.WriteAllText(filePath, newContent);
            }

            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error processing {filePath}: {ex.Message}[/]");
            return false;
        }
    }
}
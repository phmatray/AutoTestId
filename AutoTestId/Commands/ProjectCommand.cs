using AutoTestId.Commands.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace AutoTestId.Commands;

/// <summary>
/// Command to process all Razor files in a .csproj project
/// </summary>
[Description("Process all Razor files in a project")]
public class ProjectCommand : Command<ProjectCommandSettings>
{
    private readonly RazorTestIdProcessor _processor = new();

    /// <summary>
    /// Executes the project command
    /// </summary>
    /// <param name="context">The command context</param>
    /// <param name="settings">The command settings</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public override int Execute([NotNull] CommandContext context, [NotNull] ProjectCommandSettings settings)
    {
        try
        {
            if (!File.Exists(settings.ProjectPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Project file not found: {settings.ProjectPath}");
                return 1;
            }

            if (!settings.ProjectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] File is not a .csproj file: {settings.ProjectPath}");
                return 1;
            }

            var projectDir = Path.GetDirectoryName(settings.ProjectPath) ?? ".";
            var projectName = Path.GetFileNameWithoutExtension(settings.ProjectPath);

            AnsiConsole.MarkupLine($"Processing project: [cyan]{projectName}[/]");

            // Find all Razor files in the project directory
            var razorFiles = FindRazorFiles(projectDir, settings.ExcludeDirectories);

            if (razorFiles.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No Razor files found in project {projectName}[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"Found [cyan]{razorFiles.Count}[/] Razor files");

            if (settings.DryRun)
            {
                AnsiConsole.MarkupLine("[yellow]Running in dry-run mode - no files will be modified[/]");
            }

            ProcessFiles(razorFiles, settings.DryRun, projectName);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    /// <summary>
    /// Finds all Razor files in a project directory
    /// </summary>
    /// <param name="projectDir">The project directory</param>
    /// <param name="excludeDirectories">Directories to exclude</param>
    /// <returns>List of Razor file paths</returns>
    private List<string> FindRazorFiles(string projectDir, string[] excludeDirectories)
    {
        var excludeList = new List<string>(excludeDirectories);
        
        // Always exclude common directories
        excludeList.AddRange(new[] { "bin", "obj", "node_modules", ".git", ".vs" });

        return Directory.GetFiles(projectDir, "*.razor", SearchOption.AllDirectories)
            .Where(f => !IsExcluded(f, excludeList.ToArray()))
            .ToList();
    }

    /// <summary>
    /// Checks if a file path should be excluded
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
    /// <param name="projectName">Name of the project being processed</param>
    private void ProcessFiles(List<string> files, bool dryRun, string projectName)
    {
        var updatedCount = 0;
        var skippedCount = 0;
        var errors = new List<string>();

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
            .Start($"Processing {projectName}...", ctx =>
            {
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                    ctx.Status($"Processing {relativePath}");

                    try
                    {
                        var content = File.ReadAllText(file);
                        var testId = Path.GetFileNameWithoutExtension(file);
                        var newContent = _processor.ProcessContent(content, testId);

                        if (content != newContent)
                        {
                            if (!dryRun)
                            {
                                File.WriteAllText(file, newContent);
                            }
                            updatedCount++;
                            AnsiConsole.MarkupLine($"[green]✓[/] {relativePath}");
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{relativePath}: {ex.Message}");
                    }
                }
            });

        // Summary
        AnsiConsole.WriteLine();
        
        if (errors.Count > 0)
        {
            AnsiConsole.MarkupLine($"[red]Errors occurred processing {errors.Count} files:[/]");
            foreach (var error in errors)
            {
                AnsiConsole.MarkupLine($"  [red]•[/] {error}");
            }
            AnsiConsole.WriteLine();
        }

        var rule = new Rule($"[bold]Summary for {projectName}[/]")
        {
            Justification = Justify.Left
        };
        AnsiConsole.Write(rule);

        AnsiConsole.MarkupLine($"[green]Updated:[/] {updatedCount}");
        AnsiConsole.MarkupLine($"[dim]Skipped:[/] {skippedCount}");
        if (errors.Count > 0)
        {
            AnsiConsole.MarkupLine($"[red]Errors:[/] {errors.Count}");
        }
        AnsiConsole.MarkupLine($"[bold]Total:[/] {files.Count}");
    }
}
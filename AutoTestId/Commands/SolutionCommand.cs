using AutoTestId.Commands.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AutoTestId.Commands;

/// <summary>
/// Command to process all Razor files in a .sln solution
/// </summary>
[Description("Process all Razor files in a solution")]
public class SolutionCommand : Command<SolutionCommandSettings>
{
    private readonly RazorTestIdProcessor _processor = new();

    /// <summary>
    /// Executes the solution command
    /// </summary>
    /// <param name="context">The command context</param>
    /// <param name="settings">The command settings</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public override int Execute([NotNull] CommandContext context, [NotNull] SolutionCommandSettings settings)
    {
        try
        {
            if (!File.Exists(settings.SolutionPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Solution file not found: {settings.SolutionPath}");
                return 1;
            }

            if (!settings.SolutionPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] File is not a .sln file: {settings.SolutionPath}");
                return 1;
            }

            var solutionDir = Path.GetDirectoryName(settings.SolutionPath) ?? ".";
            var solutionName = Path.GetFileNameWithoutExtension(settings.SolutionPath);

            AnsiConsole.MarkupLine($"Processing solution: [cyan]{solutionName}[/]");

            // Parse the solution file to find all projects
            var projects = ParseSolutionFile(settings.SolutionPath);
            projects = FilterProjects(projects, settings);

            if (projects.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No projects found to process[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"Found [cyan]{projects.Count}[/] projects to process");

            if (settings.DryRun)
            {
                AnsiConsole.MarkupLine("[yellow]Running in dry-run mode - no files will be modified[/]");
            }

            ProcessProjects(projects, solutionDir, settings.DryRun);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    /// <summary>
    /// Parses a solution file to extract project information
    /// </summary>
    /// <param name="solutionPath">Path to the solution file</param>
    /// <returns>List of project information tuples (name, relative path)</returns>
    private List<(string Name, string Path)> ParseSolutionFile(string solutionPath)
    {
        var projects = new List<(string Name, string Path)>();
        var content = File.ReadAllText(solutionPath);
        
        // Regex to match project entries in .sln file
        var projectRegex = new Regex(
            @"Project\(""\{[A-F0-9\-]+\}""\)\s*=\s*""([^""]+)"",\s*""([^""]+)""",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        foreach (Match match in projectRegex.Matches(content))
        {
            var projectName = match.Groups[1].Value;
            var projectPath = match.Groups[2].Value;
            
            // Only include .csproj files
            if (projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                projects.Add((projectName, projectPath));
            }
        }

        return projects;
    }

    /// <summary>
    /// Filters projects based on settings
    /// </summary>
    /// <param name="projects">List of all projects</param>
    /// <param name="settings">Command settings</param>
    /// <returns>Filtered list of projects</returns>
    private List<(string Name, string Path)> FilterProjects(
        List<(string Name, string Path)> projects, 
        SolutionCommandSettings settings)
    {
        var filtered = projects;

        // Filter by include list
        if (settings.IncludeProjects.Length > 0)
        {
            filtered = filtered.Where(p => 
                settings.IncludeProjects.Any(inc => 
                    p.Name.Contains(inc, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        // Filter by exclude list
        if (settings.ExcludeProjects.Length > 0)
        {
            filtered = filtered.Where(p => 
                !settings.ExcludeProjects.Any(exc => 
                    p.Name.Contains(exc, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        // Filter test projects
        if (!settings.IncludeTestProjects)
        {
            filtered = filtered.Where(p => 
                !p.Name.Contains("Test", StringComparison.OrdinalIgnoreCase) &&
                !p.Name.Contains("Tests", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return filtered;
    }

    /// <summary>
    /// Processes all projects in the solution
    /// </summary>
    /// <param name="projects">List of projects to process</param>
    /// <param name="solutionDir">Solution directory</param>
    /// <param name="dryRun">Whether to perform a dry run</param>
    private void ProcessProjects(List<(string Name, string Path)> projects, string solutionDir, bool dryRun)
    {
        var totalUpdated = 0;
        var totalSkipped = 0;
        var totalFiles = 0;
        var projectSummaries = new List<(string Project, int Updated, int Skipped, int Total)>();

        foreach (var (projectName, projectPath) in projects)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule($"[bold cyan]{projectName}[/]"));

            var fullProjectPath = Path.Combine(solutionDir, projectPath);
            if (!File.Exists(fullProjectPath))
            {
                AnsiConsole.MarkupLine($"[red]Project file not found:[/] {fullProjectPath}");
                continue;
            }

            var projectDir = Path.GetDirectoryName(fullProjectPath) ?? ".";
            var razorFiles = FindRazorFiles(projectDir);

            if (razorFiles.Count == 0)
            {
                AnsiConsole.MarkupLine($"[dim]No Razor files found[/]");
                projectSummaries.Add((projectName, 0, 0, 0));
                continue;
            }

            var (updated, skipped) = ProcessProjectFiles(razorFiles, projectDir, dryRun);
            
            totalUpdated += updated;
            totalSkipped += skipped;
            totalFiles += razorFiles.Count;
            
            projectSummaries.Add((projectName, updated, skipped, razorFiles.Count));
        }

        // Display solution summary
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Solution Summary[/]"));

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Project")
            .AddColumn(new TableColumn("Updated").Centered())
            .AddColumn(new TableColumn("Skipped").Centered())
            .AddColumn(new TableColumn("Total").Centered());

        foreach (var (project, updated, skipped, total) in projectSummaries)
        {
            table.AddRow(
                project,
                updated > 0 ? $"[green]{updated}[/]" : "[dim]0[/]",
                skipped > 0 ? $"[dim]{skipped}[/]" : "[dim]0[/]",
                total.ToString()
            );
        }

        table.AddEmptyRow();
        table.AddRow(
            "[bold]Total[/]",
            totalUpdated > 0 ? $"[bold green]{totalUpdated}[/]" : "[bold dim]0[/]",
            totalSkipped > 0 ? $"[bold dim]{totalSkipped}[/]" : "[bold dim]0[/]",
            $"[bold]{totalFiles}[/]"
        );

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Finds all Razor files in a project directory
    /// </summary>
    /// <param name="projectDir">The project directory</param>
    /// <returns>List of Razor file paths</returns>
    private List<string> FindRazorFiles(string projectDir)
    {
        var excludeDirs = new[] { "bin", "obj", "node_modules", ".git", ".vs" };
        
        return Directory.GetFiles(projectDir, "*.razor", SearchOption.AllDirectories)
            .Where(f => !IsExcluded(f, excludeDirs))
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
        var normalizedPath = Path.GetFullPath(filePath).Replace('\\', '/');
        return excludePatterns.Any(pattern => 
            normalizedPath.Contains($"/{pattern}/", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Processes files in a project
    /// </summary>
    /// <param name="files">List of files to process</param>
    /// <param name="projectDir">Project directory</param>
    /// <param name="dryRun">Whether to perform a dry run</param>
    /// <returns>Tuple of (updated count, skipped count)</returns>
    private (int Updated, int Skipped) ProcessProjectFiles(List<string> files, string projectDir, bool dryRun)
    {
        var updated = 0;
        var skipped = 0;

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(projectDir, file);
            
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
                    updated++;
                    AnsiConsole.MarkupLine($"  [green]✓[/] {relativePath}");
                }
                else
                {
                    skipped++;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"  [red]✗[/] {relativePath}: {ex.Message}");
            }
        }

        return (updated, skipped);
    }
}
using Spectre.Console.Cli;
using System.ComponentModel;

namespace AutoTestId.Commands.Settings;

/// <summary>
/// Settings for the solution command that processes all Razor files in a .sln solution
/// </summary>
public class SolutionCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the path to the .sln file
    /// </summary>
    [Description("Path to the .sln file")]
    [CommandArgument(0, "<SOLUTION>")]
    public string SolutionPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to perform a dry run without making changes
    /// </summary>
    [Description("Preview changes without modifying files")]
    [CommandOption("-d|--dry-run")]
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets specific projects to include (if not specified, all projects are processed)
    /// </summary>
    [Description("Specific projects to include (can be specified multiple times)")]
    [CommandOption("-i|--include-project")]
    public string[] IncludeProjects { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets projects to exclude from processing
    /// </summary>
    [Description("Projects to exclude (can be specified multiple times)")]
    [CommandOption("-e|--exclude-project")]
    public string[] ExcludeProjects { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether to include test projects
    /// </summary>
    [Description("Include test projects in processing")]
    [CommandOption("-t|--include-tests")]
    public bool IncludeTestProjects { get; set; } = false;
}
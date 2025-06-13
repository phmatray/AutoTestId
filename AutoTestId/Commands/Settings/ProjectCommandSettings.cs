using Spectre.Console.Cli;
using System.ComponentModel;

namespace AutoTestId.Commands.Settings;

/// <summary>
/// Settings for the project command that processes all Razor files in a .csproj project
/// </summary>
public class ProjectCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the path to the .csproj file
    /// </summary>
    [Description("Path to the .csproj file")]
    [CommandArgument(0, "<PROJECT>")]
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to perform a dry run without making changes
    /// </summary>
    [Description("Preview changes without modifying files")]
    [CommandOption("-d|--dry-run")]
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets whether to include linked files
    /// </summary>
    [Description("Include linked files in processing")]
    [CommandOption("-l|--include-linked")]
    public bool IncludeLinkedFiles { get; set; }

    /// <summary>
    /// Gets or sets directories to exclude from processing
    /// </summary>
    [Description("Directories to exclude (can be specified multiple times)")]
    [CommandOption("-e|--exclude")]
    public string[] ExcludeDirectories { get; set; } = Array.Empty<string>();
}
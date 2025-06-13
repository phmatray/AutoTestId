using Spectre.Console.Cli;
using System.ComponentModel;

namespace AutoTestId.Commands.Settings;

/// <summary>
/// Settings for the folder command that processes all Razor files in a directory
/// </summary>
public class FolderCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the path to the folder to process
    /// </summary>
    [Description("Path to the folder containing Razor files")]
    [CommandArgument(0, "<FOLDER>")]
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to process subdirectories recursively
    /// </summary>
    [Description("Process subdirectories recursively")]
    [CommandOption("-r|--recursive")]
    public bool Recursive { get; set; } = true;

    /// <summary>
    /// Gets or sets the file pattern to match
    /// </summary>
    [Description("File pattern to match (default: *.razor)")]
    [CommandOption("-p|--pattern")]
    public string Pattern { get; set; } = "*.razor";

    /// <summary>
    /// Gets or sets whether to perform a dry run without making changes
    /// </summary>
    [Description("Preview changes without modifying files")]
    [CommandOption("-d|--dry-run")]
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets directories to exclude from processing
    /// </summary>
    [Description("Directories to exclude (can be specified multiple times)")]
    [CommandOption("-e|--exclude")]
    public string[] ExcludeDirectories { get; set; } = Array.Empty<string>();
}
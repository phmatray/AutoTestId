using Spectre.Console.Cli;
using System.ComponentModel;

namespace AutoTestId.Commands.Settings;

/// <summary>
/// Settings for the file command that processes a single Razor file
/// </summary>
public class FileCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the path to the Razor file to process
    /// </summary>
    [Description("Path to the Razor file to process")]
    [CommandArgument(0, "<FILE>")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the test ID value to use. If not specified, the filename will be used
    /// </summary>
    [Description("Custom test ID value to use (defaults to filename)")]
    [CommandOption("-t|--test-id")]
    public string? TestId { get; set; }

    /// <summary>
    /// Gets or sets whether to perform a dry run without making changes
    /// </summary>
    [Description("Preview changes without modifying files")]
    [CommandOption("-d|--dry-run")]
    public bool DryRun { get; set; }
}
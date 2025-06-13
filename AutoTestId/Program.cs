using AutoTestId.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("autotestid");
    config.SetApplicationVersion("1.0.0");

    // Add commands
    config.AddCommand<FileCommand>("file")
        .WithDescription("Process a single Razor file")
        .WithExample("file", "Component.razor")
        .WithExample("file", "Component.razor", "--test-id", "MyCustomId")
        .WithExample("file", "Component.razor", "--dry-run");

    config.AddCommand<FolderCommand>("folder")
        .WithDescription("Process all Razor files in a folder")
        .WithExample("folder", "./src")
        .WithExample("folder", "./src", "--recursive")
        .WithExample("folder", "./src", "--exclude", "bin", "--exclude", "obj");

    config.AddCommand<ProjectCommand>("project")
        .WithDescription("Process all Razor files in a project")
        .WithExample("project", "MyApp.csproj")
        .WithExample("project", "MyApp.csproj", "--dry-run");

    config.AddCommand<SolutionCommand>("solution")
        .WithDescription("Process all Razor files in a solution")
        .WithExample("solution", "MyApp.sln")
        .WithExample("solution", "MyApp.sln", "--exclude-project", "Tests")
        .WithExample("solution", "MyApp.sln", "--include-tests");

    // Add ASCII art header
    config.SetInterceptor(new CommandInterceptor());
});

return app.Run(args);

/// <summary>
/// Command interceptor to display header before command execution
/// </summary>
public class CommandInterceptor : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        AnsiConsole.Write(
            new FigletText("AutoTestId")
                .LeftJustified()
                .Color(Color.Cyan1));
        AnsiConsole.MarkupLine("[dim]Automatically add test-id attributes to Razor components[/]");
        AnsiConsole.WriteLine();
    }
}
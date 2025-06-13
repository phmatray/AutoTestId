using System.Text.RegularExpressions;

namespace AutoTestId;

public class RazorTestIdProcessor
{
    public string ProcessContent(string content, string componentName)
    {
        // Process all root-level HTML tags
        var tagRegex = new Regex(@"^(\s*)<([a-zA-Z0-9]+)([^>]*)>", RegexOptions.Multiline);
        var matches = tagRegex.Matches(content).Cast<Match>().ToList();
        
        // Process from last to first to maintain string positions
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var match = matches[i];
            
            // Check if this is a root-level tag
            if (IsRootLevelTag(content, match))
            {
                string whitespace = match.Groups[1].Value;
                string tag = match.Groups[2].Value;
                string attributes = match.Groups[3].Value;
                
                // Check if data-testid already exists and replace it
                string newAttributes;
                if (Regex.IsMatch(attributes, @"data-testid\s*=\s*[""'][^""']*[""']", RegexOptions.IgnoreCase))
                {
                    newAttributes = Regex.Replace(attributes,
                        @"data-testid\s*=\s*[""'][^""']*[""']",
                        $"data-testid=\"{componentName}\"",
                        RegexOptions.IgnoreCase);
                }
                else
                {
                    newAttributes = $" data-testid=\"{componentName}\"{attributes}";
                }

                string newTag = $"{whitespace}<{tag}{newAttributes}>";
                content = content.Substring(0, match.Index) + newTag + content.Substring(match.Index + match.Length);
            }
        }
        
        return content;
    }
    
    private bool IsRootLevelTag(string content, Match tagMatch)
    {
        // Extract the lines before the tag
        var beforeTag = content.Substring(0, tagMatch.Index);
        var lines = beforeTag.Split('\n');
        
        // Track open tags
        var tagStack = new Stack<string>();
        var tagRegex = new Regex(@"<(/?)([a-zA-Z0-9]+)([^>]*)>");
        
        foreach (var line in lines)
        {
            var lineMatches = tagRegex.Matches(line);
            foreach (Match match in lineMatches)
            {
                var isClosing = match.Groups[1].Value == "/";
                var tagName = match.Groups[2].Value;
                var attributes = match.Groups[3].Value;
                
                if (isClosing)
                {
                    if (tagStack.Count > 0 && tagStack.Peek() == tagName)
                    {
                        tagStack.Pop();
                    }
                }
                else if (!match.Value.TrimEnd().EndsWith("/>")) // Not self-closing
                {
                    tagStack.Push(tagName);
                }
            }
        }
        
        // If stack is empty, the tag is at root level
        return tagStack.Count == 0;
    }
}
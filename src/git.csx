#load "command.csx"
using System.Text.RegularExpressions;
using static Command;
public static class Git
{
    public static string GetRemoteUrl()
    {
        var result = Capture("git", "config --get remote.origin.url");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException("Not a git folder");
        }

        return result.StandardOut.RemoveNewLine();
    }

    public static RepositoryInfo GetRepositoryInfo()
    {
        var urlToPushOrigin = GetRemoteUrl();
        var match = Regex.Match(urlToPushOrigin, @".*.com\/(.*)\/(.*)\.");
        var owner = match.Groups[1].Value;
        var project = match.Groups[2].Value;
        return new RepositoryInfo() { Owner = owner, ProjectName = project };
    }

    public static string GetLocalFolder()
    {
        var result = Capture("git", "rev-parse --show-toplevel");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException("Not a git folder");
        }
        return result.StandardOut.RemoveNewLine();
    }
}

public class RepositoryInfo
{
    public string Owner { get; set; }

    public string ProjectName { get; set; }
}

private static string RemoveNewLine(this string value)
{
    string result = Regex.Replace(value, @"\r\n?|\n", "");
    return result;
}


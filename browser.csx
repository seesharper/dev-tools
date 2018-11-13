#load "command.csx"
using static Command;

public static class Browser
{
    public static void OpenUrl(string url)
    {
        ShellExecute(url);
    }
}


#!/usr/bin/env dotnet-script
#load "command.csx"
using static Command;

var pathToScript = Path.GetFullPath(Args[0]);

Execute("chmod", $"+x {pathToScript}");

string commandName = null;

if (Args.Count > 1)
{
    commandName = Args[1];
}
else
{
    commandName = Path.GetFileNameWithoutExtension(pathToScript);
}


var pathToBashScript = Path.Combine("/usr/local/bin", commandName);
var bashScriptContent = $"{pathToScript} \"$@\"";
File.WriteAllText(pathToBashScript, bashScriptContent);

Execute("chmod", $"+x {pathToBashScript}");


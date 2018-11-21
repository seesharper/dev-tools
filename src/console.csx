public void WriteError(string value)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Error.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
    Console.ResetColor();
}

public void WriteSuccess(string value)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Out.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
    Console.ResetColor();
}

public void WriteHighlighted(string value)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Out.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
    Console.ResetColor();
}

public void WriteHeader(string value)
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Out.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
    Console.ResetColor();
}


public void WriteNormal(string value)
{
    Out.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
}

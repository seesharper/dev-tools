public static void WriteError(string value)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Out.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
    Console.ResetColor();
}

public static void WriteSuccess(string value)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Out.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
    Console.ResetColor();
}

public static void WriteHighlighted(string value)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Out.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
    Console.ResetColor();
}

public static void WriteHeader(string value)
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Out.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
    Console.ResetColor();
}


public static void WriteNormal(string value)
{
    Out.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
}

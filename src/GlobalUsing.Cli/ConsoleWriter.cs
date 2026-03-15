namespace GlobalUsing.Cli;

internal interface IConsoleWriter
{
    void WriteLine(string value);

    void WriteError(string value);
}

internal sealed class ConsoleWriter : IConsoleWriter
{
    public void WriteLine(string value) => Console.Out.WriteLine(value);

    public void WriteError(string value) => Console.Error.WriteLine(value);
}

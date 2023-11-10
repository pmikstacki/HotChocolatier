using CommandLine.Text;
using CommandLine;

namespace HotChocolatier;

public class Options
{
    [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
    public bool Verbose { get; set; }

    [Option('a', "assembly", Required = true, HelpText = "Assembly path where the DbContext is")]
    public string Assembly { get; set; }

    [Option('c', "context", Required = false, HelpText = "DbContext class name, leave empty to auto recognize")]
    public string? ContextName { get; set; }

    [Option('o', "output", Required = false, HelpText = "Schema output folder, Default: Schema")]
    public string Output { get; set; }


    [Option('n', "namespace", Required = true, HelpText = "Namespace to put the output in")]
    public string Namespace { get; set; }
}
namespace HZDCoreEditorUI;

using System;
using System.Windows.Forms;
using CommandLine;
using Decima;

/// <summary>
/// Entry point of the application.
/// </summary>
public class Program
{
    /// <summary>
    /// Main method to start the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        RTTI.SetGameMode(GameType.HZD);

        var cmds = new CmdOptions();
        var parser = new Parser(with => with.HelpWriter = Console.Error);

        parser.ParseArguments<CmdOptions>(args)
            .WithParsed(o => cmds = o)
            .WithNotParsed(errs => MessageBox.Show("Unable to parse command line: {0}", string.Join(" ", args)));

        Application.Run(new UI.FormCoreView(cmds));
    }

    /// <summary>
    /// Command line options.
    /// </summary>
    public class CmdOptions
    {
        /// <summary>
        /// Gets or sets search for text.
        /// </summary>
        [Option('s', "search", HelpText = "Search for text")]
        public string Search { get; set; }

        /// <summary>
        /// Gets or sets highlight object by id.
        /// </summary>
        [Option('o', "object", HelpText = "Highlight object by id")]
        public string ObjectId { get; set; }

        /// <summary>
        /// Gets or sets file path.
        /// </summary>
        [Value(0)]
        public string File { get; set; }
    }
}

using System;
using CommandLine;
using CommandLine.Text;
using System.IO;
using static TransplanterLib.TransplanterLib;

namespace Transplanter_CLI
{
    class Options
    {
        [Option('i', "inputfile", Required = true,
            HelpText = "Input file to be processed.")]
        public string InputFile { get; set; }

        [Option('d', "destinationfile", Required = false, MutuallyExclusiveSet = "operation",
            HelpText = "Destination file to be operated on")]
        public string DestFile { get; set; }

        [Option('s', "scripts", DefaultValue = false, Required = false, MutuallyExclusiveSet = "operation", HelpText = "Dumps Function exports into a file of the same name.txt")]
        public bool Scripts { get; set; }

        [Option('v', "verbose", DefaultValue = false,
          HelpText = "Prints debugging information to the console")]
        public bool Verbose { get; set; }

        [Option('e', "extract-to", DefaultValue = null, MutuallyExclusiveSet = "operation",
          HelpText = "Extracts all GFX files from the input pcc to the specified directory")]
        public String ExtractTo { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    class Program
    {
        private static readonly int CODE_UNSET = 50;
        private static readonly int CODE_INPUT_FILE_NOT_FOUND = 10;

        static void Main(string[] args)
        {
            int code = CODE_UNSET;
            var options = new Options();
            if (new CommandLine.Parser(s =>
            {
                s.MutuallyExclusive = true;
                s.CaseSensitive = true;
                s.HelpWriter = Console.Error;
            }).ParseArguments(args, options))
            {
                // Values are available here
                if (options.Verbose)
                {
                    Verbose = true;
                    writeVerboseLine("Verbose logging is enabled");
                }

                if (File.Exists(options.InputFile))
                {
                    Boolean performedOperation = false;
                    writeVerboseLine("Input file exists: " + options.InputFile);
                    if (File.Exists(options.DestFile))
                    {
                        performedOperation = true;
                        writeVerboseLine("Output file exists: " + options.DestFile);
                        writeVerboseLine("Starting transplant procedures");

                        string gfxfolder = AppDomain.CurrentDomain.BaseDirectory + @"extractedgfx\";
                        writeVerboseLine("Extracting GFX Files from source to " + gfxfolder);
                        extractAllGFxMovies(options.InputFile, null, gfxfolder);
                        replaceSWFs(gfxfolder, options.DestFile);
                    }
                    else if (options.ExtractTo != null)
                    {
                        {
                            writeVerboseLine("Extracting GFX files from " + options.InputFile + " to " + options.ExtractTo);
                            performedOperation = true;
                            extractAllGFxMovies(options.InputFile, null, options.ExtractTo);
                        }

                        if (!performedOperation)
                        {
                            Console.WriteLine("Not enough arguments were provided to perform any operation.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Input file does not exist: " + options.InputFile);
                        code = CODE_INPUT_FILE_NOT_FOUND;
                    }
                }
                else
                {
                    Console.WriteLine("Failed to parse?");
                }
            }
            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
            Environment.ExitCode = code;
        }
    }
}

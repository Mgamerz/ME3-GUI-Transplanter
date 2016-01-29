﻿using System;
using CommandLine;
using CommandLine.Text;
using System.IO;
using static TransplanterLib.TransplanterLib;

namespace Transplanter_CLI
{
    class Options
    {

        //Inputs
        [Option('i', "inputfile", MutuallyExclusiveSet = "input", HelpText = "Input file to be processed.")]
        public string InputFile { get; set; }

        [Option('f', "inputfolder", MutuallyExclusiveSet = "input", HelpText = "Input folder to be processed.")]
        public string InputFolder { get; set; }

        //Outputs
        [Option('o', "outputfolder", HelpText = "Output folder, used by some other command line switches.")]
        public string OutputFolder { get; set; }

        //Operations
        [Option('t', "transplantfile", MutuallyExclusiveSet = "operation",
            HelpText = "Transplant file to inject GUI files from the source into. Requires --inputfile.")]
        public string TransplantFile { get; set; }

        [Option('g', "gui-extract", MutuallyExclusiveSet = "operation",
            HelpText = "Extracts all GFX files from the input (--inputfile or --inputfolder) into a folder of the same name as the pcc file. With --output folder you can redirect the output.")]
        public bool GuiExtract { get; set; }

        [Option('c', "exec-dump", MutuallyExclusiveSet = "operation",
          HelpText = "Dumps all exec functions from the specified file or folder into a file named ExecFunctions.txt (in the same folder as the file or in the specified folder). To dump into this exe directory use the --dumphere switch.")]
        public bool ExecDump { get; set; }

        [Option('x', "extract", DefaultValue = false, MutuallyExclusiveSet = "operation", HelpText = "Specifies the extract operation. Requires --inputfolder and at least one of the following: --scripts, --data, --names, --imports , --exports. Use of --outputfolder will redirect where parsed files are placed.")]
        public bool Extract { get; set; }

        //Extract Options
        [Option('n', "names", DefaultValue = false, HelpText = "Dumps the name table for the PCC.")]
        public bool Names { get; set; }

        [Option('m', "imports", DefaultValue = false, HelpText = "Dumps the list of imports for the PCC.")]
        public bool Imports { get; set; }

        [Option('s', "scripts", DefaultValue = false, HelpText = "Dumps function exports, as part of the --extract switch.")]
        public bool Scripts { get; set; }

        [Option('d', "data", DefaultValue = false, HelpText = "Dumps export binary data. This will cause a significant increase in filesize and will cause some text editors to have problems opening them. It is useful only for file comparison purposes.")]
        public bool Data { get; set; }

        [Option('r', "exports", DefaultValue = false, HelpText = "Dumps all exports metadata, such as class size and data offset")]
        public bool Exports { get; set; }

        //Options
        [Option('v', "verbose", DefaultValue = false,
          HelpText = "Prints debugging information to the console")]
        public bool Verbose { get; set; }

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
        private static readonly int CODE_NO_INPUT = 9;
        private static readonly int CODE_NO_OPERATION = 10;
        private static readonly int CODE_INPUT_FILE_NOT_FOUND = 11;
        private static readonly int CODE_INPUT_FOLDER_NOT_FOUND = 12;
        private static readonly int CODE_NO_TRANSPLANT_FILE = 13;
        private static readonly int CODE_NO_DATA_TO_DUMP = 14;


        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Values are available here
                if (options.Verbose)
                {
                    Verbose = true;
                    writeVerboseLine("Verbose logging is enabled");
                }

                if (options.InputFile == null && options.InputFolder == null)
                {
                    Console.Error.WriteLine("--inputfile or --inputfolder argument is required for all operations.");
                    endProgram(CODE_NO_INPUT);
                }

                if (options.InputFile != null && !File.Exists(options.InputFile))
                {
                    Console.Error.WriteLine("Input file does not exist: " + options.InputFile);
                    endProgram(CODE_INPUT_FILE_NOT_FOUND);
                }

                if (options.InputFolder != null && !options.InputFolder.EndsWith(@"\"))
                {
                    options.InputFolder = options.InputFolder + @"\";
                }

                if (options.InputFolder != null && !Directory.Exists(options.InputFolder))
                {
                    Console.Error.WriteLine("Input folder does not exist: " + options.InputFolder);
                    endProgram(CODE_INPUT_FOLDER_NOT_FOUND);
                }

                //Operation Switch
                if (options.GuiExtract)
                {
                    if (options.InputFile != null)
                    {
                        writeVerboseLine("Extracting GFX files from " + options.InputFile);
                        extractAllGFxMovies(options.InputFile, options.OutputFolder);
                    }
                    else if (options.InputFolder != null)
                    {
                        writeVerboseLine("Extracting GFX files from " + options.InputFolder);
                        extractAllGFxMoviesFromFolder(options.InputFolder, options.OutputFolder);
                    }
                }
                else if (options.ExecDump)
                {
                    if (options.InputFile != null)
                    {
                        writeVerboseLine("Dumping all Exec files from " + options.InputFile);
                        dumpAllExecFromFile(options.InputFile, options.OutputFolder);
                    }
                    if (options.InputFolder != null)
                    {
                        writeVerboseLine("Dumping all Exec files from " + options.InputFolder);
                        dumpAllExecFromFolder(options.InputFile, options.OutputFolder);
                    }
                }
                else if (options.Extract)
                {
                    if (options.Imports || options.Exports || options.Data || options.Scripts || options.Names)
                    {
                        bool[] dumpargs = new bool[] { options.Imports, options.Exports, options.Data, options.Scripts, options.Names };
                        

                        if (options.InputFile != null)
                        {
                            Console.Out.WriteLine("Dumping pcc data from " + options.InputFile +
                            " [Imports: " + options.Imports + ", Exports: " + options.Exports + ", Data: " + options.Data + ", Scripts: " + options.Scripts +
                            ", Names: " + options.Names + "]");
                            dumpPCCFile(options.InputFile, dumpargs, options.OutputFolder);
                        }
                        if (options.InputFolder != null)
                        {
                            Console.Out.WriteLine("Dumping pcc data from " + options.InputFolder +
                               " [Imports: " + options.Imports + ", Exports: " + options.Exports + ", Data: " + options.Data + ", Scripts: " + options.Scripts +
                               ", Names: " + options.Names + "]");
                            writeVerboseLine("Dumping all Exec files from " + options.InputFolder);
                            dumpPCCFolder(options.InputFile, dumpargs, options.OutputFolder);
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("Nothing was selected to dump. Use --scripts, --names, --data, --imports or --exports to dump items from a pcc.");
                        endProgram(CODE_NO_DATA_TO_DUMP);
                    }
                }
                else if (options.TransplantFile != null)
                {
                    writeVerboseLine("Input file exists: " + options.InputFile);
                    if (File.Exists(options.TransplantFile))
                    {
                        writeVerboseLine("Starting transplant procedures");
                        string gfxfolder = AppDomain.CurrentDomain.BaseDirectory + @"extractedgfx\";
                        writeVerboseLine("Extracting GFX Files from source to " + gfxfolder);
                        extractAllGFxMovies(options.InputFile, gfxfolder);
                        replaceSWFs(gfxfolder, options.TransplantFile);
                    }
                    else
                    {
                        Console.Error.WriteLine("File to inject GFx files into does not exist: " + options.TransplantFile);
                        endProgram(CODE_NO_TRANSPLANT_FILE);
                    }
                }
                else
                {
                    Console.Error.WriteLine("No operation was specified");
                    endProgram(CODE_NO_OPERATION);
                }
            }
            endProgram(0);
        }

        private static void endProgram(int code)
        {
            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
            Environment.Exit(code);
        }

        private static bool checkInputFile(string inputFile)
        {
            return File.Exists(inputFile);
        }

        private static bool checkOutputFile(string transplantFile)
        {
            throw new NotImplementedException();
        }

        private static void createOutputFolder(string outputFolder)
        {
            throw new NotImplementedException();
        }
    }
}

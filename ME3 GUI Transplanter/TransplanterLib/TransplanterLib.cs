using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ComponentModel;

namespace TransplanterLib
{
    public static class TransplanterLib
    {
        private static Boolean verbose;
        public static Boolean Verbose
        {
            set
            {
                verbose = value;
            }
        }

        static void dumpAllExecFunctions(string path)
        {
            string[] files = Directory.GetFiles(path, "*.pcc*", SearchOption.AllDirectories);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(path + "execfunctions.txt", false))
            {
                foreach (String pccfile in files)
                {
                    System.Console.WriteLine("Searching for EXEC functions: " + pccfile);
                    bool hasOneExec = false;
                    PCCObject pcc = new PCCObject(pccfile);
                    foreach (PCCObject.ExportEntry exp in pcc.Exports)
                    {
                        if (exp.ClassName == "Function")
                        {
                            Function func = new Function(exp.Data, pcc);
                            //exec = 9
                            int execbit = 9;
                            int flagint = func.GetFlagInt();
                            int flag = 1 << execbit;
                            if ((flagint & flag) != 0)
                            {
                                //EXEC FUNCTION!
                                System.Console.WriteLine("FOUND EXEC: " + exp.ObjectName);
                                if (!hasOneExec)
                                {
                                    file.WriteLine("\n\n=======" + Path.GetFileName(pccfile) + "========");
                                }
                                hasOneExec = true;
                                file.WriteLine(exp.PackageName + "." + exp.ObjectName);
                            }
                        }
                    }
                }
                System.Console.WriteLine("Read all PCC files");
            }
        }

        static byte[] copyByteChunk(byte[] input, int offset, int length)
        {
            byte[] output = new byte[length];

            for (int i = offset; i < offset + length; i++)
            {
                output[i - offset] = input[i];
            }

            return output;
        }

        static void replace_swf_file(PCCObject.ExportEntry ent, string swf_path)
        {
            byte[] swf_file = File.ReadAllBytes(swf_path);

            byte[] header = copyByteChunk(ent.Data, 0, 20);
            byte[] number1 = System.BitConverter.GetBytes(swf_file.Length + 4);
            byte[] filler = copyByteChunk(ent.Data, 24, 4);
            byte[] number2 = System.BitConverter.GetBytes(swf_file.Length);

            int originalSize = System.BitConverter.ToInt32(ent.Data, 28);

            byte[] bytefooter = copyByteChunk(ent.Data, 32 + originalSize, ent.Data.Length - 32 - originalSize);

            MemoryStream m = new MemoryStream();
            for (int i = 0; i < header.Length; i++)
                m.WriteByte(header[i]);

            for (int i = 0; i < number1.Length; i++)
                m.WriteByte(number1[i]);

            for (int i = 0; i < filler.Length; i++)
                m.WriteByte(filler[i]);

            for (int i = 0; i < number2.Length; i++)
                m.WriteByte(number2[i]);

            for (int i = 0; i < swf_file.Length; i++)
                m.WriteByte(swf_file[i]);

            for (int i = 0; i < bytefooter.Length; i++)
                m.WriteByte(bytefooter[i]);

            ent.Data = m.ToArray();
        }

        static void extract_swf(PCCObject.ExportEntry ent, string filename)
        {
            int originalSize = System.BitConverter.ToInt32(ent.Data, 28);
            byte[] originalFile = copyByteChunk(ent.Data, 32, originalSize);
            File.WriteAllBytes(filename, originalFile);
        }

        /// <summary>
        /// Extracts all GFX (GUI) files from the specified source file. It will extract only what is listed in packageName for the export, unless it is null, in which case all GFX files are extracted to the specified path.
        /// </summary>
        /// <param name="sourceFile">Source PCC file to inspect.</param>
        /// <param name="path">Directory to put extracted GFX files. If it does not exist, it will be created.</param>
        public static void extractAllGFxMovies(string sourceFile, string outputpath = null, BackgroundWorker worker = null)
        {
            if (outputpath == null)
            {
                outputpath = Directory.GetParent(sourceFile).ToString();
            }

            if (!outputpath.EndsWith("\\"))
            {
                outputpath = outputpath + '\\';
            }
            PCCObject pcc = new PCCObject(sourceFile);
            int numExports = pcc.Exports.Count;
            for (int i = 0; i < numExports; i++)
            {
                PCCObject.ExportEntry exp = pcc.Exports[i];
                if (exp.ClassName == "GFxMovieInfo")
                {
                    Directory.CreateDirectory(outputpath);

                    String fname = exp.ObjectName;

                    Console.WriteLine("Extracting: " + outputpath + exp.PackageFullName + "." + fname + ".swf");
                    extract_swf(exp, outputpath + exp.PackageFullName + "." + fname + ".swf");
                }

                if (worker != null)
                {
                    worker.ReportProgress((int)(((double)i / numExports) * 100));
                }
            }
        }

        public static void extractAllGFxMoviesFromFolder(string folder, string outputfolder = null)
        {
            string[] files = Directory.GetFiles(folder, "*.pcc*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                Console.WriteLine("Scanning " + file);
                string relative = GetRelativePath(folder, Directory.GetParent(file).ToString());
                string fname = Path.GetFileNameWithoutExtension(file);

                string outfolder = outputfolder;
                if (outfolder == null)
                {
                    outfolder = folder;
                }

                outfolder = outfolder + relative + @"\" + fname + @"\";
                extractAllGFxMovies(file, outfolder);
            }
        }

        /// <summary>
        /// Replaces all SWF files in a specified PCC. As each export is scanned, if it is a GFX export and a correspondingly named packagename.sf file exists, it will be repalced.
        /// </summary>
        /// <param name="gfxSourceFolder">Source folder, with gfx files in the root.</param>
        /// <param name="destinationFile">File to update GFX files in</param>
        public static void replaceSWFs(string gfxSourceFolder, string destinationFile, BackgroundWorker worker = null)
        {
            string[] gfxfiles = System.IO.Directory.GetFiles(gfxSourceFolder, "*.swf");
            List<String> packobjnames = new List<String>();
            foreach (string gfxfile in gfxfiles)
            {
                string packobjname = Path.GetFileNameWithoutExtension(gfxfile);
                writeVerboseLine(packobjname);
                packobjnames.Add(packobjname);
            }

            if (gfxfiles.Length > 0)
            {
                string backupfile = destinationFile + ".bak";
                File.Move(destinationFile, backupfile);
                writeVerboseLine("Scanning " + destinationFile);
                int numReplaced = 0;
                PCCObject pcc = new PCCObject(backupfile);
                int numExports = pcc.Exports.Count;
                for (int i = 0; i < numExports; i++)
                {
                    PCCObject.ExportEntry exp = pcc.Exports[i];

                    if (exp.ClassName == "GFxMovieInfo")
                    {
                        string packobjname = exp.PackageFullName + "." + exp.ObjectName;
                        int index = packobjnames.IndexOf(packobjname);
                        if (index > -1)
                        {
                            Console.WriteLine("Replacing " + exp.PackageFullName + "." + exp.ObjectName);
                            replace_swf_file(exp, gfxfiles[index]);
                            numReplaced++;
                        }
                    }
                    if (worker != null && numReplaced % 10 == 0)
                    {
                        //Console.WriteLine("Progress: " + i + " / "+numExports);
                        worker.ReportProgress((int)(((double)i / numExports) * 100));
                    }
                    writeVerboseLine("Replaced " + numReplaced + " files, saving.");
                }
                pcc.altSaveToFile(destinationFile, 34, worker); //34 is default
            }
            else
            {
                Console.WriteLine("No source GFX files were found.");
            }
        }

        public static string Format(string filename, string arguments)
        {
            return "'" + filename +
                ((string.IsNullOrEmpty(arguments)) ? string.Empty : " " + arguments) +
                "'";
        }

        public static string RunExternalExe(string filename, string arguments = null, string workingdir = null)
        {
            var process = new Process();

            process.StartInfo.FileName = filename;
            if (!string.IsNullOrEmpty(arguments))
            {
                process.StartInfo.Arguments = arguments;
            }

            if (!string.IsNullOrEmpty(workingdir))
            {
                process.StartInfo.WorkingDirectory = workingdir;
            }

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            var stdOutput = new StringBuilder();
            process.OutputDataReceived += (sender, args) => stdOutput.Append(args.Data);

            string stdError = null;
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                stdError = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                throw new Exception("OS error while executing " + Format(filename, arguments) + ": " + e.Message, e);
            }

            if (process.ExitCode == 0)
            {
                return stdOutput.ToString();
            }
            else
            {
                var message = new StringBuilder();

                if (!string.IsNullOrEmpty(stdError))
                {
                    message.AppendLine(stdError);
                }

                if (stdOutput.Length != 0)
                {
                    message.AppendLine("Std output:");
                    message.AppendLine(stdOutput.ToString());
                }

                throw new Exception(Format(filename, arguments) + " finished with exit code = " + process.ExitCode + ": " + message);
            }
        }

        public static void dumpAllExecFromFolder(string folder, string outputfolder = null)
        {
            SortedSet<String> uniqueExecFunctions = new SortedSet<String>();
            SortedDictionary<String, List<String>> functionFileMap = new SortedDictionary<String, List<String>>();
            string[] files = System.IO.Directory.GetFiles(folder, "*.pcc");
            foreach (String pccfile in files)
            {
                Console.WriteLine("Scanning for exec: " + pccfile);
                PCCObject pcc = new PCCObject(pccfile);
                foreach (PCCObject.ExportEntry exp in pcc.Exports)
                {
                    if (exp.ClassName == "Function")
                    {
                        Function func = new Function(exp.Data, pcc);
                        //exec = 9
                        int execbit = 9;
                        int flagint = func.GetFlagInt();
                        int flag = 1 << execbit;
                        if ((flagint & flag) != 0)
                        {
                            //EXEC FUNCTION!
                            List<String> list;
                            if (functionFileMap.TryGetValue(exp.PackageName + "." + exp.ObjectName, out list))
                            {
                                list.Add(Path.GetFileName(pccfile));
                            }
                            else
                            {
                                list = new List<String>();
                                list.Add(Path.GetFileName(pccfile));
                                functionFileMap.Add(exp.PackageName + "." + exp.ObjectName, list);
                                //add new
                            }
                            Console.WriteLine("Exec Function: " + exp.ObjectName);
                        }
                    }
                }
                System.Console.WriteLine("Read all PCC files from folder");
            }
            if (outputfolder == null)
            {
                outputfolder = folder;
            }

            if (!outputfolder.EndsWith(@"\")) outputfolder += @"\";
            string outputfile = outputfolder + "ExecFunctions.txt";

            Console.WriteLine("Saving to " + outputfile);
            Directory.CreateDirectory(Path.GetDirectoryName(outputfile));
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outputfile, false))
            {
                foreach (String key in functionFileMap.Keys)
                {
                    List<String> fileList = functionFileMap[key];
                    file.WriteLine(key);
                    file.Write("Appears in:");
                    foreach (String filename in fileList)
                    {
                        file.Write(filename + " ");
                    }
                    file.WriteLine();
                    file.WriteLine();
                }
            }
        }

        public static void dumpAllExecFromFile(string pccfile, string outputfile = null)
        {
            SortedSet<String> uniqueExecFunctions = new SortedSet<String>();
            SortedDictionary<String, List<String>> functionFileMap = new SortedDictionary<String, List<String>>();

            Console.WriteLine("Scanning for exec: " + pccfile);
            PCCObject pcc = new PCCObject(pccfile);
            foreach (PCCObject.ExportEntry exp in pcc.Exports)
            {
                if (exp.ClassName == "Function")
                {
                    Function func = new Function(exp.Data, pcc);
                    //exec = 9
                    int execbit = 9;
                    int flagint = func.GetFlagInt();
                    int flag = 1 << execbit;
                    if ((flagint & flag) != 0)
                    {
                        //EXEC FUNCTION!
                        List<String> list;
                        if (functionFileMap.TryGetValue(exp.PackageName + "." + exp.ObjectName, out list))
                        {
                            list.Add(Path.GetFileName(pccfile));
                        }
                        else
                        {
                            list = new List<String>();
                            list.Add(Path.GetFileName(pccfile));
                            functionFileMap.Add(exp.PackageName + "." + exp.ObjectName, list);
                            //add new
                        }
                        Console.WriteLine("Exec Function: " + exp.ObjectName);
                    }
                }
            }
            if (outputfile == null)
            {
                outputfile = Directory.GetParent(pccfile).ToString();
            }
            if (!outputfile.EndsWith(@"\")) outputfile += @"\";
            outputfile = outputfile + "ExecFunctions.txt";

            Console.WriteLine("Saving to " + outputfile);
            Directory.CreateDirectory(Path.GetDirectoryName(outputfile));
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outputfile, false))
            {
                foreach (String key in functionFileMap.Keys)
                {
                    List<String> fileList = functionFileMap[key];
                    file.WriteLine(key);
                    file.Write("Appears in:");
                    foreach (String filename in fileList)
                    {
                        file.Write(filename + " ");
                    }
                    file.WriteLine();
                    file.WriteLine();
                }
            }
        }



        static void Main(string[] args)
        {
            string myExeDir = (new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location)).Directory.ToString();
            string baseDir = myExeDir + @"\..\";

            //getPccDump(@"V:\Mass Effect 3\BIOGame\DLC\DLC_EXP_Pack003\CookedPCConsole\", "SFXPawn_Heavy");
            //dumpAllFunctionsFromFolder(@"V:\Mass Effect 3\BIOGame\DLC\DLC_HEN_PR\CookedPCConsole\");
            //dumpAllFunctionsFromFolder(@"V:\Mass Effect 3\BIOGame\DLC\DLC_CON_GUN02\CookedPCConsole\");
            //dumpAllFunctionsFromFolder(@"V:\Mass Effect 3\BIOGame\DLC\DLC_CON_GUN01\CookedPCConsole\");
            //dumpAllFunctionsFromFolder(@"V:\Mass Effect 3\BIOGame\DLC\DLC_CON_APP01\CookedPCConsole\");

            //dumpAllFunctionsFromFolder(@"C:\Users\Michael\BIOGame\DLC\DLC_CON_MP4\CookedPCConsole\");
            //dumpAllFunctionsFromFolder(@"C:\Users\Michael\BIOGame\DLC\DLC_CON_MP5\CookedPCConsole\");

            //extractAllGFxMovies(null, @"D:\Origin Games\Mass Effect 3\BIOGame\CookedPCConsole\EntryMenu.pcc", baseDir + @"reference_files\BASEGAME\EntryMenu");
            //string folder = @"C:\Users\Michael\Desktop\ME3CMM\mods\ControllerSupport\DLC_CON_XBX\CookedPCConsole\";
            //string[] files = System.IO.Directory.GetFiles(folder, "*.pcc");
            //foreach (string file in files)
            //{
            //    extractAllGFxMovies(null, file, Directory.GetParent(file) + @"\" + Path.GetFileNameWithoutExtension(file));
            //}
            //string[] files = System.IO.Directory.GetFiles(@"\\psf\Google Drive\MP Controller Support\ME3Controller\xbox_textures\dds", "*.dds");
            //foreach (string file in files)
            //{
            //    string newfile = Regex.Replace(file, @"0x[0-9A-F]{8}", String.Empty);
            //    File.Move(file, newfile);
            //    Console.WriteLine(newfile);
            //}
            //Thread.Sleep(10000);
            //Environment.Exit(0);
            //Build(args);
        }

        static string RemoveFromEnd(this string s, string suffix)
        {
            if (s.EndsWith(suffix))
            {
                return s.Substring(0, s.Length - suffix.Length);
            }
            else
            {
                return s;
            }
        }

        /// <summary>
        /// Dumps all function data from the 
        /// </summary>
        /// <param name="path">Base path to start dumping functions from. Will search all subdirectories for pcc files.</param>
        /// <param name="args">Set of arguments for what to dump.</param>
        /// <param name="outputfolder">Output path to place files in. If null, it will use the same folder as the currently processing PCC. Files will be placed relative to the base path.</param>
        public static void dumpPCCFolder(string path, Boolean[] args, string outputfolder = null)
        {
            string[] files = Directory.GetFiles(path, "*.pcc", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                string outfolder = outputfolder;
                if (outfolder != null)
                {
                    string relative = GetRelativePath(path, Directory.GetParent(file).ToString());
                    outfolder = outfolder + relative;
                }

                Console.WriteLine("[" + (i + 1) + "/" + files.Length + "] Dumping " + Path.GetFileNameWithoutExtension(file));
                dumpPCCFile(file, args, outfolder);
            }
        }

        public static void dumpPCCFile(string file, Boolean[] args, string outputfolder = null)
        {
            try
            {
                Boolean imports = args[0];
                Boolean exports = args[1];
                Boolean data = args[2];
                Boolean scripts = args[3];
                Boolean coalesced = args[4];
                Boolean names = args[5];

                PCCObject pcc = new PCCObject(file);

                string outfolder = outputfolder;
                if (outfolder == null)
                {
                    outfolder = Directory.GetParent(file).ToString();
                }

                string savepath = outfolder + Path.GetFileNameWithoutExtension(file) + ".txt";
                Directory.CreateDirectory(Path.GetDirectoryName(savepath));

                using (StreamWriter stringoutput = new StreamWriter(savepath))
                {

                    if (imports)
                    {
                        writeVerboseLine("Getting Imports");
                        stringoutput.WriteLine("--Imports");
                        for (int x = 0; x < pcc.Imports.Count; x++)
                        {
                            PCCObject.ImportEntry imp = pcc.Imports[x];
                            if (imp.PackageFullName != "Class" && imp.PackageFullName != "Package")
                            {
                                stringoutput.WriteLine(x + ": " + imp.PackageFullName + "." + imp.ObjectName + "(From: " + imp.PackageFile + ") " +
                                    "(Offset: 0x" + (pcc.ImportOffset + (x * PCCObject.ImportEntry.byteSize)).ToString("X4") + ")");
                            }
                            else
                            {
                                stringoutput.WriteLine(x+": "+imp.ObjectName + "(From: " + imp.PackageFile + ") "+
                                    "(Offset: 0x" + (pcc.ImportOffset + (x * PCCObject.ImportEntry. byteSize)).ToString("X4") + ")");
                            }
                        }

                        stringoutput.WriteLine("--End of Imports");
                    }

                    if (exports && !scripts)
                    {
                        stringoutput.WriteLine("--Exports");
                    }
                    else if (!exports && scripts)
                    {
                        stringoutput.WriteLine("--Scripts");
                    }
                    else if (exports && scripts)
                    {
                        stringoutput.WriteLine("--Exports and Scripts");
                    }
                    int numDone = 1;
                    int numTotal = pcc.Exports.Count;
                    int lastProgress = 0;
                    writeVerboseLine("Gathering functions,data, and exports");
                    Boolean needsFlush = false;

                    foreach (PCCObject.ExportEntry exp in pcc.Exports)
                    {
                        if (exports || coalesced || data || (scripts && (exp.ClassName == "Function")))
                        {
                            int progress = ((int)(((double)numDone / numTotal) * 100));
                            while (progress >= (lastProgress + 10))
                            {
                                Console.Write("..." + (lastProgress + 10) + "%");
                                needsFlush = true;
                                lastProgress += 10;
                            }
                            stringoutput.WriteLine("=======================================================================");
                            if (coalesced && exp.likelyCoalescedVal)
                            {
                                stringoutput.Write("[C] ");
                            }
                            stringoutput.WriteLine(exp.PackageFullName + "." + exp.ObjectName + "(" + exp.ClassName + ") (Superclass: " + exp.ClassParentWrapped + ") (Data Offset: 0x" + exp.DataOffset + ")");
                            if (scripts && (exp.ClassName == "Function"))
                            {
                                stringoutput.WriteLine("==============Function==============");
                                Function func = new Function(exp.Data, pcc);
                                stringoutput.WriteLine(func.ToRawText());
                            }
                            if (data)
                            {
                                stringoutput.WriteLine("==============Data==============");
                                stringoutput.WriteLine(BitConverter.ToString(exp.Data));
                            }
                            numDone++;
                        }
                    }
                    if (exports && !scripts)
                    {
                        stringoutput.WriteLine("--End of Exports");
                    }
                    else if (!exports && scripts)
                    {
                        stringoutput.WriteLine("--End of Scripts");
                    }
                    else if (exports && scripts)
                    {
                        stringoutput.WriteLine("--End of Exports and Scripts");
                    }
                    if (needsFlush)
                    {
                        Console.WriteLine();
                    }

                    if (names)
                    {
                        writeVerboseLine("Gathering names");
                        stringoutput.WriteLine("--Names");

                        int count = 0;
                        foreach (string s in pcc.Names)
                            stringoutput.WriteLine((count++) + " : " + s);
                        stringoutput.WriteLine("--End of Names");

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception parsing " + file);
            }
        }


        public static void writeVerboseLine(String message)
        {
            if (verbose)
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fromPath"/> or <paramref name="toPath"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath))
            {
                throw new ArgumentNullException("fromPath");
            }

            if (string.IsNullOrEmpty(toPath))
            {
                throw new ArgumentNullException("toPath");
            }

            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
            Uri toUri = new Uri(AppendDirectorySeparatorChar(toPath));

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            // Append a slash only if the path is a directory and does not have a slash.
            if (!Path.HasExtension(path) &&
                !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }
    }
}

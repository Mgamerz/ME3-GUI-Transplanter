using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
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

        static byte[] copyByteChunk(byte[] input, int offset, int length)
        {
            byte[] output = new byte[length];

            for (int i = offset; i < offset + length; i++)
            {
                output[i - offset] = input[i];
            }

            return output;
        }

        /// <summary>
        /// Replaces the data for an export with the data for a SWF (GFX) file.
        /// </summary>
        /// <param name="ent">Export Entry to update data for</param>
        /// <param name="swf_path">SWF file to use as new data. This technically will work with any file.</param>
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
            //updating array metadata, length, datasize
            for (int i = 0; i < header.Length; i++)
                m.WriteByte(header[i]);

            for (int i = 0; i < number1.Length; i++)
                m.WriteByte(number1[i]);

            for (int i = 0; i < filler.Length; i++)
                m.WriteByte(filler[i]);

            for (int i = 0; i < number2.Length; i++)
                m.WriteByte(number2[i]);

            //set swf binary data
            for (int i = 0; i < swf_file.Length; i++)
                m.WriteByte(swf_file[i]);

            //write remaining footer data that was there originally
            for (int i = 0; i < bytefooter.Length; i++)
                m.WriteByte(bytefooter[i]);
            byte[] newdata = m.ToArray();
            Console.WriteLine("newdata size vs old: " + newdata.Length + " vs " + ent.Data.Length);
            ent.Data = m.ToArray();
            Console.WriteLine("Export has changed: " + ent.hasChanged);
            // ent.DataSize = ent.Data.Length;
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

                    writeVerboseLine("Extracting: " + outputpath + exp.PackageFullName + "." + fname + ".swf");
                    extract_swf(exp, outputpath + exp.PackageFullName + "." + fname + ".swf");
                }

                if (worker != null)
                {
                    worker.ReportProgress((int)(((double)i / numExports) * 100));
                }
            }
        }

        /// <summary>
        /// Recursively finds pcc files and extracts gfx files from them. If outputfolder is specified, it will place them into that folder relative to the inputfolder <> pcc file.
        /// </summary>
        /// <param name="folder">Base folder to start scanning from</param>
        /// <param name="outputfolder">Output folder to place gfx files into. The difference between each pcc file and the input folder directories will be used to create its structure.</param>
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
        /// Replaces a single SWF file in a specified PCC. As each export is scanned, if it is a GFX export and the name matches the input gfx filename, it will be replaced.
        /// </summary>
        /// <param name="gfxFile">GFX file to insert (SWF)</param>
        /// <param name="destinationFile">File to update GFX files in</param>
        /// <param name="targetExport">Target export to scan for. If none is specified the filename is used as the packname/object name export to find</param>
        public static int replaceSingleSWF(string gfxFile, string destinationFile, string targetExport = null)
        {
            string inpackobjname = Path.GetFileNameWithoutExtension(gfxFile);
            if (targetExport != null)
            {
                inpackobjname = targetExport;
            }
            string backupfile = destinationFile + ".bak";
            if (File.Exists(backupfile))
            {
                File.Delete(backupfile);
            }
            File.Move(destinationFile, backupfile);
            writeVerboseLine("Scanning " + destinationFile);
            PCCObject pcc = new PCCObject(backupfile);
            int numExports = pcc.Exports.Count;
            bool replaced = false;
            for (int i = 0; i < numExports; i++)
            {
                PCCObject.ExportEntry exp = pcc.Exports[i];

                if (exp.ClassName == "GFxMovieInfo")
                {
                    string packobjname = exp.PackageFullName + "." + exp.ObjectName;
                    if (packobjname.ToLower() == inpackobjname.ToLower())
                    {
                        Console.WriteLine("#" + i + " Replacing " + exp.PackageFullName + "." + exp.ObjectName);
                        replace_swf_file(exp, gfxFile);
                        replaced = true;
                        break;
                    }
                }

            }
            if (replaced)
            {
                if (pcc.Exports.Exists(x => x.ObjectName == "SeekFreeShaderCache" && x.ClassName == "ShaderCache"))
                {
                    Console.WriteLine("Saving PCC (this may take a while...)");
                    pcc.saveToFile(destinationFile, false);
                }
                else
                {
                    Console.WriteLine("Reconstructing PCC...)");
                    pcc.saveByReconstructing(destinationFile);
                }
                return VerifyPCC(destinationFile);

            }
            else
            {
                Console.WriteLine("No GFX file in the PCC with the name of " + inpackobjname + " was found.");
                File.Move(backupfile, destinationFile);
                return 1;
            }
        }

        public static Boolean doesPCCContainGUIs(string pccfilepath)
        {
            PCCObject pcc = new PCCObject(pccfilepath);
            foreach (PCCObject.ExportEntry export in pcc.Exports)
            {
                if (export.ClassName == "GFxMovieInfo")
                {
                    string packobjname = export.PackageFullName + "." + export.ObjectName;
                    //check if problem UI.

                }
            }
            return false;
        }

        /// <summary>
        /// Replaces all SWF files in a specified PCC. As each export is scanned, if it is a GFX export and a correspondingly named packagename.sf file exists, it will be repalced.
        /// </summary>
        /// <param name="gfxSourceFolder">Source folder, with gfx files in the root.</param>
        /// <param name="destinationFile">File to update GFX files in</param>
        public static int replaceSWFs(string gfxSourceFolder, string destinationFile, BackgroundWorker worker = null)
        {
            bool replaced = false;
            string[] gfxfiles = System.IO.Directory.GetFiles(gfxSourceFolder, "*.swf");
            List<String> packobjnames = new List<String>();
            foreach (string gfxfile in gfxfiles)
            {
                string packobjname = Path.GetFileNameWithoutExtension(gfxfile);
                writeVerboseLine("SWF in source folder: " + packobjname);
                packobjnames.Add(packobjname);
            }

            if (gfxfiles.Length > 0)
            {
                string backupfile = destinationFile + ".bak";
                if (File.Exists(backupfile))
                {
                    File.Delete(backupfile);
                }
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
                            writeVerboseLine("#" + i + " Replacing " + exp.PackageFullName + "." + exp.ObjectName);
                            replace_swf_file(exp, gfxfiles[index]);
                            numReplaced++;
                            replaced = true;
                        }
                    }
                    if (worker != null && numReplaced % 10 == 0)
                    {
                        //Console.WriteLine("Progress: " + i + " / "+numExports);
                        worker.ReportProgress((int)(((double)i / numExports) * 100));
                    }
                }
                writeVerboseLine("Replaced " + numReplaced + " files, saving.");
                if (replaced)
                {
                    //pcc.saveByReconstructing(destinationFile); //34 is default
                    if (pcc.Exports.Exists(x => x.ObjectName == "SeekFreeShaderCache" && x.ClassName == "ShaderCache"))
                    {
                        Console.WriteLine("Saving PCC (this may take a while...)");
                        pcc.saveToFile(destinationFile, false);
                    }
                    else
                    {
                        Console.WriteLine("Reconstructing PCC (this may take a while...)");
                        pcc.saveByReconstructing(destinationFile);
                    }
                    return VerifyPCC(destinationFile);
                }
                else
                {
                    Console.WriteLine("No SWFs replaced");
                    File.Move(backupfile, destinationFile);
                    return 0;
                }
            }
            else
            {
                Console.WriteLine("No source GFX files were found.");
                return 1;
            }
        }

        /// <summary>
        /// Formats arguments as a string
        /// </summary>
        /// <param name="filename">EXE file</param>
        /// <param name="arguments">EXE arguments</param>
        /// <returns></returns>
        public static string Format(string filename, string arguments)
        {
            return "'" + filename +
                ((string.IsNullOrEmpty(arguments)) ? string.Empty : " " + arguments) +
                "'";
        }

        public static int VerifyPCC(string pcc)
        {
            try
            {
                PCCObject obj = new PCCObject(pcc);

                foreach (PCCObject.ImportEntry imp in obj.Imports)
                {
                    String teststr = imp.ClassName;
                    int testval = imp.idxObjectName;
                    teststr = imp.PackageName;
                }
                foreach (PCCObject.ExportEntry exp in obj.Exports)
                {
                    String teststr = exp.ArchtypeName;
                    int testval = exp.DataSize;
                    byte[] data = exp.Data;
                }
                Console.WriteLine("PCC Loaded OK");
                return 0;
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("PCC Failed to load, threw exception: ");
                throw e;
                Console.Error.WriteLine(e.ToString());
                return 1;
            }
        }

        /// <summary>
        /// Runs an external executable
        /// </summary>
        /// <param name="filename">EXE to run</param>
        /// <param name="arguments">EXE arguments</param>
        /// <param name="workingdir">Working directory for EXE</param>
        /// <returns></returns>
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

        /// <summary>
        /// Dumps all exec functions from all pcc's in the specified folder into a file
        /// </summary>
        /// <param name="folder">Folder to search for pcc's for.</param>
        /// <param name="outputfolder">Folder to place a file named ExecFunctions.txt into. If null it will be placed in folder.</param>
        public static void dumpAllExecFromFolder(string folder, string outputfolder = null)
        {
            SortedSet<String> uniqueExecFunctions = new SortedSet<String>(); //unique set of function names
            SortedDictionary<String, List<String>> functionFileMap = new SortedDictionary<String, List<String>>(); //what functions appear in what file(s)
            SortedDictionary<String, String> functionTextMap = new SortedDictionary<String, String>(); //Function > Function Text

            string[] files = Directory.GetFiles(folder, "*.pcc", SearchOption.AllDirectories);
            int filesDone = 1;
            foreach (String pccfile in files)
            {
                Console.WriteLine("[" + (filesDone) + "/" + files.Length + "] Scanning for Exec: " + Path.GetFileName(pccfile));
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
                            string key = exp.PackageName + "." + exp.ObjectName;
                            if (functionFileMap.TryGetValue(key, out list))
                            {
                                // add to list
                                list.Add(Path.GetFileName(pccfile));
                            }
                            else
                            {
                                //make new list, add to it
                                list = new List<String>();
                                list.Add(Path.GetFileName(pccfile));
                                functionFileMap.Add(exp.PackageName + "." + exp.ObjectName, list);
                            }

                            //Check function text
                            string functext;
                            if (functionTextMap.TryGetValue(key, out functext))
                            {
                                // add to list
                                if (functext != func.ToString())
                                {
                                    functionTextMap[key] = func.ToRawText();
                                }
                            }
                            else
                            {
                                //set first time text
                                functionTextMap[key] = func.ToRawText();
                            }
                        }
                    }
                }
                filesDone++;
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
                    file.WriteLine("============Function============");
                    file.WriteLine(functionTextMap[key]);
                    file.WriteLine();
                }
            }
        }

        /// <summary>
        /// Dumps functions labeled as Exec into a file
        /// </summary>
        /// <param name="pccfile">File to dump functions from</param>
        /// <param name="outputfile">File to save dump to</param>
        public static void dumpAllExecFromFile(string pccfile, string outputfile = null)
        {
            Console.WriteLine("Scanning " + Path.GetFileName(pccfile) + " for Exec Functions");
            SortedSet<String> uniqueExecFunctions = new SortedSet<String>();
            SortedDictionary<String, List<String>> functionFileMap = new SortedDictionary<String, List<String>>();
            SortedDictionary<String, String> functionTextMap = new SortedDictionary<String, String>(); //Function > Function Text

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
                        string key = exp.PackageName + "." + exp.ObjectName;
                        if (functionFileMap.TryGetValue(key, out list))
                        {
                            list.Add(Path.GetFileName(pccfile));
                        }
                        else
                        {
                            list = new List<String>();
                            list.Add(Path.GetFileName(pccfile));
                            functionFileMap.Add(key, list);
                            //add new
                        }
                        //Check function text
                        string functext;
                        if (functionTextMap.TryGetValue(key, out functext))
                        {
                            // add to list
                            if (functext != func.ToString())
                            {
                                functionTextMap[key] = func.ToRawText();
                            }
                        }
                        else
                        {
                            //set first time text
                            functionTextMap[key] = func.ToRawText();
                        }
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
                    file.WriteLine("============Function============");
                    file.WriteLine(functionTextMap[key]);
                    file.WriteLine();
                }
            }
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
        /// Dumps PCC data from all PCCs in the specified folder, recursively.
        /// </summary>
        /// <param name="path">Base path to start dumping functions from. Will search all subdirectories for pcc files.</param>
        /// <param name="args">Set of arguments for what to dump. In order: imports, exports, data, scripts, coalesced, names. At least 1 of these options must be true.</param>
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

        /// <summary>
        /// Dumps data from a pcc file to a text file
        /// </summary>
        /// <param name="file">PCC file path to dump from</param>
        /// <param name="args">6 element boolean array, specifying what should be dumped. In order: imports, exports, data, scripts, coalesced, names. At least 1 of these options must be true.</param>
        /// <param name="outputfolder"></param>
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
                Boolean separateExports = args[6];
                Boolean properties = args[7];

                PCCObject pcc = new PCCObject(file);

                string outfolder = outputfolder;
                if (outfolder == null)
                {
                    outfolder = Directory.GetParent(file).ToString();
                }

                if (!outfolder.EndsWith(@"\"))
                {
                    outfolder += @"\";
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
                                stringoutput.WriteLine("#" + ((x + 1) * -1) + ": " + imp.PackageFullName + "." + imp.ObjectName + "(From: " + imp.PackageFile + ") " +
                                    "(Offset: 0x " + (pcc.ImportOffset + (x * PCCObject.ImportEntry.byteSize)).ToString("X4") + ")");
                            }
                            else
                            {
                                stringoutput.WriteLine("#" + ((x + 1) * -1) + ": " + imp.ObjectName + "(From: " + imp.PackageFile + ") " +
                                    "(Offset: 0x " + (pcc.ImportOffset + (x * PCCObject.ImportEntry.byteSize)).ToString("X4") + ")");
                            }
                        }

                        stringoutput.WriteLine("--End of Imports");
                    }

                    if (exports || scripts || data || coalesced)
                    {
                        string datasets = "";
                        if (exports)
                        {
                            datasets += "Exports ";
                        }
                        if (scripts)
                        {
                            datasets += "Scripts ";
                        }
                        if (coalesced)
                        {
                            datasets += "Coalesced ";
                        }
                        if (data)
                        {
                            datasets += "Data ";
                        }

                        stringoutput.WriteLine("--Start of " + datasets);


                        int numDone = 1;
                        int numTotal = pcc.Exports.Count;
                        int lastProgress = 0;
                        writeVerboseLine("Enumerating exports");
                        Boolean needsFlush = false;
                        int index = 0;
                        foreach (PCCObject.ExportEntry exp in pcc.Exports)
                        {
                            index++;
                            //Boolean isCoalesced = coalesced && exp.likelyCoalescedVal;
                            Boolean isCoalesced = true;
                            Boolean isScript = scripts && (exp.ClassName == "Function");
                            Boolean isEnum = exp.ClassName == "Enum";
                            int progress = ((int)(((double)numDone / numTotal) * 100));
                            while (progress >= (lastProgress + 10))
                            {
                                Console.Write("..." + (lastProgress + 10) + "%");
                                needsFlush = true;
                                lastProgress += 10;
                            }
                            if (exports || data || isScript || isCoalesced)
                            {
                                if (separateExports)
                                {
                                    stringoutput.WriteLine("=======================================================================");
                                }
                                stringoutput.Write("#" + index + " ");
                                if (isCoalesced)
                                {
                                    stringoutput.Write("[C] ");
                                }

                                if (exports || isCoalesced || isScript)
                                {
                                    stringoutput.WriteLine(exp.PackageFullName + "." + exp.ObjectName + "(" + exp.ClassName + ") (Superclass: " + exp.ClassParentWrapped + ") (Data Offset: 0x " + exp.DataOffset.ToString("X4") + ")");
                                }

                                if (isEnum)
                                {
                                    SFXEnum sfxenum = new SFXEnum(pcc, exp.Data);
                                    stringoutput.WriteLine(sfxenum.ToString());
                                }

                                if (isScript)
                                {
                                    stringoutput.WriteLine("==============Function==============");
                                    Function func = new Function(exp.Data, pcc);
                                    stringoutput.WriteLine(func.ToRawText());
                                }
                                if (properties)
                                {
                                    List<PropertyReader.Property> p;

                                    byte[] buff = exp.Data;
                                    p = PropertyReader.getPropList(pcc, buff);
                                    if (p.Count > 0)
                                    {
                                        stringoutput.WriteLine("=================================================Properties=================================================");
                                        stringoutput.WriteLine(String.Format("|{0,40}|{1,15}|{2,10}|{3,30}|", "Name", "Type", "Size", "Value"));
                                        for (int l = 0; l < p.Count; l++)
                                            stringoutput.WriteLine(PropertyReader.PropertyToText(p[l], pcc));
                                        stringoutput.WriteLine("==================================================================================================");

                                    }
                                }
                                if (data)
                                {
                                    stringoutput.WriteLine("==============Data==============");
                                    stringoutput.WriteLine(BitConverter.ToString(exp.Data));
                                }
                            }
                            numDone++;
                        }
                        stringoutput.WriteLine("--End of " + datasets);

                        if (needsFlush)
                        {
                            Console.WriteLine();
                        }
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
                Console.WriteLine("Exception parsing " + file + "\n" + e.Message);
            }
        }

        /// <summary>
        /// Writes a line to the console if verbose mode is turned on
        /// </summary>
        /// <param name="message">Verbose message to write</param>
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

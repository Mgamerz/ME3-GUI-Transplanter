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

        static String getPccDump(string filepath, Boolean imports, Boolean exports, Boolean names, Boolean data, Boolean scriptdata)
        {

            PCCObject pcc = new PCCObject(filepath);
            System.IO.StringWriter stringoutput = new System.IO.StringWriter();

            if (imports)
            {
                stringoutput.WriteLine("--Imports");
                for (int i = pcc.Imports.Count - 1; i >= 0; i--)
                    stringoutput.WriteLine(pcc.Imports[i].ObjectName);
                stringoutput.WriteLine("--Imports Finished");
            }

            if (exports)
            {
                stringoutput.WriteLine("--Exports");
                foreach (PCCObject.ExportEntry exp in pcc.Exports)
                {
                    stringoutput.WriteLine("=======================================================================");
                    stringoutput.WriteLine(exp.PackageFullName + "." + exp.ObjectName + "(" + exp.ClassName + ")");
                    if (scriptdata)
                    {
                        if (exp.ClassName == "Function")
                        {
                            stringoutput.WriteLine("==============Function==============");
                            Function func = new Function(exp.Data, pcc);
                            stringoutput.WriteLine(func.ToRawText());
                        }
                    }
                    if (data)
                    {
                        stringoutput.WriteLine("==============Data==============");
                        stringoutput.WriteLine(exp.Data);
                    }
                }
                stringoutput.WriteLine("--Exports Finished");

            }


            if (names)
            {
                int count = 0;
                foreach (string s in pcc.Names)
                    stringoutput.WriteLine((count++) + " : " + s);
            }
            return stringoutput.ToString();
        }


        static void dumpAllExecFunctions(string path)
        {
            string[] files = System.IO.Directory.GetFiles(path, "*.pcc");
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
        /// <param name="packageName">Packagename to scan for. If null, all exports are scanned.</param>
        /// <param name="path">Directory to put extracted GFX files. If it does not exist, it will be created.</param>
        public static void extractAllGFxMovies(string sourceFile, string packageName, string path, BackgroundWorker worker = null)
        {
            if (!path.EndsWith("\\"))
            {
                path = path + '\\';
            }
            PCCObject pcc = new PCCObject(sourceFile);
            int numExports = pcc.Exports.Count;
            for (int i = 0; i < numExports; i++)
            {
                PCCObject.ExportEntry exp = pcc.Exports[i];
                if ((packageName == null || String.Equals(exp.PackageFullName, packageName)) && exp.ClassName == "GFxMovieInfo")
                //if package is null just match on them all
                {
                    Directory.CreateDirectory(path);

                    String fname = exp.ObjectName;

                    Console.WriteLine("Extracting to: " + path + exp.PackageFullName + "." + fname + ".swf");
                    extract_swf(exp, path + exp.PackageFullName + "." + fname + ".swf");
                }

                if (worker != null)
                {
                    worker.ReportProgress((int)(((double)i / numExports) * 100));
                }
            }
        }

        static KeyValuePair<string, string> newPackageObject(string packagename, string objectname)
        {
            return new KeyValuePair<string, string>(packagename, objectname);
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
                        worker.ReportProgress((int) (((double)i / numExports) * 100));
                    }

                    writeVerboseLine("Replaced " + numReplaced + " files, saving.");
                }
                pcc.altSaveToFile(destinationFile, true);
            }
            else
            {
                Console.WriteLine("No source GFX files were found.");
            }
        }

        static void replaceSWFs(Dictionary<String, KeyValuePair<string, string>> swfToPackageMap, string sourceFile, string destinationfile, bool altSave = true)
        {
            PCCObject pcc = new PCCObject(sourceFile);
            foreach (KeyValuePair<string, KeyValuePair<string, string>> entry in swfToPackageMap)
            {
                Boolean itemFound = false;
                foreach (PCCObject.ExportEntry exp in pcc.Exports)
                {
                    if (String.Equals(exp.PackageFullName, entry.Value.Key) && String.Equals(exp.ObjectName, entry.Value.Value) && exp.ClassName == "GFxMovieInfo")
                    {
                        Console.WriteLine("Replacing " + exp.PackageFullName + "." + exp.ObjectName);
                        replace_swf_file(exp, entry.Key);
                        itemFound = true;
                        break;
                    }
                }
                if (!itemFound)
                {
                    throw new Exception("Unable to find export to replace. Please check your build tasks for errors. Could not find export: " + entry.Value.Key + " in file " + sourceFile + ".");
                }
            }
            Console.WriteLine("Saving file " + destinationfile);
            if (altSave)
            {
                pcc.altSaveToFile(destinationfile, true);
            }
            else
            {
                pcc.saveToFile(destinationfile, true);
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
                throw new Exception("OS error while executing " + filename + " " + String.Join(" ", arguments) + ": " + e.Message, e);
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

        static void clearFolder(string FolderName)
        {
            DirectoryInfo dir = new DirectoryInfo(FolderName);

            foreach (FileInfo fi in dir.GetFiles())
            {
                fi.Delete();
            }

            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                clearFolder(di.FullName);
                di.Delete();
            }
        }

        static void makeDir(string directory)
        {
            if (Directory.Exists(directory))
            {
                return;
            }
            Directory.CreateDirectory(directory); //create
        }

        static void Copy(string sourceDir, string targetDir)
        {
            makeDir(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));

            foreach (var directory in Directory.GetDirectories(sourceDir))
                Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
        }

        static void Build(string[] args)
        {
            //List<String> folders = new List<String>();
            //folders.Add(@"D:\Origin Games\Mass Effect 3\BIOGame\CookedPCConsole");
            //folders.Add(@"C:\Users\Michael\BIOGame\DLC\DLC_TestPatch\CookedPCConsole");
            //folders.Add(@"C:\Users\Michael\BIOGame\DLC\DLC_CON_MP1\CookedPCConsole");
            //folders.Add(@"C:\Users\Michael\BIOGame\DLC\DLC_CON_MP2\CookedPCConsole");
            //folders.Add(@"C:\Users\Michael\BIOGame\DLC\DLC_CON_MP3\CookedPCConsole");
            //folders.Add(@"C:\Users\Michael\BIOGame\DLC\DLC_CON_MP4\CookedPCConsole");
            //folders.Add(@"C:\Users\Michael\BIOGame\DLC\DLC_CON_MP5\CookedPCConsole");
            //dumpAllExecFromFolders(folders);

            //Environment.Exit(0);


            string myExeDir = (new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location)).Directory.ToString();
            Console.Out.WriteLine(myExeDir);

            string baseDir = myExeDir + @"\..\";

            string outputLocation = myExeDir + @"\..\output\MP Controller Support";

            //try
            //{
            //    Ini.IniFile ini = new IniFile(myExeDir + @"\..\config\ME3Controller.ini");
            //    outputLocation = ini.IniReadValue("General", "output_location");
            //}
            //catch (Exception e)
            //{
            //    outputLocation = myExeDir + @"\..\output\MP Controller Support";
            //}

            if (outputLocation == "")
            {
                outputLocation = myExeDir + @"\..\output\MP Controller Support";
            }

            Console.Out.WriteLine("================================================================");
            Console.Out.WriteLine("ME3Controller");
            Console.Out.WriteLine("================================================================\n");
            Console.Out.WriteLine("Excutable Directory: " + myExeDir);
            Console.Out.WriteLine("Output Directory: " + outputLocation);
            Console.Out.WriteLine("----------------------------------------------------------------\n");

            if (Directory.Exists(outputLocation))
            {
                Console.Out.Write("Cleaning outputdir...");
                clearFolder(outputLocation);
                Console.Out.WriteLine("done.");
            }
            Console.Out.Write("Making output directory...");
            makeDir(outputLocation);
            Console.Out.WriteLine("done.");

            Console.Out.Write("Copying base files...");
            Copy(baseDir + @"base_modfiles", outputLocation);
            Console.Out.WriteLine("done");

            Console.Out.WriteLine("\n----------------------------------------------------------------");
            Console.Out.WriteLine("BASEGAME");
            Console.Out.WriteLine("----------------------------------------------------------------\n");

            makeDir(outputLocation + @"\BASEGAME");

            Console.Out.WriteLine("Compiling BIOP_MP_COMMON.pcc...\n");

            Dictionary<String, KeyValuePair<string, string>> baseswfToPackageMap = new Dictionary<string, KeyValuePair<string, string>>();
            baseswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\BIOP_MP_COMMON\GUI_SF_MPAppearance.MPAppearance.swf", newPackageObject("GUI_SF_MPAppearance", "MPAppearance"));
            //lobby status bars, leave commented for now, we might change it back later
            //baseswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\BIOP_MP_COMMON\GUI_SF_MPLobbyStatusBars.MPLobbyStatusBars.swf", newPackageObject("GUI_SF_MPLobbyStatusBars", "MPLobbyStatusBars"));
            baseswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\BIOP_MP_COMMON\GUI_SF_MPMatchResults.MPMatchResults.swf", newPackageObject("GUI_SF_MPMatchResults", "MPMatchResults"));
            baseswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\BIOP_MP_COMMON\GUI_SF_MPPauseMenu.MPPauseMenu.swf", newPackageObject("GUI_SF_MPPauseMenu", "MPPauseMenu"));
            replaceSWFs(baseswfToPackageMap, baseDir + @"source_files\BASEGAME\BIOP_MP_COMMON.pcc", outputLocation + @"\BASEGAME\BIOP_MP_COMMON.pcc", true);

            Console.Out.WriteLine("\nCompiling Startup.pcc...\n");
            Dictionary<String, KeyValuePair<string, string>> startupswfToPackageMap = new Dictionary<string, KeyValuePair<string, string>>();
            startupswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\Startup\GUI_SF_MessageBox.messageBox.swf", newPackageObject("GUI_SF_MessageBox", "messageBox"));
            startupswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\Startup\GUI_SF_MessageBox_Hint.MessageBox_Hint.swf", newPackageObject("GUI_SF_MessageBox_Hint", "MessageBox_Hint"));
            startupswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\Startup\GUI_SF_MPPlayerCountdown.MPPlayerCountdown.swf", newPackageObject("GUI_SF_MPPlayerCountdown", "MPPlayerCountdown"));
            startupswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\Startup\GUI_SF_ME2_HUD.ME2_HUD.swf", newPackageObject("GUI_SF_ME2_HUD", "ME2_HUD"));
            startupswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\Startup\GUI_SF_SquadRecord.SquadRecord.swf", newPackageObject("GUI_SF_SquadRecord", "SquadRecord"));
            startupswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\Startup\GUI_SF_Options.Options.swf", newPackageObject("GUI_SF_Options", "Options"));
            startupswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\Startup\GUI_SF_PC_ME2_PowerWheel.PC_ME2_PowerWheel.swf", newPackageObject("GUI_SF_PC_ME2_PowerWheel", "PC_ME2_PowerWheel"));



            replaceSWFs(startupswfToPackageMap, baseDir + @"source_files\BASEGAME\Startup_xboxicons.pcc", outputLocation + @"\BASEGAME\Startup.pcc", true);

            //Console.Out.WriteLine("\nCompiling SFXGame.pcc...\n");
            //Dictionary<String, KeyValuePair<string, string>> sfxgameswfToPackageMap = new Dictionary<string, KeyValuePair<string, string>>();
            //sfxgameswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\SFXGame\GUI_SF_WeaponModMods.WeaponModMods.swf", newPackageObject("GUI_SF_WeaponModMods", "WeaponModMods"));
            //sfxgameswfToPackageMap.Add(baseDir + @"modded_files\BASEGAME\SFXGame\GUI_SF_WeaponModStats.WeaponModStats.swf", newPackageObject("GUI_SF_WeaponModStats", "WeaponModStats"));
            //replaceSWFs(sfxgameswfToPackageMap, baseDir + @"source_files\BASEGAME\SFXGame.pcc", outputLocation + @"\BASEGAME\SFXGame.pcc", true);


            Console.Out.WriteLine("\n----------------------------------------------------------------");
            Console.Out.WriteLine("MP5");
            Console.Out.WriteLine("----------------------------------------------------------------\n");
            Console.Out.WriteLine("Compiling MPLobby.pcc...\n");
            Dictionary<String, KeyValuePair<string, string>> mp5swfToPackageMap = new Dictionary<string, KeyValuePair<string, string>>();
            mp5swfToPackageMap.Add(baseDir + @"modded_files\MP5\MPLobby\GUI_SF_Leaderboard_DLC.Leaderboard.swf", newPackageObject("GUI_SF_Leaderboard_DLC", "Leaderboard"));
            mp5swfToPackageMap.Add(baseDir + @"modded_files\MP5\MPLobby\GUI_SF_MPMatchConsum_DLC.MPMatchConsumables.swf", newPackageObject("GUI_SF_MPMatchConsum_DLC", "MPMatchConsumables"));
            mp5swfToPackageMap.Add(baseDir + @"modded_files\MP5\MPLobby\GUI_SF_WeaponSelect_DLC.WeaponSelect.swf", newPackageObject("GUI_SF_WeaponSelect_DLC", "WeaponSelect"));
            mp5swfToPackageMap.Add(baseDir + @"modded_files\MP5\MPLobby\GUI_SF_MPChallenges_DLC.MPChallenges.swf", newPackageObject("GUI_SF_MPChallenges_DLC", "MPChallenges"));
            mp5swfToPackageMap.Add(baseDir + @"modded_files\MP5\MPLobby\GUI_SF_MPSelectKit_DLC.MPSelectKit.swf", newPackageObject("GUI_SF_MPSelectKit_DLC", "MPSelectKit"));
            mp5swfToPackageMap.Add(baseDir + @"modded_files\MP5\MPLobby\GUI_SF_MPNewLobby_DLC.MPNewLobby.swf", newPackageObject("GUI_SF_MPNewLobby_DLC", "MPNewLobby"));
            mp5swfToPackageMap.Add(baseDir + @"modded_files\MP5\MPLobby\GUI_SF_MPStore_DLC.MPStore.swf", newPackageObject("GUI_SF_MPStore_DLC", "MPStore"));
            mp5swfToPackageMap.Add(baseDir + @"modded_files\MP5\MPLobby\GUI_SF_MPReinforcements_DLC.MPReinforcementsReveal.swf", newPackageObject("GUI_SF_MPReinforcements_DLC", "MPReinforcementsReveal"));
            mp5swfToPackageMap.Add(baseDir + @"modded_files\MP5\MPLobby\GUI_SF_MPPromotion_DLC.MPPromotion.swf", newPackageObject("GUI_SF_MPPromotion_DLC", "MPPromotion"));
            //replaceSWFs(mp5swfToPackageMap, baseDir + @"source_files\MP5\MPLobby.pcc", outputLocation + @"\MP5\MPLobby.pcc");
            replaceSWFs(mp5swfToPackageMap, baseDir + @"source_files\MP5\MPLobby_pcccodeui.pcc", outputLocation + @"\MP5\MPLobby.pcc");

            //Console.Out.WriteLine("\nGenerating Coalesced Default_DLC_CON_MP5.bin");
            //Console.Out.WriteLine("Execute: "+ baseDir + @"tools\MassEffect3.Coalesce\MassEffect3.Coalesce.exe "+ baseDir + @"modded_files\MP5\Default_DLC_CON_MP5\Default_DLC_CON_MP5.xml");
            //RunExternalExe("\""+baseDir + "tools\\MassEffect3.Coalesce\\MassEffect3.Coalesce.exe\"", "\""+baseDir + "modded_files\\MP5\\Default_DLC_CON_MP5\\Default_DLC_CON_MP5.xml\"");
            //File.Move(baseDir + @"modded_files\MP5\Default_DLC_CON_MP5\Default_DLC_CON_MP5.bin", outputLocation + @"\MP5\Default_DLC_CON_MP5.bin");

            Console.Out.WriteLine("\nGenerating DLC_SHARED_INT.tlk");


            Console.Out.WriteLine("\n----------------------------------------------------------------");
            Console.Out.WriteLine("TESTPATCH");
            Console.Out.WriteLine("----------------------------------------------------------------\n");
            Console.Out.WriteLine("Compiling PATCH_GUI_MP_HUD.pcc...\n");
            Dictionary<String, KeyValuePair<string, string>> testPatchMPHUDswfToPackageMap = new Dictionary<string, KeyValuePair<string, string>>();
            testPatchMPHUDswfToPackageMap.Add(baseDir + @"modded_files\TESTPATCH\Patch_GUI_MP_HUD\GUI_SF_MP_HUD.MP_HUD.swf", newPackageObject("GUI_SF_MP_HUD", "MP_HUD"));
            replaceSWFs(testPatchMPHUDswfToPackageMap, baseDir + @"source_files\TESTPATCH\Patch_GUI_MP_HUD.pcc", outputLocation + @"\TESTPATCH\Patch_GUI_MP_HUD.pcc");

            Console.Out.WriteLine("Compiling Patch_WeaponModMods.pcc...\n");
            Dictionary<String, KeyValuePair<string, string>> testPatchWeaponModsswfToPackageMap = new Dictionary<string, KeyValuePair<string, string>>();
            testPatchWeaponModsswfToPackageMap.Add(baseDir + @"modded_files\TESTPATCH\Patch_WeaponModMods\GUI_SF_WeaponModMods.WeaponModMods.swf", newPackageObject("GUI_SF_WeaponModMods", "WeaponModMods"));
            replaceSWFs(testPatchWeaponModsswfToPackageMap, baseDir + @"source_files\TESTPATCH\Patch_WeaponModMods.pcc", outputLocation + @"\TESTPATCH\Patch_WeaponModMods.pcc");

            Console.Out.WriteLine("Compiling Patch_WeaponModStats.pcc...\n");
            Dictionary<String, KeyValuePair<string, string>> testPatchWEaponModStatsswfToPackageMap = new Dictionary<string, KeyValuePair<string, string>>();
            testPatchWEaponModStatsswfToPackageMap.Add(baseDir + @"modded_files\TESTPATCH\Patch_WeaponModStats\GUI_SF_WeaponModStats.WeaponModStats.swf", newPackageObject("GUI_SF_WeaponModStats", "WeaponModStats"));
            replaceSWFs(testPatchWEaponModStatsswfToPackageMap, baseDir + @"source_files\TESTPATCH\Patch_WeaponModStats.pcc", outputLocation + @"\TESTPATCH\Patch_WeaponModStats.pcc");

            Console.Out.WriteLine("Generating Coalesced Default_DLC_TestPatch.bin");
            RunExternalExe("\"" + baseDir + "tools\\MassEffect3.Coalesce\\MassEffect3.Coalesce.exe\"", "\"" + baseDir + "modded_files\\TESTPATCH\\Default_DLC_TestPatch\\Default_DLC_TestPatch.xml\"");
            File.Move(baseDir + @"modded_files\TESTPATCH\Default_DLC_TestPatch\Default_DLC_TestPatch.bin", outputLocation + @"\TESTPATCH\Default_DLC_TestPatch.bin");



            Console.Out.WriteLine("\n----------------------------------------------------------------");
            Console.Out.WriteLine("PATCH01");
            Console.Out.WriteLine("----------------------------------------------------------------\n");

            Console.Out.WriteLine("Generating Coalesced Default_DLC_UPD_Patch01.bin");
            RunExternalExe("\"" + baseDir + "tools\\MassEffect3.Coalesce\\MassEffect3.Coalesce.exe\"", "\"" + baseDir + "modded_files\\PATCH1\\Default_DLC_UPD_Patch01\\Default_DLC_UPD_Patch01.xml\"");
            File.Move(baseDir + @"modded_files\PATCH1\Default_DLC_UPD_Patch01\Default_DLC_UPD_Patch01.bin", outputLocation + @"\PATCH1\Default_DLC_UPD_Patch01.bin");


            Console.Out.WriteLine("\nBuild Finished.\n");
        }

        private static void dumpAllExecFromFolders(List<string> folders)
        {
            SortedSet<String> uniqueExecFunctions = new SortedSet<String>();
            SortedDictionary<String, List<String>> functionFileMap = new SortedDictionary<String, List<String>>();
            foreach (String folder in folders)
            {
                string[] files = System.IO.Directory.GetFiles(folder, "*.pcc");
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
                                System.Console.WriteLine("FOUND EXEC: " + exp.ObjectName);
                            }
                        }
                    }
                }
                System.Console.WriteLine("Read all PCC files from folder");
            }
            System.Console.WriteLine("Writing to file");

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Michael\Desktop\execfunctions.txt", false))
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
            System.Console.WriteLine("Done.");
        }

        static void Main(string[] args)
        {
            string myExeDir = (new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location)).Directory.ToString();
            string baseDir = myExeDir + @"\..\";

            //getPccDump(@"V:\Mass Effect 3\BIOGame\DLC\DLC_EXP_Pack003\CookedPCConsole\", "SFXPawn_Heavy");
            dumpAllFunctionsFromFolder(@"V:\Mass Effect 3\BIOGame\DLC\DLC_HEN_PR\CookedPCConsole\");
            dumpAllFunctionsFromFolder(@"V:\Mass Effect 3\BIOGame\DLC\DLC_CON_GUN02\CookedPCConsole\");
            dumpAllFunctionsFromFolder(@"V:\Mass Effect 3\BIOGame\DLC\DLC_CON_GUN01\CookedPCConsole\");
            dumpAllFunctionsFromFolder(@"V:\Mass Effect 3\BIOGame\DLC\DLC_CON_APP01\CookedPCConsole\");

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
        public static void dumpAllFunctionsFromFolder(string path)
        {
            string[] files = System.IO.Directory.GetFiles(path, "*.pcc");
            int i = 0;
            foreach (string file in files)
            {
                i++;
                try
                {
                    Console.WriteLine("[" + i + "/" + files.Length + "] Dumping " + Path.GetFileNameWithoutExtension(file));
                    getPccDump(file, false, true, false, false, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception parsing " + file);
                }
            }
        }
        public static void writeVerboseLine(String message)
        {
            if (verbose)
            {
                Console.WriteLine(message);
            }
        }
    }
}

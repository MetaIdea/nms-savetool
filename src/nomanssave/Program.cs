// Requires Newtonsoft.Json and SpookilySharp packages to compile
// Also requires small modifications to XXTEA.cs: The includeLenght parameter of ToByteArray should always be set to false

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;

namespace nomanssave
{
    class Program
    {
        enum Mode
        {
            Decryption,
            Encryption,    // for old actually encrypted saves
            Encryption2,   // for saves after 'foundation' update
        }

        enum GameMode
        {
            Normal,
            Survival,
            Creative,
            UserChoice
        }

        static void Main(string[] args)
        {
            if (args.Length < 1 || (args[0] != "encrypt" && args[0] != "decrypt" && args[0] != "normal" && args[0] != "survival" && args[0] != "creative" && !File.Exists(args[0])))
            {
                Console.WriteLine("usage1: {0} savefile", System.AppDomain.CurrentDomain.FriendlyName);
                Console.WriteLine("usage2: {0} [normal/survival/creative] <path to saves>", System.AppDomain.CurrentDomain.FriendlyName);
                Console.WriteLine("usage3: {0} [encrypt/decrypt] <path to saves>", System.AppDomain.CurrentDomain.FriendlyName);
                Environment.Exit(0);
            }

            List<string> savepaths = new List<string>();
            if (File.Exists(args[0]))
            {
                savepaths = args.ToList();
            }
            else if (args.Length > 1)
            {
                savepaths = args.Skip(1).ToList();
            }
            else
            {
                Console.WriteLine("Attempting to find No Man's Sky save folder(s)...");

                var savepath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelloGames"), "NMS");
                if (Directory.Exists(savepath))
                {
                    // Get a list of all folders in the save directory
                    foreach (var dir in Directory.GetDirectories(savepath))
                    {
                        Console.WriteLine("Found {0}", dir);
                        savepaths.Add(dir);
                    }
                }
                else
                {
                    Console.WriteLine("Could not find No Man's Sky save directory, please specify a folder manually");
                }
            }

            foreach (var savepath in savepaths)
            {
                try
                {
                    if (args[0] == "decrypt")
                        HandleFolder(savepath, Mode.Decryption, GameMode.Normal);
                    else if (args[0] == "encrypt")
                        HandleFolder(savepath, Mode.Encryption, GameMode.Normal);
                    else if (args[0] == "normal")
                        HandleFolder(savepath, Mode.Encryption2, GameMode.Normal);
                    else if (args[0] == "survival")
                        HandleFolder(savepath, Mode.Encryption2, GameMode.Survival);
                    else if (args[0] == "creative")
                        HandleFolder(savepath, Mode.Encryption2, GameMode.Creative);
                    else if (File.Exists(args[0]))
                        HandleFolder(savepath, Mode.Encryption2, GameMode.UserChoice);
                }
                catch (Exception e)
                {
                    // Don't handle any exceptions because I'm lazy
                    Console.WriteLine(e);
                    Environment.Exit(1);
                }
            }
        }

        static private ulong? GetProfileKeyFromPath(string foldername)
        {
            ulong? profileKey = null;
            if (foldername.Contains('\\'))
                foldername = foldername.Substring(foldername.LastIndexOf('\\') + 1);

            if (foldername.Contains('_'))
            {
                // Has key
                profileKey = ulong.Parse(foldername.Substring(foldername.LastIndexOf('_') + 1));
            }

            return profileKey;
        }

        static void HandleFolder(string savepath, Mode mode, GameMode gamemode)
        {
            // Try to parse saves
            var archives = new List<Tuple<string, string, string, uint>>();
            if (mode == Mode.Encryption || mode == Mode.Decryption)
            {
                archives.Add(new Tuple<string, string, string, uint>("storage.json", "mf_storage.hg", "storage.hg", 0));
                archives.Add(new Tuple<string, string, string, uint>("storage2.json", "mf_storage2.hg", "storage2.hg", 1));
                archives.Add(new Tuple<string, string, string, uint>("storage3.json", "mf_storage3.hg", "storage3.hg", 2));
                archives.Add(new Tuple<string, string, string, uint>("storage4.json", "mf_storage4.hg", "storage4.hg", 3));
                archives.Add(new Tuple<string, string, string, uint>("storage5.json", "mf_storage5.hg", "storage5.hg", 4));
                archives.Add(new Tuple<string, string, string, uint>("storage6.json", "mf_storage6.hg", "storage6.hg", 5));
                archives.Add(new Tuple<string, string, string, uint>("storage7.json", "mf_storage7.hg", "storage7.hg", 6));
                archives.Add(new Tuple<string, string, string, uint>("storage8.json", "mf_storage8.hg", "storage8.hg", 7));
                archives.Add(new Tuple<string, string, string, uint>("storage9.json", "mf_storage9.hg", "storage9.hg", 8));
            }
            else if (gamemode != GameMode.UserChoice)
            {
                if (gamemode == GameMode.Normal)
                {
                    archives.Add(new Tuple<string, string, string, uint>("storage.hg.backup", "mf_storage.hg", "storage.hg", 0));
                    archives.Add(new Tuple<string, string, string, uint>("storage2.hg.backup", "mf_storage2.hg", "storage2.hg", 1));
                    archives.Add(new Tuple<string, string, string, uint>("storage3.hg.backup", "mf_storage3.hg", "storage3.hg", 2));
                }
                else if (gamemode == GameMode.Survival)
                {
                    archives.Add(new Tuple<string, string, string, uint>("storage4.hg.backup", "mf_storage4.hg", "storage4.hg", 3));
                    archives.Add(new Tuple<string, string, string, uint>("storage5.hg.backup", "mf_storage5.hg", "storage5.hg", 4));
                    archives.Add(new Tuple<string, string, string, uint>("storage6.hg.backup", "mf_storage6.hg", "storage6.hg", 5));
                }
                else if (gamemode == GameMode.Creative)
                {
                    archives.Add(new Tuple<string, string, string, uint>("storage7.hg.backup", "mf_storage7.hg", "storage7.hg", 6));
                    archives.Add(new Tuple<string, string, string, uint>("storage8.hg.backup", "mf_storage8.hg", "storage8.hg", 7));
                    archives.Add(new Tuple<string, string, string, uint>("storage9.hg.backup", "mf_storage9.hg", "storage9.hg", 8));
                }
            }
            else
            {
                string Filename = Path.GetFileName(savepath);
                savepath = Path.GetDirectoryName(savepath);
                string index = Regex.Match(Filename, @"\d+").Value;
                int n;
                bool isNumeric = int.TryParse(index, out n);
                string Filename_ = Filename.Replace(".json", ".hg");
                if (isNumeric)
                  archives.Add(new Tuple<string, string, string, uint>(Filename, "mf_" + Filename_, Filename_, Convert.ToUInt32(index)-1));    
                else
                  archives.Add(new Tuple<string, string, string, uint>(Filename, "mf_" + Filename_, Filename_, 0));
            }

            // Get profile key from save path if possible
            var profileKey = GetProfileKeyFromPath(savepath);

            foreach (var archive in archives)
            {
                if (mode == Mode.Encryption2)
                {
                    if (File.Exists(Path.Combine(savepath, archive.Item3)))
                    {
                        System.IO.File.Copy(Path.Combine(savepath, archive.Item3), Path.Combine(savepath, archive.Item3) + ".backup", true);
                    }
                    if (File.Exists(Path.Combine(savepath, archive.Item2)))
                    {
                        System.IO.File.Copy(Path.Combine(savepath, archive.Item2), Path.Combine(savepath, archive.Item2) + ".backup",true);
                    }
                }

                string filepath = "";

                if (mode == Mode.Decryption)
                    filepath = archive.Item2;
                else if (mode == Mode.Encryption || mode == Mode.Encryption2)
                    filepath = archive.Item1;

                filepath = Path.Combine(savepath, filepath);
                if (File.Exists(filepath))
                {
                    var jsonFilename = Path.Combine(savepath, archive.Item1);
                    var metadataFilename = Path.Combine(savepath, archive.Item2);
                    var storageFilename = Path.Combine(savepath, archive.Item3);
                    var archiveNumber = archive.Item4;

                    if (mode == Mode.Decryption)
                    {
                        //Console.WriteLine("Reading {0}...", storageFilename);

                        var output = Storage.Read(metadataFilename, storageFilename, archiveNumber, profileKey);

                        // OPTIONAL! You do not need to install Newtonsoft.Json if you don't want to pretty print the data
                        try
                        {
                            output = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(output), Formatting.Indented);
                        }
                        catch
                        {
                            // If serializing and deserializing the json failed for some reason, just output the raw json
                        }

                        Console.WriteLine("Saving {0}", jsonFilename);
                        File.WriteAllText(jsonFilename, output);

                        // Set file timestamp for save ordering reasons
                        File.SetLastWriteTime(jsonFilename, File.GetLastWriteTime(storageFilename));
                        Console.WriteLine("Savegames decrypted!");
                    }
                    else if (mode == Mode.Encryption)
                    {
                        try
                        {
                            //Console.WriteLine("Reading {0}...", jsonFilename);

                            Storage.Write(jsonFilename, metadataFilename, storageFilename, archiveNumber, profileKey);
                            File.SetLastWriteTime(metadataFilename, File.GetLastWriteTime(jsonFilename));
                            File.SetLastWriteTime(storageFilename, File.GetLastWriteTime(jsonFilename));

                            Console.WriteLine("Savegames encrypted!");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("error - savegame not created");
                            Console.WriteLine(e);
                            Environment.Exit(1);
                        }
                    }
                    else if (mode == Mode.Encryption2)  // right now same procedure as Mode.Encryption until we find a way to create metadata based on unencrypted storageX.hg
                    {
                        try
                        {
                            //Console.WriteLine("Reading {0}...", jsonFilename);

                            Storage.Write(jsonFilename, metadataFilename, storageFilename, archiveNumber, profileKey);
                            File.SetLastWriteTime(metadataFilename, File.GetLastWriteTime(jsonFilename));
                            File.SetLastWriteTime(storageFilename, File.GetLastWriteTime(jsonFilename));

                            Console.WriteLine("Savegame creation successful!");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("error - savegame not created");
                            Console.WriteLine(e);
                            Environment.Exit(1);
                        }
                    }
                }
                else
                {
                    //Console.WriteLine("Could not find {0}", filepath);
                }
            }
        }
    }
}

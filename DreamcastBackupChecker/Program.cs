using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.IO.MemoryMappedFiles;
using System.Configuration;
using System.Reflection;

namespace DreamcastBackupChecker
{
    class Program
    {


        static void Main(string[] args)
        {
            List<DCGame> games = new List<DCGame>();

            foreach (string path in args)
            {
                
                if (Directory.Exists(path))
                {
                    Console.WriteLine("Looking for games in: " + path);
                    games = processsubDirectories(path);
                }
                else
                {
                    Console.WriteLine("{0} is not a valid directory.", path);
                }
            }

            File.WriteAllText(Environment.CurrentDirectory + @"\Report.txt", DCGameVerifier.MakeReport(games.ToArray()));

            Console.ReadLine();
        }

        static private List<DCGame> processsubDirectories(string path)
        {      
            DCGameVerifier dcv = new DCGameVerifier(Path.Combine(Environment.CurrentDirectory, "Dat Files"));
            string[] subdirectoryEntries = Directory.GetDirectories(path);
            List<DCGame> games = new List<DCGame>();
            foreach (string subdirectory in subdirectoryEntries)
            {
                Console.WriteLine("Processing " + subdirectory);
                games.Add(dcv.checkGame(subdirectory));
            }
            return games;
        }
    }

    class DCGameVerifier
    {
        private string[] DatFileContents;

        public DCGameVerifier(string datFilesDir)
        {
            loadDatFiles(datFilesDir);
        }

        private void loadDatFiles(string datFilesDir)
        {
            DatFileContents = new string[] { "" };

            foreach (var file in Directory.GetFiles(datFilesDir))
            {
                DatFileContents = DatFileContents.Union(File.ReadAllLines(file)).ToArray();
            }
        }

        public DCGame checkGame(string folder)
        {
            DCGame game = new DCGame();
            game.Files = Directory.GetFiles(folder);

            foreach (string gameFile in game.Files)
            {
                if ((gameFile.EndsWith(".bin") || gameFile.EndsWith(".raw"))
                    && !gameFile.EndsWith("ip.bin"))
                {
                    string MD5 = getMD5(gameFile);
                    
                    string matchedName = matchMD5ToGame(MD5);
                    
                    if (matchedName != "")
                    {
                        // Take first match possible for Game Name
                        if (game.Name == null)
                        { 
                            game.Name = matchedName;
                        }
                        game.CheckedFiles.Add(gameFile, true);
                    }
                    else
                    {
                        game.CheckedFiles.Add(gameFile, false);
                    }
                }
            }

            return game;
        }

        private string getMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }

        private string matchMD5ToGame(string MD5)
        {
            string gameName = "";

            foreach (string line in DatFileContents)
            {
                    // Find the name of the game
                    Regex regex = new Regex("\tname \"(.+)\"");
                    Match match = regex.Match(line);

                    if (match.Success)
                    {
                        gameName = match.Groups[1].Value;
                    }

                    if (line.Contains(MD5))
                    {
                        return gameName;
                    }
            }
            // No Match
            return "";
        }

        public static string MakeReport(DCGame[] games)
        {
            string report = "";
            foreach (var game in games)
            {
                report += game.Name + System.Environment.NewLine;
                foreach (var file in game.CheckedFiles)
                {
                    if (file.Value)
                    {
                        report += "\t" + file.Key + " - OK" + System.Environment.NewLine;
                    }
                    else
                    {
                        report += "\t" + file.Key + " - MISMATCH" + System.Environment.NewLine;
                    }
                }
                report += System.Environment.NewLine;
            }
            return report;
        }
    }

    public class DCGame
    {
        public string Name { get; set; }
        public string[] Files { get; set; }
        public Dictionary<string, bool> CheckedFiles { get; set; }

        public DCGame()
        {
            this.CheckedFiles = new Dictionary<string, bool>();
        }
    }
}

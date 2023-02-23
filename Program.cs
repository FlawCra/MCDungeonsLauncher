// See https://aka.ms/new-console-template for more information

using MongoDB.Bson;
using Nadezda.Gui.Framework.Controls;
using Nadezda.Gui.Framework.Lib;
using Nadezda.Gui.Framework.Units;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;

namespace MCDungeonsLauncher;

public class Program {
    
    private static LauncherWindow launcherWindow = new LauncherWindow(false);
    private static string GamePath = "DungeonsGame/";
    private static string VersionPrefix = "Current Version: ";
    private static BsonDocument currentData = null;
    private static WebClient webClient = new WebClient();
    
    private static Label versionLabel;
    private static Label debug1;
    private static Label debug2;
    private static Label debug3;
    
    private static string HashFile(string filePath)
    {
        using (FileStream stream = File.OpenRead(filePath))
        {
            SHA1 sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", String.Empty).ToLower();
        }
    }

    private static void updateManifest() {
        string res = webClient.DownloadString("https://piston-meta.mojang.com/v1/products/dungeons/f4c685912beb55eb2d5c9e0713fe1195164bba27/windows-x64.json");
        
        BsonDocument remoteData = BsonDocument.Parse(res);
        BsonDocument dungeons = remoteData["dungeons"][0].AsBsonDocument;
        BsonDocument remoteManifest = dungeons["manifest"].AsBsonDocument;
        BsonDocument remoteVersion = dungeons["version"].AsBsonDocument;

        if(currentData["manifestsha1"] != remoteManifest["sha1"]) {
            string newManifest = new WebClient().DownloadString(remoteManifest["url"].AsString);
            currentData["manifest"] = BsonDocument.Parse(newManifest);
            currentData["remoteversion"] = remoteVersion["name"].AsString;
            currentData["manifestsha1"] = remoteManifest["sha1"];
            
            Console.WriteLine("Updated Manifest");
        }
    }
    
    private static void updateGame(ref Button updateBtn) {
        BsonDocument manifest = currentData["manifest"].AsBsonDocument;
        
        BsonDocument files = manifest["files"].AsBsonDocument;
        List<BsonElement> realFiles = new List<BsonElement>();
        
        foreach (BsonElement el in files) {
            if(el.Value["type"] != "directory") {
                realFiles.Add(el);
                continue;
            }
            if(!Directory.Exists(GamePath + el.Name)) Directory.CreateDirectory(GamePath + el.Name);
        }

        double total = realFiles.Count,
            i = 0;

        foreach (BsonElement el in realFiles) {
            BsonDocument raw = el.Value["downloads"]["raw"].AsBsonDocument;
            double percent = (i / total) * 100;
            percent = Math.Floor((i / total) * 100);
            debug3.Text = $"Updating ({percent}%/100%)";
            i++;
            if(File.Exists(GamePath + el.Name)) {
                debug1.Text = raw["sha1"].AsString;
                debug2.Text = HashFile(GamePath + el.Name);
                
                if(raw["sha1"].AsString == HashFile(GamePath + el.Name)) continue;
                Console.WriteLine($"Updating {GamePath + el.Name}");
                webClient.DownloadFile(raw["url"].AsString, GamePath + el.Name);
                continue;
            }
            Console.WriteLine($"Downloading {GamePath + el.Name}");
            webClient.DownloadFile(raw["url"].AsString, GamePath + el.Name);
        }
        
        Console.WriteLine("Update Complete!");
        debug1.Text = "Update Complete!";
        debug2.Text = "";
        debug3.Text = "";
        currentData["version"] = currentData["remoteversion"];
        versionLabel.Text = $"Installed Version: {currentData["version"]}";
    }

    public static void Main(String[] args) {
        launcherWindow.OnClose = window => {
            File.WriteAllText($"{GamePath}/game.data", currentData.ToString());
        };
        
        if(!Directory.Exists(GamePath)) Directory.CreateDirectory(GamePath);
        if(!File.Exists($"{GamePath}/game.data")) File.WriteAllText($"{GamePath}/game.data","");
        if(!BsonDocument.TryParse(File.ReadAllText($"{GamePath}/game.data"), out currentData)) {
            Dictionary<string, BsonValue> unknownData = new Dictionary<string, BsonValue>();
            unknownData.Add("remoteversion", "UNKNOWN");
            unknownData.Add("version", "UNKNOWN");
            unknownData.Add("manifestsha1", "");
            unknownData.Add("manifest", new BsonDocument());
            currentData = BsonDocument.Create(unknownData);
        }

        updateManifest();
        
        Label remoteVersionLabel = new Label(new Rectangle(5, 5, 100, 20), $"Remote Version: {currentData["remoteversion"]}", Color.GOLD, FontSize.Large);
        versionLabel = new Label(new Rectangle(5, 35, 100, 20), $"Installed Version: {currentData["version"]}", Color.GOLD, FontSize.Large);
        Label manifestSha1 = new Label(new Rectangle(5, 65, 100, 20), $"Manifest SHA1: {currentData["manifestsha1"]}", Color.GOLD, FontSize.Large);
        Button checkUpdateButton = new Button(new Rectangle(5, 95, 350, 20), "Check for Updates and Play", Color.BLUE);
        debug1 = new Label(new Rectangle(5, 125, 100, 20), $"", Color.GOLD, FontSize.Large);
        debug2 = new Label(new Rectangle(5, 155, 100, 20), $"", Color.GOLD, FontSize.Large);
        debug3 = new Label(new Rectangle(5, 185, 100, 20), $"", Color.GOLD, FontSize.Large);
        
        
        checkUpdateButton.OnClick += (sender) => {
            Task.Run(() => {
                updateManifest();
                updateGame(ref sender);
                debug2.Text = "Starting Game...";
                ProcessStartInfo psi = new ProcessStartInfo();
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Win32NT:
                        psi.FileName = $"{GamePath}Dungeons.exe";
                        break;
                    case PlatformID.Unix:
                        psi.FileName = "proton";
                        psi.Arguments = $"{GamePath}Dungeons.exe";
                        break;
                }
                
                Process p = new Process();
                p.StartInfo = psi;
                p.Start();
            });
        };
        

        launcherWindow.Controls.Add(remoteVersionLabel);
        launcherWindow.Controls.Add(versionLabel);
        launcherWindow.Controls.Add(manifestSha1);
        launcherWindow.Controls.Add(checkUpdateButton);

        launcherWindow.Controls.Add(debug1);
        launcherWindow.Controls.Add(debug2);
        launcherWindow.Controls.Add(debug3);
        
        launcherWindow.Show();
    }
}



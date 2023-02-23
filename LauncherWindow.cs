using Nadezda.Gui.Framework.Controls;
using Nadezda.Gui.Framework.Lib;

namespace MCDungeonsLauncher; 

public class LauncherWindow : Window {

    public LauncherWindow(bool debug = false) : base(new Color(15,15,15,255), new Rectangle(0,0,1280,720), "Minecraft Dungeons Launcher", debug) {
        
    }
}
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;  // N'oubliez pas d'ajouter la référence System.Windows.Forms

namespace MyDriverBoosterInstaller
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Logger.StartupMessage("");
            Logger.LogInfo("Début du processus d'extraction.");

            // Répertoire de destination
            string destinationDirectory = @"C:\RepairKit";
            if (!Directory.Exists(destinationDirectory))
            {
                Logger.LogInfo("Le répertoire de destination n'existe pas. Création...");
                Directory.CreateDirectory(destinationDirectory);
            }

            // Récupération du ZIP embarqué dans les ressources
            // Vérifiez bien que le nom de ressource correspond à celui défini dans votre .csproj / Resources.resx
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "RepairKit.Resources.RepairKit.zip"; // À adapter si besoin

            using (Stream zipStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (zipStream == null)
                {
                    Logger.LogError("Le fichier ZIP embarqué n'a pas été trouvé.");
                    return;
                }

                // Ouvrir le flux ZIP avec ZipArchive
                using (ZipArchive archive = new ZipArchive(zipStream))
                {
                    int totalEntries = archive.Entries.Count;
                    int count = 0;
                    Logger.LogInfo("Extraction des fichiers...");

                    foreach (var entry in archive.Entries)
                    {
                        count++;
                        // Construction du chemin complet de destination
                        string fullPath = Path.Combine(destinationDirectory, entry.FullName);

                        // Si l'entrée est un dossier (le nom du fichier est vide)
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(fullPath);
                        }
                        else
                        {
                            // S'assurer que le répertoire existe
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                            // Extraire le fichier (écrase s’il existe déjà)
                            entry.ExtractToFile(fullPath, true);
                        }

                        // Affichage de la barre de progression dans la console
                        ProgressBar.Draw(count, totalEntries, barSize: 50, prefix: "Extracting");
                    }
                    Console.WriteLine(); // Passe à la ligne après la barre de progression
                    Logger.LogInfo("Extraction terminée.");
                }
            }

            // Chemin complet de l'exécutable à lancer (supposé extrait)
            string mainExePath = Path.Combine(destinationDirectory, "RepairKit-1.2.8.exe");
            if (File.Exists(mainExePath))
            {
                // Demande de confirmation via une boîte de dialogue
                DialogResult result = MessageBox.Show(
                    "Voulez-vous lancer RepairKit ?", 
                    "Confirmation", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Question);
                    
                // Log de la demande de confirmation
                Logger.LogInfo("Demande de confirmation affichée via MsgBox, en attente de la réponse de l'utilisateur.");

                if (result == DialogResult.Yes)
                {
                    Logger.LogInfo("Lancement de RepairKit ...");
                    try
                    {
                        Process.Start(new ProcessStartInfo(mainExePath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Erreur lors du lancement : " + ex.Message);
                    }

                    // Création d'un raccourci sur le Bureau (méthode sans IWshRuntimeLibrary)
                    CreateShortcut("RepairKit", mainExePath);
                    Logger.LogInfo("Raccourci créé sur le Bureau.");
                }
                else
                {
                    Logger.LogInfo("Lancement de RepairKit annulé par l'utilisateur.");
                }
            }
            else
            {
                Logger.LogError("Le fichier RepairKit-1.2.8.exe n'a pas été trouvé dans les fichiers extraits.");
            }
        }

        /// <summary>
        /// Crée un raccourci Windows sur le Bureau pour le fichier cible (sans IWshRuntimeLibrary).
        /// </summary>
        /// <param name="shortcutName">Nom du raccourci (sans extension .lnk)</param>
        /// <param name="targetPath">Chemin complet du fichier cible</param>
        private static void CreateShortcut(string shortcutName, string targetPath)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutLocation = Path.Combine(desktopPath, shortcutName + ".lnk");

                // Création de l’objet ShellLink via COM
                Type shellLinkType = Type.GetTypeFromCLSID(CLSID_ShellLink);
                if (shellLinkType == null)
                {
                    Logger.LogError("Impossible de récupérer le Type ShellLink (CLSID non valide).");
                    return;
                }

                object shellLinkObj = Activator.CreateInstance(shellLinkType);
                IShellLinkW shellLink = (IShellLinkW)shellLinkObj;

                // Configuration du ShellLink
                shellLink.SetPath(targetPath); // Chemin de l'exe ciblé
                shellLink.SetWorkingDirectory(Path.GetDirectoryName(targetPath));
                shellLink.SetDescription("Raccourci vers " + shortcutName);

                // Enregistrement du raccourci via IPersistFile
                IPersistFile persistFile = (IPersistFile)shellLinkObj;
                persistFile.Save(shortcutLocation, true);
            }
            catch (Exception ex)
            {
                Logger.LogError("Échec de la création du raccourci : " + ex.Message);
            }
        }

        // CLSID pour créer un ShellLink
        private static readonly Guid CLSID_ShellLink = new Guid("00021401-0000-0000-C000-000000000046");

        /// <summary>
        /// Interface IShellLinkW importée depuis shell32 (pour UNICODE).
        /// </summary>
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            int GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
                        int cchMaxPath,
                        ref WIN32_FIND_DATAW pfd,
                        int fFlags);

            int GetIDList(out IntPtr ppidl);
            int SetIDList(IntPtr pidl);
            int GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            int GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            int GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            int GetHotkey(out short pwHotkey);
            int SetHotkey(short wHotkey);
            int GetShowCmd(out int piShowCmd);
            int SetShowCmd(int iShowCmd);
            int GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
                                int cchIconPath,
                                out int piIcon);
            int SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            int Resolve(IntPtr hWnd, int fFlags);
            int SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        /// <summary>
        /// Structure WIN32_FIND_DATAW utilisée par IShellLinkW.GetPath.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }
    }

    /// <summary>
    /// Classe utilitaire pour afficher une barre de progression dans la console
    /// </summary>
    public static class ProgressBar
    {
        /// <summary>
        /// Affiche une barre de progression
        /// </summary>
        /// <param name="current">Valeur actuelle (progression)</param>
        /// <param name="total">Valeur totale</param>
        /// <param name="barSize">Largeur de la barre (nombre de caractères)</param>
        /// <param name="prefix">Préfixe affiché avant la barre</param>
        public static void Draw(int current, int total, int barSize, string prefix = "")
        {
            double progress = (double)current / total;
            int filledBars = (int)(progress * barSize);
            string bar = new string('#', filledBars) + new string('-', barSize - filledBars);
            Console.Write($"\r{prefix} [{bar}] {progress * 100:0.0}%");
        }
    }
}

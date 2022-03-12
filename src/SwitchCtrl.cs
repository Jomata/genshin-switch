using Lnk;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace GenshinSwitch
{
    internal class SwitchCtrl
    {
        public static void Switch(int index, int? delayMs = null)
        {
            if (index < 0)
            {
                return;
            }

            try
            {
                Process[] processes = Process.GetProcessesByName("GenshinImpact");

                if (processes.Length > 0)
                {
                    foreach (var process in processes)
                    {
                        process.CloseMainWindow();
                    }
                    if (!SpinWait.SpinUntil(() => Process.GetProcessesByName("GenshinImpact").Length <= 0, 15000))
                    {
                        MessageBox.Show("Could not stop Genshin Impact, please close it manually and try again！", "Tooltip", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            Config.Instance.Accounts[index].WriteReg();

            if(delayMs == int.MaxValue)
            {
                MessageBox.Show("Login info successfully updated","Success");
                return;
            } 

            if (string.IsNullOrEmpty(Config.Instance.InstallPath))
            {
                MessageBox.Show("Please set the Genshin installation path before you use the automatic restart", "Tooltip", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                string path = Config.Instance.InstallPath;
                string arguments = string.Empty;
                string workingDirectory = Environment.CurrentDirectory;

                FileInfo fileInfo = new FileInfo(path);
                string ext = fileInfo.Extension.ToLower();

                if (ext == ".exe" || ext == ".bat" || ext == ".cmd")
                {
                    // 直接启动
                }
                else if (ext == ".lnk")
                {
                    var ll = LoadLnk(path);

                    arguments = ll.Arguments;
                    workingDirectory = ll.WorkingDirectory;
                    path = ll.LocalPath;
                }
                else
                {
                    path = Path.Combine(path, "Genshin Impact Game", "GenshinImpact.exe");
                }

                var startInfo = new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    FileName = path,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    Verb = "runas",
                };

                if (delayMs != null)
                    Thread.Sleep((int)delayMs);
                Process.Start(startInfo);
            }
        }

        public static string FindInstallPath()
        {
            try
            {
                using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Genshin Impact");
                
                if (key == null)
                {
                    return null;
                }

                object installLocation = key.GetValue("InstallPath");

                if (installLocation != null && !string.IsNullOrEmpty(installLocation.ToString()))
                {
                    return installLocation.ToString();
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
            return null;
        }

        public static LnkFile LoadLnk(string lnkFile)
        {
            var raw = File.ReadAllBytes(lnkFile);

            if (raw[0] != 0x4c)
            {
                throw new Exception($"Invalid signature!");
            }

            return new LnkFile(raw, lnkFile);
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace UdgPan
{
    public class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string gameExe = GetGameExe(args);
            if (gameExe == null)
                return;

            string enPath = Path.Combine(Path.GetDirectoryName(gameExe), "en");

            try
            {
                PatchA3(enPath);
                PatchA2(enPath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
            finally
            {
                Console.Read();
            }
        }

        private static void PatchCpk(string enPath, string cpkName, Action<string> act)
        {
            string cpkDir = Path.Combine(enPath, cpkName);
            string outDir = Path.Combine(enPath, $"{Path.GetFileNameWithoutExtension(cpkName)}extract");

            ExtractCpk(cpkDir, outDir);

            act(outDir);

            {
                string newDir = $"{cpkDir}.new";
                string bakDir = $"{cpkDir}.bak";

                MakeCpk(outDir, newDir);
                File.Delete(bakDir);
                File.Move(cpkDir, bakDir);
                File.Move(newDir, cpkDir);
            }

            Directory.Delete(outDir, true);
        }

        private static void PatchA2(string enPath) => PatchCpk(enPath, "a2.cpk", dir =>
        {
            string workDir = Path.Combine(dir, "data", "_tex_pc");
            foreach (var (src, dst) in new (string, string)[]
            {
                ("pl00_bodyD2.btx", "pbD2.btx"),
                ("pl00_bodyD2_msk.btx", "pbD2m.btx"),
                ("pl00_clothD2.btx", "pcD2.btx"),
                ("pl00_clothD2_msk.btx", "pcD2m.btx"),
            })
            {
                File.Copy(Path.Combine(workDir, src), Path.Combine(workDir, dst));
            }
        });

        private static void PatchA3(string enPath) => PatchCpk(enPath, "a3.cpk", dir =>
        {
            string destDir = Path.Combine(dir, "data", "_dat");
            foreach (string entry in new string[] { "pchr.bnd", "pchr64.bnd" })
            {
                File.Copy(Path.Combine("data", entry), Path.Combine(destDir, entry), true);
            }
        });

        private static void ExtractCpk(string src, string dst)
        {
            if (Directory.Exists(dst))
                throw new ApplicationException($"Directory {dst} already exists; delete it and try again");

            Console.WriteLine(src);
            Directory.CreateDirectory(dst);
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine("tools", "quickbms.exe"),
                Arguments = $"{Path.Combine("tools", "cpk.bms")} \"{src}\" \"{dst}\"",
                UseShellExecute = false,
            });
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Directory.Delete(dst, true);
                throw new ApplicationException($"quickbms failed to extract {src} into {dst}");
            }
        }

        private static void MakeCpk(string src, string dst)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine("tools", "cpkmakec.exe"),
                Arguments = $"\"{src}\" \"{dst}\" -align=2048 -code=UTF-8 -mode=FILENAME -forcecompress",
                UseShellExecute = false,
            });
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new ApplicationException($"cpkmakec failed to compress {src} into {dst}");
        }

        private static string GetGameExe(string[] args)
        {
            if (args.Length > 1)
                return args[1];

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Danganronpa UDG binary|game.exe|All files (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                    return openFileDialog.FileName;
            }

            return null;
        }
    }
}

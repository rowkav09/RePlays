using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static RePlays.Utils.Functions;

namespace RePlays.Utils {
    public static class Compression {
        static Dictionary<int, double> fileTime = new Dictionary<int, double>();

        // Same name with "-compressed" before the extension, for any extension. The old code only
        // handled .mp4 and .mkv (and the exit handler only .mp4), so other formats got the original
        // path back and the original was deleted.
        public static string GetCompressedPath(string filePath) {
            return Path.Join(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "-compressed" + Path.GetExtension(filePath));
        }
        public static void CompressFile(string filePath, CompressClip data) {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = Path.Join(GetFFmpegFolder(), "ffmpeg"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            foreach (var arg in new[] { "-i", filePath, "-vcodec", "libx264", "-preset", data.quality ?? "medium", GetCompressedPath(filePath) }) startInfo.ArgumentList.Add(arg);

            Process process = new Process {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (s, e) => ffmpeg_input(e.Data, process, data.game);
            process.ErrorDataReceived += (s, e) => ffmpeg_input(e.Data, process, data.game);
            process.Start();
            process.Exited += async (sender, e) => await p_ExitedAsync(sender, e, filePath, process);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            fileTime.Add(process.Id, 0);

            WebMessage.DisplayToast(process.Id.ToString(), data.game, "Compressing", "none", (long)0, 100);
        }

        static void ffmpeg_input(string e, Process process, string game) {
            if (e == null)
                return;

            if (e.Contains("Duration: ")) {
                fileTime[process.Id] = TimeSpan.Parse(e.ToString().Trim().Substring(10, 11)).TotalSeconds;
            }

            if (e.Contains("frame=") && e.Contains("speed=") && !e.Contains("Lsize=")) {
                Logger.WriteLine(e);
                try {
                    WebMessage.DisplayToast(process.Id.ToString(), game, "Compressing", "none", Convert.ToInt32(TimeSpan.Parse(e.Trim().Substring(48, 11)).TotalSeconds), Convert.ToInt32(fileTime[process.Id]));
                }
                catch (Exception ex) {
                    Logger.WriteLine("Error: {}", ex.Message);
                }
            }
        }

        static async Task p_ExitedAsync(object sender, EventArgs e, string filePathOriginal, Process process) {
            WebMessage.DestroyToast(process.Id.ToString());
            int encoderExitCode;
            try { encoderExitCode = process.ExitCode; }
            catch (InvalidOperationException) { encoderExitCode = -1; }
            try { process.Kill(); } catch (InvalidOperationException) { }

            string filePathCompressed = GetCompressedPath(filePathOriginal);

            if (encoderExitCode != 0 || !File.Exists(filePathCompressed)) {
                // ffmpeg failed (possibly after writing partial output), keep the original untouched
                Logger.WriteLine($"Compression failed, ffmpeg exit code {encoderExitCode}, output exists: {File.Exists(filePathCompressed)}");
                if (File.Exists(filePathCompressed)) File.Delete(filePathCompressed);
                WebMessage.DisplayModal("Failed to compress the file", "Error", "warning");
                return;
            }

            long originalFileSize = new FileInfo(filePathOriginal).Length;
            long compressedFileSize = new FileInfo(filePathCompressed).Length;

            var startInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                FileName = Path.Join(GetFFmpegFolder(), "ffprobe"),
            };
            // this used to pass the size in bytes as the file name, so the check always failed
            foreach (var arg in new[] { "-v", "error", "-i", filePathCompressed }) startInfo.ArgumentList.Add(arg);

            using var verifyProcess = Process.Start(startInfo);
            // ffprobe -v error reports problems on stderr; read it at the same time as stdout so neither pipe fills up
            var stderrTask = verifyProcess.StandardError.ReadToEndAsync();
            string output = verifyProcess.StandardOutput.ReadToEnd();
            verifyProcess.WaitForExit();
            string errorOutput = await stderrTask;
            Logger.WriteLine("Output: " + output + errorOutput);
            bool probeFailed = verifyProcess.ExitCode != 0 || !string.IsNullOrWhiteSpace(output) || !string.IsNullOrWhiteSpace(errorOutput);

            if (compressedFileSize > originalFileSize || compressedFileSize == 0 || probeFailed) {
                if (compressedFileSize == 0 || probeFailed) WebMessage.DisplayModal("Failed to compress the file", "Error", "warning");
                if (compressedFileSize > originalFileSize) WebMessage.DisplayModal("The compressed file turned out to be larger than the original file. We will keep the original file.", "Compression size", "warning");
                File.Delete(filePathCompressed);
                return;
            }

            try {
                // replace in one step so a failed move can't leave us without the original
                File.Move(filePathCompressed, filePathOriginal, true);
            }
            catch (Exception ex) {
                Logger.WriteLine($"Error: {ex.Message}");
                WebMessage.DisplayModal("Failed to compress the file", "Error", "warning");
                return;
            }

#if RELEASE && WINDOWS
            var t = await Task.Run(() => GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy, true));
#else
            var t = await Task.Run(() => GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy));
#endif

            Logger.WriteLine(t);
            WebMessage.SendMessage(t);

            WebMessage.DisplayModal("Successfully compressed the file from " + GetReadableFileSize(originalFileSize) + " to " + GetReadableFileSize(compressedFileSize), "Success", "success");

        }
    }
}

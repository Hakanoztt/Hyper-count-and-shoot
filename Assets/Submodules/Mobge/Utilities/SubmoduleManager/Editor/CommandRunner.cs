using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace SubmoduleManager {
    public static class CommandRunner {
        public struct OutputData {
            public int ExitCode;
            public string Output;
            public string ErrorOutput;
        }
        public static OutputData RunCommand(string fileName, string arguments) {
            var processInfo = new ProcessStartInfo(fileName, arguments) {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var process = new Process {
                StartInfo = processInfo
            };
            Debug.Log("running process: " + fileName + " " + arguments);
            try {
                process.Start();
            }
            catch (Exception) {
                return new OutputData {
                    ExitCode = -1,
                    ErrorOutput = $"{fileName} cannot be found!"
                };
            }
            var output = process.StandardOutput.ReadToEnd();
            Debug.Log(output);
            var errorOutput = process.StandardError.ReadToEnd();
            Debug.Log(errorOutput);
            process.WaitForExit();
            var exitCode = process.ExitCode;
            Debug.Log(exitCode);
            process.Close();
            return new OutputData {
                ExitCode = exitCode,
                Output = output,
                ErrorOutput = errorOutput,
            };
        }
    }
}
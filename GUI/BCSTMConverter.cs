namespace BCSTM_to_Wav_Converter_GUI
{
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using System.Windows.Forms;

	public static class BCSTMConverter
	{
		private static readonly string PathToConverter = GetVgmStreamExpectedDir();

		private static string GetVgmStreamExpectedDir()
		{
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\VGMStream\convert.exe";
		}

		public static void Run(string input, string output, out string result)
		{
			if (input.Length == 0)
			{
				MessageBox.Show("No files to convert!", "Error");
				result = "No files to convert!";
				return;
			}
			ConvertToWav(input, output, out result);
			

			// MessageBox.Show("Done!");
		}

		private static void ConvertToWav(string inputPath, string outputDir, out string result)
		{
			try
			{
				CheckIfVgmStreamAvailable();

				var file = new FileInfo(inputPath);
				if (!file.Exists)
				{
					throw new FileNotFoundException($"File {file.Name} not found!");
				}

				var converterProcess = new ProcessStartInfo(PathToConverter)
											{
												Arguments = $"-o {Path.GetFileNameWithoutExtension(inputPath)}.wav {file}", 
												WorkingDirectory = outputDir, 
												UseShellExecute = false, 
												RedirectStandardOutput = true, 
												CreateNoWindow = true
											};

				var process = Process.Start(converterProcess);
				process.WaitForExit();

				if (process.ExitCode == 0)
				{
					result = ($"{file.Name} converted successfully.");
				}
				else
				{
					result = ($"{file.Name} conversion failed.");
				}
			}
			catch (FileNotFoundException ex)
			{
				result = (ex.FileName + " was not found.");
			}
			catch (IOException ex)
			{
				result = (ex.Message);
			}
		}

		private static void CheckIfVgmStreamAvailable()
		{
			if (!File.Exists(PathToConverter))
			{
				var currentPath = PathToConverter;
				MessageBox.Show($"VGMStream not found! Make sure there's a \"VGMStream\" folder at: \r\n{currentPath}");
			}
		}
	}
}
namespace BCSTM_to_Wav_Converter_GUI
{
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Threading.Tasks;
	using System.Windows.Forms;

	public class BCSTMConverter
	{
		private string[] paths;

		private string output;

		private BCSTMToWavForm form;

		private static readonly string PathToConverter = GetVgmStreamExpectedDir();

		public BCSTMConverter(string[] paths, string output, BCSTMToWavForm form)
		{
			this.paths = paths;
			this.output = output;
			this.form = form;
		}

		private static string GetVgmStreamExpectedDir()
		{
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\VGMStream\convert.exe";
		}

		public void Run()
		{
			if (this.paths.Length == 0)
			{
				MessageBox.Show("No files to convert!", "Error");
			}

			var outputPath = SetOutputPath(this.output);

			this.form.Log($"Output path: {outputPath}");
			foreach (var path in this.paths)
			{
				var pathExists = Directory.Exists(path) || File.Exists(path);
				if (!pathExists)
				{
					this.LogMessage($"File or folder \"{path}\" not found!");
					return;
				}

				var attributes = File.GetAttributes(path);

				var inputIsDirectory = (attributes & FileAttributes.Directory) == FileAttributes.Directory;
				if (inputIsDirectory)
				{
					var filesToConvert = FindStreamFiles(path);

					this.form.Log($"Number of files to convert: {filesToConvert.Length}");
					Parallel.ForEach(filesToConvert, currentFile => { this.ConvertToWav(currentFile, outputPath); });
				}
				else
				{
					this.ConvertToWav(path, outputPath);
				}
			}

			MessageBox.Show("Finished!");
		}

		private void LogMessage(string message)
		{
			this.form.Log(message);
		}

		private static string SetOutputPath(string outputPath)
		{
			outputPath = new FileInfo(outputPath).DirectoryName + @"\converted\";

			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}

			return outputPath;
		}

		private static string[] FindStreamFiles(string path)
		{
			var dir = new DirectoryInfo(path);

			var files =
				dir.GetFiles().Where(a => a.Extension == ".bcstm").OrderBy(f => f.Length).ToArray().Select(f => f.FullName);

			return files.ToArray();
		}

		private void ConvertToWav(string inputPath, string outputDir)
		{
			try
			{
				if (!File.Exists(PathToConverter))
				{
					var currentPath = PathToConverter;
					MessageBox.Show($"VGMStream not found! Make sure there's a \"VGMStream\" folder at: \r\n{currentPath}");
				}

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
					this.form.UpdateProgressBar();
					this.LogMessage($"{file.Name} converted successfully");
				}
			}
			catch (FileNotFoundException ex)
			{
				this.LogMessage(ex.FileName + " was not found.");
			}
			catch (IOException ex)
			{
				this.LogMessage(ex.Message);
			}
		}
	}
}
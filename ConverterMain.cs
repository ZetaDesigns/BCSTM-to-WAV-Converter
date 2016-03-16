namespace BCSTM_to_Wav_Converter
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;

	public static class ConverterMain
	{
		private const string PathToConverter = @"VGMStream\convert.exe";

		public static void Main(string[] path)
		{
			if (ArgumentsAreInvalid(path))
			{
				return;
			}

			var inputPath = path[0];
			var outputPath = SetOutputPath(path);

			if (!Directory.Exists(inputPath) && !File.Exists(inputPath))
			{
				Console.WriteLine($"File or folder \"{inputPath}\" not found!");
				return;
			}

			var attributes = File.GetAttributes(inputPath);
			var inputIsDirectory = (attributes & FileAttributes.Directory) == FileAttributes.Directory;
			if (inputIsDirectory)
			{
				var filesToConvert = FindStreamFiles(inputPath);

				Parallel.ForEach(filesToConvert, (currentFile) => { ConvertToWav(currentFile, outputPath); });
			}
			else
			{
				ConvertToWav(inputPath, outputPath);
			}
		}

		private static string SetOutputPath(string[] path)
		{
			string outputPath;

			if (path.Length == 2)
			{
				outputPath = path[1];
			}
			else
			{
				outputPath = new FileInfo(path[0]).DirectoryName + @"\converted\";
			}

			return outputPath;
		}

		private static bool ArgumentsAreInvalid(string[] path)
		{
			if (path.Length == 0)
			{
				Console.WriteLine("BCSTM To Wav Converter");
				Console.WriteLine("Usage: BCSTM-to-WAV.exe FolderOrFile OutputPath");
#if !DEBUG
				Console.WriteLine("Press any key to exit...");
				Console.ReadKey();
#endif
				return true;
			}

			if (path.Length > 2)
			{
				Console.WriteLine("Invalid Argument Count");
				return true;
			}

			return false;
		}

		private static string[] FindStreamFiles(string path)
		{
			var dir = new DirectoryInfo(path);

			var files =
				dir.GetFiles().Where(a => a.Extension == ".bcstm").OrderBy(f => f.Length).ToArray().Select(f => f.FullName);

			return files.ToArray();
		}

		private static void ConvertToWav(string inputPath, string outputDir)
		{
			try
			{
				var file = new FileInfo(inputPath);
				if (!file.Exists)
				{
					throw new FileNotFoundException($"File {file.Name} not found!");
				}

				var converterProcess = new ProcessStartInfo(PathToConverter);

				converterProcess.Arguments = $"-o {Path.GetFileNameWithoutExtension(inputPath)}.wav {file}";
				converterProcess.WorkingDirectory = outputDir;
				converterProcess.RedirectStandardOutput = true;
				converterProcess.UseShellExecute = false;

				var process = Process.Start(converterProcess);
				process.WaitForExit();
				if (process.ExitCode == 0)
				{
					Console.WriteLine($"{file.Name} converted successfully");
				}
			}
			catch (FileNotFoundException ex)
			{
				Console.WriteLine(ex.FileName + " was not found.");
			}
			catch (IOException ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
	}
}
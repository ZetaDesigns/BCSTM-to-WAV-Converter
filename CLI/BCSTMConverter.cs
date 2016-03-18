namespace BCSTM_to_Wav_Converter
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Threading.Tasks;

	public class BCSTMConverter
	{
		private static readonly string PathToConverter = GetVgmStreamExpectedDir();

		private static string GetVgmStreamExpectedDir()
		{
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\VGMStream\convert.exe";
		}

		public static void ConvertToWav(string[] args)
		{
			ValidateArguments(args);

			bool outputPathDefined;
			var outputPath = SetOutputPath(args, out outputPathDefined);

			var numberOfArgsToSkip = outputPathDefined ? 2 : 0;
			var inputPaths = args.Take(args.Length - numberOfArgsToSkip).ToArray();

			Console.WriteLine($"Output path: {outputPath}");

			foreach (var inputPath in inputPaths)
			{
				var pathExists = Directory.Exists(inputPath) || File.Exists(inputPath);
				if (!pathExists)
				{
					LogError($"File or folder \"{inputPath}\" not found!");
					return;
				}

				var attributes = File.GetAttributes(inputPath);

				var inputIsDirectory = (attributes & FileAttributes.Directory) == FileAttributes.Directory;
				if (inputIsDirectory)
				{
					var filesToConvert = FindStreamFiles(inputPath);
					Console.WriteLine($"Number of files to convert: {filesToConvert.Length}");

					Parallel.ForEach(filesToConvert, currentFile => { ConvertToWav(currentFile, outputPath); });
				}
				else
				{
					ConvertToWav(inputPath, outputPath);
				}
			}

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Finished!");
			Console.ResetColor();
			Exit(0);
		}

		private static string SetOutputPath(string[] args, out bool outputPathDefinedByArgs)
		{
			string outputPath;
			var outputPathDefined = args.Length >= 3 && args[args.Length - 2] == "-o";
			if (outputPathDefined)
			{
				outputPath = args[args.Length - 1];
				outputPathDefinedByArgs = true;
			}
			else
			{
				outputPath = new FileInfo(args[0]).DirectoryName + @"\converted\";
				outputPathDefinedByArgs = false;
			}

			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}

			return outputPath;
		}

		private static void ValidateArguments(string[] path)
		{
			if (path.Length == 0)
			{
				Console.WriteLine("BCSTM To Wav Converter");
				Console.WriteLine("Usage: BCSTM-to-WAV.exe FolderOrPath FolderOrPath2 FolderOrPath3 etc");
				Console.WriteLine("If you want to output to a custom path use \"-o OutputPath\" at the end");
				Exit(1);
			}
		}

		private static void Exit(int exitCode)
		{
#if !DEBUG
			Console.WriteLine("Press any key to exit...");
#endif
			Environment.Exit(exitCode);
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
				if (!File.Exists(PathToConverter))
				{
					var currentPath = PathToConverter; // Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
					LogError($"VGMStream not found! Make sure there's a \"VGMStream\" folder at: \r\n{currentPath}");
					Exit(1);
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
												RedirectStandardOutput = true, 
												UseShellExecute = false
											};

				var process = Process.Start(converterProcess);
				process.WaitForExit();

				if (process.ExitCode == 0)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"{file.Name} converted successfully");
					Console.ForegroundColor = ConsoleColor.Gray;
				}
			}
			catch (FileNotFoundException ex)
			{
				LogError(ex.FileName + " was not found.");
			}
			catch (IOException ex)
			{
				LogError(ex.Message);
			}
		}

		private static void LogError(string error)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Error.WriteLine(error);
			Console.ResetColor();
		}
	}
}
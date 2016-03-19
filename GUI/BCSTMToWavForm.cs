// ReSharper disable InconsistentNaming
// ReSharper disable CoVariantArrayConversion
// ReSharper disable LocalizableElement
namespace BCSTM_to_Wav_Converter_GUI
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Documents;
	using System.Windows.Forms;

	using Microsoft.WindowsAPICodePack.Taskbar;

	public partial class BCSTMToWavForm : Form
	{
		private int currentFileCount;

		private int totalFileCount;

		private bool taskIsRunning;

		private CancellationTokenSource cancellationTokenSource;

		public BCSTMToWavForm()
		{
			this.InitializeComponent();
		}

		private void addButton_Click(object sender, EventArgs e)
		{
			var selectedFiles = new OpenFileDialog { Multiselect = true, Filter = "3DS Audio Stream Files (.bcstm)|*.bcstm" };

			var result = selectedFiles.ShowDialog();

			if (result == DialogResult.OK)
			{
				var selected = selectedFiles.FileNames;

				var filesBoxAlreadyHasItems = this.filesBox.Items.Count > 0;
				this.AddFiles(selected);

				if (!filesBoxAlreadyHasItems)
				{
					this.outputTextbox.Text = Path.GetDirectoryName(this.filesBox.Items[0].ToString()) + @"\Converted\";
				}
			}
		}

		private void AddFiles(string[] selected)
		{
			this.filesBox.Items.AddRange(selected);
		}

		private void removeButton_Click(object sender, EventArgs e)
		{
			var selectedItems = this.filesBox.SelectedItems.OfType<string>().ToArray();

			foreach (string item in selectedItems)
			{
				this.filesBox.Items.Remove(item);
			}
		}

		private void convertButton_Click(object sender, EventArgs e)
		{
			if (this.converterWorker.IsBusy)
			{
				MessageBox.Show("You are already converting stuff.");
				return;
			}

			var files = this.filesBox.Items.OfType<string>().ToArray();
			var output = this.outputTextbox.Text;

			if (files.Length == 0)
			{
				MessageBox.Show("No files selected for conversion.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			this.InitializeUI(files.Length);

			TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);

			this.cancellationTokenSource = new CancellationTokenSource();

			var outputPath = SetOutputPath(output);

			this.Log($"Output path: {outputPath}");

			var arguments = new object[] { files, outputPath };
			this.converterWorker.RunWorkerAsync(arguments);
		}

		private static string SetOutputPath(string outputPath)
		{
			outputPath = new FileInfo(outputPath).DirectoryName;

			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}

			return outputPath;
		}

		private void InitializeUI(int fileCount)
		{
			this.progressBar.Value = 0;
			this.progressBar.Maximum = this.filesBox.Items.Count;

			this.currentFileCount = 0;
			this.totalFileCount = fileCount;

			TaskbarManager.Instance.SetProgressValue(0, this.progressBar.Maximum);
		}

		private void browseButton_Click(object sender, EventArgs e)
		{
			var dialog = new FolderBrowserDialog();
			var result = dialog.ShowDialog();

			if (result == DialogResult.OK)
			{
				this.outputTextbox.Text = dialog.SelectedPath;
			}
		}

		private void BCSTMToWavForm_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy;
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}

		private void BCSTMToWavForm_DragDrop(object sender, DragEventArgs e)
		{
			var paths = (string[])e.Data.GetData(DataFormats.FileDrop, false);

			var filesBoxHasItems = this.filesBox.Items.Count > 0;

			foreach (var path in paths)
			{
				var attributes = File.GetAttributes(path);

				var inputIsDirectory = (attributes & FileAttributes.Directory) == FileAttributes.Directory;
				if (inputIsDirectory)
				{
					var files = this.GetFilesInDirectory(path);
					this.filesBox.Items.AddRange(files);
				}
				else
				{
					this.filesBox.Items.Add(path);
				}
			}

			if (!filesBoxHasItems)
			{
				this.outputTextbox.Text = Path.GetDirectoryName(this.filesBox.Items[0].ToString()) + @"\Converted\";
			}
		}

		private string[] GetFilesInDirectory(string path)
		{
			var dir = new DirectoryInfo(path);

			var files = dir.GetFiles().OrderByDescending(p => p.Length).Select(f => f.FullName);

			return files.ToArray();
		}

		private void UpdateProgressBar()
		{
			this.progressBar.PerformStep();
			TaskbarManager.Instance.SetProgressValue(this.progressBar.Value, this.progressBar.Maximum);
		}

		private void Log(string message)
		{
			this.logTextBox.AppendText(message + Environment.NewLine);
			this.logTextBox.ScrollToCaret();
		}

		private void IncrementNumberOfFilesDone()
		{
			this.currentFileCount++;
			this.currentFileStatusLabel.Text = $"{this.currentFileCount}/{this.totalFileCount}";
		}

		private void stopButton_Click(object sender, EventArgs e)
		{
			var isTaskBeingCancelled = this.cancellationTokenSource != null
										&& this.cancellationTokenSource.IsCancellationRequested == false;

			if (isTaskBeingCancelled)
			{
				var message = MessageBox.Show(
					"Are you sure you want to cancel the currently running task?", 
					"Warning", 
					MessageBoxButtons.YesNo, 
					MessageBoxIcon.Question);

				if (message == DialogResult.OK)
				{
					this.cancellationTokenSource.Cancel();
					TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Error);
					this.progressBar.Value = 0;
					TaskbarManager.Instance.SetProgressValue(0, 100);
					this.taskIsRunning = false;
				}
			}
		}

		private void converterWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			// lmao
			var args = (object[])e.Argument;
			var files = (string[])args[0];
			var output = (string)args[1];

			this.taskIsRunning = true;

			var options = new ParallelOptions { CancellationToken = this.cancellationTokenSource.Token };

			Parallel.ForEach(
				files, 
				(currentFile, loopState) =>
					{
						if (options.CancellationToken.IsCancellationRequested)
						{
							this.converterWorker.ReportProgress(0, "Cancelled.");
							loopState.Break();
						}

						string result;
						BCSTMConverter.Run(currentFile, output, out result);
						this.converterWorker.ReportProgress(0, result);
					});
		}

		private void uiWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (this.taskIsRunning)
			{
				string result = (string)e.UserState;
				this.Log(result);
				this.UpdateProgressBar();
				this.IncrementNumberOfFilesDone();
			}
		}

		private void BCSTMToWavForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (this.taskIsRunning)
			{
				var msgBox = MessageBox.Show(
					"Files are currently converting. \r\nDo you want to close the program?", 
					"Alert", 
					MessageBoxButtons.OKCancel);

				if (msgBox == DialogResult.OK)
				{
					this.cancellationTokenSource.Cancel();
				}
				else
				{
					e.Cancel = true;
				}
			}
		}

		private void converterWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			this.progressBar.Value = 0;
			TaskbarManager.Instance.SetProgressValue(0, 100);
			this.taskIsRunning = false;
		}
	}
}
namespace BCSTM_to_Wav_Converter_GUI
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Forms;
	using System.Windows.Shell;

	using Microsoft.WindowsAPICodePack.Taskbar;

	public partial class BCSTMToWavForm : Form
	{
		private BCSTMConverter converter;

		private int currentFileCount;

		private int totalFileCount;

		private CancellationTokenSource cancellationTokenSource;

		public BCSTMToWavForm()
		{
			this.InitializeComponent();
		}

		private void addButton_Click(object sender, EventArgs e)
		{
			var files = new OpenFileDialog { Multiselect = true };
			var result = files.ShowDialog();

			if (result == DialogResult.OK)
			{
				var selected = files.FileNames;

				var filesBoxHasItems = this.filesBox.Items.Count > 0;
				const int MaxPathLength = 260;
				if (selected.Any(a => a.Length > MaxPathLength))
				{
					Log($"The following paths have a length larger than {MaxPathLength} characters and will not be added:");
					Log(string.Join(Environment.NewLine, selected.Where(a => a.Length > MaxPathLength).ToArray()));
				}

				this.filesBox.Items.AddRange(selected.Where(a => a.EndsWith(".bcstm") && a.Length < MaxPathLength).ToArray());

				if (!filesBoxHasItems)
				{
					this.outputTextbox.Text = Path.GetDirectoryName(this.filesBox.Items[0].ToString()) + @"\Converted\";
				}
			}
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
			var files = this.filesBox.Items.OfType<string>().ToArray();
			var output = this.outputTextbox.Text;

			this.InitializeUI(files.Length);

			this.converter = new BCSTMConverter(files, output, this);

			TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);

			this.cancellationTokenSource = new CancellationTokenSource();
			Task.Factory.StartNew(() => this.converter.Run(this.cancellationTokenSource.Token), this.cancellationTokenSource.Token);
			this.cancellationTokenSource.Token.Register(() => MessageBox.Show("Operation cancelled."));
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
			var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

			var filesBoxHasItems = this.filesBox.Items.Count > 0;

			this.filesBox.Items.AddRange(files);

			if (!filesBoxHasItems)
			{
				this.outputTextbox.Text = Path.GetDirectoryName(this.filesBox.Items[0].ToString()) + @"\Converted\";
			}
		}

		public void UpdateProgressBar()
		{
			this.progressBar.PerformStep();
			TaskbarManager.Instance.SetProgressValue(this.progressBar.Value, this.progressBar.Maximum);
		}
		public void Log(string message)
		{
			this.logTextBox.AppendText(message + Environment.NewLine);
			this.logTextBox.ScrollToCaret();
		}

		public void IncrementNumberOfFilesDone()
		{
			this.currentFileCount++;
			this.currentFileStatusLabel.Text = $"{this.currentFileCount}/{this.totalFileCount}";
		}

		private void stopButton_Click(object sender, EventArgs e)
		{
			this.cancellationTokenSource.Cancel();
		}
	}
}
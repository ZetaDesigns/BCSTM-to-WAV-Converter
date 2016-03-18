namespace BCSTM_to_Wav_Converter_GUI
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Forms;

	public partial class BCSTMToWavForm : Form
	{
		private BCSTMConverter converter;

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
				this.filesBox.Items.AddRange(selected);

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

			this.progressBar.Value = 0;
			this.progressBar.Maximum = this.filesBox.Items.Count;

			this.converter = new BCSTMConverter(files, output, this);

			var task = new Task(() => this.converter.Run());
			task.Start();
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
			try
			{
				this.progressBar.PerformStep();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public void Log(string message)
		{
			this.logTextBox.AppendText(message);
			this.logTextBox.ScrollToCaret();
		}
	}
}
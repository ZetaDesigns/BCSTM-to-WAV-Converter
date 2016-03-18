namespace BCSTM_to_Wav_Converter_GUI
{
	using System;
	using System.Windows.Forms;

	public static class MainForm
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new BCSTMToWavForm());
		}
	}
}

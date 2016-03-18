using System;
using System.Windows.Forms;

namespace BCSTM_to_Wav_Converter_GUI
{
	public static class MainForm
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new BCSTMToWavForm());
		}
	}
}

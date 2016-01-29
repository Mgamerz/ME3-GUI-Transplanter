using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using static TransplanterLib.TransplanterLib;


namespace Transplanter_GUI
{
    public partial class TransplanterGUI : Form
    {
        private BackgroundWorker transplantWorker;

        public TransplanterGUI()
        {
            InitializeComponent();
            srcTextField.Text = @"C:\Users\Michael\Desktop\Transplant\Source\MPLobby.pcc";
            destTextField.Text = @"C:\Users\Michael\Desktop\Transplant\Destination\MPLobby.pcc";
            if (File.Exists(@"C:\Users\Michael\Desktop\Transplant\Destination\MPLobby.pcc.bak") && File.Exists(@"C:\Users\Michael\Desktop\Transplant\Destination\MPLobby.pcc"))
            {
                File.Delete(@"C:\Users\Michael\Desktop\Transplant\Destination\MPLobby.pcc");
            }

            if (File.Exists(@"C:\Users\Michael\Desktop\Transplant\Destination\MPLobby.pcc.bak") && !File.Exists(@"C:\Users\Michael\Desktop\Transplant\Destination\MPLobby.pcc"))
            {
                File.Move(@"C:\Users\Michael\Desktop\Transplant\Destination\MPLobby.pcc.bak", @"C:\Users\Michael\Desktop\Transplant\Destination\MPLobby.pcc");
            }

        }

        private void srcfileBrowseButton_Click(object sender, EventArgs e)
        {
            DialogResult result = srcFileChooser.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                srcTextField.Text = srcFileChooser.FileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = destFileChooser.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                destTextField.Text = destFileChooser.FileName;
            }
        }

        private void transplantButton_Click(object sender, EventArgs e)
        {
            if (srcTextField.Text.ToLower() == destTextField.Text.ToLower())
            {
                MessageBox.Show("Input and Destination files cannot be the same.", "Transplant Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(srcTextField.Text))
            {
                MessageBox.Show("Source file does not exist.", "Transplant Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!File.Exists(destTextField.Text))
            {
                MessageBox.Show("Destination file does not exist.", "Transplant Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            transplantButton.Enabled = false;

            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
            statusLabel.Text = "Extracting GFX Files";
            transplantWorker = new BackgroundWorker();
            transplantWorker.DoWork += performTransplant;
            transplantWorker.RunWorkerCompleted += transplantFinished;
            transplantWorker.WorkerReportsProgress = true;
            transplantWorker.ProgressChanged += new ProgressChangedEventHandler(transplantWorker_progressChanged);
            transplantWorker.RunWorkerAsync(new string[2] { srcTextField.Text, destTextField.Text });

        }

        private void transplantWorker_progressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            if (e.UserState != null)
            {
                if (e.UserState as String != "MARQUEE")
                {
                    Console.WriteLine(e.UserState as String);
                    progressBar1.Style = ProgressBarStyle.Continuous;
                    statusLabel.Text = e.UserState as string;
                }
                else
                {
                    progressBar1.Style = ProgressBarStyle.Marquee;
                }
            }
        }

        private void performTransplant(object sender, DoWorkEventArgs e)
        {
            string gfxfolder = AppDomain.CurrentDomain.BaseDirectory + @"extractedgfx\";
            string[] arguments = (string[])e.Argument; // 0 = src, 1 = dest
            transplantWorker.ReportProgress(0, "Extracting GFX Files");
            extractAllGFxMovies(arguments[0], gfxfolder, transplantWorker);
            transplantWorker.ReportProgress(0, "Replacing GFX Files");
            replaceSWFs(gfxfolder, arguments[1], transplantWorker);
        }


        private void transplantFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Value = 100;
            statusLabel.Text = "Transplant completed";
            transplantButton.Enabled = true;

        }
    }
}

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
            //srcTextField.Text = @"C:\Users\bdadmin\Desktop\Transplant\Source\BioP_MP_Common.pcc";
            //destTextField.Text = @"C:\Users\bdadmin\Desktop\Transplant\Destination\BioP_MP_Common.pcc";
            //if (File.Exists(@"C:\Users\bdadmin\Desktop\Transplant\Destination\BioP_MP_Common.pcc.bak") && File.Exists(@"C:\Users\bdadmin\Desktop\Transplant\Destination\BioP_MP_Common.pcc"))
            //{
            //    File.Delete(@"C:\Users\bdadmin\Desktop\Transplant\Destination\BioP_MP_Common.pcc");
            //}
            
            //if (File.Exists(@"C:\Users\bdadmin\Desktop\Transplant\Destination\BioP_MP_Common.pcc.bak") && !File.Exists(@"C:\Users\bdadmin\Desktop\Transplant\Destination\BioP_MP_Common.pcc"))
            //{
            //    File.Move(@"C:\Users\bdadmin\Desktop\Transplant\Destination\BioP_MP_Common.pcc.bak", @"C:\Users\bdadmin\Desktop\Transplant\Destination\BioP_MP_Common.pcc");
            //}

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

            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
            statusLabel.Text = "Extracting GFX Files";
            transplantWorker = new BackgroundWorker();
            transplantWorker.DoWork += performTransplant;
            transplantWorker.RunWorkerCompleted += unzipFinished;
            transplantWorker.WorkerReportsProgress = true;
            transplantWorker.ProgressChanged += new ProgressChangedEventHandler(transplantWorker_progressChanged);
            transplantWorker.RunWorkerAsync(new string[2] { srcTextField.Text, destTextField.Text });

        }

        private void transplantWorker_progressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            if (e.UserState != null)
            {
                statusLabel.Text = e.UserState as string;
            }
        }

        private void performTransplant(object sender, DoWorkEventArgs e)
        {
            string gfxfolder = AppDomain.CurrentDomain.BaseDirectory + @"extractedgfx\";
            string[] arguments = (string[])e.Argument; // 0 = src, 1 = dest
            transplantWorker.ReportProgress(0, "Extracting GFX Files");
            extractAllGFxMovies(arguments[0], null, gfxfolder, transplantWorker);
            transplantWorker.ReportProgress(0, "Replacing GFX Files");
            replaceSWFs(gfxfolder, arguments[1], transplantWorker);
        }


        private void unzipFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Value = 100;
            statusLabel.Text = "Transplant completed";
        }
    }
}

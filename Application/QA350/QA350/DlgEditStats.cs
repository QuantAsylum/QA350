using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QA350
{
    public partial class DlgEditStats : Form
    {
        public Settings AppSettings;

        private bool NumericIgnoreUpdates = false;

        public DlgEditStats()
        {
            InitializeComponent();
        }

        public DlgEditStats(Settings settings) : this()
        {
            AppSettings = settings;
        }

        private void DlgEditStats_Load(object sender, EventArgs e)
        {
            if (AppSettings.YAxisIsPPM)
                radioButton1.Checked = true;
            if (AppSettings.YAxisIsMV)
                radioButton2.Checked = true;
            if (AppSettings.YAxisIsUV)
                radioButton3.Checked = true;

            if (AppSettings.HistoBinIsMV)
                radioButton5.Checked = true;
            if (AppSettings.HistoBinIsUV)
                radioButton4.Checked = true;

            NumericIgnoreUpdates = true;
            numericUpDown1.Value = Convert.ToDecimal(AppSettings.YAxisPPMperDiv);
            numericUpDown2.Value = Convert.ToDecimal(AppSettings.YAxisMVperDiv);
            numericUpDown3.Value = Convert.ToDecimal(AppSettings.YAxisUVPerDiv);

            numericUpDown4.Value = Convert.ToDecimal(AppSettings.HistoBinInUV);
            numericUpDown5.Value = Convert.ToDecimal(AppSettings.HistoBinInMV);

            numericUpDown6.Value = Convert.ToDecimal(AppSettings.SampleHistory);
            NumericIgnoreUpdates = false;
        }

        // OK button
        private void button4_Click(object sender, EventArgs e)
        {
            AppSettings.YAxisIsPPM = false;
            AppSettings.YAxisIsMV = false;
            AppSettings.YAxisIsUV = false;
            AppSettings.HistoBinIsMV = false;
            AppSettings.HistoBinIsUV = false;

            if (radioButton1.Checked)
                AppSettings.YAxisIsPPM = true;
            if (radioButton2.Checked)
                AppSettings.YAxisIsMV = true;
            if (radioButton3.Checked)
                AppSettings.YAxisIsUV = true;
            if (radioButton4.Checked)
                AppSettings.HistoBinIsUV = true;
            if (radioButton5.Checked)
                AppSettings.HistoBinIsMV = true;

            AppSettings.YAxisPPMperDiv = Convert.ToInt32(numericUpDown1.Value);
            AppSettings.YAxisMVperDiv = Convert.ToInt32(numericUpDown2.Value);
            AppSettings.YAxisUVPerDiv = Convert.ToInt32(numericUpDown3.Value);

            AppSettings.HistoBinInUV = Convert.ToInt32(numericUpDown4.Value);
            AppSettings.HistoBinInMV = Convert.ToInt32(numericUpDown5.Value);

            AppSettings.SampleHistory = Convert.ToInt32(numericUpDown6.Value);

            Close();
        }

        // Set to 10 PPM/div button
        private void button1_Click(object sender, EventArgs e)
        {
            AppSettings.YAxisIsPPM = true;
            AppSettings.YAxisIsMV = false;
            AppSettings.YAxisIsUV = false;

            AppSettings.YAxisPPMperDiv = 10;

            radioButton1.Checked = true;
            numericUpDown1.Value = Convert.ToDecimal(10);
        }

        // Set to 10 uV/div
        private void button2_Click(object sender, EventArgs e)
        {
            AppSettings.YAxisIsPPM = false;
            AppSettings.YAxisIsMV = false;
            AppSettings.YAxisIsUV = true;

            AppSettings.YAxisUVPerDiv = 10;

            radioButton3.Checked = true;
            numericUpDown3.Value = Convert.ToDecimal(10);

        }

        // Set histo to 10uV/bin
        private void button3_Click(object sender, EventArgs e)
        {
            AppSettings.HistoBinIsMV = false;
            AppSettings.HistoBinIsUV = true;

            AppSettings.YAxisUVPerDiv = 10;

            radioButton4.Checked = true;
            numericUpDown4.Value = Convert.ToDecimal(10);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (NumericIgnoreUpdates == false)
                radioButton1.Checked = true;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            if (NumericIgnoreUpdates == false)
                radioButton2.Checked = true;
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            if (NumericIgnoreUpdates == false)
                radioButton3.Checked = true;
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            if (NumericIgnoreUpdates == false)
                radioButton5.Checked = true;
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            if (NumericIgnoreUpdates == false)
                radioButton4.Checked = true;
        }
    }
}

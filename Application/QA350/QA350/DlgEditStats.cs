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
        List<double> MathVals = new List<double>() { 1000, 100, 10, 1, 0.1, 0.01, 0.001 };

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
            switch (AppSettings.YAxisSetting)
            {
                case YAxisSettings.Ppm:
                    radioButton1.Checked = true;
                    break;
                case YAxisSettings.Mv:
                    radioButton2.Checked = true;
                    break;
                case YAxisSettings.Uv:
                    radioButton3.Checked = true;
                    break;
                default:
                    throw new NotImplementedException("DlgEditStats_Load()");
            }

            switch (AppSettings.HistoBinSetting)
            {
                case HistoBinSettings.Mv:
                    radioButton5.Checked = true;
                    break;
                case HistoBinSettings.Uv:
                    radioButton4.Checked = true;
                    break;
                default:
                    throw new NotImplementedException("DlgEditStats_Load()");
            }

            NumericIgnoreUpdates = true;
            numericUpDown1.Value = Convert.ToDecimal(AppSettings.YAxisPPMperDiv);
            numericUpDown2.Value = Convert.ToDecimal(AppSettings.YAxisMVperDiv);
            numericUpDown3.Value = Convert.ToDecimal(AppSettings.YAxisUVPerDiv);

            numericUpDown4.Value = Convert.ToDecimal(AppSettings.HistoBinInUV);
            numericUpDown5.Value = Convert.ToDecimal(AppSettings.HistoBinInMV);

            numericUpDown6.Value = Convert.ToDecimal(AppSettings.SampleHistory);
            NumericIgnoreUpdates = false;

            BindingSource bs = new BindingSource();
            bs.DataSource = MathVals;
            comboBox1.DataSource = bs;
            comboBox1.SelectedIndex = MathVals.FindIndex(o => o == AppSettings.Math);
            textBox1.Text = AppSettings.MathLabel;
        }

        // OK button
        private void button4_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                AppSettings.YAxisSetting = YAxisSettings.Ppm;
            }
            if (radioButton2.Checked)
            {
                AppSettings.YAxisSetting = YAxisSettings.Mv;
            }
            if (radioButton3.Checked)
            {
                AppSettings.YAxisSetting = YAxisSettings.Uv;
            }

            if (radioButton4.Checked)
                AppSettings.HistoBinSetting = HistoBinSettings.Uv;
            if (radioButton5.Checked)
                AppSettings.HistoBinSetting = HistoBinSettings.Mv;

            AppSettings.YAxisPPMperDiv = Convert.ToInt32(numericUpDown1.Value);
            AppSettings.YAxisMVperDiv = Convert.ToInt32(numericUpDown2.Value);
            AppSettings.YAxisUVPerDiv = Convert.ToInt32(numericUpDown3.Value);

            AppSettings.HistoBinInUV = Convert.ToInt32(numericUpDown4.Value);
            AppSettings.HistoBinInMV = Convert.ToInt32(numericUpDown5.Value);

            AppSettings.SampleHistory = Convert.ToInt32(numericUpDown6.Value);

            AppSettings.Math = MathVals[comboBox1.SelectedIndex];
            AppSettings.MathLabel = textBox1.Text;

            Close();
        }

        // Set to 10 PPM/div button
        private void button1_Click(object sender, EventArgs e)
        {
            AppSettings.YAxisSetting = YAxisSettings.Ppm;
            AppSettings.YAxisPPMperDiv = 10;

            radioButton1.Checked = true;
            numericUpDown1.Value = Convert.ToDecimal(10);
        }

        // Set to 10 uV/div
        private void button2_Click(object sender, EventArgs e)
        {
            AppSettings.YAxisSetting = YAxisSettings.Uv;

            AppSettings.YAxisUVPerDiv = 10;

            radioButton3.Checked = true;
            numericUpDown3.Value = Convert.ToDecimal(10);

        }

        // Set histo to 10uV/bin
        private void button3_Click(object sender, EventArgs e)
        {
            AppSettings.HistoBinSetting = HistoBinSettings.Uv;

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

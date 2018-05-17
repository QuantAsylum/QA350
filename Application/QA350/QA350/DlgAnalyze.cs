using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace QA350
{
    public partial class DlgAnalyze : Form
    {
        List<float> Data = new List<float>(32768);

        public DlgAnalyze()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Constants.DataFilePath;
            ofd.Filter = "Log Files|*.log";
            ofd.CheckFileExists = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                ProcessLog(ofd.FileName);
            }
        }

        private void Log(string s, params object[] obj)
        {
            s = string.Format(s + Environment.NewLine, obj);
            textBox1.AppendText(s);
        }

        void ProcessLog(string fileName)
        {
            int missedSamples = 0;
            float dt = 0;

            Data.Clear();

            using (BinaryReader br = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {

                if (br.ReadUInt32() != 0xCAFE8224)
                    return;

                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    float time = br.ReadSingle();
                    float value = br.ReadSingle();

                    if (time != 0 && dt == 0)
                        dt = time;

                    if (float.IsNaN(value))
                    {
                        ++missedSamples;
                        value = float.NaN;
                    }

                    Data.Add(value);
                }

                Log("Loaded {0} samples", Data.Count);
                Log("There were {0} missing samples [{1:0}%]", missedSamples, 100.0 * missedSamples / Data.Count);
                Log("Sample rate: {0:0.000}", 1 / dt);

                float interval, sd;
                ComputeWakeupStats(Data, dt, out interval, out sd);

            }
        }

        void ComputeWakeupStats(List<float> data, float dt, out float interval, out float sd)
        {
            List<float> wakeups = new List<float>();

            interval = 0;
            sd = 0;

            // State is unknown
            int isActive = -1;
            int lastIsActive = -1;
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i] > 0.01)
                {
                    isActive = 1;
                }
                else
                {
                    isActive = 0;
                }

                if ((lastIsActive == 0) && (isActive == 1))
                {
                    // Found a transition
                    wakeups.Add(i * dt);
                }

                lastIsActive = isActive;
            }

            Log("Found {0} wakeups", wakeups.Count);

            for (int i = 1; i < wakeups.Count; i++)
            {
                int startIndex = (int)(wakeups[i - 1] / dt);
                int stopIndex = (int)(wakeups[i] / dt);
                Log("Wakeup Interval {0}: {1:0.000} seconds   RMS: {2}", i, (wakeups[i] - wakeups[i - 1]), ComputeRms(data, startIndex, stopIndex));
            }

            if (wakeups.Count > 1)
            {
                int startIndex = (int)(wakeups.First() / dt);
                int stopIndex = (int)(wakeups.Last() / dt);
                Log("First to Last Wakup Span: {0:0.0000} seconds. OVerall RMS: {1}", (wakeups.Last() - wakeups.First()), ComputeRms(data, startIndex, stopIndex).ToEngineeringNotation());
            }


            double rms = ComputeRms(data, 0, data.Count);
            Log("RMS: {0:0.000}", rms.ToEngineeringNotation());
        }

        private double ComputeRms(List<float> data, int startIndex, int stopIndex)
        {
            float sum = 0;
            for (int i = startIndex; i < stopIndex; i++)
            {
                sum += data[i] * data[i];
            }

            float mean = sum / (stopIndex - startIndex);

            float rms = (float)Math.Sqrt(mean);

            return rms;
        }
    }
}

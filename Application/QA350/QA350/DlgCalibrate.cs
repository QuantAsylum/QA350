using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QA350
{
    public partial class DlgCalibrate : Form
    {
        public CalibrationClass calData = new CalibrationClass();
        public DlgCalibrate()
        {
            InitializeComponent();

            label2.Visible = false;
            button2.Visible = false;
            label3.Visible = false;
            button3.Visible = false;
            label4.Visible = false;
            button4.Visible = false;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        // Pressed when user wants to continue with calibration
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            label2.Visible = true;
            button2.Visible = true;
        }

        // Pressed when user wishes to perform 0 calibration with input shorted
        private void button2_Click(object sender, EventArgs e)
        {
           
            int readCount = 6;

            // Low gain. 
            Thread.Sleep(100);
            Form1.HW.SetAtten(0);
            Thread.Sleep(1000);

            List<int> reads = new List<int>();
            for (int i=0; i<readCount; i++)
            {
                int val  = Form1.HW.ReadVoltageCounts();
                reads.Add(val);
                Debug.WriteLine("Read Low Gain Shorted Input Value of :" + val);
                Thread.Sleep(500);
            }
            reads.RemoveAt(0);
            double avg = reads.Average();
            Debug.WriteLine("Read Low Gain Shorted Average :" + avg);
            calData.LowRangeCountsOffset = avg;

            // High gain
            reads.Clear();
            Thread.Sleep(100);
            Form1.HW.SetAtten(1);
            Thread.Sleep(1000);

            reads.Clear();
            for (int i = 0; i < readCount; i++)
            {
                int val = Form1.HW.ReadVoltageCounts();
                reads.Add(val);
                Debug.WriteLine("Read High Gain Shorted Input Value of :" + val);
                Thread.Sleep(500);
            }
            reads.RemoveAt(0);
            avg = reads.Average();
            Debug.WriteLine("Read High Gain Shorted Average :" + avg);
            calData.HiRangeCountsOffset = (int)Math.Round(avg);

            button2.Enabled = false;
            label3.Visible = true;
            button3.Visible = true;
        }

        // Pressed when user wants to perform to internal calibration
        private void button3_Click(object sender, EventArgs e)
        {
            int readCount = 6;


            // Low gain
            Thread.Sleep(100);
            Form1.HW.SetAtten(0);
            Thread.Sleep(1000);

            List<double> reads = new List<double>();

            for (int i = 0; i < readCount; i++)
            {
                int RawDataCounts = Form1.HW.ReadVoltageCounts();

                // Convert counts to voltage.  
                double v;
                v = Form1.ComputeUncalibratedVoltage(RawDataCounts - calData.LowRangeCountsOffset);
                //v = v * calData.LowRangeGain;
                reads.Add(v);
                Debug.WriteLine("Read Low Gain Voltage :" + v);
                Thread.Sleep(500);
            }
            reads.RemoveAt(0);
            double gain = 2.5 / reads.Average();
            calData.LowRangeGain = gain;
            Debug.WriteLine("Low Gain: " + gain);
            


            // Hi Gain
            Thread.Sleep(100);
            Form1.HW.SetAtten(1);
            Thread.Sleep(1000);

            reads.Clear();
            for (int i = 0; i < readCount; i++)
            {
                int RawDataCounts = Form1.HW.ReadVoltageCounts();

                // Convert counts to voltage.  
                double v;
                v = Form1.ComputeUncalibratedVoltage(RawDataCounts - calData.HiRangeCountsOffset);
                //v = v * calData.HiRangeGain;
                reads.Add(v);
                Debug.WriteLine("Read Hi Gain Voltage :" + v);
                Thread.Sleep(500);
            }
            reads.RemoveAt(0);

            gain = 2.5 / reads.Average();
            calData.HiRangeGain = gain;
            Debug.WriteLine("Hi Gain:" + gain);

            button3.Enabled = false;

            label4.Visible = true;
            button4.Visible = true;
            
        }

        // Cal done, OK to continue
        private void button4_Click(object sender, EventArgs e)
        {

        }
    }
}

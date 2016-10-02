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
    /// <summary>
    /// This is used to calibrate to an external reference with a voltage that might be higher than 2.5V
    /// </summary>
    public partial class DlgCalibrate2 : Form
    {
        public CalibrationClass calData = new CalibrationClass();
        public double ExtCalVoltage = -1;

        public DlgCalibrate2()
        {
            InitializeComponent();

            label2.Visible = false;
            button2.Visible = false;

            label3.Visible = false;
            button3.Visible = false;

            label4.Visible = false;
            button4a.Visible = false;
            button4b.Visible = false;

            label5.Visible = false;
            textBox5.Visible = false;
            button5.Visible = false;

            label6.Visible = false;
            button6.Visible = false;

            label7.Visible = false;
            button7.Visible = false;
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

        // Pressed when user wishes to perform 0 calibration with input shorted. At this point
        // assume the input is shorted. 
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
            calData.HiRangeCountsOffset = avg;

            button2.Enabled = false;
            label3.Visible = true;
            button3.Visible = true; 
        }

       

        double DoReadings(int readCount, double countsOffsets)
        {
            List<double> reads = new List<double>();

            for (int i = 0; i < readCount; i++)
            {
                int RawDataCounts = Form1.HW.ReadVoltageCounts();

                // Convert counts to voltage.  
                double v;
                v = Form1.ComputeUncalibratedVoltage(RawDataCounts - countsOffsets);
                reads.Add(v);
                Debug.WriteLine("Read Voltage :" + v);
                Thread.Sleep(500);
            }
            reads.RemoveAt(0);

            return reads.Average();
        }

        

        // Pressed once connected to 2.5V reference. 
        private void CalToInternalReference_Click(object sender, EventArgs e)
        {
            int readCount = 6;

            // We're connected to 2.5V reference. Take high span readings
            Thread.Sleep(100);
            Form1.HW.SetAtten(1);
            Thread.Sleep(1000);
            double hiGainAvg = DoReadings(readCount, calData.HiRangeCountsOffset);

            // Take low-span readings
            Thread.Sleep(100);
            Form1.HW.SetAtten(0);
            Thread.Sleep(1000);
            double loGainAv = DoReadings(readCount, calData.LowRangeCountsOffset);

            //double ratio = loGainAv / hiGainAvg;

            //double gain =  calData.HiRangeGain / ratio;
            calData.LowRangeGain = 2.5 / loGainAv;
            calData.HiRangeGain = 2.5 / hiGainAvg;
            Debug.WriteLine("Ratio:" + calData.HiRangeGain/calData.LowRangeGain);
            Debug.WriteLine("Low Gain Range:" + calData.LowRangeGain);
            Debug.WriteLine("Hi Gain Range:" + calData.HiRangeGain);

            button3.Enabled = false;
            label4.Visible = true;
            button4a.Visible = true;
            button4b.Visible = true;
        }

        // Clicked when user indicate he does NOT want to cal to voltage higher than 7V
        private void button4b_Click(object sender, EventArgs e)
        {
            label7.Visible = true;
            button7.Visible = true;
        }

        private void InputExternalReference(object sender, EventArgs e)
        {
            button4a.Enabled = false;
            button4b.Enabled = false;

            label5.Visible = true;
            textBox5.Visible = true;
            button5.Visible = true;
        }

        // Pressed when the user has indicated the calibration voltage
        private void ParseExtReferenceVoltage_Click(object sender, EventArgs e)
        {
            if (double.TryParse(textBox5.Text, out ExtCalVoltage))
            {
                button5.Enabled = false;
                label6.Visible = true;
                button6.Visible = true;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {

        }

        // Here, user wants to cal to higher external voltage
        private void ExtReferenceCal_Click(object sender, EventArgs e)
        {
            int readCount = 6;

            // Hi Gain
            Thread.Sleep(100);
            Form1.HW.SetAtten(1);
            Thread.Sleep(1000);

            // The ration of the HW divider has already been established. 
            double ratio = calData.HiRangeGain/calData.LowRangeGain;

            double avg = DoReadings(readCount, calData.HiRangeCountsOffset);
            double gain = ExtCalVoltage / avg;
            calData.HiRangeGain = gain;
            calData.LowRangeGain = gain / ratio;
            Debug.WriteLine("Hi Gain:" + calData.HiRangeGain);
            Debug.WriteLine("Lo Gain:" + calData.LowRangeGain);


            //// Low gain
            //Thread.Sleep(100);
            //Form1.HW.SetAtten(0);
            //Thread.Sleep(1000);
            //double avg = DoReadings(readCount);



            //double gain = 2.5 / reads.Average();
            //calData.LowRangeGain = gain;
            //Debug.WriteLine("Low Gain: " + gain);

            button6.Enabled = false;
            label7.Visible = true;
            button7.Visible = true;

        }

        // Cal done, OK to continue
        private void button4_Click(object sender, EventArgs e)
        {
            // Indicate the cal data was arrived at via actual cal
            calData.IsDefault = false;
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

       

       

      

       
    }
}

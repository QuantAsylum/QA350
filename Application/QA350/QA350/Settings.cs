using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA350
{
    /// <summary>
    /// Class to hold vars we'd like to persist
    /// </summary>
    public class Settings
    {
        // Y Axis Settings V versus T graph
        internal bool YAxisIsPPM = true;
        public int YAxisPPMperDiv = 10;
        public bool YAxisIsMV = false;
        public int YAxisMVperDiv = 1;
        public bool YAxisIsUV = false;
        public int YAxisUVPerDiv = 10;


        // Bin Size Histogram
        public int BinCount = 100;
        public bool HistoBinIsMV = false;
        public int HistoBinInMV = 1;
        public bool HistoBinIsUV = true;
        public int HistoBinInUV = 10;

        // Time
        public int SampleHistory = 101;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace QA350
{
    /// <summary>
    /// Holds the calibration vars for the device. BUGBUG: These could be tightened up in the future. Right now, 
    /// they are pretty far off.
    /// </summary>
    public class CalibrationClass
    {
        public double LowRangeCountsOffset = 0;
        public double LowRangeGain = 1.9;
        public double HiRangeCountsOffset = 0;
        public double HiRangeGain = 25.0;

        /// <summary>
        /// If true, then this means the calibration values are the defaults and the device hasn't been calibrated
        /// </summary>
        public bool IsDefault = true;
    }
}

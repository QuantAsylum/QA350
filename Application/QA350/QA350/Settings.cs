using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA350
{
    public enum YAxisSettings {Ppm, Mv, Uv };
    public enum HistoBinSettings { Mv, Uv }
    /// <summary>
    /// Class to hold vars we'd like to persist
    /// </summary>
    public class Settings
    {
        // Y Axis Settings V versus T graph
        public YAxisSettings YAxisSetting = YAxisSettings.Ppm;
        public int YAxisPPMperDiv = 10;
        public int YAxisMVperDiv = 1;
        public int YAxisUVPerDiv = 10;

        // Bin Size Histogram
        public HistoBinSettings HistoBinSetting;
        public int BinCount = 100;
        public int HistoBinInMV = 1;
        public int HistoBinInUV = 10;

        // Time
        public int SampleHistory = 100;

        // Math
        public double Math = 1;
        public string MathLabel = "";

        public int LoggingIntervalSec = 10;
    }
}

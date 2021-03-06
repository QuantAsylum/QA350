﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA350
{
    class Constants
    {
        static public string DataFilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\QuantAsylum\QA350\";
        static public string CalibrationFile = DataFilePath + "CalibrationData.xml";
        static public string SettingsFile = DataFilePath + "QA350Settings.xml";

        static public double Version = 1.772;

        static public Int32 RequiredFwVersion = 12;
    }
}

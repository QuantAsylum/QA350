using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace QA350
{
    public class CalibrationClass
    {
        public double LowRangeCountsOffset = 0;
        public double LowRangeGain = 1.9;
        public double HiRangeCountsOffset = 0;
        public double HiRangeGain = 25.0;
        public bool IsDefault = true;

        //public string Serialize()
        //{
        //    XmlSerializer serializer = new XmlSerializer(this.GetType());
        //    using (StringWriter stream = new StringWriter())
        //    {
        //        serializer.Serialize(stream, this);
        //        stream.Flush();
        //        string s = stream.ToString();
        //        return s;
        //    }
        //}

        //public static CalibrationClass Deserialize(string xml)
        //{
        //    if (string.IsNullOrEmpty(xml))
        //    {
        //        throw new ArgumentNullException("xml deserialization failed in CalibrationClass");
        //    }

        //    XmlSerializer serializer = new XmlSerializer(typeof(CalibrationClass));
        //    using (StreamReader stream = new StreamReader(xml))
        //    {
        //        try
        //        {
        //            return (CalibrationClass)serializer.Deserialize(stream);
        //        }
        //        catch (Exception ex)
        //        {
        //            // The serialization error messages are cryptic at best.
        //            // Give a hint at what happened
        //            throw new InvalidOperationException("Failed to create object from xml string in CalibrationClass", ex);
        //        }
        //    }
        //}
    }
}

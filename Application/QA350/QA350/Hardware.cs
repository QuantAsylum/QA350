using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA350
{
    class Hardware
    {
        HidDevice Msp430;

        public bool Open()
        {
            Msp430 = HidDevices.Enumerate(0x2047, 0x0301).FirstOrDefault();

            if (Msp430 != null)
            {
                Msp430.OpenDevice();

                //Msp430.Inserted += Msp430_Inserted;
                //Msp430.Removed += Msp430_Removed;
                //label1.Text = "Opened";
                //StartTime = DateTime.Now;

                //LineItem line = new LineItem("", GraphData, Color.LimeGreen, SymbolType.None);
                //zedGraphControl1.GraphPane.CurveList.Add(line);
                return true;
            }
            else
            {
                //label1.Text = "Open failed";
                return false;
            }
        }

        public void Close()
        {
            if ((Msp430 != null) && (Msp430.IsConnected))
            {
                Msp430.CloseDevice();
            }
        }

        /// <summary>
        /// Reads the voltage counts at the inputs indicated by the last SetAtten() call. These are raw
        /// reads, no corrections are applied
        /// </summary>
        /// <returns></returns>
        public int ReadVoltageCounts()
        {
            if ((Msp430 != null) && (Msp430.IsConnected))
            {
                // Read raw voltage
                if (USBSendData(new byte[] { 0x01, 0x00 }))
                {
                    byte[] buffer;

                    if (USBRecvData(out buffer, 32))
                    {
                        if ((buffer[2] & 0x80) > 0)
                        {
                            // Sign extend
                            buffer[3] = 0xff;
                        }
                        else
                        {
                            buffer[3] = 0;
                        }

                        Int32 data = (buffer[3] << 24) + (buffer[2] << 16) + (buffer[1] << 8) + buffer[0];
                        return data;
                    }
                }
            }

            return 0;
        }

        public void SetAtten(int atten)
        {
            if ((Msp430 != null) && (Msp430.IsConnected))
            {
                USBSendData(new byte[] { 0x03, (byte)atten });
            }
        }

        public void KickLED()
        {
            if ((Msp430 != null) && (Msp430.IsConnected))
            {
                USBSendData(new byte[] { 0x00, 0x00 });
            }
        }

        //public void SetPGA(int pga)
        //{
        //    if ((Msp430 != null) && (Msp430.IsConnected))
        //    {
        //        USBSendData(new byte[] { 0x02, (byte)pga });
        //    }
        //}

        bool USBSendData(byte[] data)
        {
            if (data.Length > 62)
                throw new FormatException("Data cannot be longer than 62 bytes in length");

            byte[] array = new byte[64];
            array[0] = 0x3f;
            array[1] = (byte)data.Length;
           
            Array.Copy(data, 0, array, 2, data.Length);

            return Msp430.Write(array, 50);
        }

        bool USBRecvData(out byte[] buffer, byte len)
        {
            buffer = new byte[64];
            HidDeviceData hdd = Msp430.Read();

            if (hdd != null)
            {
                Array.Copy(hdd.Data, 2, buffer, 0, 62);
                return true;
            }

            else
                return false;
        }
    }
}

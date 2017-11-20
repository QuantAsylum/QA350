using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QA350
{
    /// <summary>
    /// Class encapsulates hardware functionality of the QA350
    /// </summary>
    static class Hardware  
    {
        /// <summary>
        /// Instance of the HID library
        /// </summary>
        static HidDevice Msp430;

        static int timeout = 50;

        static public bool IsConnected = false;

        /// <summary>
        /// Attempt to open the USB connection to the QA350. If already
        /// opened, returns true
        /// </summary>
        /// <returns></returns>
        static public bool Open()
        {
            // See if we can see the USB ID of this product
            Msp430 = HidDevices.Enumerate(0x2047, 0x0301).FirstOrDefault();

            timeout = 50;

            if (Msp430 != null)
            {
                // Device ID is connected. Open it.
                Msp430.OpenDevice();
                IsConnected = true;
                return true;
            }

            // Device ID wasn't connected. No need to try and open
            return false;
        }

        /// <summary>
        /// Opens a connection to the device for re-flashing. Prior to calling this
        /// method, the MSP430 needs to have entered its bootlaoder. Note below
        /// that the product ID differs when in bootloader mode
        /// </summary>
        /// <returns></returns>
        static public bool OpenBSL()
        {
            // See if we can see the USB ID of this product
            Msp430 = HidDevices.Enumerate(0x2047, 0x200).FirstOrDefault();
            timeout = 8000;

            if (Msp430 != null)
            {
                // Device ID is connected. Open it.
                Msp430.OpenDevice();
                IsConnected = true;
                return true;
            }

            // Device ID wasn't connected. No need to try and open
            return false;
        }

        /// <summary>
        /// Close the connection to the device
        /// </summary>
        static public void Close()
        {
            if ((Msp430 != null) && (Msp430.IsConnected))
            {
                Msp430.CloseDevice();
                IsConnected = false;
            }
        }

        static public int GetFirmwareVersion()
        {
            if ((Msp430 != null) && (Msp430.IsConnected))
            {
                if (USBSendData(new byte[] { 0xFE, 0x00 }))
                {
                    byte[] buffer;

                    if (USBRecvData(out buffer))
                    {
                        return buffer[0];
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Reads the voltage counts at the inputs indicated by the last SetAtten() call. These are raw
        /// reads, no corrections are applied
        /// </summary>
        /// <returns></returns>
        static public int ReadVoltageCounts()
        {
            if ((Msp430 != null) && (Msp430.IsConnected))
            {
                // Read raw voltage by sending a two byte command. This will be received on the MSP430 as a
                // 0x0001 command (read ADC)
                if (USBSendData(new byte[] { 0x01, 0x00 }))
                {
                    byte[] buffer;

                    if (USBRecvData(out buffer))
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

            IsConnected = false;

            return 0;
        }

        /// <summary>
        /// Sets the attenuator on the device. 
        /// </summary>
        /// <param name="atten"></param>
        static public void SetAtten(int atten)
        {
            if ((Msp430 != null) && (Msp430.IsConnected))
            {
                USBSendData(new byte[] { 0x03, (byte)atten });
            }
        }

        /// <summary>
        /// Tells the device to keep the 'LINK' LED active for another 1000 mS or so
        /// </summary>
        static public void KickLED()
        {
            if ((Msp430 != null) && (Msp430.IsConnected))
            {
                USBSendData(new byte[] { 0x00, 0x00 });
            }
        }

        /// <summary>
        /// Sends a string of bytes to the device. Due to the protocol used by TI, the data cannot be longer
        /// that 62 bytes. 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static public bool USBSendData(byte[] data)
        {
            if (data.Length > 62)
                throw new FormatException("Data cannot be longer than 62 bytes in length");

            // Create a new array to hold the data, as we're going to add a two-byte header
            byte[] frame = new byte[data.Length + 2];

            // First byte of header
            frame[0] = 0x3f;
            // Length of payload
            frame[1] = (byte)data.Length;
           
            Array.Copy(data, 0, frame, 2, data.Length);

            bool retVal = Msp430.Write(frame, timeout);
            return retVal;

        }

        /// <summary>
        /// Receive data from USB device. The return value will always be a single 32-bit word, and the 
        /// data will only be sent if we request it via a command. There is no unsoliced data. 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        static public bool USBRecvData(out byte[] buffer)
        {
            buffer = new byte[0];

            HidDeviceData hdd = Msp430.Read(timeout);

            if (hdd != null)
            {
                buffer = new byte[hdd.Data[1]];
                Array.Copy(hdd.Data, 2, buffer, 0, hdd.Data[1]);
                return true;
            }

            else
                return false;
        }
    }
}

using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QA350
{
    class StreamSample
    {
        public byte SequenceId;
        public Int32 Value;
    }

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
            if (Msp430 != null)
            {
                Msp430.CloseDevice();
                IsConnected = false;
            }
        }

        static public int GetFirmwareVersion()
        {
            if (Msp430 != null) 
            {
                if (USBSendData(new byte[] { 0xFE, 0x00 }))
                {
                    byte[] buffer;

                    if (USBRecvData(out buffer))
                    {
                        return buffer[3];
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
            if (Msp430 != null)
            {
                // Read raw voltage by sending a two byte command. This will be received on the MSP430 as a
                // 0x0001 command (read ADC)
                if (USBSendData(new byte[] { 0x01, 0x00 }))
                {
                    byte[] buffer;

                    if (USBRecvData(out buffer))
                    {
                        // Data comes back as AA BB CC DD
                        // AA = sequence number
                        // BB = MSB of voltage reading
                        // CC
                        // DD = LSB of voltage reading
                        if ((buffer[1] & 0x80) > 0)
                        {
                            // Sign extend
                            buffer[0] = 0xff;   
                        }
                        else
                        {
                            buffer[0] = 0;
                        }

                        Int32 data = (buffer[0] << 24) + (buffer[1] << 16) + (buffer[2] << 8) + buffer[3];
                        return data;
                    }
                }
            }

            IsConnected = false;

            return 0;
        }

        static public int GetFifoDepth()
        {
            if (USBSendData(new byte[] { 0x05, 0x00 }))
            {
                byte[] buffer;

                if (USBRecvData(out buffer))
                {
                    Int32 data = (buffer[0] << 24) + (buffer[1] << 16) + (buffer[2] << 8) + buffer[3];
                    return data;
                }
            }

            return 0;
        }

        static public StreamSample[] ReadVoltageStream()
        {
            StreamSample[] samples = new StreamSample[12];

            if (Msp430 != null) 
            {
                if (USBSendData(new byte[] { 0x04, 0x00 }))
                {
                    byte[] wordBuf;

                    if (USBRecvData(out wordBuf))
                    {
                        for (int i=0; i<wordBuf.Length; i+=4)
                        {
                            StreamSample sample = new StreamSample();
                            sample.SequenceId = wordBuf[i + 0];

                            if ((wordBuf[i+1] & 0x80) > 0)
                            {
                                // Sign extend
                                wordBuf[i+0] = 0xff;
                            }
                            else
                            {
                                wordBuf[i+0] = 0;
                            }

                            Int32 data = (wordBuf[i+0] << 24) + (wordBuf[i+1] << 16) + (wordBuf[i+2] << 8) + wordBuf[i+3];
                            sample.Value = data;
                            samples[i >> 2] = sample;
                        }

                        return samples;
                    }
                }
            }

            IsConnected = false;

            return null;
        }

        /// <summary>
        /// Sets the attenuator on the device. 
        /// </summary>
        /// <param name="atten"></param>
        static public void SetAtten(int atten)
        {
            if (Msp430 != null) 
            {
                USBSendData(new byte[] { 0x03, (byte)atten });
            }
        }

        /// <summary>
        /// Tells the device to keep the 'LINK' LED active for another 1000 mS or so
        /// </summary>
        static public void KickLED()
        {
            if (Msp430 != null)
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

            bool retVal = Msp430.FastWrite(frame, timeout);
            return retVal;

        }

        /// <summary>
        /// Receive data from USB device. The return value will always be 48 bytes, and the 
        /// data will only be sent if we request it via a command. There is no unsoliced data. 
        /// First two bytes of HID buffer are TI's HID protcol:
        /// Byte 0: 0x3F
        ///      1: Length of user payload (0x30 = 48)
        ///      2: First byte of user data
        ///      49: Last byte of user data
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        static public bool USBRecvData(out byte[] buffer)
        {
            buffer = new byte[0];

            HidDeviceData hdd = Msp430.FastRead(timeout);   

            if (hdd != null)
            {
                buffer = new byte[hdd.Data[1]];
                Array.Copy(hdd.Data, 2, buffer, 0, hdd.Data[1]);
                return true;
            }

            else
                return false;
        }
        
        // BUGBUG: Not needed diagnostics only. Pull it out
        static public bool HidIsConnected()
        {
            return Msp430.IsConnected;
        }

       
    }
}

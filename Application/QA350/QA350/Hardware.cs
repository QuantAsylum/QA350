﻿using HidLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    enum SampleRate { Slow, Fast};

    enum Mode { DC = 0, RMS = 1 }



    /// <summary>
    /// Class encapsulates hardware functionality of the QA350
    /// </summary>
    static class Hardware  
    {

        public const int INVALID_VALUE = unchecked((int)0x80FFFFFF);

        /// <summary>
        /// Instance of the HID library
        /// </summary>
        static HidDevice Msp430;

        static int Timeout = 50;

        static public bool IsConnected = false;

        static object UsbLockObj = new object();

        static Mode Mode = Mode.DC;

        /// <summary>
        /// Attempt to open the USB connection to the QA350. If already
        /// opened, returns true
        /// </summary>
        /// <returns></returns>
        static public bool Open()
        {
            // See if we can see the USB ID of this product
            Msp430 = HidDevices.Enumerate(0x2047, 0x0301).FirstOrDefault();

            Timeout = 50;

            if (Msp430 != null)
            {
                // Device ID is connected. Open it.
                Msp430.OpenDevice();
                // Set to defaults (DC mode, slow sample rate, no atten, etc)
                Reset();
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
        static public bool OpenBSL(int secondsToTry = 15)
        {
            DateTime start = DateTime.Now;

            while (DateTime.Now.Subtract(start).TotalSeconds < secondsToTry)
            {
                // See if we can see the USB ID of this product
                Msp430 = HidDevices.Enumerate(0x2047, 0x200).FirstOrDefault();
                Timeout = 8000;

                if (Msp430 != null)
                {
                    // Device ID is connected. Open it.
                    Msp430.OpenDevice();
                    IsConnected = true;
                    Thread.Sleep(1000);
                    return true;
                }

                Thread.Sleep(1000);
                Debug.WriteLine("Looping in OpenBsl");
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

        // Sends a two byte message, and receives a 48 byte buffer in return
        static private int SendRecv(byte sendData)
        {
            lock (UsbLockObj)
            {
                if (Msp430 != null)
                {
                    if (USBSendData(new byte[] { sendData, 0x00 }))
                    {
                        byte[] buffer;

                        if (USBRecvData(out buffer))
                        {
                            int data = unchecked((int)((buffer[0] << 24) + (buffer[1] << 16) + (buffer[2] << 8) + buffer[3]));
                            return data;
                        }
                    }
                }
            }

            throw new Exception("USB Failure in Hardware.cs SendRecv");
        }

        static public int GetFirmwareVersion()
        {
            return SendRecv(0xFE);
        }

        static public int GetProductId()
        {
            return SendRecv(0xFD);
        }

        static public int GetMspTemp()
        {
            return SendRecv(51);
        }

        /// <summary>
        /// Reads the voltage counts at the inputs indicated by the last SetAtten() call. These are raw
        /// reads, no corrections are applied. 
        /// </summary>
        /// <returns></returns>
        static public int ReadVoltageCounts()
        {
            //return SendRecv(0x1);

            if (GetMode() != Mode.DC)
                throw new InvalidOperationException("Invalid mode operation in ReadVoltageCounts()");

            int rawVal = SendRecv(0x1);

            if (rawVal == INVALID_VALUE)
            {
                return INVALID_VALUE;
            }

            uint val = unchecked ((uint)rawVal);

            // Given a word AABBCCDD, the voltage is represented as a 24-bit value
            // BBCCDD. We need to see if is negative or not (based on BB value) and 
            // sign extend if it is
            if ( ((val >> 16) & 0xFF) > 0x80)
            {
                // Sign extend
                val |= 0xFF000000;
            }
            else
            {
                val &= 0x00FFFFFF;
            }

            Debug.WriteLine("Val: " + val.ToString() + " " + val.ToString("X"));
            return unchecked((int)val);
        }

        static public int GetFifoDepth()
        {
            return SendRecv(0x5);
        }

        static public StreamSample[] ReadVoltageStream()
        {
            StreamSample[] samples = new StreamSample[12];

            lock (UsbLockObj)
            {
                if (Msp430 != null)
                {
                    // Request stream of 48 bytes (12 samples)
                    if (USBSendData(new byte[] { 0x04, 0x00 }))
                    {
                        byte[] wordBuf;

                        if (USBRecvData(out wordBuf))
                        {
                            for (int i = 0; i < wordBuf.Length; i += 4)
                            {
                                StreamSample sample = new StreamSample();
                                sample.SequenceId = wordBuf[i + 0];

                                if ((wordBuf[i + 1] & 0x80) > 0)
                                {
                                    // Sign extend
                                    wordBuf[i + 0] = 0xff;
                                }
                                else
                                {
                                    wordBuf[i + 0] = 0;
                                }

                                Int32 data = (wordBuf[i + 0] << 24) + (wordBuf[i + 1] << 16) + (wordBuf[i + 2] << 8) + wordBuf[i + 3];
                                sample.Value = data;
                                samples[i >> 2] = sample;
                            }

                            return samples;
                        }
                    }
                }
            }

            IsConnected = false;

            return null;
        }

        /// <summary>
        /// Sets the attenuator on the device. 0 means no atten is active (low range), 
        /// 1 means atten is active (high range)
        /// </summary>
        /// <param name="atten"></param>
        static public void SetAtten(int atten)
        {
            if (Msp430 != null) 
            {
                USBSendData(new byte[] { 0x03, (byte)atten });
            }
        }

        static public void SetSampleRate(SampleRate sr)
        {
            if (Msp430 != null)
            {
                byte val = (sr == SampleRate.Slow ? (byte)0 : (byte)1);
                USBSendData(new byte[] { 0x06, val });
            }
        }

        static public void Reset()
        {
            if (Msp430 != null)
            {
                USBSendData(new byte[] { 251, 0 });
            }
        }

        static public void SetMode(Mode mode)
        {
            if (Msp430 != null)
            {
                USBSendData(new byte[] { 12, (byte)mode });
                Mode = mode;
            }
        }

        static public Mode GetMode()
        {
            return Mode;
        }

        static public void StartRmsConversion()
        {
            USBSendData(new byte[] { 13, 0 });
        }

        static public int ReadRmsCounts()
        {
            if (GetMode() != Mode.RMS)
            {
                throw new InvalidOperationException("Invalid mode operation in ReadRmsCounts()");
            }

            int rawVal = SendRecv(14);

            if (rawVal == INVALID_VALUE)
            {
                return INVALID_VALUE;
            }

            uint val = unchecked((uint)rawVal);

            // Given a word AABBCCDD, the voltage is represented as a 24-bit value
            // BBCCDD. We need to see if is negative or not (based on BB value) and 
            // sign extend if it is
            if (((val >> 16) & 0xFF) > 0x80)
            {
                // Sign extend
                val |= 0xFF000000;
            }
            else
            {
                val &= 0x00FFFFFF;
            }

            return unchecked((int)val);
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

        static public void EnterBSL()
        {
            // Force hardware to jump to BSL
            if (Msp430 != null)
                Hardware.USBSendData(new byte[] { 0xFF, 0x00 });
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

            bool retVal = Msp430.FastWrite(frame, Timeout);
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

            HidDeviceData hdd = Msp430.FastRead(Timeout);   

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

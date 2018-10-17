using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QA350
{
    

    static class Bootloader
    {
        public delegate void FlashStatusCallback(string s);

        // Note, there are two ways to enter the BSL on the MSP. The first is by having the firmware code jump to a specific 
        // vector and start executing. The second is if the ISR vectors are unitialized. 
        // The MSP430F5529 only has minimal BSL functionality, and thus a ram-based BSL must be downloaded. The minimal
        // BSL functionality only supports the commands RX PASSWORD, RX DATA BLOCK FAST (to ram addresses only) and SET PC

        // The process for flash update is as follows:
        // 1. Submit password of all FFs. This will work on factory fresh device. On already programmed device, this 
        //    will fail and cause the entire flash to be erased
        // 2. Once erased, then download RAM-based BSL and starts its execution
        // 3. Perform a mass erase. This will ensure partially programmed parts (from previous failed attempts) are good to go
        // 4. Program file. Note that ideally the reset vector (@0xFFFE-0xFFFF) should be the last thing programmed as this will ensure
        //    partial programming failures will still fall back to the BSL

        /// <summary>
        /// Loads external bootstrap file, verifies needed boot files are present, and updates
        /// internal flash image.
        /// </summary>
        static public void EnterBootloader(FlashStatusCallback statusUpdate)
        {
            const string bootstrapFile = "RAM_BSL.00.07.08.38.bsl";

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Boot Files|*.boot";
            ofd.Title = "Enter flash file";
            ofd.CheckFileExists = true;
            ofd.FileName = "QA350.boot";
            ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

            statusUpdate?.Invoke("Wait...");

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            // Make sure boottloader is valid
            if (VerifyValidBootloader(ofd.FileName) == false)
            {
                MessageBox.Show("This doesn't appear to be a valid bootloader file. Reflash will be aborted.");
                return;
            }

            // Make sure the bootloader boostrap exists
            if (File.Exists(bootstrapFile) == false)
            {
                MessageBox.Show("The bootstrap file is missing. Reflash will be aborted.");
                return;
            }

            if (VerifyValidBootstrap(bootstrapFile) == false)
            {
                MessageBox.Show("This doesn't appear to be a valid bootsrap file. Reflash will be aborted.");
                return;
            }

            // Tell the device to enter BSL if we're already connected
            if (Hardware.IsConnected && Hardware.USBSendData(new byte[] { 0xFF, 0x00 }))
            {
                Thread.Sleep(4000);
            }

            // Now reconnect with BL. Do not enter this section of code if 
            // everything you need isn't verified. Becuase failure inside here
            // probably means a bricked device.
            try
            {
                Hardware.OpenBSL();
                Thread.Sleep(1000);

                SubmitPassword();  // This might fail and perform mass erase
                SubmitPassword();  // This will always succeed and perform mass erase

                // Once we are mass-erased, then the bootloader will always run at start

                // Load ram-based BSL
                WriteFlash(bootstrapFile, statusUpdate, true);

                // Run ram-based BSL
                SetPC(0x2504);
                Thread.Sleep(4000);
                Hardware.OpenBSL();
                Thread.Sleep(1000);
                Debug.WriteLine("BSL Version: " + GetBSLVersion().ToString("X"));

                // Erase everything
                MassErase();
                
                // Write the new image
                WriteFlash(ofd.FileName, statusUpdate, false);

                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception during update: " + ex.Message);
                MessageBox.Show("An exception occured during reflash: " + ex.Message);
            }
        }

        static void SubmitPassword()
        {
            byte[] data = new byte[1 + 51];

            data[0] = 0x11;  // CMD = RX Password

            for (int i = 1; i < 1 + 51; i++)
            {
                data[i] = 0xFF;
            }

            // Send command to erase flash
            byte[] rxBuf;
            Hardware.USBSendData(data);
            Hardware.USBRecvData(out rxBuf);

            if (rxBuf[0] == 0x3B && rxBuf[1] == 0x0)
            {
                Debug.WriteLine("Password succeeded");
            }
            else
            {
                Debug.WriteLine("Password failed");
            }
        }

        static void SetPC(int pc)
        {
            Hardware.USBSendData(new byte[] { 0x17, (byte)(pc), (byte)(pc>>8), (byte)(pc >> 16) });
        }

        static int GetBSLVersion()
        {
            byte[] txBuf = new byte[1];

            txBuf[0] = 0x19;
            Hardware.USBSendData(txBuf);

            byte[] rxBuf;
            Hardware.USBRecvData(out rxBuf);

            if (rxBuf[0] == 0x3A)
            {
                return (rxBuf[1] << 24) + (rxBuf[2] << 16) + (rxBuf[3] << 8) + (rxBuf[4] << 0);
            }
            else
            {
                Debug.WriteLine("GetBSLVersion() failed");
                return 0;
            }
        }

        static void MassErase()
        {
            byte[] txBuf = new byte[1];

            txBuf[0] = 0x15;
            Hardware.USBSendData(txBuf);

            byte[] rxBuf;
            Hardware.USBRecvData(out rxBuf);

            if (rxBuf[0] == 0x3B && rxBuf[1] == 0x0)
            {
                Debug.WriteLine("Mass erase succeeded");
            }
            else
            {
                Debug.WriteLine("Mass erase failed");
            }

        }

        static bool VerifyValidBootstrap(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);

            if (lines[0] == "@2500" && lines[lines.Length - 1] == "q")
                return true;
            else
                return false;
        }

        static bool VerifyValidBootloader(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);

            if (lines[0] == "@4400" && lines[lines.Length-1] == "q")
                return true;
            else
                return false;
        }

        /// <summary>
        /// Parses the specified ram-based bootloader file (provided by TI) and sends
        /// it down to the MSP430, and then execute the ram-based file. This is needed
        /// because the resident bootloader in the MSP430F5529 doesn't have all the 
        /// functionality needed for a full-reflash.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fastWrite"></param>
        /// <returns></returns>
        static bool WriteFlash(string fileName, FlashStatusCallback statusUpdate, Boolean fastWrite)
        {
            byte[] lastArray = null;
            int lastAddress = 0;
            int resetAddr = 0;

            string[] lines = File.ReadAllLines(fileName);

            int address = 0;

            for (int i = 0; i < lines.Length; i++ )
            {
                string s = lines[i];

                // Check if we have an address
                if (s[0] == '@')
                {
                    string addr = s.Substring(1);
                    addr = addr.Trim();
                    address = Convert.ToInt32(addr, 16);
                    continue;
                }

                // Check if we're done
                if (char.ToUpper(s[0]) == 'Q')
                    break;

                s = s.Trim();
                string[] toks = s.Split(' ');

                byte[] data = new byte[toks.Length];
                for (int j=0; j<toks.Length; j++)
                {
                    data[j] = Convert.ToByte(toks[j], 16);
                }

                if (fastWrite)
                {
                    if (WriteBytesFast(address, data))
                    {
                        //Debug.WriteLine(string.Format("Fast Write Line: {0}  Address: 0x{1:X}", i, address));
                    }
                }
                else
                {
                    // Look to see if this blocks writes the reset vector. If it does, then save it for last
                    if ( (address <= 0xFFFE ) && (address + data.Length >= 0xFFFF) )
                    {
                        lastAddress = address;
                        lastArray = data;
                        Debug.WriteLine(string.Format("Reset vector found: 0x{0:X}", lastAddress));

                        resetAddr = (data[0xFFFF - address] << 8) + (data[0xFFFE - address]);
                    }
                    else
                    {
                        if (WriteBytes(address, data))
                        {
                            Debug.WriteLine(string.Format("Line: {0}  Address: 0x{1:X}  Len: {2}", i, address, data.Length));
                            if ( (i % 10) == 0)
                                statusUpdate?.Invoke(i.ToString() + " of " + lines.Length + " blocks");
                        }
                        else
                        {
                            Debug.WriteLine(string.Format("ERROR:    Line: {0}  Address: 0x{1:X}", i, address));
                        }
                    }
                }

                address += data.Length;
            }

            // Here we've written everything EXCEPT, possibly, the reset vector. 
            if (lastArray != null)
            {
                if (WriteBytes(lastAddress, lastArray))
                {
                    Debug.WriteLine(string.Format("Reset vector written. Address: 0x{0:X}  Len: {1}", lastAddress, lastArray.Length));
                    Thread.Sleep(100);
                    SetPC(resetAddr);
                    statusUpdate?.Invoke("Flash updated");
                }
                else
                    Debug.WriteLine("Failed to write reset vector");
            }

            return true;
        }

        static bool WriteBytesFast(int address, byte[] data)
        {
            const int maxLen = 48;
            if (data.Length > maxLen)
                throw new InvalidOperationException("Write cannot be longer than " + maxLen.ToString() + " bytes");

            byte[] txBuf = new byte[data.Length + 4];

            txBuf[0] = 0x1B;
            txBuf[1] = (byte)address;
            txBuf[2] = (byte)(address >> 8);
            txBuf[3] = (byte)(address >> 16);

            Array.Copy(data, 0, txBuf, 4, data.Length);
            Hardware.USBSendData(txBuf);

            return true;
        }

        static bool Verify(int address, byte[] data)
        {
            byte[] txBuf = new byte[6];

            txBuf[0] = 0x18;
            txBuf[1] = (byte)address;
            txBuf[2] = (byte)(address >> 8);
            txBuf[3] = (byte)(address >> 16);
            txBuf[4] = (byte)data.Length;
            txBuf[5] = (byte)(data.Length >> 8);
            Hardware.USBSendData(txBuf);

            byte[] recvBuf = new byte[0];
            if (Hardware.USBRecvData(out recvBuf))
            {
                for (int i=0; i<data.Length; i++)
                {
                    if (recvBuf[i+1] != data[i])
                        return false;
                }
            }

            return true;
        }
        

        static bool WriteBytes(int address, byte[] data)
        {
            const int maxLen = 48;
            if (data.Length > maxLen)
                throw new InvalidOperationException("Write cannot be longer than " + maxLen.ToString() + " bytes");

            byte[] txBuf = new byte[data.Length + 4];

            txBuf[0] = 0x10;
            txBuf[1] = (byte)address;
            txBuf[2] = (byte)(address >> 8);
            txBuf[3] = (byte)(address >> 16);

            Array.Copy(data, 0, txBuf, 4, data.Length);
            Hardware.USBSendData(txBuf);

            byte[] recvBuf = new byte[0];
            if (Hardware.USBRecvData(out recvBuf))
            {
                if (recvBuf[0] == 0x3B && recvBuf[1] == 0x00)
                {
                    Thread.Sleep(1);
                    return true;
                    //return Verify(address, data);
                }
                else
                {
                    Debug.WriteLine(string.Format("Write failed. Message0 0x{0:x}  Message1 0x{1:x}", recvBuf[0], recvBuf[1]));
                }


            }

            // Something went wrong. Need to provide a bit more info here
            return false;
        }
    }
}

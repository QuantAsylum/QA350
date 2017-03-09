﻿using System;
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
    /*
    static class Crc16
    {
        const ushort polynomial = 0xA001;
        static readonly ushort[] table = new ushort[256];

        public static ushort ComputeChecksum(byte[] bytes, int len)
        {
            ushort crc = 0;
            for (int i = 0; i < len; ++i)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }
            return crc;
        }

        static Crc16()
        {
            ushort value;
            ushort temp;
            for (ushort i = 0; i < table.Length; ++i)
            {
                value = 0;
                temp = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                    {
                        value = (ushort)((value >> 1) ^ polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                table[i] = value;
            }
        }
    }*/

    static class Bootloader
    {
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

        static public void EnterBootloader()
        {
            /*
            // 1 + 51 bytes
            byte[] data = new byte[1 + 51];

            data[0] = 0x11;  // CMD = RX Password

            for (int i=1; i<1+51; i++)
            {
                data[i] = 0xFF;
            }
            */

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text Files|*.txt";
            ofd.Title = "Enter flash file";
            ofd.CheckFileExists = true;

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            // Tell the device to enter BSL if we're already connected
            if (Hardware.IsConnected && Hardware.USBSendData(new byte[] { 0xFF, 0x00 }))
            {
                Thread.Sleep(4000);
            }


            // Now reconnect with BL
            try
            {
                Hardware.OpenBSL();
                Thread.Sleep(1000);

                SubmitPassword();  // This might fail and perform mass erase
                SubmitPassword();  // This will always succeed and perform mass erase

                // Load ram-based BSL
                WriteFlash("RAM_BSL.00.07.08.38.txt", true);

                // Run ram-based BSL
                SetPC(0x2504);
                Thread.Sleep(4000);
                Hardware.OpenBSL();
                Thread.Sleep(1000);
                Console.WriteLine("BSL Version: " + GetBSLVersion().ToString("X"));

                // Erase everything
                MassErase();
                
                // Write the new image
                WriteFlash(ofd.FileName, false);

                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception during update: " + ex.Message);
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

        static bool WriteFlash(string fileName, Boolean fastWrite)
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
                        //Console.WriteLine(string.Format("Fast Write Line: {0}  Address: 0x{1:X}", i, address));
                    }
                }
                else
                {
                    // Look to see if this blocks writes the reset vector. If it does, then save it for last
                    if ( (address <= 0xFFFE ) && (address + data.Length >= 0xFFFF) )
                    {
                        lastAddress = address;
                        lastArray = data;
                        Console.WriteLine(string.Format("Reset vector found: 0x{0:X}", lastAddress));

                        resetAddr = (data[0xFFFF - address] << 8) + (data[0xFFFE - address]);
                    }
                    else
                    {
                        if (WriteBytes(address, data))
                        {
                            Console.WriteLine(string.Format("Line: {0}  Address: 0x{1:X}  Len: {2}", i, address, data.Length));
                        }
                        else
                        {
                            Console.WriteLine(string.Format("ERROR:    Line: {0}  Address: 0x{1:X}", i, address));
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
                    Console.WriteLine(string.Format("Reset vector written. Address: 0x{0:X}  Len: {1}", lastAddress, lastArray.Length));
                    Thread.Sleep(100);
                    SetPC(resetAddr);
                }
                else
                    Console.WriteLine("Failed to write reset vector");
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
                    Console.WriteLine(string.Format("Write failed. Message0 0x{0:x}  Message1 0x{1:x}", recvBuf[0], recvBuf[1]));
                }


            }

            // Something went wrong. Need to provide a bit more info here
            return false;
        }
    }
}
# QA350

Source code from [QA350 Product](https://quantasylum.com/products/qa350-microvolt-dc-volt-meter). This product is licensed under the MIT license.

This repo contains the source for the both the PC application and the firmware for the QA350. The firmware is based on the MSP430F5529, and is built under Release 8 of Code Composer Studio. 

The QA350 provides high-resolution voltage measurements across 2 ranges: +/-5V and +/-50V. In the +/-5V range, the QA350 deliver uV resolution. In the +/-50V range, the QA350 provides 10 uV resolution. The QA350 is NOT for use on high-energy circuits or anything that is connected to the mains. It is designed for measuring very high resolution sensors and other low voltage applications where precision is required. 

As of release 1.70, the QA350 can support true RMS measurements, with a 3 dB bandwith of 4 KHz.

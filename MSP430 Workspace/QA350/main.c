/* --COPYRIGHT--,BSD
 * Copyright (c) 2014, Texas Instruments Incorporated
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * *  Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *
 * *  Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * *  Neither the name of Texas Instruments Incorporated nor the names of
 *    its contributors may be used to endorse or promote products derived
 *    from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * --/COPYRIGHT--*/
/*  
 * ======== main.c ========
 * H4 Example
 *
 * Packet Protocol Demo:
 *
 * This application emulates a simple packet protocol that receives packets 
 * like the following:
 * Establish a connection with the HID Demo App.  No text is initially 
 * displayed.  Send a single digit, a number between 1-9.  Then, send that 
 * number of bytes.  For example, send '3', and then send "abc".  The 
 * application responds by indicating it has received the packet, and waits for 
 * another.
 * ----------------------------------------------------------------------------+
 * Please refer to the Examples Guide for more details.
 *----------------------------------------------------------------------------*/
#include <string.h>
#include <math.h>

#include "driverlib.h"

#include "USB_config/descriptors.h"
#include "USB_API/USB_Common/device.h"
#include "USB_API/USB_Common/usb.h"                 // USB-specific functions
#include "USB_API/USB_HID_API/UsbHid.h"
#include "USB_app/usbConstructs.h"

#include "hal.h"

#include "fifo.h"

#define UART 		USCI_A1_BASE
#define TIMER 		TIMER_A0_BASE
#define ADC         ADC12_A_BASE
#define REF         REF_BASE
#define SPI 		USCI_A1_BASE



#define MODE_DC 	0
#define MODE_RMS 	1

#define SR_2P5  0
#define SR_1k   1
#define SR_30k  2

uint8_t DcSampleRate;

uint8_t Mode;

#define  RMS_SAMPLES 16384
int64_t  RmsAccum;
uint16_t RmsSample;
uint8_t  RmsBusy;


// Forward Declarations
uint32_t GetSysTime();
void DelayUS(uint16_t delayUS);
void Delay(uint32_t delayMS);
void ProcessUsbData();
void SetADS1256SampleRate(uint8_t);
void ResetQA350();

// Global flags set by events
//volatile uint8_t bCommandBeingProcessed = FALSE;
volatile uint8_t bDataReceiveCompleted_event = FALSE;  // data receive completed event
volatile uint8_t bSampleReady_event = FALSE;

// Application globals
uint16_t x,y;

// USB return buffer is 48 bytes. It can be a little larger, but
// the HID protocol TI uses for a pipe has some overhead itself.
// An ADC word is 24 bits, and the upper 8 bits are used for housekeeping
// so this means we can hold 12 32-bit samples per HID read
#define USB_BUF_LEN 48
uint8_t UsbBuffer[USB_BUF_LEN];

// This is updated at 1mS rate. This keeps track of the number of milliseconds elapsed since
// system boot
volatile uint32_t SysTicks;

volatile uint16_t LEDConnected;  // Kicked by PC application at least once per second to keep LED lit
volatile uint8_t LEDCommand;     // Toggled for 50 mS upon receipt of command from the PC

int8_t IntRefCount;

void EIntRefCounted()
{
	if (IntRefCount > 0)
		--IntRefCount;

	if (IntRefCount == 0)
		__enable_interrupt();
}

void DIntRefCounted()
{
	if (IntRefCount < 126)
		++IntRefCount;

	if (IntRefCount == 1)
		__disable_interrupt();
}

void InitClocks()
{
    // Set system freqs as follows:
    // DCOCLK: 24 MHz
    // MCLK (Master Clock): 24 MHz
    // SMCLCK (Subsystem Master Clock): 24 MHz
    // ACLK (Aux Clock): 24 MHz

    // For clock settings below, see Fig 5.1 in http://www.ti.com/lit/ug/slau208n/slau208n.pdf
    // FLL reference clock is REFOCLK

    // Good link on setting MSP430F5529 clocks here: http://mostlyanalog.blogspot.com/2015/04/clocking-msp430f5529-launchpad.html

    // XT2 IO pins to drive the xtal
	GPIO_setAsPeripheralModuleFunctionInputPin(GPIO_PORT_P5, GPIO_PIN2);
	GPIO_setAsPeripheralModuleFunctionOutputPin(GPIO_PORT_P5, GPIO_PIN3);

	// Turn on xtal drive
	UCS_XT2Start(UCS_XT2DRIVE_4MHZ_8MHZ);

	// XT1 defaults to 32KHz, and we're using 4 MHz resonator for XT2. Note, though
	// we don't have an xtal in place for 32 KHz
	UCS_setExternalClockSource(32768, 4000000);

	// Into FLL with 1 MHz reference
	UCS_clockSignalInit(UCS_FLLREF, UCS_XT2CLK_SELECT, UCS_CLOCK_DIVIDER_4 );
	// Out of FLL with 24X multiplier to 24 MHz. Note we specify KHz in first arg.
	// This gives us a DCO frequency of 24 MHz
	UCS_initFLLSettle(24000, 24);

	// Set all system clocks to run at the DCO frequency of 24 MHz
	UCS_clockSignalInit(UCS_ACLK, UCS_DCOCLK_SELECT, UCS_CLOCK_DIVIDER_1 );
	UCS_clockSignalInit(UCS_MCLK, UCS_DCOCLK_SELECT, UCS_CLOCK_DIVIDER_1 );
	UCS_clockSignalInit(UCS_SMCLK, UCS_DCOCLK_SELECT, UCS_CLOCK_DIVIDER_1 );
}


// ADS1256 Primitives
#define IS_DATAREADY   (~P2IN & 0x20)
//#define ASSERT_SYNC    (P5OUT = P5IN & ~0x40)
//#define DEASSERT_SYNC  (P5OUT = P5IN | 0x40)
//#define ASSERT_CS      (P4OUT = P4IN & ~0x40)
//#define DEASSERT_CS    (P4OUT = P4IN & ~0x40)
//#define ASSERT_RESET   (P4OUT = P4IN & ~0x80)
//#define DEASSERT_RESET (P4OUT = P4IN | 0x80)

void InitGPIO()
{
	// Top LED
	GPIO_setAsOutputPin(GPIO_PORT_P1, GPIO_PIN3 | GPIO_PIN2 | GPIO_PIN1);
	P1OUT |= 0x07;

	// Set P1.0 as ACLK output
	GPIO_setAsPeripheralModuleFunctionOutputPin(GPIO_PORT_P1, GPIO_PIN0);

	// ADS1256 DRDY Input pin is P2.5 and P5.7 (P5.7 on old board). When low, it indicates that data is ready.
	// Older boards used P5.7, but that pin didn't have an interrupt and must be modified
	// to route the DRDY signal to P2.5. On new boards, P5.7 can be disconnected completely
	GPIO_setAsInputPin(GPIO_PORT_P2, GPIO_PIN5);
	GPIO_interruptEdgeSelect(GPIO_PORT_P2, GPIO_PIN5, GPIO_HIGH_TO_LOW_TRANSITION);


	// ADS1256 SYNC
	//GPIO_setAsOutputPin(GPIO_PORT_P5, GPIO_PIN6);

	// ADS1256 RESET
	//GPIO_setAsOutputPin(GPIO_PORT_P4, GPIO_PIN7);

	// ADS1256 CS
	//GPIO_setAsOutputPin(GPIO_PORT_P4, GPIO_PIN6);

	// ADS1256 DOUT == MSP430 SPI DIN
	GPIO_setAsInputPin(GPIO_PORT_P4, GPIO_PIN5);
	GPIO_setAsPeripheralModuleFunctionInputPin(GPIO_PORT_P4, GPIO_PIN5);    // Controlled by SPI HW

	// ADS1256 DIN == MSP430 DOUT
	GPIO_setAsOutputPin(GPIO_PORT_P4, GPIO_PIN4);
	GPIO_setAsPeripheralModuleFunctionOutputPin(GPIO_PORT_P4, GPIO_PIN4);   // Controlled by SPI HW

	// ADS1256 CLOCK
	GPIO_setAsOutputPin(GPIO_PORT_P4, GPIO_PIN0);
	GPIO_setAsPeripheralModuleFunctionOutputPin(GPIO_PORT_P4, GPIO_PIN0);	// Controlled by SPI HW
}


//
// Init TimerA to generate an interrupt at 1 KHz
//
void InitTimerA()
{
    // Setup Timer A to generate an interrupt at 1 KHz
	Timer_A_initUpModeParam  tp;
	tp.captureCompareInterruptEnable_CCR0_CCIE = TIMER_A_CCIE_CCR0_INTERRUPT_ENABLE;
    tp.clockSource = TIMER_A_CLOCKSOURCE_ACLK;
    tp.clockSourceDivider = TIMER_A_CLOCKSOURCE_DIVIDER_64; // This is 24 MHz / 64 = 375 KHz
    tp.startTimer = true;
    tp.timerClear = TIMER_A_DO_CLEAR;
    tp.timerInterruptEnable_TAIE = TIMER_A_TAIE_INTERRUPT_DISABLE;
    tp.timerPeriod = 375;  // Divide 375K by 375 to get 1mS interrupt rate
    Timer_A_initUpMode(TIMER, &tp);
}

//
// SPI communication with ADS ADC. We'll run at 2.5 MHz. Max for ADS1256 is 2.5 MHz
// (see figure 1 in spec, where t2l + t2h = 400 nS = 2.5 MHz
//
void InitSPI()
{
	USCI_A_SPI_initMasterParam param;
	param.selectClockSource = USCI_A_SPI_CLOCKSOURCE_ACLK;
	param.clockSourceFrequency = UCS_getACLK();
	param.desiredSpiClock = 2500000;
	param.msbFirst = USCI_A_SPI_MSB_FIRST;
	param.clockPhase = USCI_A_SPI_PHASE_DATA_CHANGED_ONFIRST_CAPTURED_ON_NEXT; // Latch on falling
	param.clockPolarity = USCI_A_SPI_CLOCKPOLARITY_INACTIVITY_LOW;
	//param.spiMode = USCI_A_SPI_3PIN;
	USCI_A_SPI_initMaster(SPI, &param);
	USCI_A_SPI_enable(SPI);
}

//
// Send a 32-bit word via SPI
//
/*
uint32_t SendSPI(uint32_t data)
{
	uint32_t r = 0;

	USCI_A_SPI_transmitData(SPI, data >> 24);
	r = (r << 8) + USCI_A_SPI_receiveData(SPI);

	USCI_A_SPI_transmitData(SPI, data >> 16);
	r = (r << 8) + USCI_A_SPI_receiveData(SPI);

	USCI_A_SPI_transmitData(SPI, data >> 8);
	r = (r << 8) + USCI_A_SPI_receiveData(SPI);

	USCI_A_SPI_transmitData(SPI, data >> 0);
	r = (r << 8) + USCI_A_SPI_receiveData(SPI);

	return r;
}
*/

// ADS1256 data notes:
// Input clock is 7.68 MHz == 130 nS
// Max sclock is 1.92 MHz
// Issuing data read to clocking out data is 6.56 uS
// Aim for 50 or 60 SPS. This is Fdata, and tau data is thus 20 mS
// The max SCLK is 10*20 mS or 200 mS
// Data should be latched on falling edge of SCLK
//

//
// Write to ADS register
//
void WriteADS1256Reg(uint8_t reg, uint8_t val)
{
	reg &= 0x0F;  // Mask register
	reg |= 0x50;  // Command

	USCI_A_SPI_transmitData(SPI, reg);   // This is the 1st command byte
	while (USCI_A_SPI_isBusy(SPI));
    USCI_A_SPI_receiveData(SPI);         // Junk read

	USCI_A_SPI_transmitData(SPI, 0);     // This is the number of bytes to write MINUS 1
	while (USCI_A_SPI_isBusy(SPI));
    USCI_A_SPI_receiveData(SPI);         // Junk read

	USCI_A_SPI_transmitData(SPI, val);   // Run the clock for 8 more cycles to write the byte
	while (USCI_A_SPI_isBusy(SPI));
	USCI_A_SPI_receiveData(SPI);         // Junk read
}

//
// Read from ADS register
//
uint8_t ReadADS1256Reg(uint8_t reg)
{
	reg &= 0x0F;  // Mask register
	reg |= 0x10;  // Command

	USCI_A_SPI_transmitData(SPI, reg);   // This is the 1st command byte
	while (USCI_A_SPI_isBusy(SPI));
    USCI_A_SPI_receiveData(SPI);         // Junk read

	USCI_A_SPI_transmitData(SPI, 0);     // This is the number of bytes to read MINUS 1
	while (USCI_A_SPI_isBusy(SPI));
    USCI_A_SPI_receiveData(SPI);         // Junk read

	DelayUS(5);						// This is t6 in spec (6.5uS is the min). See Fig1 and Fig 30

	USCI_A_SPI_transmitData(SPI, 0);    // Run the clock for 8 more cycles to grab the byte
	while (USCI_A_SPI_isBusy(SPI));

	return USCI_A_SPI_receiveData(SPI);
}

//
// Read data from ADS. This will block until the part is ready. If the part never
// becomes ready, then this will hang
//
int32_t ReadADS1256Data()
{
	uint32_t data = 0;

	// SPIN while DREADY bit is high (indicating it is NOT ready)
	while (IS_DATAREADY == 0)
		;

	USCI_A_SPI_transmitData(SPI, 0x01);  // Send first command byte (command + addr)
	while (USCI_A_SPI_isBusy(SPI));
	USCI_A_SPI_receiveData(SPI);         // Junk read

	// Delay t6 (see spec: at least 6.5us) to wait for response
	DelayUS(5);

	USCI_A_SPI_transmitData(SPI, 0x00);
	while (USCI_A_SPI_isBusy(SPI));
	data = (data << 8) + USCI_A_SPI_receiveData(SPI);

	USCI_A_SPI_transmitData(SPI, 0x00);
	while (USCI_A_SPI_isBusy(SPI));
	data = (data << 8) + USCI_A_SPI_receiveData(SPI);

	USCI_A_SPI_transmitData(SPI, 0x00);
	while (USCI_A_SPI_isBusy(SPI));
	data = (data << 8) + USCI_A_SPI_receiveData(SPI);

	// Sign extend to full 32 bits
	if ( (data & 0x00800000) > 0)
	{
		data += 0xFF000000;
	}

	return data;
}

// Issues SYNC command. Forces sampling to re-start, which will
// also cause DRDY to go high. This needs to be called whenever
// mux is changed, or pga is changed, or sample rate is changed,
// etc
void SyncADS1256()
{
	// See figure 36 in spec
	USCI_A_SPI_transmitData(SPI, 0xFC);  // Sync command
	DelayUS(10);                         // See t11 in spec. This is 3.1uS minimum
	USCI_A_SPI_transmitData(SPI, 0x00);  // Wakeup command

}


void ResetADS1256()
{
	uint8_t i;

	// Reset part. We don't have a CS, so if the comm gets out of sync with
	// the part, then the SPI bus will require 32 DRDY periods to reset. At
	// the slow sample rate, this is 12.8 seconds. So it's very important
	// to ensure SPI communications never get out of sync. But if they do
	// this will try 8 times to reset, hopefully overcoming any sync issue
	//
	// Upon reset we'll revert to 30K sample rate. And then we must
	// wait 32 cycles for the SPI bus to clear. This is 32 * 33uS = 1mS
	for (i=0; i<8; i++)
	{
		USCI_A_SPI_transmitData(SPI, 0xFE);
		DelayUS(50);
	}

	DelayUS(5000);
}

//
// Spins until ADS is ready
//
void WaitUntilDataReady()
{
	while (IS_DATAREADY == 0)
		;
}

//
// Set ADS PGA Level
void SetADS1256PGA(int pga)
{
	pga &= 0x7;
	WriteADS1256Reg(2, pga);
	SyncADS1256();
}

// Atten Level 0 = inputs 0/1 with D3 set high (atten SSR closed)
// Atten Level 1 = inputs 2/3 with D3 set low (atten SSR open)
void SetADS1256Atten(int attenLevel)
{
	switch (attenLevel)
	{
		case 0:
			WriteADS1256Reg(1, 0x01); WriteADS1256Reg(4, 0x78);
			break;
		case 1:
			WriteADS1256Reg(1, 0x23); WriteADS1256Reg(4, 0x70);
			break;
		default:
			while (1);



	}

	SyncADS1256();

}

void ADS1256SelfCal()
{
	    USCI_A_SPI_transmitData(SPI, 0xF0);  // Single write of 0xF0 does selfcal
		while (USCI_A_SPI_isBusy(SPI));
	    USCI_A_SPI_receiveData(SPI);         // Junk read
}

void InitADS1256()
{
	DelayUS(1000);

	WriteADS1256Reg(0, 0x6);  // Enable buffer and enable auto cal
	SyncADS1256();

	SetADS1256Atten(0);       // Select inputs 3/4. This will issue sync

	WriteADS1256Reg(2, 0);    // CLKout off, sensor detect off, PGA = 1
	SyncADS1256();

	DcSampleRate = SR_1k;
	SetADS1256SampleRate(SR_1k);
	SyncADS1256();
}

//
// Sample rate of 0 is slow, and 1 is fast
//
void SetADS1256SampleRate(uint8_t sampleRate)
{
	if (sampleRate == SR_2P5)
	{
		// Sample slow, 400mS sample period
		WriteADS1256Reg(3, 0x3); // Set to 2.5 SPS
	}
	else if (sampleRate == SR_1k)
	{
		// Sample fast, 1 mS period
		WriteADS1256Reg(3, 0xA1); // Set to 1000 sps
	}
	else if (sampleRate == SR_30k)
	{
		// RMS mode, set to 30,000 SPS
		WriteADS1256Reg(3, 0xE0);

		// 7500 sps
		//WriteADS1256Reg(3, 0xD0);
	}

	SyncADS1256();
}

//
// Cycle through the LEDs to indicate to the user  we've turned on.
// This is self-time, no other timing resources needed
//
void SweepLED()
{
	volatile uint32_t i, j;
	volatile uint32_t delayVal = 30000;

	 // All LED off
	P1OUT &= ~(BIT3 | BIT2 | BIT1);

	// Sweep LEDs
	for (i=0; i<10; i++)
	{
		P1OUT |= BIT3;
		for (j=0; j<delayVal; j++);
		P1OUT &= ~(BIT3 | BIT2 | BIT1);

		P1OUT |= BIT2;
		for (j=0; j<delayVal; j++);
		P1OUT &= ~(BIT3 | BIT2 | BIT1);

		P1OUT |= BIT1;
		for (j=0; j<delayVal; j++);
		P1OUT &= ~(BIT3 | BIT2 | BIT1);
	}
}

void SetMode(uint8_t mode)
{
	Mode = mode;

	if (mode == MODE_DC)
	{
		Mode = MODE_DC;
		SetADS1256SampleRate(DcSampleRate);
	}
	else if (mode == MODE_RMS)
	{
		Mode = MODE_RMS;
		//SetADS1256SampleRate(SR_30k);
		//RmsAccum = 0;
		//RmsSample = 0;
		//RmsBusy = 1;
	}
}

/*  
 * ======== main ========
 */
void main (void)
{
    WDT_A_hold(WDT_A_BASE); // Stop watchdog timer

    // Minimum Vcore setting required for the USB API is PMM_CORE_LEVEL_2 .
    PMM_setVCore(PMM_CORE_LEVEL_2);

    __disable_interrupt();

    InitGPIO();
    InitClocks();

    // Indicate we've booted
    SweepLED();

    InitSPI();
    InitTimerA();

    // At this point, interrupts must be enabled as the
    // routines immediately following rely on them for timing
    __enable_interrupt();  // Enable interrupts globally

    ResetADS1256();
    DelayUS(5000);           // Safe number
    InitADS1256();
    DelayUS(5000);           // Safe number
    DcSampleRate = SR_2P5;
    SetADS1256SampleRate(SR_2P5); // Set slow sample rate to match UI default

    USB_setup(TRUE, TRUE); // Init USB & events; if a host is present, connect

    ResetQA350();

    // Enable DRDY interrupt handling.
	GPIO_enableInterrupt(GPIO_PORT_P2, GPIO_PIN5);

    while (1)
    {
        // Check the USB state and directly main loop accordingly
        switch (USB_connectionState())
        {
            // This case is executed while your device is enumerated on the
            // USB host
            case ST_ENUM_ACTIVE:

            	// Only open it if we haven't already done so
				if (!(USBHID_intfStatus(HID0_INTFNUM, &x, &y) & kUSBHID_waitingForReceive))
				{
					// Start a receive operation. Everything arrives as 2 byte packets.
					if (USBHID_receiveData(UsbBuffer, 2, HID0_INTFNUM) == kUSBHID_busNotAvailable)
					{
						// Abort receive is BUS is not available
						USBHID_abortReceive(&x,HID0_INTFNUM);
						break;
					}
					else
					{
						// The bus was available and we're waiting to receive

					}
				}
				else
				{
					// Here, we are already waiting for receive. Do nothing.
				}


                // Wait in LPM0 until a receive operation has completed
                __bis_SR_register(LPM0_bits + GIE);

                if (bDataReceiveCompleted_event)
                {
                    bDataReceiveCompleted_event = FALSE;
                    ProcessUsbData();
                }

                if (bSampleReady_event)
                {
                	bSampleReady_event = FALSE;
                	volatile int32_t data = ReadADS1256Data();

					if (Mode == MODE_DC)
					{
						FifoPush(data);
					}
					else if (Mode == MODE_RMS)
					{
						if (RmsSample < RMS_SAMPLES)
						{
							++RmsSample;

							data = data >> 4;

							RmsAccum += ((int64_t)data * (int64_t)data);
						}
						else
						{
							// Done
							RmsBusy = false;
						}
					}
					else
					{
						// Unknown mode
						while (1);
					}
                }

                break;

            // These cases are executed while your device is disconnected from
            // the host (meaning, not enumerated); enumerated but suspended
            // by the host, or connected to a powered hub without a USB host
            // present.
            case ST_PHYS_DISCONNECTED:
            case ST_ENUM_SUSPENDED:
            case ST_PHYS_CONNECTED_NOENUM_SUSP:
                __bis_SR_register(LPM3_bits + GIE);
                _NOP();
                break;

            // The default is executed for the momentary state
            // ST_ENUM_IN_PROGRESS.  Usually, this state only last a few
            // seconds.  Be sure not to enter LPM3 in this state; USB
            // communication is taking place here, and therefore the mode must
            // be LPM0 or active-CPU.
            case ST_ENUM_IN_PROGRESS:
            default:;
        }

    }  //while(1)
} //main()

//
// Called from the main loop above. this handles all the USB command processing
//
void ProcessUsbData()
{
	uint8_t i;
    uint32_t data;

	// Here, data was received. The first byte is the command
	switch (UsbBuffer[0])
	{
		case 0:
			// This is a kick command. Light the LED for another 1000 mS
			LEDConnected = 1000;  	// 1 second timeout
			break;

		case 1:
			// Read last ADC reading. This won't disturb the fifo. If not in DC
			// mode, this will intentionally hang
			LEDCommand = 100; 		// 100 mS flash

			if (Mode != MODE_DC)
			{
				while (1)
				{
					LEDCommand = 100;
				}
			}

			data = FifoPeekLastPushed();

			UsbBuffer[0] = data >> 24;
			UsbBuffer[1] = data >> 16;
			UsbBuffer[2] = data >> 8;
			UsbBuffer[3] = data >> 0;

			if (hidSendDataInBackground( UsbBuffer, USB_BUF_LEN, HID0_INTFNUM,0))
			{
				// Operation may still be open; cancel it if the
				// send fails, escape the main loop
				USBHID_abortSend(&x,HID0_INTFNUM);
				break;
			}
			else
			{
				// Send was successful

			}
			break;

		case 2:
			// Set PGA
			SetADS1256PGA(UsbBuffer[1]);
			break;

		case 3:
			// Set attentuator
			SetADS1256Atten(UsbBuffer[1]);
			break;

		case 4:
			// Stream data. Grab 12 words / 48 bytes from the FIFO. If not used in
			// DC mode, this will intentionally hang the device
			LEDCommand = 100; 		// 100 mS flash

			if (Mode != MODE_DC)
			{
				while (1)
				{
					LEDCommand = 100;
				}
			}

			for (i=0; i<48; i+=4)
			{
				data = FifoPop();
				UsbBuffer[i+0] = data >> 24;
				UsbBuffer[i+1] = data >> 16;
				UsbBuffer[i+2] = data >> 8;
				UsbBuffer[i+3] = data >> 0;
			}

			if (hidSendDataInBackground( UsbBuffer, USB_BUF_LEN, HID0_INTFNUM,0))
			{
				// Operation may still be open; cancel it if the
				// send fails, escape the main loop
				USBHID_abortSend(&x,HID0_INTFNUM);
				break;
			}
			else
			{
				// Send was successful

			}
			break;

		case 5:
			// Query fifo count. If more than 12, then host can pull a full buffer. This
			// can be done in any mode, but doesn't make sense unless you are in DC mode
			data = FifoCount();
			UsbBuffer[0] = data >> 24;
			UsbBuffer[1] = data >> 16;
			UsbBuffer[2] = data >> 8;
			UsbBuffer[3] = data >> 0;

			if (hidSendDataInBackground( UsbBuffer, USB_BUF_LEN, HID0_INTFNUM,0))
			{
				// Operation may still be open; cancel it if the
				// send fails, escape the main loop
				USBHID_abortSend(&x,HID0_INTFNUM);
				break;
			}
			else
			{
				// Send was successful

			}
			break;

		// Sets DC sample rate, either 2.5SPS or 1K sps. May be called
	    // while in DC or RMS mode, but will only change settings
		// immediately if in DC mode.
		case 6:
			DcSampleRate = UsbBuffer[1];
			if (Mode == MODE_DC)
				SetADS1256SampleRate(DcSampleRate);
			FifoClear();
			break;

		// Set mode of operation, either DC or RMS
		case 12:
			if (Mode == MODE_DC)
				SetADS1256SampleRate(DcSampleRate);

			SetMode(UsbBuffer[1]);

			FifoClear();
			break;

		// Start RMS sample. If not in RMS mode, this will
		// intentionally hang
		case 13:
			LEDCommand = 100; 		// 100 mS flash

			if (Mode != MODE_RMS)
			{
				while (1)
				{
					LEDCommand = 100;
				}
			}

			RmsAccum = 0;
			RmsSample = 0;
			RmsBusy = 1;
			SetADS1256SampleRate(SR_30k);
			break;

		// Retrieve RMS reading. If not in RMS mode, this
		// will intentionally hang
		case 14:
			LEDCommand = 100; 		// 100 mS flash

			if (Mode != MODE_RMS)
			{
				while (1)
				{
					LEDCommand = 100;
				}
			}

			if (RmsBusy)
			{
				UsbBuffer[0] = INVALID_RESULT >> 24;
				UsbBuffer[1] = INVALID_RESULT >> 16;
				UsbBuffer[2] = INVALID_RESULT >> 8;
				UsbBuffer[3] = INVALID_RESULT >> 0;
			}
			else
			{
				RmsAccum = RmsAccum / (int64_t)RMS_SAMPLES;
			    uint32_t result = (int32_t)sqrtl(RmsAccum);

			    result = result << 4;

				UsbBuffer[0] = result >> 24;
				UsbBuffer[1] = result >> 16;
				UsbBuffer[2] = result >> 8;
				UsbBuffer[3] = result >> 0;
			}

			if (hidSendDataInBackground( UsbBuffer, USB_BUF_LEN, HID0_INTFNUM,0))
			{
				// Operation may still be open; cancel it if the
				// send fails, escape the main loop
				USBHID_abortSend(&x,HID0_INTFNUM);
				break;
			}
			else
			{
				// Send was successful

			}
			break;

		// Reset to known state
		case 251:
			ResetQA350();
			break;

		case 253:
			// Read serial number
			data = 12345678;
			UsbBuffer[0] = data >> 24;
			UsbBuffer[1] = data >> 16;
			UsbBuffer[2] = data >> 8;
			UsbBuffer[3] = data >> 0;

			if (hidSendDataInBackground( UsbBuffer, USB_BUF_LEN, HID0_INTFNUM,0))
			{
				// Operation may still be open; cancel it if the
				// send fails, escape the main loop
				USBHID_abortSend(&x,HID0_INTFNUM);
				break;
			}
			else
			{
				// Send was successful

			}
			break;

		case 254:
			// Read software version
			data = 11;
			UsbBuffer[0] = data >> 24;
			UsbBuffer[1] = data >> 16;
			UsbBuffer[2] = data >> 8;
			UsbBuffer[3] = data >> 0;

			if (hidSendDataInBackground( UsbBuffer, USB_BUF_LEN, HID0_INTFNUM,0))
			{
				// Operation may still be open; cancel it if the
				// send fails, escape the main loop
				USBHID_abortSend(&x,HID0_INTFNUM);
				break;
			}
			else
			{
				// Send was successful

			}
			break;

		case 255:
			// Update MSP430 Firmware. See section 3.8.1 in http://www.ti.com/lit/ug/slau319l/slau319l.pdf
			__disable_interrupt();
			USBKEYPID = 0x9628;  		// Unlock USB configuration registers
			USBCNF &= ~PUR_EN; 			// Set PUR pin to hi-Z, logically disconnect from host
			USBPWRCTL &= ~VBOFFIE; 		// Disable VUSBoff interrupt
			USBKEYPID = 0x9600; 		// Lock USB configuration register
			__delay_cycles(500000);
			((void (*)())0x1000)(); 	// Call BSL

			break;
	}

}

void ResetQA350()
{
	SetMode(MODE_DC);
	SetADS1256SampleRate(SR_2P5);
	SetADS1256PGA(0);

	// Low range, no atten active
	SetADS1256Atten(0);
	FifoClear();
}

//
// ISR Code
//
#pragma vector=PORT2_VECTOR
__interrupt void Port_5(void)
{
	bSampleReady_event = TRUE;
	/*
	// BUGBUG: Get rid of volatile. This is to help with debug only and will
	// impact optimization
	volatile int32_t data = ReadADS1256Data();

	if (Mode == MODE_DC)
	{
		FifoPush(data);
	}
	else if (Mode == MODE_RMS)
	{
		if (RmsSample < RMS_SAMPLES)
		{
			++RmsSample;

			data = data >> 4;

			RmsAccum += ((int64_t)data * (int64_t)data);
		}
		else
		{
			// Done
			RmsBusy = false;
		}
	}
	else
	{
		// Unknown mode
		while (1);
	}
	*/

	GPIO_clearInterruptFlag(GPIO_PORT_P2, GPIO_PIN5);
	__bic_SR_register_on_exit(CPUOFF);

}



#pragma vector=TIMER0_A0_VECTOR
__interrupt void Timer0_A0 (void)
{
	++SysTicks;

	// Indicate link is present. Top LED of hardware is BIT3
	if (LEDConnected > 0)
	{
		--LEDConnected;
		P1OUT |= BIT3;
	}
	else
	{
		P1OUT &= ~BIT3;
	}

	// Indicate LED Command. Middle LED is BIT2
	if (LEDCommand > 0)
	{
		--LEDCommand;
		P1OUT |= BIT2;
	}
	else
	{
		P1OUT &= ~BIT2;
	}

	// Stable LED Indicates power has been applied for > 10 minutes (600K ticks). Bottom LED is BIT1.
	// BUGBUG 2^32 mS = 1100 hours. This means the Stable LED will go off after 1100 hours
	// of being on
	if (SysTicks > 600000L)
	{
		P1OUT |= BIT1;
	}
	else
	{
		P1OUT &= ~ BIT1;
	}
}


/*  
 * ======== UNMI_ISR ========
 */
#if defined(__TI_COMPILER_VERSION__) || (__IAR_SYSTEMS_ICC__)
#pragma vector = UNMI_VECTOR
__interrupt void UNMI_ISR (void)
#elif defined(__GNUC__) && (__MSP430__)
void __attribute__ ((interrupt(UNMI_VECTOR))) UNMI_ISR (void)
#else
#error Compiler not found!
#endif
{
    switch (__even_in_range(SYSUNIV, SYSUNIV_BUSIFG ))
    {
        case SYSUNIV_NONE:
            __no_operation();
            break;
        case SYSUNIV_NMIIFG:
            __no_operation();
            break;
        case SYSUNIV_OFIFG:
#ifndef DRIVERLIB_LEGACY_MODE
            UCS_clearFaultFlag(UCS_XT2OFFG);
            UCS_clearFaultFlag(UCS_DCOFFG);
            SFR_clearInterrupt(SFR_OSCILLATOR_FAULT_INTERRUPT);
#else
            UCS_clearFaultFlag(UCS_BASE, UCS_XT2OFFG);
            UCS_clearFaultFlag(UCS_BASE, UCS_DCOFFG);
            SFR_clearInterrupt(SFR_BASE, SFR_OSCILLATOR_FAULT_INTERRUPT);
#endif
            break;
        case SYSUNIV_ACCVIFG:
            __no_operation();
            break;
        case SYSUNIV_BUSIFG:
            // If the CPU accesses USB memory while the USB module is
            // suspended, a "bus error" can occur.  This generates an NMI.  If
            // USB is automatically disconnecting in your software, set a
            // breakpoint here and see if execution hits it.  See the
            // Programmer's Guide for more information.
            SYSBERRIV = 0; //clear bus error flag
            USB_disable(); //Disable
    }
}

//
// Utils
//

//
// delays the specified number of milliseconds
//
void Delay(uint32_t delayMS)
{
	uint32_t target = GetSysTime() + delayMS;

	while (SysTicks < target)
		;
}

//
// Delays specified number of microseconds. This is not exact
//
void DelayUS(uint16_t delayUS)
{
	if (delayUS == 0)
		return;

	// Main CPU runs at 24 MHz, so 24 cycles = 1 microsecond. After tuning
	// looks like the real value is 35
	while (delayUS--)
		__delay_cycles(24);
}

//
// Returns the number of half milliseconds elapsed since boot
//
uint32_t GetSysTime()
{
	uint32_t time;

	// Need to disable while copying as ISR could corrupt the copying of the 4 bytes
	_DINT();
	time = SysTicks;
	_EINT();

	return time;
}

//Released_Version_4_20_00

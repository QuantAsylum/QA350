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

#include "driverlib.h"

#include "USB_config/descriptors.h"
#include "USB_API/USB_Common/device.h"
#include "USB_API/USB_Common/usb.h"                 // USB-specific functions
#include "USB_API/USB_HID_API/UsbHid.h"
#include "USB_app/usbConstructs.h"

//
// NOTE: Modify hal.h to select a specific evaluation board and customize for
// your own board.
//
#include "hal.h"

#define UART 		USCI_A1_BASE
#define TIMER 		TIMER_A0_BASE
#define ADC         ADC12_A_BASE
#define REF         REF_BASE
#define SPI 		USCI_A1_BASE

// Fwd decl
uint32_t GetSysTime();
void DelayUS(uint16_t delayUS);
void Delay(uint16_t delayMS);

// Global flags set by events
volatile uint8_t bCommandBeingProcessed = FALSE;
volatile uint8_t bDataReceiveCompleted_event = FALSE;  // data receive completed event

// Application globals
uint16_t x,y;
uint8_t size;
char c[2] = "";

// USB buffer is 4 bytes.
#define bufferLen 4
uint8_t buffer[bufferLen];

// Holds outgoing strings to be sent
char outString[65];

#define ADCWORDS 16
int ADCData[ADCWORDS];

// This is updated at 1mS rate. This keeps track of the number of milliseconds elapsed since
// system boot
volatile uint32_t SysTicks;


volatile uint16_t LEDConnected;  // Kicked by PC application at least once per second to keep LED lit
volatile uint8_t LEDCommand;     // Toggled for 50 mS upon receipt of command from the PC

volatile uint8_t MainThreadRun;

// Set by ISR, cleared by main thread. The ISR sets this var when it wants the main task to
// run.
bool RunMain = false;


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
#define IS_DATAREADY   (~P5IN & 0x80)
#define ASSERT_SYNC    (P5OUT = P5IN & ~0x40)
#define DEASSERT_SYNC  (P5OUT = P5IN | 0x40)
#define ASSERT_CS      (P4OUT = P4IN & ~0x40)
#define DEASSERT_CS    (P4OUT = P4IN & ~0x40)
#define ASSERT_RESET   (P4OUT = P4IN & ~0x80)
#define DEASSERT_RESET (P4OUT = P4IN | 0x80)

void InitGPIO()
{
	// Top LED
	GPIO_setAsOutputPin(GPIO_PORT_P1, GPIO_PIN3 | GPIO_PIN2 | GPIO_PIN1);
	P1OUT |= 0x07;

	// Set P1.0 as ACLK output
	GPIO_setAsPeripheralModuleFunctionOutputPin(GPIO_PORT_P1, GPIO_PIN0);

	// ADS1256 DRDY Input pin. When low, it indicates that data is ready
	GPIO_setAsInputPin(GPIO_PORT_P5, GPIO_PIN7);

	// ADS1256 SYNC
	GPIO_setAsOutputPin(GPIO_PORT_P5, GPIO_PIN6);

	// ADS1256 RESET
	GPIO_setAsOutputPin(GPIO_PORT_P4, GPIO_PIN7);

	// ADS1256 CS
	GPIO_setAsOutputPin(GPIO_PORT_P4, GPIO_PIN6);

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
    tp.timerPeriod = 375;
    Timer_A_initUpMode(TIMER, &tp);
}

//
// SPI communication with ADS ADC. We'll run at 1 MHz
//
void InitSPI()
{
	USCI_A_SPI_initMasterParam param;
	param.selectClockSource = USCI_A_SPI_CLOCKSOURCE_ACLK;
	param.clockSourceFrequency = UCS_getACLK();
	param.desiredSpiClock = 1000000;
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
uint32_t SendSPI(uint32_t data)
{
	uint32_t r = 0;

	ASSERT_CS;

	USCI_A_SPI_transmitData(SPI, data >> 24);
	r = (r << 8) + USCI_A_SPI_receiveData(SPI);

	USCI_A_SPI_transmitData(SPI, data >> 16);
	r = (r << 8) + USCI_A_SPI_receiveData(SPI);

	USCI_A_SPI_transmitData(SPI, data >> 8);
	r = (r << 8) + USCI_A_SPI_receiveData(SPI);

	USCI_A_SPI_transmitData(SPI, data >> 0);
	r = (r << 8) + USCI_A_SPI_receiveData(SPI);

	DEASSERT_CS;

	return r;
}

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

	ASSERT_CS;

	USCI_A_SPI_transmitData(SPI, reg);   // This is the 1st command byte
	while (USCI_A_SPI_isBusy(SPI));
    USCI_A_SPI_receiveData(SPI);         // Junk read

	USCI_A_SPI_transmitData(SPI, 0);     // This is the number of bytes to write MINUS 1
	while (USCI_A_SPI_isBusy(SPI));
    USCI_A_SPI_receiveData(SPI);         // Junk read

	USCI_A_SPI_transmitData(SPI, val);    // Run the clock for 8 more cycles to write the byte
	while (USCI_A_SPI_isBusy(SPI));
	USCI_A_SPI_receiveData(SPI);          // Junk read
	DelayUS(2);

	DEASSERT_CS;
	DelayUS(10);
}

//
// Read from ADS register
//
uint8_t ReadADS1256Reg(uint8_t reg)
{
	reg &= 0x0F;  // Mask register
	reg |= 0x10;  // Command

	ASSERT_CS;

	USCI_A_SPI_transmitData(SPI, reg);   // This is the 1st command byte
	while (USCI_A_SPI_isBusy(SPI));
    USCI_A_SPI_receiveData(SPI);         // Junk read


	USCI_A_SPI_transmitData(SPI, 0);     // This is the number of bytes to read MINUS 1
	while (USCI_A_SPI_isBusy(SPI));
    USCI_A_SPI_receiveData(SPI);         // Junk read

	DelayUS(10);

	USCI_A_SPI_transmitData(SPI, 0);    // Run the clock for 8 more cycles to grab the byte
	while (USCI_A_SPI_isBusy(SPI));
	DelayUS(2);
	DEASSERT_CS;

	DelayUS(10);

	return USCI_A_SPI_receiveData(SPI);
}

//
// Read data from ADS. This will block until the part is ready. If the part never
// becomes ready, then this will hang
//
uint32_t ReadADS1256Data()
{
	uint32_t data = 0;

	// SPIN while DREADY bit is high (indicating it is NOT ready)
	while (IS_DATAREADY == 0)
		;

	ASSERT_CS;

	USCI_A_SPI_transmitData(SPI, 0x01);  // Send first command byte (command + addr)
	while (USCI_A_SPI_isBusy(SPI));
	USCI_A_SPI_receiveData(SPI);         // Junk read

	// Delay 10 uS to wait for response
	DelayUS(10);

	USCI_A_SPI_transmitData(SPI, 0x00);
	while (USCI_A_SPI_isBusy(SPI));
	data = (data << 8) + USCI_A_SPI_receiveData(SPI);

	USCI_A_SPI_transmitData(SPI, 0x00);
	while (USCI_A_SPI_isBusy(SPI));
	data = (data << 8) + USCI_A_SPI_receiveData(SPI);

	USCI_A_SPI_transmitData(SPI, 0x00);
	while (USCI_A_SPI_isBusy(SPI));
	data = (data << 8) + USCI_A_SPI_receiveData(SPI);

	DEASSERT_CS;

	return data;
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
	//WaitUntilDataReady(); // We can't wait, otherwise it screws up USB communications
}

// Atten Level 0 = inputs 0/1 with D3 set high (atten SSR closed)
// Atten Level 1 = inputs 2/3 with D3 set low (atten SSR open)
void SetADS1256Atten(int attenLevel)
{
	switch (attenLevel)
	{
		case 0: WriteADS1256Reg(1, 0x01); WriteADS1256Reg(4, 0x78); break;
		case 1: WriteADS1256Reg(1, 0x23); WriteADS1256Reg(4, 0x70);break;
		//case 2: WriteADS1256Reg(1, 0x43); break;
		//case 3: WriteADS1256Reg(1, 0x52); break;
	}

	//ADS1256SelfCal();
}

void ADS1256SelfCal()
{
	    USCI_A_SPI_transmitData(SPI, 0xF0);  // Single write of 0xF0 does selfcal
		while (USCI_A_SPI_isBusy(SPI));
	    USCI_A_SPI_receiveData(SPI);         // Junk read
}

void InitADS1256()
{
    volatile uint8_t a, b, c, d, e;

	DEASSERT_RESET;
	DEASSERT_SYNC;
	Delay(1);


	WriteADS1256Reg(0, 0x6);  // Enable buffer
	WaitUntilDataReady();

	SetADS1256Atten(0);       // Select inputs 3/4
	WaitUntilDataReady();

	WriteADS1256Reg(2, 0);    // CLKout off, sensor detect off, PGA = 1
	WaitUntilDataReady();

	WriteADS1256Reg(3, 0x3);  // Set to 2.5 SPS
	//WriteADS1256Reg(3, 0x23);   // Set to 10SPS
	//WriteADS1256Reg(3, 0x82);   // Set to 100SPS
	WaitUntilDataReady();
}

//
// Cycle through the LEDs to indicate to the user  we've turned on
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

/*  
 * ======== main ========
 */
void main (void)
{
    WDT_A_hold(WDT_A_BASE); // Stop watchdog timer

    // Minimum Vcore setting required for the USB API is PMM_CORE_LEVEL_2 .
#ifndef  DRIVERLIB_LEGACY_MODE
    PMM_setVCore(PMM_CORE_LEVEL_2);
#else
    PMM_setVCore(PMM_BASE, PMM_CORE_LEVEL_2);
#endif

    __disable_interrupt();

    //initPorts();           // Config GPIOS for low-power (output low)
    InitGPIO();
    InitClocks();

    InitSPI();
    SweepLED();
    InitTimerA();

    InitADS1256();

    USB_setup(TRUE, TRUE); // Init USB & events; if a host is present, connect

    __enable_interrupt();  // Enable interrupts globally
    
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
					if (USBHID_receiveData(buffer, 2, HID0_INTFNUM) == kUSBHID_busNotAvailable)
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

                uint32_t data;

                if (bDataReceiveCompleted_event)
                {
                    bDataReceiveCompleted_event = FALSE;
                    
                    // Here, data was received. Verify the byte is the command to start conversion
                    switch (buffer[0])
                    {
						case 0:
							// This is a kick command. Light the LED for another 1000 mS
							LEDConnected = 1000;
							break;

						case 1:
							// Read ADC data command
							LEDCommand = 100;
							data = ReadADS1256Data();

							if (hidSendDataInBackground( (uint8_t*)&data, 4, HID0_INTFNUM,0))
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
							SetADS1256PGA(buffer[1]);
							break;

						case 3:
							// Set attentuator
							SetADS1256Atten(buffer[1]);
							break;

						case 253:
							// Read serial number
							data = 12345678;

							if (hidSendDataInBackground( (uint8_t*)&data, 4, HID0_INTFNUM,0))
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
							data = 3;

							if (hidSendDataInBackground( (uint8_t*)&data, 4, HID0_INTFNUM,0))
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
// ISR Code
//


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

	// Stable LED Indicates power has been applied for > 10 minutes. Bottom LED is BIT1.
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
// s the specified number of milliseconds
//
void Delay(uint16_t delayMS)
{
	uint32_t target = GetSysTime() + delayMS;

	while (SysTicks < target)
		;
}

//
// Delays specified number of microseconds.
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
// Returns the number of milliseconds elapsed since boot
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

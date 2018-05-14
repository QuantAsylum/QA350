#include "fifo.h"
#include "stdint.h"

// Must be power of 2. Number of words to hold
#define BUFSIZE 256

uint16_t ReadPtr;
uint16_t WritePtr;
uint8_t SequenceId;

uint32_t DataBuffer[BUFSIZE];
uint32_t LastPushed;

// When we push data, we will never wrap around. If pushing a value would
// result in a wrap-around, then we'll remove the oldest data. This function
// must be called with interrupts masked
void FifoPush(uint32_t data)
{
	uint8_t willWrap = 0;

	if ( ((WritePtr + 1) & (BUFSIZE-1)) == ReadPtr)
	{
		// Here, the FIFO is full. Rather than wrapping, we'll also increment
		// the readptr before we exit so that the the FIFO will always have the
		// freshest data available
		willWrap = 1;
	}

	LastPushed = ((uint32_t)SequenceId << 24) + (data & 0xFFFFFF);
	DataBuffer[WritePtr] =  LastPushed;
	++SequenceId;

	++WritePtr;
	WritePtr &= (BUFSIZE-1);

	if (willWrap)
	{
		++ReadPtr;
		ReadPtr &= (BUFSIZE-1);
	}
}

uint8_t  FifoDataAvail()
{
	uint8_t retVal;

	__disable_interrupt();

	if (ReadPtr != WritePtr)
		retVal = 1;
	else
		retVal = 0;

	__enable_interrupt();

	return retVal;
}

// Retrieves the most recently pushed data
uint32_t FifoPeekLastPushed()
{
	return LastPushed;
}

uint32_t FifoPop()
{
	// If nothign is in the fifo, indicate it with a
	// special return value
	if (FifoDataAvail() == 0)
	   return 0xFFFFFFFF;

	uint32_t data = DataBuffer[ReadPtr];

	__disable_interrupt();
	++ReadPtr;
	ReadPtr &= (BUFSIZE-1);
	__enable_interrupt();

	return data;
}

// Returns the number of items in the fifo
uint16_t FifoCount()
{
	uint16_t rPtr, wPtr;

	__disable_interrupt();
	rPtr = ReadPtr;
	wPtr = WritePtr;
	__enable_interrupt();

	if (wPtr < rPtr)
	{
		wPtr += BUFSIZE;
	}

	return wPtr - rPtr;
}


void FifoClear()
{
	__disable_interrupt();
	ReadPtr = 0;
	WritePtr = 0;
	__enable_interrupt();
}





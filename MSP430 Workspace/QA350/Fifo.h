#ifndef FIFO_H_
#define FIFO_H_

#include "stdint.h"

// When this result is returned to the PC, the PC should consider
// the result as invalid
#define INVALID_RESULT 0x80FFFFFF

void 	 FifoPush(uint32_t data);
uint8_t  FifoDataAvail();
uint32_t FifoPop();
uint32_t FifoPeekLastPushed();
uint16_t FifoCount();
void 	 FifoClear();

#endif /* FIFO_H_ */

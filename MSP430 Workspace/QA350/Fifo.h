#ifndef FIFO_H_
#define FIFO_H_

#include "stdint.h"

void 	 FifoPush(uint32_t data);
uint8_t  FifoDataAvail();
uint32_t FifoPop();
uint32_t FifoPeekLastPushed();
uint16_t FifoCount();




#endif /* FIFO_H_ */

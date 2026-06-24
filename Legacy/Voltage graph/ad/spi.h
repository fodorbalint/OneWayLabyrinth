#ifndef SPI_H
#define SPI_H

#include <stdint.h>
#include <stddef.h>

int spi_init(const char *device, uint32_t speed_hz);
void spi_transfer(int fd, const uint8_t *tx, uint8_t *rx, size_t len);
void spi_close(int fd);

#endif
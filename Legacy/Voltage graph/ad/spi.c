#include "spi.h"
#include <fcntl.h>
#include <linux/spi/spidev.h>
#include <stdio.h>
#include <sys/ioctl.h>
#include <unistd.h>

int spi_init(const char *device, uint32_t speed_hz) {
    int fd = open(device, O_RDWR);
    if (fd < 0) {
        perror("open SPI");
        return -1;
    }

    uint8_t bits = 8;
    ioctl(fd, SPI_IOC_WR_BITS_PER_WORD, &bits);
    ioctl(fd, SPI_IOC_RD_BITS_PER_WORD, &bits);

    uint8_t mode = SPI_MODE_2;
    ioctl(fd, SPI_IOC_WR_MODE, &mode);
    ioctl(fd, SPI_IOC_RD_MODE, &mode);

    uint8_t lsb = 0;
    ioctl(fd, SPI_IOC_WR_LSB_FIRST, &lsb);
    ioctl(fd, SPI_IOC_RD_LSB_FIRST, &lsb);

    ioctl(fd, SPI_IOC_WR_MAX_SPEED_HZ, &speed_hz);
    return fd;
}

void spi_transfer(int fd, const uint8_t *tx, uint8_t *rx, size_t len) {
    struct spi_ioc_transfer tr = {
        .tx_buf = (unsigned long)tx,
        .rx_buf = (unsigned long)rx,
        .len = len,
        .delay_usecs = 0,
        .speed_hz = 16000000,
        .bits_per_word = 8
    };
    ioctl(fd, SPI_IOC_MESSAGE(1), &tr);
}

void spi_close(int fd) {
    close(fd);
}
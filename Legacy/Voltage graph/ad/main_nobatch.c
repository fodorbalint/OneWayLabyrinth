#include <stdio.h>
#include <unistd.h>
#include <sys/time.h>
#include <string.h>
#include "gpio.h"
#include "spi.h"

#define CONVST_PIN  18   // GPIO18 (Pin )
#define BUSY_PIN    25   // GPIO27 (Pin )
#define RESET_PIN   17   // GPIO17 (Pin 11)

long long micros_since_epoch() {
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return (long long)tv.tv_sec * 1000000LL + tv.tv_usec;
}

int main(void) {
    // Disable buffering on stdout so data is pushed immediately
    setvbuf(stdout, NULL, _IONBF, 0);

    int spi_fd = spi_init("/dev/spidev0.0", 5000000); // 5 MHz
    if (spi_fd < 0) return 1;

    gpio_export(CONVST_PIN);
    gpio_export(BUSY_PIN);
    gpio_export(RESET_PIN);
    gpio_set_dir(CONVST_PIN, 1);
    gpio_set_dir(RESET_PIN, 1);
    gpio_set_dir(BUSY_PIN, 0);

    gpio_write(RESET_PIN, 1);
    usleep(1);
    gpio_write(RESET_PIN, 0);
    usleep(1);

    uint8_t rx[16];
    uint8_t frame[24]; 

    long long startTime = micros_since_epoch();
    long long nowTime = 0;

    while (nowTime <= 1000000) {

        // Trigger conversion
        gpio_write(CONVST_PIN, 1);
        gpio_write(CONVST_PIN, 0);

        while (gpio_read(BUSY_PIN));

        spi_transfer(spi_fd, NULL, rx, 16);
        nowTime = micros_since_epoch() - startTime;
        
        // copy ADC data (rx already contains 16 bytes)
        memcpy(frame, rx, 16);

        // append timestamp in little endian (LSB first)
        for (int i = 0; i < 8; i++) {
            frame[16 + i] = (nowTime >> (8 * i)) & 0xFF;
        }

        // Fill with dummy data for now
        //for (int i = 0; i < 16; i++) rx[i] = i;

        write(STDOUT_FILENO, frame, sizeof(frame));

        // Write raw bytes
        /*if (write(STDOUT_FILENO, frame, sizeof(frame)) != sizeof(frame)) {
            perror("write failed");
            return 1;
        }*/
    }

    /**/

    // --- uint8_t rx[16];

    // --- uint32_t counter = 1;
    // long long startTime = micros_since_epoch();
    // printf("Start time: %lld\n", startTime);

    // double arr[80000] = { [0 ... 79999] = -1 };
    // long long nowTime;

    /*
    // empty loop takes 3.8 ms
    while (counter <= 10000) {
        // 61 ms
        gpio_write(CONVST_PIN, 1);
        gpio_write(CONVST_PIN, 0);
        // 104 ms
        while (gpio_read(BUSY_PIN));

        // 1110 ms
        spi_transfer(spi_fd, NULL, rx, 16);

        // 1110 - 1150 ms
        for (int i = 0; i < 8; ++i) {
            int16_t raw = (rx[2*i] << 8) | rx[2*i+1];
            double voltage = raw * (5 / 32768.0);
            arr[counter*8 + i] = voltage;
        }
        counter++;
    }
    */

    // nowTime = micros_since_epoch();
    // printf("Mid time: %lld \t", nowTime - startTime);
    // startTime = nowTime;

    /*while (counter < 10) {
        // Trigger conversion
        gpio_write(CONVST_PIN, 1);
        gpio_write(CONVST_PIN, 0);

        while (gpio_read(BUSY_PIN));

        // spi_transfer(spi_fd, NULL, rx, 16);
        
        // Fill with dummy data for now
        for (int i = 0; i < 16; i++) rx[i] = i;

        // Write raw bytes
        if (write(STDOUT_FILENO, rx, sizeof(rx)) != sizeof(rx)) {
            perror("write failed");
            return 1;
        }

        // Simulate sampling delay (e.g. 1 ms)
        usleep(1000000);
        counter++;
    }*/

    //write(STDOUT_FILENO, rx, 16);
    //fflush(stdout);
    // printf("got 16 bytes\n");

    /*for (int i = 0; i < 8; ++i) {
        int16_t raw = (rx[2*i] << 8) | rx[2*i+1];
        double voltage = raw * (5 / 32768.0);
        arr[counter*8 + i] = voltage;
    }*/

    // nowTime = micros_since_epoch();
    // printf("End time: %lld \t", nowTime - startTime);

    //spi_close(spi_fd);
    //return 0;
}

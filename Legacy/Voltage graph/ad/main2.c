#include <stdio.h>
#include <unistd.h>
#include <sys/time.h>
#include <string.h>
#include "gpio.h"
#include "spi.h"
#include <fcntl.h>
#include <linux/spi/spidev.h>
#include <sys/ioctl.h>
#include <unistd.h>
#include <stdlib.h>
#include <gpiod.h>
#include <math.h>

#define CONVST_PIN  18   // GPIO18 (Pin 12)
#define BUSY_PIN    25   // GPIO27 (Pin 22)
#define RESET_PIN   17   // GPIO17 (Pin 11)
#define SIGNAL_PIN   24   // GPIO24 (Pin 18)

#define FRAMES_PER_BATCH 8
#define FRAME_SIZE 16       // 8 channels × 2 bytes
#define FRAME_SIZE_TS 24      
#define BATCH_SIZE (FRAMES_PER_BATCH * (FRAME_SIZE + 8)) // 8 is for timestamp
#define MAX_FRAMES 500000  // enough for ~10s at 50kS/s
static uint8_t ringbuf[MAX_FRAMES][FRAME_SIZE_TS];  // data + timestamp

long long micros_since_epoch() {
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return (long long)tv.tv_sec * 1000000LL + tv.tv_usec;
}

int main(void) {

    setvbuf(stdout, NULL, _IONBF, 0);
    setvbuf(stderr, NULL, _IONBF, 0);

    char cmd[21];
    int mode = 0;

    int spi_fd = spi_init("/dev/spidev0.0", 16000000); // 5 MHz
    if (spi_fd < 0) return 1;

    /*gpio_export(CONVST_PIN);
    gpio_export(BUSY_PIN);
    gpio_export(RESET_PIN);
    gpio_set_dir(CONVST_PIN, 1);
    gpio_set_dir(RESET_PIN, 1);
    gpio_set_dir(BUSY_PIN, 0);

    gpio_write(RESET_PIN, 1);
    usleep(1);
    gpio_write(RESET_PIN, 0);
    usleep(1);*/

    struct gpiod_line *lines[32] = {0};
    struct gpiod_chip *chip = NULL;
    chip = gpiod_chip_open_by_name("gpiochip0");
    lines[CONVST_PIN] = gpiod_chip_get_line(chip, CONVST_PIN);
    lines[RESET_PIN] = gpiod_chip_get_line(chip, RESET_PIN);
    lines[BUSY_PIN] = gpiod_chip_get_line(chip, BUSY_PIN);
    gpiod_line_request_output(lines[CONVST_PIN], "ad7606", 0);
    gpiod_line_request_output(lines[RESET_PIN], "ad7606", 0);
    gpiod_line_request_input(lines[BUSY_PIN], "ad7606");

    gpiod_line_set_value(lines[RESET_PIN], 1);
    usleep(1);
    gpiod_line_set_value(lines[RESET_PIN], 0);
    usleep(1);

    /*struct spi_ioc_transfer trs;
    uint8_t frame[FRAME_SIZE]; 

    gpiod_line_set_value(lines[CONVST_PIN], 1);
    gpiod_line_set_value(lines[CONVST_PIN], 0);

    trs.tx_buf = (unsigned long)NULL;
    trs.rx_buf = (unsigned long)&frame;
    trs.len = FRAME_SIZE;
    trs.delay_usecs = 0;
    trs.speed_hz = 16000000;
    trs.bits_per_word = 8;

    ioctl(spi_fd, SPI_IOC_MESSAGE(1), &trs);

    uint8_t trigger_channel = 0;
    int16_t raw = (frame[2*trigger_channel] << 8) | frame[2*trigger_channel + 1];  // big-endian → signed 16
    double channel_v1 = (double)raw * 5 / 32768.0;

    trigger_channel = 1;
    raw = (frame[2*trigger_channel] << 8) | frame[2*trigger_channel + 1];  // big-endian → signed 16
    double channel_v2 = (double)raw * 5 / 32768.0;

    trigger_channel = 2;
    raw = (frame[2*trigger_channel] << 8) | frame[2*trigger_channel + 1];  // big-endian → signed 16
    double channel_v3 = (double)raw * 5 / 32768.0;

    trigger_channel = 3;
    raw = (frame[2*trigger_channel] << 8) | frame[2*trigger_channel + 1];  // big-endian → signed 16
    double channel_v4 = (double)raw * 5 / 32768.0;

    for (int i=0; i<16; i++) printf("%02X ", frame[i]);
    printf("\n");

    printf("%f %f %f %f", channel_v1, channel_v2, channel_v3, channel_v4);

    return 0;*/

    lines[SIGNAL_PIN] = gpiod_chip_get_line(chip, SIGNAL_PIN);
    gpiod_line_request_input(lines[SIGNAL_PIN], "signal");

    while (1) {
        int n = read(STDIN_FILENO, cmd, sizeof(cmd) - 1);
        
        // int n = 20;
        // memcpy(cmd, "FAST0010000010030020", 20);

        // int n = 13;
        // memcpy(cmd, "FAST00100000", 12);

        if (n > 1) { 
            cmd[n] = '\0';            

            if (strncmp(cmd, "FAST", 4) == 0) mode = 1;
            else if (strncmp(cmd, "SLOW", 4) == 0) mode = 0;
            else if (strncmp(cmd, "SIGN", 4) == 0) mode = 2;
            else if (strncmp(cmd, "SAVE", 4) == 0) mode = 3;

            if (mode == 1) {
                // fprintf(stderr, "Received command: '%s' (%d bytes)\n", cmd, n);
                // fflush(stderr);

                char duration_str[9];
                memcpy(duration_str, cmd + 4, 8);  // copy 8 chars after "FAST"
                duration_str[8] = '\0';
                uint32_t duration = atoll(duration_str);

                uint32_t startTime = 0;
                uint32_t nowTime = 0;

                uint16_t batchSize = 1000;
                uint8_t frames[batchSize][FRAME_SIZE_TS]; 
                uint16_t lastBatchSize;
                uint16_t batchCount;

                if (n == 13) {
                    struct spi_ioc_transfer trs[batchSize];
                    uint8_t batch[batchSize*FRAME_SIZE_TS]; 

                    /* data rate without issuing pulses: 52 - 53000

                    gpio_write(CONVST_PIN, 1);
                    gpio_write(CONVST_PIN, 0);

                    */

                    // fprintf(stderr, "Duration: %d\n", duration);

                    uint8_t all_values[duration/1000000 * 50000 * FRAME_SIZE_TS];

                    while (nowTime < duration) {                
                        for (int f = 0; f < batchSize; f++) {
                            
                            // normal data rate 33000 - 34000, 42700 if I don't wait for busy                        

                            gpiod_line_set_value(lines[CONVST_PIN], 1);
                            gpiod_line_set_value(lines[CONVST_PIN], 0);

                            // while(gpiod_line_get_value(lines[BUSY_PIN]));

                            /*gpio_write(CONVST_PIN, 1);
                            gpio_write(CONVST_PIN, 0);

                            while (gpio_read(BUSY_PIN)); // normal rate 33600, 42000 if I don't wait for busy */                      
                            
                            trs[f].tx_buf = (unsigned long)NULL;
                            trs[f].rx_buf = (unsigned long)&batch[f * (FRAME_SIZE + 8)];
                            // trs[f].rx_buf = (unsigned long)&batch[f * FRAME_SIZE];
                            trs[f].len = FRAME_SIZE;
                            trs[f].delay_usecs = 0;
                            trs[f].speed_hz = 16000000;
                            trs[f].bits_per_word = 8;

                            ioctl(spi_fd, SPI_IOC_MESSAGE(1), &trs[f]);

                            if (startTime == 0) {
                                for (int i = 0; i < 8; i++) {
                                    batch[f * (FRAME_SIZE + 8) + 16 + i] = (nowTime >> (8 * i)) & 0xFF;
                                }

                                startTime = micros_since_epoch();
                            }
                            else {
                                nowTime = micros_since_epoch() - startTime;
                                /*if (nowTime < 1000) {
                                    fprintf(stderr, "%d\n", nowTime);
                                    nowTime = round(micros_since_epoch() - startTime);
                                    printf("2: %d\n", nowTime);
                                }*/

                                for (int i = 0; i < 8; i++) {
                                    batch[f * (FRAME_SIZE + 8) + 16 + i] = (nowTime >> (8 * i)) & 0xFF;
                                }

                                if (nowTime >= duration) {
                                    batchSize = ++f;
                                    break;
                                }

                            }                                          
                        }

                        write(STDOUT_FILENO, batch, batchSize*FRAME_SIZE_TS);                        
                    } 
                    uint8_t sentinel = 0xFF;  // any value not used in your data
                    write(STDOUT_FILENO, &sentinel, 1);  // write single byte                   
                }
                else { 
                    uint32_t triggerTime = 0;

                    char trigger_channel_str[2];
                    memcpy(trigger_channel_str, cmd + 12, 1); 
                    trigger_channel_str[1] = '\0';
                    uint8_t trigger_channel = atoll(trigger_channel_str) - 1;

                    char trigger_direction_str[2];
                    memcpy(trigger_direction_str, cmd + 13, 1); 
                    trigger_direction_str[1] = '\0';
                    uint8_t trigger_direction = atoll(trigger_direction_str);

                    char trigger_threshold_str[5];
                    memcpy(trigger_threshold_str, cmd + 14, 4); 
                    trigger_threshold_str[4] = '\0';
                    float trigger_threshold = atof(trigger_threshold_str) / 1000;

                    char trigger_percent_str[3];
                    memcpy(trigger_percent_str, cmd + 18, 2); 
                    trigger_percent_str[2] = '\0';
                    uint8_t trigger_percent = atoll(trigger_percent_str);

                    int head = 0;
                    int trigger_index = -1;
                    bool isFull = false;

                    struct spi_ioc_transfer trs;
                    uint8_t frame[FRAME_SIZE_TS]; 

                    startTime = micros_since_epoch();

                    // uint16_t counter = 0;

                    // fprintf(stderr, "Duration %d trigger channel %d trigger direction %d trigger threshold %f trigger percent %d\n", duration, trigger_channel, trigger_direction, trigger_threshold, trigger_percent);
                    // fflush(stderr); 

                    while (gpiod_line_get_value(lines[SIGNAL_PIN]) == 1 &&(trigger_index == -1 || (trigger_index != -1 && nowTime - triggerTime < duration * (100 - trigger_percent) / 100))) {

                        gpiod_line_set_value(lines[CONVST_PIN], 1);
                        gpiod_line_set_value(lines[CONVST_PIN], 0);

                        trs.tx_buf = (unsigned long)NULL;
                        trs.rx_buf = (unsigned long)&frame;
                        trs.len = FRAME_SIZE;
                        trs.delay_usecs = 0;
                        trs.speed_hz = 16000000;
                        trs.bits_per_word = 8;

                        ioctl(spi_fd, SPI_IOC_MESSAGE(1), &trs);

                        nowTime = micros_since_epoch() - startTime;

                        for (int i = 0; i < 8; i++) {
                            frame[16 + i] = (nowTime >> (8 * i)) & 0xFF;
                        }

                        memcpy(ringbuf[head], frame, FRAME_SIZE_TS); 

                        int16_t raw = (frame[2*trigger_channel] << 8) | frame[2*trigger_channel + 1];  // big-endian → signed 16
                        double channel_v = (double)raw * 5 / 32768.0;

                        // fprintf(stderr, "NowTime %d %d %d %f\n", head, counter, nowTime, channel_v);
                        // fflush(stderr);

                        if (trigger_index == -1 && ((trigger_direction == 1 && channel_v >= trigger_threshold) || (trigger_direction == 0 && channel_v <= trigger_threshold))) {// rising or falling edge                            
                            trigger_index = head;
                            triggerTime = nowTime;
                        }

                        head = (head + 1) % MAX_FRAMES;
                        if (head == 0) isFull = true;

                        // counter++;
                        // if (counter == 3720) break;
                    }

                    if (trigger_index == -1) { // waint interrupted
                        uint8_t sentinel = 0xFF; 
                        write(STDOUT_FILENO, &sentinel, 1);
                    }
                    else {
                        uint32_t c = 0;
                        long long startTime = micros_since_epoch();                        

                        fprintf(stderr, "isFull %d timestamp %d head %d last batch size %d trigger index %d\n", isFull, nowTime, head, head % batchSize, trigger_index);
                        fflush(stderr);

                        if (!isFull) {
                            // find starting point
                            uint16_t start = 0;
                            while (true) {
                                memcpy(frame, ringbuf[start], FRAME_SIZE_TS);
                                uint32_t ts = 0;
                                for (int j = 0; j < 8; j++) {
                                    ts |= ((uint64_t)frame[FRAME_SIZE + j]) << (8 * j);
                                }
                                if (triggerTime - ts <= (trigger_percent / 100) * duration) break;
                                start++;
                            }
                            
                            // write in batches of set size plus remainder
                            lastBatchSize = (head - start) % batchSize;
                            batchCount = ((head - start) - lastBatchSize) / batchSize;

                            uint32_t i;
                            for (i = 0; i < batchCount; i++) {
                                for (int j = 0; j < batchSize; j++) {
                                    memcpy(frames[j], ringbuf[start + i*batchSize + j], FRAME_SIZE_TS);
                                }
                                write(STDOUT_FILENO, frames, batchSize*FRAME_SIZE_TS);
                                c++;                              
                            }
                            if (lastBatchSize > 0) {
                                for (int j = 0; j < lastBatchSize; j++) {
                                    memcpy(frames[j], ringbuf[start + i*batchSize + j], FRAME_SIZE_TS);
                                }
                                write(STDOUT_FILENO, frames, lastBatchSize*FRAME_SIZE_TS); 
                            }                            
                        }
                        else { 
                            // find starting point
                            uint16_t start = head;
                            while (true) {
                                memcpy(frame, ringbuf[start % MAX_FRAMES], FRAME_SIZE_TS);
                                uint32_t ts = 0;
                                for (int j = 0; j < 8; j++) {
                                    ts |= ((uint64_t)frame[FRAME_SIZE + j]) << (8 * j);
                                }
                                if (triggerTime - ts <= (trigger_percent / 100) * duration) break;
                                start++;
                            }

                            // write in batches of set size plus remainder
                            uint32_t frameCount = (head > start) ? head - start : head + MAX_FRAMES - start;
                            lastBatchSize = frameCount % batchSize;
                            batchCount = (frameCount - lastBatchSize) / batchSize;

                            uint32_t i;
                            for (i = 0; i < batchCount; i++) {
                                for (int j = 0; j < batchSize; j++) {
                                    memcpy(frames[j], ringbuf[(start + i*batchSize + j) % MAX_FRAMES], FRAME_SIZE_TS);
                                }
                                write(STDOUT_FILENO, frames, batchSize*FRAME_SIZE_TS); 
                                c++;                            
                            }
                            if (lastBatchSize > 0) {
                                for (int j = 0; j < lastBatchSize; j++) {
                                    memcpy(frames[j], ringbuf[(start + i*batchSize + j) % MAX_FRAMES], FRAME_SIZE_TS);
                                }
                                write(STDOUT_FILENO, frames, lastBatchSize*FRAME_SIZE_TS); 
                            } 
                        }

                        fprintf(stderr, "wrote %d in %llu\n", c*batchCount + lastBatchSize, micros_since_epoch() - startTime);
                        fflush(stderr);

                        uint8_t sentinel = 0xFF;  // any value not used in your data
                        write(STDOUT_FILENO, &sentinel, 1);  // write single byte

                        // fprintf(stderr, "Write complete\n");
                        // fflush(stderr); 
                    }                    
                }
            }
            else if (mode == 0) {
                struct spi_ioc_transfer trs;
                uint8_t frame[FRAME_SIZE]; 

                /*gpio_write(CONVST_PIN, 1);
                gpio_write(CONVST_PIN, 0);
                while (gpio_read(BUSY_PIN));*/

                gpiod_line_set_value(lines[CONVST_PIN], 1);
                gpiod_line_set_value(lines[CONVST_PIN], 0);

                while(gpiod_line_get_value(lines[BUSY_PIN]));
                
                trs.tx_buf = (unsigned long)NULL;
                trs.rx_buf = (unsigned long)&frame;
                trs.len = FRAME_SIZE;
                trs.delay_usecs = 0;
                trs.speed_hz = 16000000;
                trs.bits_per_word = 8;

                ioctl(spi_fd, SPI_IOC_MESSAGE(1), &trs);
                write(STDOUT_FILENO, frame, FRAME_SIZE);
            }
            else if (mode == 2) {
                fprintf(stderr, "%d\n", gpiod_line_get_value(lines[SIGNAL_PIN]));
                fflush(stderr);
            }
        }        

        usleep(1000);
    }

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

    spi_close(spi_fd);
    return 0;
}

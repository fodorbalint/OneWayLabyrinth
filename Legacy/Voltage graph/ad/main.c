// compile:
// make
// run:
// sudo ./ad7606_reader

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
#include <dirent.h>
#include <regex.h>
#include <ctype.h>
#include <limits.h>   // for PATH_MAX (if available)

#define CONVST_PIN  18   // GPIO18 (Pin 12)
#define BUSY_PIN    25   // GPIO27 (Pin 22)
#define RESET_PIN   17   // GPIO17 (Pin 11)
#define SIGNAL_PIN   24   // GPIO24 (Pin 18)

#define FRAME_SIZE 8       // 8 channels × 2 bytes
#define FRAME_SIZE_TS 12      
#define BATCH_SIZE (FRAMES_PER_BATCH * (FRAME_SIZE + 4)) // 4 is for timestamp
#define MAX_FRAMES 1500000  // enough for ~10s at 60kS/s
static uint8_t ringbuf[MAX_FRAMES][FRAME_SIZE_TS];  // data + timestamp
// uint8_t all_values[duration/1000000 * 50000][FRAME_SIZE_TS];
static uint8_t all_values[MAX_FRAMES][FRAME_SIZE_TS];
// Frame all_values[MAX_FRAMES];
uint32_t all_values_size = 0;
#define ADC_RANGE_V 5.0f
#define ADC_SCALE (ADC_RANGE_V / 32768.0f)
#define TS_OFFSET 8      // timestamp starts at byte 16 (8 bytes)
#define CH1_OFFSET 0
#define CH2_OFFSET 2
#define CH3_OFFSET 4
#define CH4_OFFSET 6

long long micros_since_epoch() {
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return (long long)tv.tv_sec * 1000000LL + tv.tv_usec;
}

int get_highest_number(const char *directory) {
    const char *prefix = "voltage4_log_";
    const char *suffix = ".txt";
    size_t prelen = strlen(prefix);
    size_t suflen = strlen(suffix);

    DIR *dir = opendir(directory);
    if (!dir) {
        perror("opendir");
        return 0;          // no files -> return 0
    }

    struct dirent *entry;
    int max_number = 0;

    while ((entry = readdir(dir)) != NULL) {
        const char *name = entry->d_name;
        size_t len = strlen(name);

        // fast checks
        if (len <= prelen + suflen) continue;                 // too short
        if (strncmp(name, prefix, prelen) != 0) continue;     // wrong prefix
        if (strcmp(name + len - suflen, suffix) != 0) continue; // wrong suffix

        // digits between prefix and suffix?
        size_t digit_len = len - prelen - suflen;
        int valid = 1;
        for (size_t i = 0; i < digit_len; ++i) {
            if (!isdigit((unsigned char)name[prelen + i])) {
                valid = 0;
                break;
            }
        }
        if (!valid) continue;

        // parse number safely (avoid atoi which has no overflow detection)
        long num = 0;
        for (size_t i = 0; i < digit_len; ++i) {
            num = num * 10 + (name[prelen + i] - '0');
            if (num > INT_MAX) { num = INT_MAX; break; }
        }

        if ((int)num > max_number) max_number = (int)num;
    }

    closedir(dir);
    return max_number;
}

/* Build next filename into buffer. Returns 0 on success, -1 on truncation/error. */
int make_next_filename(char *buf, size_t bufsz, const char *directory, int next_number) {
    // If you want the filename in the directory: "directory/voltage4_log_X.txt"
    // If directory is ".", just create the file name without prefixing "./"
    if (directory == NULL || directory[0] == '\0' || (directory[0] == '.' && directory[1] == '\0')) {
        int n = snprintf(buf, bufsz, "voltage4_log_%d.txt", next_number);
        if (n < 0 || (size_t)n >= bufsz) return -1;
        return 0;
    } else {
        // join path + filename (avoid double slash)
        size_t dlen = strlen(directory);
        const char *fmt = (directory[dlen - 1] == '/') ? "%svoltage4_log_%d.txt" : "%s/voltage4_log_%d.txt";
        int n = snprintf(buf, bufsz, fmt, directory, next_number);
        if (n < 0 || (size_t)n >= bufsz) return -1;
        return 0;
    }
}

// Helpers to convert big-endian bytes to host integers
static inline int16_t be16_to_int16(const uint8_t *p) {
    uint16_t raw = (uint16_t)p[0] << 8 | (uint16_t)p[1];
    return (int16_t)raw;   // preserves sign bit correctly
}

void save_frames_to_file(size_t count, const char *filename, uint8_t ch1offset, uint8_t ch2offset, uint8_t ch3offset, uint8_t ch4offset) {
    FILE *f = fopen(filename, "w");
    if (!f) {
        perror("fopen");
        return;
    }
    for (size_t i = 0; i < count; ++i) {
        uint8_t *frame = all_values[i];

        // extract raw channel data (2 bytes each)
        //int16_t ch2, ch3, ch4, ch5;
        uint64_t timestamp_us;

        // Read channels (big-endian 16-bit)
        int16_t ch1 = be16_to_int16(frame + CH1_OFFSET);
        int16_t ch2 = be16_to_int16(frame + CH2_OFFSET);
        int16_t ch3 = be16_to_int16(frame + CH3_OFFSET);
        int16_t ch4 = be16_to_int16(frame + CH4_OFFSET);

        memcpy(&timestamp_us, frame + TS_OFFSET, sizeof(uint32_t));

        // convert to voltage (adjust sign or scaling if needed)
        float v1 = ch1 * ADC_SCALE;
        float v2 = ch2 * ADC_SCALE;
        float v3 = ch3 * ADC_SCALE;
        float v4 = ch4 * ADC_SCALE;

        float values[4] = {v1, v2, v3, v4};

        if (ch1offset > 0 && ch1offset < 5) {
            v1 -= values[ch1offset - 1];
        }
        else if (ch1offset > 5 && ch1offset < 10) {
            v1 = values[ch1offset - 6] - v1;
        }

        if (ch2offset > 0 && ch2offset < 5) {
            v2 -= values[ch2offset - 1];
        }
        else if (ch2offset > 5 && ch2offset < 10) {
            v2 = values[ch2offset - 6] - v2;
        }

        if (ch3offset > 0 && ch3offset < 5) {
            v3 -= values[ch3offset - 1];
        }
        else if (ch3offset > 5 && ch3offset < 10) {
            v3 = values[ch3offset - 6] - v3;
        }

        if (ch4offset > 0 && ch4offset < 5) {
            v4 -= values[ch4offset - 1];
        }
        else if (ch4offset > 5 && ch4offset < 10) {
            v4 = values[ch4offset - 6] - v4;
        }

        if (ch1offset == 5) {
            v1 = 0;
        }
        else if (ch1offset == 10) {
            v1 *= -1;
        }
        if (ch2offset == 5) {
            v2 = 0;
        }
        else if (ch2offset == 10) {
            v2 *= -1;
        }
        if (ch3offset == 5) {
            v3 = 0;
        }
        else if (ch3offset == 10) {
            v3 *= -1;
        }
        if (ch4offset == 5) {
            v4 = 0;
        }
        else if (ch4offset == 10) {
            v4 *= -1;
        }

        fprintf(f, "%llu %.6f %.6f %.6f %.6f\n",
                (unsigned long long)timestamp_us, v1, v2, v3, v4);
    }
    fclose(f);
    memset(all_values, 0, sizeof(all_values));
}

int main(void) {

    setvbuf(stdout, NULL, _IONBF, 0);
    setvbuf(stderr, NULL, _IONBF, 0);

    char cmd[22];
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
                /*fprintf(stderr, "Received command: (%d bytes)\n", n);
                fflush(stderr);*/

                char duration_str[9];
                memcpy(duration_str, cmd + 4, 8);  // copy 8 chars after "FAST"
                duration_str[8] = '\0';
                uint32_t duration = atoll(duration_str);

                uint32_t startTime = 0;
                uint32_t nowTime = 0; 

                if (n == 13) {
                    // data rate without issuing pulses: 52 - 53000
                    // normal data rate 33000 - 34000, 42700 if I don't wait for busy 

                    struct spi_ioc_transfer trs = {0};
                
                    uint32_t counter = 0;
                    while (nowTime < duration) {
                        gpiod_line_set_value(lines[CONVST_PIN], 1);
                        gpiod_line_set_value(lines[CONVST_PIN], 0);

                        trs.tx_buf = (unsigned long)NULL;
                        trs.rx_buf = (unsigned long)&all_values[counter];
                        trs.len = FRAME_SIZE;
                        trs.delay_usecs = 0;
                        trs.speed_hz = 16000000;
                        trs.bits_per_word = 8;

                        ioctl(spi_fd, SPI_IOC_MESSAGE(1), &trs);

                        if (startTime == 0) {
                            startTime = micros_since_epoch();
                        }
                        else {
                            nowTime = micros_since_epoch() - startTime;                                        
                        }

                        memcpy(&all_values[counter][FRAME_SIZE], &nowTime, sizeof(uint32_t));

                        counter++;                       
                    }

                    /*fprintf(stderr, "Counter: %d\n", counter);
                    fflush(stderr);*/

                    /*for (int i = 0; i < FRAME_SIZE; i++)
                    fprintf(stderr, "%02X ", all_values[counter -1][i]);
                    fprintf(stderr, "\n");

                    int16_t ch1 = be16_to_int16(all_values[counter -1] + CH1_OFFSET);
                    int16_t ch2 = be16_to_int16(all_values[counter -1] + CH2_OFFSET);
                    int16_t ch3 = be16_to_int16(all_values[counter -1] + CH3_OFFSET);
                    int16_t ch4 = be16_to_int16(all_values[counter -1] + CH4_OFFSET);

                    fprintf(stderr, "Last readings: %d %d %d %d\n", ch1, ch2, ch3, ch4);
                    fflush(stderr);
                    
                    for (int i = 0; i < FRAME_SIZE; i++)
                    fprintf(stderr, "%02X ", all_values[0][i]);
                    fprintf(stderr, "\n");

                    ch1 = be16_to_int16(all_values[0] + CH1_OFFSET);
                    ch2 = be16_to_int16(all_values[0] + CH2_OFFSET);
                    ch3 = be16_to_int16(all_values[0] + CH3_OFFSET);
                    ch4 = be16_to_int16(all_values[0] + CH4_OFFSET);

                    fprintf(stderr, "First readings: %d %d %d %d\n", ch1, ch2, ch3, ch4);
                    fflush(stderr);*/

                    uint32_t size = counter * FRAME_SIZE_TS;
                    write(STDOUT_FILENO, &size, sizeof(size));
                    write(STDOUT_FILENO, all_values, size); 
                    all_values_size = counter;                                     
                }
                else { 
                    uint32_t triggerTime = 0;

                    char trigger_channel_str[2];
                    memcpy(trigger_channel_str, cmd + 12, 1); 
                    trigger_channel_str[1] = '\0';
                    uint8_t trigger_channel = atoll(trigger_channel_str) - 1;

                    char offset_channel_str[2];
                    memcpy(offset_channel_str, cmd + 13, 1); 
                    offset_channel_str[1] = '\0';
                    uint8_t offset_channel = (uint8_t)strtoul(offset_channel_str, NULL, 16);

                    char trigger_direction_str[2];
                    memcpy(trigger_direction_str, cmd + 14, 1); 
                    trigger_direction_str[1] = '\0';
                    uint8_t trigger_direction = atoll(trigger_direction_str);

                    char trigger_threshold_str[5];
                    memcpy(trigger_threshold_str, cmd + 15, 4); 
                    trigger_threshold_str[4] = '\0';
                    float trigger_threshold = atof(trigger_threshold_str) / 1000;

                    char trigger_percent_str[3];
                    memcpy(trigger_percent_str, cmd + 19, 2); 
                    trigger_percent_str[2] = '\0';
                    float trigger_percent = atof(trigger_percent_str);

                    uint32_t head = 0;
                    uint32_t trigger_index = -1;
                    bool isFull = false;

                    struct spi_ioc_transfer trs = {0};
                    uint8_t frame[FRAME_SIZE_TS]; 

                    startTime = micros_since_epoch();

                    // uint32_t counter = 0;

                    // fprintf(stderr, "Cmd -%s- Duration %d trigger channel %d offset channel %d trigger direction %d trigger threshold %f trigger percent %f\n", cmd, duration, trigger_channel, offset_channel, trigger_direction, trigger_threshold, trigger_percent);
                    fflush(stderr);
                    
                    double channel_v = -1;

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

                        memcpy(ringbuf[head], frame, FRAME_SIZE_TS);
                        memcpy(&ringbuf[head][FRAME_SIZE], &nowTime, sizeof(uint32_t)); 

                        int16_t raw = (frame[2 * trigger_channel] << 8) | frame[2 * trigger_channel + 1];  // big-endian → signed 16
                        channel_v = (double)raw * 5 / 32768.0;

                        if (offset_channel > 0 && offset_channel < 5) {
                            raw = (frame[2 * (offset_channel - 1)] << 8) | frame[2 * (offset_channel - 1) + 1];  // big-endian → signed 16
                            channel_v -= (double)raw * 5 / 32768.0;
                        }
                        else if (offset_channel > 5 && offset_channel < 10) {
                            raw = (frame[2 * ((offset_channel - 1) - 5)] << 8) | frame[2 * ((offset_channel - 1) - 5) + 1];  // big-endian → signed 16
                            channel_v = (double)raw * 5 / 32768.0 - channel_v;
                        }
                        else if (offset_channel == 5) {
                            channel_v = 0;
                        }
                        else if (offset_channel == 10) {
                            channel_v *= -1;
                        }

                        // fprintf(stderr, "NowTime %d %d %d %f\n", head, counter, nowTime, channel_v);
                        // fflush(stderr);

                        if (trigger_index == -1 && ((trigger_direction == 1 && channel_v >= trigger_threshold) || (trigger_direction == 0 && channel_v <= trigger_threshold))) {// rising or falling edge                            
                            trigger_index = head;
                            triggerTime = nowTime;
                        }

                        head = (head + 1) % MAX_FRAMES;
                        if (head == 0) isFull = true;

                        // counter++;
                    }

                    /* fprintf(stderr, "channel_v %f, isFull %d triggerTime %d head %d trigger_index %d trigger_channel %d\n", channel_v, isFull, triggerTime, head, trigger_index, trigger_channel);
                    fflush(stderr); */

                    if (trigger_index == -1) { // wait interrupted
                        /*fprintf(stderr, "isFull %d triggerTime %d head %d trigger_index %d trigger_channel %d\n", isFull, triggerTime, head, trigger_index, trigger_channel);
                        fprintf(stderr, "isFull %d triggerTime %d head %d trigger_index %d trigger_channel %d\n", isFull, triggerTime, head, trigger_index, trigger_channel);*/
                        fflush(stderr);
                        uint32_t size = 0;
                        write(STDOUT_FILENO, &size, sizeof(size));
                    }
                    else {
                        // long long startTime = micros_since_epoch();
                        
                        uint32_t count;                        
                        uint32_t periodStartTime = 0;

                        if (!isFull) {
                            // find starting point
                            uint32_t start = 0;
                            while (true) {
                                memcpy(frame, ringbuf[start], FRAME_SIZE_TS);
                                uint32_t ts = 0;
                                for (int j = 0; j < 4; j++) {
                                    ts |= ((uint32_t)frame[FRAME_SIZE + j]) << (8 * j);
                                }
                                if (triggerTime - ts <= trigger_percent / 100 * duration) {
                                    // take previous frame, to ensure that the pre-trigger period is at least the set percent
                                    if (start > 0) {
                                        start--;
                                        memcpy(frame, ringbuf[start], FRAME_SIZE_TS);
                                        ts = 0;
                                        for (int j = 0; j < 4; j++) {
                                            ts |= ((uint32_t)frame[FRAME_SIZE + j]) << (8 * j);
                                        }
                                    }
                                    periodStartTime = ts;
                                    break;
                                }
                                start++;
                            }

                            /*fprintf(stderr, "Period start time %d start index %d\n", periodStartTime, start);
                            fflush(stderr);*/

                            count = head - start;
                            for (uint32_t i = 0; i < count; i++) {
                                memcpy(frame, ringbuf[start + i], FRAME_SIZE_TS);
                                uint32_t absTime = 0;
                                for (int j = 0; j < 4; j++) {
                                    absTime |= ((uint32_t)frame[FRAME_SIZE + j]) << (8 * j);
                                }
                                uint32_t relativeTime = absTime - periodStartTime;
                                for (int i = 0; i < 4; i++) {
                                    frame[FRAME_SIZE + i] = (relativeTime >> (8 * i)) & 0xFF;
                                }                                
                                memcpy(all_values[i], frame, FRAME_SIZE_TS);
                            }

                            memcpy(frame, all_values[0], FRAME_SIZE_TS);
                            uint32_t ts1 = 0;
                            for (int j = 0; j < 4; j++) {
                                ts1 |= ((uint32_t)frame[FRAME_SIZE + j]) << (8 * j);
                            }

                            memcpy(frame, all_values[1], FRAME_SIZE_TS);
                            uint32_t ts2 = 0;
                            for (int j = 0; j < 4; j++) {
                                ts2 |= ((uint32_t)frame[FRAME_SIZE + j]) << (8 * j);
                            }

                            /*fprintf(stderr, "count %d first ts %d second ts %d\n", count, ts1, ts2);
                            fflush(stderr);*/

                            uint32_t size = (head - start) * FRAME_SIZE_TS;
                            write(STDOUT_FILENO, &size, sizeof(size));
                            write(STDOUT_FILENO, all_values, size); 
                            all_values_size = head - start;                            
                        }
                        else { 
                            // find starting point
                            uint32_t start = head;                            
                            while (true) {
                                memcpy(frame, ringbuf[start % MAX_FRAMES], FRAME_SIZE_TS);
                                uint32_t ts = 0;
                                for (int j = 0; j < 4; j++) {
                                    ts |= ((uint32_t)frame[FRAME_SIZE + j]) << (8 * j);
                                }
                                if (triggerTime - ts <= trigger_percent / 100 * duration) {                                    
                                    start = (start == 0) ? MAX_FRAMES - 1 : start - 1;
                                    memcpy(frame, ringbuf[start % MAX_FRAMES], FRAME_SIZE_TS);
                                    ts = 0;
                                    for (int j = 0; j < 4; j++) {
                                        ts |= ((uint32_t)frame[FRAME_SIZE + j]) << (8 * j);
                                    }
                                    periodStartTime = ts;

                                    break;
                                }
                                start++;
                            }

                            /*fprintf(stderr, "Period start time %d start index %d\n", periodStartTime, start);
                            fflush(stderr);*/                            

                            count = (head > start) ? head - start : head + MAX_FRAMES - start;
                            for (uint32_t i = 0; i < count; i++) {
                                memcpy(frame, ringbuf[(start + i) % MAX_FRAMES], FRAME_SIZE_TS);
                                uint32_t absTime = 0;
                                for (int j = 0; j < 4; j++) {
                                    absTime |= ((uint32_t)frame[FRAME_SIZE + j]) << (8 * j);
                                }
                                uint32_t relativeTime = absTime - periodStartTime;
                                for (int i = 0; i < 4; i++) {
                                    frame[FRAME_SIZE + i] = (relativeTime >> (8 * i)) & 0xFF;
                                }                                
                                memcpy(all_values[i], frame, FRAME_SIZE_TS);
                            }

                            memcpy(frame, all_values[0], FRAME_SIZE_TS);
                            uint32_t ts1 = 0;
                            for (int j = 0; j < 4; j++) {
                                ts1 |= ((uint32_t)frame[FRAME_SIZE + j]) << (8 * j);
                            }

                            memcpy(frame, all_values[1], FRAME_SIZE_TS);
                            uint32_t ts2 = 0;
                            for (int j = 0; j < 4; j++) {
                                ts2 |= ((uint32_t)frame[FRAME_SIZE + j]) << (8 * j);
                            }

                            /*fprintf(stderr, "count %d first ts %d second ts %d\n", count, ts1, ts2);
                            fflush(stderr);*/

                            uint32_t size = count * FRAME_SIZE_TS;
                            write(STDOUT_FILENO, &size, sizeof(size));
                            write(STDOUT_FILENO, all_values, size);
                            all_values_size = count;
                        }

                        // fprintf(stderr, "wrote %d in %llu\n", c*batchCount + lastBatchSize, micros_since_epoch() - startTime);
                        // fflush(stderr);
                    }                    
                }
            }
            else if (mode == 0) {
                gpiod_line_set_value(lines[RESET_PIN], 1);
                usleep(10);
                gpiod_line_set_value(lines[RESET_PIN], 0);
                usleep(10);

                struct spi_ioc_transfer trs = {0};
                uint8_t frame[FRAME_SIZE + 2]; 

                gpiod_line_set_value(lines[CONVST_PIN], 1);
                gpiod_line_set_value(lines[CONVST_PIN], 0);

                while(gpiod_line_get_value(lines[BUSY_PIN]));
                
                trs.tx_buf = (unsigned long)NULL;
                trs.rx_buf = (unsigned long)&frame;
                trs.len = FRAME_SIZE + 2;
                trs.delay_usecs = 0;
                trs.speed_hz = 16000000;
                trs.bits_per_word = 8;              

                ioctl(spi_fd, SPI_IOC_MESSAGE(1), &trs);

                /*for (int i = 0; i < FRAME_SIZE; i++)
                    fprintf(stderr, "%02X ", frame[i]);
                fprintf(stderr, "\n");*/

                write(STDOUT_FILENO, frame, FRAME_SIZE + 2);
            }
            else if (mode == 2) {
                fprintf(stderr, "%d\n", gpiod_line_get_value(lines[SIGNAL_PIN]));
                fflush(stderr);
            }
            else if (mode == 3) {
                char ch1offset_str[2];
                memcpy(ch1offset_str, cmd + 4, 1);
                ch1offset_str[1] = '\0';
                uint8_t ch1offset = (uint8_t)strtoul(ch1offset_str, NULL, 16);

                char ch2offset_str[2];
                memcpy(ch2offset_str, cmd + 5, 1);
                ch2offset_str[1] = '\0';
                uint8_t ch2offset = (uint8_t)strtoul(ch2offset_str, NULL, 16);

                char ch3offset_str[2];
                memcpy(ch3offset_str, cmd + 6, 1);
                ch3offset_str[1] = '\0';
                uint8_t ch3offset = (uint8_t)strtoul(ch3offset_str, NULL, 16);

                char ch4offset_str[2];
                memcpy(ch4offset_str, cmd + 7, 1);
                ch4offset_str[1] = '\0';
                uint8_t ch4offset = (uint8_t)strtoul(ch4offset_str, NULL, 16);

                uint64_t startTime = micros_since_epoch();
                const char *dir = "/home/fodorbalint/Documents/"; 
                uint16_t fileNumber = get_highest_number(dir) + 1; // 306 us

                char filename[PATH_MAX];
                if (make_next_filename(filename, sizeof(filename), dir, fileNumber) != 0) {
                    fprintf(stderr, "Filename buffer too small\n");
                    fflush(stderr);
                    usleep(1000);
                    continue;
                } // 322 us
                save_frames_to_file(all_values_size, filename, ch1offset, ch2offset, ch3offset, ch4offset); // 400 ms for 1 s (44000 frames)

                fprintf(stderr, "Saved %d lines to %s in %llu\n", all_values_size, filename, micros_since_epoch() - startTime);
                fflush(stderr);

                /*gpiod_line_set_value(lines[RESET_PIN], 1);
                usleep(1);
                gpiod_line_set_value(lines[RESET_PIN], 0);
                usleep(1);*/

                // 1s 45000 samples: little endian, incorrect: 1772999, 855982, 496568, 484412, 1852891
                // big endian, correct: 527791, 507755, 510069
                // with trigger (40000 frames): 1931852, 837402, 466002, 463821             
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

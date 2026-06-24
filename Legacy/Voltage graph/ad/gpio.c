#include "gpio.h"
#include <gpiod.h>
#include <stdio.h>

static struct gpiod_chip *chip = NULL;
static struct gpiod_line *lines[32] = {0};

void gpio_export(int pin) {
    if (!chip) chip = gpiod_chip_open_by_name("gpiochip0");
    lines[pin] = gpiod_chip_get_line(chip, pin);
}

void gpio_set_dir(int pin, int output) {
    if (output)
        gpiod_line_request_output(lines[pin], "ad7606", 0);
    else
        gpiod_line_request_input(lines[pin], "ad7606");
}

void gpio_write(int pin, int value) {
    gpiod_line_set_value(lines[pin], value);
}

int gpio_read(int pin) {
    return gpiod_line_get_value(lines[pin]);
}

CC=gcc
CFLAGS=-Wall -O2
LDFLAGS=-lgpiod

all: ad7606_reader

ad7606_reader: main.c gpio.c spi.c
	$(CC) $(CFLAGS) -o $@ $^ $(LDFLAGS)

clean:
	rm -f ad7606_reader
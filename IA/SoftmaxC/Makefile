CC = gcc
CFLAGS = -std=c11 -O2
LDLIBS = -lm

SRC = main.c lib.c
OUT = xor_test

.PHONY: all build run test clean

all: build

build:
	$(CC) $(CFLAGS) -o $(OUT) $(SRC) $(LDLIBS)

run: build
	./$(OUT) 0.1 20000 42

# deterministic test (no random seed)
test: build
	./$(OUT) 0.1 20000 -1

clean:
	rm -f $(OUT)

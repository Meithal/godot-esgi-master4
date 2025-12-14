CC=clang
CFLAGS=-Wall -O2 -fPIC
TARGET=libsoftmodel.dylib
SRCS=softmax.c

all: $(TARGET)

$(TARGET): $(SRCS)
	$(CC) $(CFLAGS) -dynamiclib -o $(TARGET) $(SRCS) -lm

clean:
	rm -f $(TARGET)
CC = gcc
CFLAGS = -std=c11 -O2
LDLIBS = -lm

SRC = main.c lib.c
OUT = xor_test
LIB_SRC = model.c
LIB_OUT = libsoftmodel.dylib


.PHONY: all build lib run test clean

all: build lib

build:
	$(CC) $(CFLAGS) -o $(OUT) $(SRC) $(LDLIBS)

lib:
	$(CC) -dynamiclib -o $(LIB_OUT) $(LIB_SRC) $(LDLIBS)

run: build
	./$(OUT) 0.1 20000 42

# deterministic test (no random seed)
test: build
	./$(OUT) 0.1 20000 -1

clean:
	rm -f $(OUT) $(LIB_OUT)

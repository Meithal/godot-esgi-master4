CC = gcc
# Use ?= to allow environment override, but we usually want to append
# So we will just use standard += logic in the targets if needed, or rely on command line override
CFLAGS ?= -std=c11 -O2
LDLIBS = -lm
LDLIBS = -lm

SRC = main.c lib.c
OUT = xor_test
LIB_SRC = model.c
LIB_OUT = libsoftmodel.dylib


.PHONY: all build lib run test clean viz viz_flappy run_viz run_viz_flappy

all: build lib viz viz_flappy
    
# Run visualizations
run_viz: viz
	../Python/venv/bin/python viz_learning.py

run_viz_flappy: viz_flappy
	../Python/venv/bin/python viz_learning.py --flappy

viz: viz_test

viz_flappy: viz_flappy.c lib.c
	$(CC) $(CFLAGS) -o viz_flappy viz_flappy.c lib.c $(LDLIBS)

viz_test: viz_test.c lib.c
	$(CC) $(CFLAGS) -o viz_test viz_test.c lib.c $(LDLIBS)

build:
	$(CC) $(CFLAGS) -o $(OUT) $(SRC) $(LDLIBS)

lib: $(LIB_OUT)

$(LIB_OUT): $(LIB_SRC)
	$(CC) $(CFLAGS) -dynamiclib -o $(LIB_OUT) $(LIB_SRC) $(LDLIBS)

run: build
	./$(OUT) 0.1 20000 42

# deterministic test (no random seed)
test: build
	./$(OUT) 0.1 20000 -1

clean:
	rm -f $(OUT) $(LIB_OUT) viz_test viz_flappy

#!/bin/bash
set -e
echo "Building libsoftmodel.dylib..."
if [ -f Makefile ] || [ -f makefile ]; then
    make
else
    cc -dynamiclib -o libsoftmodel.dylib softmax.c -lm
fi
echo "Done."

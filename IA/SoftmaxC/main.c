#include <stdio.h>

#include "lib.h"

int main() {
    Network net = {0};

    Neuron *x1 = add_neuron(&net, "x1", 1, 0);
    Neuron *x2 = add_neuron(&net, "x2", 1, 0);

    Neuron *h1 = add_neuron(&net, "h1", 0, 0);
    Neuron *h2 = add_neuron(&net, "h2", 0, 0);

    Neuron *o1 = add_neuron(&net, "A", 0, 1);
    Neuron *o2 = add_neuron(&net, "B", 0, 1);

    net.outputs[0] = o1;
    net.outputs[1] = o2;
    net.out_count = 2;

    connect(&net, x1, h1, 0.1);
    connect(&net, x2, h1, 0.1);
    connect(&net, x1, h2, -0.1);
    connect(&net, x2, h2, 0.1);

    connect(&net, h1, o1, 0.1);
    connect(&net, h2, o1, 0.1);
    connect(&net, h1, o2, -0.1);
    connect(&net, h2, o2, 0.1);

    for (int epoch = 0; epoch < 1000; epoch++) {
        x1->value = 1.0;
        x2->value = -1.0;

        forward(&net);
        backprop_softmax(&net, 0, 0.05);
    }

    forward(&net);
    printf("classe = %d\n", classify(&net));
}
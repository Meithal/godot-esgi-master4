#include <stdio.h>
#include <stdlib.h>
#include <time.h>

#include "lib.h"

// Usage: xor_test [lr] [max_epochs] [seed]
// If seed >= 0, weights are randomized with that seed. Otherwise deterministic weights used.
int main(int argc, char **argv) {
    double lr = 0.1;
    int max_epochs = 5000;
    int seed = -1;
    if (argc > 1) lr = atof(argv[1]);
    if (argc > 2) max_epochs = atoi(argv[2]);
    if (argc > 3) seed = atoi(argv[3]);

    if (seed >= 0) srand(seed);

    Network net = {0};

    Neuron *x1 = add_neuron(&net, "x1", 1, 0);
    Neuron *x2 = add_neuron(&net, "x2", 1, 0);

    Neuron *h1 = add_neuron(&net, "h1", 0, 0);
    Neuron *h2 = add_neuron(&net, "h2", 0, 0);

    Neuron *o1 = add_neuron(&net, "o1", 0, 1);
    Neuron *o2 = add_neuron(&net, "o2", 0, 1);

    net.outputs[0] = o1;
    net.outputs[1] = o2;
    net.out_count = 2;

    // Initial connections (deterministic or randomized depending on seed)
    connect(&net, x1, h1, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : 0.5);
    connect(&net, x2, h1, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : 0.5);
    connect(&net, x1, h2, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : 0.5);
    connect(&net, x2, h2, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : -0.5);

    connect(&net, h1, o1, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : 0.5);
    connect(&net, h2, o1, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : -0.5);
    connect(&net, h1, o2, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : -0.5);
    connect(&net, h2, o2, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : 0.5);

    // XOR dataset: inputs and target class (0 or 1)
    double inputs[4][2] = {{0.0, 0.0}, {0.0, 1.0}, {1.0, 0.0}, {1.0, 1.0}};
    int targets[4] = {0, 1, 1, 0};

    for (int epoch = 0; epoch < max_epochs; ++epoch) {
        // Batch (epoch) accumulation of gradients for stability
        double conn_grads[MAX_CONNS];
        double bias_grads[MAX_NEURONS];
        for (int k = 0; k < net.conn_count; ++k) conn_grads[k] = 0.0;
        for (int k = 0; k < net.neuron_count; ++k) bias_grads[k] = 0.0;

        for (int i = 0; i < 4; ++i) {
            x1->value = inputs[i][0];
            x2->value = inputs[i][1];
            forward(&net);
            // compute deltas only
            compute_deltas_softmax(&net, targets[i]);
            // accumulate grads
            for (int k = 0; k < net.conn_count; ++k) {
                Connection *c = &net.conns[k];
                conn_grads[k] += c->from->value * c->to->delta;
            }
            for (int k = 0; k < net.neuron_count; ++k) {
                Neuron *n = &net.neurons[k];
                if (!n->is_input) bias_grads[k] += n->delta;
            }
        }

        // apply average gradient over 4 patterns
        apply_gradients(&net, lr, conn_grads, bias_grads, 4);

        (void)epoch; // no debug prints in production test

        int all_correct = 1;
        for (int i = 0; i < 4; ++i) {
            x1->value = inputs[i][0];
            x2->value = inputs[i][1];
            forward(&net);
            int pred = classify(&net);
            if (pred != targets[i]) { all_correct = 0; break; }
        }
        if (all_correct) { printf("Converged at epoch %d\n", epoch); break; }
        if (epoch == max_epochs - 1) printf("Did not converge after %d epochs\n", max_epochs);
    }

    for (int i = 0; i < 4; ++i) {
        x1->value = inputs[i][0];
        x2->value = inputs[i][1];
        forward(&net);
        double probs[2] = {0.0, 0.0};
        softmax(&net, probs);
        int pred = classify(&net);
        printf("in=(%.1f,%.1f) -> p0=%.4f p1=%.4f pred=%d expected=%d\n",
               inputs[i][0], inputs[i][1], probs[0], probs[1], pred, targets[i]);
    }

    return 0;
}
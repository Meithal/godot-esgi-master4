#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <string.h>

# include "lib.h"


double act_tanh(double z) {
    return tanh(z);
}

double d_tanh_from_value(double a) {
    return 1.0 - a * a;
}

Neuron* add_neuron(Network *net, const char *name, int is_input, int is_output) {
    Neuron *n = &net->neurons[net->neuron_count++];
    memset(n, 0, sizeof(Neuron));
    strcpy(n->name, name);
    n->bias = 0.0;
    n->is_input = is_input;
    n->is_output = is_output;
    return n;
}

void connect(Network *net, Neuron *from, Neuron *to, double w) {
    Connection *c = &net->conns[net->conn_count++];
    c->from = from;
    c->to = to;
    c->weight = w;

    to->in[to->in_count++] = c;
    from->out[from->out_count++] = c;
}

void forward(Network *net) {
    for (int i = 0; i < net->neuron_count; i++) {
        Neuron *n = &net->neurons[i];
        if (n->is_input) continue;

        double z = -n->bias;
        for (int j = 0; j < n->in_count; j++) {
            Connection *c = n->in[j];
            z += c->weight * c->from->value;
        }
        if (n->is_output) {
            // outputs act as logits for softmax (identity)
            n->value = z;
        } else {
            n->value = act_tanh(z);
        }
    }
}

void softmax(Network *net, double *probs) {
    double max = -1e9;
    for (int i = 0; i < net->out_count; i++) {
        double v = net->outputs[i]->value;
        if (v > max) max = v;
    }

    double sum = 0.0;
    for (int i = 0; i < net->out_count; i++) {
        probs[i] = exp(net->outputs[i]->value - max);
        sum += probs[i];
    }

    for (int i = 0; i < net->out_count; i++) {
        probs[i] /= sum;
    }
}

void backprop_softmax(
    Network *net,
    int target_index,
    double lr
) {
    // compute deltas and apply updates (backwards-compatible)
    compute_deltas_softmax(net, target_index);

    // apply gradients immediately
    double conn_grads[MAX_CONNS];
    double bias_grads[MAX_NEURONS];
    for (int i = 0; i < net->conn_count; ++i) conn_grads[i] = 0.0;
    for (int i = 0; i < net->neuron_count; ++i) bias_grads[i] = 0.0;

    // accumulate per-connection gradients based on current deltas
    for (int i = 0; i < net->conn_count; ++i) {
        Connection *c = &net->conns[i];
        conn_grads[i] += c->from->value * c->to->delta;
    }
    for (int i = 0; i < net->neuron_count; ++i) {
        Neuron *n = &net->neurons[i];
        if (!n->is_input) bias_grads[i] += n->delta;
    }

    apply_gradients(net, lr, conn_grads, bias_grads, 1);
}

void compute_deltas_softmax(Network *net, int target_index) {
    double probs[MAX_OUT];
    softmax(net, probs);

    // output deltas (dL/dz for logits)
    for (int i = 0; i < net->out_count; i++) {
        double y = (i == target_index) ? 1.0 : 0.0;
        net->outputs[i]->delta = probs[i] - y;
    }

    // hidden deltas
    for (int i = net->neuron_count - 1; i >= 0; i--) {
        Neuron *n = &net->neurons[i];
        if (n->is_output || n->is_input) continue;

        double err = 0.0;
        for (int j = 0; j < n->out_count; j++) {
            Connection *c = n->out[j];
            err += c->weight * c->to->delta;
        }

        n->delta = err * d_tanh_from_value(n->value);
    }
}

void apply_gradients(Network *net, double lr, double *conn_grads, double *bias_grads, int normalize_by) {
    double norm = (normalize_by > 0) ? (double)normalize_by : 1.0;
    // update weights
    for (int i = 0; i < net->conn_count; i++) {
        Connection *c = &net->conns[i];
        c->weight -= lr * (conn_grads[i] / norm);
    }

    // update biases
    for (int i = 0; i < net->neuron_count; i++) {
        Neuron *n = &net->neurons[i];
        if (!n->is_input)
            // forward uses z = -bias + sum(w*x), so gradient dL/dbias = -delta
            // therefore bias must be updated with opposite sign
            n->bias += lr * (bias_grads[i] / norm);
    }
}

int classify(Network *net) {
    double probs[MAX_OUT];
    softmax(net, probs);

    int best = 0;
    for (int i = 1; i < net->out_count; i++)
        if (probs[i] > probs[best])
            best = i;

    return best;
}


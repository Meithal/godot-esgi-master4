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
        n->value = act_tanh(z);
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
    double probs[MAX_OUT];
    softmax(net, probs);

    // delta sortie
    for (int i = 0; i < net->out_count; i++) {
        double y = (i == target_index) ? 1.0 : 0.0;
        net->outputs[i]->delta = probs[i] - y;
    }

    // delta cachées
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

    // update poids
    for (int i = 0; i < net->conn_count; i++) {
        Connection *c = &net->conns[i];
        c->weight -= lr * c->from->value * c->to->delta;
    }

    // update biais
    for (int i = 0; i < net->neuron_count; i++) {
        Neuron *n = &net->neurons[i];
        if (!n->is_input)
            n->bias -= lr * n->delta;
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


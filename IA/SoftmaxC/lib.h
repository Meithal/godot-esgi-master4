#define MAX_IN 16
#define MAX_OUT 16
#define MAX_NEURONS 128
#define MAX_CONNS 256


typedef struct Neuron Neuron;

typedef struct {
    Neuron *from;
    Neuron *to;
    double weight;
} Connection;

struct Neuron {
    char name[32];

    Connection *in[MAX_IN];
    int in_count;

    Connection *out[MAX_OUT];
    int out_count;

    double bias;
    double value;   // activation
    double delta;   // dL/dz

    int is_input;
    int is_output;
};

typedef struct {
    Neuron neurons[MAX_NEURONS];
    int neuron_count;

    Connection conns[MAX_CONNS];
    int conn_count;

    Neuron *outputs[MAX_OUT];
    int out_count;
} Network;

Neuron* add_neuron(Network *net, const char *name, int is_input, int is_output);
void connect(Network *net, Neuron *from, Neuron *to, double w);
void forward(Network *net);
void softmax(Network *net, double *probs);
void backprop_softmax(
    Network *net,
    int target_index,
    double lr
);
int classify(Network *net);

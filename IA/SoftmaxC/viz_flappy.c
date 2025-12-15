#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#include "lib.h"

#define MAX_SAMPLES 10000
#define MAX_INPUTS 20
#define MAX_HIDDEN_LAYERS 5
#define MAX_LAYER_SIZE 32

// Feature names for visualization (order matches training script)
const char *FEATURE_NAMES[] = {"vs", "dr", "dx", "p", "db", "dt", "tti"};
const int NUM_FEATURE_NAMES = 7;

typedef struct {
  double inputs[MAX_INPUTS];
  int target;
} Sample;

typedef struct {
  int n_inputs;
  int n_layers;
  int layer_sizes[MAX_HIDDEN_LAYERS + 1]; // includes output layer
} ModelTopology;

Sample dataset[MAX_SAMPLES];
int data_count = 0;

// Read model topology from model_weights.txt
int read_model_topology(const char *filename, ModelTopology *topo) {
  FILE *f = fopen(filename, "r");
  if (!f) {
    fprintf(stderr, "Warning: Could not open %s, using default topology\n",
            filename);
    return 0;
  }

  char line[1024];
  if (!fgets(line, sizeof(line), f)) {
    fclose(f);
    return 0;
  }

  // Parse: MLP n_inputs n_layers layer_size1 layer_size2 ... layer_sizeN
  char header[16];
  int parsed =
      sscanf(line, "%s %d %d", header, &topo->n_inputs, &topo->n_layers);

  if (parsed != 3 || strcmp(header, "MLP") != 0) {
    fclose(f);
    return 0;
  }

  // Read layer sizes
  char *ptr = line;
  // Skip "MLP n_inputs n_layers"
  for (int i = 0; i < 3; i++) {
    while (*ptr && *ptr != ' ')
      ptr++;
    while (*ptr == ' ')
      ptr++;
  }

  for (int i = 0; i < topo->n_layers && i < MAX_HIDDEN_LAYERS + 1; i++) {
    topo->layer_sizes[i] = atoi(ptr);
    while (*ptr && *ptr != ' ')
      ptr++;
    while (*ptr == ' ')
      ptr++;
  }

  fclose(f);
  fprintf(stderr, "Read topology: %d inputs, %d layers [", topo->n_inputs,
          topo->n_layers);
  for (int i = 0; i < topo->n_layers; i++) {
    fprintf(stderr, "%d%s", topo->layer_sizes[i],
            i < topo->n_layers - 1 ? ", " : "");
  }
  fprintf(stderr, "]\\n");

  return 1;
}

void load_data(const char *filename, int n_inputs) {
  FILE *f = fopen(filename, "r");
  if (!f) {
    fprintf(stderr, "Error opening data file: %s\\n", filename);
    exit(1);
  }

  char line[2048];
  while (fgets(line, sizeof(line), f) && data_count < MAX_SAMPLES) {
    char *ptr = line;
    char *end;
    for (int i = 0; i < n_inputs; ++i) {
      dataset[data_count].inputs[i] = strtod(ptr, &end);
      if (ptr == end)
        break;
      ptr = end;
    }
    dataset[data_count].target = (int)strtod(ptr, &end);
    data_count++;
  }
  fclose(f);
  fprintf(stderr, "Loaded %d samples\\n", data_count);
}

// Usage: viz_flappy [lr] [max_epochs] [seed] [snapshot_interval] [data_file]
int main(int argc, char **argv) {
  double lr = 0.05;
  int max_epochs = 2000;
  int seed = 42;
  int snapshot_interval = 50;
  const char *data_file = "viz_data.txt";
  const char *model_file = "model_weights.txt";

  if (argc > 1)
    lr = atof(argv[1]);
  if (argc > 2)
    max_epochs = atoi(argv[2]);
  if (argc > 3)
    seed = atoi(argv[3]);
  if (argc > 4)
    snapshot_interval = atoi(argv[4]);
  if (argc > 5)
    data_file = argv[5];

  if (seed >= 0)
    srand(seed);

  // Read model topology from model_weights.txt
  ModelTopology topo;
  if (!read_model_topology(model_file, &topo)) {
    // Default topology: 7 inputs, 2 hidden layers [16, 8], 1 output
    fprintf(stderr, "Using default topology: 7-16-8-1\\n");
    topo.n_inputs = 7;
    topo.n_layers = 3;
    topo.layer_sizes[0] = 16;
    topo.layer_sizes[1] = 8;
    topo.layer_sizes[2] = 1;
  }

  load_data(data_file, topo.n_inputs);
  if (data_count == 0)
    return 0;

  Network net = {0};

  // Create input neurons
  Neuron *inputs[MAX_INPUTS];
  for (int i = 0; i < topo.n_inputs; ++i) {
    const char *name = (i < NUM_FEATURE_NAMES) ? FEATURE_NAMES[i] : "in";
    inputs[i] = add_neuron(&net, name, 1, 0);
  }

  // Create hidden layers dynamically
  Neuron *layers[MAX_HIDDEN_LAYERS][MAX_LAYER_SIZE];
  int num_hidden = topo.n_layers - 1; // Exclude output layer

  for (int layer = 0; layer < num_hidden; layer++) {
    int layer_size = topo.layer_sizes[layer];
    int prev_size = (layer == 0) ? topo.n_inputs : topo.layer_sizes[layer - 1];

    for (int i = 0; i < layer_size; ++i) {
      char name[16];
      snprintf(name, 16, "h%d_%d", layer + 1, i);
      layers[layer][i] = add_neuron(&net, name, 0, 0);

      // Connect to previous layer
      if (layer == 0) {
        for (int j = 0; j < topo.n_inputs; ++j) {
          connect(&net, inputs[j], layers[layer][i],
                  (seed >= 0) ? ((double)rand() / RAND_MAX * 0.2) - 0.1 : 0.0);
        }
      } else {
        for (int j = 0; j < prev_size; ++j) {
          connect(&net, layers[layer - 1][j], layers[layer][i],
                  (seed >= 0) ? ((double)rand() / RAND_MAX * 0.2) - 0.1 : 0.0);
        }
      }
    }
  }

  // Create output neurons (always 2 for softmax binary classification)
  Neuron *o0 = add_neuron(&net, "No", 0, 1);
  Neuron *o1 = add_neuron(&net, "Jump", 0, 1);
  net.outputs[0] = o0;
  net.outputs[1] = o1;
  net.out_count = 2;

  // Connect last hidden layer to outputs
  int last_hidden_size = topo.layer_sizes[num_hidden - 1];
  for (int i = 0; i < last_hidden_size; ++i) {
    connect(&net, layers[num_hidden - 1][i], o0,
            (seed >= 0) ? ((double)rand() / RAND_MAX * 0.2) - 0.1 : 0.0);
    connect(&net, layers[num_hidden - 1][i], o1,
            (seed >= 0) ? ((double)rand() / RAND_MAX * 0.2) - 0.1 : 0.0);
  }

  printf("[\n");

  for (int epoch = 0; epoch <= max_epochs; ++epoch) {
    double conn_grads[MAX_CONNS];
    double bias_grads[MAX_NEURONS];
    for (int k = 0; k < net.conn_count; ++k)
      conn_grads[k] = 0.0;
    for (int k = 0; k < net.neuron_count; ++k)
      bias_grads[k] = 0.0;

    double total_loss = 0.0;
    int correct = 0;

    for (int i = 0; i < data_count; ++i) {
      for (int k = 0; k < topo.n_inputs; ++k)
        inputs[k]->value = dataset[i].inputs[k];

      forward(&net);

      double probs[MAX_OUT];
      softmax(&net, probs);

      // Target
      int t = dataset[i].target; // 0 or 1
      double p = probs[t];
      if (p < 1e-9)
        p = 1e-9;
      total_loss -= log(p);

      int pred = (probs[1] > probs[0]) ? 1 : 0;
      if (pred == t)
        correct++;

      compute_deltas_softmax(&net, t);

      for (int k = 0; k < net.conn_count; ++k) {
        Connection *c = &net.conns[k];
        conn_grads[k] += c->from->value * c->to->delta;
      }
      for (int k = 0; k < net.neuron_count; ++k) {
        Neuron *n = &net.neurons[k];
        if (!n->is_input)
          bias_grads[k] += n->delta;
      }
    }

    apply_gradients(&net, lr, conn_grads, bias_grads, data_count);

    if (epoch % snapshot_interval == 0 || epoch == max_epochs) {
      if (epoch > 0)
        printf(",\n");

      printf("  {\"epoch\": %d, \"loss\": %.4f, \"accuracy\": %.2f, ", epoch,
             total_loss / data_count, (double)correct / data_count);

      printf("\"neurons\": [");
      for (int i = 0; i < net.neuron_count; ++i) {
        Neuron *n = &net.neurons[i];
        if (i > 0)
          printf(", ");
        printf("{\"name\": \"%s\", \"bias\": %.4f, \"value\": "
               "%.4f, \"delta\": "
               "%.4f}",
               n->name, n->bias, n->value, n->delta);
      }
      printf("], ");

      printf("\"connections\": [");
      for (int i = 0; i < net.conn_count; ++i) {
        Connection *c = &net.conns[i];
        if (i > 0)
          printf(", ");
        printf("{\"from\": \"%s\", \"to\": \"%s\", "
               "\"weight\": %.4f}",
               c->from->name, c->to->name, c->weight);
      }
      printf("]}");
    }
  }
  printf("\n]\n");

  return 0;
}

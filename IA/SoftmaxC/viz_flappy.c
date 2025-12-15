#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#include "lib.h"

#define MAX_SAMPLES 10000
#define NUM_INPUTS 10

typedef struct {
  double inputs[NUM_INPUTS];
  int target;
} Sample;

Sample dataset[MAX_SAMPLES];
int data_count = 0;

void load_data(const char *filename) {
  FILE *f = fopen(filename, "r");
  if (!f) {
    fprintf(stderr, "Error opening data file: %s\n", filename);
    exit(1);
  }

  char line[2048];
  while (fgets(line, sizeof(line), f) && data_count < MAX_SAMPLES) {
    char *ptr = line;
    char *end;
    for (int i = 0; i < NUM_INPUTS; ++i) {
      dataset[data_count].inputs[i] = strtod(ptr, &end);
      if (ptr == end)
        break;
      ptr = end;
    }
    dataset[data_count].target = (int)strtod(ptr, &end);
    data_count++;
  }
  fclose(f);
  fprintf(stderr, "Loaded %d samples\n", data_count);
}

// Usage: viz_flappy [lr] [max_epochs] [seed] [snapshot_interval] [data_file]
int main(int argc, char **argv) {
  double lr = 0.05;
  int max_epochs = 2000;
  int seed = 42;
  int snapshot_interval = 50;
  const char *data_file = "viz_data.txt";

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

  load_data(data_file);
  if (data_count == 0)
    return 0;

  Network net = {0};

  // Inputs
  char *input_names[] = {"fh", "fx", "vs", "dr", "dx",
                         "oy", "p",  "db", "dt", "tti"};
  Neuron *inputs[NUM_INPUTS];
  for (int i = 0; i < NUM_INPUTS; ++i) {
    inputs[i] = add_neuron(&net, input_names[i], 1, 0);
  }

  // Hidden 1 (16)
  Neuron *h1[16];
  for (int i = 0; i < 16; ++i) {
    char name[16];
    snprintf(name, 16, "h1_%d", i);
    h1[i] = add_neuron(&net, name, 0, 0);
    for (int j = 0; j < NUM_INPUTS; ++j) {
      connect(&net, inputs[j], h1[i],
              (seed >= 0) ? ((double)rand() / RAND_MAX * 0.2) - 0.1 : 0.0);
    }
  }

  // Hidden 2 (8)
  Neuron *h2[8];
  for (int i = 0; i < 8; ++i) {
    char name[16];
    snprintf(name, 16, "h2_%d", i);
    h2[i] = add_neuron(&net, name, 0, 0);
    for (int j = 0; j < 16; ++j) {
      connect(&net, h1[j], h2[i],
              (seed >= 0) ? ((double)rand() / RAND_MAX * 0.2) - 0.1 : 0.0);
    }
  }

  // Output: 2 neurons for Softmax (0=No Jump, 1=Jump)
  Neuron *o0 = add_neuron(&net, "No", 0, 1);
  Neuron *o1 = add_neuron(&net, "Jump", 0, 1);
  net.outputs[0] = o0;
  net.outputs[1] = o1;
  net.out_count = 2;

  for (int i = 0; i < 8; ++i) {
    connect(&net, h2[i], o0,
            (seed >= 0) ? ((double)rand() / RAND_MAX * 0.2) - 0.1 : 0.0);
    connect(&net, h2[i], o1,
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
      for (int k = 0; k < NUM_INPUTS; ++k)
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
        printf("{\"name\": \"%s\", \"bias\": %.4f, \"value\": %.4f, \"delta\": "
               "%.4f}",
               n->name, n->bias, n->value, n->delta);
      }
      printf("], ");

      printf("\"connections\": [");
      for (int i = 0; i < net.conn_count; ++i) {
        Connection *c = &net.conns[i];
        if (i > 0)
          printf(", ");
        printf("{\"from\": \"%s\", \"to\": \"%s\", \"weight\": %.4f}",
               c->from->name, c->to->name, c->weight);
      }
      printf("]}");
    }
  }
  printf("\n]\n");

  return 0;
}

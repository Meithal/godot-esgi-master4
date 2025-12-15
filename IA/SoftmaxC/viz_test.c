#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>

#include "lib.h"

// Usage: viz_test [lr] [max_epochs] [seed]
// Outputs CSV to stdout: epoch,loss,accuracy
// Usage: viz_test [lr] [max_epochs] [seed] [snapshot_interval]
// Outputs JSON lines to stdout
int main(int argc, char **argv) {
  double lr = 0.1;
  int max_epochs = 5000;
  int seed = 42;
  int snapshot_interval = 100;

  if (argc > 1)
    lr = atof(argv[1]);
  if (argc > 2)
    max_epochs = atoi(argv[2]);
  if (argc > 3)
    seed = atoi(argv[3]);
  if (argc > 4)
    snapshot_interval = atoi(argv[4]);

  if (seed >= 0)
    srand(seed);

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

  // Initial connections
  connect(&net, x1, h1, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : 0.5);
  connect(&net, x2, h1, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : 0.5);
  connect(&net, x1, h2, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : 0.5);
  connect(&net, x2, h2, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : -0.5);

  connect(&net, h1, o1, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : 0.5);
  connect(&net, h2, o1, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : -0.5);
  connect(&net, h1, o2, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : -0.5);
  connect(&net, h2, o2, (seed >= 0) ? ((double)rand() / RAND_MAX) - 0.5 : 0.5);

  double inputs[4][2] = {{0.0, 0.0}, {0.0, 1.0}, {1.0, 0.0}, {1.0, 1.0}};
  int targets[4] = {0, 1, 1, 0};

  printf("[\n"); // Start JSON array

  for (int epoch = 0; epoch <= max_epochs; ++epoch) {

    // Train on all patterns
    double conn_grads[MAX_CONNS];
    double bias_grads[MAX_NEURONS];
    for (int k = 0; k < net.conn_count; ++k)
      conn_grads[k] = 0.0;
    for (int k = 0; k < net.neuron_count; ++k)
      bias_grads[k] = 0.0;

    double total_loss = 0.0;
    int correct = 0;

    for (int i = 0; i < 4; ++i) {
      x1->value = inputs[i][0];
      x2->value = inputs[i][1];
      forward(&net);

      compute_deltas_softmax(&net, targets[i]);

      // Metrics
      double probs[MAX_OUT];
      softmax(&net, probs);
      total_loss -= log(probs[targets[i]] + 1e-9);
      if ((probs[1] > probs[0] && targets[i] == 1) ||
          (probs[0] > probs[1] && targets[i] == 0))
        correct++;

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

    apply_gradients(&net, lr, conn_grads, bias_grads, 4);

    // Snapshot
    if (epoch % snapshot_interval == 0 || epoch == max_epochs) {
      if (epoch > 0)
        printf(",\n");

      printf("  {\"epoch\": %d, \"loss\": %.4f, \"accuracy\": %.2f, ", epoch,
             total_loss / 4.0, (double)correct / 4.0);

      // Neurons
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

      // Connections
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
  printf("\n]\n"); // End JSON array
  return 0;
}

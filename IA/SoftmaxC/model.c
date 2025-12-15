#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int loaded = 0;

/* MLP structures - Dynamic */
static int mlp_nin = 0;
static int mlp_num_layers = 0;
static int *mlp_layer_sizes = NULL;

static double *mlp_means = NULL;
static double *mlp_stds = NULL;

/* weights */
static double ***mlp_W = NULL;
static double **mlp_b = NULL;

static double sigmoid(double x) { return 1.0 / (1.0 + exp(-x)); }
static double tanh_activation(double x) { return tanh(x); }

/* helper: read a line and parse expected_count doubles into dest */
static int parse_line_doubles(char *line, double *dest, int expected_count) {
  char *tok = strtok(line, " \t\n\r");
  int i = 0;
  while (tok && i < expected_count) {
    char *endptr = NULL;
    dest[i] = strtod(tok, &endptr);
    if (endptr == tok)
      return -1;
    i++;
    tok = strtok(NULL, " \t\n\r");
  }
  return (i == expected_count) ? 0 : -1;
}

static void try_load() {
  if (loaded)
    return;

  const char *paths[] = {"model_weights.txt",
                         "../IA/SoftmaxC/model_weights.txt",
                         "IA/SoftmaxC/model_weights.txt"};

  FILE *f = NULL;
  for (int i = 0; i < 3; ++i) {
    f = fopen(paths[i], "r");
    if (f) {
      break;
    }
  }

  if (!f) {
    return;
  }

  char buf[4096];
  if (!fgets(buf, sizeof(buf), f)) {
    fclose(f);
    loaded = 1;
    return;
  }

  /* Check for MLP header */
  if (strncmp(buf, "MLP", 3) != 0) {
    /* Invalid format (or legacy), just fail silently or keep default 0 output
     */
    fclose(f);
    loaded = 1;
    return;
  }

  /* Parse MLP header: MLP <nin> <nlayers> <l1> <l2> ... */
  char *ptr = buf + 4; // Skip "MLP "
  mlp_nin = (int)strtol(ptr, &ptr, 10);
  mlp_num_layers = (int)strtol(ptr, &ptr, 10);

  if (mlp_num_layers <= 0) {
    fclose(f);
    loaded = 1;
    return;
  }

  /* Allocate layer sizes */
  mlp_layer_sizes = (int *)malloc(sizeof(int) * mlp_num_layers);
  for (int k = 0; k < mlp_num_layers; ++k) {
    mlp_layer_sizes[k] = (int)strtol(ptr, &ptr, 10);
  }

  /* Read normalization stats (means, stds) */
  mlp_means = (double *)malloc(sizeof(double) * mlp_nin);
  mlp_stds = (double *)malloc(sizeof(double) * mlp_nin);

  /* Line 2: Means */
  if (!fgets(buf, sizeof(buf), f) ||
      parse_line_doubles(buf, mlp_means, mlp_nin) != 0) {
    fclose(f);
    loaded = 1;
    return;
  }

  /* Line 3: Stds */
  if (!fgets(buf, sizeof(buf), f) ||
      parse_line_doubles(buf, mlp_stds, mlp_nin) != 0) {
    fclose(f);
    loaded = 1;
    return;
  }

  /* Allocate structure */
  mlp_W = (double ***)malloc(sizeof(double **) * mlp_num_layers);
  mlp_b = (double **)malloc(sizeof(double *) * mlp_num_layers);

  /* Read layers */
  for (int k = 0; k < mlp_num_layers; ++k) {
    int din = (k == 0) ? mlp_nin : mlp_layer_sizes[k - 1];
    int dout = mlp_layer_sizes[k];

    /* Allocate W[k] (rows = dout) */
    mlp_W[k] = (double **)malloc(sizeof(double *) * dout);
    for (int r = 0; r < dout; ++r) {
      mlp_W[k][r] = (double *)malloc(sizeof(double) * din);
      if (!fgets(buf, sizeof(buf), f) ||
          parse_line_doubles(buf, mlp_W[k][r], din) != 0) {
        fclose(f);
        loaded = 1;
        return;
      }
    }

    /* Allocate b[k] (dout values) */
    mlp_b[k] = (double *)malloc(sizeof(double) * dout);
    if (!fgets(buf, sizeof(buf), f) ||
        parse_line_doubles(buf, mlp_b[k], dout) != 0) {
      fclose(f);
      loaded = 1;
      return;
    }
  }

  fclose(f);
  loaded = 1;
}

int predict(double fh, double fx, double vs, double distRoof, double dx,
            double oy, double passes, double distBottom, double distTop,
            double tti) {
  try_load();

  if (!mlp_W)
    return 0; /* Model not loaded or failed */

/* Normalization */
/* Support up to 32 inputs for safety on stack */
#define MAX_IN 32
#define MAX_WIDTH 128

  if (mlp_nin > MAX_IN) {
    return 0; /* Too many inputs for static buffer */
  }

  double inputs[MAX_IN];
  double current_act[MAX_WIDTH];
  double next_act[MAX_WIDTH];

  /* Populate inputs */
  double raw_in[MAX_IN];
  raw_in[0] = fh;
  raw_in[1] = fx;
  raw_in[2] = vs;
  raw_in[3] = distRoof;
  raw_in[4] = dx;
  raw_in[5] = oy;
  raw_in[6] = passes;
  raw_in[7] = distBottom;
  raw_in[8] = distTop;
  raw_in[9] = tti;

  /* Zero out remainder */
  for (int i = 10; i < mlp_nin; ++i)
    raw_in[i] = 0.0;

  /* Normalize */
  for (int i = 0; i < mlp_nin; ++i) {
    inputs[i] = (raw_in[i] - mlp_means[i]) / mlp_stds[i];
  }

  double *in_ptr = inputs;
  int in_size = mlp_nin;

  for (int k = 0; k < mlp_num_layers; ++k) {
    int dout = mlp_layer_sizes[k];
    if (dout > MAX_WIDTH)
      return 0; /* Exceeds stack buffer */

    for (int r = 0; r < dout; ++r) {
      double s = mlp_b[k][r];
      /* Dot product */
      double *row = mlp_W[k][r];
      for (int c = 0; c < in_size; ++c) {
        s += row[c] * in_ptr[c];
      }

      /* Activation */
      if (k == mlp_num_layers - 1) {
        /* Output layer -> Sigmoid */
        next_act[r] = sigmoid(s);
      } else {
        /* Hidden layer -> Tanh */
        next_act[r] = tanh_activation(s);
      }
    }

    /* Prepare for next layer */
    for (int i = 0; i < dout; ++i)
      current_act[i] = next_act[i];

    in_ptr = current_act;
    in_size = dout;
  }

  /* Final output is 1st element of last result */
  double p = in_ptr[0];
  return (p >= 0.5) ? 1 : 0;
}

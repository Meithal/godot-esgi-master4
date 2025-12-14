#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

static int loaded = 0;
static int model_type = 0; /* 0=none, 1=linear, 2=mlp */

/* Linear fallback */
static double lin_weights[7] = {0};
static double lin_bias = 0.0;

/* MLP structures */
static int mlp_nin = 0, mlp_h1 = 0, mlp_h2 = 0;
static double *mlp_means = NULL;
static double *mlp_stds = NULL;
static double **mlp_W1 = NULL; /* h1 x nin */
static double *mlp_b1 = NULL;  /* h1 */
static double **mlp_W2 = NULL; /* h2 x h1 */
static double *mlp_b2 = NULL;  /* h2 */
static double *mlp_W3 = NULL;  /* h2 */
static double mlp_b3 = 0.0;

static double sigmoid(double x) { return 1.0 / (1.0 + exp(-x)); }

/* helper: read a line and parse expected_count doubles into dest (must be allocated) */
static int parse_line_doubles(char *line, double *dest, int expected_count) {
    char *tok = strtok(line, " \t\n\r");
    int i = 0;
    while (tok && i < expected_count) {
        char *endptr = NULL;
        dest[i] = strtod(tok, &endptr);
        if (endptr == tok) return -1;
        i++;
        tok = strtok(NULL, " \t\n\r");
    }
    return (i == expected_count) ? 0 : -1;
}

static void try_load() {
    if (loaded) return;
    FILE *f = fopen("model_weights.txt", "r");
    if (!f) { loaded = 1; return; }

    char buf[4096];
    if (!fgets(buf, sizeof(buf), f)) { fclose(f); loaded = 1; return; }
    if (strncmp(buf, "MLP", 3) == 0) {
        /* parse header: MLP n_in h1 h2 */
        if (sscanf(buf, "MLP %d %d %d", &mlp_nin, &mlp_h1, &mlp_h2) != 3) {
            fclose(f); loaded = 1; return;
        }
        /* allocate means/stds */
        mlp_means = (double*)malloc(sizeof(double)*mlp_nin);
        mlp_stds = (double*)malloc(sizeof(double)*mlp_nin);
        /* read means */
        if (!fgets(buf, sizeof(buf), f)) { fclose(f); loaded = 1; return; }
        if (parse_line_doubles(buf, mlp_means, mlp_nin) != 0) { fclose(f); loaded = 1; return; }
        /* read stds */
        if (!fgets(buf, sizeof(buf), f)) { fclose(f); loaded = 1; return; }
        if (parse_line_doubles(buf, mlp_stds, mlp_nin) != 0) { fclose(f); loaded = 1; return; }

        /* allocate W1 and read h1 rows */
        mlp_W1 = (double**)malloc(sizeof(double*)*mlp_h1);
        for (int i = 0; i < mlp_h1; ++i) {
            mlp_W1[i] = (double*)malloc(sizeof(double)*mlp_nin);
            if (!fgets(buf, sizeof(buf), f)) { fclose(f); loaded = 1; return; }
            if (parse_line_doubles(buf, mlp_W1[i], mlp_nin) != 0) { fclose(f); loaded = 1; return; }
        }
        /* read b1 */
        mlp_b1 = (double*)malloc(sizeof(double)*mlp_h1);
        if (!fgets(buf, sizeof(buf), f)) { fclose(f); loaded = 1; return; }
        if (parse_line_doubles(buf, mlp_b1, mlp_h1) != 0) { fclose(f); loaded = 1; return; }

        /* W2 */
        mlp_W2 = (double**)malloc(sizeof(double*)*mlp_h2);
        for (int i = 0; i < mlp_h2; ++i) {
            mlp_W2[i] = (double*)malloc(sizeof(double)*mlp_h1);
            if (!fgets(buf, sizeof(buf), f)) { fclose(f); loaded = 1; return; }
            if (parse_line_doubles(buf, mlp_W2[i], mlp_h1) != 0) { fclose(f); loaded = 1; return; }
        }
        mlp_b2 = (double*)malloc(sizeof(double)*mlp_h2);
        if (!fgets(buf, sizeof(buf), f)) { fclose(f); loaded = 1; return; }
        if (parse_line_doubles(buf, mlp_b2, mlp_h2) != 0) { fclose(f); loaded = 1; return; }

        /* W3 (single row of h2) */
        mlp_W3 = (double*)malloc(sizeof(double)*mlp_h2);
        if (!fgets(buf, sizeof(buf), f)) { fclose(f); loaded = 1; return; }
        if (parse_line_doubles(buf, mlp_W3, mlp_h2) != 0) { fclose(f); loaded = 1; return; }
        /* b3 */
        if (!fgets(buf, sizeof(buf), f)) { fclose(f); loaded = 1; return; }
        if (sscanf(buf, "%lf", &mlp_b3) != 1) { fclose(f); loaded = 1; return; }

        model_type = 2;
        fclose(f);
        loaded = 1;
        return;
    }

    /* Not MLP: fall back to legacy linear format. Try to parse up to 7 doubles + bias from file start. */
    rewind(f);
    int read = 0;
    for (int i = 0; i < 7; ++i) {
        if (fscanf(f, "%lf", &lin_weights[i]) == 1) read++; else lin_weights[i] = 0.0;
    }
    if (fscanf(f, "%lf", &lin_bias) != 1) lin_bias = 0.0;
    if (read > 0) model_type = 1; else model_type = 0;
    fclose(f);
    loaded = 1;
}

static double tanh_activation(double x) { return tanh(x); }

int predict(double fh, double fx, double vs, double distRoof, double dx, double oy, double passes) {
    try_load();
    if (model_type == 2) {
        /* normalize inputs */
        double x[16]; /* small fixed buffer for up to 16 inputs */
        if (mlp_nin > 16) return 0; /* unexpected */
        double invals[16];
        invals[0] = fh; invals[1] = fx; invals[2] = vs; invals[3] = distRoof; invals[4] = dx; invals[5] = oy;
        if (mlp_nin == 7) invals[6] = passes;
        for (int i = 0; i < mlp_nin; ++i) x[i] = (invals[i] - mlp_means[i]) / mlp_stds[i];
        /* layer1 */
        double a1[64];
        if (mlp_h1 > 64) return 0;
        for (int i = 0; i < mlp_h1; ++i) {
            double s = mlp_b1[i];
            for (int j = 0; j < mlp_nin; ++j) s += mlp_W1[i][j] * x[j];
            a1[i] = tanh_activation(s);
        }
        /* layer2 */
        double a2[64];
        if (mlp_h2 > 64) return 0;
        for (int i = 0; i < mlp_h2; ++i) {
            double s = mlp_b2[i];
            for (int j = 0; j < mlp_h1; ++j) s += mlp_W2[i][j] * a1[j];
            a2[i] = tanh_activation(s);
        }
        /* output */
        double s = mlp_b3;
        for (int j = 0; j < mlp_h2; ++j) s += mlp_W3[j] * a2[j];
        double p = sigmoid(s);
        return (p >= 0.5) ? 1 : 0;
    } else if (model_type == 1) {
        double sum = fh*lin_weights[0] + fx*lin_weights[1] + vs*lin_weights[2] + distRoof*lin_weights[3] + dx*lin_weights[4] + oy*lin_weights[5] + passes*lin_weights[6] + lin_bias;
        double p = sigmoid(sum);
        return (p >= 0.5) ? 1 : 0;
    }
    return 0;
}

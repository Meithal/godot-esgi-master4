// Simple C wrapper that reads model_weights.txt and exposes a predict(...) function
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <dlfcn.h>

static double weights[7]; // 6 features + bias
static int weights_loaded = 0;

static void load_weights()
{
    if (weights_loaded) return;
    const char *fname = "model_weights.txt";
    char path[4096] = {0};

    Dl_info info;
    if (dladdr((void*)&load_weights, &info) && info.dli_fname) {
        // build path relative to the shared lib
        const char *libpath = info.dli_fname;
        strncpy(path, libpath, sizeof(path)-1);
        char *p = strrchr(path, '/');
        if (p) *(p+1) = '\0'; // keep trailing slash
        strncat(path, fname, sizeof(path)-strlen(path)-1);
    }

    FILE *f = NULL;
    if (path[0]) f = fopen(path, "r");
    if (!f) f = fopen(fname, "r"); // fallback to CWD
    if (!f) {
        // Could not find weights, default to zeros
        for (int i = 0; i < 7; ++i) weights[i] = 0.0;
        weights_loaded = 1;
        return;
    }

    for (int i = 0; i < 7; ++i) {
        if (fscanf(f, "%lf", &weights[i]) != 1) weights[i] = 0.0;
    }
    fclose(f);
    weights_loaded = 1;
}

__attribute__((visibility("default")))
int predict(double flappyHeight, double flappyX, double verticalSpeed, double distToRoof, double obs_dx, double obs_y)
{
    load_weights();
    double z = 0.0;
    // weights: w0..w5, bias at index 6
    z += weights[0]*flappyHeight;
    z += weights[1]*flappyX;
    z += weights[2]*verticalSpeed;
    z += weights[3]*distToRoof;
    z += weights[4]*obs_dx;
    z += weights[5]*obs_y;
    z += weights[6];

    double p = 1.0 / (1.0 + exp(-z));
    return p >= 0.5 ? 1 : 0;
}

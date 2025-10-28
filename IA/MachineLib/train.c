#include <stdio.h>
#include <stdlib.h>
#include "Machinelib.h"

int main(int argc, char *argv[]) {
    if(argc < 3) {
        printf("Usage: %s <binary_data_file> <output_weights_file>\n", argv[0]);
        return 1;
    }

    FILE *fp = fopen(argv[1], "rb");
    if(!fp) { perror("fopen input"); return 1; }

    int model_type;
    int n_samples;
    int input_size;

    // Lire les infos principales
    fread(&model_type, sizeof(int), 1, fp);
    fread(&n_samples, sizeof(int), 1, fp);
    fread(&input_size, sizeof(int), 1, fp);

    // Allocation X
    double **X = (double **)malloc(n_samples * sizeof(double *));
    for(int i = 0; i < n_samples; i++)
        X[i] = (double *)malloc(input_size * sizeof(double));

    // Lire X
    for(int i = 0; i < n_samples; i++)
        fread(X[i], sizeof(double), input_size, fp);

    Model *model = NULL;
    FILE *out_fp = fopen(argv[2], "wb");
    if(!out_fp) { perror("fopen output"); return 1; }

    // --- Linear classifier ---
    if(model_type == 0) {
        int *Y = (int *)malloc(n_samples * sizeof(int));
        fread(Y, sizeof(int), n_samples, fp);

        model = init_linear_classifier(input_size, 0.1, 100);
        train_linear_classifier(model, X, Y, n_samples);

        printf("Training finished (Linear Classifier).\n");
        for(int i = 0; i < input_size; i++)
            printf("w[%d] = %f\n", i, model->w[i]);
        printf("Bias = %f\n", model->b);

        fwrite(&input_size, sizeof(int), 1, out_fp);
        fwrite(model->w, sizeof(double), input_size, out_fp);
        fwrite(&model->b, sizeof(double), 1, out_fp);

        free(Y);
    }

    // --- Linear regression ---
    else if(model_type == 1) {
        double *Y = (double *)malloc(n_samples * sizeof(double));
        fread(Y, sizeof(double), n_samples, fp);

        model = init_linear_regression(input_size, 0.1, 100);
        train_linear_regression(model, Y, X, n_samples);

        printf("Training finished (Linear Regression).\n");
        for(int i = 0; i < input_size; i++)
            printf("w[%d] = %f\n", i, model->w[i]);
        printf("Bias = %f\n", model->b);

        fwrite(&input_size, sizeof(int), 1, out_fp);
        fwrite(model->w, sizeof(double), input_size, out_fp);
        fwrite(&model->b, sizeof(double), 1, out_fp);

        free(Y);
    }

    // --- Perceptron (Rosenblatt) ---
    else if(model_type == 2) {
        int *Y = (int *)malloc(n_samples * sizeof(int));
        fread(Y, sizeof(int), n_samples, fp);

        model = init_perceptron(input_size, 0.1, 100);
        train_perceptron(model, X, Y, n_samples);

        printf("Training finished (Rosenblatt Perceptron).\n");
        for(int i = 0; i < input_size; i++)
            printf("w[%d] = %f\n", i, model->w[i]);
        printf("Bias = %f\n", model->b);

        fwrite(&input_size, sizeof(int), 1, out_fp);
        fwrite(model->w, sizeof(double), input_size, out_fp);
        fwrite(&model->b, sizeof(double), 1, out_fp);

        free(Y);
    }

    else {
        printf("Unknown model type: %d\n", model_type);
    }

    // --- Cleanup ---
    for(int i = 0; i < n_samples; i++)
        free(X[i]);
    free(X);

    if(model) {
        free(model->w);
        free(model);
    }

    fclose(fp);
    fclose(out_fp);

    printf("Weights written to %s\n", argv[2]);
    return 0;
}

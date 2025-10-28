#include <stdio.h>
#include <stdlib.h>
#include "Machinelib.h"

// ---------------- Linear classifier ----------------
Model *init_linear_classifier(int input_size, double lr, int epochs) {
    Model *model = (Model *)malloc(sizeof(Model));
    model->input_size = input_size;
    model->lr = lr;
    model->epochs = epochs;

    model->w = (double *)calloc(input_size, sizeof(double));
    model->b = 0.0;

    return model;
}

void train_linear_classifier(Model *model, double **X, int *Y, int n_samples) {
    for(int epoch = 0; epoch < model->epochs; epoch++) {
        int errors = 0;
        for(int i = 0; i < n_samples; i++) {
            double activation = model->b;
            for(int j = 0; j < model->input_size; j++)
                activation += model->w[j] * X[i][j];

            int y_pred = (activation >= 0) ? 1 : -1;
            if(Y[i] * y_pred <= 0) {
                for(int j = 0; j < model->input_size; j++)
                    model->w[j] += model->lr * Y[i] * X[i][j];
                model->b += model->lr * Y[i];
                errors++;
            }
        }
        printf("Epoch %d: errors = %d\n", epoch + 1, errors);
    }

    printf("Final bias = %f\n", model->b);
    printf("Final weights: ");
    for(int j = 0; j < model->input_size; j++)
        printf("%f ", model->w[j]);
    printf("\n");
}

// ---------------- Linear regression ----------------
Model *init_linear_regression(int input_size, double lr, int epochs) {
    Model *model = (Model *)malloc(sizeof(Model));
    model->input_size = input_size;
    model->lr = lr;
    model->epochs = epochs;

    model->w = (double *)calloc(input_size, sizeof(double));
    model->b = 0.0;

    return model;
}

void train_linear_regression(Model *model, double *Y, double **X, int n_samples) {
    for(int epoch = 0; epoch < model->epochs; epoch++) {
        for(int i = 0; i < n_samples; i++) {
            double pred = model->b;
            for(int j = 0; j < model->input_size; j++)
                pred += model->w[j] * X[i][j];
            double error = pred - Y[i];
            for(int j = 0; j < model->input_size; j++)
                model->w[j] -= model->lr * error * X[i][j];
            model->b -= model->lr * error;
        }
    }
}

// ---------------- Perceptron (Rosenblatt) ----------------
Model *init_perceptron(int input_size, double lr, int epochs) {
    Model *model = (Model *)malloc(sizeof(Model));
    model->input_size = input_size;
    model->lr = lr;
    model->epochs = epochs;

    model->w = (double *)calloc(input_size, sizeof(double));
    model->b = 0.0;

    return model;
}

void train_perceptron(Model *model, double **X, int *Y, int n_samples) {
    for(int epoch = 0; epoch < model->epochs; epoch++) {
        int errors = 0;
        for(int i = 0; i < n_samples; i++) {
            double activation = model->b;
            for(int j = 0; j < model->input_size; j++)
                activation += model->w[j] * X[i][j];

            int y_pred = (activation >= 0.0) ? 1 : -1;

            if(Y[i] != y_pred) {
                for(int j = 0; j < model->input_size; j++)
                    model->w[j] += model->lr * (Y[i] - y_pred) * X[i][j];
                model->b += model->lr * (Y[i] - y_pred);
                errors++;
            }
        }
        printf("Epoch %d: errors = %d\n", epoch + 1, errors);
    }

    printf("Final bias = %f\n", model->b);
    printf("Final weights: ");
    for(int j = 0; j < model->input_size; j++)
        printf("%f ", model->w[j]);
    printf("\n");
}

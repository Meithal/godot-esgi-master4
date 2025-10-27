#ifndef MACHINELIB_H
#define MACHINELIB_H

typedef struct {
    int input_size;
    int hidden_size;
    int output_size;
    double lr;
    int epochs;

    // Linear / Regression
    double *w;
    double b;

    // MLP
    double **W1;
    double **W2;
    double *b_mlp;

} Model;

// Initialisation
Model *init_linear_classifier(int input_size, double lr, int epochs);
Model *init_linear_regression(int input_size, double lr, int epochs);
Model *init_perceptron(int input_size, double lr, int epochs);

// Entraînement
void train_linear_classifier(Model *model, double **X, int *Y, int n_samples);
void train_linear_regression(Model *model, double *Y, double **X, int n_samples);
void train_perceptron(Model *model, double **X, int *Y, int n_samples);


#endif

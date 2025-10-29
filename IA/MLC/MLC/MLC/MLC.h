#ifndef MLC_H
#define MLC_H

#include <stdint.h>

#ifdef _WIN32
#ifdef BUILD_DLL
#define DLL_EXPORT __declspec(dllexport)
#else
#define DLL_EXPORT __declspec(dllimport)
#endif
#else
#define DLL_EXPORT
#endif

#include <stddef.h>

// -----------------------------
// Fonction simple (exemple)
// -----------------------------
DLL_EXPORT int32_t add(int32_t a, int32_t b);

// -----------------------------
// Linear / Perceptron Model
// -----------------------------
typedef struct {
    int32_t input_size;   // Nombre de features + 1 pour biais
    int32_t output_size;  // Nombre de classes ou 1 pour régression
    double lr;
    int32_t epochs;

    double** W;           // Matrice poids: output_size x input_size (biais inclus)
} LinearModel;

// Initialisation Linear / Perceptron
DLL_EXPORT LinearModel* init_linear(int32_t input_size, int32_t output_size, double lr, int32_t epochs);
DLL_EXPORT LinearModel* init_perceptron(int32_t input_size, int32_t output_size, double lr, int32_t epochs);

// Entraînement Linear / Perceptron
DLL_EXPORT void train_linear(LinearModel* model, double** X, int32_t* Y, int32_t n_samples);
DLL_EXPORT void train_perceptron(LinearModel* model, double** X, int32_t* Y, int32_t n_samples);

// -----------------------------
// MLP Model
// -----------------------------
typedef struct {
    int32_t n_layers;      // Nombre total de couches (entrée + cachées + sortie)
    int32_t* layer_sizes;  // Tableau: [input_size, hidden1, ..., output_size]
    double lr;
    int32_t epochs;

    double*** W;           // Tableau de matrices W[i]: couche i -> i+1 (biais inclus)
    // W[i] a layer_sizes[i+1] lignes et layer_sizes[i]+1 colonnes
} MLPModel;

// Initialisation MLP
DLL_EXPORT MLPModel* init_mlp(int32_t n_layers, int32_t* layer_sizes, double lr, int32_t epochs);

// Entraînement MLP
DLL_EXPORT void train_mlp(MLPModel* model, double** X, int32_t* Y, int32_t n_samples);

#endif // MLC_H

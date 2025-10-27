# Nom de l'exécutable
TARGET = train_model.exe

# Compilateur
CC = gcc

# Options de compilation
CFLAGS = -Wall -O2

# Fichiers sources
SRCS = train.c Machinelib.c

# Fichiers objets
OBJS = $(SRCS:.c=.o)

# Règle par défaut : compilation de l'exécutable
all: $(TARGET)

# Lien de l'exécutable
$(TARGET): $(OBJS)
	$(CC) $(CFLAGS) -o $@ $(OBJS)

# Compilation des fichiers .c en .o
%.o: %.c
	$(CC) $(CFLAGS) -c $< -o $@

# Nettoyage des fichiers objets et de l'exécutable
clean:
	del $(OBJS) $(TARGET) 2>nul

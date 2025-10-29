Cours
===

Apprentissage supervise, semi supervise
---

En semi supervise on etiquette qu'une partie

Le danger de l'apprentissage supervise est de se restreindre
au dataset d'entree et de ne pas etre capable de generaliser.

L'avantage du machine learning par rapport a l'intelligence
artificelle a base de if/else est d'etre capable de generaliser.

Les relation doivent etre pertinentes et correlees. On suppose
qu'il existe un modele pertinent qui approxime
les exemples et les fait correspondre a leur etiquetage.

Arnaque a la prediction : croire faussement qu'un
agent a une fonction de prediction valable, qu'il
y a une correlation la ou il n'y en a pas.

### Comment valider un modele ?

Cours sur la recherche de modeles : https://work.caltech.edu/telecourse.html

Theoreme central limite (TCM) : justifie l'usage d'une gaussienne pour valider. Ne nous interesse pas.

Inegalite de Hoeffding. Nous interesse : valide l'echantillon
d'un sondage, plutot que sonder toute la population.
C'est la probabilite de depasser un seuil au vu d'un echantillon.

$\mathbb{P}\left(\left|S_n-\mathbb{E}[S_n]\right|\ge x\sqrt{n}\right)
\le 2\exp\left(-2\,x^2\right)$

L'exponentielle negative fait que la probabilite decroit rapidement au vu de la taille de l'echantillon. Le carre
suit la meme idee. $S_n$ est la proportion reelle
d'une population. $\mathbb{E}[S_n]$ est la probabilite
de piocher la couleur recherchee dans un echantillon.

Vapnik-Cherno

nombre maximum de dichotomies possibles. C'est le nombre de plans
separateurs potentiellement possibles.

Tout ce questionnement sert a determiner la taille du
dataset qu'on veut entrainer pour qu'il corresponde
a la realite.

A la louche on utilise un dataset 10 fois plus grand
que le nombre d'inputs.

https://colab.research.google.com/drive/1c3gi0d1NA2TBaqOpEQ-LzGqfJGvwMUuE?usp=sharing


Normalisation standardisation
---

On adapte nots entrees a la fonction d'activation.

Normalisation : (X - xmin) / (xmax - xmin) -> pour un pixel de 0 a 255 on fait valeur pixel / 255

Standardisation : on soustrait la moyenne et on divise par l'ecart type
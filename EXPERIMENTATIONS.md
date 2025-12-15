Experimentations
===

Softmax sur algorithme de Rosenblatt
---

Vu que sur training de OR, (0, 0) cherche a renforcer "0" 
et que les trois autres cherchent à l'affaiblir, et que pour
(0, 0), seul le biais peut influer sur la sortie du modèle,
pourquoi ne pas appliquer softmax avec un pas d'apprentissage
plus grand sur les variables contribuant le plus à converger
vers le résultat voulu.

Concretement pour (1,1), les deux poids en amont auront
une grande correction lors de l'itération et le biais
n'aura qu'une faible correction. Pour (1,0) et (0,1)
le poids considéré seul aura une grande correction et
le biais une faible.

Pour (0,0) seul le biais sera modifié, très grandement.

Pour que cela fonctionne il faut prendre 
un pas d'apprentissage plus grand, typiquement
le double de ce qu'on prendrait habituellement.

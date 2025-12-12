+++
date = '2025-10-26T18:45:58+01:00'
title = 'Ia'
+++


Pour le projet IA on decide de d'abord créer une librairie python puis
de faire une librairie en C qui se baserait dessus.

La priorité est donc d'avoir quelque chose qui foncionne en python.

On part de la base qui est de simuler le neurone de Mac Culloch et Pitts
pour ensuite simuler le neurone (perceptron) de rosenblatt, puis d'ajouter
successivement des couches en fonction du progrès de la science des neurones artificiels durant ces 70 dernières années.

Apres Rosenblatt on veut ajouter des couches cachées (*deep learning*)
puis un systeme de *back propagation*.

Dans Python on a une classe qui contient une liste de neurones.
Avoir une classe pour cela permet de lui donner un nom, et de fournir
des outils pour différiencier le neurones d'entrée, de sortie et
intermédiaires.

Dans la première version, les neurones avaient un 
simple attribut "entrees", qui
est une liste de neurones, ce qui
etait suffisant pour les visualiser via graphviz.

On part de la sortie et de proche en proche on peut reconstituer
le reseau dans graphviz :

![un reseau simple schématisé avec graphviz](../reseau_graphviz.png)

Ce simple algorithme est limité lorsqu'on veut écrire
un réseau pondéré donc on rajoute un objet de type "Connexion"
qui fait le lien entre deux neurones et qui possède un poids,
ainsi qu'un attribut `amont` et `aval`. Une connexion de peut
lier que deux neurones donc pas besoin de liste.

Un neurone possède maintenant deux attributs `entrees` et
`sorties` qui sont une liste de connexions.

On considère un neurone qui a au moins une connexion amont vers le
néants (`None` en python) comme un neurone d'entrée. Un neurone
qui a au moins une connexion de sortie vers le neant comme un
neurone de sortie.

Lorsqu'un neurone fait son travail, il lit la valeur
de tous ses neurones parents et en fait la somme, puis
s'active en écrivant dans ses connexions de sortie.

Lorsqu'on souhaite amorcer notre réseau de neurones,
on boucle à travers tous les neurones d'entrée (notre
classe `Reseau` se charge pour nous de les identifier),
on écrit dans la connexion amont (qui fait office de
photorecepteur) les valeurs que l'on souhaite, puis
chaque neurone lit les connexions amont, fait la somme,
puis écrit le résultat dans le neurone de sortie.

Le problème avec ce système est qu'on répète l'information
de sortie dans toutes les connexions sortantes, donc 
pour les neurones intermédiaires on décide que c'est le neurone
lui même qui con tient la valeur qu'il a calculé, et lorsqu'un
neurone fils veut connaitre la veleur de son parent, il
interroge la connexion qui va soit lui communiquer
la veleur du neurone parent, soit sa propre valeur : avec
ce système, la "valeur" d'une connexionn devient une methode
qui retourne soit la valeur du neurone parent soit
sa valeur propre : une connexion avec une valeur
propre est considéré comme une connexion interface
avec le monde exterieur. Une connexion peut donc soit
contenir une valeur intrinsèque, soit un lien vers
un neurone parent, mais pas les deux en même temps.

Mais lorsque l'on souhaite visualiser un tel réseau
avec graphviz, on se retrouve avec le problème que
graphviz n'accepte pas le edge depuis et vers nulle part :
ceci n'est pas autorisé

```graphviz
digraph "Reseau de neurones" {
	1 [label="1: 0.0"]
	2 [label="2: 0.0"]
	3 [label="3: 0.0"]
	4 [label="4: 0.0"]
	OUT [label="OUT: 0.0"]
	1 -> OUT [label=0.0]
	2 -> OUT [label=0.0]
	3 -> OUT [label=0.0]
	4 -> OUT [label=0.0]
	-> 1 [label=entree]
}
```

On consolide le code de visualisation graphique
pour ignorer les entrées vers le néant.

Reparer la visualisation permet de détecter
que notre cas de test AND ne fonctionne pas bien
car une entrée mise a 1 sur 10 avec un seuil de 10 active 
notre neurone : on fait la somme sans verifier le seuil, et
au lieu d'écrire 1 dans la connexion de sortie on écrit 
la somme elle même. Un neurone de Pitts fonctionne uniquement
de manière booleenne donc l'information de la somme des
neurones avals est perdue.

De la meme façon il n'est pas possible de simuler un
noeud logique NOT avec ce systeme vu qu'on fait une somme
et on verifie seulement que la valeur de sortie
est superieure ou egale a la velur de seuil.

On doit donc ajouter de la ponderation a chaque connexion (axone),
cette pondération doit nécessairement pouvoir prendre une valeur négative
si l'on souhaite pourvoir simuler une porte NON.

Backpropagation
---

Si on veut que notre reseau soit capable d'apprendre, on
doit donner une direction d'apprentissage, donc la sortie
attendue devient -1 et +1. Pareillement il ne s'agit
plus d'emuler un bit (0/1) mais de trouver la sortie
la plus probable. On a donc deux sorties, une pour
zero et une pour 1.

Probleme lorsqu'on veut renforcer la sortier zero quand 
les deux inputs sont a zero, on mutiplie
$w <- w + \eta y_i x_i$
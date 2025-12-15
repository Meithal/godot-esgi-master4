Instructions pour generer dataset, train et visdualiser
===

fonctionne probablement mieux depuis un WSL ou tout environnement POSIX

Pour générer un dataset

```bash
make run-godot GODOT_BIN="/Applications/Godot_mono.app/Contents/MacOS/Godot"
```

En remplacant GODOT_BIN par le chemin absolu vers son propre godot_mono

Sinon en demarrant godot mono et rajoutant ./godot/ comme racine de projet

Sinon en tapant soi meme les instructions qu'execute le makefile

```bash
<chemin absolu vers godot mono> --path ./godot
```

Une fois en jeu appuyer sur play et jouer un peu. On essaye de faire
des clips de gameplay courts donc essayer de perdre apres un ou deux obstacles.

Vu que le nombre d'obstacles passés est une feature on essaye d'entrainer notre
bird a perdre apres un ou deux obstacles pour valider que notre entrainement
fonctionne bien.

Une fois plusieurs dataset constitués lancer

```bash
make update-model
```

qui lance le training `./IA/Python/train_from_demos.py`
puis régénère la librairie C.

Lancer

```bash
make build-godot
```

Pour réincorporer notre librairie dans notre godot.

En cliquant sur "Play like me", la scene godot
écoute les inputs depuis notre libaririe C qui feed forward
l'état du jeu et sort un output qui correspond au modèle.

Pour visualiser le training 

```bash
make viz-flappy
```

Bonus
---

Pour modifier les features considérer [MODIF.md](/MODIF.md)
C'est ce qu'on a du faire pour que le flappy fonctionne.

Pour les expérimentations considérées, vue que c'est demandé,
voir le fichier [EXPERIMENTATIONS.md](/EXPERIMENTATIONS.md).

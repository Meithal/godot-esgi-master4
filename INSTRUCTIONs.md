Instructions pour l'aspect machine learning
===

Requirements
---

Fonctionne probablement mieux depuis un WSL ou tout environnement POSIX

Creer un venv (testé avec python 3.10) dans le répertoire ./IA/Python
puis depuis celui-ci lancer `pip install -r requirements.txt`.

Pour générer un dataset

```bash
make run-godot GODOT_BIN="/Applications/Godot_mono.app/Contents/MacOS/Godot"
```

En remplacant GODOT_BIN par le chemin absolu vers son propre godot_mono

Sinon en demarrant godot mono et rajoutant ./godot/ comme racine de projet

Sinon en tapant soi meme les instructions qu'execute le makefile :

```bash
<chemin absolu vers godot mono> --path ./godot
```

Une fois en jeu appuyer sur play et jouer un peu. On essaye de faire
des clips de gameplay courts donc essayer de perdre apres un ou deux obstacles.

Vu que le nombre d'obstacles passés est une feature on essaye d'entrainer notre
bird a perdre apres un ou deux obstacles pour valider que notre entrainement
fonctionne bien.

Les dataset sont sauvegardés dans ./IA/Python au format .csv.

Une fois plusieurs dataset constitués lancer

```bash
make update-model
```

qui lance le training `./IA/Python/train_from_demos.py`

puis régénère la librairie C, notamment le frontend `./IA/SoftMaxC/model.c`
qui contient la fonction `predict()` que notre godot utilise pour jouer
selon le modele.

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

Modifications et tuning du modele
---

Root.cs :

- changer `PredictDelegate`
- changer Appel à `_nativePredictFunc` dans `_Process`

train_from_demos.py :

Ca ne sert a rien de tout modifier il faut juste changer
ce qu'on passe a X.append() dans load_all(). Notre godot
nous passe des données brutes et c'est ici qu'on les transforme
en features plus utilisables comme "time to impact", etc.

C'est ici qu'on fait aussi de la normalisation.

model.c :

- changer signature de predict
- changer   raw_in[0] = vs; etc

viz_flappy.c :

- changer FEATURE_NAMES

flappy_viewer.html:

- faire coller a viz_flappy.c
- changer les legendes

make update-model
make viz-flappy
make run-godot


Bonus
---

Pour les expérimentations considérées, vue que c'est demandé,
voir le fichier [EXPERIMENTATIONS.md](/EXPERIMENTATIONS.md).

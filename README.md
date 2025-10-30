# Flappy Simulation – Moteur Hybride Unity / Godot

## Présentation

Ce projet implémente une **simulation du jeu Flappy Bird** à l’aide d’un **moteur de logique central en C# (.NET)** intégré à deux moteurs de jeu distincts :

- **Unity** pour une visualisation en 3D.  
- **Godot** pour une visualisation en 2D.

L’objectif principal est de démontrer une **architecture hybride** où la logique de jeu est **décorrélée du moteur graphique**, assurant ainsi une simulation cohérente et reproductible, quel que soit le moteur utilisé.

---

## Architecture du projet

### 1. Noyau logique (`FlappyCore.dll`)
La logique du jeu est entièrement implémentée en C# pur et compilée en bibliothèque dynamique (`.dll`).

Principales classes :
- **`Flappy`** : gère la physique du joueur, la gravité, les collisions et les interactions.
- **`FlappyEntry`** : interface principale du moteur, responsable de l’initialisation, de la mise à jour et de la réinitialisation de la simulation.

Structures de données :
- **`InputData`** : données d’entrée (ex. appui sur le saut, delta time).  
- **`OutputData`** : données de sortie (positions, obstacles, état du jeu, game over, etc.).

### 2. Vue Unity
L’intégration Unity repose sur :
- Un **GameManager** contrôlant les états du jeu (menu, partie, game over).
- Un **FlappyView** mettant à jour la position du joueur et des obstacles à partir des données `OutputData`.
- Un **système d’entrée** utilisant le Input System pour relayer les commandes au moteur logique.

### 3. Vue Godot
L’intégration Godot repose sur :
- Un script **`Root.cs`** gérant le cycle du jeu, la communication avec `FlappyEntry`, et le dessin 2D des obstacles.  
- Des **panneaux UI** (`MainMenu`, `GameOver`) intégrés dans un `CanvasLayer` et contrôlés via des signaux.  
- Un **système de dessin manuel (`_Draw`)** pour visualiser la simulation.  
- Une **skybox 2D** défilante via un `Parallax2D`.

---

## Fonctionnement général

1. Le joueur appuie sur "Play" dans le menu.  
2. Le `GameManager` (Unity) ou le `Root` (Godot) initialise la DLL `FlappyCore`.  
3. À chaque frame :
   - Les entrées sont collectées (`InputData`).
   - La simulation est mise à jour via `FlappyEntry.Update()`.
   - Les sorties (`OutputData`) sont utilisées pour déplacer les entités visibles.
4. En cas de collision, `GameOver` passe à `true`, le panneau de fin s’affiche.
5. Le joueur peut relancer une partie via `Reset()`.

---

## Lancer le projet

### Option 1 — Dans Unity
1. Ouvrir le dossier Unity dans l’éditeur.  
2. Vérifier que la DLL `FlappyCore.dll` est présente dans `Assets/Plugins/`.  
3. Lancer la scène principale (`FlappyScene.unity`).  
4. Appuyer sur **Play** pour tester.  

### Option 2 — Dans Godot
1. Ouvrir le dossier Godot du projet (`FlappyGodot/`).  
2. S’assurer que `FlappyCore.dll` est dans le répertoire `godot/bin/` ou accessible depuis `res://`.  
3. Lancer la scène principale (`root.tscn`).  
4. Appuyer sur "Play" dans le menu du jeu.  

---

## Exporter le projet Godot

1. Installer les **export templates** via `Editor → Manage Export Templates`.  
2. Aller dans `Project → Export` et ajouter un profil **Windows Desktop**.  
3. Choisir un nom de fichier (ex: `builds/Flappy.exe`) et cocher *Embed PCK*.  
4. Exporter pour obtenir un exécutable fonctionnel.

---

## Ce que nous avons appris

- Intégration d’une **DLL C# externe** dans Unity et Godot.  
- Utilisation de la **hiérarchie de scènes** propre à chaque moteur.  
- Gestion des **panneaux UI dynamiques** et des états de jeu.  
- Communication entre la logique et la vue via des structures de données partagées.  
- Création d’un **environnement de simulation cohérent et multi-moteur**.

---

## Pistes d’amélioration

- Ajout d’un **système de score**.  
- Intégration de **sons et animations**.  
- Ajout d’un **scrolling automatique du fond** dans Godot.  
- Visualisation 3D améliorée dans Unity.  
- Export multiplateforme (Linux / WebGL).

---

## Auteurs

- **Ivo Talvet**  
- **Timothée M’Bassije**  

Projet réalisé dans le cadre du module  
**Biomimétisme/ moteur Godot ESGI— 2025**  

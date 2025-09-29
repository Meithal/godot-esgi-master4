+++
date = '2025-09-29T19:54:34+02:00'
draft = false
title = 'Install Dotnet Infrastructure'
+++

# Outils de base


Pour installer godot-mono, sur Mac

```
brew install godot-mono
```

Cela installe automatiquement dotnet 8 comme dependance.

# Godot

Pour la partie Godot il faut creer le projet via 
le GUI de godot et creer un script C# qu'on
attache au composant root.

Le fait d'ouvrir ce script cree automatiquement un .sln et un .csproj

# Partie dotnet

La mission qu'on doit realiser est la suivante

Dans un dossier vide, frère du dossier avec le
projet Godot:
- Créer une solution .Net 8
- Créer un projet Core de type Classe Library
- Créer un projet Iteration/Test de type
Console Application/Unit Test
- Créer votre dépôt git avec le projet Godot et
la solution C# chacun dans son dossier avec
chacun son gitignore
- Dans la solution du projet Godot, ajouter
une dépendance au projet Core

Solution .net 8
---

Taper `dotnet --help` dans une console donne un apercu de ce qu'on peut faire.

`dotnet help new` ouvre une page web assez utile https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new

On y apprend que `dotnet new list` permet de lister tous les templates
qu'on peut creer. On suppose que `classlib` est le template qu'on
veut utiliser. Comment s'assurer que le projet soit bien
installe en .net 8 ? En effet `dotnet --list-sdks` affiche
```
8.0.414 [/usr/local/share/dotnet/sdk]
9.0.305 [/usr/local/share/dotnet/sdk]
```

On suppose que "framework" et "sdk" sont synonymes et 
que `dotnet new classlib --framework 8.0.414` fonctionne.

Mais cela affiche une erreur

```
Error: Invalid option(s):
--framework 8.0.414
   '8.0.414' is not a valid value for --framework. The possible values are:
      net8.0           - Target net8.0
      net9.0           - Target net9.0
      netstandard2.0   - Target netstandard2.0
      netstandard2.1   - Target netstandard2.1
```

Apparamment net8.0 est ce qu'on doit mettre.

Cela cree une arborescence avec un fichier Class1.cs
et un repertoire `obj` avec beaucoup de chemins de
dossiers en nom absolu, qu'on suppose doit mettre dans un gitignore.

Une fois fait on considere que le point 1 du projet est realise et on
fait un premier commit.

Mais on se rend compte qu'on a cree un projet avant de creer la solution.
Du coup `dotnet new sln`.

Puis `dotnet new classlib --framework net8.0`
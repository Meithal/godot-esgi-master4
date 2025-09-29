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

Projet Class Library Core
--- 

Mais on se rend compte qu'on a cree un projet avant de creer la solution.
Du coup `dotnet new sln`.

Puis `dotnet new classlib --framework net8.0`

Tests
---

Pour ce qui est des tests on trouve plusieurs recettes

```
MSTest Playwright Test Project    mstest-playwright           [C#]        Test/MSTest/Playwright/Desktop/Web
MSTest Test Class                 mstest-class                [C#],F#,VB  Test/MSTest                       
MSTest Test Project               mstest                      [C#],F#,VB  Test/MSTest/Desktop/Web           
...
NUnit 3 Test Item                 nunit-test                  [C#],F#,VB  Test/NUnit                        
NUnit 3 Test Project              nunit                       [C#],F#,VB  Test/NUnit/Desktop/Web            
NUnit Playwright Test Project     nunit-playwright            [C#]        Test/NUnit/Playwright/Desktop/Web 
...
xUnit Test Project                xunit                       [C#],F#,VB  Test/xUnit/Desktop/Web            
```
Aucun ne semble correspondre a "projet Iteration/Test de type
Console Application/Unit Test".

On remarque au passage qu'il existe une recette `dotnet new gitignore`
qui remplace notre propre gitignore qui contenanit juste

```
obj/
```

Mais aussi une recette `editorconfig` qui est toujours utile.

https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test
indique que VSTest est l'outil de test par defaut et qu'il se lance via `dotnet test`
or les recettes que nous avons sont pour MStest, NUnit 3 et xUnit.

On tape au hasard `dotnet test` dans la console et on a juste un message de nuget.

https://learn.microsoft.com/en-us/dotnet/core/testing/ indique que MStest est le
framework par defaut donc on part sur ca.

`dotnet new mstest` dans le meme repertoire que la librairie de
classe cause une erreur car ca ecraserait le `.csproj` existant,
donc on a envie de creer un repertoire dedie pour les tests.

On regarde `dotnet new mstest --help` si il y a une option pour creer un repertoire.
Il semble que non, mais l'option `--project <project>     The project that should be used for context evaluation.` semble etre utile pour lier le test a notre librairie. 

Je n'ai pas l'impression que notr eprojet ait un nom donc je suppose que `<project>` designe
un nom de repertoire. L'option frsamework a aussi l'air utile.

Donc on cree un repertoire et on execute `dotnet new mstest --project .. --framework net8.0`.

Ca fonctionne, mais en ouvrant `Test1.cs` et `MSTestSettings.cs` on a de nombreux passages
soulignes en rouge.

En lisant cette documentation https://learn.microsoft.com/en-us/visualstudio/test/walkthrough-creating-and-running-unit-tests-for-managed-code?view=vs-2022
on suppose qu'on est cense mettre en places les tests via l'IDE visual studio.

https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-mstest
est plus utile, notamment `dotnet sln add ***.csproj` est necessaire.

On tape

```
dotnet sln add core-dotnet.csproj 
dotnet sln add tests/tests.csproj
```

mais ca n'enleve pas le rouge dans les fichier tests. 
Peut etre que l'arborescence doit strictement obeir
le format `<nom du projet>.Tests`.

Sinon `dotnet sln --help` nous indique qu'il existe
une commande `dotnet sln list` qui liste les projets,
c'est peut etre ca qu'il fallait mettre dans `dotnet new mstest`.

Apres quelques `dotnet sln remove`, recreations, on a enleve le
texte souligne en rouge dans nos tests.

En redemarrant VSCode, le rouge s'enleve donc on part
du principe que la creation du projet de tests fonctionne.

Commandes a garder en tete
```
dotnet new classlib --framework net8.0 
dotnet new mstest --framework net8.0
dotnet reference add FlappyCore/FlappyCore.csproj --project FlappyCore.Tests/FlappyCore.Tests.csproj --framework net8.0
dotnet sln add FlappyCore.Tests/FlappyCore.Tests.csproj
```

Puis redemarrer VSCode.
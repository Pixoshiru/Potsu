# Potsu

Potsu est un jouet musical numérique pour PC, Mac et iOS, fait avec Unity. On joue à la souris ou du bout des doigts.

Potsu est **paramétrique** : presque rien n'y est enregistré à l'avance. Pas de samples, pas de textures : l'image et le son sortent de formules mathématiques et de synthèse sonore.

## La version actuelle (V1)

- Un écran noir. Un clic (ou un toucher) fait naître une onde qui se propage comme sur une surface liquide, et joue une note.
- La note est tirée au hasard sur deux octaves, dans la gamme du moment. La gamme change toutes les 30 secondes.
- Le son est une sinusoïde simple : attaque courte, volume moyen, decay d'une seconde.
- Le son et l'onde partent **au relâchement**. Plusieurs clics rapides forment un accord.
- On peut **charger** un clic en le maintenant : plus l'appui dure (jusqu'à 1,5 s), plus l'accord joué contient de notes, et plus l'onde est forte.
- Chaque frappe **se répète** toutes les secondes, six fois, en s'usant : moins forte, plus sourde, onde plus faible. Puis elle se tait.

## Ouvrir le projet

1. Installer **Unity 6.6** (6000.6.x) avec Unity Hub, ainsi que les modules de build voulus (Mac, Windows, iOS).
2. Cloner ce dépôt (GitHub Desktop ou `git clone`), puis dans Unity Hub : **Add → Add project from disk** et choisir **le dossier du clone**. C'est ce dossier que Unity doit ouvrir, sinon les mises à jour récupérées par Git n'arrivent pas dans Unity.
3. Ouvrir n'importe quelle scène (même vide) et appuyer sur **Play**. Potsu se met en place tout seul : caméra, surface, son.

La gamme en cours s'affiche dans la console Unity à chaque changement.

## Configuration technique

| Élément | Choix |
|---|---|
| Unity | 6.6 (`ProjectSettings/ProjectVersion.txt`) |
| Rendu | **URP** (Universal Render Pipeline 17.6). Assets du pipeline à la racine de `Assets/` : `New Universal Render Pipeline Asset` (+ son `_Renderer`), `UniversalRenderPipelineGlobalSettings`, `DefaultVolumeProfile`. |
| Entrées | **Input System 1.20.1**, avec *Active Input Handling* réglé sur *Input System Package (New)*. |
| Contenu | Aucun sample, aucune texture, aucune scène obligatoire : tout est créé par code au lancement. |

À commiter : `Assets/` (avec les fichiers `.meta`), `Packages/`, `ProjectSettings/`. À ne pas commiter : `Library/`, `Temp/`, `Logs/`, `UserSettings/` (déjà exclus par `.gitignore`).

## Organisation

Le projet est découpé en quatre modules, décrits dans [Docs/](Docs/) :

| Module | Rôle | Dossier |
|---|---|---|
| [Surface](Docs/Modules/Surface.md) | La surface de jeu : son aspect visuel et ses interactions | `Assets/Potsu/Surface` |
| [Synthétiseur](Docs/Modules/Synthetiseur.md) | La génération du son et du timbre | `Assets/Potsu/Synthesizer` |
| [Compositeur](Docs/Modules/Compositeur.md) | Les gammes et les notes jouées | `Assets/Potsu/Composer` |
| [Pendule](Docs/Modules/Pendule.md) | La temporalité d'une session : répétitions, et à terme l'horloge de la machine | `Assets/Potsu/Pendulum` |

`Assets/Potsu/Core` contient le point d'entrée qui relie les modules. Voir [Docs/Architecture.md](Docs/Architecture.md).

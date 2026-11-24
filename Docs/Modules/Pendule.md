# Pendule

La temporalité d'une session de Potsu. Dossier `Assets/Potsu/Pendulum`, namespace `Potsu.Timing`.

## Rôle

Donner une forme dans le temps à ce qui est joué. Le Pendule ne produit ni son ni image : il **annonce des événements** dans le temps, et `PotsuApp` les transmet au Synthétiseur et à la Surface.

## État actuel (V1) : les échos

Chaque frappe posée **se répète** à intervalle régulier et **s'use** à chaque répétition, puis se tait.

- **Intervalle** : 1 s entre deux répétitions (`interval`).
- **Répétitions** : 6, après quoi la frappe n'est plus répétée (`repetitions`).
- Un écho rejoue **les mêmes notes** que la frappe d'origine, au même endroit, même si la gamme a changé entre-temps.
- **Usure** : la répétition *r* sur *N* a une usure de *r / N*, de 1/6 à 1. Le Pendule ne décide que de cette valeur. Ce qu'elle produit est décidé ailleurs :
  - le Synthétiseur la traduit en timbre (`Synthesizer.Worn`) : moins fort, plus sourd, attaque plus douce ;
  - `PotsuApp` la traduit en force d'onde : l'onde de la dernière répétition a 20 % de la force d'origine.

| Fichier | Contenu |
|---|---|
| `Pendulum.cs` | Le composant : pose des frappes, programmation et annonce des échos. |
| `Strike.cs` | `Strike` (une frappe posée) et `Echo` (une de ses répétitions). |

### Horloge et précision

Le Pendule lit l'heure dans une horloge interchangeable (`Pendulum.Clock`). `PotsuApp` la branche sur **l'horloge audio** du Synthétiseur, qui compte les échantillons réellement rendus. Les échos tombent donc à l'échantillon près et ne dérivent jamais, même si l'image saccade.

Chaque écho est annoncé deux fois :

1. `EchoAhead`, 100 ms à l'avance (`lookahead`), avec son instant exact : le son est programmé dans le Synthétiseur, qui le démarre au bon échantillon.
2. `EchoNow`, au moment même, pour ce qui se cale sur l'image (l'onde).

### Paramètres (Inspector, composant `Pendulum` sur l'objet `Potsu`)

| Paramètre | Défaut | Effet |
|---|---|---|
| `interval` | 1 s | Temps entre deux répétitions. |
| `repetitions` | 6 | Nombre de répétitions. |
| `lookahead` | 0,1 s | Avance de programmation du son. |

## Où l'on va

Le but est que la temporalité de Potsu dépende de :

- **l'horloge interne de la machine** sur laquelle l'app tourne (heure réelle, voire date) et non d'un intervalle fixe ;
- **la temporalité des autres notes posées** : les échos devront tenir compte de ce qui sonne déjà (se caler, s'éviter, se répondre).

Ce qui est déjà prévu pour y aller :

- l'horloge est une simple fonction (`Clock`). On pourra la brancher sur l'heure système, recalée sur l'horloge audio pour garder la précision ;
- toutes les frappes posées et leurs échos à venir sont dans le Pendule, qui pourra les consulter avant de programmer un nouvel écho ;
- le calcul de l'instant d'un écho tient en une ligne (`strike.Time + r * interval` dans `Register`). C'est là que viendra une règle plus riche.

## Questions ouvertes

- Une session a-t-elle une durée, ou est-elle infinie ?
- Quel lien entre l'heure réelle et le rythme : tempo lié à l'heure, cycles jour/nuit, minutes rondes ?
- Comment les échos d'une frappe tiennent-ils compte des autres : quantification sur une grille commune, ou interaction libre ?
- Le changement de gamme toutes les 30 s (aujourd'hui dans le Compositeur) doit-il passer sous le contrôle du Pendule ?

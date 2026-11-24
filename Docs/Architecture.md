# Architecture

## Principe

Chaque module a une seule responsabilité et ne connaît pas les autres. Un seul endroit les relie : `Core/PotsuApp.cs`. On peut ainsi remplacer un module (un autre synthé, une autre surface) sans toucher aux autres.

```
            frappe (position, charge)
 Surface ───────────────────────────────► PotsuApp
    ▲                                        │
    │ onde (position, charge)                │ charge
    │                                        ▼
    └──────────────────────────────── Compositeur ──► notes MIDI
                                             │
                                             ▼
                                       Synthétiseur ──► son
```

1. La **Surface** détecte un appui, mesure sa durée et, au relâchement, émet une *frappe* : une position à l'écran et une *charge* entre 0 (clic bref) et 1 (maintien de 1,5 s ou plus).
2. **PotsuApp** demande au **Compositeur** les notes correspondant à cette charge.
3. Le **Synthétiseur** joue ces notes, et la **Surface** lance une onde au même endroit.
4. Le **Pendule** viendra rythmer la session (changements de gamme, évolution dans le temps). Pour l'instant, le Compositeur gère lui-même le changement de gamme toutes les 30 secondes.

## Le vocabulaire commun

| Terme | Sens |
|---|---|
| Frappe | Un appui relâché sur la surface : position + charge. |
| Charge | Valeur de 0 à 1, proportionnelle à la durée de maintien (plafonnée). |
| Note | Un numéro MIDI (60 = Do4). Le Compositeur parle en notes, le Synthétiseur les convertit en fréquences. |
| Gamme | Une tonique et une liste d'intervalles. |
| Set de gammes | La règle qui choisit la gamme suivante (`IScaleGenerator`). |

## Démarrage

`PotsuApp` se crée tout seul au lancement de n'importe quelle scène (`RuntimeInitializeOnLoadMethod`). Il ajoute les trois composants actifs sur un objet `Potsu`. Si une scène contient déjà un `PotsuApp`, configuré à la main dans l'Inspector, c'est celui-là qui est utilisé.

## Plateformes

- **Mac / PC** : souris, un appui à la fois.
- **iOS** : multitouch, chaque doigt est une frappe indépendante, donc plusieurs doigts relâchés ensemble forment un accord.
- Les entrées passent par le nouvel Input System s'il est actif, sinon par l'ancien Input Manager.

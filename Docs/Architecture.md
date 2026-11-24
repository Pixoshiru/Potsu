# Architecture

## Principe

Chaque module a une seule responsabilité et ne connaît pas les autres. Un seul endroit les relie : `Core/PotsuApp.cs`. On peut ainsi remplacer un module (un autre synthé, une autre surface) sans toucher aux autres.

```
            frappe (position, charge)
 Surface ───────────────────────────────► PotsuApp ◄──────── Pendule
    ▲                                     │   │  échos     ▲ (horloge audio)
    │ onde (position, charge, force)      │   │            │
    └─────────────────────────────────────┘   │ pose       │
                                              ├────────────┘
                     charge ──► Compositeur ──► notes MIDI
                                              │
                         notes, timbre, instant▼
                                        Synthétiseur ──► son
```

1. La **Surface** détecte un appui, mesure sa durée et, au relâchement, émet une *frappe* : une position à l'écran et une *charge* entre 0 (clic bref) et 1 (maintien de 1,5 s ou plus).
2. **PotsuApp** demande au **Compositeur** les notes correspondant à cette charge.
3. Le **Synthétiseur** joue ces notes, et la **Surface** lance une onde au même endroit.
4. La frappe est **posée** dans le **Pendule**, qui annonce ensuite ses *échos* : une répétition par seconde, six fois, de plus en plus usée. Pour chaque écho, PotsuApp programme le son (timbre usé) et lance une onde plus faible.
5. Le Pendule lit le temps sur l'**horloge audio** du Synthétiseur, ce qui rend les échos précis à l'échantillon près. Le changement de gamme toutes les 30 secondes reste pour l'instant dans le Compositeur.

## Le vocabulaire commun

| Terme | Sens |
|---|---|
| Frappe | Un appui relâché sur la surface : position + charge. |
| Charge | Valeur de 0 à 1, proportionnelle à la durée de maintien (plafonnée). |
| Note | Un numéro MIDI (60 = Do4). Le Compositeur parle en notes, le Synthétiseur les convertit en fréquences. |
| Gamme | Une tonique et une liste d'intervalles. |
| Set de gammes | La règle qui choisit la gamme suivante (`IScaleGenerator`). |
| Poser | Confier une frappe au Pendule, qui la fera revenir. |
| Écho | Une répétition d'une frappe posée. |
| Usure | De 0 (neuf) à 1 (dernière répétition) : le Synthétiseur et la Surface la traduisent chacun à leur façon. |

## Démarrage

`PotsuApp` se crée tout seul au lancement de n'importe quelle scène (`RuntimeInitializeOnLoadMethod`). Il ajoute les quatre composants sur un objet `Potsu`. Si une scène contient déjà un `PotsuApp`, configuré à la main dans l'Inspector, c'est celui-là qui est utilisé.

## Plateformes

- **Mac / PC** : souris, un appui à la fois.
- **iOS** : multitouch, chaque doigt est une frappe indépendante, donc plusieurs doigts relâchés ensemble forment un accord.
- Les entrées passent par le **nouvel Input System**, qui est le réglage du projet. `PointerTracker` garde une branche pour l'ancien Input Manager, utilisée seulement si *Active Input Handling* repasse sur l'ancien système.
- Le rendu passe par **URP**. Le shader des ondes est un shader unlit simple, compatible avec URP comme avec l'ancien pipeline.

# Synthétiseur

La génération du son et du timbre.

## Rôle

- Recevoir des notes (numéros MIDI) et les jouer.
- Fabriquer le son en temps réel, sans samples.

## État actuel (V1)

| Fichier | Contenu |
|---|---|
| `Synthesizer.cs` | Synthé polyphonique à sinusoïdes, calculé dans `OnAudioFilterRead`. |

### Fonctionnement

- Chaque note occupe une **voix** : une sinusoïde avec sa phase, son âge, son volume et son propre filtre passe-bas.
- **Enveloppe** : montée linéaire (attaque de 8 ms), puis decay exponentiel qui atteint -60 dB en 1 s. La voix se libère ensuite.
- **Polyphonie** : 48 voix. Si toutes sont prises, la plus ancienne est volée.
- **Accords** : le volume par note est divisé par √(nombre de notes), pour qu'un accord ne soit pas plus fort qu'une note seule.
- **Limiteur doux** en sortie (approximation de tanh) : les empilements de notes saturent en douceur plutôt que de craquer.
- Le son est calculé dans le thread audio. Les notes y arrivent par une petite file protégée, sans allocation mémoire pendant le calcul.

### Horloge audio et notes programmées

`AudioTime` donne le temps écoulé en comptant les échantillons réellement rendus. `PlayNotes(notes, timbre, atTime)` programme des notes à un instant précis de cette horloge : elles démarrent au bon échantillon, même au milieu d'un bloc audio. Le Pendule s'en sert pour que les répétitions ne dérivent pas.

### Timbre et usure

Chaque note porte un `NoteTimbre` : volume relatif, coupure du passe-bas, attaque. `Worn(usure)` donne le timbre d'une note usée, de 0 (neuve) à 1 (complètement usée). Les trois courbes sont exponentielles : chaque étape d'usure retire la même proportion.

| Usure | Volume | Passe-bas | Attaque |
|---|---|---|---|
| 0 | 100 % | aucun | 8 ms |
| 1/6 (1re répétition) | ≈ 70 % | ≈ 1 900 Hz | ≈ 11 ms |
| 1 (6e répétition) | 12 % | 220 Hz | 50 ms |

Sur une sinusoïde pure, un passe-bas ne change pas la couleur d'une note : il l'atténue, d'autant plus qu'elle est aiguë. L'effet « plus sourd » se traduit donc par des aigus qui s'éteignent plus vite que les graves, plus une attaque plus feutrée. Il deviendra un vrai changement de couleur dès que le timbre aura des harmoniques.

### Paramètres (Inspector, composant `Synthesizer`)

| Paramètre | Défaut | Effet |
|---|---|---|
| `attack` | 0,008 s | Durée de l'attaque. |
| `decay` | 1 s | Temps pour retomber à -60 dB. |
| `noteVolume` | 0,3 | Volume d'une note seule. |
| `masterVolume` | 0,8 | Volume général. |
| `maxVoices` | 48 | Nombre de voix simultanées. |
| `wornGain` | 0,12 | Volume relatif d'une note complètement usée. |
| `slightlyWornLowpassHz` | 3 000 Hz | Coupure du passe-bas au tout début de l'usure. |
| `wornLowpassHz` | 220 Hz | Coupure du passe-bas d'une note complètement usée. |
| `wornAttack` | 0,05 s | Attaque d'une note complètement usée. |

## Pistes

- Timbres : harmoniques, FM, oscillateurs désaccordés, bruit filtré.
- Enveloppe complète (ADSR), et des paramètres de timbre qui dépendent de la charge.
- Spatialisation stéréo selon la position de la frappe.
- Réverbération ou délai procéduraux, pour l'idée d'espace liquide.

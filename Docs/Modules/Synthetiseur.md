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

- Chaque note occupe une **voix** : une sinusoïde avec sa phase, son âge et son volume.
- **Enveloppe** : montée linéaire (attaque de 8 ms), puis decay exponentiel qui atteint -60 dB en 1 s. La voix se libère ensuite.
- **Polyphonie** : 32 voix. Si toutes sont prises, la plus ancienne est volée.
- **Accords** : le volume par note est divisé par √(nombre de notes), pour qu'un accord ne soit pas plus fort qu'une note seule.
- **Limiteur doux** en sortie (approximation de tanh) : les empilements de notes saturent en douceur plutôt que de craquer.
- Le son est calculé dans le thread audio. Les notes y arrivent par une petite file protégée, sans allocation mémoire pendant le calcul.

### Paramètres (Inspector, composant `Synthesizer`)

| Paramètre | Défaut | Effet |
|---|---|---|
| `attack` | 0,008 s | Durée de l'attaque. |
| `decay` | 1 s | Temps pour retomber à -60 dB. |
| `noteVolume` | 0,3 | Volume d'une note seule. |
| `masterVolume` | 0,8 | Volume général. |
| `maxVoices` | 32 | Nombre de voix simultanées. |

## Pistes

- Timbres : harmoniques, FM, oscillateurs désaccordés, bruit filtré.
- Enveloppe complète (ADSR), et des paramètres de timbre qui dépendent de la charge.
- Spatialisation stéréo selon la position de la frappe.
- Réverbération ou délai procéduraux, pour l'idée d'espace liquide.

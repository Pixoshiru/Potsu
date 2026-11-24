# Compositeur

Les gammes et les notes jouées.

## Rôle

- Tenir la **gamme du moment** et en changer.
- Choisir **quelles notes** jouer pour une frappe, selon sa charge.

Le Compositeur ne s'occupe ni du son ni de l'image : il renvoie des numéros de notes MIDI.

## État actuel (V1)

| Fichier | Contenu |
|---|---|
| `Composer.cs` | Le composant : gamme courante, tessiture, choix des notes. |
| `Scale.cs` | Une gamme : tonique + intervalles + nom. |
| `Modes.cs` | Les sept modes (ionien, dorien, phrygien, lydien, mixolydien, éolien, locrien). |
| `IScaleGenerator.cs` | L'interface d'un **set de gammes**. |
| `RandomModeGenerator.cs` | Le set par défaut : tonique et mode au hasard. |

### Choix des notes

- **Tessiture** : deux octaves à partir de la tonique en octave 3 (par exemple Ré3 à Ré5), tonique du haut comprise. Pour un mode, cela fait 15 notes possibles.
- **Nombre de notes** : `1 + charge × 4`, plafonné à 4. Donc 1 note sous 0,375 s de maintien, 2 jusqu'à 0,75 s, 3 jusqu'à 1,125 s, 4 au-delà.
- **Une note** : tirée au hasard dans la tessiture.
- **Accord** : empilement de tierces dans la gamme (un degré sur deux) à partir d'un degré tiré au hasard. On obtient ainsi les accords naturels du mode.

### Changement de gamme

Toutes les 30 secondes (`scaleDuration`), le Compositeur demande la gamme suivante à son set de gammes. Ce minutage ira dans le Pendule quand celui-ci existera.

## Changer de set de gammes

Le choix de la gamme suivante est isolé dans `IScaleGenerator`. Pour essayer une autre règle, on écrit une nouvelle classe et on l'assigne :

```csharp
// Exemple : avancer d'une quinte dans le même mode à chaque changement.
public sealed class CircleOfFifthsGenerator : IScaleGenerator
{
    public Scale Next(Scale current, System.Random random)
    {
        int root = current == null ? 0 : current.Root + 7;
        return Modes.Create(root, Mode.Ionian);
    }
}

// Quelque part au démarrage :
composer.ScaleGenerator = new CircleOfFifthsGenerator();
```

Une gamme n'est pas forcément un mode. `new Scale(root, new[] { 0, 2, 4, 7, 9 }, "pentatonique majeure")` fonctionne aussi.

### Paramètres (Inspector, composant `Composer`)

| Paramètre | Défaut | Effet |
|---|---|---|
| `scaleDuration` | 30 s | Durée d'une gamme. |
| `lowestOctave` | 3 | Octave de départ de la tessiture. |
| `octaveSpan` | 2 | Tessiture en octaves. |
| `maxChordNotes` | 4 | Taille maximale d'un accord. |

## Pistes

- Sets de gammes : cycle des quintes, modulations vers des tons voisins, gammes non occidentales.
- Choix de notes moins aléatoire : mémoire des dernières notes, mouvements conjoints, hauteur liée à la position de la frappe.
- Sélection du set depuis l'Inspector (ScriptableObjects).

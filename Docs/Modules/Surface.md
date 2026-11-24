# Surface

La surface de jeu : ce qu'on voit et la façon dont on la touche.

## Rôle

- Afficher la surface liquide et ses ondes.
- Transformer les appuis (souris, doigts) en **frappes** : position + charge.
- Lancer une onde quand on le lui demande.

## État actuel (V1)

| Fichier | Contenu |
|---|---|
| `RippleSurface.cs` | Prépare la caméra et un quad plein écran, convertit les appuis en frappes, envoie les ondes au shader. |
| `PointerTracker.cs` | Suit les appuis souris et tactiles, mesure leur durée, signale les relâchements. |
| `Resources/PotsuRipple.shader` | Le rendu des ondes, entièrement procédural. |

### Interaction

- Un appui commence au clic ou au toucher, et la frappe part **au relâchement**.
- Charge = durée de maintien / 1,5 s, plafonnée à 1 (`fullChargeSeconds`).
- L'onde part de l'endroit où l'appui a **commencé**.

### Rendu des ondes

Chaque onde est un paquet d'ondes circulaire : une sinusoïde enveloppée d'une gaussienne, qui s'éloigne du centre à vitesse constante, s'atténue avec le temps et se dilue en s'étalant. Les ondes s'additionnent, donc elles interfèrent.

Le shader calcule une hauteur en chaque point, en déduit une normale, et éclaire cette normale. Une eau immobile reste noire : seules les pentes renvoient de la lumière. C'est ce qui donne l'aspect liquide, sans texture.

### Styles de rendu

Le composant `RippleSurface` (objet `Potsu`) a un réglage **Style** :

- **OneBit** (par défaut) : encre noire sur papier blanc, comme les premiers Macintosh. L'image est calculée sur des « gros pixels » de `pixelSize` pixels écran (2 par défaut). Chaque gros pixel est soit noir, soit blanc : la quantité d'encre (pentes éclairées, creux, reflets) est convertie par un **tramage ordonné** (matrice de Bayer 4×4). Une surface immobile reste blanche.
- **Liquid** : le rendu d'origine, reflets clairs sur fond noir.

Le style se change en direct pendant Play. Sur le matériau, `_InkColor` et `_PaperColor` permettent d'autres paires de couleurs, et `_ShadowInk` règle la part d'encre dans les creux des vagues (0 = seulement les pentes éclairées).

Le tramage ordonné donne la trame régulière typique du 1-bit. Le tramage d'Atkinson des vieux Macs, plus organique, est une diffusion d'erreur : il ne se calcule pas pixel par pixel dans un shader et demanderait un post-traitement en plusieurs passes.

Au plus 64 ondes vivent en même temps (frappes et échos confondus). Les plus anciennes sont remplacées. Un écho relance une onde au même endroit, plus faible selon son usure.

### Paramètres (sur le matériau)

| Propriété | Effet |
|---|---|
| `_Speed` | Vitesse de propagation, en hauteurs d'écran par seconde. |
| `_Wavelength` | Écart entre les crêtes. |
| `_PacketWidth` | Épaisseur de l'anneau. |
| `_Damping` | Vitesse d'atténuation. |
| `_Lifetime` | Durée de vie d'une onde. |
| `_Slope` | Relief perçu (force des reflets sur les pentes). |
| `_Intensity`, `_LightColor`, `_LightDir`, `_BaseColor` | Éclairage et couleurs (style liquide). |
| `_InkColor`, `_PaperColor`, `_ShadowInk` | Couleurs et densité d'encre (style 1-bit). |

En Play, sélectionner l'objet `Potsu Surface` pour régler ces valeurs en direct. Le style et la taille des pixels se règlent sur le composant `RippleSurface` de l'objet `Potsu`.

## Pistes

- Retour visuel pendant la charge (une lueur qui grossit sous le doigt).
- Vraie simulation d'eau (équation d'onde sur une grille) pour les rebonds sur les bords et les interférences réalistes.
- Réfraction, caustiques, couleur liée à la gamme ou à la note.

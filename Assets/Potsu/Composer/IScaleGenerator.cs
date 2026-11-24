namespace Potsu.Composition
{
    /// <summary>
    /// Décide de la gamme suivante. C'est le point d'extension du Compositeur :
    /// pour changer de « set » (cycle des quintes, progressions, gammes exotiques…),
    /// il suffit d'écrire une nouvelle implémentation et de l'assigner à
    /// <see cref="Composer.ScaleGenerator"/>.
    /// </summary>
    public interface IScaleGenerator
    {
        /// <param name="current">La gamme en cours, ou null au démarrage.</param>
        /// <param name="random">Générateur aléatoire partagé du Compositeur.</param>
        Scale Next(Scale current, System.Random random);
    }
}

namespace Potsu.Composition
{
    /// <summary>Set par défaut : tonique et mode tirés au hasard, jamais deux fois la même gamme d'affilée.</summary>
    public sealed class RandomModeGenerator : IScaleGenerator
    {
        public Scale Next(Scale current, System.Random random)
        {
            Scale next;
            do
            {
                next = Modes.Create(random.Next(12), (Mode)random.Next(Modes.Count));
            } while (next.SameAs(current));
            return next;
        }
    }
}

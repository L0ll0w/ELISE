using System;

/// <summary>
/// Structure immuable représentant une coordonnée de case sur la grille circulaire (Anneau, Secteur).
/// Remplace l'utilisation de clés chaînes de caractères (ex: "0_3") pour éliminer les allocations mémoire (GC) pendant le combat.
/// </summary>
public readonly struct GridCell : IEquatable<GridCell>
{
    public int Ring { get; }
    public int Sector { get; }

    public GridCell(int ring, int sector)
    {
        Ring = ring;
        Sector = sector;
    }

    public bool Equals(GridCell other)
    {
        return Ring == other.Ring && Sector == other.Sector;
    }

    public override bool Equals(object obj)
    {
        return obj is GridCell other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Ring * 397) ^ Sector;
        }
    }

    public static bool operator ==(GridCell left, GridCell right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GridCell left, GridCell right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"{Ring}_{Sector}";
    }
}

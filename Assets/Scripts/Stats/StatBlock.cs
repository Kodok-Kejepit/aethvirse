using System;
using UnityEngine;

namespace Aethvirse.Stats
{
    public enum Stat { STR, AGI, INT, VIT, SEN }

    /// <summary>The five primary stats (§8). Floats so class growth can be fractional; shown rounded.</summary>
    [Serializable]
    public struct StatBlock
    {
        public float strength;
        public float agility;
        public float intelligence;
        public float vitality;
        public float sense;

        public StatBlock(float str, float agi, float intel, float vit, float sen)
        {
            strength = str;
            agility = agi;
            intelligence = intel;
            vitality = vit;
            sense = sen;
        }

        public float this[Stat stat]
        {
            get => stat switch
            {
                Stat.STR => strength, Stat.AGI => agility, Stat.INT => intelligence, Stat.VIT => vitality, _ => sense,
            };
            set
            {
                switch (stat)
                {
                    case Stat.STR: strength = value; break;
                    case Stat.AGI: agility = value; break;
                    case Stat.INT: intelligence = value; break;
                    case Stat.VIT: vitality = value; break;
                    default: sense = value; break;
                }
            }
        }

        /// <summary>this + other × factor.</summary>
        public StatBlock Plus(StatBlock other, float factor = 1f) => new StatBlock(
            strength + other.strength * factor, agility + other.agility * factor, intelligence + other.intelligence * factor,
            vitality + other.vitality * factor, sense + other.sense * factor);

        public StatBlock Rounded() => new StatBlock(Mathf.Round(strength), Mathf.Round(agility), Mathf.Round(intelligence),
            Mathf.Round(vitality), Mathf.Round(sense));

        public static readonly Stat[] All = { Stat.STR, Stat.AGI, Stat.INT, Stat.VIT, Stat.SEN };
    }
}

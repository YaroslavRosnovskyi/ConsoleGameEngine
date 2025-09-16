using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameBase
{
    public enum GhostBehaviorType
    {
        Patrol,
        Hunt,
        Block,
        Random
    }

    public enum DifficultyLevel
    {
        Easy = 1,
        Medium = 2,
        Hard = 3
    }

    public static class GhostAIConfig
    {
        public static Dictionary<DifficultyLevel, int> VisionRange = new Dictionary<DifficultyLevel, int>
        {
            { DifficultyLevel.Easy, 3 },
            { DifficultyLevel.Medium, 5 },
            { DifficultyLevel.Hard, 7 }
        };

        public static Dictionary<DifficultyLevel, int> GhostVisionRange = new Dictionary<DifficultyLevel, int>
        {
            { DifficultyLevel.Easy, 2 },
            { DifficultyLevel.Medium, 4 },
            { DifficultyLevel.Hard, 6 }
        };

        public static Dictionary<DifficultyLevel, int> PlayerVisionRange = new Dictionary<DifficultyLevel, int>
        {
            { DifficultyLevel.Easy, 4 },
            { DifficultyLevel.Medium, 6 },
            { DifficultyLevel.Hard, 8 }
        };

        public static Dictionary<DifficultyLevel, double> CooperationChance = new Dictionary<DifficultyLevel, double>
        {
            { DifficultyLevel.Easy, 0.1 },
            { DifficultyLevel.Medium, 0.3 },
            { DifficultyLevel.Hard, 0.6 }
        };

        public static Dictionary<DifficultyLevel, List<GhostBehaviorType>> ActiveBehaviors = 
            new Dictionary<DifficultyLevel, List<GhostBehaviorType>>
        {
            { 
                DifficultyLevel.Easy, 
                new List<GhostBehaviorType> { GhostBehaviorType.Patrol, GhostBehaviorType.Random } 
            },
            { 
                DifficultyLevel.Medium, 
                new List<GhostBehaviorType> { GhostBehaviorType.Patrol, GhostBehaviorType.Hunt, GhostBehaviorType.Random } 
            },
            { 
                DifficultyLevel.Hard, 
                new List<GhostBehaviorType> { GhostBehaviorType.Patrol, GhostBehaviorType.Hunt, GhostBehaviorType.Block, GhostBehaviorType.Random } 
            }
        };
    }
}

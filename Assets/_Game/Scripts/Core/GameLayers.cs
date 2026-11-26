using UnityEngine;

namespace RunChildRun
{
    /// <summary>Physics layer names set up in Tags and Layers.</summary>
    public static class GameLayers
    {
        public const string PlayerName = "Player";
        public const string EnemyName = "Enemy";
        public const string ObstacleName = "Obstacle";

        public static int Player => LayerMask.NameToLayer(PlayerName);
        public static int Enemy => LayerMask.NameToLayer(EnemyName);
        public static int Obstacle => LayerMask.NameToLayer(ObstacleName);
    }

    /// <summary>Sprite sorting orders so enemy bullets always draw on top (GDD 9).</summary>
    public static class SortOrder
    {
        public const int Floor = -100;
        public const int Ground = -10;
        public const int Obstacle = 0;
        public const int Item = 5;
        public const int Enemy = 10;
        public const int Player = 20;
        public const int PlayerProjectile = 30;
        public const int Fx = 40;
        public const int EnemyProjectile = 60;
    }
}

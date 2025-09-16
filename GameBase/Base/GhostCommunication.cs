using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameBase
{
    public class PlayerInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Direction LastDirection { get; set; }
        public int TimeSeen { get; set; }
        public int ReporterId { get; set; }
    }

    public static class GhostCommunication
    {
        private static Dictionary<int, PlayerInfo> sharedPlayerInfo = new Dictionary<int, PlayerInfo>();
        private static int currentTick = 0;

        public static void UpdateTick()
        {
            currentTick++;
            
            var keysToRemove = sharedPlayerInfo.Where(kvp => currentTick - kvp.Value.TimeSeen > 10)
                                              .Select(kvp => kvp.Key)
                                              .ToList();
            foreach (var key in keysToRemove)
            {
                sharedPlayerInfo.Remove(key);
            }
        }

        public static void ReportPlayerPosition(int ghostId, int playerX, int playerY, Direction playerDirection)
        {
            sharedPlayerInfo[ghostId] = new PlayerInfo
            {
                X = playerX,
                Y = playerY,
                LastDirection = playerDirection,
                TimeSeen = currentTick,
                ReporterId = ghostId
            };
        }

        public static List<PlayerInfo> GetPlayerInfo(int ghostId, int ghostX, int ghostY, int communicationRange)
        {
            var availableInfo = new List<PlayerInfo>();

            foreach (var info in sharedPlayerInfo.Values)
            {
                if (info.ReporterId == ghostId)
                    continue;

                if (currentTick - info.TimeSeen > 5)
                    continue;

                availableInfo.Add(info);
            }

            return availableInfo;
        }

        public static void Clear()
        {
            sharedPlayerInfo.Clear();
            currentTick = 0;
        }

        public static PlayerInfo GetLatestPlayerInfo(int ghostId, int ghostX, int ghostY, int communicationRange)
        {
            var allInfo = GetPlayerInfo(ghostId, ghostX, ghostY, communicationRange);
            return allInfo.OrderByDescending(info => info.TimeSeen).FirstOrDefault();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameBase
{
    public class Ghost : EntityBase
    {
        public override string name { get; set; } = "Ghost";
        public override char character { get; set; } = Constant.GhostChar;
        
        public int GhostId { get; private set; }
        private static int nextGhostId = 1;
        
        private Direction previousDirection = Direction.NONE;
        private GhostBehaviorType currentBehavior = GhostBehaviorType.Random;
        private DifficultyLevel difficulty = DifficultyLevel.Medium;
        
        private List<Direction> patrolRoute = new List<Direction>();
        private int patrolIndex = 0;
        private int stuckCounter = 0;
        
        private int huntCooldown = 0;
        private PlayerInfo lastKnownPlayerPosition = null;
        
        private bool isBlocking = false;
        private int blockingTurns = 0;
        
        private Queue<Direction> recentMoves = new Queue<Direction>();
        private const int MAX_RECENT_MOVES = 8;
        private int idleCounter = 0;
        private int lastX = -1, lastY = -1;

        public override void Start(GameScene scene, int x, int y)
        {
            base.Start(scene, x, y);
            GhostId = nextGhostId++;
            pixel.BackgroundColor = ConsoleColor.Magenta;
            
            GeneratePatrolRoute();
        }

        public void SetDifficulty(DifficultyLevel level)
        {
            difficulty = level;
        }

        public override void Update()
        {
            UpdateBehavior();
            
            var nextDirection = GetNextDirection();
            
            UpdatePreviousDirection(nextDirection);
            
            ExecuteMovement(nextDirection);
            
            UpdateCounters();
        }

        private void UpdateBehavior()
        {
            var availableBehaviors = GhostAIConfig.ActiveBehaviors[difficulty];
            
            var playerDirection = GetDirectPlayerDirection();
            if (playerDirection != Direction.NONE && availableBehaviors.Contains(GhostBehaviorType.Hunt))
            {
                currentBehavior = GhostBehaviorType.Hunt;
                huntCooldown = 10;
                
                var playerPos = GetPlayerPosition();
                if (playerPos.HasValue)
                {
                    GhostCommunication.ReportPlayerPosition(GhostId, playerPos.Value.x, playerPos.Value.y, Direction.NONE);
                }
                return;
            }
            
            var sharedInfo = GhostCommunication.GetLatestPlayerInfo(GhostId, x, y, 
                GhostAIConfig.GhostVisionRange[difficulty]);
            
            if (sharedInfo != null && availableBehaviors.Contains(GhostBehaviorType.Hunt))
            {
                lastKnownPlayerPosition = sharedInfo;
                currentBehavior = GhostBehaviorType.Hunt;
                huntCooldown = 5;
                return;
            }
            
            if (availableBehaviors.Contains(GhostBehaviorType.Block) && 
                random.NextDouble() < GhostAIConfig.CooperationChance[difficulty] &&
                idleCounter < 3 && !IsStuckInCycle())
            {
                var nearbyGhosts = GetNearbyGhosts();
                if (nearbyGhosts.Count > 0 && !isBlocking)
                {
                    currentBehavior = GhostBehaviorType.Block;
                    isBlocking = true;
                    blockingTurns = 5;
                    return;
                }
            }
            
            if (huntCooldown <= 0 && blockingTurns <= 0)
            {
                if (availableBehaviors.Contains(GhostBehaviorType.Patrol))
                {
                    currentBehavior = GhostBehaviorType.Patrol;
                }
                else
                {
                    currentBehavior = GhostBehaviorType.Random;
                }
            }
        }

        private Direction GetNextDirection()
        {
            switch (currentBehavior)
            {
                case GhostBehaviorType.Hunt:
                    return GetHuntDirection();
                case GhostBehaviorType.Block:
                    return GetBlockDirection();
                case GhostBehaviorType.Patrol:
                    return GetPatrolDirection();
                case GhostBehaviorType.Random:
                default:
                    return GetRandomDirection();
            }
        }

        private Direction GetHuntDirection()
        {
            var directDirection = GetDirectPlayerDirection();
            if (directDirection != Direction.NONE)
                return directDirection;
            
            if (lastKnownPlayerPosition != null)
            {
                var direction = GetDirectionToPoint(lastKnownPlayerPosition.X, lastKnownPlayerPosition.Y);
                if (direction != Direction.NONE)
                    return direction;
            }
            
            return GetRandomDirection();
        }

        private Direction GetBlockDirection()
        {
            var directions = GetAvailableDirections();
            
            if (directions.Count > 1)
            {
                directions.RemoveAll(d => d == GetOppositeDirection(previousDirection));
                
                if (recentMoves.Count > 0)
                {
                    var recentMovesArray = recentMoves.ToArray();
                    var filteredDirections = directions.Where(d => !recentMovesArray.Contains(d)).ToList();
                    if (filteredDirections.Count > 0)
                        directions = filteredDirections;
                }
            }
            
            if (blockingTurns <= 2)
            {
                return GetRandomDirection();
            }
            
            Direction bestDirection = Direction.NONE;
            int maxOpenPaths = 0;
            
            foreach (var dir in directions)
            {
                var openPaths = CountOpenPathsInDirection(dir);
                if (openPaths > maxOpenPaths)
                {
                    maxOpenPaths = openPaths;
                    bestDirection = dir;
                }
            }
            
            return bestDirection != Direction.NONE ? bestDirection : GetRandomDirection();
        }

        private Direction GetPatrolDirection()
        {
            if (patrolRoute.Count == 0)
            {
                GeneratePatrolRoute();
                return GetRandomDirection();
            }
            
            var targetDirection = patrolRoute[patrolIndex];
            var availableDirections = GetAvailableDirections();
            
            if (availableDirections.Contains(targetDirection))
            {
                patrolIndex = (patrolIndex + 1) % patrolRoute.Count;
                stuckCounter = 0;
                return targetDirection;
            }
            else
            {
                stuckCounter++;
                if (stuckCounter > 3)
                {
                    GeneratePatrolRoute();
                    stuckCounter = 0;
                }
                return GetRandomDirection();
            }
        }

        private Direction GetRandomDirection()
        {
            var directions = GetAvailableDirections();
            
            if (directions.Count > 1)
                directions.RemoveAll(d => d == GetOppositeDirection(previousDirection));
            
            if (directions.Count > 1 && recentMoves.Count > 0)
            {
                var recentMovesArray = recentMoves.ToArray();
                var filteredDirections = directions.Where(d => !recentMovesArray.Contains(d)).ToList();
                
                if (filteredDirections.Count > 0)
                    directions = filteredDirections;
            }
            
            if (directions.Count == 0)
                return Direction.NONE;
            
            return directions[random.Next(0, directions.Count)];
        }

        private Direction GetDirectPlayerDirection()
        {
            var playerVisionRange = GhostAIConfig.PlayerVisionRange[difficulty];
            
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                if (direction == Direction.NONE) continue;
                
                for (int i = 1; i <= playerVisionRange; i++)
                {
                    var entity = GetEntityInDirection(direction, i);
                    if (entity == null) break;
                    
                    if (entity.entityType == EntityType.WALL || entity.entityType == EntityType.GHOST)
                        break;
                    
                    if (entity.entityType == EntityType.PLAYER)
                        return direction;
                }
            }
            
            return Direction.NONE;
        }

        private List<Direction> GetAvailableDirections()
        {
            var directions = new List<Direction>();
            var visionRange = GhostAIConfig.VisionRange[difficulty];
            
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                if (direction == Direction.NONE) continue;
                
                var entity = GetEntityInDirection(direction, 1);
                if (entity != null && entity.entityType != EntityType.WALL && entity.entityType != EntityType.GHOST)
                {
                    directions.Add(direction);
                }
            }
            
            return directions;
        }

        private Direction GetDirectionToPoint(int targetX, int targetY)
        {
            int deltaX = targetX - x;
            int deltaY = targetY - y;
            
            var availableDirections = GetAvailableDirections();
            
            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                var preferredDirection = deltaX > 0 ? Direction.RIGHT : Direction.LEFT;
                if (availableDirections.Contains(preferredDirection))
                    return preferredDirection;
            }
            else if (Math.Abs(deltaY) > 0)
            {
                var preferredDirection = deltaY > 0 ? Direction.DOWN : Direction.UP;
                if (availableDirections.Contains(preferredDirection))
                    return preferredDirection;
            }
            
            if (Math.Abs(deltaY) > 0)
            {
                var alternativeDirection = deltaY > 0 ? Direction.DOWN : Direction.UP;
                if (availableDirections.Contains(alternativeDirection))
                    return alternativeDirection;
            }
            
            if (Math.Abs(deltaX) > 0)
            {
                var alternativeDirection = deltaX > 0 ? Direction.RIGHT : Direction.LEFT;
                if (availableDirections.Contains(alternativeDirection))
                    return alternativeDirection;
            }
            
            return Direction.NONE;
        }

        private List<Ghost> GetNearbyGhosts()
        {
            var nearbyGhosts = new List<Ghost>();
            var ghostVisionRange = GhostAIConfig.GhostVisionRange[difficulty];
            
            for (int dy = -ghostVisionRange; dy <= ghostVisionRange; dy++)
            {
                for (int dx = -ghostVisionRange; dx <= ghostVisionRange; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int checkX = x + dx;
                    int checkY = y + dy;
                    
                    if (checkX < 0 || checkY < 0 || checkX >= gridView.GetLength(1) || checkY >= gridView.GetLength(0))
                        continue;
                    
                    var entities = gridView[checkY, checkX];
                    if (entities != null)
                    {
                        foreach (var entity in entities)
                        {
                            if (entity is Ghost ghost && ghost.GhostId != this.GhostId)
                            {
                                nearbyGhosts.Add(ghost);
                            }
                        }
                    }
                }
            }
            
            return nearbyGhosts;
        }

        private (int x, int y)? GetPlayerPosition()
        {
            var playerVisionRange = GhostAIConfig.PlayerVisionRange[difficulty];
            
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                if (direction == Direction.NONE) continue;
                
                for (int i = 1; i <= playerVisionRange; i++)
                {
                    var entity = GetEntityInDirection(direction, i);
                    if (entity == null) break;
                    
                    if (entity.entityType == EntityType.WALL || entity.entityType == EntityType.GHOST)
                        break;
                    
                    if (entity.entityType == EntityType.PLAYER)
                    {
                        int playerX = x;
                        int playerY = y;
                        
                        switch (direction)
                        {
                            case Direction.LEFT: playerX -= i; break;
                            case Direction.RIGHT: playerX += i; break;
                            case Direction.UP: playerY -= i; break;
                            case Direction.DOWN: playerY += i; break;
                        }
                        
                        return (playerX, playerY);
                    }
                }
            }
            
            return null;
        }

        private int CountOpenPathsInDirection(Direction direction)
        {
            int count = 0;
            var entity = GetEntityInDirection(direction, 1);
            
            if (entity != null && entity.entityType != EntityType.WALL && entity.entityType != EntityType.GHOST)
            {
                count++;
                
                for (int i = 2; i <= 3; i++)
                {
                    var nextEntity = GetEntityInDirection(direction, i);
                    if (nextEntity != null && nextEntity.entityType != EntityType.WALL && nextEntity.entityType != EntityType.GHOST)
                        count++;
                    else
                        break;
                }
            }
            
            return count;
        }

        private void GeneratePatrolRoute()
        {
            patrolRoute.Clear();
            var routeLength = random.Next(3, 7);
            
            for (int i = 0; i < routeLength; i++)
            {
                var directions = new[] { Direction.UP, Direction.DOWN, Direction.LEFT, Direction.RIGHT };
                patrolRoute.Add(directions[random.Next(directions.Length)]);
            }
            
            patrolIndex = 0;
        }

        private Direction GetOppositeDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.UP: return Direction.DOWN;
                case Direction.DOWN: return Direction.UP;
                case Direction.LEFT: return Direction.RIGHT;
                case Direction.RIGHT: return Direction.LEFT;
                default: return Direction.NONE;
            }
        }

        private void UpdatePreviousDirection(Direction nextDirection)
        {
            if (nextDirection != Direction.NONE)
                previousDirection = nextDirection;
        }

        private void ExecuteMovement(Direction nextDirection)
        {
            var destX = x;
            var destY = y;
            
            switch (nextDirection)
            {
                case Direction.UP: destY--; break;
                case Direction.DOWN: destY++; break;
                case Direction.LEFT: destX--; break;
                case Direction.RIGHT: destX++; break;
            }

            var destEntity = GetEntityInDirection(nextDirection, 1);
            if (destEntity != null && destEntity.entityType == EntityType.PLAYER)
                StartTransition(TransitionType.Dead);

            if (destX != x || destY != y)
            {
                Move(destX, destY);
                
                TrackMovement(nextDirection);
            }
        }
        
        private void TrackMovement(Direction direction)
        {
            if (direction != Direction.NONE)
            {
                recentMoves.Enqueue(direction);
                
                while (recentMoves.Count > MAX_RECENT_MOVES)
                {
                    recentMoves.Dequeue();
                }
            }
        }

        private void UpdateCounters()
        {
            if (huntCooldown > 0) huntCooldown--;
            if (blockingTurns > 0) 
            {
                blockingTurns--;
                if (blockingTurns <= 0)
                    isBlocking = false;
            }
            
            if (lastX == x && lastY == y)
            {
                idleCounter++;
            }
            else
            {
                idleCounter = 0;
                lastX = x;
                lastY = y;
            }
            
            if (idleCounter > 5 || IsStuckInCycle())
            {
                ResetBehavior();
            }
        }
        
        private bool IsStuckInCycle()
        {
            if (recentMoves.Count < MAX_RECENT_MOVES)
                return false;
                
            var moves = recentMoves.ToArray();
            
            if (moves.Length >= 2)
            {
                var lastMove = moves[moves.Length - 1];
                var secondLastMove = moves[moves.Length - 2];
                
                if (GetOppositeDirection(lastMove) == secondLastMove)
                {
                    int backForthCount = 0;
                    for (int i = moves.Length - 2; i >= 1; i--)
                    {
                        if (GetOppositeDirection(moves[i]) == moves[i - 1])
                            backForthCount++;
                    }
                    
                    return backForthCount >= 2;
                }
            }
            
            return false;
        }
        
        private void ResetBehavior()
        {
            currentBehavior = GhostBehaviorType.Random;
            isBlocking = false;
            blockingTurns = 0;
            huntCooldown = 0;
            stuckCounter = 0;
            idleCounter = 0;
            recentMoves.Clear();
            
            GeneratePatrolRoute();
            
            var availableDirections = GetAvailableDirections();
            if (availableDirections.Count > 0)
            {
                var randomDir = availableDirections[random.Next(availableDirections.Count)];
            }
        }
    }
}

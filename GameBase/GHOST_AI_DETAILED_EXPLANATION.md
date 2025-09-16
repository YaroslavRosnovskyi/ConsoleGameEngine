# 👻 Детальний розбір поведінки привидів

## 🧠 Загальна архітектура ШІ

Кожен привид - це **автономний агент** з обмеженим сприйняттям та простими правилами, які разом створюють складну **емержентну поведінку**.

## 📊 Структура даних привида

### **Ідентифікація:**
```csharp
public int GhostId { get; private set; }           // Унікальний ID (1, 2, 3...)
private static int nextGhostId = 1;                // Лічильник для ID
```

### **Поведінкові параметри:**
```csharp
private Direction previousDirection = Direction.NONE;              // Останній напрямок руху
private GhostBehaviorType currentBehavior = GhostBehaviorType.Random; // Поточна поведінка
private DifficultyLevel difficulty = DifficultyLevel.Medium;       // Рівень складності
```

### **Система патрулювання:**
```csharp
private List<Direction> patrolRoute = new List<Direction>();  // Маршрут патрулювання
private int patrolIndex = 0;                                  // Поточний крок маршруту
private int stuckCounter = 0;                                 // Лічильник застрягання
```

### **Система полювання:**
```csharp
private int huntCooldown = 0;                                 // Час полювання
private PlayerInfo lastKnownPlayerPosition = null;           // Остання відома позиція гравця
```

### **Система блокування:**
```csharp
private bool isBlocking = false;                              // Чи блокує привид
private int blockingTurns = 0;                                // Скільки тактів блокувати
```

### **Антициклічні механізми:**
```csharp
private Queue<Direction> recentMoves = new Queue<Direction>(); // Останні рухи
private const int MAX_RECENT_MOVES = 4;                       // Максимум рухів для запам'ятовування
private int idleCounter = 0;                                  // Лічильник бездіяльності
private int lastX = -1, lastY = -1;                          // Остання позиція
```

## 🔄 Основний цикл оновлення

### **Метод Update() - серце ШІ:**
```csharp
public override void Update()
{
    // 1. Аналіз ситуації та вибір поведінки
    UpdateBehavior();
    
    // 2. Визначення напрямку руху на основі поточної поведінки
    var nextDirection = GetNextDirection();
    
    // 3. Запам'ятовування попереднього напрямку
    UpdatePreviousDirection(nextDirection);
    
    // 4. Виконання руху
    ExecuteMovement(nextDirection);
    
    // 5. Оновлення внутрішніх лічильників та станів
    UpdateCounters();
}
```

## 🧩 Система вибору поведінки

### **UpdateBehavior() - "Мозок" привида:**

#### **1. Пріоритет 1: Пряме бачення гравця**
```csharp
var playerDirection = GetDirectPlayerDirection();
if (playerDirection != Direction.NONE && availableBehaviors.Contains(GhostBehaviorType.Hunt))
{
    currentBehavior = GhostBehaviorType.Hunt;
    huntCooldown = 10; // Полюємо 10 тактів
    
    // 🗣️ КОМУНІКАЦІЯ: Повідомляємо іншим привидам!
    var playerPos = GetPlayerPosition();
    if (playerPos.HasValue)
    {
        GhostCommunication.ReportPlayerPosition(GhostId, playerPos.Value.x, playerPos.Value.y, Direction.NONE);
    }
    return;
}
```

#### **2. Пріоритет 2: Інформація від інших привидів**
```csharp
var sharedInfo = GhostCommunication.GetLatestPlayerInfo(GhostId, x, y, 
    GhostAIConfig.GhostVisionRange[difficulty]);

if (sharedInfo != null && availableBehaviors.Contains(GhostBehaviorType.Hunt))
{
    lastKnownPlayerPosition = sharedInfo;
    currentBehavior = GhostBehaviorType.Hunt;
    huntCooldown = 5; // Коротше полювання за непрямою інформацією
    return;
}
```

#### **3. Пріоритет 3: Кооперативне блокування**
```csharp
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
```

#### **4. Пріоритет 4: Базова поведінка**
```csharp
if (huntCooldown <= 0 && blockingTurns <= 0)
{
    if (availableBehaviors.Contains(GhostBehaviorType.Patrol))
        currentBehavior = GhostBehaviorType.Patrol;
    else
        currentBehavior = GhostBehaviorType.Random;
}
```

## 🎯 Чотири типи поведінки

### **1. 🔍 ПОЛЮВАННЯ (Hunt)**

#### **GetHuntDirection() - Активне переслідування:**
```csharp
private Direction GetHuntDirection()
{
    // Спочатку перевіряємо пряме бачення гравця
    var directDirection = GetDirectPlayerDirection();
    if (directDirection != Direction.NONE)
        return directDirection;  // Рухаємося прямо до гравця
    
    // Якщо є інформація від інших привидів, рухаємося до останньої відомої позиції
    if (lastKnownPlayerPosition != null)
    {
        var direction = GetDirectionToPoint(lastKnownPlayerPosition.X, lastKnownPlayerPosition.Y);
        if (direction != Direction.NONE)
            return direction;  // Рухаємося до останньої відомої позиції
    }
    
    // Якщо нічого не знаємо, рухаємося випадково
    return GetRandomDirection();
}
```

### **2. 🚧 БЛОКУВАННЯ (Block)**

#### **GetBlockDirection() - Стратегічне позиціонування:**
```csharp
private Direction GetBlockDirection()
{
    var directions = GetAvailableDirections();
    
    // Уникаємо повернення назад та нещодавніх рухів
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
    
    // Якщо блокуємо довго, переходимо до випадкового руху
    if (blockingTurns <= 2)
        return GetRandomDirection();
    
    // 🎯 СТРАТЕГІЯ: Шукаємо перехрестя (місця з найбільшою кількістю шляхів)
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
```

### **3. 🚶 ПАТРУЛЮВАННЯ (Patrol)**

#### **GetPatrolDirection() - Рух по маршруту:**
```csharp
private Direction GetPatrolDirection()
{
    if (patrolRoute.Count == 0)
    {
        GeneratePatrolRoute();  // Генеруємо новий маршрут
        return GetRandomDirection();
    }
    
    var targetDirection = patrolRoute[patrolIndex];
    var availableDirections = GetAvailableDirections();
    
    if (availableDirections.Contains(targetDirection))
    {
        // Можемо рухатися по маршруту
        patrolIndex = (patrolIndex + 1) % patrolRoute.Count;  // Циклічний маршрут
        stuckCounter = 0;
        return targetDirection;
    }
    else
    {
        // Застрягли, рахуємо спроби
        stuckCounter++;
        if (stuckCounter > 3)
        {
            GeneratePatrolRoute();  // Генеруємо новий маршрут якщо застрягли
            stuckCounter = 0;
        }
        return GetRandomDirection();
    }
}
```

#### **Генерація маршруту патрулювання:**
```csharp
private void GeneratePatrolRoute()
{
    patrolRoute.Clear();
    var routeLength = random.Next(3, 7);  // Маршрут від 3 до 6 кроків
    
    for (int i = 0; i < routeLength; i++)
    {
        var directions = new[] { Direction.UP, Direction.DOWN, Direction.LEFT, Direction.RIGHT };
        patrolRoute.Add(directions[random.Next(directions.Length)]);
    }
    
    patrolIndex = 0;
}
```

### **4. 🎲 ВИПАДКОВИЙ РУХ (Random)**

#### **GetRandomDirection() - Розумна випадковість:**
```csharp
private Direction GetRandomDirection()
{
    var directions = GetAvailableDirections();
    
    // 🚫 Уникаємо повернення назад
    if (directions.Count > 1)
        directions.RemoveAll(d => d == GetOppositeDirection(previousDirection));
    
    // 🔄 Уникаємо нещодавніх рухів, щоб запобігти циклам
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
```

## 👁️ Система сприйняття

### **1. Бачення гравця:**
```csharp
private Direction GetDirectPlayerDirection()
{
    var playerVisionRange = GhostAIConfig.PlayerVisionRange[difficulty];
    
    foreach (Direction direction in Enum.GetValues(typeof(Direction)))
    {
        if (direction == Direction.NONE) continue;
        
        for (int i = 1; i <= playerVisionRange; i++)  // Дивимося на відстань
        {
            var entity = GetEntityInDirection(direction, i);
            if (entity == null) break;  // Вийшли за межі карти
            
            if (entity.entityType == EntityType.WALL || entity.entityType == EntityType.GHOST)
                break;  // Перешкода блокує бачення
            
            if (entity.entityType == EntityType.PLAYER)
                return direction;  // Знайшли гравця!
        }
    }
    
    return Direction.NONE;  // Гравця не видно
}
```

### **2. Бачення інших привидів:**
```csharp
private List<Ghost> GetNearbyGhosts()
{
    var nearbyGhosts = new List<Ghost>();
    var ghostVisionRange = GhostAIConfig.GhostVisionRange[difficulty];
    
    // Сканування квадратної області навколо привида
    for (int dy = -ghostVisionRange; dy <= ghostVisionRange; dy++)
    {
        for (int dx = -ghostVisionRange; dx <= ghostVisionRange; dx++)
        {
            if (dx == 0 && dy == 0) continue;  // Пропускаємо себе
            
            int checkX = x + dx;
            int checkY = y + dy;
            
            // Перевіряємо межі карти
            if (checkX < 0 || checkY < 0 || checkX >= gridView.GetLength(1) || checkY >= gridView.GetLength(0))
                continue;
            
            var entities = gridView[checkY, checkX];
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    if (entity is Ghost ghost && ghost.GhostId != this.GhostId)
                        nearbyGhosts.Add(ghost);
                }
            }
        }
    }
    
    return nearbyGhosts;
}
```

## 🗣️ Система комунікації

### **Структура інформації:**
```csharp
public class PlayerInfo
{
    public int X { get; set; }                    // Позиція X гравця
    public int Y { get; set; }                    // Позиція Y гравця
    public Direction LastDirection { get; set; }  // Напрямок руху гравця
    public int TimeSeen { get; set; }            // Коли бачили (такт)
    public int ReporterId { get; set; }          // Хто повідомив
}
```

### **Повідомлення про гравця:**
```csharp
GhostCommunication.ReportPlayerPosition(GhostId, playerX, playerY, Direction.NONE);
```

### **Отримання інформації:**
```csharp
var sharedInfo = GhostCommunication.GetLatestPlayerInfo(GhostId, x, y, 
    GhostAIConfig.GhostVisionRange[difficulty]);
```

### **Застарівання інформації:**
- Інформація видаляється через **10 тактів**
- Для полювання використовується тільки **5 тактів**

## ⚙️ Конфігурація складності

### **Легкий рівень:**
```csharp
VisionRange: 3          // Бачення лабіринту
GhostVisionRange: 2     // Бачення привидів
PlayerVisionRange: 4    // Бачення гравця
CooperationChance: 0.1  // 10% кооперації
ActiveBehaviors: [Patrol, Random]  // Тільки патрулювання та випадковий рух
```

### **Середній рівень:**
```csharp
VisionRange: 5          
GhostVisionRange: 4     
PlayerVisionRange: 6    
CooperationChance: 0.3  // 30% кооперації
ActiveBehaviors: [Patrol, Hunt, Random]  // + полювання
```

### **Важкий рівень:**
```csharp
VisionRange: 7          
GhostVisionRange: 6     
PlayerVisionRange: 8    
CooperationChance: 0.6  // 60% кооперації
ActiveBehaviors: [Patrol, Hunt, Block, Random]  // Всі поведінки
```

## 🛡️ Антициклічні механізми

### **Детекція циклів:**
```csharp
private bool IsStuckInCycle()
{
    if (recentMoves.Count < MAX_RECENT_MOVES) return false;
    
    var moves = recentMoves.ToArray();
    
    // Перевіряємо на цикл "вперед-назад"
    if (moves.Length >= 2)
    {
        var lastMove = moves[moves.Length - 1];
        var secondLastMove = moves[moves.Length - 2];
        
        if (GetOppositeDirection(lastMove) == secondLastMove)
        {
            // Рахуємо кількість циклів
            int backForthCount = 0;
            for (int i = moves.Length - 2; i >= 1; i--)
            {
                if (GetOppositeDirection(moves[i]) == moves[i - 1])
                    backForthCount++;
            }
            
            return backForthCount >= 2; // Якщо більше 2 разів вперед-назад
        }
    }
    
    return false;
}
```

### **Скидання поведінки:**
```csharp
private void ResetBehavior()
{
    // Скидаємо всі стани
    currentBehavior = GhostBehaviorType.Random;
    isBlocking = false;
    blockingTurns = 0;
    huntCooldown = 0;
    stuckCounter = 0;
    idleCounter = 0;
    recentMoves.Clear();
    
    // Генеруємо новий маршрут
    GeneratePatrolRoute();
}
```

## 🎭 Емержентна поведінка

### **Що виникає природно:**

1. **Координовані атаки** - привиди несвідомо оточують гравця через комунікацію
2. **Територіальний контроль** - різні привиди патрулюють різні зони
3. **Адаптивна стратегія** - поведінка змінюється залежно від дій гравця
4. **Колективна пам'ять** - інформація поширюється між привидами
5. **Блокування відступу** - один привид полює, інші блокують шляхи

### **Приклад емержентної поведінки:**
1. Привид A бачить гравця → переходить в режим Hunt
2. Привид A повідомляє іншим про позицію гравця
3. Привид B отримує інформацію → теж переходить в Hunt
4. Привид C поруч з B → активує Block (кооперація)
5. Результат: скоординована атака без центрального управління!

## 🔧 Технічні деталі

### **Порядок виконання в тіку:**
1. `GhostCommunication.UpdateTick()` - оновлення глобальної інформації
2. `ghost.Update()` для кожного привида:
   - `UpdateBehavior()` - аналіз ситуації
   - `GetNextDirection()` - вибір напрямку
   - `ExecuteMovement()` - рух
   - `UpdateCounters()` - оновлення станів

### **Запобігання конфліктів:**
- Використання черги виконання
- Копія сітки для кожного об'єкта
- Атомарні операції руху

Ця система створює **складну, непередбачувану та цікаву поведінку** привидів, використовуючи тільки **прості правила** та **обмежену інформацію**! 🎮👻

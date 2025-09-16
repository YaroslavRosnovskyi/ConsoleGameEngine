# Архітектура ігрового двигуна PacMan Console

## 🎮 Загальний огляд

Цей ігровий двигун побудований за принципом **Entity-Component System (ECS)** з консольним рендерингом. Він використовує тактову систему оновлень та сітчасту структуру для зберігання об'єктів.

## 📋 Основні компоненти

### 1. **Точка входу - Program.cs**
```csharp
static void Main(string[] args)
{
    new GameController().Run();
}
```
- Запускає головний контролер гри
- Простий entry point без додаткової логіки

### 2. **Головний контролер - GameController.cs**
```csharp
public void Run()
{
    while (true)
    {
        Intro() → DifficultyScene() → GameScene() → [Finish/Dead]Scene()
    }
}
```

**Основний цикл гри:**
1. **Intro Scene** - головне меню
2. **Difficulty Scene** - вибір складності
3. **Game Scene** - основна гра з тактовим циклом
4. **End Scenes** - екрани завершення (перемога/поразка)

**Тактовий цикл:**
```csharp
while (s.transition == TransitionType.None)
{
    sw.Start();
    s.Tick();                    // Оновлення логіки
    sw.Stop();
    
    // Контроль FPS (400ms між кадрами)
    var target = GameLoopDelay - elapsed;
    Task.Delay(target).Wait();
}
```

### 3. **Ігрова сцена - GameScene.cs**

#### Структура даних:
```csharp
public LinkedList<EntityBase>[,] grid;  // Сітка 21x15
```
- **2D масив** зв'язаних списків
- Кожна клітинка може містити **множину об'єктів**
- Розмір: 21x15 клітинок (згідно Constant.cs)

#### Завантаження рівня:
```csharp
public void Load(string filePath)
{
    // Читає map.txt
    // Створює об'єкти за символами:
    // 'C' → Player, '@' → Ghost, '?' → Wall, '.' → Score, ' ' → Space
}
```

#### Основний цикл оновлення:
```csharp
public void Tick()
{
    // 1. Оновлення ШІ комунікації
    GhostCommunication.UpdateTick();
    
    // 2. Збір всіх об'єктів в чергу
    var executionQueue = new Queue<EntityBase>();
    for (y, x in grid)
        executionQueue.Enqueue(all entities);
    
    // 3. Виконання Update() для кожного об'єкта
    while (queue not empty)
        item.Update();
    
    // 4. Рендеринг
    renderer.Render(grid);
}
```

**🎯 Ключова особливість:** Використання черги запобігає подвійному оновленню об'єктів, які рухаються під час того ж тіку.

### 4. **Базовий об'єкт - EntityBase.cs**

#### Абстрактний клас для всіх ігрових об'єктів:
```csharp
public abstract class EntityBase
{
    // Позиція
    public int x, y { get; private set; }
    
    // Візуальне представлення
    public Pixel pixel;
    public abstract char character;
    
    // Логіка
    public abstract void Update();
    
    // Доступ до ігрового світу
    public EntityBase GetEntityInDirection(Direction, distance);
    public LinkedList<EntityBase>[,] gridView;
}
```

#### Система руху:
```csharp
public void Move(int x, int y)
{
    // Видаляє з поточної позиції
    scene.grid[this.y, this.x].RemoveFirst();
    
    // Додає в нову позицію
    scene.grid[y, x].AddFirst(this);
    
    // Оновлює координати
    this.x = x; this.y = y;
}
```

### 5. **Рендеринг - Renderer.cs**

#### Консольний рендерер з масштабуванням:
```csharp
// Базовий розмір: 21x15
// Масштаб: x4 по горизонталі, x2 по вертикалі
// Фінальний розмір консолі: 84x30
```

#### Процес рендерингу:
```csharp
public void Render(LinkedList<EntityBase>[,] grid)
{
    for (y, x in grid)
    {
        var topEntity = grid[y, x].First.Value;  // Верхній об'єкт
        
        // Встановлює кольори
        Console.ForegroundColor = topEntity.pixel.ForegroundColor;
        Console.BackgroundColor = topEntity.pixel.BackgroundColor;
        
        // Виводить символ з масштабуванням
        Console.Write(topEntity.character);
    }
}
```

**🎨 Візуальна система:**
- Кожен об'єкт має `Pixel` з кольорами фону та тексту
- Рендериться тільки **верхній об'єкт** зі стеку в кожній клітинці
- Масштабування для кращої видимості

### 6. **Система вводу - Input.cs**

#### Клавіатурний ввід:
```csharp
public InputStatus GetInput()
{
    // Використовує System.Windows.Input.Keyboard
    // Перевіряє стрілки: Up, Down, Left, Right
    // Повертає поточний стан (не події)
}
```

## 🏗️ Архітектурні принципи

### 1. **Entity-Component Pattern**
- `EntityBase` - базова сутність
- Кожен тип (Player, Ghost, Wall) наслідує і реалізує свою логіку
- Компоненти: позиція, візуал, поведінка

### 2. **Grid-Based World**
- Світ розділений на дискретні клітинки
- Кожна клітинка - стек об'єктів (LinkedList)
- Рух відбувається між клітинками

### 3. **Tick-Based Updates**
- Синхронні оновлення всіх об'єктів
- Фіксований FPS (400ms між кадрами)
- Детерміністична логіка

### 4. **State Machine**
- Сцени як стани: Intro → Difficulty → Game → End
- Переходи через TransitionType
- Циклічність гри (повернення до Intro)

## 🎲 Ігрові об'єкти

### **Player (Гравець)**
- Символ: 'C'
- Колір: DarkYellow
- Логіка: Керування клавіатурою, збір очок

### **Ghost (Привид)**
- Символ: '@'
- Колір: Magenta
- Логіка: Складна ШІ система (патрулювання, полювання, блокування)

### **Wall (Стіна)**
- Символ: '?'
- Логіка: Статичний об'єкт, блокує рух

### **Score (Очко)**
- Символ: '.'
- Логіка: Знищується при зборі, рахує прогрес

### **Space (Пустота)**
- Символ: ' '
- Логіка: Фонова клітинка, завжди присутня

## ⚡ Оптимізації та особливості

### 1. **Execution Queue**
- Запобігає подвійному оновленню об'єктів
- Гарантує порядок виконання

### 2. **Grid View**
- Кожен об'єкт має доступ до копії сітки
- Безпечний доступ до сусідніх об'єктів

### 3. **Масштабування рендерингу**
- Покращує читаність на консолі
- Налаштовується через константи

### 4. **Система переходів**
- Елегантне завершення гри
- Можливість різних кінцівок

## 🔧 Налаштування

### Константи (Constant.cs):
```csharp
WindowXSize = 21      // Ширина ігрового поля
WindowYSize = 15      // Висота ігрового поля
WindowXScale = 4      // Масштаб по X
WindowYScale = 2      // Масштаб по Y
GameLoopDelay = 400   // Затримка між тіками (мс)
```

### Символи об'єктів:
```csharp
PlayerChar = 'C'      // Пакмен
GhostChar = '@'       // Привид
WallChar = '?'        // Стіна
ScoreChar = '.'       // Очко
SpaceChar = ' '       // Пустота
```

## 🎯 Переваги архітектури

✅ **Простота** - Зрозуміла структура  
✅ **Розширюваність** - Легко додавати нові об'єкти  
✅ **Детермінізм** - Передбачувана поведінка  
✅ **Продуктивність** - Ефективне використання пам'яті  
✅ **Модульність** - Розділені відповідальності  

Цей двигун ідеально підходить для 2D ігор з сітчастою структурою та покроковою логікою!

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameBase
{
    public class GameScene
    {
        public LinkedList<EntityBase>[,] grid;
        public int xSize;
        public int ySize;
        public Renderer renderer;
        public Input input;
        public TransitionType transition = TransitionType.None;
        public DifficultyLevel difficulty = DifficultyLevel.Medium;

        private int ScoreInMap = 0;


        public GameScene(int x, int y, Renderer renderer, DifficultyLevel difficultyLevel = DifficultyLevel.Medium)
        {
            grid = new LinkedList<EntityBase>[y, x];
            xSize = x;
            ySize = y;
            difficulty = difficultyLevel;

            this.renderer = renderer;
            Renderer.Clear();
            input = new Input();
            
            GhostCommunication.Clear();
        }

        public void Load(string filePath)
        {
            var text = File.ReadAllText(filePath);
            text = text.Replace("\r", "");
            var list = text.Split('\n');
            for (var y = 0; y < ySize; y++)
            {
                var line = list[y];
                for (int x = 0; x < xSize; x++)
                {
                    var character = line[x];
                    var linkedList = new LinkedList<EntityBase>();
                    grid[y, x] = linkedList;

                    linkedList.AddFirst(new Space());

                    if (character == Constant.PlayerChar)
                    {
                        var p = new Player();
                        p.Start(this, x, y);
                        linkedList.AddFirst(p);
                    }
                    else if (character == Constant.GhostChar)
                    {
                        var p = new Ghost();
                        p.SetDifficulty(difficulty);
                        p.Start(this, x, y);
                        linkedList.AddFirst(p);
                    }
                    else if (character == Constant.WallChar)
                    {
                        var p = new Wall();
                        p.Start(this, x, y);
                        linkedList.AddFirst(p);
                    }
                    else if (character == Constant.ScoreChar)
                    {
                        var p = new Score();
                        p.Start(this, x, y);
                        linkedList.AddFirst(p);
                        ScoreInMap++;
                    }

                }
            }
        }

        public void Tick()
        {
            GhostCommunication.UpdateTick();
            
            var executionQueue = new Queue<EntityBase>();
            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    var entities = grid[y, x];
                    var item = entities.First;
                    while (item != null)
                    {
                        executionQueue.Enqueue(item.Value);
                        item = item.Next;
                    }
                }
            }

            while (executionQueue.Count > 0 && transition == TransitionType.None)
            {
                var item = executionQueue.Dequeue();
                if (!item.isDestroyed)
                    item.Update();
            }

            renderer.Render(grid);
        }

        public void DestroyEntity(EntityBase e)
        {
            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    var entities = grid[y, x];
                    var item = entities.First;
                    while (item != null)
                    {
                        if (item.Value == e)
                        {
                            entities.Remove(item);

                            if(item.Value is Score)
                            {
                                ScoreInMap--;
                                if (ScoreInMap <= 0)
                                    StartTransition(TransitionType.Finish);
                            }
                        }
                        item = item.Next;
                    }
                }
            }
        }

        public void StartTransition(TransitionType type)
        {
            transition = type;
        }


    }
}

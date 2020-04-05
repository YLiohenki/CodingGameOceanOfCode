using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
public class Program
{
    static void Main(string[] args)
    {
        var seed = new Random().Next();
        Console.Error.WriteLine(seed);
        var program = new Program();
        List<string> forInput = new List<string>();
        var input = Console.ReadLine();
        Console.Error.WriteLine(input);
        forInput.Add(input);
        int height = int.Parse(input.Split(' ')[1]);
        for (int y = 0; y < height; y++)
        {
            string line = Console.ReadLine();
            forInput.Add(line);
            Console.Error.WriteLine(line);
        }

        // Write an action using Console.WriteLine()
        // To debug: Console.Error.WriteLine("Debug messages...");
        Console.WriteLine(program.InitGame(forInput, seed));

        // game loop
        while (true)
        {
            var fromConsole = new List<string>() { Console.ReadLine(), Console.ReadLine(), Console.ReadLine() };
            Console.Error.WriteLine(fromConsole[0]);
            Console.Error.WriteLine(fromConsole[1]);
            Console.Error.WriteLine(fromConsole[2]);
            Console.WriteLine(program.GameCycle(fromConsole));
        }
    }

    public string InitGame(List<string> fromConsole, int seed)
    {
        map = new Map();
        var inputs = fromConsole[0].Split(' ');
        int width = int.Parse(inputs[0]);
        int height = int.Parse(inputs[1]);
        decider = new Decider(seed, width, height);
        int myId = int.Parse(inputs[2]);
        map.height = height;
        map.width = width;
        map.islands = new bool[width, height];
        map.reachSilence = new bool[width, height];
        map.visited = new bool[width, height];
        map.enemyPossibility.map = new bool[width, height];
        map.mePossibility.map = new bool[width, height];
        map.reachTorpedo = new int[width, height];
        map.enemyPossibility.total = 0;
        map.mePossibility.total = 0;

        map.enemyPath = new bool[width * 2 - 1, height * 2 - 1];
        map.myPath = new bool[width * 2 - 1, height * 2 - 1];
        map.enemyPath[width - 1, height - 1] = true;
        map.myPath[width - 1, height - 1] = true;
        for (int y = 0; y < height; y++)
        {
            string line = fromConsole[1 + y];
            var islands = line.ToCharArray().Select(c => c == 'x').ToArray();
            for (int x = 0; x < width; x++)
            {
                var isIsland = islands[x];
                map.islands[x, y] = isIsland;
                map.enemyPossibility.map[x, y] = !isIsland;
                map.enemyPossibility.total += isIsland ? 0 : 1;
                map.mePossibility.map[x, y] = !isIsland;
                map.mePossibility.total += isIsland ? 0 : 1;
            }
        }

        // Write an action using Console.WriteLine()
        // To debug: Console.Error.WriteLine("Debug messages...");

        return decider.FindStartingSpot(map);
    }

    public Player me = new Player();
    public Player enemy = new Player();
    public Decider decider;
    public Map map = new Map();
    public string GameCycle(List<string> fromConsole)
    {

        var inputs = fromConsole[0].Split(' ');
        int x = int.Parse(inputs[0]);
        int y = int.Parse(inputs[1]);
        int myLife = int.Parse(inputs[2]);
        int oppLife = int.Parse(inputs[3]);
        int torpedoCooldown = int.Parse(inputs[4]);
        int sonarCooldown = int.Parse(inputs[5]);
        int silenceCooldown = int.Parse(inputs[6]);
        int mineCooldown = int.Parse(inputs[7]);
        string sonarResult = fromConsole[1];
        string opponentOrders = fromConsole[2];
        me.x = x;
        me.y = y;
        me.health = myLife;
        me.sonarCooldown = sonarCooldown;
        me.silenceCooldown = silenceCooldown;
        me.mineCooldown = mineCooldown;
        me.torpedoCooldown = torpedoCooldown;
        enemy.health = oppLife;
        if (sonarResult == "Y" || sonarResult == "N")
        {
            me.previousOrders.Last().actions.Find(a => a.type == ActionType.sonar).result = sonarResult == "Y";
        }

        enemy.previousOrders.Add(Order.ParceOrder(opponentOrders));

        var order = decider.Decide(map, me, enemy);

        me.previousOrders.Add(order);

        return order.ToString();
    }
}
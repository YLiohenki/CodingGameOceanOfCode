using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Player
{
    public int health;
    public int x;
    public int y;
    public int torpedoCooldown;
    public int sonarCooldown;
    public int silenceCooldown;
    public int mineCooldown;
    public List<Order> previousOrders = new List<Order>();
}
public class MapSquare
{
    public bool island;
    public bool visited;
    public bool enemyPossible;
    public int reachTorpedo;
    public bool reachSilence;
}
public class Map
{
    public int height, width;
    public int totalPossibleEnemySpots = 225;
    public bool canReachPossibleEnemy = false;
    public MapSquare[,] squares;
    public bool[,] enemyPath;

    public string[] getMap(string prop)
    {
        var result = new string[height];
        for (int i = 0; i < width; i++)
        {
            var line = "";
            for (int j = 0; j < height; j++)
            {
                if (prop == "enemyPossible")
                    line += squares[j, i].enemyPossible == true ? 'x' : '-';
            }
            result[i] = line;
        }
        return result;
    }
}

public enum Ability
{
    TORPEDO,
    SONAR,
    SILENCE,
    MINE
}

public enum ActionType
{
    move,
    surface,
    torpedo,
    silence,
    sonar,
    mine
}

public class Action
{
    public ActionType type;
    public int x;
    public int y;
    public int distance;
    public char direction;
    public int sector;
    public Ability charge;
}

public class Order
{
    public List<Action> actions = new List<Action>();

    public override string ToString()
    {
        var results = new List<string>();
        foreach (var action in actions)
        {
            switch (action.type)
            {
                case ActionType.move:
                    results.Add($"MOVE {action.direction} {action.charge}");
                    break;
                case ActionType.surface:
                    results.Add($"SURFACE");
                    break;
                case ActionType.torpedo:
                    results.Add($"TORPEDO {action.x} {action.y}");
                    break;
                case ActionType.silence:
                    results.Add($"SILENCE {action.direction} {action.distance}");
                    break;
                case ActionType.sonar:
                    results.Add($"SONAR {action.sector}");
                    break;
                case ActionType.mine:
                    results.Add($"MINE {action.direction}");
                    break;
            }
        }
        return string.Join("|", results);
    }


    public static Order ParceOrder(string input)
    {
        //implement sequence
        var inputs = input.ToUpper().Split('|').Select(s => s.Trim());
        var result = new Order();
        foreach (var str in inputs)
        {
            if (str.StartsWith("MOVE"))
            {
                result.actions.Add(new Action() { type = ActionType.move, direction = str[5] });
            }

            if (str.StartsWith("TORPEDO"))
            {
                var x = int.Parse(str.Split(' ')[1]);
                var y = int.Parse(str.Split(' ')[2]);
                result.actions.Add(new Action() { type = ActionType.torpedo, x = x, y = y });
            }

            if (str.StartsWith("SURFACE"))
            {
                var sector = int.Parse(str.Split(' ')[1]);
                result.actions.Add(new Action() { type = ActionType.surface, sector = sector });
            }

            if (str.StartsWith("SILENCE"))
            {
                result.actions.Add(new Action() { type = ActionType.silence });
            }

            if (str.StartsWith("SONAR"))
            {
                var sector = int.Parse(str.Split(' ')[1]);
                result.actions.Add(new Action() { type = ActionType.sonar, sector = sector });
            }

            if (str.StartsWith("MINE"))
            {
            }
        }
        return result;
    }
}

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

public class PossiblePositions
{
    public bool[,] map;
    public int total = 225;

    public PossiblePositions Clone()
    {
        var result = new PossiblePositions();
        result.map = map.Clone() as bool[,];
        result.total = total;
        return result;
    }
}
public class TorpedoPosition
{
    public int x;
    public int y;
    public double enemyExpectedDamage;
    public double myDamage;
    public bool canReach;
    public int[,] reach;
}
public class Map
{
    public int height, width;
    public PossiblePositions enemyPossibility = new PossiblePositions();
    public PossiblePositions mePossibility = new PossiblePositions();
    public TorpedoPosition torpedo = new TorpedoPosition();
    public bool[,] islands;
    public List<int[]> islandsList;
    public double[,] probablyDamage;
    public bool[,] enemyPath;
    public bool[,] myPath;
    public bool[,] visited;
    public bool[,] reachSilence;
    public int[,] paint;

    public string[] getMap(string prop)
    {
        if (prop == "enemyPossibility" || prop == "mePossibility" || prop == "visited" || prop == "islands" || prop == "reachSilence")
        {
            var result = new string[height];
            for (int i = 0; i < height; ++i)
            {
                var line = "";
                for (int j = 0; j < width; ++j)
                {
                    if (prop == "enemyPossibility")
                        line += enemyPossibility.map[j, i] == true ? 'x' : '-';
                    else if (prop == "mePossibility")
                        line += mePossibility.map[j, i] == true ? 'x' : '-';
                    else if (prop == "visited")
                        line += visited[j, i] == true ? 'x' : '-';
                    else if (prop == "islands")
                        line += islands[j, i] == true ? 'x' : '-';
                    else if (prop == "reachSilence")
                        line += reachSilence[j, i] == true ? 'x' : '-';
                }
                result[i] = line;
            }
            return result;
        }
        if (prop == "enemyPath" || prop == "myPath")
        {
            var result = new string[height * 2 - 1];
            for (int i = 0; i < height * 2 - 1; ++i)
            {
                var line = "";
                for (int j = 0; j < width * 2 - 1; ++j)
                {
                    if (prop == "enemyPath")
                        line += enemyPath[j, i] == true ? 'x' : '-';
                    else if (prop == "myPath")
                        line += myPath[j, i] == true ? 'x' : '-';
                }
                result[i] = line;
            }
            return result;
        }
        return null;
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
    mine,
    trigger
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
    public bool result;
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
                result.actions.Add(new Action() { type = ActionType.mine });
            }
        }
        return result;
    }
}

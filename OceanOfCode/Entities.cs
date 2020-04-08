using System.Collections.Generic;
using System.Linq;

public class Player
{
    public int health;
    public int previousHealth;
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
public class PathMark
{
    public Mine mine = null;
    public bool silenceIn = false;
    public bool silenceOut = false;
    public bool visited = false;
    public double probabilityVisited = 0.0;
    public bool processed = false;
    public PathMark Clone()
    {
        return new PathMark() { mine = mine, silenceIn = silenceIn, silenceOut = silenceOut, probabilityVisited = probabilityVisited, visited = visited, processed = processed };
    }
}

public class Point
{
    public Point(int _x, int _y)
    {
        x = _x;
        y = _y;
    }
    public int x;
    public int y;
}

public class Minefield
{
    public List<Point> mines = new List<Point>();
    public bool[,] map;
}
public class TorpedoPosition
{
    public double evalFire;
    public int x;
    public int y;
    public double enemyExpectedDamage;
    public double myDamage;
    public bool canReach;
    public int oldProssibility;
    public int newProssibility;
    public int[,] toReach;
    public int[,] fromReach;
}
public enum VisitedState
{
    unknow = 0,
    fresh = 1,
    old = 2
}

public class Mine
{
    public int totalSpaceOccupied;
}

public class ProbableMines
{
    public double[,] probability;
    public List<Mine>[,] mapList;
}
public class Map
{
    public int height, width;
    public PossiblePositions enemyPossibility = new PossiblePositions();
    public List<PossiblePositions> enemyPossibilityHistory = new List<PossiblePositions>();
    public PossiblePositions myPossibility = new PossiblePositions();
    public List<PossiblePositions> myPossibilityHistory = new List<PossiblePositions>();
    public TorpedoPosition torpedo = new TorpedoPosition();
    public bool[,] islands;
    public List<Point> islandsList;
    public double[,] probablyDamage;
    public PathMark[,] enemyPath;
    public PathMark[,] myPath;
    public bool[,] visited;
    public VisitedState[,] enemyDerivedVisited;
    public VisitedState[,] myDerivedVisited;
    public bool[,] reachSilence;
    public int[,] paint;
    public ProbableMines myProbableMines = new ProbableMines();
    public ProbableMines enemyProbableMines = new ProbableMines();
    public Minefield myMinefield = new Minefield();
    public PathMark[,] ClonePath(PathMark[,] path)
    {
        var result = new PathMark[path.GetLength(0), path.GetLength(1)];
        for (int x = 0; x < path.GetLength(0); ++x)
        {
            for (int y = 0; y < path.GetLength(1); y++)
            {
                result[x, y] = path[x, y].Clone();
            }
        }
        return result;
    }

    public string[] getMap(string prop)
    {
        if (prop == "enemyPossibility" || prop == "mePossibility" || prop == "visited" || prop == "islands" || prop == "reachSilence" || prop == "enemyDerivedVisited" || prop == "myDerivedVisited")
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
                        line += myPossibility.map[j, i] == true ? 'x' : '-';
                    else if (prop == "visited")
                        line += visited[j, i] == true ? 'x' : '-';
                    else if (prop == "islands")
                        line += islands[j, i] == true ? 'x' : '-';
                    else if (prop == "reachSilence")
                        line += reachSilence[j, i] == true ? 'x' : '-';
                    else if (prop == "enemyDerivedVisited")
                        line += enemyDerivedVisited[j, i] == VisitedState.unknow ? 'u' : (enemyDerivedVisited[j, i] == VisitedState.old ? 'o' : 'f');
                    else if (prop == "myDerivedVisited")
                        line += myDerivedVisited[j, i] == VisitedState.unknow ? 'u' : (enemyDerivedVisited[j, i] == VisitedState.old ? 'o' : 'f');
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
                        line += enemyPath[j, i].visited == true ? 'x' : '-';
                    else if (prop == "myPath")
                        line += myPath[j, i].visited == true ? 'x' : '-';
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
                case ActionType.trigger:
                    results.Add($"TRIGGER {action.x} {action.y}");
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

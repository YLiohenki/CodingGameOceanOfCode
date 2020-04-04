using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Decider
{
    public Decider(int seed)
    {
        rand = new Random(seed);
    }
    private Random rand;

    public void fillPossibleTorpedoes(Map map, Player me)
    {
        for (int i = 0; i < map.width; i++)
        {
            for (int j = 0; j < map.height; j++)
            {
                map.squares[i, j].reachTorpedo = 0;
            }
        }
        map.canReachPossibleEnemy = false;
        fillPossibleTorpedoesRecurs(map, me.x, me.y, 5);
    }

    public void fillPossibleTorpedoesRecurs(Map map, int x, int y, int step)
    {
        if (S.isOutOfBoundsOrIsland(map, x, y) || step <= 0 || map.squares[x, y].reachTorpedo >= step)
            return;
        map.squares[x, y].reachTorpedo = step;
        if (map.squares[x, y].enemyPossible)
        {
            map.canReachPossibleEnemy = true;
        }
        fillPossibleTorpedoesRecurs(map, x, y - 1, step - 1);
        fillPossibleTorpedoesRecurs(map, x - 1, y, step - 1);
        fillPossibleTorpedoesRecurs(map, x, y + 1, step - 1);
        fillPossibleTorpedoesRecurs(map, x + 1, y, step - 1);
    }

    public void fillPossibleEnemyTorpedoRecurs(Map map, int x, int y, int step)
    {
        if (S.isOutOfBoundsOrIsland(map, x, y) || step <= 0 || map.squares[x, y].reachTorpedo >= step)
            return;
        map.squares[x, y].reachTorpedo = step;
        fillPossibleTorpedoesRecurs(map, x, y - 1, step - 1);
        fillPossibleTorpedoesRecurs(map, x - 1, y, step - 1);
        fillPossibleTorpedoesRecurs(map, x, y + 1, step - 1);
        fillPossibleTorpedoesRecurs(map, x + 1, y, step - 1);
    }

    private void clearVisitedPlace(Map map)
    {
        for (int i = 0; i < map.height; i++)
        {
            for (int j = 0; j < map.width; j++)
            {
                map.squares[i, j].visited = false;
            }
        }
    }

    private void processEnemyMove(Map map, Action action)
    {
        var dX = -S.MoveX(action.direction);
        var dY = -S.MoveY(action.direction);
        for (int i = -1; i <= map.width; i++)
        {
            for (int j = -1; j <= map.height; j++)
            {
                var x = dX >= 0 ? i : map.width - 1 - i;
                var y = dY >= 0 ? j : map.height - 1 - j;
                if (S.isOutOfBoundsOrIsland(map, x, y))
                {
                    if (!S.isOutOfBoundsOrIsland(map, x + dX, y + dY) && map.squares[x + dX, y + dY].enemyPossible)
                    {
                        map.totalPossibleEnemySpots -= 1;
                    }
                    continue;
                }
                if (S.isOutOfBoundsOrIsland(map, x + dX, y + dY))
                {
                    map.squares[x, y].enemyPossible = false;
                }
                else
                {
                    map.squares[x, y].enemyPossible = map.squares[x + dX, y + dY].enemyPossible;
                }
            }
        }
        for (int x = dX >= 0 ? 0 : map.width * 2 - 2; dX >= 0 ? x < map.width * 2 - 1 : x >= 0; x += dX >= 0 ? 1 : -1)
        {
            for (int y = dY >= 0 ? 0 : map.height * 2 - 2; dY >= 0 ? y < map.height * 2 - 1 : y >= 0; y += dY >= 0 ? 1 : -1)
            {
                if (x + dX < 0 || x + dX >= map.height * 2 - 1 || y + dY < 0 || y + dY >= map.height * 2 - 1)
                {
                    map.enemyPath[x, y] = false;
                }
                else
                {
                    map.enemyPath[x, y] = map.enemyPath[x + dX, y + dY];
                }
            }
        }
        map.enemyPath[map.width - 1, map.height - 1] = true;
    }

    private void processEnemyTorped(Map map, Action action)
    {
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                map.squares[x, y].reachTorpedo = 0;
            }
        }
        fillPossibleEnemyTorpedoRecurs(map, action.x, action.y, 5);
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                if (map.squares[x, y].reachTorpedo == 0)
                {
                    if (map.squares[x, y].enemyPossible)
                    {
                        map.totalPossibleEnemySpots -= 1;
                    }
                    map.squares[x, y].enemyPossible = false;
                }
            }
        }
    }

    private void processEnemySurface(Map map, Action action)
    {
        map.totalPossibleEnemySpots = 0;
        S.SectorToCoord(action.sector, out int minX, out int maxX, out int minY, out int maxY);
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                if (x < minX || x > maxX || y < minY || y > maxY || S.isOutOfBoundsOrIsland(map, x, y))
                {
                    map.squares[x, y].enemyPossible = false;
                }
                else if (map.squares[x, y].enemyPossible)
                {
                    map.totalPossibleEnemySpots += 1;
                }
            }
        }
        S.EraseEnemyPath(map);
    }

    private void processEnemySilence(Map map, Action action)
    {
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                map.squares[x, y].reachSilence = false;
            }
        }

        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                if (map.squares[x, y].enemyPossible)
                {
                    foreach (var dir in S.possibleDirections)
                    {
                        var dX = S.MoveX(dir);
                        var dY = S.MoveY(dir);
                        for (int k = 1; k <= 4; k++)
                        {
                            if (S.isOutOfBoundsOrIsland(map, x + dX * k, y + dY * k) || map.enemyPath[S.enemyPathCenterX + dX, S.enemyPathCenterY + dY])
                            {
                                break;
                            }
                            map.squares[x, y].reachSilence = true;
                        }
                    }
                }
            }
        }
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                if (map.squares[x, y].reachSilence)
                {
                    map.squares[x, y].enemyPossible = true;
                    ++map.totalPossibleEnemySpots;
                }
            }
        }
        //TODO: implement branching instead of erasing memory here:
        S.EraseEnemyPath(map);
    }
    private void processEnemySonar(Map map, Action action)
    {
    }
    private void processEnemyAction(Map map, Action action)
    {
        switch (action.type)
        {
            case ActionType.move:
                processEnemyMove(map, action);
                break;
            case ActionType.surface:
                processEnemySurface(map, action);
                break;
            case ActionType.torpedo:
                processEnemyTorped(map, action);
                break;
            case ActionType.silence:
                processEnemySilence(map, action);
                break;
            case ActionType.sonar:
                processEnemySonar(map, action);
                break;
        }
    }
    public Ability DecideOnCharge(Map map, Player me, Player enemy)
    {
        if (map.totalPossibleEnemySpots < 10 && me.torpedoCooldown > 0)
            return Ability.TORPEDO;
        if (me.sonarCooldown > 0)
            return Ability.SONAR;
        if (me.silenceCooldown > 0)
            return Ability.SILENCE;
        return Ability.TORPEDO;
    }
    public Order Decide(Map map, Player me, Player enemy)
    {
        if (me.previousOrders.Count > 0 && me.previousOrders.Last().actions.Any(a => a.type == ActionType.surface))
        {
            clearVisitedPlace(map);
        }
        if (enemy.previousOrders.Count > 0)
        {
            enemy.previousOrders.Last().actions.ForEach(a => processEnemyAction(map, a));
        }
        map.squares[me.x, me.y].visited = true;
        var result = new Order();
        var moved = false;
        if (me.torpedoCooldown == 0)
        {
            var foundTorpedoPlace = false;
            this.fillPossibleTorpedoes(map, me);
            if (map.canReachPossibleEnemy && map.totalPossibleEnemySpots < 10)
            {
                var x = enemy.x;
                var y = enemy.y;
                while (!foundTorpedoPlace)
                {
                    if (!S.isOutOfBoundsOrIsland(map, x, y) && map.squares[x, y].reachTorpedo > 0 && map.squares[x, y].enemyPossible)
                    {
                        if (map.totalPossibleEnemySpots == 1 || (enemy.x != 0 || Math.Abs(x - me.x) > 1 || Math.Abs(y - me.y) > 1))
                        {
                            foundTorpedoPlace = true;
                            moved = true;
                            result.actions.Add(new Action()
                            {
                                type = ActionType.torpedo,
                                x = x,
                                y = y
                            });
                        }
                    }
                    x = me.x + this.rand.Next(-4, 5);
                    y = me.y + this.rand.Next(-4, 5);
                }
            }
        }
        if (!moved)
        {
            var direction = this.findMoveDirection(map, me);
            if (direction != null)
            {
                result.actions.Add(new Action()
                {
                    type = ActionType.move,
                    direction = direction.Value,
                    charge = DecideOnCharge(map, me, enemy)
                });
                moved = true;
            }
        }
        if (!moved)
        {
            result.actions.Add(new Action()
            {
                type = ActionType.surface
            });
        }
        return result;
    }

    private char? findMoveDirection(Map map, Player me)
    {
        char? finalDir = null;
        var minCenterDistance = Math.Sqrt(8 * 8 + 8 * 8);
        foreach (var dir in S.possibleDirections)
        {
            var x = me.x + S.MoveX(dir);
            var y = me.y + S.MoveY(dir);
            if (S.isOutOfBoundsOrIsland(map, x, y) || map.squares[x, y].visited)
            {
                continue;
            }
            var centerDistance = Math.Sqrt((x - S.centerX) * (x - S.centerX) + (y - S.centerY) * (y - S.centerY));
            if (centerDistance < minCenterDistance)
            {
                finalDir = dir;
                minCenterDistance = centerDistance;
            }
        }
        return finalDir;
    }

    public string FindStartingSpot(Map map)
    {
        var x = rand.Next(map.width);
        var y = rand.Next(map.height);
        while (map.squares[x, y].island == true)
        {
            x = rand.Next(map.width);
            y = rand.Next(map.height);
        }
        return $"{x} {y}";
    }
}

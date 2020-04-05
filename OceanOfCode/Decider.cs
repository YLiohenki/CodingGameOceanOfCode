using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Decider
{
    public Decider(int _seed, int _height, int _width)
    {
        rand = new Random(_seed);
        this.height = _height;
        this.width = _width;
    }
    private Random rand;
    private int height;
    private int width;

    public void fillPossibleTorpedoes(Map map, Player me)
    {
        for (int i = 0; i < map.width; i++)
        {
            for (int j = 0; j < map.height; j++)
            {
                map.reachTorpedo[i, j] = 0;
            }
        }
        map.canReachPossibleEnemy = false;
        fillPossibleTorpedoesRecurs(map, me.x, me.y, 5);
    }

    public void fillPossibleTorpedoesRecurs(Map map, int x, int y, int step)
    {
        if (S.isOutOfBoundsOrIsland(map.islands, x, y, width, height) || step <= 0 || map.reachTorpedo[x, y] >= step)
            return;
        map.reachTorpedo[x, y] = step;
        if (map.enemyPossibility.map[x, y])
        {
            map.canReachPossibleEnemy = true;
        }
        fillPossibleTorpedoesRecurs(map, x, y - 1, step - 1);
        fillPossibleTorpedoesRecurs(map, x - 1, y, step - 1);
        fillPossibleTorpedoesRecurs(map, x, y + 1, step - 1);
        fillPossibleTorpedoesRecurs(map, x + 1, y, step - 1);
    }

    private void clearMyVisitedPlace(Map map, Player me)
    {
        for (int i = 0; i < map.height; i++)
        {
            for (int j = 0; j < map.width; j++)
            {
                map.visited[i, j] = false;
            }
        }
    }

    private void fillPossiblePositionOnMove(PossiblePositions possibility, Action action, bool[,] islands)
    {
        var dX = -S.MoveX(action.direction);
        var dY = -S.MoveY(action.direction);
        for (int i = -1; i <= width; i++)
        {
            for (int j = -1; j <= height; j++)
            {
                var x = dX >= 0 ? i : width - 1 - i;
                var y = dY >= 0 ? j : height - 1 - j;
                if (S.isOutOfBoundsOrIsland(islands, x, y, width, height))
                {
                    if (!S.isOutOfBoundsOrIsland(islands, x + dX, y + dY, width, height) && possibility.map[x + dX, y + dY])
                    {
                        possibility.total -= 1;
                    }
                    continue;
                }
                if (S.isOutOfBoundsOrIsland(islands, x + dX, y + dY, width, height))
                {
                    possibility.map[x, y] = false;
                }
                else
                {
                    possibility.map[x, y] = possibility.map[x + dX, y + dY];
                }
            }
        }
    }

    private void processMove(PossiblePositions possibility, Action action, bool[,] islands, bool[,] recordedPath)
    {
        fillPossiblePositionOnMove(possibility, action, islands);
        var dX = -S.MoveX(action.direction);
        var dY = -S.MoveY(action.direction);
        for (int x = dX >= 0 ? 0 : width * 2 - 2; dX >= 0 ? x < width * 2 - 1 : x >= 0; x += dX >= 0 ? 1 : -1)
        {
            for (int y = dY >= 0 ? 0 : height * 2 - 2; dY >= 0 ? y < height * 2 - 1 : y >= 0; y += dY >= 0 ? 1 : -1)
            {
                if (x + dX < 0 || x + dX >= height * 2 - 1 || y + dY < 0 || y + dY >= height * 2 - 1)
                {
                    recordedPath[x, y] = false;
                }
                else
                {
                    recordedPath[x, y] = recordedPath[x + dX, y + dY];
                }
            }
        }
        recordedPath[width - 1, height - 1] = true;
    }

    private void processTorped(bool[,] islands, int[,] reachTorpedo, Action action, PossiblePositions possibility)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                reachTorpedo[x, y] = 0;
            }
        }
        fillPossibleFireTorpedoPositions(islands, reachTorpedo, action.x, action.y, 5);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (reachTorpedo[x, y] == 0)
                {
                    if (possibility.map[x, y])
                    {
                        possibility.total -= 1;
                    }
                    possibility.map[x, y] = false;
                }
            }
        }
    }

    public void fillPossibleFireTorpedoPositions(bool[,] islands, int[,] reachTorpedo, int x, int y, int step)
    {
        if (S.isOutOfBoundsOrIsland(islands, x, y, width, height) || step <= 0 || reachTorpedo[x, y] >= step)
            return;
        reachTorpedo[x, y] = step;
        fillPossibleFireTorpedoPositions(islands, reachTorpedo, x, y - 1, step - 1);
        fillPossibleFireTorpedoPositions(islands, reachTorpedo, x - 1, y, step - 1);
        fillPossibleFireTorpedoPositions(islands, reachTorpedo, x, y + 1, step - 1);
        fillPossibleFireTorpedoPositions(islands, reachTorpedo, x + 1, y, step - 1);
    }


    public void processSurface(PossiblePositions posibility, Action action, bool[,] recordedPath, bool[,] islands)
    {
        posibility.total = 0;
        S.SectorToCoord(action.sector, out int minX, out int maxX, out int minY, out int maxY);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < minX || x > maxX || y < minY || y > maxY || S.isOutOfBoundsOrIsland(islands, x, y, width, height))
                {
                    posibility.map[x, y] = false;
                }
                else if (posibility.map[x, y])
                {
                    posibility.total += 1;
                }
            }
        }
        S.EraseRecordedPath(recordedPath, width, height);
    }

    public void processSilence(Action action, bool[,] reachSilence, PossiblePositions possibility, bool[,] islands, bool[,] recordedPath)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                reachSilence[x, y] = false;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (possibility.map[x, y])
                {
                    foreach (var dir in S.possibleDirections)
                    {
                        var dX = S.MoveX(dir);
                        var dY = S.MoveY(dir);
                        for (int k = 1; k <= 4; k++)
                        {
                            if (S.isOutOfBoundsOrIsland(islands, x + dX * k, y + dY * k, width, height) || recordedPath[S.enemyPathCenterX + dX * k, S.enemyPathCenterY + dY * k])
                            {
                                break;
                            }
                            reachSilence[x + dX * k, y + dY * k] = true;
                        }
                    }
                }
            }
        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (reachSilence[x, y] && !possibility.map[x, y])
                {
                    possibility.map[x, y] = true;
                    ++possibility.total;
                }
            }
        }
        //TODO: implement branching instead of erasing memory here:
        S.EraseRecordedPath(recordedPath, width, height);
    }
    private void processEnemySonar(Map map, Action action, Player me)
    {
        S.SectorToCoord(action.sector, out int minX, out int maxX, out int minY, out int maxY);
        if (me.x <= maxX && me.x >= minX && me.y <= maxY && me.y >= minY)
        {
            for (int x = 0; x < height; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    if (x > maxX || x < minX || y > maxY || y < minY)
                    {
                        if (map.mePossibility.map[x, y])
                        {
                            map.mePossibility.total -= 1;
                            map.mePossibility.map[x, y] = false;
                        }
                    }
                }
            }
        }
        else
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (map.mePossibility.map[x, y])
                    {
                        map.mePossibility.total -= 1;
                        map.mePossibility.map[x, y] = false;
                    }
                }
            }
        }
    }
    private void processEnemyAction(Map map, Action action, Player me)
    {
        switch (action.type)
        {
            case ActionType.move:
                processMove(map.enemyPossibility, action, map.islands, map.enemyPath);
                break;
            case ActionType.surface:
                processSurface(map.enemyPossibility, action, map.enemyPath, map.islands);
                break;
            case ActionType.torpedo:
                processTorped(map.islands, map.reachTorpedo, action, map.enemyPossibility);
                break;
            case ActionType.silence:
                processSilence(action, map.reachSilence, map.enemyPossibility, map.islands, map.enemyPath);
                break;
            case ActionType.sonar:
                processEnemySonar(map, action, me);
                break;
        }
    }

    private int? findBestSonarPlace(Map map)
    {
        var result = 1;
        var maxEnemyCells = 0;
        var differentPossibleSectors = 0;
        for (var sector = 1; sector <= 9; ++sector)
        {
            var sectorEnemyCells = 0;
            S.SectorToCoord(sector, out int minX, out int maxX, out int minY, out int maxY);
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (map.enemyPossibility.map[x, y])
                    {
                        ++sectorEnemyCells;
                    }
                }
            }
            if (sectorEnemyCells > 0)
            {
                ++differentPossibleSectors;
            }
            if (sectorEnemyCells > maxEnemyCells)
            {
                maxEnemyCells = sectorEnemyCells;
                result = sector;
            }
        }
        return differentPossibleSectors > 1 ? result : (int?)null;
    }
    private void processMySonar(Map map, Action action, Player enemy)
    {
        S.SectorToCoord(action.sector, out int minX, out int maxX, out int minY, out int maxY);
        var lastOrder = enemy.previousOrders.Last();
        var dX = 0;
        var dY = 0;
        var moveAction = lastOrder.actions.Find(a => a.type == ActionType.move);
        if (moveAction != null)
        {
            dX = S.MoveX(moveAction.direction);
            dY = S.MoveY(moveAction.direction);
        }
        var silenceUsed = lastOrder.actions.Any(a => a.type == ActionType.silence);
        if (action.result == false)
        {
            if (silenceUsed)
            {
                return;
            }
            for (int x = minX + dX; x <= maxX + dX; x++)
            {
                for (int y = minY + dY; y <= maxY + dY; y++)
                {
                    if (!S.isOutOfBoundsOrIsland(map.islands, x, y, width, height) && map.enemyPossibility.map[x, y])
                    {
                        map.enemyPossibility.total -= 1;
                        map.enemyPossibility.map[x, y] = false;
                    }
                }
            }
        }
        else
        {
            var enemyMinX = minX + dX - (silenceUsed ? 4 : 0);
            var enemyMaxX = maxX + dX + (silenceUsed ? 4 : 0);
            var enemyMinY = minY + dY - (silenceUsed ? 4 : 0);
            var enemyMaxY = maxY + dY + (silenceUsed ? 4 : 0);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x < enemyMinX || x > enemyMaxX || y < enemyMinY || y > enemyMaxY)
                    {
                        if (map.enemyPossibility.map[x, y])
                        {
                            map.enemyPossibility.total -= 1;
                            map.enemyPossibility.map[x, y] = false;
                        }
                    }
                }
            }
        }
    }
    private void processMyAction(Map map, Action action, Player me, Player enemy)
    {
        switch (action.type)
        {
            case ActionType.move:
                processMove(map.mePossibility, action, map.islands, map.myPath);
                break;
            case ActionType.surface:
                processSurface(map.mePossibility, action, map.myPath, map.islands);
                clearMyVisitedPlace(map, me);
                break;
            case ActionType.torpedo:
                processTorped(map.islands, map.reachTorpedo, action, map.mePossibility);
                break;
            case ActionType.silence:
                processSilence(action, map.reachSilence, map.mePossibility, map.islands, map.myPath);
                break;
            case ActionType.sonar:
                processMySonar(map, action, enemy);
                break;
        }
    }
    public Ability DecideOnCharge(Map map, Player me, Player enemy)
    {
        if (map.enemyPossibility.total < 10 && me.torpedoCooldown > 0)
            return Ability.TORPEDO;
        if (map.enemyPossibility.total > 50 && me.sonarCooldown > 0)
            return Ability.SONAR;
        //if (me.mineCooldown > 0)
        //    return Ability.MINE;
        if (me.silenceCooldown > 0)
            return Ability.SILENCE;
        if (me.torpedoCooldown > 0)
            return Ability.TORPEDO;
        if (me.sonarCooldown > 0)
            return Ability.SONAR;
        return Ability.TORPEDO;
    }
    public Order Decide(Map map, Player me, Player enemy)
    {
        if (me.previousOrders.Count > 0)
        {
            me.previousOrders.Last().actions.ForEach(a => processMyAction(map, a, me, enemy));
        }
        if (enemy.previousOrders.Count > 0)
        {
            enemy.previousOrders.Last().actions.ForEach(a => processEnemyAction(map, a, me));
        }
        map.visited[me.x, me.y] = true;
        var result = new Order();
        var addedActionInCycle = true;
        var moved = false;
        var surfaces = false;
        var torpeded = false;
        var silenced = false;
        var mined = false;
        var soned = false;

        while (addedActionInCycle)
        {
            addedActionInCycle = false;
            if (map.enemyPossibility.total > 5 && me.sonarCooldown == 0 && !soned)
            {
                var sector = findBestSonarPlace(map);
                if (sector != null)
                {
                    result.actions.Add(new Action()
                    {
                        type = ActionType.sonar,
                        sector = sector.Value
                    });
                    addedActionInCycle = true;
                    soned = true;
                }
            }
            if (!addedActionInCycle && map.mePossibility.total < 10 && me.silenceCooldown == 0 && !silenced)
            {
                var action = findBestSilencePlace(map, me);
                if (action != null)
                {
                    result.actions.Add(action);
                    addedActionInCycle = true;
                    silenced = true;
                }
            }
            if (!addedActionInCycle && me.torpedoCooldown == 0 && !torpeded)
            {
                var foundTorpedoPlace = false;
                this.fillPossibleTorpedoes(map, me);
                if (map.canReachPossibleEnemy && map.enemyPossibility.total < 10)
                {
                    var x = enemy.x;
                    var y = enemy.y;
                    while (!foundTorpedoPlace)
                    {
                        if (!S.isOutOfBoundsOrIsland(map.islands, x, y, width, height) && map.reachTorpedo[x, y] > 0 && map.enemyPossibility.map[x, y])
                        {
                            if (map.enemyPossibility.total == 1 || (enemy.x != 0 || Math.Abs(x - me.x) > 1 || Math.Abs(y - me.y) > 1))
                            {
                                foundTorpedoPlace = true;
                                result.actions.Add(new Action()
                                {
                                    type = ActionType.torpedo,
                                    x = x,
                                    y = y
                                });
                                addedActionInCycle = true;
                                torpeded = true;
                            }
                        }
                        x = me.x + this.rand.Next(-4, 5);
                        y = me.y + this.rand.Next(-4, 5);
                    }
                }
            }
            if (!addedActionInCycle && !moved)
            {
                var action = this.findMoveDirection(map, me);
                if (action != null)
                {
                    action.charge = DecideOnCharge(map, me, enemy);
                    result.actions.Add(action);
                    addedActionInCycle = true;
                    moved = true;
                }
            }
            if (!addedActionInCycle && !surfaces && !moved && !silenced && !torpeded && !soned)
            {
                surfaces = true;
                addedActionInCycle = true;
                result.actions.Add(new Action()
                {
                    type = ActionType.surface,
                    sector = S.CoordToSector(me.x, me.y)
                });
            }
        }
        return result;
    }
    private Action findBestSilencePlace(Map map, Player me)
    {
        Action result = null;
        var possibility = map.mePossibility.Clone();
        var action = new Action() { type = ActionType.silence };
        processSilence(action, map.reachSilence, possibility, map.islands, map.myPath);
        if (possibility.total < map.mePossibility.total + 4)
        {
            return null;
        }
        foreach (var dir in S.possibleDirections)
        {
            for (int k = 0; k <= 4; ++k)
            {
                var x = me.x + S.MoveX(dir) * k;
                var y = me.y + S.MoveY(dir) * k;
                if (S.isOutOfBoundsOrIsland(map.islands, x, y, width, height) || map.visited[x, y])
                {
                    break;
                }
                result = new Action()
                {
                    type = ActionType.silence,
                    direction = dir
                };
            }
        }
        return result;
    }
    private Action findMoveDirection(Map map, Player me)
    {
        Action result = null;
        var minCenterDistance = Math.Sqrt(8 * 8 + 8 * 8);
        var maxPossibility = 1;
        foreach (var dir in S.possibleDirections)
        {
            var x = me.x + S.MoveX(dir);
            var y = me.y + S.MoveY(dir);
            if (S.isOutOfBoundsOrIsland(map.islands, x, y, width, height) || map.visited[x, y])
            {
                continue;
            }
            var possibility = map.mePossibility.Clone();
            var action = new Action() { type = ActionType.move, direction = dir };
            fillPossiblePositionOnMove(possibility, action, map.islands);
            var centerDistance = Math.Sqrt((x - S.centerX) * (x - S.centerX) + (y - S.centerY) * (y - S.centerY));
            if (possibility.total <= 0 || possibility.total >= maxPossibility || (possibility.total == maxPossibility && centerDistance < minCenterDistance))
            {
                if (possibility.total <= 0)
                    Console.Error.WriteLine("ERROR MY PROBABILITY CALCULATION");
                result = action;
                maxPossibility = possibility.total;
                minCenterDistance = centerDistance;
            }
        }
        return result;
    }

    public string FindStartingSpot(Map map)
    {
        var x = rand.Next(map.width);
        var y = rand.Next(map.height);
        while (map.islands[x, y] == true)
        {
            x = rand.Next(map.width);
            y = rand.Next(map.height);
        }
        return $"{x} {y}";
    }
}

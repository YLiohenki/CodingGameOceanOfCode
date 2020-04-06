using System;
using System.Collections.Generic;
using System.Linq;

public class Decider
{
    public Decider(int _seed, int _height, int _width)
    {
        rand = new Random(_seed);
        this.height = _height;
        this.width = _width;
        S = new S(width, height);
        W = new W();
    }
    private Random rand;
    private int height;
    private int width;
    private S S;
    private W W;

    public void fillPossibleTorpedoes(Map map, Player me)
    {
        for (int i = 0; i < map.width; i++)
        {
            for (int j = 0; j < map.height; j++)
            {
                map.torpedo.reach[i, j] = 0;
            }
        }
        map.torpedo.canReach = false;
        fillPossibleTorpedoesRecurs(map, me.x, me.y, 5, me);
    }

    public void fillPossibleTorpedoesRecurs(Map map, int x, int y, int step, Player me)
    {
        if (S.isOutOfBoundsOrIsland(map.islands, x, y) || step <= 0 || map.torpedo.reach[x, y] >= step)
            return;
        map.torpedo.reach[x, y] = step;
        var totalEnemyDamage = 0.0;
        foreach (var adj in S.adjustedCells)
        {
            if (!S.isOutOfBoundsOrIsland(map.islands, x + adj[0], y + adj[1]) && map.enemyPossibility.map[x + adj[0], y + adj[1]])
            {
                totalEnemyDamage += 1.0 / map.enemyPossibility.total;
            }
        }
        if (map.enemyPossibility.map[x, y])
        {
            totalEnemyDamage += 2.0 / map.enemyPossibility.total;
        }
        var myDamage = 0.0;
        if (me.x == x && me.y == y)
        {
            myDamage += 2;
        }
        else if (Math.Abs(x - me.x) <= 1 && Math.Abs(y - me.y) <= 1)
        {
            myDamage += 1;
        }
        if (totalEnemyDamage - myDamage >= W.torpedoFileThreshold && totalEnemyDamage - myDamage >= map.torpedo.enemyExpectedDamage - map.torpedo.myDamage)
        {
            map.torpedo.canReach = true;
            map.torpedo.x = x;
            map.torpedo.y = y;
            map.torpedo.enemyExpectedDamage = totalEnemyDamage;
            map.torpedo.myDamage = myDamage;
        }
        fillPossibleTorpedoesRecurs(map, x, y - 1, step - 1, me);
        fillPossibleTorpedoesRecurs(map, x - 1, y, step - 1, me);
        fillPossibleTorpedoesRecurs(map, x, y + 1, step - 1, me);
        fillPossibleTorpedoesRecurs(map, x + 1, y, step - 1, me);
    }

    private void clearMyVisitedPlace(bool[,] visited, int x, int y)
    {
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                visited[i, j] = false;
            }
        }
        visited[x, y] = true;
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
                if (S.isOutOfBoundsOrIsland(islands, x, y))
                {
                    if (!S.isOutOfBoundsOrIsland(islands, x + dX, y + dY) && possibility.map[x + dX, y + dY])
                    {
                        possibility.total -= 1;
                    }
                    continue;
                }
                if (S.isOutOfBoundsOrIsland(islands, x + dX, y + dY))
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
        var dX = S.MoveX(action.direction);
        var dY = S.MoveY(action.direction);
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
        if (S.isOutOfBoundsOrIsland(islands, x, y) || step <= 0 || reachTorpedo[x, y] >= step)
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
                if (x < minX || x > maxX || y < minY || y > maxY || S.isOutOfBoundsOrIsland(islands, x, y))
                {
                    posibility.map[x, y] = false;
                }
                else if (posibility.map[x, y])
                {
                    posibility.total += 1;
                }
            }
        }
        S.EraseRecordedPath(recordedPath);
    }
    public void fillVisitedOnMySilence(bool[,] visited, int x, int y, Action action)
    {
        var dX = -S.MoveX(action.direction);
        var dY = -S.MoveY(action.direction);
        for (int k = 0; k <= action.distance; ++k)
        {
            visited[x + k * dX, y + k * dY] = true;
        }
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
                            if (S.isOutOfBoundsOrIsland(islands, x + dX * k, y + dY * k) || recordedPath[S.enemyPathCenterX + dX * k, S.enemyPathCenterY + dY * k])
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
        S.EraseRecordedPath(recordedPath);
    }
    private int[] paintSectorsAroundBoat(bool[,] islands, bool[,] visited, int boatX, int boatY, int[,] paint)
    {
        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < width; y++)
            {
                paint[x, y] = 0;
            }
        }
        paintSectorsAroundBoatRecurs(islands, visited, boatX + 1, boatY, paint, 1);
        paintSectorsAroundBoatRecurs(islands, visited, boatX - 1, boatY, paint, 2);
        paintSectorsAroundBoatRecurs(islands, visited, boatX, boatY - 1, paint, 3);
        paintSectorsAroundBoatRecurs(islands, visited, boatX, boatY + 1, paint, 4);
        var totalCells = new int[] { 0, 0, 0, 0 };
        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < width; y++)
            {
                if (paint[x, y] != 0)
                    ++totalCells[paint[x, y] - 1];
            }
        }
        return totalCells;
    }
    private void paintSectorsAroundBoatRecurs(bool[,] islands, bool[,] visited, int x, int y, int[,] paint, int color)
    {
        if (S.isOutOfBoundsOrIsland(islands, x, y) || paint[x, y] != 0 || visited[x, y])
            return;
        paint[x, y] = color;
        paintSectorsAroundBoatRecurs(islands, visited, x + 1, y, paint, color);
        paintSectorsAroundBoatRecurs(islands, visited, x - 1, y, paint, color);
        paintSectorsAroundBoatRecurs(islands, visited, x, y - 1, paint, color);
        paintSectorsAroundBoatRecurs(islands, visited, x, y + 1, paint, color);
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
                processTorped(map.islands, map.torpedo.reach, action, map.enemyPossibility);
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
                    if (!S.isOutOfBoundsOrIsland(map.islands, x, y) && map.enemyPossibility.map[x, y])
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
                clearMyVisitedPlace(map.visited, me.x, me.y);
                break;
            case ActionType.torpedo:
                processTorped(map.islands, map.torpedo.reach, action, map.mePossibility);
                break;
            case ActionType.silence:
                processSilence(action, map.reachSilence, map.mePossibility, map.islands, map.myPath);
                fillVisitedOnMySilence(map.visited, me.x, me.y, action);
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
        if (me.mineCooldown > 0)
            return Ability.MINE;
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
            if (!addedActionInCycle && map.mePossibility.total < 40 && me.silenceCooldown == 0 && !silenced)
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
                this.fillPossibleTorpedoes(map, me);
                if (map.torpedo.canReach && map.enemyPossibility.total < 40)
                {
                    result.actions.Add(new Action()
                    {
                        type = ActionType.torpedo,
                        x = map.torpedo.x,
                        y = map.torpedo.y
                    });
                    addedActionInCycle = true;
                    torpeded = true;
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
                if (x != me.x || y != me.y)
                    if (S.isOutOfBoundsOrIsland(map.islands, x, y) || map.visited[x, y])
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
        var maxPossibileCells = double.MinValue;
        foreach (var dir in S.possibleDirections)
        {
            var visitedList = new List<bool[,]> { map.visited };
            var possibilityList = new List<PossiblePositions> { map.mePossibility };
            var steps = 4;
            var currentWay = new char[steps + 2];
            currentWay[1] = dir;
            if (!S.isOutOfBoundsOrIsland(map.islands, me.x + S.MoveX(dir), me.y + S.MoveY(dir)) && !map.visited[me.x + S.MoveX(dir), me.y + S.MoveY(dir)])
            {
                var currentMaxCells = findMoveDirectionRecurs(map.islands, map.paint, me.x + S.MoveX(dir), me.y + S.MoveY(dir), visitedList, possibilityList, steps, 1, ref currentWay);
                if (currentMaxCells > maxPossibileCells)
                {
                    maxPossibileCells = currentMaxCells;
                    result = new Action() { type = ActionType.move, direction = currentWay[1] };
                }
            }
        }
        return result;
    }

    private double findMoveDirectionRecurs(bool[,] islands, int[,] paint, int x, int y, List<bool[,]> visitedList, List<PossiblePositions> possibilityList, int maxDepth, int currentStep, ref char[] currentWay)
    {
        if (S.isOutOfBoundsOrIsland(islands, x, y))
        {
            return double.MinValue;
        }
        if (currentStep >= visitedList.Count)
        {
            visitedList.Add(visitedList.Last().Clone() as bool[,]);
            possibilityList.Add(possibilityList.Last().Clone());
        }
        else
        {
            visitedList[currentStep] = visitedList[currentStep - 1].Clone() as bool[,];
            possibilityList[currentStep] = possibilityList[currentStep - 1].Clone();
        }
        var visitedMap = visitedList[currentStep];
        var visited = visitedMap[x, y];
        var possibility = possibilityList[currentStep];
        var surfaceFine = 0.0;
        if (visited)
        {
            clearMyVisitedPlace(visitedMap, x, y);
            surfaceFine = W.surfaceFine;
        }
        var compactnessBonus = 0.0;
        foreach (var adjust in S.adjustedCells)
        {
            if (S.isOutOfBoundsOrIsland(islands, x + adjust[0], y + adjust[1]) || visitedMap[x + adjust[0], y + adjust[1]])
            {
                compactnessBonus += W.adjustedCellBonus;
            }
        }
        visitedMap[x, y] = true;
        var dir = currentWay[currentStep];
        fillPossiblePositionOnMove(possibility, new Action() { direction = dir, type = ActionType.move }, islands);
        if (currentStep <= maxDepth)
        {
            var maxPossibility = double.MinValue;
            char[] childrenMaxPossibilityWay = null;

            foreach (var newDir in S.possibleDirections)
            {
                currentWay[currentStep + 1] = newDir;
                var newPossibility = findMoveDirectionRecurs(islands, paint, x + S.MoveX(newDir), y + S.MoveY(newDir), visitedList, possibilityList, maxDepth, currentStep + 1, ref currentWay);
                if (newPossibility > maxPossibility)
                {
                    maxPossibility = newPossibility;
                    childrenMaxPossibilityWay = currentWay.Clone() as char[];
                }
            }
            if (childrenMaxPossibilityWay != null)
            {
                var rechableBonus = 0.0;
                if (currentStep == maxDepth)
                {
                    var result = paintSectorsAroundBoat(islands, visitedMap, x, y, paint);
                    rechableBonus = result.Max() * W.reachableCells;
                }
                currentWay = childrenMaxPossibilityWay;
                return maxPossibility + surfaceFine + rechableBonus + compactnessBonus;
            }
            else
                return double.MinValue;
        }
        return possibility.total + surfaceFine + compactnessBonus;
    }

    public string FindStartingSpot(Map map)
    {
        var lowerPushForse = double.MaxValue;
        var resultX = 0;
        var resultY = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!S.isOutOfBoundsOrIsland(map.islands, x, y))
                {
                    var totalPushForce = 0.0;
                    foreach (var island in map.islandsList)
                    {
                        totalPushForce += 1.0 / Math.Sqrt((x - island[0]) * (x - island[0]) + (y - island[1]) * (y - island[1]));
                    }
                    if (totalPushForce < lowerPushForse)
                    {
                        lowerPushForse = totalPushForce;
                        resultX = x;
                        resultY = y;
                    }
                }
            }
        }
        return $"{resultX} {resultY}";
    }
}

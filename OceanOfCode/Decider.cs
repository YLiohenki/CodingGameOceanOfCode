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
        E = new Evaluator(width, height);
    }
    private Random rand;
    private int height;
    private int width;
    private S S;
    private W W;
    private Evaluator E;

    public Action findBestMinePlacement(Map map, Player me)
    {
        Action result = null;
        var minMinePushAwayForce = double.MaxValue;
        foreach (var dir in S.possibleDirections)
        {
            var x = me.x + S.MoveX(dir);
            var y = me.y + S.MoveY(dir);
            if (!S.isOutOfBoundsOrIsland(map.islands, x, y))
            {
                var pushForce = 0.0;
                map.myMinefield.mines.ForEach(mine =>
                {
                    pushForce += W.mineToMinePushForce / Math.Sqrt((x - mine.x) * (x - mine.x) + (y - mine.y) * (y - mine.y));
                });
                map.islandsList.ForEach(island =>
                {
                    pushForce += W.islandToMinePushForce / Math.Sqrt((x - island.x) * (x - island.x) + (y - island.y) * (y - island.y));
                });
                var leftX = -1;
                var rightX = width + 1;
                var topY = -1;
                var bottomY = height + 1;
                for (var i = 0; i < width; ++i)
                {
                    pushForce += W.borderToMinePushForce / Math.Sqrt((x - i) * (x - i) + (y - topY) * (y - topY));
                    pushForce += W.borderToMinePushForce / Math.Sqrt((x - i) * (x - i) + (y - bottomY) * (y - bottomY));
                    pushForce += W.borderToMinePushForce / Math.Sqrt((x - leftX) * (x - leftX) + (y - i) * (y - i));
                    pushForce += W.borderToMinePushForce / Math.Sqrt((x - rightX) * (x - rightX) + (y - i) * (y - i));
                }
                if (pushForce < minMinePushAwayForce)
                {
                    minMinePushAwayForce = pushForce;
                    result = new Action() { type = ActionType.mine, direction = dir, x = x, y = y };
                }
            }
        }
        return result;
    }
    public Action findBestMineTrigger(Map map, Player me)
    {
        Action result = null;
        var maxDamage = 0.0;
        map.myMinefield.mines.ForEach((mine) =>
        {
            var totalEnemyDamage = 0.0;
            foreach (var adj in S.adjustedCells)
            {
                if (!S.isOutOfBoundsOrIsland(map.islands, mine.x + adj[0], mine.y + adj[1]) && map.enemyPossibility.map[mine.x + adj[0], mine.y + adj[1]])
                {
                    totalEnemyDamage += 1.0 / map.enemyPossibility.total;
                }
            }
            if (map.enemyPossibility.map[mine.x, mine.y])
            {
                totalEnemyDamage += 2.0 / map.enemyPossibility.total;
            }
            var myDamage = 0.0;
            if (me.x == mine.x && me.y == mine.y)
            {
                myDamage += 2;
            }
            else if (Math.Abs(mine.x - me.x) <= 1 && Math.Abs(mine.y - me.y) <= 1)
            {
                myDamage += 1;
            }
            if (totalEnemyDamage - myDamage > W.triggerMineThreshold && totalEnemyDamage - myDamage > maxDamage)
            {
                result = new Action() { type = ActionType.trigger, x = mine.x, y = mine.y };
                maxDamage = totalEnemyDamage - myDamage;
            }
        });
        return result;
    }

    public void fillPossibleTorpedoes(Map map, Player me)
    {
        for (int i = 0; i < map.width; i++)
        {
            for (int j = 0; j < map.height; j++)
            {
                map.torpedo.toReach[i, j] = 0;
            }
        }
        map.torpedo.canReach = false;
        map.torpedo.evalFire = 0;
        fillPossibleTorpedoesRecurs(map, me.x, me.y, 5, me);
    }

    public void fillPossibleTorpedoesRecurs(Map map, int x, int y, int step, Player me)
    {
        if (S.isOutOfBoundsOrIsland(map.islands, x, y) || step <= 0 || map.torpedo.toReach[x, y] >= step)
            return;
        map.torpedo.toReach[x, y] = step;
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
        if (totalEnemyDamage - myDamage >= W.torpedoCalculationThreshold)
        {
            var myNewpossibility = map.myPossibility.Clone();
            processFireTorpedo(map.islands, map.torpedo.fromReach, new Action()
            { type = ActionType.torpedo, x = map.torpedo.x, y = map.torpedo.y }, myNewpossibility);
            var evalFire = E.EvaluateTorpedoFire(totalEnemyDamage, myDamage, map.myPossibility.total, myNewpossibility.total);
            if (evalFire >= W.torpedoFireThreshold && evalFire >= map.torpedo.evalFire)
            {
                map.torpedo.evalFire = evalFire;
                map.torpedo.canReach = true;
                map.torpedo.x = x;
                map.torpedo.y = y;
                map.torpedo.enemyExpectedDamage = totalEnemyDamage;
                map.torpedo.myDamage = myDamage;
                map.torpedo.oldProssibility = map.myPossibility.total;
                map.torpedo.newProssibility = myNewpossibility.total;
            }
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

    private void fillPossiblePositionOnMove(PossiblePositions possibility, Action action, bool[,] islands, VisitedState[,] derivedVisisted)
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
                if (derivedVisisted[x, y] == VisitedState.fresh)
                    derivedVisisted[x, y] = VisitedState.old;
            }
        }
    }

    private void processMove(PossiblePositions possibility, Action action, bool[,] islands, PathMark[,] recordedPath, VisitedState[,] derivedVisisted)
    {
        fillPossiblePositionOnMove(possibility, action, islands, derivedVisisted);
        var dX = S.MoveX(action.direction);
        var dY = S.MoveY(action.direction);
        for (int x = dX >= 0 ? 0 : S.pathWidth - 1; dX >= 0 ? x < S.pathWidth : x >= 0; x += (dX >= 0 ? 1 : -1))
        {
            for (int y = dY >= 0 ? 0 : S.pathHeight - 1; dY >= 0 ? y < S.pathHeight : y >= 0; y += (dY >= 0 ? 1 : -1))
            {
                if (x + dX < 0 || x + dX >= S.pathWidth - 1 || y + dY < 0 || y + dY >= S.pathHeight - 1)
                {
                    recordedPath[x, y].visited = false;
                    recordedPath[x, y].processed = false;
                }
                else
                {
                    recordedPath[x, y] = recordedPath[x + dX, y + dY];
                }
            }
        }
        recordedPath[width - 1, height - 1].visited = true;
        recordedPath[width - 1, height - 1].processed = false;
    }

    private void processFireTorpedo(bool[,] islands, int[,] reachTorpedo, Action action, PossiblePositions atackerPossibility)
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
                    if (atackerPossibility.map[x, y])
                    {
                        atackerPossibility.total -= 1;
                    }
                    atackerPossibility.map[x, y] = false;
                }
            }
        }
    }

    private void processDamageImpact(List<Action> damageActions, int targetOldHealth, int targetNewHealth, List<Action> targetActions, PossiblePositions targetPossibility)
    {
        if (targetOldHealth != -1)
        {
            var surface = targetActions.Any(a => a.type == ActionType.surface);
            var dif = targetNewHealth - targetOldHealth - (surface ? 1 : 0);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var shouldErase = false;
                    if (damageActions.Count == 1)
                    {
                        if (dif == 2 && (x != damageActions[0].x || y != damageActions[0].y))
                            shouldErase = true;
                        else if (dif == 1 && (Math.Abs(x - damageActions[0].x) > 1 || Math.Abs(y - damageActions[0].y) > 1)) //hit, but it's too far
                            shouldErase = true;
                        else if (dif == 1 && x == damageActions[0].x && y == damageActions[0].y)//hit but not a direct center
                            shouldErase = true;
                        else if (dif == 0 && Math.Abs(x - damageActions[0].x) <= 1 && Math.Abs(y - damageActions[0].y) <= 1) //didn't hit
                            shouldErase = true;
                    }
                    else
                    {
                        if (dif == 4 && (x != damageActions[0].x || y != damageActions[0].y)) //double direct hit. should be the same spot
                            shouldErase = true;
                        else if (dif == 3 && (x != damageActions[0].x || y != damageActions[0].y) && (x != damageActions[1].x || y != damageActions[1].y)) // at least on direct hit, should be on the spot of 1 action
                            shouldErase = true;
                        else if (dif == 2 && (x != damageActions[0].x || y != damageActions[0].y) && (x != damageActions[1].x || y != damageActions[1].y))//not direct hit
                            shouldErase = true;
                        else if (dif == 2 && !(Math.Abs(x - damageActions[0].x) <= 1 && Math.Abs(y - damageActions[0].y) <= 1 && Math.Abs(x - damageActions[1].x) <= 1 && Math.Abs(y - damageActions[1].y) <= 1))//and not close to both shots simulteniously
                            shouldErase = true;
                        else if (dif == 1 && (Math.Abs(x - damageActions[0].x) > 1 || Math.Abs(y - damageActions[0].y) > 1) && (Math.Abs(x - damageActions[1].x) > 1 || Math.Abs(y - damageActions[1].y) > 1)) // not close to any
                            shouldErase = true;
                        else if (dif == 0 && (Math.Abs(x - damageActions[0].x) <= 1 && Math.Abs(y - damageActions[0].y) <= 1) && (Math.Abs(x - damageActions[1].x) <= 1 || Math.Abs(y - damageActions[1].y) <= 1)) // no hit, but it's close to some damage center
                            shouldErase = true;
                    }
                    if (shouldErase)
                    {
                        if (targetPossibility.map[x, y])
                        {
                            targetPossibility.total -= 1;
                        }
                        targetPossibility.map[x, y] = false;
                    }
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


    public void processSurface(PossiblePositions posibility, Action action, PathMark[,] recordedPath, bool[,] islands, VisitedState[,] derivedPath)
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
                derivedPath[x, y] = VisitedState.unknow;
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
    public void processMyMine(Action action, Map map)
    {
        map.myMinefield.map[action.x, action.y] = true;
        map.myMinefield.mines.Add(new Point(action.x, action.y));
    }
    public void processMyTrigger(Action action, Map map)
    {
        map.myMinefield.map[action.x, action.y] = false;
        map.myMinefield.mines = map.myMinefield.mines.Where(m => !(m.x == action.x && m.y == action.y)).ToList();
    }

    public void processSilence(Action action, bool[,] reachSilence, PossiblePositions possibility, bool[,] islands, PathMark[,] recordedPath)
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
                            if (S.isOutOfBoundsOrIsland(islands, x + dX * k, y + dY * k) || recordedPath[S.pathCenterX + dX * k, S.pathCenterY + dY * k].visited)
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
                        if (map.myPossibility.map[x, y])
                        {
                            map.myPossibility.total -= 1;
                            map.myPossibility.map[x, y] = false;
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
                    if (map.myPossibility.map[x, y])
                    {
                        map.myPossibility.total -= 1;
                        map.myPossibility.map[x, y] = false;
                    }
                }
            }
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
    private void processMySonar(Map map, Action action)
    {
        S.SectorToCoord(action.sector, out int minX, out int maxX, out int minY, out int maxY);
        if (action.result == false)
        {
            for (int x = minX; x <= maxX; ++x)
            {
                for (int y = minY; y <= maxY; ++y)
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
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x < minX || x > maxX || y < minY || y > maxY)
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
    public void processEnemyMine(ProbableMines enemyProbableMines, PossiblePositions possibility, Action action)
    {
        var mine = new Mine();

        for (var x = 0; x < width; ++x)
        {
            for (var y = 0; y < width; ++y)
            {
                if (possibility.map[x, y])
                { 
                }
            }
        }
    }
    private void processEnemyAction(Map map, Action action, Player me)
    {
        switch (action.type)
        {
            case ActionType.move:
                processMove(map.enemyPossibility, action, map.islands, map.enemyPath, map.enemyDerivedVisited);
                break;
            case ActionType.surface:
                processSurface(map.enemyPossibility, action, map.enemyPath, map.islands, map.enemyDerivedVisited);
                break;
            case ActionType.torpedo:
                processFireTorpedo(map.islands, map.torpedo.fromReach, action, map.enemyPossibility);
                break;
            case ActionType.silence:
                processSilence(action, map.reachSilence, map.enemyPossibility, map.islands, map.enemyPath);
                break;
            case ActionType.mine:
                processEnemyMine(action, map.reachSilence, map.enemyPossibility, map.islands, map.enemyPath);
                break;
            case ActionType.trigger:
                processEnemyTrigger(action, map.reachSilence, map.enemyPossibility, map.islands, map.enemyPath);
                break;
        }
    }
    private void processMyAction(Map map, Action action, Player me, Player enemy)
    {
        switch (action.type)
        {
            case ActionType.move:
                processMove(map.myPossibility, action, map.islands, map.myPath, map.myDerivedVisited);
                break;
            case ActionType.surface:
                processSurface(map.myPossibility, action, map.myPath, map.islands, map.myDerivedVisited);
                clearMyVisitedPlace(map.visited, me.x, me.y);
                break;
            case ActionType.torpedo:
                processFireTorpedo(map.islands, map.torpedo.fromReach, action, map.myPossibility);
                break;
            case ActionType.silence:
                processSilence(action, map.reachSilence, map.myPossibility, map.islands, map.myPath);
                fillVisitedOnMySilence(map.visited, me.x, me.y, action);
                break;
            case ActionType.mine:
                processMyMine(action, map);
                break;
            case ActionType.trigger:
                processMyTrigger(action, map);
                break;
        }
    }
    public Ability DecideOnCharge(Map map, Player me, Player enemy)
    {
        if (map.enemyPossibility.total < 10 && me.torpedoCooldown > 0)
            return Ability.TORPEDO;
        if (map.enemyPossibility.total > 50 && me.sonarCooldown > 0)
            return Ability.SONAR;
        if (me.silenceCooldown > 0)
            return Ability.SILENCE;
        if (me.mineCooldown > 0)
            return Ability.MINE;
        if (me.torpedoCooldown > 0)
            return Ability.TORPEDO;
        if (me.sonarCooldown > 0)
            return Ability.SONAR;
        return Ability.TORPEDO;
    }
    public void CheckNoPossibilityOverVisited(VisitedState[,] derivedVisited, PossiblePositions possibility)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (derivedVisited[x, y] == VisitedState.old && possibility.map[x, y])
                {
                    possibility.total--;
                    possibility.map[x, y] = false;
                }
            }
        }
    }
    public Order Decide(Map map, Player me, Player enemy)
    {
        //my actions first
        me.previousOrders.LastOrDefault()?.actions.Where(a => a.type == ActionType.sonar).ToList().ForEach(a => processMySonar(map, a));
        var myDamageActions = me.previousOrders.LastOrDefault()?.actions.Where(a => a.type == ActionType.trigger || a.type == ActionType.torpedo).ToList();
        if (myDamageActions != null && myDamageActions.Count > 0)
            processDamageImpact(myDamageActions, enemy.previousHealth, enemy.health, enemy.previousOrders.Last().actions, map.enemyPossibility);
        me.previousOrders.LastOrDefault()?.actions.Where(a => a.type != ActionType.sonar).ToList().ForEach(a => processMyAction(map, a, me, enemy));

        //enemy actions second
        enemy.previousOrders.LastOrDefault()?.actions.Where(a => a.type == ActionType.sonar).ToList().ForEach(a => processEnemySonar(map, a, me));
        var enemyDamageActions = enemy.previousOrders.LastOrDefault()?.actions.Where(a => a.type == ActionType.trigger || a.type == ActionType.torpedo).ToList();
        if (enemyDamageActions != null && enemyDamageActions.Count > 0)
            processDamageImpact(enemyDamageActions, me.previousHealth, me.health, me.previousOrders.Last().actions, map.myPossibility);
        enemy.previousOrders.LastOrDefault()?.actions.Where(a => a.type != ActionType.sonar).ToList().ForEach(a => processEnemyAction(map, a, me));

        map.visited[me.x, me.y] = true;
        if (map.enemyPossibility.total < 1)
            Console.Error.WriteLine("ERROR enemy possibility is less than 1");
        if (map.myPossibility.map[me.x, me.y] == false)
            Console.Error.WriteLine("ERROR I'm inprobable");
        if (map.myPossibility.total == 1)
            MarkVisitedPath(map.myDerivedVisited, map.myPath, map.myPossibility);
        if (map.enemyPossibility.total == 1)
            MarkVisitedPath(map.enemyDerivedVisited, map.enemyPath, map.enemyPossibility);
        CheckNoPossibilityOverVisited(map.myDerivedVisited, map.myPossibility);
        CheckNoPossibilityOverVisited(map.enemyDerivedVisited, map.enemyPossibility);

        var result = decideOnNextActions(map, me, enemy);
        return result;
    }

    private void MarkVisitedPath(VisitedState[,] possibleVisited, PathMark[,] path, PossiblePositions possibility)
    {
        var targetX = 0;
        var targetY = 0;
        var found = false;
        for (int x = 0; x < height && !found; x++)
        {
            for (int y = 0; y < width && !found; y++)
            {
                if (possibility.map[x, y])
                {
                    targetX = x;
                    targetY = y;
                    found = true;
                }
            }
        }
        MarkVisitedPathRecurs(possibleVisited, path, targetX, targetY, 0, 0);
        possibleVisited[targetX, targetY] = VisitedState.fresh;
    }

    public void MarkVisitedPathRecurs(VisitedState[,] possibleVisited, PathMark[,] path, int x, int y, int dX, int dY)
    {
        if (S.pathCenterX + dX < 0
            || S.pathCenterY + dY < 0
            || S.pathCenterX + dX >= S.pathWidth
            || S.pathCenterY + dY >= S.pathHeight
            || path[S.pathCenterX + dX, S.pathCenterY + dY].processed
            || !path[S.pathCenterX + dX, S.pathCenterY + dY].visited)
        {
            return;
        }
        path[S.pathCenterX + dX, S.pathCenterY + dY].processed = true;
        possibleVisited[x + dX, y + dY] = VisitedState.old;
        MarkVisitedPathRecurs(possibleVisited, path, x, y, dX + 1, dY);
        MarkVisitedPathRecurs(possibleVisited, path, x, y, dX - 1, dY);
        MarkVisitedPathRecurs(possibleVisited, path, x, y, dX, dY + 1);
        MarkVisitedPathRecurs(possibleVisited, path, x, y, dX, dY - 1);
    }

    private Order decideOnNextActions(Map map, Player me, Player enemy)
    {
        var result = new Order();
        var addedActionInCycle = true;
        var moved = false;
        var surfaces = false;
        var torpeded = false;
        var silenced = false;
        var minedOrTriggered = false;
        var soned = false;
        var noCDs = me.torpedoCooldown == 0 && me.sonarCooldown == 0 && me.silenceCooldown == 0 && me.mineCooldown == 0;
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
            if (!addedActionInCycle && (map.myPossibility.total < 40 || noCDs) && me.silenceCooldown == 0 && !silenced)
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
            if (!addedActionInCycle && !minedOrTriggered && (me.mineCooldown == 0 || map.myMinefield.mines.Count > 0))
            {
                if (map.myMinefield.mines.Count > 0)
                {
                    var action = this.findBestMineTrigger(map, me);
                    if (action != null)
                    {
                        result.actions.Add(action);
                        addedActionInCycle = true;
                        minedOrTriggered = true;
                    }
                }
                if (!minedOrTriggered && me.mineCooldown == 0)
                {
                    var action = this.findBestMinePlacement(map, me);
                    if (action != null)
                    {
                        result.actions.Add(action);
                        addedActionInCycle = true;
                        minedOrTriggered = true;
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
            if (!addedActionInCycle && !surfaces && !moved && !silenced && !torpeded && !soned && !minedOrTriggered)
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
        var possibility = map.myPossibility.Clone();
        var action = new Action() { type = ActionType.silence };
        processSilence(action, map.reachSilence, possibility, map.islands, map.myPath);
        if (possibility.total < map.myPossibility.total + 4)
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
            var possibilityList = new List<PossiblePositions> { map.myPossibility };
            var steps = 4;
            var currentWay = new char[steps + 2];
            currentWay[1] = dir;
            if (!S.isOutOfBoundsOrIsland(map.islands, me.x + S.MoveX(dir), me.y + S.MoveY(dir)) && !map.visited[me.x + S.MoveX(dir), me.y + S.MoveY(dir)])
            {
                var currentMaxCells = findMoveDirectionRecurs(map.islands, map.paint, me.x + S.MoveX(dir), me.y + S.MoveY(dir), visitedList, map.ClonePath(map.myPath), possibilityList, steps, 1, ref currentWay, map.myDerivedVisited.Clone() as VisitedState[,]);
                if (currentMaxCells > maxPossibileCells)
                {
                    maxPossibileCells = currentMaxCells;
                    result = new Action() { type = ActionType.move, direction = currentWay[1] };
                }
            }
        }
        return result;
    }

    private double findMoveDirectionRecurs(bool[,] islands, int[,] paint, int x, int y, List<bool[,]> visitedList, PathMark[,] path, List<PossiblePositions> possibilityList, int maxDepth, int currentStep, ref char[] currentWay, VisitedState[,] derivedVisited)
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
        fillPossiblePositionOnMove(possibility, new Action() { direction = dir, type = ActionType.move }, islands, derivedVisited);
        if (currentStep <= maxDepth)
        {
            var maxPossibility = double.MinValue;
            char[] childrenMaxPossibilityWay = null;

            foreach (var newDir in S.possibleDirections)
            {
                currentWay[currentStep + 1] = newDir;
                var newPossibility = findMoveDirectionRecurs(islands, paint, x + S.MoveX(newDir), y + S.MoveY(newDir), visitedList, path, possibilityList, maxDepth, currentStep + 1, ref currentWay, derivedVisited);
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
                        totalPushForce += 1.0 / Math.Sqrt((x - island.x) * (x - island.x) + (y - island.y) * (y - island.y));
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

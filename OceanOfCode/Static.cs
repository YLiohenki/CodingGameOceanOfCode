using System;
using System.Collections.Generic;
using System.Text;

public static class S
{
    public static int centerX = 7;
    public static int centerY = 7;
    public static int enemyPathCenterX = 14;
    public static int enemyPathCenterY = 14;
    public static char[] possibleDirections = new char[] { 'N', 'E', 'W', 'S' };
    public static int CoordToSector(int x, int y)
    {
        return 1 + (y / 5) * 3 + (x / 5);
    }

    public static void EraseRecordedPath(bool[,] recordedPath, int width, int height)
    {
        for (int x = 0; x < width * 2 - 1; ++x)
        {
            for (int y = 0; y < height * 2 - 1; ++y)
            {
                recordedPath[x, y] = false;
            }
        }
        recordedPath[width - 1, height - 1] = true;
    }

    public static void SectorToCoord(int sector, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = ((sector - 1) % 3) * 5;
        maxX = ((sector - 1) % 3 + 1) * 5 - 1;
        minY = ((sector - 1) / 3) * 5;
        maxY = ((sector - 1) / 3 + 1) * 5 - 1;
    }
    public static bool isOutOfBoundsOrIsland(bool[,] islands, int x, int y, int width, int height)
    {
        return x < 0 || x >= width || y < 0 || y >= height || islands[x, y];
    }
    public static int MoveX(char direction)
    {
        switch (direction)
        {
            case 'N':
                return 0;
            case 'S':
                return 0;
            case 'E':
                return 1;
            case 'W':
                return -1;
        }
        return 0;
    }

    public static int MoveY(char direction)
    {
        switch (direction)
        {
            case 'N':
                return -1;
            case 'S':
                return 1;
            case 'E':
                return 0;
            case 'W':
                return 0;
        }
        return 0;
    }
}

using System;
using System.Collections.Generic;
using System.Text;

public class S
{
    public S(int _width, int _height)
    {
        height = _height;
        width = _width;
    }
    public int height ;
    public int width;
    public int centerX = 7;
    public int centerY = 7;
    public int enemyPathCenterX = 14;
    public int enemyPathCenterY = 14;
    public char[] possibleDirections = new char[] { 'N', 'E', 'W', 'S' };
    public int[][] adjustedCells = new int[][] { new int[] { -1, -1 }, new int[] { -1, 0 }, new int[] { -1, 1 }, new int[] { 0, -1 }, new int[] { 0, 1 }, new int[] { 1, -1 }, new int[] { 1, 0 }, new int[] { 1, 1 } };
    public int CoordToSector(int x, int y)
    {
        return 1 + (y / 5) * 3 + (x / 5);
    }

    public void EraseRecordedPath(bool[,] recordedPath)
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


    public void SectorToCoord(int sector, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = ((sector - 1) % 3) * 5;
        maxX = ((sector - 1) % 3 + 1) * 5 - 1;
        minY = ((sector - 1) / 3) * 5;
        maxY = ((sector - 1) / 3 + 1) * 5 - 1;
    }
    public bool isOutOfBoundsOrIsland(bool[,] islands, int x, int y)
    {
        return x < 0 || x >= width || y < 0 || y >= height || islands[x, y];
    }
    public int MoveX(char direction)
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

    public int MoveY(char direction)
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

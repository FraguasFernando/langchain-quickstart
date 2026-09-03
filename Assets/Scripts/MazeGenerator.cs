using System.Collections.Generic;

namespace MazePrototype
{
    /// <summary>
    /// Pure C# maze generator. No Unity types, no MonoBehaviour — it just produces
    /// a grid of booleans that says "this tile is a wall" / "this tile is open floor".
    ///
    /// Algorithm: RECURSIVE BACKTRACKER (a depth-first search that carves passages).
    /// It produces a "perfect" maze: every cell is reachable from every other cell,
    /// and there is exactly one path between any two cells. So the centre is ALWAYS
    /// reachable from the spawn corner — solvability is guaranteed by construction.
    /// </summary>
    public class MazeGenerator
    {
        // Direction indices used all over this class. Order matters: opposite of d is (d + 2) % 4.
        // 0 = North (+y), 1 = East (+x), 2 = South (-y), 3 = West (-x)
        private static readonly int[] DirX = { 0, 1, 0, -1 };
        private static readonly int[] DirY = { 1, 0, -1, 0 };

        /// <summary>Maze size in CELLS (not tiles). A 15x15 maze has 15x15 rooms.</summary>
        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>The seed actually used, so it can be logged and replayed.</summary>
        public int Seed { get; private set; }

        /// <summary>
        /// The "occupancy grid" the geometry builder consumes.
        /// It is (Width * 2 + 1) x (Height * 2 + 1) tiles: every maze cell becomes one
        /// open tile, and the tiles between cells are either carved open (a doorway)
        /// or left solid (a wall). The outer ring is always solid, so the maze is closed.
        /// true  = solid wall tile
        /// false = walkable floor tile
        /// </summary>
        public bool[,] Solid { get; private set; }

        /// <summary>Tile width of <see cref="Solid"/>.</summary>
        public int TileWidth => Width * 2 + 1;

        /// <summary>Tile height of <see cref="Solid"/>.</summary>
        public int TileHeight => Height * 2 + 1;

        /// <summary>
        /// Generates a maze. <paramref name="seed"/> &lt;= 0 means "pick a random seed".
        /// </summary>
        public MazeGenerator(int width, int height, int seed)
        {
            // Clamp to something sane so a typo in the inspector cannot produce a 0-sized
            // (or absurdly huge) maze and blow up later code.
            Width = width < 2 ? 2 : (width > 200 ? 200 : width);
            Height = height < 2 ? 2 : (height > 200 ? 200 : height);

            // A seed of 0 or less means "surprise me". Environment.TickCount changes every
            // millisecond, so every Play session gets a different maze.
            Seed = seed > 0 ? seed : System.Environment.TickCount & 0x7FFFFFFF;
            if (Seed <= 0) Seed = 1; // paranoia: TickCount can be 0 right after boot

            Generate();
        }

        private void Generate()
        {
            var rng = new System.Random(Seed);

            // links[x, y] is a 4-bit mask: bit d set means "passage open from this cell
            // towards direction d". Filled in as we carve.
            var links = new int[Width, Height];
            var visited = new bool[Width, Height];

            // An explicit stack instead of real recursion. Same algorithm, but a 200x200
            // maze would be 40,000 frames deep with real recursion and could blow the stack.
            var stack = new Stack<(int x, int y)>();

            int startX = 0, startY = 0;
            visited[startX, startY] = true;
            stack.Push((startX, startY));

            // Scratch buffer for the "which neighbours have I not visited yet" check.
            // Reused every iteration so we are not allocating inside the hot loop.
            var candidates = new int[4];

            while (stack.Count > 0)
            {
                var (cx, cy) = stack.Peek();

                // Collect every neighbour that is inside the grid and not yet visited.
                int candidateCount = 0;
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + DirX[d];
                    int ny = cy + DirY[d];
                    if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                    if (visited[nx, ny]) continue;
                    candidates[candidateCount++] = d;
                }

                if (candidateCount == 0)
                {
                    // Dead end: backtrack. This is the "backtracker" half of the algorithm.
                    stack.Pop();
                    continue;
                }

                // Pick one unvisited neighbour at random and knock down the wall between us.
                int dir = candidates[rng.Next(candidateCount)];
                int tx = cx + DirX[dir];
                int ty = cy + DirY[dir];

                links[cx, cy] |= 1 << dir;                  // open this side of the wall
                links[tx, ty] |= 1 << ((dir + 2) % 4);      // ...and the other side

                visited[tx, ty] = true;
                stack.Push((tx, ty));
            }

            BuildOccupancyGrid(links);
        }

        /// <summary>
        /// Converts the per-cell passage mask into the tile grid the mesh builder uses.
        /// Cell (x, y) lives at tile (2x + 1, 2y + 1); the tile between two linked cells
        /// gets carved open.
        /// </summary>
        private void BuildOccupancyGrid(int[,] links)
        {
            int tw = TileWidth;
            int th = TileHeight;

            Solid = new bool[tw, th];

            // Start with everything solid, then carve. (bool[,] defaults to false, so we
            // have to fill it explicitly.)
            for (int i = 0; i < tw; i++)
                for (int j = 0; j < th; j++)
                    Solid[i, j] = true;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int ti = x * 2 + 1;
                    int tj = y * 2 + 1;
                    Solid[ti, tj] = false; // the room itself

                    // Carve the doorway tile for each open side. Carving from both cells is
                    // harmless (it just sets the same tile to false twice).
                    for (int d = 0; d < 4; d++)
                    {
                        if ((links[x, y] & (1 << d)) == 0) continue;
                        Solid[ti + DirX[d], tj + DirY[d]] = false;
                    }
                }
            }
        }

        /// <summary>Tile coordinates of the centre cell — where the landmark cube goes.</summary>
        public void GetCentreTile(out int tileX, out int tileY)
        {
            tileX = (Width / 2) * 2 + 1;
            tileY = (Height / 2) * 2 + 1;
        }

        /// <summary>Tile coordinates of cell (0,0) — where the player spawns.</summary>
        public void GetStartTile(out int tileX, out int tileY)
        {
            tileX = 1;
            tileY = 1;
        }
    }
}

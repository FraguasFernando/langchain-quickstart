using UnityEngine;
using UnityEngine.Rendering;

namespace MazePrototype
{
    /// <summary>
    /// ONE-STEP SETUP. Put this script on a single empty GameObject in any scene and press
    /// Play. At runtime it builds everything from code: floor, maze walls, the red centre
    /// landmark, a light (if the scene has none), the player, and the first-person camera.
    /// Nothing needs to be wired up by hand in the Inspector.
    ///
    /// Every entry into Play mode regenerates the maze with a fresh random seed, unless
    /// you set <see cref="seed"/> to a positive number.
    /// </summary>
    [DisallowMultipleComponent]
    public class MazeBootstrap : MonoBehaviour
    {
        [Header("Maze")]
        [Tooltip("Maze size in CELLS (rooms), not tiles. 15 x 15 is a good starting point.")]
        public int mazeWidth = 15;
        public int mazeHeight = 15;

        [Tooltip("0 or less = new random maze every Play. Above 0 = always the same maze (handy for debugging).")]
        public int seed = 0;

        [Header("Dimensions")]
        [Tooltip("Size of one tile in world units. This is BOTH the corridor width and the wall thickness.")]
        public float tileSize = 2f;

        [Tooltip("How tall the walls are, in world units.")]
        public float wallHeight = 3f;

        [Header("Colours (greybox)")]
        public Color wallColor = new Color(0.85f, 0.85f, 0.85f);
        public Color floorColor = new Color(0.55f, 0.55f, 0.58f);
        [Tooltip("The landmark cube sitting at the centre of the maze.")]
        public Color markerColor = new Color(0.85f, 0.12f, 0.12f);

        [Header("Player")]
        public float moveSpeed = 4.5f;
        public float mouseSensitivity = 2.0f;
        [Tooltip("Eye height above the floor, in world units.")]
        public float eyeHeight = 1.6f;

        [Header("Scene setup")]
        [Tooltip("Create a directional light if the scene does not already have any light.")]
        public bool createLightIfMissing = true;

        [Tooltip("Flat ambient colour, so corridors are not pitch black. Set to black to disable.")]
        public Color ambientLight = new Color(0.35f, 0.35f, 0.38f);

        // Everything we spawn goes under this object, so a rebuild can wipe it in one call.
        private GameObject _root;

        private MazeGenerator _maze;

        private void Start()
        {
            Build();
        }

        /// <summary>
        /// Tears down anything we built before and builds the whole prototype from scratch.
        /// Exposed as a context-menu item: right-click the component header while in Play
        /// mode and choose "Rebuild Maze" to roll a new one without restarting.
        /// </summary>
        [ContextMenu("Rebuild Maze")]
        public void Build()
        {
            // DestroyImmediate (not Destroy) on purpose: Destroy is deferred to the end of
            // the frame, and the FindObjectsByType calls further down would then still find
            // the old light/camera and skip recreating them.
            if (_root != null) DestroyImmediate(_root);
            _root = new GameObject("MazeRoot");

            _maze = new MazeGenerator(mazeWidth, mazeHeight, seed);
            Debug.Log($"[MazeBootstrap] Built {_maze.Width}x{_maze.Height} maze with seed {_maze.Seed}. " +
                      "Set the 'seed' field to this number to replay the exact same maze.");

            // Guard against silly inspector values that would produce degenerate geometry.
            if (tileSize <= 0.1f) tileSize = 2f;
            if (wallHeight <= 0.1f) wallHeight = 3f;

            var wallMat = CreateMaterial(wallColor);
            var floorMat = CreateMaterial(floorColor);
            var markerMat = CreateMaterial(markerColor);

            BuildFloor(floorMat);
            BuildWalls(wallMat);
            BuildCentreMarker(markerMat);
            EnsureLighting();
            SpawnPlayer();
        }

        // ------------------------------------------------------------------ geometry

        private void BuildFloor(Material mat)
        {
            float w = _maze.TileWidth * tileSize;
            float h = _maze.TileHeight * tileSize;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(_root.transform, false);

            // Unity's built-in Plane is 10x10 world units at scale 1, hence the /10.
            floor.transform.localScale = new Vector3(w / 10f, 1f, h / 10f);
            floor.transform.position = new Vector3(w * 0.5f, 0f, h * 0.5f);
            ApplyMaterial(floor, mat);
        }

        private void BuildWalls(Material mat)
        {
            int tw = _maze.TileWidth;
            int th = _maze.TileHeight;
            bool[,] solid = _maze.Solid;

            var wallsParent = new GameObject("Walls");
            wallsParent.transform.SetParent(_root.transform, false);

            // Instead of one cube per solid tile, we merge each horizontal RUN of solid tiles
            // into a single stretched cube. Same shape, same collision, far fewer objects.
            for (int j = 0; j < th; j++)
            {
                int i = 0;
                while (i < tw)
                {
                    if (!solid[i, j]) { i++; continue; }

                    int runStart = i;
                    while (i < tw && solid[i, j]) i++;
                    int runLength = i - runStart;

                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"Wall_{runStart}_{j}";
                    cube.transform.SetParent(wallsParent.transform, false);
                    cube.transform.localScale = new Vector3(runLength * tileSize, wallHeight, tileSize);
                    cube.transform.position = new Vector3(
                        (runStart + runLength * 0.5f) * tileSize,
                        wallHeight * 0.5f,
                        (j + 0.5f) * tileSize);
                    ApplyMaterial(cube, mat);
                }
            }
        }

        private void BuildCentreMarker(Material mat)
        {
            _maze.GetCentreTile(out int cx, out int cy);
            float size = tileSize * 0.6f;

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "CentreMarker";
            marker.transform.SetParent(_root.transform, false);
            marker.transform.localScale = Vector3.one * size;
            marker.transform.position = new Vector3(
                (cx + 0.5f) * tileSize,
                size * 0.5f,
                (cy + 0.5f) * tileSize);
            ApplyMaterial(marker, mat);

            // Purely a landmark — no win logic. Remove the collider so you can walk into it.
            var col = marker.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        // ------------------------------------------------------------------ scene services

        private void EnsureLighting()
        {
            // Flat ambient guarantees the corridors are readable even if the scene has no
            // skybox. Set ambientLight to black in the Inspector to leave lighting alone.
            if (ambientLight.maxColorComponent > 0f)
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = ambientLight;
            }

            if (!createLightIfMissing) return;

            // Only add a light if the scene genuinely has none, so we don't double up on
            // whatever lighting an existing scene already has.
            var existing = FindObjectsByType<Light>(FindObjectsSortMode.None);
            if (existing != null && existing.Length > 0) return;

            var lightGo = new GameObject("Directional Light");
            lightGo.transform.SetParent(_root.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private void SpawnPlayer()
        {
            _maze.GetStartTile(out int sx, out int sy);

            var player = new GameObject("Player");
            player.transform.SetParent(_root.transform, false);
            player.transform.position = new Vector3(
                (sx + 0.5f) * tileSize,
                0.05f,                       // a hair above the floor so we don't start clipped
                (sy + 0.5f) * tileSize);

            // Face down whichever corridor actually leaves the start cell, so you never
            // spawn nose-first into a wall. The tile east of the start is (sx + 1, sy).
            bool eastOpen = !_maze.Solid[sx + 1, sy];
            player.transform.rotation = Quaternion.Euler(0f, eastOpen ? 90f : 0f, 0f);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = Mathf.Min(0.4f, tileSize * 0.3f); // stay narrower than the corridor
            cc.center = new Vector3(0f, 0.9f, 0f);        // capsule sits on the feet at y = 0
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.03f;

            var camTransform = SetUpCamera(player.transform);

            var fpc = player.AddComponent<FirstPersonController>();
            fpc.cameraTransform = camTransform;
            fpc.moveSpeed = moveSpeed;
            fpc.mouseSensitivity = mouseSensitivity;
        }

        /// <summary>
        /// Reuses whatever camera the scene already has (so we never end up rendering twice),
        /// or creates one if there is none. Returns the camera's transform.
        /// </summary>
        private Transform SetUpCamera(Transform player)
        {
            var cam = FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);

            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();

                // Only add an AudioListener if the scene has none — Unity warns loudly
                // about having two.
                var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
                if (listeners == null || listeners.Length == 0)
                    camGo.AddComponent<AudioListener>();
            }

            cam.gameObject.tag = "MainCamera"; // built-in tag, so Camera.main keeps working
            cam.transform.SetParent(player, false);
            cam.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
            cam.transform.localRotation = Quaternion.identity;
            cam.nearClipPlane = 0.05f;         // stops walls clipping when you hug them
            cam.farClipPlane = 500f;

            return cam.transform;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// Builds a plain lit material for the current render pipeline.
        /// Shader names differ between Built-in ("Standard") and URP/HDRP ("... /Lit"), and
        /// colour property names differ too, so we probe for whatever is available.
        /// </summary>
        private static Material CreateMaterial(Color color)
        {
            // Ask which pipeline is actually ACTIVE first. Just probing shader names is not
            // enough: a Built-in project with the URP package merely installed would still
            // find "Universal Render Pipeline/Lit" and render everything magenta.
            var pipelineAsset = GraphicsSettings.currentRenderPipeline;

            Shader shader;
            if (pipelineAsset == null)
            {
                shader = Shader.Find("Standard"); // null pipeline asset = Built-in pipeline
            }
            else
            {
                // e.g. "UniversalRenderPipelineAsset" or "HDRenderPipelineAsset"
                string assetName = pipelineAsset.GetType().Name;
                shader = assetName.Contains("HD")
                    ? Shader.Find("HDRP/Lit")
                    : Shader.Find("Universal Render Pipeline/Lit");
            }

            // Fallbacks, in case the expected shader is missing or stripped from a build.
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default"); // always present

            // Shader.Find can still return null in a stripped build; a null shader would
            // throw, so fall back to whatever material Unity gives a fresh primitive.
            if (shader == null)
            {
                Debug.LogWarning("[MazeBootstrap] No usable shader found; using default material.");
                return null;
            }

            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); // URP / HDRP
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);         // Built-in
            return mat;
        }

        /// <summary>
        /// Assigns a material to a primitive. If CreateMaterial could not find a shader it
        /// returns null, and in that case we leave the primitive's default material alone
        /// rather than blanking it out.
        /// </summary>
        private static void ApplyMaterial(GameObject go, Material mat)
        {
            if (mat == null) return;
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
        }
    }
}

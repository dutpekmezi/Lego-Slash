using System.Collections.Generic;
using UnityEngine;

namespace TrashRush.Game
{
    internal static class VoxelBrickSlash
    {
        public static bool Launch(Camera camera, SlashConfig config, Vector3 start, Vector3 end,
            out SlashObject slashObject, List<SlashObject> hitObjects = null)
        {
            slashObject = null;
            if (camera == null) return false;
            // The game uses an orthographic camera. Extrude the whole cut through
            // its clipping range so every intersecting trigger, including rear cells, is found.
            var right = camera.transform.right;
            var up = camera.transform.up;
            var forward = camera.transform.forward;
            var delta = end - start;
            var planarDelta = right * Vector3.Dot(delta, right) + up * Vector3.Dot(delta, up);
            float length = planarDelta.magnitude;
            if (length <= Mathf.Epsilon) return false;
            var midpoint = (start + end) * .5f;
            var planeCenter = midpoint - forward * Vector3.Dot(midpoint - camera.transform.position, forward);
            float depth = (camera.farClipPlane - camera.nearClipPlane) * .5f;
            var center = planeCenter + forward * (camera.nearClipPlane + depth);
            var rotation = Quaternion.LookRotation(forward, planarDelta / length);
            var extents = new Vector3(config != null ? config.BrickCutHalfWidth : .04f, length * .5f, depth);
            // Objects are moved by tweens, often before the next physics step.
            Physics.SyncTransforms();
            var hits = Physics.OverlapBox(center, extents, rotation, ~0, QueryTriggerInteraction.Collide);
            var bricks = new HashSet<VoxelBrick>();
            var ownerBricks = new Dictionary<SlashObject, List<VoxelBrick>>();
            foreach (var hit in hits)
            {
                var brick = hit.GetComponentInParent<VoxelBrick>();
                if (brick == null || brick.IsDetached) continue;
                var owner = brick.GetComponentInParent<SlashObject>();
                if (owner != null && !owner.IsSlashable) continue;
                if (slashObject == null && owner != null) slashObject = owner;
                bricks.Add(brick);
            }
            // Collect by model before changing any hierarchy.
            foreach (var brick in bricks)
            {
                var owner = brick.GetComponentInParent<SlashObject>();
                if (owner == null)
                {
                    brick.Launch(config);
                    continue;
                }
                if (!ownerBricks.TryGetValue(owner, out var candidates))
                    ownerBricks.Add(owner, candidates = new List<VoxelBrick>());
                candidates.Add(brick);
            }
            foreach (var entry in ownerBricks)
            {
                var owner = entry.Key;
                var candidates = entry.Value;
                hitObjects?.Add(owner);
                if (owner.Data.Hardness <= 0f)
                {
                    if (owner.Data.Slashable)
                        owner.CutAndComplete(camera, start, end, config);
                    else
                        owner.ReleaseAllBricks(config);
                    continue;
                }
                bool split = owner.CanSplit(config) && owner.Data.Hardness < 10f;
                var breakRatio = split ? (config != null ? config.SplitHitBreakRatio : 0.2f) : 1f;
                int releaseCount = Mathf.FloorToInt(candidates.Count * owner.Data.BrickReleaseRatio * breakRatio);
                // Sample without replacement so resistance is spread across the cut.
                for (int i = 0; i < releaseCount; i++)
                {
                    int selected = Random.Range(i, candidates.Count);
                    var brick = candidates[selected];
                    candidates[selected] = candidates[i];
                    candidates[i] = brick;
                    brick.Launch(config, owner.Data);
                }
                if (split) owner.SplitAlongCut(camera, start, end, config);
            }
            // A resistant model still registers a hit even when no bricks are released.
            return bricks.Count > 0;
        }
    }
}

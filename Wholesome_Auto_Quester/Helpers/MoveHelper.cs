using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Move = SmoothMove.Move;
using Timer = System.Threading.Timer;

namespace FlXProfiles {
    internal static class MoveHelper {
        private static readonly object Lock = new object();
        private static Task _currentMovementTask;
        private static CancellationTokenSource _currentMovementToken;
        private static Vector3 _currentMovementTarget;

        public static bool IsMovementThreadRunning {
            get {
                lock (Lock) {
                    return !_currentMovementTask?.Finished() ?? false;
                }
            }
        }

        public static Vector3 CurrentMovementTarget {
            get {
                lock (Lock) {
                    return IsMovementThreadRunning ? _currentMovementTarget : null;
                }
            }
        }

        public static void StopAllMove() {
            StopCurrentMovementThread();
            MovementManager.StopMoveNewThread();
            MovementManager.StopMove();
        }

        public static void StopCurrentMovementThread() {
            try {
                lock (Lock) {
                    _currentMovementToken?.Cancel();
                    if (!_currentMovementTask?.Wait(5000, _currentMovementToken?.Token ?? CancellationToken.None) ??
                        false)
                        Logger.LogError("Unable to end movement thread.");
                    // ResetCurrentMovementCache();
                }
            } catch {
                // We can safely ignore this
            }
        }

        private static void ResetCurrentMovementCache() {
            lock (Lock) {
                _currentMovementTask = null;
                _currentMovementToken = null;
                _currentMovementTarget = null;
            }
        }

        public static CancellationTokenSource StartGoToThread(Vector3 target,
            bool face = true, bool precise = false, Func<bool> abortIf = null, float randomizeEnd = 0,
            float randomization = 0, bool checkCurrent = true, float precision = 1,
            bool shortCut = false, int jumpRareness = 2, bool showPath = false) {
            if (MovementManager.InMovement)
                MovementManager.StopMove();

            lock (Lock) {
                if (IsMovementThreadRunning) {
                    if (checkCurrent && CurrentMovementTarget.DistanceTo(target) < 2)
                        return null;
                    StopCurrentMovementThread();
                }

                var cts = new CancellationTokenSource();
                Task goToTask = Task.Factory.StartNew(()
                    => Move.GoTo(target, face, precise, randomizeEnd, randomization, cts.Token,
                        precision: precision, shortCut: shortCut, jumpRareness: jumpRareness,
                        showPath: showPath, avoidDangerousEnemies: true), cts.Token).ContinueWith(
                    task => ResetCurrentMovementCache(), cts.Token);

                if (abortIf != null)
                    Task.Factory.StartNew(() => {
                        while (!goToTask.IsCompleted && !goToTask.IsCanceled && !goToTask.IsFaulted
                               && !cts.Token.IsCancellationRequested) {
                            if (abortIf()) {
                                cts.Cancel();
                                StopAllMove();
                                break;
                            }

                            Thread.Sleep(100);
                        }
                    }, cts.Token);

                _currentMovementToken = cts;
                _currentMovementTarget = target;
                _currentMovementTask = goToTask;
                return cts;
            }
        }

        private static bool Finished(this Task task) => task.IsCompleted || task.IsCanceled || task.IsFaulted;

        public static CancellationTokenSource StartMoveAlongThread(List<Vector3> pathRaw,
            bool face = true, bool precise = false, Func<bool> abortIf = null, float randomization = 0,
            bool checkCurrent = true, float precision = 1, List<byte> customRadius = null, bool shortCut = false,
            int jumpRareness = 2, bool showPath = false) {
            // var path = new List<Vector3>(pathRaw.Count) {ObjectManager.Me.PositionWithoutType};
            // path.AddRange(pathRaw);
            if (pathRaw.Count <= 0) {
                Logger.LogError("Called MoveAlongThread without a path.");
                return null;
            }

            List<Vector3> path = pathRaw;

            if (IsMovementThreadRunning) {
                if (checkCurrent && CurrentMovementTarget.DistanceTo(path.LastOrDefault()) < 2)
                    return null;
                StopCurrentMovementThread();
            }

            if (MovementManager.InMovement)
                MovementManager.StopMove();

            var cts = new CancellationTokenSource();
            Task moveAlongTask = Task.Factory.StartNew(()
                        => Move.MoveAlong(path, face, precise, randomization, cts.Token, customRadius: customRadius,
                            shortCut: shortCut, precision: precision, jumpRareness: jumpRareness, showPath: showPath),
                    cts.Token)
                .ContinueWith(task => ResetCurrentMovementCache(), cts.Token);

            if (abortIf != null)
                Task.Factory.StartNew(() => {
                    while (!moveAlongTask.IsCompleted && !moveAlongTask.IsCanceled && !moveAlongTask.IsFaulted
                           && !cts.Token.IsCancellationRequested) {
                        if (abortIf()) {
                            cts.Cancel();
                            StopAllMove();
                            break;
                        }

                        Thread.Sleep(100);
                    }
                }, cts.Token);

            _currentMovementToken = cts;
            _currentMovementTarget = path.LastOrDefault();
            _currentMovementTask = moveAlongTask;
            return cts;
        }

        public static (List<Vector3>, List<byte>) SplitPathData(this List<(Vector3, byte)> pathWithData) {
            var path = new List<Vector3>(pathWithData.Count);
            var rnds = new List<byte>(pathWithData.Count);

            foreach ((Vector3 point, byte radian) in pathWithData) {
                path.Add(point);
                rnds.Add(radian);
            }

            return (path, rnds);
        }

        public static List<Vector3> PathFromClosestPoint(List<Vector3> path) {
            if (path.Count <= 0) return path;
            var sortingList = new Tuple<short, Vector3>[path.Count];

            for (short i = 0; i < path.Count; i++) sortingList[i] = new Tuple<short, Vector3>(i, path[i]);

            Vector3 myPosition = ObjectManager.Me.PositionWithoutType;
            IOrderedEnumerable<Tuple<short, Vector3>> sortedPath =
                sortingList.OrderBy(tuple => tuple.Item2.DistanceTo(myPosition));
            short startIndex = sortedPath.FirstOrDefault(tuple => !TraceLine.TraceLineGo(tuple.Item2))?.Item1 ?? -1;

            if (startIndex == -1) {
                Logger.LogDebug("Can't see any point of the path. PathFinding to closest point.");
                (short newStartIndex, Vector3 newStart) = sortedPath.First();
                List<Vector3> startPath = PathFinder.FindPath(newStart, out bool foundPath);
                if (!foundPath) {
                    Logger.LogDebug("Could not find a path. Just returning the normal path.");
                    return path;
                }

                startPath.AddRange(path.GetRange(newStartIndex, path.Count - newStartIndex));
                return startPath;
            }

            if (startIndex > 0) Logger.LogDebug($"Skipped the first {startIndex} steps of the path.");

            return startIndex != 0 ? path.GetRange(startIndex, path.Count - startIndex) : path;
        }

        public static bool MoveToWait(Vector3 target, int targetPrecision = 4, int maxRestarts = 3,
            bool face = true, bool precise = false, Func<bool> abortIf = null, float randomizeEnd = 0,
            float randomization = 0, bool checkCurrent = true, float precision = 1,
            bool shortCut = false, int jumpRareness = 2, bool showPath = false) {
            var movementStarts = 0;
            do {
                if (!IsMovementThreadRunning || CurrentMovementTarget.DistanceTo(target) > targetPrecision) {
                    if (++movementStarts > 3) {
                        StopAllMove();
                        return false;
                    }

                    StartGoToThread(target, face, precise, abortIf, randomizeEnd, randomization, checkCurrent,
                        precision,
                        shortCut, jumpRareness, showPath);
                    Thread.Sleep(500);
                }

                Thread.Sleep(50);
            } while (IsMovementThreadRunning);

            return true;
        }

        public static bool ToPositionAndInteractWithNpc(Vector3 expectedPosition, int npcEntry,
            long timeout = 60 * 1000) {
            WoWUnit foundNpc = ToolBox.FindClosestUnitByEntry(npcEntry);

            if (foundNpc == null) {
                Logger.LogDebug($"Going to {expectedPosition} to interact with NPC#{npcEntry}");
                long timeoutTime = ToolBox.CurTime + timeout;
                if (!MoveToWait(expectedPosition, randomizeEnd: 3, abortIf: () =>
                        (foundNpc = ToolBox.FindClosestUnitByEntry(npcEntry)) != null || ToolBox.CurTime > timeoutTime)
                    || foundNpc == null) {
                    Logger.Log($"Failed to find NPC #{npcEntry} after {timeout / 1000}s.");
                    return false;
                }

                // StartGoToThread(expectedPosition, randomizeEnd: 3f);
                // while (foundNpc == null) {
                //     if (!IsMovementThreadRunning && ToolBox.CurTime >= timeoutTime) {
                //         Logger.Log($"Failed to find NPC #{npcEntry} after {timeout / 1000}s.");
                //         return false;
                //     }
                //
                //     Thread.Sleep(50);
                //     foundNpc = ToolBox.FindClosestUnitByEntry(npcEntry);
                // }
            }

            Logger.LogDebug($"Found NPC {foundNpc.Name} to interact with! Moving to him.");
            MoveToWait(foundNpc.PositionWithoutType, face: false, abortIf: () => foundNpc.InInteractDistance());

            Logger.LogDebug($"Interacting with {foundNpc.Name}.");
            Interact.InteractGameObject(foundNpc.GetBaseAddress);
            return true;
        }

        public static bool ToPositionAndInteractWithGameObject(Vector3 expectedPosition, int objectEntry,
            long timeout = 5 * 1000) {
            WoWGameObject foundObject = ToolBox.FindClosestGameObjectByEntry(objectEntry);

            if (foundObject == null) {
                Logger.LogDebug($"Going to {expectedPosition} to interact with GameObject#{objectEntry}");
                long timeoutTime = ToolBox.CurTime + timeout;
                if (!MoveToWait(expectedPosition, randomizeEnd: 3, abortIf: () =>
                        (foundObject = ToolBox.FindClosestGameObjectByEntry(objectEntry)) != null ||
                        ToolBox.CurTime > timeoutTime)
                    || foundObject == null) {
                    Logger.Log($"Failed to find GameObject #{foundObject} after {timeout / 1000}s.");
                    return false;
                }

                // StartGoToThread(expectedPosition, randomizeEnd: 3f);
                // long timeoutTime = ToolBox.CurTime + timeout;
                // while (foundObject == null) {
                //     if (!IsMovementThreadRunning && ToolBox.CurTime >= timeoutTime) {
                //         Logger.Log($"Failed to find GameObject #{objectEntry} after {timeout / 1000}s.");
                //         return false;
                //     }
                //
                //     Thread.Sleep(50);
                //     foundObject = ToolBox.FindClosestGameObjectByEntry(objectEntry);
                // }
            }

            Logger.LogDebug($"Found GameObject {foundObject.Name} to interact with! Moving to it.");
            MoveToWait(foundObject.Position, face: false, abortIf: () => foundObject.IsGoodInteractDistance);

            Logger.LogDebug($"Interacting with {foundObject.Name}.");
            Interact.InteractGameObject(foundObject.GetBaseAddress);
            return true;
        }
    }
}
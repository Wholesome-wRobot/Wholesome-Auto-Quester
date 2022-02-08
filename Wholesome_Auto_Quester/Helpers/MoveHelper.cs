using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class MoveHelper
{
    private static readonly Vector3 Empty = Vector3.Empty;
    private static readonly object Lock = new();
    private static bool Running = false;

    private static void Wait()
    {
        if (CurrentTarget != Empty)
        {
            Running = false;
            Monitor.Wait(Lock);
        }
    }

    public static void StopAllMove(bool stopWalking = false)
    {
        lock (Lock)
        {
            Wait();
        }
        if (stopWalking)
        {
            MovementManager.StopMove();
            MovementManager.StopMoveTo();
        }
    }

    public static Vector3 CurrentTarget { get; private set; } = Empty;

    public static bool IsMovementThreadRunning => CurrentTarget != Empty/* || MovementManager.CurrentPath.Count > 0*/;

    public static float GetCurrentPathRemainingDistance()
    {
        float result = 0;
        List<Vector3> currentPath = MovementManager.CurrentPath;
        Vector3 nextNode = MovementManager.CurrentMoveTo;
        result += ObjectManager.Me.Position.DistanceTo(nextNode);
        bool nextNodeFound = false;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            if (!nextNodeFound && currentPath[i] == nextNode)
            {
                nextNodeFound = true;
            }
            if (nextNodeFound)
            {
                result += currentPath[i].DistanceTo(currentPath[i + 1]);
            }
        }
        return result;
    }

    public static float GetCurrentPathTotalDistance()
    {
        float result = 0;
        List<Vector3> currentPath = MovementManager.CurrentPath;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            result += currentPath[i].DistanceTo(currentPath[i + 1]);
        }
        return result;
    }

    public static void StartGoToThread(Vector3 target, string log = null)
    {
        lock (Lock)
        {
            if (CurrentTarget.Equals(target))
            {
                return;
            }

            Wait();

            CurrentTarget = target;
            Running = true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (log != null)
                {
                    Logger.Log(log);
                }
                GoToTask.ToPosition(target, conditionExit: _ => Running);
                //Logger.LogDebug($"GoToTask finished towards {target}");
                lock (Lock)
                {
                    CurrentTarget = Empty;
                    Monitor.Pulse(Lock);
                }
            });
        }
    }
}

public class WAQPath
{
    public List<Vector3> Path;
    public float Distance;
    public bool IsReachable => Distance > 0f;
    public Vector3 Destination => Path.Last();
    public WAQPath(List<Vector3> path, float distance)
    {
        Path = path;
        Distance = distance;
    }
}
using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;

public static class MoveHelper
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
        }
    }

    public static Vector3 CurrentTarget { get; private set; } = Empty;

    public static bool IsMovementThreadRunning => CurrentTarget != Empty;

    public static void StartGoToThread(Vector3 target, string log)
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
                //Logger.Log($"GoToTask started towards {target}");
                GoToTask.ToPosition(target, conditionExit: _ => Running);
                //Logger.Log($"GoToTask finished towards {target}");
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
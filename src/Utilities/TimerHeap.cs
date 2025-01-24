using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Object = System.Object;

public class TimerHeap
{

    public string name;

    public delegate double Func();

    private List<Timer> heap = new List<Timer>();
    private List<IEnumerator<int>> activeCoroutines = new List<IEnumerator<int>>();
    public double time { get; private set; }

    private Dictionary<Object, Timer> keys = new Dictionary<Object, Timer>();

    public class Timer
    {
        internal static readonly Func InvalidatedFunc = delegate ()
        {
            return 0d;
        };

        internal Object key { get; set; }
        public double fireTime { get; internal set; }
        internal Func func { get; private set; }
        internal Timer next;

        public override string ToString()
        {
            return "<Fire: " + fireTime + " key: " + key + " func: " + func + ">";
        }

        public Timer(Object key, double time, Func func)
        {
            this.key = key;
            this.fireTime = time;
            this.func = func;
        }

        public void Invalidate()
        {
            func = InvalidatedFunc;
        }

        internal Timer Remove(Timer timer)
        {
            return (this == timer ? next : this);
        }

        public bool isValid()
        {
            return func != InvalidatedFunc;
        }
    }

    private void Swap(int a, int b)
    {
        var temp = heap[a];
        heap[a] = heap[b];
        heap[b] = temp;
    }

    private void UpHeap(int i)
    {
        int parent = (i - 1) / 2;
        if (i > 0 && heap[i].fireTime < heap[parent].fireTime)
        {
            Swap(i, parent); UpHeap(parent);
        }
    }

    private int DownHeapChild(int i)
    {
        int left = 2 * i + 1;
        int right = 2 * i + 2;

        if (right < heap.Count)
        {
            return (heap[left].fireTime < heap[right].fireTime ? left : right);
        }
        else
        {
            return left;
        }
    }

    private void DownHeap(int i)
    {
        var child = DownHeapChild(i);

        if (child < heap.Count && heap[child].fireTime < heap[i].fireTime)
        {
            Swap(i, child); DownHeap(child);
        }
    }

    public Timer ScheduleAt(Object key, double time, Func func)
    {
        if (time < this.time) throw new Exception("Scheduled time is in the past.");

        var timer = new Timer(key, time, func);
        heap.Add(timer);
        UpHeap(heap.Count - 1);

        keys.TryGetValue(key, out timer.next);
        if (key != null)
        {
            keys[key] = timer;
        }

        return timer;
    }

    public double GetFireTime(Object key)
    {
        if (HasTimerForKey(key))
        {
            return keys[key].fireTime;
        }
        else
        {
            return -1;
        }
    }

    public string InspectKey(Object key)
    {
        if (HasTimerForKey(key))
        {
            Timer t = keys[key];
            return t.ToString();
        }
        else
        {
            return "<No key: " + key + ">";
        }
    }

    public Timer Schedule(Object key, double delay, Func func)
    {
        //		Debug.Log ("Scheduled to run at: " + (time + delay));
        return ScheduleAt(key, time + delay, func);
    }

    public Timer ScheduleAt(Object key, double time, IEnumerator<double> coro)
    {
        return ScheduleAt(key, time, delegate () { return (coro.MoveNext() ? coro.Current : 0.0); });
    }

    public Timer Schedule(Object key, double delay, IEnumerator<double> coro)
    {
        return ScheduleAt(key, time + delay, coro);
    }

    public void InvalidateTimersForKey(Object key)
    {
        if (keys.ContainsKey(key))
        {
            for (var timer = keys[key]; timer != null; timer = timer.next)
            {
                timer.Invalidate();
            }
            keys.Remove(key);
        }
    }


    public bool HasTimerForKey(Object key)
    {
        return keys.ContainsKey(key) && keys[key] != null;
    }

    public void InvalidateListOfTimersForKey(List<Object> list)
    {
        foreach (Object key in list)
        {
            if (keys.ContainsKey(key))
            {
                for (var timer = keys[key]; timer != null; timer = timer.next)
                {
                    timer.Invalidate();
                }
                keys.Remove(key);
            }
        }
    }

    public void StepTo(double targetTime)
    {
        while (heap.Count > 0)
        {
            var timer = heap[0];
            if (timer.fireTime > targetTime)
            {
                this.time = targetTime;
                break;
            }
            this.time = timer.fireTime;
            var delay = timer.func();
            if (delay > 0)
            {
                timer.fireTime += delay;
                DownHeap(0);
            }
            else
            {
                int last = heap.Count - 1;
                Swap(0, last);
                heap.RemoveAt(last);
                if (heap.Count > 0) DownHeap(0);

                var key = timer.key;
                if (keys.ContainsKey(key))
                {
                    var replacement = keys[key].Remove(timer);
                    if (replacement == null)
                    {
                        keys.Remove(key);
                    }
                    else
                    {
                        keys[key] = replacement;
                    }
                }
            }
        }

        List<IEnumerator<int>> toRemove = new List<IEnumerator<int>>();
        // advance coroutines:

        //		Debug.Log (this.name + " @ " + this.time+ " - Checking active coroutines: " + activeCoroutines.Count);
        for (int i = 0; i < activeCoroutines.Count; i++)
        {
            IEnumerator<int> ie = activeCoroutines[i];
            ie.MoveNext();
            int removeThis = ie.Current;
            if (removeThis == 1)
            {
                toRemove.Add(ie);
            }
        }
        for (int i = 0; i < toRemove.Count; i++)
        {
            activeCoroutines.Remove(toRemove[i]);
        }

        this.time = targetTime;
    }

    public void Step(double dt)
    {
        StepTo(time + dt);
    }

    public IEnumerator<double> Coro(double t)
    {
        for (int i = 0; i <= t; i++)
        {
            Log.Info("" + time);
            yield return 1.0;
        }
    }

    public void Schedule(double t)
    {
        Schedule("foo", t / 10.0, Coro(t));
    }


    static public void TestTimerHeap()
    {
        var timers = new TimerHeap();

        timers.Schedule(9);

        timers.Step(100);
    }

}